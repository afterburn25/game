using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Simulation.Diplomacy;
using Game.Simulation.Models;

namespace Game.Persistence;

/// <summary>
/// Authoritative campaign-level persistence boundary.
///
/// Save format v8 established the stable galaxy/species/body/combat payload. Format v9 wraps
/// that proven payload with Diplomacy's bounded persistence-ready snapshot instead of moving
/// political state into GalaxyState or duplicating the v8 galaxy serializer. Preset-aware
/// galaxies use v10; their v11 campaign wrapper causes older readers to reject new canonical
/// catalogs instead of silently regenerating procedural worlds. Old catalogs remain v8/v9.
/// </summary>
public sealed class CampaignStatePersistenceService
{
    public const int LegacyFormatVersion = 9;
    public const int PresetFormatVersion = 11;
    public const int CurrentFormatVersion = 13;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = false,
    };

    private readonly CampaignSaveService _galaxyPersistence;

    public CampaignStatePersistenceService(CampaignSaveService? galaxyPersistence = null)
    {
        _galaxyPersistence = galaxyPersistence ?? new CampaignSaveService();
    }

    public void Save(
        string path,
        GalaxyState galaxy,
        double simulationDays,
        DiplomacyState diplomacy) =>
        SaveCore(path, galaxy, simulationDays, diplomacy, preserveExistingBackup: false);

    /// <summary>
    /// Atomically replaces the primary campaign file without rotating the existing .bak file.
    /// This is reserved for the first successful repair save after startup recovered from that
    /// known-good backup. Ordinary saves must continue through Save so backup rotation resumes.
    /// </summary>
    public void SavePreservingBackup(
        string path,
        GalaxyState galaxy,
        double simulationDays,
        DiplomacyState diplomacy) =>
        SaveCore(path, galaxy, simulationDays, diplomacy, preserveExistingBackup: true);

    private void SaveCore(
        string path,
        GalaxyState galaxy,
        double simulationDays,
        DiplomacyState diplomacy,
        bool preserveExistingBackup)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A save path is required.", nameof(path));
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(diplomacy);
        if (!double.IsFinite(simulationDays) || simulationDays < 0.0)
            throw new ArgumentOutOfRangeException(nameof(simulationDays), "Simulation time must be finite and non-negative.");

        var snapshot = diplomacy.Snapshot();
        DiplomacySnapshotInvariantValidator.Validate(snapshot);
        DiplomacyCampaignReferenceValidator.Validate(galaxy, snapshot);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var nonce = Guid.NewGuid().ToString("N");
        var stagedGalaxyPath = path + $".{nonce}.galaxy";
        var finalTempPath = path + $".{nonce}.tmp";

        try
        {
            _galaxyPersistence.Save(stagedGalaxyPath, galaxy, simulationDays);
            var root = JsonNode.Parse(File.ReadAllText(stagedGalaxyPath))?.AsObject()
                ?? throw new InvalidDataException("Galaxy persistence did not produce a campaign JSON object.");

            var galaxyFormat = root["FormatVersion"]?.GetValue<int>()
                ?? throw new InvalidDataException("Galaxy persistence omitted FormatVersion.");
            if (galaxyFormat != CampaignSaveService.LegacyFormatVersion && galaxyFormat != CampaignSaveService.PresetFormatVersion && galaxyFormat != CampaignSaveService.CurrentFormatVersion)
            {
                throw new InvalidDataException(
                    $"Expected galaxy payload format {CampaignSaveService.CurrentFormatVersion}, got {galaxyFormat}.");
            }

            root["FormatVersion"] = galaxyFormat + 1;
            root["Diplomacy"] = JsonSerializer.SerializeToNode(snapshot, JsonOptions)
                ?? throw new InvalidDataException("Diplomacy snapshot could not be serialized.");

            File.WriteAllText(finalTempPath, root.ToJsonString(JsonOptions));
            if (File.Exists(path))
            {
                File.Replace(
                    finalTempPath,
                    path,
                    preserveExistingBackup ? null : path + ".bak",
                    ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(finalTempPath, path);
            }
        }
        finally
        {
            DeleteIfPresent(stagedGalaxyPath);
            DeleteIfPresent(stagedGalaxyPath + ".tmp");
            DeleteIfPresent(stagedGalaxyPath + ".bak");
            DeleteIfPresent(finalTempPath);
        }
    }

    public LoadedCampaignState Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A save path is required.", nameof(path));

        var json = File.ReadAllText(path);
        var root = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidDataException("Save file did not contain a campaign JSON object.");
        var formatVersion = root["FormatVersion"]?.GetValue<int>()
            ?? throw new InvalidDataException("Save file did not declare FormatVersion.");

        if (formatVersion < 1 || formatVersion > CurrentFormatVersion)
        {
            throw new InvalidDataException(
                $"Unsupported campaign save format {formatVersion}; maximum supported is {CurrentFormatVersion}.");
        }

        if (formatVersion <= CampaignSaveService.LegacyFormatVersion || formatVersion == CampaignSaveService.PresetFormatVersion || formatVersion == CampaignSaveService.CurrentFormatVersion)
        {
            // Legacy saves did not persist political state. Do not infer contacts, trust, claims,
            // treaties or wars from omniscient galaxy data during migration.
            var legacy = _galaxyPersistence.Load(path);
            return new LoadedCampaignState(
                legacy.Galaxy,
                legacy.SimulationDays,
                legacy.GameVersion,
                legacy.SavedAtUtc,
                new DiplomacyState());
        }

        if (formatVersion != LegacyFormatVersion && formatVersion != PresetFormatVersion && formatVersion != CurrentFormatVersion)
            throw new InvalidDataException($"No migration path is defined for campaign save format {formatVersion}.");

        var diplomacyNode = root["Diplomacy"]
            ?? throw new InvalidDataException($"Format v{formatVersion} save is missing the authoritative Diplomacy snapshot.");

        DiplomacyStateSnapshot snapshot;
        try
        {
            snapshot = diplomacyNode.Deserialize<DiplomacyStateSnapshot>(JsonOptions)
                ?? throw new InvalidDataException($"Format v{formatVersion} Diplomacy snapshot was empty.");
            DiplomacySnapshotInvariantValidator.Validate(snapshot);
        }
        catch (DiplomacySnapshotValidationException ex)
        {
            throw new InvalidDataException($"Format v{formatVersion} Diplomacy snapshot failed strict invariant validation.", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Format v{formatVersion} Diplomacy snapshot could not be decoded.", ex);
        }

        // v9 wraps procedural v8; v11 wraps preset-aware v10; v13 wraps surface-construction v12.
        // Normalize to the matching galaxy version so neither path silently reinterprets the
        // other catalog. Species/body/Combat validation remains in CampaignSaveService.
        var normalized = (JsonObject)root.DeepClone();
        normalized["FormatVersion"] = formatVersion - 1;
        normalized.Remove("Diplomacy");

        var normalizedPath = path + $".{Guid.NewGuid():N}.v8load";
        try
        {
            File.WriteAllText(normalizedPath, normalized.ToJsonString(JsonOptions));
            var galaxy = _galaxyPersistence.Load(normalizedPath);
            DiplomacyCampaignReferenceValidator.Validate(galaxy.Galaxy, snapshot);
            return new LoadedCampaignState(
                galaxy.Galaxy,
                galaxy.SimulationDays,
                galaxy.GameVersion,
                galaxy.SavedAtUtc,
                DiplomacyState.Restore(snapshot));
        }
        finally
        {
            DeleteIfPresent(normalizedPath);
            DeleteIfPresent(normalizedPath + ".tmp");
            DeleteIfPresent(normalizedPath + ".bak");
        }
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}

public sealed record LoadedCampaignState(
    GalaxyState Galaxy,
    double SimulationDays,
    string GameVersion,
    DateTimeOffset SavedAtUtc,
    DiplomacyState Diplomacy);

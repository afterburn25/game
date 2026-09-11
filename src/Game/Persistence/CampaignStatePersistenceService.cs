using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Simulation.Diplomacy;
using Game.Simulation.Models;
using Game.Simulation.Research.Adaptive;

namespace Game.Persistence;

public sealed record CampaignRestorationProgress(double Fraction, string Status)
{
    public CampaignRestorationProgress Validate()
    {
        if (!double.IsFinite(Fraction) || Fraction < 0 || Fraction >= 1)
            throw new ArgumentOutOfRangeException(nameof(Fraction));
        if (string.IsNullOrWhiteSpace(Status)) throw new ArgumentException("A restoration status is required.", nameof(Status));
        return this;
    }
}

/// <summary>
/// Authoritative campaign-level persistence boundary.
///
/// Save format v17 wraps the authoritative v16 galaxy payload with Diplomacy and bounded
/// Adaptive Research snapshots. Older v9/v11/v13/v15 campaigns retain their historical
/// procedural, preset, surface, and Adaptive Research migration semantics.
/// </summary>
public sealed class CampaignStatePersistenceService
{
    public const int LegacyFormatVersion = 9;
    public const int PresetFormatVersion = 11;
    public const int SurfaceFormatVersion = 13;
    public const int AdaptiveFormatVersion = 15;
    public const int CurrentFormatVersion = 17;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = false,
    };

    private readonly CampaignSaveService _galaxyPersistence;
    private readonly AdaptiveResearchStrategicRuntime _adaptiveResearchRuntime;
    private readonly AdaptiveResearchCampaignFactory _adaptiveResearchFactory;
    private readonly AdaptiveResearchCampaignSnapshotCodec _adaptiveResearchCodec;

    public CampaignStatePersistenceService(
        CampaignSaveService? galaxyPersistence = null,
        AdaptiveResearchStrategicRuntime? adaptiveResearchRuntime = null)
    {
        _galaxyPersistence = galaxyPersistence ?? new CampaignSaveService();
        _adaptiveResearchRuntime = adaptiveResearchRuntime ?? AdaptiveResearchStrategicRuntime.LoadFromDirectory(
            AdaptiveResearchDataLocator.FindDataRoot());
        _adaptiveResearchFactory = new AdaptiveResearchCampaignFactory(_adaptiveResearchRuntime);
        _adaptiveResearchCodec = new AdaptiveResearchCampaignSnapshotCodec(_adaptiveResearchRuntime);
    }

    public AdaptiveResearchCampaignState CreateAdaptiveResearchState(GalaxyState galaxy) =>
        _adaptiveResearchFactory.Create(galaxy);

    public void Save(
        string path,
        GalaxyState galaxy,
        double simulationDays,
        DiplomacyState diplomacy) =>
        Save(path, galaxy, simulationDays, diplomacy, _adaptiveResearchFactory.Create(galaxy));

    public void Save(
        string path,
        GalaxyState galaxy,
        double simulationDays,
        DiplomacyState diplomacy,
        AdaptiveResearchCampaignState adaptiveResearch) =>
        SaveCore(path, galaxy, simulationDays, diplomacy, adaptiveResearch, preserveExistingBackup: false);

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
        SavePreservingBackup(path, galaxy, simulationDays, diplomacy, _adaptiveResearchFactory.Create(galaxy));

    public void SavePreservingBackup(
        string path,
        GalaxyState galaxy,
        double simulationDays,
        DiplomacyState diplomacy,
        AdaptiveResearchCampaignState adaptiveResearch) =>
        SaveCore(path, galaxy, simulationDays, diplomacy, adaptiveResearch, preserveExistingBackup: true);

    internal void SaveDeveloperPayload(
        string path,
        GalaxyState galaxy,
        double simulationDays,
        DiplomacyState diplomacy) =>
        SaveDeveloperPayload(path, galaxy, simulationDays, diplomacy, _adaptiveResearchFactory.Create(galaxy));

    internal void SaveDeveloperPayload(
        string path,
        GalaxyState galaxy,
        double simulationDays,
        DiplomacyState diplomacy,
        AdaptiveResearchCampaignState adaptiveResearch) =>
        SaveCore(path, galaxy, simulationDays, diplomacy, adaptiveResearch, preserveExistingBackup: false, developerPayload: true);

    private void SaveCore(
        string path,
        GalaxyState galaxy,
        double simulationDays,
        DiplomacyState diplomacy,
        AdaptiveResearchCampaignState adaptiveResearch,
        bool preserveExistingBackup,
        bool developerPayload = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A save path is required.", nameof(path));
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(diplomacy);
        ArgumentNullException.ThrowIfNull(adaptiveResearch);
        if ((galaxy.DeveloperSession is not null) != developerPayload)
            throw new InvalidOperationException(developerPayload
                ? "Developer payload serialization requires explicit Developer session provenance."
                : "A Developer campaign cannot be written as a Player save. Use Developer campaign persistence.");
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
            if (developerPayload)
                _galaxyPersistence.SaveDeveloperPayload(stagedGalaxyPath, galaxy, simulationDays);
            else
                _galaxyPersistence.Save(stagedGalaxyPath, galaxy, simulationDays);
            var root = JsonNode.Parse(File.ReadAllText(stagedGalaxyPath))?.AsObject()
                ?? throw new InvalidDataException("Galaxy persistence did not produce a campaign JSON object.");

            var galaxyFormat = root["FormatVersion"]?.GetValue<int>()
                ?? throw new InvalidDataException("Galaxy persistence omitted FormatVersion.");
            if (galaxyFormat != CampaignSaveService.CurrentFormatVersion)
            {
                throw new InvalidDataException(
                    $"Expected galaxy payload format {CampaignSaveService.CurrentFormatVersion}, got {galaxyFormat}.");
            }

            var campaignFormat = CurrentFormatVersion;
            root["FormatVersion"] = campaignFormat;
            root["GalaxyFormatVersion"] = galaxyFormat;
            root["Diplomacy"] = JsonSerializer.SerializeToNode(snapshot, JsonOptions)
                ?? throw new InvalidDataException("Diplomacy snapshot could not be serialized.");
            root["AdaptiveResearch"] = JsonSerializer.SerializeToNode(
                new AdaptiveResearchCampaignSnapshotCodec(adaptiveResearch.Runtime).Capture(adaptiveResearch), JsonOptions)
                ?? throw new InvalidDataException("Adaptive Research campaign snapshot could not be serialized.");

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

    public LoadedCampaignState Load(string path, Action<CampaignRestorationProgress>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A save path is required.", nameof(path));

        void Report(double fraction, string status) => progress?.Invoke(new CampaignRestorationProgress(fraction, status).Validate());
        Report(.04, "Reading saved campaign");
        var json = File.ReadAllText(path);
        Report(.16, "Decoding saved campaign");
        var root = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidDataException("Save file did not contain a campaign JSON object.");
        if (root.ContainsKey("DeveloperFormatVersion"))
            throw new InvalidDataException("Developer campaign envelopes cannot be opened as Player saves.");
        var formatVersion = root["FormatVersion"]?.GetValue<int>()
            ?? throw new InvalidDataException("Save file did not declare FormatVersion.");

        if (formatVersion < 1 || formatVersion > CurrentFormatVersion)
        {
            throw new InvalidDataException(
                $"Unsupported campaign save format {formatVersion}; maximum supported is {CurrentFormatVersion}.");
        }

        if (formatVersion <= CampaignSaveService.LegacyFormatVersion ||
            formatVersion is CampaignSaveService.PresetFormatVersion or CampaignSaveService.SurfaceFormatVersion or CampaignSaveService.CurrentFormatVersion)
        {
            // Legacy saves did not persist political state. Do not infer contacts, trust, claims,
            // treaties or wars from omniscient galaxy data during migration.
            Report(.35, "Restoring galaxy state");
            var legacy = _galaxyPersistence.Load(path);
            Report(.82, "Rebuilding campaign systems");
            var adaptive = _adaptiveResearchFactory.Create(legacy.Galaxy);
            Report(.97, "Validating restored campaign");
            return new LoadedCampaignState(
                legacy.Galaxy,
                legacy.SimulationDays,
                legacy.GameVersion,
                legacy.SavedAtUtc,
                new DiplomacyState(),
                adaptive);
        }

        if (formatVersion != LegacyFormatVersion && formatVersion != PresetFormatVersion &&
            formatVersion != SurfaceFormatVersion && formatVersion != AdaptiveFormatVersion &&
            formatVersion != CurrentFormatVersion)
            throw new InvalidDataException($"No migration path is defined for campaign save format {formatVersion}.");

        Report(.25, "Restoring diplomacy");
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
        // v15 records its historical inner galaxy version; v17 always records v16.
        // Normalize to the matching galaxy version so neither path silently reinterprets the
        // other catalog. Species/body/Combat validation remains in CampaignSaveService.
        Report(.42, "Restoring galaxy state");
        var normalized = (JsonObject)root.DeepClone();
        normalized["FormatVersion"] = formatVersion is AdaptiveFormatVersion or CurrentFormatVersion
            ? ReadGalaxyFormatVersion(root, formatVersion)
            : formatVersion - 1;
        normalized.Remove("Diplomacy");
        normalized.Remove("AdaptiveResearch");
        normalized.Remove("GalaxyFormatVersion");

        var normalizedPath = path + $".{Guid.NewGuid():N}.v8load";
        try
        {
            File.WriteAllText(normalizedPath, normalized.ToJsonString(JsonOptions));
            var galaxy = _galaxyPersistence.Load(normalizedPath);
            Report(.72, "Validating campaign references");
            DiplomacyCampaignReferenceValidator.Validate(galaxy.Galaxy, snapshot);
            Report(.82, "Restoring research progress");
            var adaptiveResearch = formatVersion is AdaptiveFormatVersion or CurrentFormatVersion
                ? RestoreAdaptiveResearch(root, galaxy.Galaxy, formatVersion)
                : _adaptiveResearchFactory.Create(galaxy.Galaxy);
            Report(.97, "Finalizing restored campaign");
            return new LoadedCampaignState(
                galaxy.Galaxy,
                galaxy.SimulationDays,
                galaxy.GameVersion,
                galaxy.SavedAtUtc,
                DiplomacyState.Restore(snapshot),
                adaptiveResearch);
        }
        finally
        {
            DeleteIfPresent(normalizedPath);
            DeleteIfPresent(normalizedPath + ".tmp");
            DeleteIfPresent(normalizedPath + ".bak");
        }
    }

    private static int ReadGalaxyFormatVersion(JsonObject root, int campaignFormatVersion)
    {
        var version = root["GalaxyFormatVersion"]?.GetValue<int>()
            ?? throw new InvalidDataException($"Format v{campaignFormatVersion} save is missing GalaxyFormatVersion.");
        var supported = campaignFormatVersion == CurrentFormatVersion
            ? version == CampaignSaveService.CurrentFormatVersion
            : version is CampaignSaveService.LegacyFormatVersion or CampaignSaveService.PresetFormatVersion or CampaignSaveService.SurfaceFormatVersion;
        if (!supported)
            throw new InvalidDataException($"Format v{campaignFormatVersion} save references unsupported galaxy format {version}.");
        return version;
    }

    private AdaptiveResearchCampaignState RestoreAdaptiveResearch(
        JsonObject root,
        GalaxyState galaxy,
        int formatVersion)
    {
        var node = root["AdaptiveResearch"]
            ?? throw new InvalidDataException($"Format v{formatVersion} save is missing Adaptive Research state.");
        try
        {
            var snapshot = node.Deserialize<AdaptiveResearchCampaignSnapshot>(JsonOptions)
                ?? throw new InvalidDataException($"Format v{formatVersion} Adaptive Research state was empty.");
            return _adaptiveResearchCodec.Restore(galaxy, snapshot);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Format v{formatVersion} Adaptive Research state could not be decoded.", ex);
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
    DiplomacyState Diplomacy,
    AdaptiveResearchCampaignState AdaptiveResearch);

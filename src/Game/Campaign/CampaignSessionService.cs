using System;
using System.IO;
using Game.Persistence;
using Game.Simulation.Diplomacy;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Campaign;

public enum CampaignBootstrapSource
{
    NewCampaign,
    LoadedSave,
    RecoveredFromBackup,
    RecoveredFromInvalidSave,
}

public sealed record CampaignBootstrapResult(
    GalaxyState Galaxy,
    DiplomacyState Diplomacy,
    double SimulationDays,
    CampaignBootstrapSource Source,
    string GameVersion,
    DateTimeOffset? SavedAtUtc,
    string? LoadFailure)
{
    public long Seed => Galaxy.Seed;
    public bool WasLoaded => Source is CampaignBootstrapSource.LoadedSave or CampaignBootstrapSource.RecoveredFromBackup;
    public bool RecoveredFromBackup => Source == CampaignBootstrapSource.RecoveredFromBackup;
    public bool RecoveredFromInvalidSave => Source == CampaignBootstrapSource.RecoveredFromInvalidSave;
}

/// <summary>
/// Plain-C# campaign lifecycle boundary. It composes deterministic generation and versioned
/// campaign persistence without depending on Godot. Galaxy state remains owned by the v8
/// serializer while format v9 adds Diplomacy beside it at the campaign boundary.
/// </summary>
public sealed class CampaignSessionService
{
    private readonly GalaxyGenerator _generator;
    private readonly CampaignStatePersistenceService _saveService;

    public CampaignSessionService(
        GalaxyGenerator? generator = null,
        CampaignStatePersistenceService? saveService = null)
    {
        _generator = generator ?? new GalaxyGenerator();
        _saveService = saveService ?? new CampaignStatePersistenceService();
    }

    public CampaignBootstrapResult CreateNew(long seed, GalaxyGenerationSettings? settings = null)
    {
        var galaxy = _generator.Generate(seed, settings);
        return new CampaignBootstrapResult(
            galaxy,
            new DiplomacyState(),
            0.0,
            CampaignBootstrapSource.NewCampaign,
            global::Game.GameVersion.Current,
            null,
            null);
    }

    public CampaignBootstrapResult LoadOrCreate(
        string savePath,
        long fallbackSeed,
        GalaxyGenerationSettings? fallbackSettings = null)
    {
        if (string.IsNullOrWhiteSpace(savePath))
            throw new ArgumentException("A save path is required.", nameof(savePath));

        var backupPath = savePath + ".bak";
        if (File.Exists(savePath))
        {
            try
            {
                return Load(savePath, CampaignBootstrapSource.LoadedSave, loadFailure: null);
            }
            catch (Exception primaryFailure)
            {
                if (File.Exists(backupPath))
                {
                    try
                    {
                        return Load(
                            backupPath,
                            CampaignBootstrapSource.RecoveredFromBackup,
                            $"Primary autosave failed and the previous backup was recovered.\n{primaryFailure}");
                    }
                    catch (Exception backupFailure)
                    {
                        return RecoverNewCampaign(
                            fallbackSeed,
                            fallbackSettings,
                            $"Primary autosave failed:\n{primaryFailure}\nBackup autosave also failed:\n{backupFailure}");
                    }
                }

                return RecoverNewCampaign(
                    fallbackSeed,
                    fallbackSettings,
                    $"Primary autosave failed and no backup was available.\n{primaryFailure}");
            }
        }

        if (File.Exists(backupPath))
        {
            try
            {
                return Load(
                    backupPath,
                    CampaignBootstrapSource.RecoveredFromBackup,
                    "Primary autosave was missing; the previous backup was recovered.");
            }
            catch (Exception backupFailure)
            {
                return RecoverNewCampaign(
                    fallbackSeed,
                    fallbackSettings,
                    $"Primary autosave was missing and the backup autosave failed:\n{backupFailure}");
            }
        }

        return CreateNew(fallbackSeed, fallbackSettings);
    }

    /// <summary>
    /// Compatibility overload for callers that have not yet acquired a campaign Diplomacy owner.
    /// It deliberately persists an empty state rather than inferring political knowledge.
    /// </summary>
    public void Save(string savePath, GalaxyState galaxy, double simulationDays) =>
        Save(savePath, galaxy, new DiplomacyState(), simulationDays);

    public void Save(
        string savePath,
        GalaxyState galaxy,
        DiplomacyState diplomacy,
        double simulationDays) =>
        SaveCore(savePath, galaxy, diplomacy, simulationDays, preserveExistingBackup: false);

    /// <summary>
    /// Repairs/recreates the primary autosave after startup loaded the known-good .bak file.
    /// The existing backup is deliberately preserved for this one write; ordinary Save calls
    /// resume normal primary-to-backup rotation after repair succeeds.
    /// </summary>
    public void SavePreservingBackup(
        string savePath,
        GalaxyState galaxy,
        DiplomacyState diplomacy,
        double simulationDays) =>
        SaveCore(savePath, galaxy, diplomacy, simulationDays, preserveExistingBackup: true);

    private void SaveCore(
        string savePath,
        GalaxyState galaxy,
        DiplomacyState diplomacy,
        double simulationDays,
        bool preserveExistingBackup)
    {
        if (string.IsNullOrWhiteSpace(savePath))
            throw new ArgumentException("A save path is required.", nameof(savePath));
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(diplomacy);
        if (!double.IsFinite(simulationDays) || simulationDays < 0.0)
            throw new ArgumentOutOfRangeException(nameof(simulationDays), "Simulation time must be finite and non-negative.");

        if (preserveExistingBackup)
            _saveService.SavePreservingBackup(savePath, galaxy, simulationDays, diplomacy);
        else
            _saveService.Save(savePath, galaxy, simulationDays, diplomacy);
    }

    private CampaignBootstrapResult Load(
        string path,
        CampaignBootstrapSource source,
        string? loadFailure)
    {
        var loaded = _saveService.Load(path);
        return new CampaignBootstrapResult(
            loaded.Galaxy,
            loaded.Diplomacy,
            loaded.SimulationDays,
            source,
            loaded.GameVersion,
            loaded.SavedAtUtc,
            loadFailure);
    }

    private CampaignBootstrapResult RecoverNewCampaign(
        long fallbackSeed,
        GalaxyGenerationSettings? fallbackSettings,
        string loadFailure)
    {
        var recovered = CreateNew(fallbackSeed, fallbackSettings);
        return recovered with
        {
            Source = CampaignBootstrapSource.RecoveredFromInvalidSave,
            LoadFailure = loadFailure,
        };
    }
}

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
    public bool WasLoaded => Source == CampaignBootstrapSource.LoadedSave;
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

        if (!File.Exists(savePath))
            return CreateNew(fallbackSeed, fallbackSettings);

        try
        {
            var loaded = _saveService.Load(savePath);
            return new CampaignBootstrapResult(
                loaded.Galaxy,
                loaded.Diplomacy,
                loaded.SimulationDays,
                CampaignBootstrapSource.LoadedSave,
                loaded.GameVersion,
                loaded.SavedAtUtc,
                null);
        }
        catch (Exception ex)
        {
            var recovered = CreateNew(fallbackSeed, fallbackSettings);
            return recovered with
            {
                Source = CampaignBootstrapSource.RecoveredFromInvalidSave,
                LoadFailure = ex.ToString(),
            };
        }
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
        double simulationDays)
    {
        if (string.IsNullOrWhiteSpace(savePath))
            throw new ArgumentException("A save path is required.", nameof(savePath));
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(diplomacy);
        if (!double.IsFinite(simulationDays) || simulationDays < 0.0)
            throw new ArgumentOutOfRangeException(nameof(simulationDays), "Simulation time must be finite and non-negative.");

        _saveService.Save(savePath, galaxy, simulationDays, diplomacy);
    }
}

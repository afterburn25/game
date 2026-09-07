using System;
using System.IO;
using Game.Persistence;
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
/// persistence without depending on Godot, so startup/load-recovery behavior can be exercised
/// by automated validation and future non-scene-tree tooling.
/// </summary>
public sealed class CampaignSessionService
{
    private readonly GalaxyGenerator _generator;
    private readonly CampaignSaveService _saveService;

    public CampaignSessionService(
        GalaxyGenerator? generator = null,
        CampaignSaveService? saveService = null)
    {
        _generator = generator ?? new GalaxyGenerator();
        _saveService = saveService ?? new CampaignSaveService();
    }

    public CampaignBootstrapResult CreateNew(long seed, GalaxyGenerationSettings? settings = null)
    {
        var galaxy = _generator.Generate(seed, settings);
        return new CampaignBootstrapResult(
            galaxy,
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

    public void Save(string savePath, GalaxyState galaxy, double simulationDays)
    {
        if (string.IsNullOrWhiteSpace(savePath))
            throw new ArgumentException("A save path is required.", nameof(savePath));
        ArgumentNullException.ThrowIfNull(galaxy);
        if (!double.IsFinite(simulationDays) || simulationDays < 0.0)
            throw new ArgumentOutOfRangeException(nameof(simulationDays), "Simulation time must be finite and non-negative.");

        _saveService.Save(savePath, galaxy, simulationDays);
    }
}

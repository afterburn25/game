using System;
using System.Linq;
using Godot;
using Game.Campaign;
using Game.Diagnostics;
using Game.Persistence;
using Game.Simulation.Models;
using Game.Simulation.Time;

namespace Game.Presentation;

/// <summary>
/// Godot-facing adapter for the plain-C# campaign lifecycle. Presentation reset/logging stays
/// here; deterministic generation, load recovery and persistence dispatch stay in CampaignSessionService.
/// </summary>
public partial class Main
{
    private readonly CampaignSessionService _campaignSessionService = new();

    protected void RunIntegratedCampaignReady()
    {
        _font = ThemeDB.FallbackFont;
        SupportLogger.Initialize();

        var fallbackSeed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var bootstrap = _campaignSessionService.LoadOrCreate(AutosavePath, fallbackSeed);
        ApplyIntegratedCampaign(bootstrap);

        switch (bootstrap.Source)
        {
            case CampaignBootstrapSource.LoadedSave:
                SetStatus($"Loaded autosave from {bootstrap.SavedAtUtc?.LocalDateTime:g}");
                SupportLogger.Log(
                    "save",
                    $"Loaded autosave seed={_galaxy.Seed} date={CampaignCalendar.FormatDate(_clock.SimulationDays)} stage={PlayerCivilization.DevelopmentStage} format={CampaignStatePersistenceService.CurrentFormatVersion}");
                break;

            case CampaignBootstrapSource.RecoveredFromInvalidSave:
                SupportLogger.Log("save-error", bootstrap.LoadFailure ?? "Unknown autosave load failure.");
                LogIntegratedCampaignStartup("recovery");
                SetStatus("Autosave could not be loaded; generated a new 2050 campaign.");
                break;

            default:
                LogIntegratedCampaignStartup("startup");
                break;
        }

        QueueRedraw();
    }

    protected void CreateIntegratedNewCampaign()
    {
        var seed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var bootstrap = _campaignSessionService.CreateNew(seed);
        ApplyIntegratedCampaign(bootstrap);
        LogIntegratedCampaignStartup("startup");
        SetStatus("Generated a new campaign beginning January 1, 2050.");
        QueueRedraw();
    }

    protected void SaveIntegratedCampaign()
    {
        try
        {
            _campaignSessionService.Save(AutosavePath, _galaxy, _diplomacyState, _clock.SimulationDays);
            SupportLogger.Log("save", $"Autosaved seed={_galaxy.Seed} date={CampaignCalendar.FormatDate(_clock.SimulationDays)} format={CampaignStatePersistenceService.CurrentFormatVersion}");
            SetStatus("Autosave complete.");
        }
        catch (Exception ex)
        {
            SupportLogger.Log("save-error", ex.ToString());
            SetStatus("Autosave failed. See logs.", 8.0);
        }

        QueueRedraw();
    }

    protected void HandleIntegratedCloseRequest()
    {
        if (_galaxy is not null)
            SaveIntegratedCampaign();
        GetTree().Quit();
    }

    private void ApplyIntegratedCampaign(CampaignBootstrapResult bootstrap)
    {
        _galaxy = bootstrap.Galaxy;
        _diplomacyState = bootstrap.Diplomacy;
        _clock.Restore(bootstrap.SimulationDays);
        RebuildIntegratedCoreSimulation();
        ResetIntegratedCampaignPresentation();
    }

    private void ResetIntegratedCampaignPresentation()
    {
        _selectedSystemId = -1;
        _researchCandidateIndex = 0;
        _constructionCandidateIndex = 0;
        _shipDesignCandidateIndex = 0;
        _pan = Godot.Vector2.Zero;
        _zoom = 0.55f;

        foreach (var marker in _scienceFleetMarkers.Values)
            marker.QueueFree();
        _scienceFleetMarkers.Clear();
    }

    private void LogIntegratedCampaignStartup(string category)
    {
        SupportLogger.Log(
            category,
            $"Generated 2050 campaign seed={_galaxy.Seed} systems={_galaxy.Systems.Count} prewarp={_galaxy.Civilizations.Count(c => c.DevelopmentStage == CivilizationDevelopmentStage.PreWarp)} ancient={_galaxy.Civilizations.Count(c => c.IsSeededAncient)} player={PlayerCivilization.Name}");
    }
}

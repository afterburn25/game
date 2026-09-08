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
    private readonly CampaignAutosaveScheduler _autosaveScheduler = new();

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

            case CampaignBootstrapSource.RecoveredFromBackup:
                if (!string.IsNullOrWhiteSpace(bootstrap.LoadFailure))
                    SupportLogger.Log("save-recovery", bootstrap.LoadFailure);
                SupportLogger.Log(
                    "save-recovery",
                    $"Recovered backup autosave seed={_galaxy.Seed} date={CampaignCalendar.FormatDate(_clock.SimulationDays)} savedAt={bootstrap.SavedAtUtc?.LocalDateTime:g} format={CampaignStatePersistenceService.CurrentFormatVersion}");
                // Replace a missing/corrupt primary promptly, without retrying on every frame.
                // Delaying one simulation day preserves the known-good backup while the recovered
                // campaign becomes active, instead of immediately rotating a corrupt primary into it.
                _autosaveScheduler.MarkFailure(_clock.SimulationDays);
                SetStatus("Primary autosave was unavailable; recovered the previous backup. A fresh autosave is scheduled after 1 simulation day.", 8.0);
                break;

            case CampaignBootstrapSource.RecoveredFromInvalidSave:
                SupportLogger.Log("save-error", bootstrap.LoadFailure ?? "Unknown autosave load failure.");
                LogIntegratedCampaignStartup("recovery");
                if (TryPersistIntegratedCampaign(
                    logCategory: "save-recovery-checkpoint",
                    showSuccessStatus: false,
                    failureStatus: "Recovered campaign checkpoint failed; retry scheduled after 1 simulation day. See logs."))
                {
                    SetStatus("Autosave and backup could not be loaded; generated and checkpointed a new 2050 campaign.");
                }
                break;

            default:
                LogIntegratedCampaignStartup("startup");
                TryPersistIntegratedCampaign(
                    logCategory: "save-initial",
                    showSuccessStatus: false,
                    failureStatus: "Initial campaign checkpoint failed; retry scheduled after 1 simulation day. See logs.");
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

        if (TryPersistIntegratedCampaign(
            logCategory: "save-new-game",
            showSuccessStatus: false,
            failureStatus: "New campaign checkpoint failed; retry scheduled after 1 simulation day. See logs."))
        {
            SetStatus("Generated a new campaign beginning January 1, 2050.");
        }

        QueueRedraw();
    }

    protected void SaveIntegratedCampaign()
    {
        TryPersistIntegratedCampaign(
            logCategory: "save",
            showSuccessStatus: true,
            failureStatus: "Autosave failed. See logs.");
        QueueRedraw();
    }

    protected void RunIntegratedScheduledAutosave()
    {
        if (_galaxy is null)
            return;

        var simulationDays = _clock.SimulationDays;
        if (!_autosaveScheduler.IsDue(simulationDays))
            return;

        TryPersistIntegratedCampaign(
            logCategory: "autosave",
            showSuccessStatus: false,
            failureStatus: "Autosave failed; retry scheduled after 1 simulation day. See logs.");
    }

    protected void HandleIntegratedCloseRequest()
    {
        if (_galaxy is not null)
        {
            TryPersistIntegratedCampaign(
                logCategory: "save-exit",
                showSuccessStatus: false,
                failureStatus: "Exit autosave failed. See logs.");
        }

        GetTree().Quit();
    }

    private bool TryPersistIntegratedCampaign(
        string logCategory,
        bool showSuccessStatus,
        string failureStatus)
    {
        var simulationDays = _clock.SimulationDays;
        try
        {
            _campaignSessionService.Save(AutosavePath, _galaxy, _diplomacyState, simulationDays);
            _autosaveScheduler.MarkSuccess(simulationDays);
            SupportLogger.Log(
                logCategory,
                $"Autosaved seed={_galaxy.Seed} date={CampaignCalendar.FormatDate(simulationDays)} format={CampaignStatePersistenceService.CurrentFormatVersion} nextAutoDay={_autosaveScheduler.NextDueDay:0.###}");

            if (showSuccessStatus)
                SetStatus("Autosave complete.");
            return true;
        }
        catch (Exception ex)
        {
            _autosaveScheduler.MarkFailure(simulationDays);
            SupportLogger.Log("save-error", ex.ToString());
            SetStatus(failureStatus, 8.0);
            return false;
        }
    }

    private void ApplyIntegratedCampaign(CampaignBootstrapResult bootstrap)
    {
        _galaxy = bootstrap.Galaxy;
        _diplomacyState = bootstrap.Diplomacy;
        _clock.Restore(bootstrap.SimulationDays);
        _autosaveScheduler.Reset(_clock.SimulationDays);
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

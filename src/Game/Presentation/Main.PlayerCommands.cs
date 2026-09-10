using System.IO;
using System.Collections.Generic;
using System.Linq;
using Game.Diagnostics;
using Game.Presentation.Spatial;
using Game.Simulation;
using Game.Simulation.Combat;
using Game.Simulation.Research.Adaptive;
using Game.Simulation.Time;

namespace Game.Presentation;

/// <summary>
/// Narrow player-command surface used by Godot UI controls. The UI invokes the same
/// authoritative commands as the existing prototype keyboard shortcuts rather than
/// duplicating simulation rules in presentation code.
/// </summary>
public partial class Main
{
    private readonly PlayerNotificationFeed _playerNotifications = new();
    public bool UiIsPaused => _clock.Speed == SimulationClock.SpeedLevel.Paused;
    public string UiSpeedLabel => _clock.Speed == SimulationClock.SpeedLevel.Demo
        ? $"24× Developer · {_clock.EffectiveMultiplier:0.00}× effective"
        : $"{_clock.RequestedMultiplier:0}× · {_clock.EffectiveMultiplier:0.00}× effective";
    public string UiBuildLabel => $"Stellar Continuum {GameVersion.Current}";
    public string UiStatusMessage => _statusTimer > 0 ? _statusText : string.Empty;
    public IReadOnlyList<UiPlayerNotification> UiNotifications => _playerNotifications.Items;
    public bool UiIsMenuOpen => GetNodeOrNull<MainMenuLayer>("MainMenuLayer")?.IsBlockingGameplay == true;
    public ulong UiPointerCommandRevision { get; protected set; }

    private void PublishPlayerNotification(string category, string message) =>
        _playerNotifications.Publish(category, CampaignCalendar.FormatDate(_clock.SimulationDays), message);

    /// <summary>Astronomical positions are public catalog data, independent of survey detail.</summary>
    public Godot.Vector2? UiGetCatalogScreenPosition(int systemId)
    {
        var system = _galaxy?.Systems.FirstOrDefault(candidate => candidate.Id == systemId);
        return system is null || UiIsSystemSpatialView ? null :
            ToScreen(system.Position, GetViewportRect().Size * 0.5f + _pan);
    }

    protected bool ShouldBlockGameplayInput()
    {
        if (!UiIsMenuOpen && !UiIsSurfaceOpen && !UiIsDeveloperToolsOpen)
            return false;
        _panning = false;
        return true;
    }

    public void UiOpenMenu() => GetNode<MainMenuLayer>("MainMenuLayer").ShowMenu();

    public void UiTogglePause() => UiSetPaused(!UiIsPaused);

    public void UiSetPaused(bool paused, bool announce = true)
    {
        _clock.SetSpeed(paused ? SimulationClock.SpeedLevel.Paused : SimulationClock.SpeedLevel.Normal);
        if (announce)
            SetStatus(paused ? "Simulation paused." : "Simulation resumed.");
        QueueRedraw();
    }

    public void UiSetSpeed(int level)
    {
        if (level < (int)SimulationClock.SpeedLevel.Normal || level > (int)SimulationClock.SpeedLevel.Maximum)
            return;

        _clock.SetSpeed((SimulationClock.SpeedLevel)level);
        SetStatus($"Simulation speed set to {_clock.Speed}.");
        QueueRedraw();
    }

    public void UiCycleResearch()
    {
        CycleResearchCandidate();
        QueueRedraw();
    }

    public void UiStartResearch()
    {
        StartSelectedResearch();
        QueueRedraw();
    }

    public void UiStartResearch(string technologyId)
    {
        var result = StartAdaptiveResearch(technologyId);
        SetStatus(result.Message, 6.0);
        SupportLogger.Log("research-order", $"technology={technologyId} accepted={result.Accepted} message={result.Message}");
        if (result.Accepted)
            PublishPlayerNotification("Research", result.Message);
        QueueRedraw();
    }

    public void UiPauseResearch(string technologyId)
    {
        if (_adaptiveResearch is null)
        {
            SetStatus("Adaptive Research is not initialized.", 5.0);
            return;
        }
        var result = AdaptiveResearchCampaignCommands.PauseDirectedResearch(
            _adaptiveResearch, _galaxy.PlayerCivilizationId, technologyId);
        SetStatus(result.Message, 6.0);
        SupportLogger.Log("research-pause", $"technology={technologyId} accepted={result.Accepted} message={result.Message}");
        if (result.Accepted)
            PublishPlayerNotification("Research", result.Message);
        QueueRedraw();
    }

    public void UiResumeResearch(string technologyId)
    {
        if (_adaptiveResearch is null)
        {
            SetStatus("Adaptive Research is not initialized.", 5.0);
            return;
        }
        var state = _adaptiveResearch.GetCivilization(_galaxy.PlayerCivilizationId);
        if (!state.ActiveProjects.TryGetValue(technologyId, out var project))
        {
            SetStatus("That research project is no longer active.", 5.0);
            return;
        }
        var result = AdaptiveResearchCampaignCommands.ResumeDirectedResearch(
            _galaxy, _adaptiveResearch, _galaxy.PlayerCivilizationId,
            technologyId, project.AssignedEffectiveLabs);
        SetStatus(result.Message, 6.0);
        SupportLogger.Log("research-resume", $"technology={technologyId} accepted={result.Accepted} message={result.Message}");
        if (result.Accepted)
            PublishPlayerNotification("Research", result.Message);
        QueueRedraw();
    }

    public void UiCycleConstruction()
    {
        CycleConstructionCandidate();
        QueueRedraw();
    }

    public void UiStartConstruction()
    {
        StartSelectedConstruction();
        QueueRedraw();
    }

    public void UiStartConstruction(string projectId)
    {
        var result = _construction.StartProject(_galaxy, _galaxy.PlayerCivilizationId, projectId);
        SetStatus(result.Message, 6.0);
        SupportLogger.Log("construction-order", $"project={projectId} accepted={result.Accepted} message={result.Message}");
        if (result.Accepted)
            PublishPlayerNotification("Construction", result.Message);
        QueueRedraw();
    }

    public void UiCycleShipDesign()
    {
        CycleShipDesignCandidate();
        QueueRedraw();
    }

    public void UiBuildShip()
    {
        StartSelectedShipBuild();
        QueueRedraw();
    }

    public void UiBuildShip(string designId)
    {
        var result = _shipbuilding.StartBuild(_galaxy, _galaxy.PlayerCivilizationId, designId);
        SetStatus(result.Message, result.Accepted ? 6.0 : 7.0);
        SupportLogger.Log("shipbuilding-order", $"design={designId} accepted={result.Accepted} message={result.Message}");
        if (result.Accepted)
            PublishPlayerNotification("Ships", result.Message);
        QueueRedraw();
    }

    public void UiOpenSelectedSystem() => EnterSelectedSystemView();

    public void UiReturnToRegion() => ReturnToStellarView(announce: true);

    public void UiZoomIn() => ZoomSpatialAt(1.35f, SpatialZoomButtonAnchor());

    public void UiZoomOut() => ZoomSpatialAt(1f / 1.35f, SpatialZoomButtonAnchor());

    public void UiSelectHomeSystem()
    {
        UiSelectSystem(PlayerCivilization.HomeSystemId, "Home system selected. Open System to inspect its known orbits.");
    }

    public void UiFocusOwnedFleet(int fleetId) => UiSelectOwnedFleet(fleetId, center: true);

    public void UiIssueMilitaryOrder(int fleetId, MilitaryOrderType orderType)
    {
        var fleet = _galaxy.Fleets.FirstOrDefault(item => item.Id == fleetId && item.IsActive &&
            item.CivilizationId == _galaxy.PlayerCivilizationId);
        if (fleet is null)
        {
            SetStatus("That fleet is no longer available.", 5);
            return;
        }
        var order = new MilitaryOrder(orderType,
            DefendSystemId: orderType == MilitaryOrderType.Defend ? fleet.CurrentSystemId : null);
        var result = _coreSimulation.IssueMilitaryOrder(_galaxy, _galaxy.PlayerCivilizationId, fleetId, order);
        SetStatus(result.Message, result.Accepted ? 5 : 7);
        SupportLogger.Log("military-order", $"fleet={fleetId} type={orderType} accepted={result.Accepted} message={result.Message}");
        QueueRedraw();
    }

    public void UiEngageHostiles(int fleetId)
    {
        var result = _coreSimulation.IssueEngageHostilesOrder(
            _galaxy, _galaxy.PlayerCivilizationId, fleetId);
        SetStatus(result.Message, result.Accepted ? 6 : 7);
        SupportLogger.Log("military-order", $"fleet={fleetId} type=EngageHostiles accepted={result.Accepted} message={result.Message}");
        QueueRedraw();
    }

    public void UiDeployMilitaryFleet(int fleetId)
    {
        var result = _coreSimulation.IssueMilitaryDeploymentOrder(
            _galaxy, _galaxy.PlayerCivilizationId, fleetId, _selectedSystemId);
        SetStatus(result.Message, result.Accepted ? 7 : 8);
        SupportLogger.Log("military-deployment", $"fleet={fleetId} system={_selectedSystemId} accepted={result.Accepted} message={result.Message}");
        QueueRedraw();
    }

    private void UiSelectSystem(int systemId, string message)
    {
        ReturnToStellarView(announce: false);
        _selectedSystemId = systemId;
        var home = _galaxy.Systems.FirstOrDefault(system => system.Id == _selectedSystemId);
        if (home is not null)
        {
            _zoom = SpatialNavigationLayout.StellarRegionScale;
            _pan = -new Godot.Vector2(home.Position.X, home.Position.Y) * _zoom;
            SynchronizeRegionalCamera();
        }
        SetStatus(message);
        QueueRedraw();
    }

    public void UiNewCampaign() => GetNode<MainMenuLayer>("MainMenuLayer").RequestNewCampaign();

    public void UiSave() => SaveIntegratedCampaign();

    public void UiExportDiagnostics()
    {
        var bundle = SupportLogger.ExportSupportBundle(File.Exists(CurrentCampaignSavePath) ? CurrentCampaignSavePath : null);
        SetStatus($"Support bundle exported: {bundle}", 8.0);
        _diagnostics.Add("support", $"Support bundle exported to {bundle}");
        QueueRedraw();
    }

    public void UiQuit() => HandleIntegratedCloseRequest();
}

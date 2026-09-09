using System.IO;
using System.Linq;
using Game.Diagnostics;
using Game.Simulation;

namespace Game.Presentation;

/// <summary>
/// Narrow player-command surface used by Godot UI controls. The UI invokes the same
/// authoritative commands as the existing prototype keyboard shortcuts rather than
/// duplicating simulation rules in presentation code.
/// </summary>
public partial class Main
{
    public bool UiIsPaused => _clock.Speed == SimulationClock.SpeedLevel.Paused;
    public string UiSpeedLabel => $"{(_clock.Speed == SimulationClock.SpeedLevel.Demo ? "Developer" : _clock.Speed.ToString())} · {_clock.EffectiveMultiplier:0.00}x";
    public string UiBuildLabel => $"Stellar Continuum {GameVersion.Current}";
    public string UiStatusMessage => _statusTimer > 0 ? _statusText : string.Empty;
    public bool UiIsMenuOpen => GetNodeOrNull<MainMenuLayer>("MainMenuLayer")?.IsBlockingGameplay == true;
    public ulong UiPointerCommandRevision { get; protected set; }

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
        var result = _research.StartResearch(_galaxy, _galaxy.PlayerCivilizationId, technologyId);
        SetStatus(result.Message, 6.0);
        SupportLogger.Log("research-order", $"technology={technologyId} accepted={result.Accepted} message={result.Message}");
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
        QueueRedraw();
    }

    public void UiSendScout() => IssueExplorationOrder(PlayerScout, _selectedSystemId, "scout");

    public void UiSendScience() => IssueExplorationOrder(PlayerScienceVessel, _selectedSystemId, "science vessel");

    public void UiOpenSelectedSystem() => EnterSelectedSystemView();

    public void UiReturnToRegion() => ReturnToStellarView(announce: true);

    public void UiZoomIn() => ZoomSpatialAt(1.35f, SpatialZoomButtonAnchor());

    public void UiZoomOut() => ZoomSpatialAt(1f / 1.35f, SpatialZoomButtonAnchor());

    public void UiSelectHomeSystem()
    {
        UiSelectSystem(PlayerCivilization.HomeSystemId, "Home system selected. Open System to inspect its known orbits.");
    }

    public void UiFocusOwnedFleet(int fleetId)
    {
        var fleet = _galaxy.Fleets.FirstOrDefault(item => item.Id == fleetId && item.IsActive &&
            item.CivilizationId == _galaxy.PlayerCivilizationId);
        var systemId = fleet?.CurrentSystemId ?? fleet?.DestinationSystemId;
        if (fleet is null || systemId is null)
        {
            SetStatus("That fleet is currently between mapped systems.", 5);
            return;
        }
        GetNode<CampaignSidebar>("CampaignSidebar").CloseDrawer();
        UiSelectSystem(systemId.Value, $"{fleet.Name} located at {_galaxy.Systems.First(system => system.Id == systemId.Value).Name}.");
    }

    private void UiSelectSystem(int systemId, string message)
    {
        ReturnToStellarView(announce: false);
        _selectedSystemId = systemId;
        var home = _galaxy.Systems.FirstOrDefault(system => system.Id == _selectedSystemId);
        if (home is not null)
        {
            _zoom = 0.55f;
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

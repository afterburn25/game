using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Game.Presentation;
using Game.Simulation;
using Game.Simulation.Models;
using Godot;

namespace Game.Tools;

/// <summary>
/// Focused Developer-labelled native evidence for civilian Hold, Resume, and Return controls.
/// Setup uses only visible Developer commands; all fleet actions use their production mouse controls.
/// </summary>
public partial class ScreenshotCapture
{
    private async Task VerifyCivilianRecoveryControlsAsync(MainMenuLayer menu, ConfirmationDialog dialog)
    {
        Require(_main.UiIsMenuOpen && !_main.UiIsDeveloperMode,
            "Civilian recovery must start from the normal campaign menu.");
        await ClickNamedButtonAsync(menu, "OpenDevelopment");
        await ClickNamedButtonAsync(menu, "NewDeveloperCampaign");
        Require(dialog.Visible, "Developer civilian-recovery fixture skipped its confirmation.");
        await ClickControlAsync(dialog.GetOkButton());
        await WaitForCampaignLoadingAsync();
        Require(_main.UiIsDeveloperMode && !_main.UiDeveloperToolsUsed,
            "Civilian recovery did not enter a fresh, labelled Developer campaign.");
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");

        await RunVisibleDeveloperCommandAsync(menu, "DeveloperCommand_unlock_technology");
        await RunVisibleDeveloperCommandAsync(menu, "DeveloperCommand_reveal_galaxy");
        await RunVisibleDeveloperCommandAsync(menu, "DeveloperCommand_grant_resources");
        var playerPath = ProjectSettings.GlobalizePath("user://saves/autosave.json");
        var playerHash = HashFile(playerPath);

        var scoutId = await BuildVisibleCivilianShipAsync(menu, "warp_scout", FleetRole.Scout);
        var colonyId = await BuildVisibleCivilianShipAsync(menu, "colony_ship", FleetRole.Colony);
        Require(_main.UiDeveloperToolsUsed && _main.UiIsPaused,
            "Civilian recovery setup lost Developer provenance or resumed simulation.");

        await VerifyVisibleHoldResumeAndReturnAsync(scoutId);
        await VerifyVisiblePaidReturnConfirmationAsync(menu, playerPath, playerHash, colonyId);
        await SaveViewportAsync("civilian-recovery-03-returning.png");
        WriteCivilianRecoveryEvidence(scoutId, colonyId);
    }

    private async Task RunVisibleDeveloperCommandAsync(MainMenuLayer menu, string command)
    {
        await OpenCampaignMenuAsync();
        await ClickNamedButtonAsync(menu, "OpenDevelopment");
        await ClickNamedButtonAsync(menu, "DeveloperTools");
        await ClickNamedButtonAsync(_main.GetNode("DeveloperToolsLayer"), command);
        await ClickNamedButtonAsync(_main, "DeveloperToolsClose");
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");
    }

    private async Task<int> BuildVisibleCivilianShipAsync(MainMenuLayer menu, string designId, FleetRole role)
    {
        var before = _main.UiOwnedFleets.Select(fleet => fleet.FleetId).ToHashSet();
        await OpenSectionAsync("ships");
        await ClickNamedButtonAsync(ActivePanel(), "Choose" + designId);
        await WaitForRefreshAsync();
        await RunVisibleDeveloperCommandAsync(menu, "DeveloperCommand_finish_orders");
        var built = _main.UiOwnedFleets.SingleOrDefault(fleet => fleet.Role == role && !before.Contains(fleet.FleetId));
        Require(built is not null, $"Visible Developer construction did not commission {designId}.");
        return built!.FleetId;
    }

    private async Task VerifyVisibleHoldResumeAndReturnAsync(int scoutId)
    {
        if (_sidebar.IsDrawerOpen) await CloseDrawerAsync();
        await ClickButtonAsync(_dock, "Home");
        await SelectFleetByMarkerAsync(scoutId);
        var opening = _main.UiOwnedFleets.Single(fleet => fleet.FleetId == scoutId);
        var homeId = opening.CurrentSystemId
            ?? throw new InvalidOperationException("Developer scout did not begin at its owned base.");
        var destination = FindDirectRecoveryTarget(opening);
        await ClickPositionAsync(destination.Point, MouseButton.Right);
        await WaitForRefreshAsync();
        var ordered = _main.UiOwnedFleets.Single(fleet => fleet.FleetId == scoutId);
        Require(ordered.DestinationSystemId == destination.SystemId && ordered.RemainingRouteLegs == 1 &&
                ordered.RemainingRouteDistanceLightYears > 0,
            "Visible right-click did not accept the direct outbound scout course.");

        var departureFuel = ordered.FuelRemainingLightYears;
        var departureDistance = ordered.RemainingRouteDistanceLightYears;
        await SelectNormalPlayerSpeedAsync();
        await ClickNamedButtonAsync(_main, "SimulationPause");
        Require(!_main.UiIsPaused && _main.UiCurrentSpeed == SimulationClock.SpeedLevel.Normal,
            "Visible Resume did not begin the outbound lane at controlled 1x speed.");
        await WaitForCivilianConditionAsync(() => _main.UiOwnedFleets.Any(fleet => fleet.FleetId == scoutId &&
            fleet.CurrentSystemId is null && fleet.DestinationSystemId == destination.SystemId &&
            fleet.RemainingRouteDistanceLightYears > 0 && fleet.RemainingRouteDistanceLightYears < departureDistance &&
            fleet.FuelRemainingLightYears < departureFuel),
            "Outbound scout did not enter its lane with positive distance and fuel consumption");
        await ClickNamedButtonAsync(_main, "SimulationPause");
        await WaitForRefreshAsync();
        var inLane = _main.UiOwnedFleets.Single(fleet => fleet.FleetId == scoutId);
        Require(_main.UiIsPaused && inLane.CurrentSystemId is null &&
                inLane.DestinationSystemId == destination.SystemId && inLane.RemainingRouteDistanceLightYears > 0 &&
                inLane.RemainingRouteDistanceLightYears < departureDistance && inLane.FuelRemainingLightYears < departureFuel,
            "Visible Pause did not preserve the scout's positive unfinished in-lane movement.");

        await ClickNamedButtonAsync(_dock, "CivilianHoldResume");
        await WaitForRefreshAsync();
        var heldInLane = _main.UiOwnedFleets.Single(fleet => fleet.FleetId == scoutId);
        Require(heldInLane.HoldRequested && heldInLane.CurrentSystemId is null &&
                heldInLane.DestinationSystemId == inLane.DestinationSystemId &&
                heldInLane.RemainingRouteDistanceLightYears == inLane.RemainingRouteDistanceLightYears &&
                heldInLane.FuelRemainingLightYears == inLane.FuelRemainingLightYears,
            "Hold did not preserve the paused scout's current lane and exact remaining route.");
        await SelectDeveloperSpeedAsync();
        await WaitForCivilianConditionAsync(() => _main.UiOwnedFleets.Any(fleet => fleet.FleetId == scoutId &&
            fleet.CurrentSystemId == destination.SystemId && fleet.HoldRequested && fleet.DestinationSystemId is null),
            "Held scout did not finish exactly one lane and stop");
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");
        var heldAtDestination = _main.UiOwnedFleets.Single(fleet => fleet.FleetId == scoutId);
        Require(heldAtDestination.FuelRemainingLightYears < departureFuel &&
                Math.Abs((departureFuel - heldAtDestination.FuelRemainingLightYears) - departureDistance) < .05,
            "Held scout did not consume the exact positive fuel for its completed lane.");
        Check(true, "developer-civilian-hold-finishes-one-lane-with-exact-fuel");
        await SaveViewportAsync("civilian-recovery-01-held.png");

        await ClickNamedButtonAsync(_dock, "CivilianHoldResume");
        await WaitForRefreshAsync();
        Require(!_main.UiOwnedFleets.Single(fleet => fleet.FleetId == scoutId).HoldRequested,
            "Resume did not release the exact selected scout.");
        Check(true, "developer-civilian-resume-uses-visible-control");
        await ClickNamedButtonAsync(_dock, "CivilianReturnToBase");
        await WaitForRefreshAsync();
        var returning = _main.UiOwnedFleets.Single(fleet => fleet.FleetId == scoutId);
        Require(returning.ReturnToBaseRequested && returning.DestinationSystemId == homeId &&
                returning.RemainingRouteDistanceLightYears > 0,
            "Return to base did not accept the selected scout's canonical route.");
        var returnFuel = returning.FuelRemainingLightYears;
        await SelectDeveloperSpeedAsync();
        await WaitForCivilianConditionAsync(() => _main.UiOwnedFleets.Any(fleet => fleet.FleetId == scoutId &&
            fleet.CurrentSystemId is null && fleet.FuelRemainingLightYears < returnFuel),
            "Returning scout did not move gradually or consume fuel");
        var moving = _main.UiOwnedFleets.Single(fleet => fleet.FleetId == scoutId);
        Require(moving.ReturnToBaseRequested && moving.FuelRemainingLightYears < returnFuel,
            "Returning scout lost its return intent before consuming positive fuel.");
        await WaitForCivilianConditionAsync(() => _main.UiOwnedFleets.Any(fleet => fleet.FleetId == scoutId &&
            fleet.CurrentSystemId == homeId && fleet.DestinationSystemId is null && !fleet.ReturnToBaseRequested),
            "Returning scout did not physically arrive at its original base");
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");
        var home = _main.UiOwnedFleets.Single(fleet => fleet.FleetId == scoutId);
        Require(Math.Abs(home.FuelRemainingLightYears - home.FuelCapacityLightYears) < .001,
            "Returning scout did not receive the canonical full-colony refuel.");
        Check(true, "developer-civilian-return-travels-consumes-fuel-and-refuels");
    }

    private async Task VerifyVisiblePaidReturnConfirmationAsync(
        MainMenuLayer menu, string playerPath, string playerHash, int colonyId)
    {
        if (_sidebar.IsDrawerOpen) await CloseDrawerAsync();
        await ClickButtonAsync(_dock, "Home");
        var opportunity = FindFundedSettlementOpportunity();
        Require(opportunity is { CanOrder: true, FleetId: not null, SystemId: not null, PlanetaryBodyId: not null } &&
                opportunity.FleetId == colonyId,
            "Revealed Developer world exposed no funded canonical colony opportunity.");
        await OpenSectionAsync("colonies");
        var previous = RequireButton(ActivePanel(), "← Site");
        while (!previous.Disabled)
        {
            await ClickControlAsync(previous);
            previous = RequireButton(ActivePanel(), "← Site");
        }
        for (var site = 0; site < opportunity.SiteIndex; site++)
            await ClickButtonAsync(ActivePanel(), "Site →");
        await ClickControlAsync(RequireButton(ActivePanel(), "Select ship on map"));
        var destinationPoint = _main.UiGetCatalogScreenPosition(opportunity.SystemId!.Value)
            ?? throw new InvalidOperationException("Developer colony opportunity has no visible public map point.");
        await ClickPositionAsync(destinationPoint, MouseButton.Right);
        await SelectDeveloperSpeedAsync();
        await WaitForCivilianConditionAsync(() => _main.UiOwnedFleets.Any(fleet => fleet.FleetId == colonyId &&
            fleet.CurrentSystemId == opportunity.SystemId && fleet.DestinationSystemId is null),
            "Colony ship did not finish its visible canonical transit");
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");
        await ClickPositionAsync(destinationPoint, MouseButton.Left);
        await ClickButtonAsync(_dock, "Open System");
        await WaitForCameraAsync();
        await ClickNamedButtonAsync(_main, "SystemFleet" + colonyId);
        var bodyPoint = _main.UiGetBodyScreenPosition(opportunity.PlanetaryBodyId!.Value)
            ?? throw new InvalidOperationException("Developer settlement target has no visible body marker.");
        await ClickPositionAsync(bodyPoint, MouseButton.Right);
        await WaitForRefreshAsync();
        var authorized = _main.UiOwnedFleets.Single(fleet => fleet.FleetId == colonyId);
        Require(authorized.DestinationPlanetaryBodyId == opportunity.PlanetaryBodyId &&
                authorized.EmbarkedPopulationMillions > 0,
            "Visible body order did not create a paid populated colony authorization.");
        await SelectDeveloperSpeedAsync();
        await WaitForCivilianConditionAsync(() => _main.UiOwnedFleets.Any(fleet => fleet.FleetId == colonyId &&
            fleet.SettlementBodyId == opportunity.PlanetaryBodyId) &&
            _main.UiSelectedCivilianReturnPreview.Contains("days", StringComparison.Ordinal) &&
            !_main.UiSelectedCivilianReturnPreview.Contains("progress: 0 days", StringComparison.Ordinal),
            "Paid settlement did not begin and expose live progress");
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");
        var progressPreview = _main.UiSelectedCivilianReturnPreview;
        var paidTreasury = _main.UiDashboard.Credits;
        var paidFleet = _main.UiOwnedFleets.Single(fleet => fleet.FleetId == colonyId);

        await ClickNamedButtonAsync(_dock, "CivilianReturnToBase");
        await WaitForRefreshAsync();
        Require(_main.UiSelectedCivilianReturnNeedsConfirmation &&
                Descendants(_dock).OfType<Button>().Single(button => button.Name == "CivilianReturnToBase").Text ==
                    "Confirm return (no refund)" &&
                _main.UiOwnedFleets.Single(fleet => fleet.FleetId == colonyId) == paidFleet &&
                _main.UiSelectedCivilianReturnPreview == progressPreview &&
                progressPreview.Contains("days", StringComparison.Ordinal) &&
                Math.Abs(_main.UiDashboard.Credits - paidTreasury) < .001,
            "First paid return click mutated the paused ship, preview, or treasury instead of only arming confirmation.");
        await SaveViewportAsync("civilian-recovery-02-confirmation.png");

        await ClickNamedButtonAsync(_dock, "CloseShipInspector");
        await ClickNamedButtonAsync(_main, "SystemFleet" + colonyId);
        await WaitForRefreshAsync();
        Require(!_main.UiSelectedCivilianReturnNeedsConfirmation &&
                Descendants(_dock).OfType<Button>().Single(button => button.Name == "CivilianReturnToBase").Text == "Return to base",
            "Deselecting and reselecting a fleet retained a stale return confirmation.");
        Check(true, "developer-civilian-return-confirmation-resets-on-deselection");

        await ClickNamedButtonAsync(_dock, "CivilianReturnToBase");
        await WaitForRefreshAsync();
        Require(_main.UiSelectedCivilianReturnNeedsConfirmation, "Paid return confirmation could not be recreated before reload.");
        await OpenSectionAsync("menu");
        await ClickButtonAsync(ActivePanel(), "Save");
        _ = await ReloadDeveloperThroughPlayerAsync(playerPath, playerHash);
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");
        if (_sidebar.IsDrawerOpen) await CloseDrawerAsync();
        await ClickNamedButtonAsync(_dock, "OverviewFleet" + colonyId);
        await WaitForRefreshAsync();
        Require(!_main.UiSelectedCivilianReturnNeedsConfirmation &&
                _main.UiOwnedFleets.Single(fleet => fleet.FleetId == colonyId).SettlementBodyId == opportunity.PlanetaryBodyId &&
                _main.UiSelectedCivilianReturnPreview == progressPreview &&
                Math.Abs(_main.UiDashboard.Credits - paidTreasury) < .001,
            "Campaign replacement retained confirmation or lost the saved paid settlement progress.");
        Check(true, "developer-civilian-return-confirmation-resets-on-campaign-reload");

        await ClickNamedButtonAsync(_dock, "CivilianReturnToBase");
        await WaitForRefreshAsync();
        await ClickNamedButtonAsync(_dock, "CivilianReturnToBase");
        await WaitForRefreshAsync();
        var returning = _main.UiOwnedFleets.Single(fleet => fleet.FleetId == colonyId);
        Require(returning.ReturnToBaseRequested && !returning.HoldRequested &&
                returning.SettlementBodyId is null && returning.DestinationPlanetaryBodyId is null &&
                returning.EmbarkedPopulationMillions == authorized.EmbarkedPopulationMillions &&
                Math.Abs(_main.UiDashboard.Credits - paidTreasury) < .001,
            "Confirmed no-refund return did not preserve passengers and start the authoritative route.");
        Check(true, "developer-paid-colony-return-confirms-without-refund-or-passenger-loss");
    }

    private (int SystemId, Vector2 Point) FindDirectRecoveryTarget(UiOwnedFleetSnapshot fleet)
    {
        var currentId = fleet.CurrentSystemId ?? throw new InvalidOperationException("Selected fleet is not at a system.");
        var currentPoint = _main.UiGetCatalogScreenPosition(currentId)
            ?? throw new InvalidOperationException("Current system has no public map point.");
        var candidate = _main.UiSpatialCatalog.Where(entry => entry.SystemId != currentId)
            .Select(entry => new { entry.SystemId,
                Point = _main.UiGetCatalogScreenPosition(entry.SystemId),
                Reach = _main.UiGetFleetRouteAssessment(fleet.FleetId, entry.SystemId) })
            .Where(entry => entry.Point is Vector2 point && new Rect2(110, 120, 740, 500).HasPoint(point) &&
                entry.Reach.ReachSupported && entry.Reach.DistanceLy > 1 &&
                entry.Reach.DistanceLy < fleet.FuelRemainingLightYears * .4 &&
                Math.Abs(entry.Reach.DistanceLy - point.DistanceTo(currentPoint) / _main.UiMapZoom) < .1)
            .OrderBy(entry => entry.Reach.DistanceLy).FirstOrDefault()
            ?? throw new InvalidOperationException("No visible direct public route preserves return fuel.");
        return (candidate.SystemId, candidate.Point!.Value);
    }

    private async Task SelectDeveloperSpeedAsync()
    {
        if (!_main.UiIsPaused && _main.UiCurrentSpeed == SimulationClock.SpeedLevel.Demo) return;
        var selector = Descendants(_main.GetNode("PlayerControls")).OfType<OptionButton>()
            .Single(control => control.Name == "SimulationSpeed");
        await ClickPositionAsync(ScreenRect(selector).GetCenter(), MouseButton.Left);
        await WaitFramesAsync(2);
        var popup = selector.GetPopup();
        Require(popup.Visible, "Developer speed selector did not open.");
        for (var step = 0; popup.GetFocusedItem() != 4 && step <= selector.ItemCount; step++)
            await PressKeyAsync(Key.Down);
        Require(popup.GetFocusedItem() == 4, "Developer speed item did not receive popup focus.");
        await PressKeyAsync(Key.Enter);
        Require(selector.Selected == 4 && selector.GetItemId(selector.Selected) == 24,
            $"Visible speed selector did not select the 24x Developer item (selected {selector.Selected}).");
        if (_main.UiIsPaused)
        {
            await ClickNamedButtonAsync(_main, "SimulationPause");
            await WaitForRefreshAsync();
        }
        Require(_main.UiCurrentSpeed == SimulationClock.SpeedLevel.Demo && !_main.UiIsPaused,
            "Visible speed selector did not resume at 24x Developer speed.");
        await WaitForRefreshAsync();
    }

    private async Task WaitForCivilianConditionAsync(Func<bool> condition, string failure)
    {
        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < TimeSpan.FromSeconds(45))
        {
            if (condition()) return;
            await WaitFramesAsync(1);
        }
        var selected = _main.UiSelectedFleetId is int selectedId
            ? _main.UiOwnedFleets.SingleOrDefault(fleet => fleet.FleetId == selectedId)
            : null;
        throw new InvalidOperationException(failure +
            $". status='{_main.UiStatusMessage}', date='{_main.UiDashboard.Date}', day={_main.UiSimulationDays:0.###}, " +
            $"speed={_main.UiCurrentSpeed}, paused={_main.UiIsPaused}, selected=" +
            (selected is null ? "none" :
                $"{selected.FleetId}:current={selected.CurrentSystemId?.ToString() ?? "lane"},destination={selected.DestinationSystemId?.ToString() ?? "none"},remaining={selected.RemainingRouteDistanceLightYears:0.###},fuel={selected.FuelRemainingLightYears:0.###},hold={selected.HoldRequested}"));
    }

    private void WriteCivilianRecoveryEvidence(int scoutId, int colonyId)
    {
        var evidence = new
        {
            schema_version = 1,
            git_sha = System.Environment.GetEnvironmentVariable("STELLAR_CAPTURE_SHA") ?? "unknown",
            scope = "focused Developer-labelled civilian Hold, Resume, and Return UI",
            input_mode = "Input.ParseInputEvent",
            developer_tools_used = _main.UiDeveloperToolsUsed,
            mouse_actions = _mouseActions,
            scout_id = scoutId,
            colony_id = colonyId,
            checks = _checks.ToArray(),
            captures = _captureRecords.ToArray(),
        };
        File.WriteAllText(Path.Combine(_outputDirectory, "civilian-recovery-ui.json"),
            JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
    }
}

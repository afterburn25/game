using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Game.Presentation;
using Game.Simulation.Models;

namespace Game.Tools;

public partial class ScreenshotCapture
{
    private async Task VerifyShipMouseOrdersAsync()
    {
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");
        if (!_main.UiOwnedFleets.Any(f => f.Role == FleetRole.Scout))
        {
            await OpenCampaignMenuAsync();
            await ClickNamedButtonAsync(_main.GetNode("MainMenuLayer"), "OpenDevelopment");
            await ClickNamedButtonAsync(_main.GetNode("MainMenuLayer"), "DeveloperTools");
            await ClickNamedButtonAsync(_main, "DeveloperCommand_finish_orders");
            await ClickNamedButtonAsync(_main, "DeveloperToolsClose");
        }
        if (_sidebar.IsDrawerOpen) await CloseDrawerAsync();
        await ClickButtonAsync(_dock, "Home"); await WaitForCameraAsync();
        var ship = _main.UiOwnedFleets.First(f => f.Role == FleetRole.Scout);
        var point = _main.UiGetFleetScreenPosition(ship.FleetId)!.Value;
        await ClickPositionAsync(point, MouseButton.Left); await WaitForRefreshAsync();
        Require(_main.UiSelectedFleetId == ship.FleetId, "Clicking a ship icon did not select the exact ship.");
        Require(Descendants(_main).OfType<Label>().Single(l => l.Name == "SelectedShipName").Text == ship.Name,
            "Selected ship stats did not identify the clicked vessel.");
        var home = _main.UiSelectedSystemId;
        var homePoint = StarPoint(home);
        var target = _main.UiSpatialCatalog.Where(s => s.SystemId != home)
            .Select(s => new { s.SystemId, Point = StarPoint(s.SystemId) })
            .Where(s => new Rect2(100, 150, 780, 470).HasPoint(s.Point) && s.Point.DistanceTo(homePoint) > 70 &&
                s.Point.DistanceTo(homePoint) < ship.MaximumLegRangeLightYears * .85)
            .OrderBy(s => s.Point.DistanceTo(homePoint)).First();
        await ClickPositionAsync(target.Point, MouseButton.Right); await WaitForRefreshAsync();
        var ordered = _main.UiOwnedFleets.Single(f => f.FleetId == ship.FleetId);
        Require(_main.UiSelectedFleetId == ship.FleetId && ordered.RemainingRouteDistanceLightYears > 0 &&
            _main.UiGetFleetScreenPosition(ship.FleetId)!.Value.DistanceTo(point) < .1f,
            "Right-click failed to order the selected vessel or teleported it while paused.");
        await SaveViewportAsync("28-selected-ship-route.png");
        await ClickNamedButtonAsync(_main, "SimulationPause"); await WaitFramesAsync(20);
        await ClickNamedButtonAsync(_main, "SimulationPause"); await WaitForRefreshAsync();
        var moved = _main.UiOwnedFleets.Single(f => f.FleetId == ship.FleetId);
        Require(moved.RemainingRouteDistanceLightYears < ordered.RemainingRouteDistanceLightYears && moved.RemainingRouteDistanceLightYears > 0 &&
            _main.UiGetFleetScreenPosition(ship.FleetId)!.Value.DistanceTo(point) > .1f && moved.FuelRemainingLightYears < ordered.FuelRemainingLightYears,
            "Travel did not move gradually, consume fuel, and leave an unfinished route.");
        Check(true, "ship-icon-selection-right-click-and-timed-travel");
        await ClickNamedButtonAsync(_main, "CloseShipInspector");
        await ClickButtonAsync(_dock, "Home"); await ClickButtonAsync(_dock, "Open System"); await WaitForCameraAsync();
        await ClickPositionAsync(_main.UiGetInfrastructureScreenPosition("orbital_shipyard")!.Value, MouseButton.Left);
        await WaitForRefreshAsync();
        Require(_main.UiSelectedOrbitalConstruction is { State: "Operational" } &&
            Descendants(_main).OfType<Game.Presentation.Spatial.OrbitalStructureView>().Count() >= 3,
            "Completed orbital infrastructure did not produce selectable 3D models.");
        await SaveViewportAsync("29-orbital-shipyard.png");
        await ClickNamedButtonAsync(_main, "CloseOrbitalInspector");
        await ClickButtonAsync(_dock, "Back to Region"); await WaitForCameraAsync();
    }
}

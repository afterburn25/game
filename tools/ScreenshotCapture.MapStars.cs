using System.Linq;
using System.Threading.Tasks;
using Game.Presentation;
using Game.Presentation.Spatial;
using Godot;

namespace Game.Tools;

public partial class ScreenshotCapture
{
    // Bounded visual receipt: it uses the same real controls as immersive evidence, but
    // deliberately stops before surface descent so the runner returns a numeric exit code.
    private async Task VerifyMapStarVisualsAsync()
    {
        var menu = _main.GetNode<MainMenuLayer>("MainMenuLayer");
        await OpenCampaignMenuAsync();
        await ClickNamedButtonAsync(menu, "NewPlayerCampaign");
        await ClickNamedButtonAsync(menu, "SandboxCampaignOption");
        await ClickNamedButtonAsync(menu, "StartConfiguredSandbox");
        await PressKeyAsync(Key.Escape);
        await ClickNamedButtonAsync(menu, "SandboxSetupBack");
        await ClickNamedButtonAsync(menu, "NewGameBack");
        await ClickNamedButtonAsync(menu, "ResumeCampaign");
        await ClickNamedButtonAsync(_main, "SimulationPlaybackButton");
        await ClickButtonAsync(_dock, "Home");
        await WaitForCameraAsync();
        await ClickControlAsync(Descendants(_main).OfType<Button>().Single(button => button.Name == "SpatialOverview"));
        await WaitForCameraAsync();
        Require(_main.UiOverviewBlend > .95f, "map-star capture did not reach the galaxy overview");
        await SaveViewportAsync("map-stars-01-galaxy.png", 0, 0);
        await ClickButtonAsync(_dock, "Home");
        await WaitForCameraAsync();
        await ClickButtonAsync(_dock, "Open System");
        await WaitForCameraAsync();
        Require(_main.UiIsSystemSpatialView && _main.UiSystemMeshBodyCount > 8,
            "map-star capture did not enter the restored 2D orbital system");
        await SaveViewportAsync("map-stars-02-system.png", 0, 0);
        var canvas = _main.GetNode<SystemSpatialCanvas>("SystemSpatialCanvas");
        await ClickPositionAsync(canvas.GetStarScreenPosition()!.Value, MouseButton.Left, doubleClick: true);
        await WaitForCameraAsync();
        Require(canvas.IsStarFocused, "double-clicking Sol did not enter native stellar focus");
        var defaultDistance = canvas.Scene.TargetDistance;
        await SaveViewportAsync("map-stars-02a-sol-close.png", 0, 0);
        await WheelAsync(true, new Vector2(620, 390));
        Require(canvas.IsStarFocused && canvas.Scene.TargetDistance < defaultDistance,
            "stellar focus did not allow a closer radius-bounded view");
        await WaitFramesAsync(180);
        await SaveViewportAsync("map-stars-02b-sol-surface-motion.png", 0, 0);
        await WheelAsync(false, new Vector2(620, 390));
        await WheelAsync(false, new Vector2(620, 390));
        Require(canvas.IsStarFocused && canvas.Scene.TargetDistance > defaultDistance,
            "the first outward stellar zoom step exited instead of retaining close context");
        await ClickNamedButtonAsync(_main, "SpatialBack");
        await WaitForCameraAsync();
        Require(!canvas.IsDetailedFocus && _main.UiIsSystemSpatialView,
            "Back from stellar focus did not restore Sol's orbital system");
        await ClickPositionAsync(BodyPoint(3), MouseButton.Left, doubleClick: true);
        await WaitForCameraAsync();
        Require(_main.UiFocusedPlanetBodyId == 3, "map-star capture did not enter Earth orbital focus");
        await SaveViewportAsync("map-stars-03-orbital-close.png", 0, 0);
        await ClickButtonAsync(_dock, "Home");
        await ClickButtonAsync(_dock, "Open System");
        await WaitForCameraAsync();
        var lane = canvas.GetLocalLanes!.Invoke().First();
        var gate = canvas.GetLaneScreenPosition(lane.DestinationSystemId);
        Require(gate.HasValue, "local lane did not expose a narrow clickable gate target");
        await ClickPositionAsync(gate.GetValueOrDefault(), MouseButton.Left);
        Require(_main.UiSelectedSystemId == lane.DestinationSystemId,
            "clicking a local lane did not select its actual connected catalog system");
        await SaveViewportAsync("map-stars-04-lane-click.png", 0, 0);
    }
}

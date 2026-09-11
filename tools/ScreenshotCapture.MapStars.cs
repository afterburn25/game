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
        await ClickNamedButtonAsync(menu, "OpenDevelopment");
        await ClickNamedButtonAsync(menu, "NewDeveloperCampaign");
        await ClickControlAsync(Descendants(menu).OfType<ConfirmationDialog>().Single().GetOkButton());
        await WaitForCampaignLoadingAsync();
        Require(_main.UiIsDeveloperMode, "map-star gate fixture did not start its disposable Developer campaign");
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
        var initialLanes = canvas.GetLocalLanes!.Invoke();
        Require(canvas.SystemBoundaryScreenRadius > 0f && initialLanes.All(lane =>
                canvas.GetLaneMarkerBoundaryClearance(lane.DestinationSystemId) is > 1f),
            "outer-system delimiter did not clear every visible triangular lane gate");
        var visibleEvidenceArea = new Rect2(120f, 175f, GetViewport().GetVisibleRect().Size.X - 450f,
            GetViewport().GetVisibleRect().Size.Y - 235f);
        var unknown = initialLanes.Where(lane => !lane.IsKnown &&
                canvas.GetLaneMarkerBounds(lane.DestinationSystemId) is { } bounds &&
                Encloses(visibleEvidenceArea, bounds))
            .OrderByDescending(lane => initialLanes.Where(other => other.DestinationSystemId != lane.DestinationSystemId)
                .Min(other => canvas.GetLaneScreenPosition(other.DestinationSystemId)!.Value.DistanceTo(
                    canvas.GetLaneScreenPosition(lane.DestinationSystemId)!.Value)))
            .First();
        var unknownGate = canvas.GetLaneScreenPosition(unknown.DestinationSystemId);
        Require(unknownGate.HasValue, "unknown local lane did not expose its visible marker body");
        var unknownPoint = unknownGate.GetValueOrDefault();
        var beforeSystem = _main.UiSelectedSystemId;
        var beforeSurvey = _main.UiSpatialCatalog.Single(item => item.SystemId == unknown.DestinationSystemId).SurveyLevel;
        var beforeCamera = (canvas.Camera.Scale, canvas.Camera.OriginX, canvas.Camera.OriginY,
            canvas.Camera.TargetScale, canvas.Camera.TargetOriginX, canvas.Camera.TargetOriginY);
        var unknownNative = GetViewport().GetFinalTransform() * unknownPoint;
        Input.ParseInputEvent(new InputEventMouseMotion { Position = unknownNative, GlobalPosition = unknownNative });
        Input.FlushBufferedEvents(); await WaitFramesAsync(2);
        Require(canvas.HoveredLaneDestinationId == unknown.DestinationSystemId,
            "unknown lane marker did not accept real hover input");
        Require(canvas.TooltipText == "????", "unknown lane hover leaked its undiscovered catalog name");
        RequireLaneBodyIsOrange(canvas, unknown.DestinationSystemId, "unknown");
        GD.Print($"STELLAR_LANE_EVIDENCE unknown={unknown.DestinationSystemId} point={unknownPoint} bounds={canvas.GetLaneMarkerBounds(unknown.DestinationSystemId)}");
        await EnsureMapStarCaptureWindowAsync();
        await SaveViewportAsync("map-stars-04-unknown-hover.png", 0, 0);
        await ClickPositionAsync(unknownPoint, MouseButton.Left);
        var afterCamera = (canvas.Camera.Scale, canvas.Camera.OriginX, canvas.Camera.OriginY,
            canvas.Camera.TargetScale, canvas.Camera.TargetOriginX, canvas.Camera.TargetOriginY);
        Require(_main.UiSelectedSystemId == beforeSystem && beforeCamera == afterCamera &&
            _main.UiSpatialCatalog.Single(item => item.SystemId == unknown.DestinationSystemId).SurveyLevel == beforeSurvey &&
            _main.UiStatusMessage == "Long-range telemetry is incomplete. Dispatch a scout vessel to chart this system before approach.",
            "unknown gate changed selection, camera, survey state, or exact reconnaissance guidance");
        await SaveViewportAsync("map-stars-05-unknown-advisory.png", 0, 0);

        var reveal = _main.UiRunDeveloperCommand("reveal_galaxy");
        Require(reveal.Accepted, "Developer reconnaissance fixture failed to establish actual neighbor knowledge");
        await WaitForRefreshAsync();
        var known = canvas.GetLocalLanes!.Invoke().Single(lane => lane.DestinationSystemId == unknown.DestinationSystemId);
        Require(known.IsKnown && known.Label != "????", "reconnaissance did not replace the unknown gate with its catalog name");
        var knownGate = canvas.GetLaneScreenPosition(known.DestinationSystemId);
        Require(knownGate.HasValue, "known local lane did not expose its visible marker body");
        var knownPoint = knownGate.GetValueOrDefault();
        var awayNative = GetViewport().GetFinalTransform() * new Vector2(180f, 650f);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = awayNative, GlobalPosition = awayNative });
        Input.FlushBufferedEvents(); await WaitFramesAsync(2);
        var knownNative = GetViewport().GetFinalTransform() * knownPoint;
        Input.ParseInputEvent(new InputEventMouseMotion { Position = knownNative, GlobalPosition = knownNative });
        Input.FlushBufferedEvents(); await WaitFramesAsync(2);
        Require(canvas.HoveredLaneDestinationId == known.DestinationSystemId,
            "known lane marker did not accept real hover input");
        Require(canvas.TooltipText == known.Label, "known lane hover did not expose its full catalog name");
        RequireLaneBodyIsOrange(canvas, known.DestinationSystemId, "known");
        GD.Print($"STELLAR_LANE_EVIDENCE known={known.DestinationSystemId} label={known.Label} point={knownPoint} bounds={canvas.GetLaneMarkerBounds(known.DestinationSystemId)}");
        await EnsureMapStarCaptureWindowAsync();
        await SaveViewportAsync("map-stars-06-known-hover.png", 0, 0);
        await ClickPositionAsync(knownPoint, MouseButton.Left);
        Require(_main.UiSelectedSystemId == known.DestinationSystemId && _main.UiIsSystemSpatialView &&
            canvas.SystemName == known.Label,
            "known adjacent gate did not open its actual connected orbital system");
        await SaveViewportAsync("map-stars-07-known-system.png", 0, 0);
    }

    private void RequireLaneBodyIsOrange(SystemSpatialCanvas canvas, int destinationSystemId, string state)
    {
        var sample = canvas.GetLaneBodyColorSamplePosition(destinationSystemId);
        Require(sample.HasValue, $"{state} lane did not expose a visible body sample");
        var samplePoint = sample.GetValueOrDefault();
        using var image = GetViewport().GetTexture().GetImage();
        var color = image.GetPixel((int)samplePoint.X, (int)samplePoint.Y);
        Require(color.R > .8f && color.G is > .45f and < .75f && color.B < .35f,
            $"{state} lane hover body was not orange at {sample}: {color}");
    }

    private async Task EnsureMapStarCaptureWindowAsync()
    {
        if (GetWindow().Mode == Window.ModeEnum.Minimized)
            GetWindow().Mode = Window.ModeEnum.Windowed;
        await WaitFramesAsync(5);
        Require(GetWindow().Mode == Window.ModeEnum.Windowed, "map-star evidence window was minimized before capture");
    }
}

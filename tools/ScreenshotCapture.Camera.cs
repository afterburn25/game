using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Game.Presentation.Spatial;
using Game.Simulation.Knowledge;

namespace Game.Tools;

public partial class ScreenshotCapture
{
    private bool _sawSmoothCameraTransition;

    private sealed record CameraObservation(string Level, float Zoom, Vector2 Pan,
        float TargetZoom, Vector2 TargetPan, int? FocusedBodyId, bool IsTransitioning);

    private CameraObservation ObserveCamera()
    {
        var camera = _main.UiCameraSnapshot;
        Require(float.IsFinite(camera.Zoom) && camera.Zoom > 0 &&
            float.IsFinite(camera.Pan.X) && float.IsFinite(camera.Pan.Y) &&
            float.IsFinite(camera.TargetZoom) && camera.TargetZoom > 0 &&
            float.IsFinite(camera.TargetPan.X) && float.IsFinite(camera.TargetPan.Y),
            "The displayed camera contains a non-finite transform.");
        return new(camera.Level, camera.Zoom, camera.Pan, camera.TargetZoom, camera.TargetPan,
            camera.FocusedBodyId, camera.IsTransitioning);
    }

    private async Task WaitForCameraAsync()
    {
        var started = Time.GetTicksMsec();
        var previous = ObserveCamera();
        while (true)
        {
            await WaitFramesAsync(1);
            var current = ObserveCamera();
            if (previous.Level == current.Level && previous.IsTransitioning && current.IsTransitioning &&
                (Math.Abs(previous.Zoom - current.Zoom) > 0.00001f || previous.Pan.DistanceTo(current.Pan) > 0.05f))
                _sawSmoothCameraTransition = true;
            if (!current.IsTransitioning)
            {
                Require(Math.Abs(current.Zoom - current.TargetZoom) < 0.0001f &&
                    current.Pan.DistanceTo(current.TargetPan) < 0.1f,
                    "Camera reported settled before reaching its requested transform.");
                return;
            }
            Require(Time.GetTicksMsec() - started < 4000, $"Camera did not settle within four seconds: {current}.");
            previous = current;
        }
    }

    private static bool SameCamera(CameraObservation expected, CameraObservation actual) =>
        expected.Level == actual.Level && expected.FocusedBodyId == actual.FocusedBodyId &&
        Math.Abs(expected.Zoom - actual.Zoom) <= Math.Max(0.0001f, expected.Zoom * 0.001f) &&
        expected.Pan.DistanceTo(actual.Pan) < 1.0f && !actual.IsTransitioning;

    private static bool SameZoomRoute(CameraObservation before, CameraObservation after,
        Vector2 anchorBefore, Vector2 anchorAfter, Vector2 otherBefore, Vector2 otherAfter) =>
        before.Level == after.Level && after.Zoom > before.Zoom && anchorBefore.DistanceTo(anchorAfter) < 1 &&
        Math.Abs(otherAfter.DistanceTo(anchorAfter) / otherBefore.DistanceTo(anchorBefore) - after.Zoom / before.Zoom) < 0.005f;

    private Vector2 StarPoint(int id) => _main.UiGetCatalogScreenPosition(id)
        ?? throw new InvalidOperationException($"Catalog star {id} has no rendered position.");

    private Vector2 BodyPoint(int id) => _main.UiGetBodyScreenPosition(id)
        ?? throw new InvalidOperationException($"Known body {id} has no rendered position.");

    private Button ZoomButton(bool inward) => Descendants(_dock).OfType<Button>().Single(button =>
        button.Name == (inward ? "MapZoomIn" : "MapZoomOut"));

    private async Task WheelAsync(bool inward, Vector2 point)
    {
        await ClickPositionAsync(point, inward ? MouseButton.WheelUp : MouseButton.WheelDown);
        await WaitForCameraAsync();
    }

    private async Task ClickZoomAsync(bool inward)
    {
        await ClickControlAsync(ZoomButton(inward));
        await WaitForCameraAsync();
    }

    private async Task VerifyCameraJourneyAsync()
    {
        Require(_main.UiIsPaused && !_sidebar.IsDrawerOpen && !_main.UiIsSystemSpatialView,
            "Camera acceptance must start on the paused, unobstructed normal map.");
        await WaitForCameraAsync();
        var home = _main.UiSelectedSystemId;
        var baseline = ObserveCamera();
        var homeBefore = StarPoint(home);
        var comparisonId = PublicCatalogIds().First(id => id != home);
        var comparisonBefore = StarPoint(comparisonId);

        await WheelAsync(true, StarPoint(home));
        var wheelCamera = ObserveCamera();
        var wheelComparison = StarPoint(comparisonId);
        Require(wheelCamera.Zoom > baseline.Zoom && !SameCamera(baseline, wheelCamera),
            "Uncovered regional wheel positive control did not zoom.");
        await WheelAsync(false, StarPoint(home));
        Require(SameCamera(baseline, ObserveCamera()), "Opposite regional wheel did not restore its transform.");
        await ClickZoomAsync(true);
        Check(SameZoomRoute(baseline, wheelCamera, homeBefore, StarPoint(home), comparisonBefore, wheelComparison) &&
            SameZoomRoute(baseline, ObserveCamera(), homeBefore, StarPoint(home), comparisonBefore, StarPoint(comparisonId)),
            "regional-wheel-button-zoom-parity");
        await ClickZoomAsync(false);

        var overviewSteps = 0;
        while (ObserveCamera().Level != "GalaxyOverview" || !FullGalaxyArtworkFits() || !PublicCatalogFits())
        {
            Require(overviewSteps++ < 24, "The full public galaxy catalog could not fit in the overview.");
            var previous = ObserveCamera();
            await WheelAsync(false, StarPoint(home));
            Require(ObserveCamera().Zoom < previous.Zoom, "Overview zoom stopped before the public catalog fitted.");
        }
        Check(overviewSteps > 0 && !_main.UiIsSystemSpatialView, "galaxy-overview-reachable-by-wheel");
        Check(PublicCatalogFits() && FullGalaxyArtworkFits(), "galaxy-overview-shows-public-catalog");
        await SaveViewportAsync("14-galaxy-overview.png");
        await ClickControlAsync(Descendants(_main).OfType<Button>().Single(button => button.Name == "SpatialRegion"));
        await WaitForCameraAsync();
        Check(SameCamera(baseline, ObserveCamera()) && StarPoint(home).DistanceTo(homeBefore) < 1 &&
            StarPoint(comparisonId).DistanceTo(comparisonBefore) < 1, "galaxy-region-zoom-roundtrip-restores");

        await WheelAsync(true, StarPoint(home));
        await WheelAsync(true, StarPoint(home));
        Require(ObserveCamera().Level == "StellarRegion" && ObserveCamera().Zoom > baseline.Zoom,
            "Zoomed regional view crossed into an unrelated camera level.");
        await SaveViewportAsync("15-zoomed-region.png");
        var dragStart = StarPoint(home);
        var dragEnd = dragStart + new Vector2(42, -27);
        await DragAsync(dragStart, dragEnd);
        await WaitForCameraAsync();
        Require(StarPoint(home).DistanceTo(dragEnd) < 1, "Region camera did not follow a real middle drag.");
        await SelectDifferentStarAsync(home);
        await ClickPositionAsync(StarPoint(home), MouseButton.Left);
        Check(_main.UiSelectedSystemId == home, "regional-pan-inverse-hit");
        await VerifyDrawerWheelShieldingAsync(home);
        await VerifyResizedStarPickingAsync(home);

        var regionReturn = ObserveCamera();
        var regionReturnHome = StarPoint(home);
        var regionReturnOther = StarPoint(comparisonId);
        await ClickButtonAsync(_dock, "Open System");
        await WaitForCameraAsync();
        Require(ObserveCamera().Level == "StarSystem" && _main.UiIsSystemSpatialView,
            "The ordinary Open System button did not enter orbital space.");
        var infrastructure = _main.UiSystemInfrastructure;
        Check(infrastructure.Count == 2 &&
            infrastructure.Any(item => item.ProjectId == "orbital_launch_complex" &&
                item.State == SystemSpatialInfrastructureState.Available) &&
            infrastructure.Any(item => item.ProjectId == "orbital_shipyard" &&
                item.State == SystemSpatialInfrastructureState.Locked),
            "home-orbit-shows-infrastructure-plan");
        await ClickPositionAsync(BodyPoint(3), MouseButton.Left);
        Require(_main.UiSelectedBodyId == 3, "Earth was not selected for the system zoom anchor.");
        var systemBefore = ObserveCamera();
        var earthZoomAnchor = BodyPoint(3);
        var marsBefore = BodyPoint(4);
        await WheelAsync(true, BodyPoint(3));
        var systemWheel = ObserveCamera();
        var marsWheel = BodyPoint(4);
        Require(systemWheel.Zoom > systemBefore.Zoom, "Uncovered system wheel positive control did not zoom.");
        await WheelAsync(false, BodyPoint(3));
        Require(SameCamera(systemBefore, ObserveCamera()), "Opposite system wheel did not restore its transform.");
        await ClickZoomAsync(true);
        Check(SameZoomRoute(systemBefore, systemWheel, earthZoomAnchor, BodyPoint(3), marsBefore, marsWheel) &&
            SameZoomRoute(systemBefore, ObserveCamera(), earthZoomAnchor, BodyPoint(3), marsBefore, BodyPoint(4)),
            "system-wheel-button-zoom-parity");
        await ClickZoomAsync(false);

        await OpenSectionAsync("research");
        var shieldCamera = ObserveCamera();
        var shieldEarth = BodyPoint(3);
        foreach (var inward in new[] { true, false })
        {
            await WheelAsync(inward, ScreenRect(_drawer).Position + new Vector2(4, 74));
            Require(SameCamera(shieldCamera, ObserveCamera()) && BodyPoint(3).DistanceTo(shieldEarth) < 0.1f,
                "A wheel event zoomed orbital space through the research drawer.");
        }
        await CloseDrawerAsync();
        Check(true, "drawer-blocks-camera-wheel");

        var earthBeforePan = BodyPoint(3);
        await DragAsync(new Vector2(460, 500), new Vector2(492, 476));
        await WaitForCameraAsync();
        Require(BodyPoint(3).DistanceTo(earthBeforePan + new Vector2(32, -24)) < 1,
            "System camera did not follow the real middle drag.");
        var revision = _main.UiPointerCommandRevision;
        await ClickPositionAsync(BodyPoint(4), MouseButton.Left);
        Require(_main.UiSelectedBodyId == 4, "Mars inverse-hit positive control did not change selection.");
        await ClickPositionAsync(BodyPoint(3), MouseButton.Left);
        Check(_main.UiSelectedBodyId == 3 && _main.UiPointerCommandRevision == revision, "system-pan-inverse-hit");
        await VerifyResizedBodyPickingAsync();
        var focusReturn = ObserveCamera();
        var focusReturnEarth = BodyPoint(3);
        var focusReturnMars = BodyPoint(4);
        await ClickPositionAsync(BodyPoint(3), MouseButton.Left, doubleClick: true);
        await WaitForCameraAsync();
        Check(ObserveCamera().Level == "PlanetFocus" && ObserveCamera().FocusedBodyId == 3 &&
            _main.UiSelectedBodyId == 3 && _main.UiPointerCommandRevision == revision, "planet-focus-by-real-double-click");
        await SaveViewportAsync("16-earth-focus.png");
        await VerifyFocusedMenuShieldingAsync();
        var spatialBack = Descendants(_main).OfType<Button>().Single(button => button.Name == "SpatialBack");
        await ClickControlAsync(spatialBack);
        await WaitForCameraAsync();
        Check(SameCamera(focusReturn, ObserveCamera()) && BodyPoint(3).DistanceTo(focusReturnEarth) < 1 &&
            BodyPoint(4).DistanceTo(focusReturnMars) < 1, "planet-focus-back-restores-system-camera");
        await ClickControlAsync(Descendants(_main).OfType<Button>().Single(button => button.Name == "SpatialPlanet"));
        await WaitForCameraAsync();
        Require(ObserveCamera().Level == "PlanetFocus" && ObserveCamera().FocusedBodyId == 3,
            "The visible planet breadcrumb did not share double-click's focus route.");
        await WheelAsync(false, BodyPoint(3));
        Check(SameCamera(focusReturn, ObserveCamera()) && BodyPoint(3).DistanceTo(focusReturnEarth) < 1,
            "planet-wheel-button-route-parity");
        Require(_main.UiCachedPlanetMaterialCount > 0 &&
            _main.UiSystemBodies.Any(body => body.BodyId == 3 && body.SurfaceKey == "earth" && body.HasDetailedEnvironment),
            "Known Earth material positive control is missing from the observer-safe presentation.");
        await ClickButtonAsync(_dock, "Back to Region");
        await WaitForCameraAsync();
        Check(SameCamera(regionReturn, ObserveCamera()) && StarPoint(home).DistanceTo(regionReturnHome) < 1 &&
            StarPoint(comparisonId).DistanceTo(regionReturnOther) < 1, "system-back-restores-region-camera");

        // Wheel navigation must cross the same entry boundary, then restore the last regional camera.
        CameraObservation beforeEntry = ObserveCamera();
        for (var step = 0; !_main.UiIsSystemSpatialView; step++)
        {
            Require(step < 20, "Wheel zoom never entered the selected known star's system.");
            beforeEntry = ObserveCamera();
            await WheelAsync(true, StarPoint(home));
        }
        Require(ObserveCamera().Level == "StarSystem", "Wheel entry did not use the ordinary orbital view.");
        await ClickButtonAsync(_dock, "Back to Region");
        await WaitForCameraAsync();
        Check(SameCamera(beforeEntry, ObserveCamera()), "wheel-enters-system-and-restores-region");
        await VerifyUnknownEntryPrivacyAsync(home);
        await ClickButtonAsync(_dock, "Home");
        await WaitForCameraAsync();
        Check(_sawSmoothCameraTransition, "camera-transitions-settle-smoothly");
        Require(SameCamera(baseline, ObserveCamera()) && _main.UiSelectedSystemId == home && _main.UiIsPaused,
            "Camera probes failed to restore the ordinary campaign for the existing acceptance checks.");
    }

    private int[] PublicCatalogIds() => _main.UiSpatialCatalog.Select(system => system.SystemId).ToArray();

    private bool FullGalaxyArtworkFits() => Encloses(GetViewport().GetVisibleRect(), _main.UiGalaxyArtworkScreenRect);

    private bool PublicCatalogFits()
    {
        var mapBounds = new Rect2(112, 146, GetViewport().GetVisibleRect().Size.X - 128,
            GetViewport().GetVisibleRect().Size.Y - 282);
        var ids = PublicCatalogIds();
        return ids.Length > 1 && ids.All(id => mapBounds.HasPoint(StarPoint(id)));
    }

    private async Task SelectDifferentStarAsync(int target)
    {
        var mapBounds = new Rect2(125, 152, GetViewport().GetVisibleRect().Size.X - 150,
            GetViewport().GetVisibleRect().Size.Y - 300);
        var other = PublicCatalogIds().Where(id => id != target).Select(id => (Id: id, Point: StarPoint(id)))
            .First(candidate => mapBounds.HasPoint(candidate.Point) && candidate.Point.DistanceTo(StarPoint(target)) > 35);
        await ClickPositionAsync(other.Point, MouseButton.Left);
        Require(_main.UiSelectedSystemId == other.Id, "Alternate catalog star positive control did not change selection.");
    }

    private async Task VerifyDrawerWheelShieldingAsync(int home)
    {
        await OpenSectionAsync("research");
        var camera = ObserveCamera();
        var homePoint = StarPoint(home);
        var covered = ScreenRect(_drawer).Position + new Vector2(4, 74);
        foreach (var inward in new[] { true, false })
        {
            await WheelAsync(inward, covered);
            Require(SameCamera(camera, ObserveCamera()) && StarPoint(home).DistanceTo(homePoint) < 0.1f,
                "A wheel event zoomed the map through the research drawer.");
        }
        await CloseDrawerAsync();
    }

    private async Task VerifyFocusedMenuShieldingAsync()
    {
        var camera = ObserveCamera();
        await OpenSectionAsync("menu");
        await ClickNamedButtonAsync(ActivePanel(), "CampaignMenu");
        Require(_main.UiIsMenuOpen, "Focused-planet menu did not open through its ordinary controls.");
        foreach (var button in new[] { MouseButton.WheelUp, MouseButton.WheelDown })
        {
            await ClickPositionAsync(new Vector2(220, 380), button);
            Require(Equals(camera, ObserveCamera()), "The menu allowed a focused-planet wheel gesture.");
        }
        await DragAsync(new Vector2(220, 380), new Vector2(250, 410));
        Check(Equals(camera, ObserveCamera()) && _main.UiSelectedBodyId == 3 && _main.UiIsMenuOpen,
            "focused-menu-blocks-camera");
        await ClickNamedButtonAsync(_main.GetNode<Godot.CanvasLayer>("MainMenuLayer"), "ResumeCampaign");
        if (!_main.UiIsPaused) await PressKeyAsync(Key.Space);
        await CloseDrawerAsync();
        Require(SameCamera(camera, ObserveCamera()) && _main.UiIsPaused,
            "Continue changed the focused camera or could not restore the paused acceptance campaign.");
    }

    private async Task AtLargerViewportAsync(Func<Task> verify)
    {
        var window = GetWindow();
        var previousSize = window.Size;
        var previousContentScale = window.ContentScaleSize;
        try
        {
            window.ContentScaleSize = new Vector2I(1600, 900);
            window.Size = new Vector2I(1600, 900);
            await WaitFramesAsync(12);
            await WaitForCameraAsync();
            Require(GetViewport().GetVisibleRect().Size == new Vector2(1600, 900),
                "Resize probe did not change the logical drawing and picking viewport.");
            await verify();
        }
        finally
        {
            window.ContentScaleSize = previousContentScale;
            window.Size = previousSize;
            await WaitFramesAsync(12);
            await WaitForCameraAsync();
        }
        Require(GetViewport().GetVisibleRect().Size == new Vector2(1280, 720),
            "Resize probe did not restore the minimum supported viewport.");
    }

    private async Task VerifyResizedStarPickingAsync(int home)
    {
        var previousPoint = StarPoint(home);
        await AtLargerViewportAsync(async () =>
        {
            await SelectDifferentStarAsync(home);
            await ClickPositionAsync(StarPoint(home), MouseButton.Left);
            Check(_main.UiSelectedSystemId == home && !_main.UiIsSystemSpatialView, "resize-preserves-star-hit");
        });
        Require(StarPoint(home).DistanceTo(previousPoint) < 1, "Resizing back shifted the regional selection's camera.");
    }

    private async Task VerifyResizedBodyPickingAsync()
    {
        var previousPoint = BodyPoint(3);
        var revision = _main.UiPointerCommandRevision;
        await AtLargerViewportAsync(async () =>
        {
            await ClickPositionAsync(BodyPoint(4), MouseButton.Left);
            Require(_main.UiSelectedBodyId == 4, "Resized Mars positive control did not change selection.");
            await ClickPositionAsync(BodyPoint(3), MouseButton.Left);
            Check(_main.UiSelectedBodyId == 3 && _main.UiPointerCommandRevision == revision, "resize-preserves-body-hit");
        });
        Require(BodyPoint(3).DistanceTo(previousPoint) < 1, "Resizing back shifted the system selection's camera.");
        var railScroll = _main.GetNode<ScrollContainer>("CampaignSidebar/NavigationRail/NavigationScroll");
        Require(railScroll.ScrollVertical == 0,
            $"Navigation rail retained a {railScroll.ScrollVertical}px vertical scroll at the minimum viewport.");
        foreach (var button in Descendants(railScroll).OfType<Button>())
            Require(Encloses(ScreenRect(railScroll), ScreenRect(button)),
                $"Navigation button {button.Name} escaped the minimum rail: rail={ScreenRect(railScroll)}, button={ScreenRect(button)}.");
        Require(Encloses(GetViewport().GetVisibleRect(), ScreenRect(_dock)),
            $"Action dock escaped the restored minimum viewport: dock={ScreenRect(_dock)}.");
        Check(true, "resize-restores-minimum-layout");
    }

    private async Task VerifyUnknownEntryPrivacyAsync(int home)
    {
        await ClickButtonAsync(_dock, "Home");
        await WaitForCameraAsync();
        var mapBounds = new Rect2(125, 152, 1125, 420);
        var catalog = _main.UiSpatialCatalog;
        var unknown = catalog.First(system => system.SystemId != home && system.SurveyLevel < SystemSurveyLevel.PartiallySurveyed &&
            mapBounds.HasPoint(StarPoint(system.SystemId)) && catalog.Where(other => other.SystemId != system.SystemId)
                .All(other => StarPoint(other.SystemId).DistanceTo(StarPoint(system.SystemId)) > 18));
        await ClickPositionAsync(StarPoint(unknown.SystemId), MouseButton.Left);
        Require(_main.UiSelectedSystemId == unknown.SystemId, "Unknown-star pointer selection failed.");
        await ClickButtonAsync(_dock, "Open System");
        await WaitForCameraAsync();
        Require(!_main.UiIsSystemSpatialView, "Open System exposed an unreconnoitred system.");
        await ClickPositionAsync(StarPoint(unknown.SystemId), MouseButton.Left, doubleClick: true);
        await WaitForCameraAsync();
        Require(!_main.UiIsSystemSpatialView, "Double-click exposed an unreconnoitred system.");
        for (var step = 0; step < 24; step++)
        {
            var before = ObserveCamera();
            await WheelAsync(true, StarPoint(unknown.SystemId));
            Require(!_main.UiIsSystemSpatialView, "Wheel entry exposed an unreconnoitred system.");
            if (Math.Abs(before.Zoom - ObserveCamera().Zoom) < 0.00001f) break;
            Require(step < 23, "Unknown-star zoom failed to reach its safe camera limit.");
        }
        Check(_main.UiSelectedSystemId == unknown.SystemId && !_main.UiIsSystemSpatialView &&
            ObserveCamera().FocusedBodyId is null, "unknown-system-entry-preserves-privacy");
        Check(_main.UiSystemBodies.Count == 0 && _main.UiCachedPlanetMaterialCount == 0 && _main.UiSelectedBodyId is null &&
            _main.UiGetBodyLabel(3) is null && _main.UiGetBodyScreenPosition(3) is null,
            "unknown-body-materials-redacted");
    }
}

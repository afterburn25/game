using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Game.Presentation;
using Game.Simulation;
using Godot;

namespace Game.Tools;

public partial class ScreenshotCapture
{
    private async Task<string> VerifySurfaceJourneyAsync(string normalSave, string normalSaveHash)
    {
        await CloseDrawerAsync();
        var surface = await LandOnEarthAsync();
        var initial = _main.UiCurrentSurface ?? throw new InvalidOperationException("Surface has no owned-colony snapshot.");
        Check(initial.BodyId == 3 && initial.PlanetName == "Earth" && initial.Buildings.Count == 0 &&
            _main.UiIsSurfaceOpen, "earth-surface-opens-from-real-breadcrumb");
        foreach (var button in Descendants(surface).OfType<Button>().Where(button => button.IsVisibleInTree()))
            AssertInsideViewport(button, "surface " + button.Name);
        Check(true, "surface-controls-fit-1280x720");
        await ClickControlAsync(SurfaceButton(surface, "SurfacePause"));
        Require(_main.UiIsPaused, "Surface Pause did not stop the real campaign.");
        var revision = _main.UiPointerCommandRevision;
        var camera = surface.CameraPosition;
        await DragAsync(new Vector2(350, 330), new Vector2(388, 344));
        Require(surface.CameraPosition.DistanceTo(camera) > 1, "Surface middle drag did not move its 3D camera.");
        var beforeZoom = surface.CameraPosition;
        await ClickPositionAsync(new Vector2(350, 330), MouseButton.WheelUp);
        Require(surface.CameraPosition.DistanceTo(beforeZoom) > 1, "Surface wheel positive control did not zoom.");
        await ClickPositionAsync(new Vector2(350, 330), MouseButton.WheelDown);
        Require(surface.CameraPosition.DistanceTo(beforeZoom) < 0.1f, "Surface reciprocal wheel lost its prior camera distance.");
        var hudPoint = ScreenRect(surface.GetNode<Control>("SurfaceHeader")).Position + new Vector2(4, 4);
        await ClickPositionAsync(hudPoint, MouseButton.WheelUp);
        Check(surface.CameraPosition.DistanceTo(beforeZoom) < 0.1f && _main.UiPointerCommandRevision == revision,
            "surface-camera-input-and-hud-shielding");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceCenterHub"));

        await ClickControlAsync(SurfaceButton(surface, "SurfaceBuild_science_lab"));
        var labGround = await FindValidSurfacePointAsync(surface);
        Check(surface.SelectedBuildingType == "science_lab" && surface.HasGroundPreview && surface.PlacementErrorText is null,
            "surface-valid-free-placement-preview");
        await SaveViewportAsync("17-surface-placement.png");
        var industryBefore = _main.UiCurrentSurface!.Industry;
        await ClickPositionAsync(labGround.Screen, MouseButton.Left);
        await WaitForRefreshAsync();
        var afterLab = _main.UiCurrentSurface!;
        var lab = afterLab.Buildings.Single();
        Check(lab.TypeId == "science_lab" && Math.Abs(lab.X - labGround.X) < 0.1f && Math.Abs(lab.Z - labGround.Z) < 0.1f &&
            lab.Progress == 0 && !lab.Complete && !lab.Powered && afterLab.Industry == industryBefore,
            "surface-real-ground-click-places-unfunded-site");
        await ClickPositionAsync(labGround.Screen, MouseButton.Left);
        await WaitForRefreshAsync();
        Check(_main.UiCurrentSurface!.Buildings.Count == 1 && _main.UiCurrentSurface.Industry == industryBefore &&
            surface.PlacementErrorText?.Contains("overlap", StringComparison.OrdinalIgnoreCase) == true,
            "surface-collision-rejected-without-charge");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceBuild_power_generator"));
        await ClickControlAsync(SurfaceButton(surface, "SurfaceRotate"));
        var generatorGround = await FindValidSurfacePointAsync(surface);
        await ClickPositionAsync(generatorGround.Screen, MouseButton.Left);
        await WaitForRefreshAsync();
        var placed = _main.UiCurrentSurface!;
        Require(placed.Buildings.Count == 2 && placed.Buildings.Any(building => building.TypeId == "power_generator" &&
            Math.Abs(building.X - generatorGround.X) < 0.1f && Math.Abs(building.Z - generatorGround.Z) < 0.1f &&
            building.RotationDegrees == 15) && placed.Buildings.All(building => building.Progress == 0) &&
            placed.Industry == industryBefore && _main.UiPointerCommandRevision == revision,
            "The rotated generator placement leaked input, snapped coordinates, or advanced while paused.");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceCancel"));
        await ClickControlAsync(SurfaceButton(surface, "SurfaceSave"));
        Check(File.Exists(ProjectSettings.GlobalizePath("user://saves/developer-autosave.json")) &&
            HashFile(normalSave) == normalSaveHash, "surface-save-keeps-normal-campaign-separate");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceBack"));
        Check(!_main.UiIsSurfaceOpen && ObserveCamera().Level == "PlanetFocus" && _main.UiSelectedBodyId == 3 &&
            _main.UiPointerCommandRevision == revision, "surface-back-restores-orbit-without-map-input");

        // Resume using the ordinary guide; the surface Pause button intentionally resumes normal speed.
        await ClickButtonAsync(_dock, "Back to Region");
        await WaitForCameraAsync();
        // Milestone visibility refreshes on its own cadence after the system closes.
        await WaitForRefreshAsync();
        await ClickButtonAsync(_main.GetNode("DemoProgressPanel/DemoMilestones"), "Guide");
        await ClickNamedButtonAsync(ActivePanel(), "DeveloperResumeSpeed");
        Require(_main.UiCurrentSpeed == SimulationClock.SpeedLevel.Demo, "The real demo guide did not resume its accelerated clock.");
        await CloseDrawerAsync();
        surface = await LandOnEarthAsync();
        var started = Time.GetTicksMsec();
        var sawIncompleteProgress = false;
        while (true)
        {
            var current = _main.UiCurrentSurface ?? throw new InvalidOperationException("Surface closed during ordinary construction.");
            Require(current.Buildings.Count == 2, "Ordinary construction lost or duplicated a placed site.");
            sawIncompleteProgress |= current.Buildings.Any(building => building.Progress is > 0 and < 1);
            if (current.Buildings.All(building => building.Complete && building.Powered)) break;
            Require(Time.GetTicksMsec() - started < 90000, "Ordinary demo construction failed to complete within the bounded rendering run.");
            await ToSignal(GetTree().CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
        }
        await ClickControlAsync(SurfaceButton(surface, "SurfacePause"));
        var complete = _main.UiCurrentSurface!;
        Check(sawIncompleteProgress && _main.UiIsPaused && complete.PowerSupply >= complete.PowerDemand &&
            complete.Buildings.All(building => building.Complete && building.Powered && building.Progress == 1) &&
            complete.Buildings.All(building => placed.Buildings.Any(old => old.Id == building.Id && old.X == building.X &&
                old.Z == building.Z && old.RotationDegrees == building.RotationDegrees)),
            "surface-ordinary-progress-completes-powered-buildings");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceCenterHub"));
        await SaveViewportAsync("18-surface-colony.png");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceSave"));
        Require(HashFile(normalSave) == normalSaveHash, "Completed surface save changed the normal campaign.");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceBack"));
        await ClickButtonAsync(_dock, "Back to Region");
        await WaitForCameraAsync();
        await OpenSectionAsync("menu");
        await ClickNamedButtonAsync(ActivePanel(), "CampaignMenu");
        normalSaveHash = await ReloadDeveloperThroughPlayerAsync(normalSave, normalSaveHash);
        await CloseDrawerAsync();
        surface = await LandOnEarthAsync();
        var reloaded = _main.UiCurrentSurface!;
        Check(reloaded.Buildings.Count == complete.Buildings.Count && reloaded.Buildings.All(building =>
            complete.Buildings.Any(old => old.Id == building.Id && old.TypeId == building.TypeId && old.X == building.X &&
                old.Z == building.Z && old.RotationDegrees == building.RotationDegrees && building.Complete && building.Powered)) &&
            HashFile(normalSave) == normalSaveHash, "surface-real-save-reload-retains-buildings");
        return normalSaveHash;
    }

    private async Task<PlanetSurfaceView> LandOnEarthAsync()
    {
        await ClickButtonAsync(_dock, "Home");
        await WaitForCameraAsync();
        await ClickButtonAsync(_dock, "Open System");
        await WaitForCameraAsync();
        await ClickPositionAsync(BodyPoint(3), MouseButton.Left, doubleClick: true);
        await WaitForCameraAsync();
        await ClickControlAsync(Descendants(_main).OfType<Button>().Single(button => button.Name == "SpatialSurface"));
        await WaitForRefreshAsync();
        Require(_main.UiIsSurfaceOpen, "The visible Surface breadcrumb did not open owned Earth terrain.");
        return _main.GetNode<PlanetSurfaceView>("PlanetSurfaceLayer/PlanetSurfaceView");
    }

    private static Button SurfaceButton(PlanetSurfaceView surface, string name) =>
        Descendants(surface).OfType<Button>().Single(button => button.Name == name);

    private async Task<(float X, float Z, Vector2 Screen)> FindValidSurfacePointAsync(PlanetSurfaceView surface)
    {
        // Candidate points use fractional metres; the actual terrain ray decides the placement.
        var clearGround = new Rect2(70, 165, 1140, 280);
        var existing = _main.UiCurrentSurface!.Buildings;
        var candidates = from x in Enumerable.Range(-4, 9)
                         from z in Enumerable.Range(-4, 9)
                         let px = x * 25.3f + 0.375f
                         let pz = z * 25.3f + 0.125f
                         let screen = surface.GetSurfaceScreenPosition(px, pz)
                         where px * px + pz * pz > 55 * 55 && screen.HasValue && clearGround.HasPoint(screen.Value)
                         where existing.All(building => new Vector2(building.X - px, building.Z - pz).Length() > 70)
                         orderby screen.GetValueOrDefault().DistanceTo(new Vector2(640, 340))
                         select (X: px, Z: pz, Screen: screen.GetValueOrDefault());
        foreach (var candidate in candidates)
        {
            Input.ParseInputEvent(new InputEventMouseMotion { Position = candidate.Screen, GlobalPosition = candidate.Screen });
            _mouseActions++;
            GD.Print($"STELLAR_MOUSE_INPUT SurfacePreview {candidate.Screen.X:0.0},{candidate.Screen.Y:0.0}");
            await WaitFramesAsync(3);
            if (surface.HasGroundPreview && surface.PlacementErrorText is null) return candidate;
        }
        throw new InvalidOperationException("No visible valid free ground placement was reachable through the real camera.");
    }
}

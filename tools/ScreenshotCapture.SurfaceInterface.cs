using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Game.Presentation;
using Godot;

namespace Game.Tools;

public partial class ScreenshotCapture
{
    // Focused real-engine lane. Setup uses a separate Developer campaign; all surface actions
    // are actual UI input. This lane never emits the full-campaign acceptance marker.
    private async Task CaptureSurfaceInterfaceAsync()
    {
        _outputDirectory = System.Environment.GetEnvironmentVariable("STELLAR_SCREENSHOT_DIR")
            ?? ProjectSettings.GlobalizePath("user://surface-interface-captures");
        Directory.CreateDirectory(_outputDirectory);
        _main = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<Main>();
        AddChild(_main);
        await WaitFramesAsync(30);
        _main.UiCreateDeveloperCampaignConfirmed(20260908);
        await WaitForRefreshAsync();
        await ClickNamedButtonAsync(_main.GetNode("MainMenuLayer"), "ResumeCampaign");
        _main.UiSetPaused(true);
        Require(_main.UiRunDeveloperCommand("grant_resources").Accepted, "Could not fund isolated test campaign.");
        _main.UiOpenOwnedColony(_main.UiOwnedColonies.Single(item => item.PlanetName == "Earth").ColonyId, true);
        await WaitForRefreshAsync();
        var surface = _main.GetNode<PlanetSurfaceView>("PlanetSurfaceLayer/PlanetSurfaceView");
        Require(surface.IsOpen && !surface.BuildMenuOpen, "Surface entry did not keep the catalogue closed.");
        foreach (var button in Descendants(surface).OfType<Button>().Where(button => button.IsVisibleInTree()))
            AssertInsideViewport(button, "surface entry " + button.Name);
        await SaveViewportAsync("surface-01-colony.png");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceBuildMenu"));
        Require(surface.BuildMenuOpen, "Build did not open catalogue.");
        await SaveViewportAsync("surface-02-build.png");
        await SelectSurfaceBuildAsync(surface, "SurfaceBuild_science_lab");
        var point = await FindValidSurfacePointAsync(surface);
        var before = _main.UiCurrentSurface!;
        var impact = Descendants(surface).OfType<Label>().Single(item => item.Name == "SurfaceDetailImpact");
        Require(impact.Text.Contains("Balance") && before.Buildings.Count == 0, "Preview has no visible power impact.");
        await SaveViewportAsync("surface-03-placement.png");
        await ClickPositionAsync(point.Screen, MouseButton.Left);
        await WaitForRefreshAsync();
        Require(_main.UiCurrentSurface!.Buildings.Count == 1 && _main.UiCurrentSurface.Industry == before.Industry &&
            _main.UiCurrentSurface.Credits == before.Credits - 40, "Placement did not charge only authorization credits.");
        await ClickPositionAsync(point.Screen, MouseButton.Left);
        await WaitForRefreshAsync();
        Require(_main.UiCurrentSurface!.Buildings.Count == 1, "Overlap created a duplicate site.");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceProjects"));
        Require(Descendants(surface).OfType<Button>().Any(item => item.Name == "SurfaceProject_1" && item.IsVisibleInTree()), "Active site is absent from Projects.");
        await SaveViewportAsync("surface-04-projects.png");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceProject_1"));
        Require(SurfaceButton(surface, "SurfaceRemove").Text == "Cancel site", "Projects did not select its construction site.");
        Require(_main.UiRunDeveloperCommand("finish_orders").Accepted, "Could not finish the isolated test site.");
        await WaitForRefreshAsync();
        var funded = _main.UiCurrentSurface!;
        await ClickControlAsync(SurfaceButton(surface, "SurfaceUpgrade"));
        Require(surface.UpgradePreviewOpen && _main.UiCurrentSurface!.Credits == funded.Credits &&
            _main.UiCurrentSurface.Industry == funded.Industry, "Upgrade preview spent resources.");
        Require(impact.Text.Contains("need more power"), "Upgrade preview hid its power deficit.");
        await SaveViewportAsync("surface-05-upgrade-preview.png");
        // Hold across multiple snapshot refreshes: refreshing the preview must not
        // hide/re-show the confirm button and cancel its mouse-down state.
        var confirm = SurfaceButton(surface, "SurfaceConfirmUpgrade");
        await RevealControlAsync(confirm);
        var confirmPoint = ScreenRect(confirm).GetCenter();
        Input.ParseInputEvent(new InputEventMouseMotion { Position = confirmPoint, GlobalPosition = confirmPoint });
        Input.ParseInputEvent(new InputEventMouseButton { Position = confirmPoint, GlobalPosition = confirmPoint,
            ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        await WaitForRefreshAsync();
        Require(_main.UiCurrentSurface!.Credits == funded.Credits, "Upgrade committed before confirmation release.");
        Input.ParseInputEvent(new InputEventMouseButton { Position = confirmPoint, GlobalPosition = confirmPoint,
            ButtonIndex = MouseButton.Left, Pressed = false });
        _mouseActions++;
        GD.Print($"STELLAR_MOUSE_INPUT SurfaceConfirmHeld {confirmPoint.X:0.0},{confirmPoint.Y:0.0}");
        await WaitForRefreshAsync();
        Require(_main.UiCurrentSurface!.Buildings.Single().TypeId == "advanced_science_lab" &&
            !_main.UiCurrentSurface.Buildings.Single().Powered, "The confirmed upgrade did not follow real power rules.");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceLayerPower"));
        Require(surface.SurfaceLayer == "Power", "Power layer did not activate.");
        await SaveViewportAsync("surface-06-power.png");
        await SelectSurfaceBuildAsync(surface, "SurfaceBuild_power_generator");
        var generator = await FindValidSurfacePointAsync(surface);
        await ClickPositionAsync(generator.Screen, MouseButton.Left);
        await WaitForRefreshAsync();
        await ClickControlAsync(SurfaceButton(surface, "SurfaceLayerConstruction"));
        await ClickControlAsync(SurfaceButton(surface, "SurfaceProjects"));
        await SaveViewportAsync("surface-07-construction.png");
        Require(_main.UiRunDeveloperCommand("finish_orders").Accepted, "Could not finish generator.");
        await WaitForRefreshAsync();
        Require(_main.UiCurrentSurface!.Buildings.All(item => item.Complete && item.Powered), "Generator did not restore the laboratory.");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceOverview"));
        await ClickControlAsync(SurfaceButton(surface, "SurfaceLayerColony"));
        await SaveViewportAsync("surface-08-overview.png");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceReducedMotion"));
        Require(SurfaceButton(surface, "SurfaceReducedMotion").ButtonPressed, "Reduced motion did not switch on.");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceDrawerClose"));
        var camera = surface.CameraPosition;
        await DragAsync(new Vector2(350, 330), new Vector2(400, 350), MouseButton.Left);
        Require(surface.CameraPosition.DistanceTo(camera) > 1, "Terrain camera input stopped working.");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceCenterHub"));
        await WaitForRefreshAsync();
        Require(surface.CameraPosition.DistanceTo(camera) < .1f, "Center hub did not restore its camera.");
        await ClickControlAsync(SurfaceButton(surface, "SurfaceSave"));
        File.WriteAllText(Path.Combine(_outputDirectory, "surface-interface-result.json"), JsonSerializer.Serialize(new
        {
            passed = true, scope = "Focused surface UI with real input; isolated Developer setup and completion commands",
            fullCampaignGate = false, captures = _captures, mouseActions = _mouseActions,
            viewport = "1280x720", checks = new[] { "collapsed catalogue", "real placement and overlap", "project selection",
                "mutation-free upgrade preview", "upgrade power deficit", "power restoration", "layers", "reduced motion", "camera", "save" },
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}

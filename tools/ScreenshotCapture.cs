using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Game.Presentation;
using Game.Simulation;
using Godot;

namespace Game.Tools;

/// <summary>
/// CI-only visual smoke/capture driver. It instantiates the real Main.tscn, verifies the
/// integrated C# entry point and key UI scripts are alive, then saves rendered PNGs.
/// Normal gameplay never references this scene.
/// </summary>
public partial class ScreenshotCapture : Node
{
    private string _outputDirectory = string.Empty;
    private readonly List<string> _captures = new();
    private readonly List<string> _checks = new();

    public override async void _Ready()
    {
        try
        {
            await CaptureSuiteAsync();
            GD.Print("STELLAR_SCREENSHOT_CAPTURE_COMPLETE");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"Screenshot capture failed: {exception}");
            GetTree().Quit(1);
        }
    }

    private async Task CaptureSuiteAsync()
    {
        _outputDirectory = System.Environment.GetEnvironmentVariable("STELLAR_SCREENSHOT_DIR")
            ?? ProjectSettings.GlobalizePath("user://screenshots");
        Directory.CreateDirectory(_outputDirectory);

        var packedMain = GD.Load<PackedScene>("res://scenes/Main.tscn")
            ?? throw new InvalidOperationException("Could not load res://scenes/Main.tscn.");

        var instantiated = packedMain.Instantiate();
        AddChild(instantiated);

        if (instantiated is not Main main)
        {
            throw new InvalidOperationException(
                "Main.tscn did not instantiate as Game.Presentation.Main. " +
                "This usually means the real C# entry script failed to load.");
        }

        // These typed lookups intentionally make this a stronger semantic startup check than
        // merely trusting Godot's process exit code.
        var menu = main.GetNodeOrNull<MainMenuLayer>("MainMenuLayer")
            ?? throw new InvalidOperationException("MainMenuLayer did not instantiate.");
        _ = main.GetNodeOrNull<ExplorationMissionPanel>("ExplorationMissionPanel")
            ?? throw new InvalidOperationException("ExplorationMissionPanel did not instantiate.");
        var relationsPanel = main.GetNodeOrNull<RelationsPanel>("RelationsPanel")
            ?? throw new InvalidOperationException("RelationsPanel did not instantiate.");
        var logistics = main.GetNodeOrNull<LogisticsNetworkPanel>("LogisticsNetworkPanel")
            ?? throw new InvalidOperationException("LogisticsNetworkPanel did not instantiate.");
        var sidebar = main.GetNode<CampaignSidebar>("CampaignSidebar");
        var inspection = main.GetNode<SystemInspectionPanel>("SystemInspectionPanel");
        var dialog = FindNode<ConfirmationDialog>(menu)
            ?? throw new InvalidOperationException("Campaign confirmation dialog did not instantiate.");
        var toolbar = main.GetNode<Control>("PlayerControls/MapToolbar");

        await WaitFramesAsync(30);
        Check(main.UiIsMenuOpen && main.UiIsPaused && !main.UiIsPlayableDemo, "normal-startup-menu-paused");
        await PressKeyAsync(Key.N);
        Check(main.UiIsMenuOpen && main.UiIsPaused && !dialog.Visible && !main.UiIsPlayableDemo,
            "new-game-shortcut-blocked-by-menu");
        await SaveViewportAsync("01-main-menu.png");

        await ActivateButtonAsync(menu, "Continue");
        Check(!main.UiIsMenuOpen && !main.UiIsPaused, "continue-resumes-normal-campaign");
        await WaitForRefreshAsync();
        await SaveViewportAsync("02-campaign-overview.png");

        await ActivateButtonAsync(main, "Colony Sites");
        await SaveViewportAsync("03-colony-sites.png");

        await ActivateButtonAsync(main, "Relations");
        Check(relationsPanel.Visible, "relations-button-opens-overlay");
        await WaitForRefreshAsync();
        var relationsOverlay = relationsPanel.GetNode<Control>("RelationsOverlay");
        AssertInsideViewport(relationsOverlay, "Relations overlay");
        AssertInsideViewport(RequireButton(relationsPanel, "Close"), "Relations Close");
        await SaveViewportAsync("04-relations.png");
        await RevealControlAsync(RequireButton(relationsPanel, "Deny Access"));
        AssertInsideViewport(RequireButton(relationsPanel, "Deny Access"), "Relations last action");
        Check(true, "relations-actions-fit-and-scroll");
        await SaveViewportAsync("05-relations-actions.png");
        await ActivateButtonAsync(relationsPanel, "Close");

        await ActivateButtonAsync(main, "Menu");
        await ActivateButtonAsync(menu, "Play Demo — guided 24x opening");
        Check(dialog.Visible && main.UiIsMenuOpen && !main.UiIsPlayableDemo,
            "play-demo-requires-confirmation");
        var dialogBounds = new Rect2((Vector2)dialog.Position, (Vector2)dialog.Size);
        var viewportBounds = GetViewport().GetVisibleRect();
        Check(dialog.DialogAutowrap && dialogBounds.Position.X >= viewportBounds.Position.X &&
            dialogBounds.Position.Y >= viewportBounds.Position.Y &&
            dialogBounds.End.X <= viewportBounds.End.X && dialogBounds.End.Y <= viewportBounds.End.Y,
            "demo-confirmation-wraps-inside-viewport");
        await SaveViewportAsync("06-demo-confirmation.png");
        // Use the dialog's real buttons, preserving Godot's native confirmation/cancel handlers.
        dialog.GetCancelButton().EmitSignal(BaseButton.SignalName.Pressed);
        await WaitFramesAsync(3);
        Check(!dialog.Visible && main.UiIsMenuOpen && !main.UiIsPlayableDemo && main.UiIsPaused,
            "cancel-demo-preserves-normal-campaign");
        await ActivateButtonAsync(menu, "Play Demo — guided 24x opening");
        dialog.GetOkButton().EmitSignal(BaseButton.SignalName.Pressed);
        await WaitFramesAsync(3);
        Check(!dialog.Visible && !main.UiIsMenuOpen && main.UiIsPlayableDemo &&
            main.UiCurrentSpeed == SimulationClock.SpeedLevel.Demo, "confirm-starts-guided-demo-at-24x");
        await WaitForRefreshAsync();
        var guidance = main.GetNode<Control>("CampaignSidebar/Scroll/Panels/DemoProgress");
        Check(guidance.IsVisibleInTree() && !string.IsNullOrWhiteSpace(main.UiDemoObjective?.Objective),
            "demo-guidance-visible-with-objective");
        await RevealControlAsync(guidance);
        await SaveViewportAsync("07-demo-guidance.png");

        // Invoke ordinary early-game commands. No technology/resource injection or accelerated
        // test-only simulation path is used; prerequisite messages remain player-visible.
        await ActivateButtonAsync(main, "Next Ship");
        await ActivateButtonAsync(main, "Build / Queue Ship");
        Check(main.UiIsPlayableDemo && !string.IsNullOrWhiteSpace(main.UiShipbuildingSummary),
            "early-game-ship-buttons-dispatch");
        await SaveViewportAsync("08-demo-ship-controls.png");

        await ActivateButtonAsync(toolbar, "Hide Panels");
        Check(!sidebar.Visible && !inspection.Visible && !logistics.Visible && !relationsPanel.Visible &&
            RequireButton(toolbar, "Show Panels").IsVisibleInTree(), "hide-panels-clears-map");
        await ActivateButtonAsync(toolbar, "Home");
        var homeId = main.UiSelectedSystemId;
        Check(homeId >= 0 && !main.UiIsSystemSpatialView, "home-selects-known-star");
        await SaveViewportAsync("09-demo-map-only.png");
        await ActivateButtonAsync(toolbar, "Send Scout");
        await ActivateButtonAsync(toolbar, "Send Science");
        Check(main.UiSelectedSystemId == homeId && main.UiIsPlayableDemo,
            "early-game-exploration-buttons-dispatch");
        await ActivateButtonAsync(toolbar, "Open System");
        Check(main.UiIsSystemSpatialView && main.UiSelectedSystemId == homeId, "open-system-enters-home-orbits");
        await ActivateButtonAsync(toolbar, "Send Science");
        var feedback = main.GetNode<Control>("PlayerControls/CommandFeedback");
        Check(feedback.IsVisibleInTree() && !string.IsNullOrWhiteSpace(main.UiStatusMessage),
            "command-feedback-visible-over-system-view");
        AssertInsideViewport(feedback, "Command feedback");
        await WaitForRefreshAsync();
        await SaveViewportAsync("10-demo-home-orbits.png");
        await ActivateButtonAsync(toolbar, "Back to Region");
        Check(!main.UiIsSystemSpatialView && main.UiSelectedSystemId == homeId,
            "back-to-region-preserves-selection");
        await SaveViewportAsync("11-demo-back-to-region.png");
        await ActivateButtonAsync(toolbar, "Show Panels");
        Check(sidebar.Visible && inspection.Visible && logistics.Visible &&
            RequireButton(toolbar, "Hide Panels").IsVisibleInTree(), "show-panels-restores-controls");
        await ActivateButtonAsync(main, "Menu");
        await PressKeyAsync(Key.N);
        Check(main.UiIsPlayableDemo && main.UiIsMenuOpen && main.UiIsPaused && !dialog.Visible &&
            main.UiSelectedSystemId == homeId, "new-game-shortcut-cannot-replace-demo-under-menu");
        await ActivateButtonAsync(menu, "Continue");
        Check(main.UiIsPlayableDemo && !main.UiIsMenuOpen && main.UiCurrentSpeed == SimulationClock.SpeedLevel.Demo,
            "continue-restores-demo-speed");
        await RevealControlAsync(guidance);
        await SaveViewportAsync("12-demo-panels-restored.png");

        var captureSha = System.Environment.GetEnvironmentVariable("STELLAR_CAPTURE_SHA") ?? "unknown";
        File.WriteAllText(
            Path.Combine(_outputDirectory, "manifest.txt"),
            $"Stellar Continuum screenshot capture{System.Environment.NewLine}" +
            $"Build: {main.UiBuildLabel}{System.Environment.NewLine}" +
            $"Git SHA: {captureSha}{System.Environment.NewLine}" +
            "Scene: res://scenes/Main.tscn (real integrated runtime)" + System.Environment.NewLine +
            $"Screenshots: {string.Join(", ", _captures)}{System.Environment.NewLine}" +
            $"Passed checks: {string.Join(", ", _checks)}{System.Environment.NewLine}" +
            "Scope: real rendered UI and button-signal command wiring; early-game prerequisite handling. " +
            "Does not certify mouse hit testing, long campaign progression, or the Windows package renderer." +
            System.Environment.NewLine);
    }

    private async Task SaveViewportAsync(string fileName)
    {
        // Give the renderer a few additional frames after UI/state changes before reading back.
        await WaitFramesAsync(3);

        var image = GetViewport().GetTexture().GetImage();
        if (image is null || image.GetWidth() < 640 || image.GetHeight() < 360)
        {
            throw new InvalidOperationException(
                $"Rendered viewport was unavailable or unexpectedly small while capturing {fileName}.");
        }

        var path = Path.Combine(_outputDirectory, fileName);
        var result = image.SavePng(path);
        if (result != Error.Ok)
            throw new IOException($"Godot could not save {fileName}: {result}.");

        var info = new FileInfo(path);
        if (!info.Exists || info.Length < 4096)
            throw new IOException($"Screenshot {fileName} was not written or is unexpectedly small.");

        GD.Print($"STELLAR_SCREENSHOT_CAPTURED {fileName} {image.GetWidth()}x{image.GetHeight()} {info.Length} bytes");
        _captures.Add(fileName);
    }

    private void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException($"Visual interaction check failed: {name}.");
        _checks.Add(name);
        GD.Print($"STELLAR_UI_CHECK_PASS {name}");
    }

    private async Task ActivateButtonAsync(Node root, string text)
    {
        var button = RequireButton(root, text);
        await RevealControlAsync(button);
        if (!button.IsVisibleInTree() || button.Disabled)
            throw new InvalidOperationException($"Button is hidden or disabled: {text}.");
        AssertInsideViewport(button, text);
        button.EmitSignal(BaseButton.SignalName.Pressed);
        await WaitFramesAsync(3);
    }

    private async Task RevealControlAsync(Control control)
    {
        for (Node? ancestor = control.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
        {
            if (ancestor is ScrollContainer scroll)
            {
                scroll.EnsureControlVisible(control);
                await WaitFramesAsync(3);
            }
        }
    }

    private void AssertInsideViewport(Control control, string label)
    {
        var bounds = control.GetGlobalRect();
        var viewport = GetViewport().GetVisibleRect();
        if (bounds.Position.X < viewport.Position.X - 1 || bounds.Position.Y < viewport.Position.Y - 1 ||
            bounds.End.X > viewport.End.X + 1 || bounds.End.Y > viewport.End.Y + 1)
            throw new InvalidOperationException($"{label} is clipped by viewport: {bounds} vs {viewport}.");
    }

    private async Task PressKeyAsync(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        await WaitFramesAsync(2);
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await WaitFramesAsync(2);
    }

    private async Task WaitForRefreshAsync()
    {
        // Presentation panels poll at up to 0.5 seconds; frame counts alone are not a time barrier.
        await ToSignal(GetTree().CreateTimer(0.7), SceneTreeTimer.SignalName.Timeout);
        await WaitFramesAsync(3);
    }

    private static Button RequireButton(Node root, string text) => FindButton(root, text)
        ?? throw new InvalidOperationException($"Could not find button: {text}.");

    private static T? FindNode<T>(Node node) where T : Node
    {
        if (node is T typed) return typed;
        foreach (Node child in node.GetChildren())
        {
            var match = FindNode<T>(child);
            if (match is not null) return match;
        }
        return null;
    }

    private async Task WaitFramesAsync(int frameCount)
    {
        for (var frame = 0; frame < frameCount; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static Button? FindButton(Node node, string text)
    {
        if (node is Button button && string.Equals(button.Text, text, StringComparison.Ordinal))
            return button;

        foreach (Node child in node.GetChildren())
        {
            var match = FindButton(child, text);
            if (match is not null)
                return match;
        }

        return null;
    }
}

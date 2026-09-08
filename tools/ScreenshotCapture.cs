using System;
using System.IO;
using System.Threading.Tasks;
using Game.Presentation;
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
        _outputDirectory = Environment.GetEnvironmentVariable("STELLAR_SCREENSHOT_DIR")
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
        _ = main.GetNodeOrNull<MainMenuLayer>("MainMenuLayer")
            ?? throw new InvalidOperationException("MainMenuLayer did not instantiate.");
        _ = main.GetNodeOrNull<ExplorationMissionPanel>("ExplorationMissionPanel")
            ?? throw new InvalidOperationException("ExplorationMissionPanel did not instantiate.");
        var relationsPanel = main.GetNodeOrNull<RelationsPanel>("RelationsPanel")
            ?? throw new InvalidOperationException("RelationsPanel did not instantiate.");
        _ = main.GetNodeOrNull<LogisticsNetworkPanel>("LogisticsNetworkPanel")
            ?? throw new InvalidOperationException("LogisticsNetworkPanel did not instantiate.");

        await WaitFramesAsync(30);
        await SaveViewportAsync("01-main-menu.png");

        var continueButton = FindButton(main, "Continue")
            ?? throw new InvalidOperationException("Could not find the startup Continue button.");
        continueButton.EmitSignal(BaseButton.SignalName.Pressed);

        await WaitFramesAsync(45);
        await SaveViewportAsync("02-campaign-overview.png");

        var colonySitesButton = FindButton(main, "Colony Sites")
            ?? throw new InvalidOperationException("Could not find the Colony Sites button.");
        colonySitesButton.EmitSignal(BaseButton.SignalName.Pressed);

        await WaitFramesAsync(15);
        await SaveViewportAsync("03-colony-sites.png");

        relationsPanel.Visible = true;
        await WaitFramesAsync(15);
        await SaveViewportAsync("04-relations.png");

        var captureSha = Environment.GetEnvironmentVariable("STELLAR_CAPTURE_SHA") ?? "unknown";
        File.WriteAllText(
            Path.Combine(_outputDirectory, "manifest.txt"),
            $"Stellar Continuum screenshot capture{Environment.NewLine}" +
            $"Build: {main.UiBuildLabel}{Environment.NewLine}" +
            $"Git SHA: {captureSha}{Environment.NewLine}" +
            "Scene: res://scenes/Main.tscn (real integrated runtime)" + Environment.NewLine +
            "Screenshots: main menu, campaign overview, colony sites, relations" + Environment.NewLine);
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

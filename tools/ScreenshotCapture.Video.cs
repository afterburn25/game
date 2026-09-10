using System;
using System.Linq;
using System.Threading.Tasks;
using Game.Presentation;
using Godot;

namespace Game.Tools;

public partial class ScreenshotCapture
{
    private async Task VerifyVideoSettingsAsync(MainMenuLayer menu)
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        Require(viewportSize.X >= 1280 && viewportSize.Y >= 720,
            $"Video settings probe requires at least 1280x720, received {viewportSize}.");

        await ClickNamedButtonAsync(menu, "VideoSettings");
        var panel = Descendants(menu).OfType<Control>().Single(control => control.Name == "VideoSettingsPanel");
        var confirmation = Descendants(menu).OfType<Control>().Single(control => control.Name == "VideoSettingsConfirmation");
        var options = Descendants(panel).OfType<OptionButton>().ToArray();
        Require(options.Length == 5, "Video settings did not expose the five supported control groups.");
        foreach (var control in Descendants(panel).OfType<Control>().Where(control => control.IsVisibleInTree()))
            AssertInsideViewport(control, "video settings " + control.Name);
        if (DisplayServer.GetName() != "headless")
            await SaveViewportAsync($"video-settings-{(int)viewportSize.X}x{(int)viewportSize.Y}.png", (int)viewportSize.X, (int)viewportSize.Y);

        var resolutions = options[0];
        Require(resolutions.ItemCount > 0, "Display mode enumeration returned no resolutions.");
        var resolutionValues = Enumerable.Range(0, resolutions.ItemCount).Select(resolutions.GetItemText).ToArray();
        Require(resolutionValues.Distinct(StringComparer.Ordinal).Count() == resolutionValues.Length &&
                resolutionValues.Contains("1280 × 720"),
            "Display mode enumeration must be unique and include the safe 1280x720 fallback.");
        var detectedScreen = DisplayServer.ScreenGetSize();
        if (detectedScreen.X >= 1280 && detectedScreen.Y >= 720)
            Require(resolutionValues.Contains($"{detectedScreen.X} × {detectedScreen.Y}"),
                $"Display mode enumeration omitted the current monitor resolution {detectedScreen}.");
        Check(true, "video-native-mode-enumeration");

        Require(VideoSettingsService.WindowModeFor(VideoSettingsService.DisplayMode.Windowed) == Window.ModeEnum.Windowed &&
                VideoSettingsService.WindowModeFor(VideoSettingsService.DisplayMode.Borderless) == Window.ModeEnum.Fullscreen &&
                VideoSettingsService.WindowModeFor(VideoSettingsService.DisplayMode.Fullscreen) == Window.ModeEnum.ExclusiveFullscreen,
            "The explicit display-mode mapping regressed.");
        Check(true, "video-explicit-window-mode-mapping");

        var adapterLabel = Descendants(panel).OfType<Label>().FirstOrDefault(label =>
            label.Text.Contains(RenderingServer.GetVideoAdapterName(), StringComparison.Ordinal));
        Require(adapterLabel is not null && adapterLabel.Text.Contains(RenderingServer.GetVideoAdapterApiVersion(), StringComparison.Ordinal),
            "Video settings did not show the actual adapter and renderer API.");
        var discovery = new VideoSettingsService();
        var nvidiaInstalled = discovery.FindNvidiaControlPanel() is not null;
        var nvidiaButton = Descendants(panel).OfType<Button>().Any(button => button.Name == "OpenNvidiaControlPanel");
        Require(nvidiaButton == (discovery.IsNvidiaAdapter && nvidiaInstalled),
            "NVIDIA Control Panel action visibility did not match adapter and installation discovery.");
        Check(true, "video-real-gpu-and-driver-control-discovery");

        var original = VideoSettingsService.Current;
        await ChooseVideoOptionAsync(options[3], options[3].Selected == 0 ? 1 : 0);
        await ClickNamedButtonAsync(menu, "VideoSettingsCancel");
        Require(VideoSettingsService.Current == original, "Cancel changed runtime video settings without Apply.");
        Check(true, "video-cancel-does-not-apply-or-save");

        await ClickNamedButtonAsync(menu, "VideoSettings");
        options = Descendants(panel).OfType<OptionButton>().ToArray();
        await ChooseVideoOptionAsync(options[3], options[3].Selected == 0 ? 1 : 0);
        await ClickNamedButtonAsync(menu, "VideoSettingsDone");
        Require(confirmation.Visible && VideoSettingsService.Current != original,
            "Apply did not preview the selected rendering setting behind confirmation.");
        if (DisplayServer.GetName() != "headless")
            await SaveViewportAsync($"video-confirm-{(int)viewportSize.X}x{(int)viewportSize.Y}.png", (int)viewportSize.X, (int)viewportSize.Y);
        await ClickNamedButtonAsync(menu, "RevertVideoSettings");
        Require(VideoSettingsService.Current == original, "Revert did not restore the complete prior video state.");
        Check(true, "video-preview-and-revert");

        await ChooseVideoOptionAsync(options[4], original.RenderScale == .75f ? 1 : 0);
        await ClickNamedButtonAsync(menu, "VideoSettingsDone");
        await ClickNamedButtonAsync(menu, "KeepVideoSettings");
        var kept = VideoSettingsService.Current;
        var loader = new VideoSettingsService();
        Require(VideoSettingsService.Current == kept, "Constructing a settings reader changed the active runtime state.");
        var loaded = loader.Load();
        Require(loaded == kept, "Keep did not persist the validated video settings.");
        Check(true, "video-keep-persists-validated-settings");

        await ClickNamedButtonAsync(menu, "VideoSettings");
        options = Descendants(panel).OfType<OptionButton>().ToArray();
        await ChooseVideoOptionAsync(options[3], options[3].Selected == 0 ? 1 : 0);
        await ClickNamedButtonAsync(menu, "VideoSettingsDone");
        await ToSignal(GetTree().CreateTimer(5), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(GetTree().CreateTimer(5), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(GetTree().CreateTimer(5.5), SceneTreeTimer.SignalName.Timeout);
        Require(!confirmation.Visible && VideoSettingsService.Current == kept,
            "Display confirmation did not automatically restore the prior settings after 15 seconds.");
        Check(true, "video-automatic-rollback-after-timeout");
    }

    private async Task ChooseVideoOptionAsync(OptionButton option, int index)
    {
        AssertInsideViewport(option, "video option " + option.Name);
        await ClickPositionAsync(ScreenRect(option).GetCenter(), MouseButton.Left);
        Require(option.GetPopup().Visible, $"Video option popup did not open: {option.Name}.");
        for (var step = 0; step < option.ItemCount; step++) await PressKeyAsync(Key.Up);
        for (var step = 0; step < index; step++) await PressKeyAsync(Key.Down);
        await PressKeyAsync(Key.Enter);
        Require(option.Selected == index, $"Keyboard selection failed for {option.Name}: expected {index}, got {option.Selected}.");
    }
}

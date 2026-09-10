using System;
using System.Linq;
using Godot;

namespace Game.Presentation.Audio.Voice;

public partial class VoicePlaybackController
{
    private Control _voiceWindow = null!;
    private VBoxContainer _labForm = null!;
    private Label? _labDiagnostics, _labSubtitle;
    private OptionButton _labProfile = null!, _labEmotion = null!;
    private TextEdit _labText = null!;
    private Button _labToggle = null!;
    private bool _updatingSettings;

    private void BuildSettingsWindow()
    {
        var layer = new CanvasLayer { Name = "VoiceSettingsLayer", Layer = 120 }; AddChild(layer);
        _voiceWindow = new Control { Name = "VoiceSettings", Visible = false };
        _voiceWindow.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); layer.AddChild(_voiceWindow);
        var shade = new ColorRect { Color = new Color(0, 0, 0, .72f) };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); _voiceWindow.AddChild(shade);
        var center = new CenterContainer(); center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); _voiceWindow.AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = new(610, 0) };
        panel.AddThemeStyleboxOverride("panel", CinematicArt.Frame("panel", 18)); center.AddChild(panel);
        var column = new VBoxContainer(); column.AddThemeConstantOverride("separation", 10); panel.AddChild(column);
        var heading = new HBoxContainer(); column.AddChild(heading);
        var title = VisualUi.Text("VOICE & SUBTITLES", 22, VisualUi.Gold); title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; heading.AddChild(title);
        var close = VisualUi.Button("Close", "Return to the game settings", HideVoiceSettings, VisualIconLibrary.NavClose);
        close.Name = "VoiceSettingsClose"; heading.AddChild(close);
        var scroll = new ScrollContainer { CustomMinimumSize = new(580, 422), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(scroll);
        var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; body.AddThemeConstantOverride("separation", 8); scroll.AddChild(body);
        body.AddChild(VisualUi.Text("Speech is generated locally. Availability and vocal quality depend on installed Windows voices.", 12, VisualUi.Muted, true));
        Toggle(body, "Enable voices", Settings.EnableVoices, value => ApplySettings(Settings with { EnableVoices = value }), "VoiceEnabled");
        Slider(body, "Voice volume", Settings.Volume, value => ApplySettings(Settings with { Volume = value }), "VoiceVolume");
        Toggle(body, "Subtitles", Settings.Subtitles, value => ApplySettings(Settings with { Subtitles = value }), "VoiceSubtitles");
        var size = new HBoxContainer(); body.AddChild(size); size.AddChild(VisualUi.Text("Subtitle size", 13, VisualUi.Muted));
        var sizeChoice = new OptionButton { Name = "VoiceSubtitleSize", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (var value in new[] { 14, 18, 22, 26, 32 }) sizeChoice.AddItem(value + " px", value);
        sizeChoice.Select(Array.IndexOf(new[] { 14, 18, 22, 26, 32 }, Settings.SubtitleSize));
        sizeChoice.ItemSelected += _ => ApplySettings(Settings with { SubtitleSize = sizeChoice.GetSelectedId() }); size.AddChild(sizeChoice);
        Slider(body, "Subtitle background", Settings.Opacity, value => ApplySettings(Settings with { Opacity = value }), "VoiceSubtitleOpacity");
        Toggle(body, "Speaker labels", Settings.SpeakerLabels, value => ApplySettings(Settings with { SpeakerLabels = value }), "VoiceSpeakerLabels");
        Slider(body, "Communication filter", Settings.CommsIntensity, value => ApplySettings(Settings with { CommsIntensity = value }), "VoiceFilter");
        Toggle(body, "Important announcements only", Settings.ChatterLevel < .5f, value => ApplySettings(Settings with { ChatterLevel = value ? 0 : 1 }), "VoiceReducedChatter");
        Toggle(body, "Do not interrupt dialogue", Settings.NoInterruptions, value => ApplySettings(Settings with { NoInterruptions = value }), "VoiceNoInterruptions");
        var actions = VisualUi.Actions(body);
        var replay = VisualUi.Button("Replay last announcement", "Hear or read the last announcement again", ReplayLast); replay.Name = "VoiceReplay"; actions.AddChild(replay);
        var stop = VisualUi.Button("Stop", "Stop speech and discard queued lines", Stop); stop.Name = "VoiceStop"; actions.AddChild(stop);
        _labToggle = VisualUi.Button("Developer Voice Lab", "Preview voice profiles and processing", () => { _labForm.Visible = !_labForm.Visible; });
        _labToggle.Name = "VoiceLabOpen"; body.AddChild(_labToggle);
        _labForm = new VBoxContainer { Name = "VoiceLab", Visible = false }; _labForm.AddThemeConstantOverride("separation", 8); body.AddChild(_labForm);
        _labForm.AddChild(VisualUi.Text("VOICE LAB", 16, VisualUi.Gold));
        _labProfile = new OptionButton { Name = "VoiceLabProfile" }; _labForm.AddChild(_labProfile);
        _labEmotion = new OptionButton { Name = "VoiceLabEmotion" };
        foreach (var emotion in new[] { "calm", "curious", "urgent", "diplomatic", "confident" }) _labEmotion.AddItem(emotion);
        _labForm.AddChild(_labEmotion);
        _labText = new TextEdit { Name = "VoiceLabText", CustomMinimumSize = new(0, 92),
            WrapMode = TextEdit.LineWrappingMode.Boundary,
            Text = "Commander, FTL-01 is approaching Alpha Centauri at 0.42 c." };
        _labForm.AddChild(_labText);
        var play = VisualUi.Button("Synthesize and play", "Generate a local voice sample using the selected profile", PlayLabSample);
        play.Name = "VoiceLabPlay"; _labForm.AddChild(play);
        _labSubtitle = VisualUi.Text("", 16, Colors.White, true); _labForm.AddChild(_labSubtitle);
        _labDiagnostics = VisualUi.Text("", 12, VisualUi.Muted, true); _labForm.AddChild(_labDiagnostics);
        _labForm.AddChild(VisualUi.Text("Installed SAPI does not provide neural emotion or independent formant control. Profiles use cadence, pitch, EQ, harmonic doubling and short room effects. Voice identity fallback is reported above.", 11, VisualUi.Muted, true));
    }
    public void ShowVoiceSettings()
    {
        _voiceWindow.Show(); _labToggle.Visible = _main.UiIsDeveloperMode;
        if (!_main.UiIsDeveloperMode) _labForm.Hide();
        _labProfile.Clear();
        foreach (var profile in Profiles) _labProfile.AddItem(profile.DisplayName + " · " + profile.Presentation);
    }
    public void ShowVoiceLab()
    {
        if (!_main.UiIsDeveloperMode) return;
        ShowVoiceSettings(); _labForm.Show();
    }
    private void HideVoiceSettings() { _voiceWindow.Hide(); _labForm.Hide(); }
    private static string WrapDiagnostic(string value) => value
        .Replace("\\", "\\\u200b", StringComparison.Ordinal)
        .Replace("/", "/\u200b", StringComparison.Ordinal);
    private void PlayLabSample()
    {
        if (!_main.UiIsDeveloperMode || Profiles.Count == 0) return;
        var index = Math.Clamp(_labProfile.Selected, 0, Profiles.Count - 1);
        Stop(); _recent.Clear(); _engine?.ClearPending();
        Speak(new SpeechRequest(Profiles[index].Id, _labText.Text) {
            Priority = (int)VoicePriority.Cinematic, Category = "voice-lab", DedupeKey = "lab:" + _time,
            Emotion = _labEmotion.GetItemText(_labEmotion.Selected) });
    }
    public override void _Input(InputEvent @event)
    {
        if (_voiceWindow.Visible && @event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        { HideVoiceSettings(); GetViewport().SetInputAsHandled(); }
    }
    private void Toggle(Container body, string text, bool value, Action<bool> action, string name)
    {
        var control = new CheckButton { Name = name, Text = text, ButtonPressed = value, CustomMinimumSize = new(0, 30) };
        control.Toggled += selected => { if (!_updatingSettings) action(selected); }; body.AddChild(control);
    }
    private void Slider(Container body, string label, float value, Action<float> action, string name)
    {
        var row = new HBoxContainer(); body.AddChild(row);
        var text = VisualUi.Text(label, 13, VisualUi.Muted); text.CustomMinimumSize = new(210, 0); row.AddChild(text);
        var slider = new HSlider { Name = name, MinValue = 0, MaxValue = 100, Value = value * 100, Step = 1,
            CustomMinimumSize = new(0, 28), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        slider.ValueChanged += selected => { if (!_updatingSettings) action((float)selected / 100); }; row.AddChild(slider);
    }
}

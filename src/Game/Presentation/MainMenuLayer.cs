using System;
using Godot;
using Game.Simulation;

namespace Game.Presentation;

/// <summary>
/// Early-release startup menu. It pauses strategic simulation while visible and delegates
/// every game action back to Main's player-command surface.
/// </summary>
public partial class MainMenuLayer : CanvasLayer
{
    private Main _main = null!;
    private Control _overlay = null!;
    private ConfirmationDialog _confirmation = null!;
    private Button _continueDemo = null!;
    private Label _saveError = null!;
    private Action? _confirmedStart;
    private SimulationClock.SpeedLevel _resumeSpeed = SimulationClock.SpeedLevel.Normal;
    public bool IsBlockingGameplay => (_overlay?.IsVisibleInTree() ?? false) || (_confirmation?.Visible ?? false);

    public override void _Ready()
    {
        _main = GetParent() as Main
            ?? throw new InvalidOperationException("MainMenuLayer must be a child of Main.");

        _overlay = new ColorRect
        {
            Color = VisualPalette.Canvas,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var backdrop = new MainMenuBackdrop();
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _overlay.AddChild(backdrop);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _overlay.AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(430, 0),
        };
        center.AddChild(panel);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 12);
        panel.AddChild(content);

        var title = new Label
        {
            Text = "STELLAR CONTINUUM",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.AddThemeColorOverride("font_color", VisualPalette.TextPrimary);
        content.AddChild(title);

        var buildLabel = new Label
        {
            Text = _main.UiBuildLabel,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        buildLabel.AddThemeColorOverride("font_color", VisualPalette.TextSecondary);
        content.AddChild(buildLabel);

        content.AddChild(new HSeparator());

        AddButton(content, "Continue", "Continue the campaign currently loaded into memory.", ContinueCampaign);
        AddButton(content, "New Game — 2050", "Generate a normal campaign beginning January 1, 2050.", RequestNewCampaign);
        AddButton(content, "Play Demo — guided 24x opening", "Repeatable 2050 campaign with ordinary research, construction and ships. Separate demo save slot; accelerated time.", RequestDemo);
        _continueDemo = AddButton(content, "Continue Demo", "Resume your separate demo save at 24x.", ContinueDemo);
        _continueDemo.Disabled = !_main.UiHasDemoSave;
        AddButton(content, "Quit", "Autosave the current campaign and exit.", _main.UiQuit);

        var releaseNote = new Label
        {
            Text = "Early-release interface · more New Game options and settings are still in development.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        releaseNote.AddThemeColorOverride("font_color", VisualPalette.TextMuted);
        content.AddChild(releaseNote);
        _saveError = new Label
        {
            Visible = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        content.AddChild(_saveError);

        AddChild(_overlay);
        _confirmation = new ConfirmationDialog { Title = "Start a new campaign?" };
        _confirmation.Confirmed += ConfirmStart;
        _confirmation.Canceled += () => _confirmedStart = null;
        AddChild(_confirmation);

        // Child _Ready runs before the parent Main _Ready in Godot. The clock already exists,
        // so pausing here prevents time from advancing as soon as the first frame begins.
        _main.UiResumeAtSpeed(SimulationClock.SpeedLevel.Paused);
    }

    private void ContinueCampaign()
    {
        _overlay.Hide();
        _main.UiResumeAtSpeed(_resumeSpeed);
    }

    public void ShowMenu()
    {
        if (_overlay.IsVisibleInTree()) return;
        _resumeSpeed = _main.UiCurrentSpeed;
        _main.UiResumeAtSpeed(SimulationClock.SpeedLevel.Paused);
        _continueDemo.Disabled = !_main.UiHasDemoSave;
        _overlay.Show();
    }

    public void ShowSaveFailure(string message)
    {
        _saveError.Text = message;
        _saveError.Show();
    }

    public void ClearSaveFailure() => _saveError.Hide();

    public void RequestNewCampaign()
    {
        ShowMenu();
        _confirmedStart = _main.UiCreateNewCampaignConfirmed;
        _confirmation.DialogText = "Start a new normal campaign? The current campaign will be saved first. The previous normal autosave is retained as its backup. The separate demo save is unchanged.";
        _confirmation.PopupCentered(new Vector2I(480, 180));
    }

    private void RequestDemo()
    {
        ShowMenu();
        _confirmedStart = _main.UiPlayDemoConfirmed;
        _confirmation.DialogText = "Start a fresh guided demo? The current campaign will be saved first. Your normal autosave stays separate; any previous demo is retained as the demo backup. Ordinary research, resources and ship rules apply, with an optional 24x clock.";
        _confirmation.PopupCentered(new Vector2I(480, 200));
    }

    private void ConfirmStart()
    {
        var start = _confirmedStart;
        _confirmedStart = null;
        if (start is null || !_main.UiCheckpointBeforeCampaignSwitch()) return;
        start();
        _overlay.Hide();
    }

    private void ContinueDemo()
    {
        if (!_main.UiCheckpointBeforeCampaignSwitch()) return;
        _main.UiContinueDemo();
        _overlay.Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsBlockingGameplay || !@event.IsActionPressed("ui_cancel")) return;
        if (_confirmation.Visible)
        {
            _confirmation.Hide();
            _confirmedStart = null;
        }
        else ContinueCampaign();
        GetViewport().SetInputAsHandled();
    }

    private static Button AddButton(Container parent, string text, string tooltip, Action action)
    {
        var button = new Button
        {
            Text = text,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(390, 44),
            FocusMode = Control.FocusModeEnum.All,
        };
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }
}

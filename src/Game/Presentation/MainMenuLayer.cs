using System;
using Godot;

namespace Game.Presentation;

/// <summary>
/// Early-release startup menu. It pauses strategic simulation while visible and delegates
/// every game action back to Main's player-command surface.
/// </summary>
public partial class MainMenuLayer : CanvasLayer
{
    private Main _main = null!;
    private Control _overlay = null!;

    public override void _Ready()
    {
        _main = GetParent() as Main
            ?? throw new InvalidOperationException("MainMenuLayer must be a child of Main.");

        _overlay = new ColorRect
        {
            Color = new Color(0.02f, 0.03f, 0.06f, 0.94f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

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

        content.AddChild(new Label
        {
            Text = "STELLAR CONTINUUM",
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        content.AddChild(new Label
        {
            Text = _main.UiBuildLabel,
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        content.AddChild(new HSeparator());

        AddButton(content, "Continue", "Continue the campaign currently loaded into memory.", ContinueCampaign);
        AddButton(content, "New Game — 2050", "Generate a new campaign beginning January 1, 2050.", StartNewCampaign);
        AddButton(content, "Quit", "Autosave the current campaign and exit.", _main.UiQuit);

        content.AddChild(new Label
        {
            Text = "Early-release interface · more New Game options and settings are still in development.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        AddChild(_overlay);

        // Child _Ready runs before the parent Main _Ready in Godot. The clock already exists,
        // so pausing here prevents time from advancing as soon as the first frame begins.
        _main.UiSetPaused(true, announce: false);
    }

    private void ContinueCampaign()
    {
        _overlay.Hide();
        _main.UiSetPaused(false, announce: false);
    }

    private void StartNewCampaign()
    {
        _main.UiNewCampaign();
        _overlay.Hide();
        _main.UiSetPaused(false, announce: false);
    }

    private static void AddButton(Container parent, string text, string tooltip, Action action)
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
    }
}

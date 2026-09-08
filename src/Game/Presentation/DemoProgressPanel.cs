using Godot;

namespace Game.Presentation;

/// <summary>Optional demo guidance shares the existing scrolling sidebar.</summary>
public partial class DemoProgressPanel : CanvasLayer
{
    private Main _main = null!;
    private PanelContainer _panel = null!;
    private Label _objective = null!;
    private Label _research = null!;
    private Label _construction = null!;
    private double _refresh;

    public override void _Ready()
    {
        _main = (Main)GetParent();
        _panel = new PanelContainer { Name = "DemoProgress" };
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 6);
        _panel.AddChild(content);
        content.AddChild(new Label { Text = "PLAYABLE DEMO · separate save slot" });
        _objective = AddText(content);
        _research = AddText(content);
        _construction = AddText(content);
        var speed = new Button { Text = "Resume demo at 24x", TooltipText = "Accelerate the same simulation rules. Normal 1–4x controls remain available.", FocusMode = Control.FocusModeEnum.All };
        speed.Pressed += _main.UiResumeDemoSpeed;
        content.AddChild(speed);
        _main.GetNode<CampaignSidebar>("CampaignSidebar").AddPanel(_panel);
        Refresh();
    }

    public override void _Process(double delta)
    {
        _refresh += delta;
        if (_refresh < 0.25) return;
        _refresh = 0;
        Refresh();
    }

    private void Refresh()
    {
        _panel.Visible = _main.UiIsPlayableDemo;
        var state = _main.UiDemoObjective;
        if (state is null) return;
        _objective.Text = state.Objective;
        _research.Text = state.Research;
        _construction.Text = state.Construction;
    }

    private static Label AddText(Container parent)
    {
        var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        parent.AddChild(label);
        return label;
    }
}

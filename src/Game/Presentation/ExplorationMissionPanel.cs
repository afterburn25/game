using System;
using Godot;

namespace Game.Presentation;

/// <summary>
/// Compact observer-safe mission panel. It renders only Main's ExplorationReadModel-backed text
/// and never derives navigation/survey facts directly from authoritative hidden state.
/// </summary>
public partial class ExplorationMissionPanel : CanvasLayer
{
    private Main _main = null!;
    private Label _content = null!;
    private double _refreshTimer;

    public override void _Ready()
    {
        _main = GetParent() as Main
            ?? throw new InvalidOperationException("ExplorationMissionPanel must be a child of Main.");

        var panel = new PanelContainer
        {
            OffsetLeft = 16.0f,
            OffsetTop = 378.0f,
            OffsetRight = 700.0f,
            OffsetBottom = 680.0f,
        };

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 8);
        panel.AddChild(root);

        root.AddChild(new Label
        {
            Text = "EXPLORATION MISSIONS",
            TooltipText = "Mission phases and ETAs come from the observer-safe Exploration read model. Detailed science-survey time remains unknown until reconnaissance establishes system complexity.",
        });

        _content = new Label
        {
            Text = "Exploration missions are initializing…",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(650, 245),
            VerticalAlignment = VerticalAlignment.Top,
        };
        root.AddChild(_content);

        AddChild(panel);
    }

    public override void _Process(double delta)
    {
        _refreshTimer += delta;
        if (_main is null || _content is null || _refreshTimer < 0.5)
            return;

        _refreshTimer = 0.0;
        _content.Text = _main.UiExplorationMissionDetails;
    }
}

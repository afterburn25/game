using System;
using Godot;

namespace Game.Presentation;

/// <summary>
/// Compact observer-safe exploration/colonization panel. It renders only Main's already-filtered
/// presentation strings and never derives navigation, survey, Species suitability, or reach facts
/// directly from authoritative hidden state.
/// </summary>
public partial class ExplorationMissionPanel : CanvasLayer
{
    private Main _main = null!;
    private Label _content = null!;
    private double _refreshTimer;
    private bool _showColonySites;

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

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        root.AddChild(header);

        header.AddChild(new Label
        {
            Text = "EXPLORATION / COLONIZATION",
            TooltipText = "Mission phases and colony opportunities come from observer-safe simulation read models. The panel does not calculate survey, biological suitability, or operational reach itself.",
        });

        var missionsButton = new Button
        {
            Text = "Missions",
            TooltipText = "Show active scout, science, and colony mission phases and ETAs.",
            CustomMinimumSize = new Vector2(84, 28),
        };
        missionsButton.Pressed += () =>
        {
            _showColonySites = false;
            RefreshContent();
        };
        header.AddChild(missionsButton);

        var colonyButton = new Button
        {
            Text = "Colony Sites",
            TooltipText = "Show bounded fully surveyed settlement opportunities for populated player colony ships. Suitability and reach come from shared simulation contracts.",
            CustomMinimumSize = new Vector2(104, 28),
        };
        colonyButton.Pressed += () =>
        {
            _showColonySites = true;
            RefreshContent();
        };
        header.AddChild(colonyButton);

        _content = new Label
        {
            Text = "Exploration missions are initializing…",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(650, 235),
            VerticalAlignment = VerticalAlignment.Top,
        };
        root.AddChild(_content);

        AddChild(panel);
        RefreshContent();
    }

    public override void _Process(double delta)
    {
        _refreshTimer += delta;
        if (_main is null || _content is null || _refreshTimer < 0.5)
            return;

        _refreshTimer = 0.0;
        RefreshContent();
    }

    private void RefreshContent()
    {
        if (_main is null || _content is null)
            return;

        _content.Text = _showColonySites
            ? _main.UiColonyOpportunityDetails
            : _main.UiExplorationMissionDetails;
    }
}

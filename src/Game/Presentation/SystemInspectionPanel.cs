using System;
using Godot;

namespace Game.Presentation;

/// <summary>
/// Dedicated early-release system/colony inspection panel. It renders only the already-filtered
/// player-facing inspection view exposed by Main and never queries authoritative galaxy state.
/// </summary>
public partial class SystemInspectionPanel : CanvasLayer
{
    private Main _main = null!;
    private Label _content = null!;
    private int _lastSystemId = int.MinValue;
    private double _refreshTimer;

    public override void _Ready()
    {
        _main = GetParent() as Main
            ?? throw new InvalidOperationException("SystemInspectionPanel must be a child of Main.");

        var panel = new PanelContainer { Name = "SystemInspection" };

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 8);
        panel.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        root.AddChild(header);
        header.AddChild(new TextureRect
        {
            Texture = VisualIconLibrary.Info,
            CustomMinimumSize = new Vector2(22, 22),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        header.AddChild(new Label
        {
            Text = "KNOWN SYSTEM DATA",
            TooltipText = "Shows only information your civilization currently knows about the selected system.",
        });

        _content = new Label
        {
            Text = "Select a star system to inspect it.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(0, 120),
            VerticalAlignment = VerticalAlignment.Top,
        };
        root.AddChild(_content);

        _main.GetNode<CampaignSidebar>("CampaignSidebar").AddPanel(panel);
    }

    public override void _Process(double delta)
    {
        _refreshTimer += delta;
        if (_main is null || _content is null)
            return;

        var selectionChanged = _main.UiSelectedSystemId != _lastSystemId;
        if (!selectionChanged && _refreshTimer < 0.5)
            return;

        _lastSystemId = _main.UiSelectedSystemId;
        _refreshTimer = 0.0;
        _content.Text = _main.UiSelectedSystemInspection;
    }
}

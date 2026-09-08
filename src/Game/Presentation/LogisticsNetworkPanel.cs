using System;
using Godot;

namespace Game.Presentation;

/// <summary>
/// Read-only player panel for the reconstructible home-system logistics network.
/// It renders Main's filtered presentation string and never mutates or owns logistics state.
/// </summary>
public partial class LogisticsNetworkPanel : CanvasLayer
{
    private Main _main = null!;
    private Label _content = null!;
    private double _refreshTimer;

    public override void _Ready()
    {
        _main = GetParent() as Main
            ?? throw new InvalidOperationException("LogisticsNetworkPanel must be a child of Main.");

        var panel = new PanelContainer { Name = "LogisticsNetwork" };

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 8);
        panel.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        root.AddChild(header);
        header.AddChild(new TextureRect
        {
            Texture = VisualIconLibrary.Logistics,
            CustomMinimumSize = new Vector2(22, 22),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        header.AddChild(new Label
        {
            Text = "SUPPLY & INFRASTRUCTURE",
            TooltipText = "Derived from represented colonies, completed orbital infrastructure, local support and bounded logistics flow. This panel does not create or own simulation state.",
        });

        _content = new Label
        {
            Text = "Campaign logistics are initializing…",
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
        if (_main is null || _content is null || _refreshTimer < 0.5)
            return;

        _refreshTimer = 0.0;
        _content.Text = _main.UiHomeSystemLogisticsDetails;
    }
}

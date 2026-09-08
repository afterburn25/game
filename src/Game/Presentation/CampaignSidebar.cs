using Godot;

namespace Game.Presentation;

/// <summary>
/// Layout host for the existing command and exploration panels. Containers reserve each
/// panel's natural height; scrolling keeps longer status text and controls reachable.
/// </summary>
public partial class CampaignSidebar : CanvasLayer
{
    private const float LeftMargin = 16;
    private const float TopMargin = 206;
    private const float BottomMargin = 66;
    private const float PreferredWidth = 684;
    private const float RightPanelReservation = 406;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _panels = null!;

    public override void _Ready()
    {
        _scroll = new ScrollContainer
        {
            Name = "Scroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            FollowFocus = true,
        };
        AddChild(_scroll);

        _panels = new VBoxContainer
        {
            Name = "Panels",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _panels.AddThemeConstantOverride("separation", 16);
        _scroll.AddChild(_panels);

        GetViewport().SizeChanged += UpdateBounds;
        UpdateBounds();
    }

    public override void _ExitTree() => GetViewport().SizeChanged -= UpdateBounds;

    public void AddPanel(PanelContainer panel)
    {
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _panels.AddChild(panel);
    }

    private void UpdateBounds()
    {
        var viewport = GetViewport().GetVisibleRect().Size;
        _scroll.Position = new Vector2(LeftMargin, TopMargin);
        _scroll.Size = new Vector2(
            Mathf.Max(1, Mathf.Min(PreferredWidth, viewport.X - LeftMargin - RightPanelReservation)),
            Mathf.Max(1, viewport.Y - TopMargin - BottomMargin));
    }
}

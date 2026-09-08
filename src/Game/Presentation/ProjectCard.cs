using Godot;

namespace Game.Presentation;

/// <summary>A visual project summary driven only by a display snapshot.</summary>
public partial class ProjectCard : VBoxContainer
{
    private Label _title = null!;
    private Label _detail = null!;
    private Label _progressText = null!;
    private ProgressBar _progress = null!;

    public void Build(Texture2D icon, string category)
    {
        AddThemeConstantOverride("separation", 12);
        var emblem = new ProjectEmblem { Texture = icon, CustomMinimumSize = new Vector2(0, 164) };
        AddChild(emblem);
        AddChild(VisualUi.Text(category.ToUpperInvariant(), 11, VisualUi.Accent));
        _title = VisualUi.Text("Preparing…", 23, wrap: true);
        AddChild(_title);
        _progress = new ProgressBar { MinValue = 0, MaxValue = 100, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 7) };
        var trough = new StyleBoxFlat { BgColor = new Color("142b3c"), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        var fill = (StyleBoxFlat)trough.Duplicate();
        fill.BgColor = VisualUi.Accent;
        _progress.AddThemeStyleboxOverride("background", trough);
        _progress.AddThemeStyleboxOverride("fill", fill);
        AddChild(_progress);
        _progressText = VisualUi.Text("", 12, VisualUi.Accent);
        AddChild(_progressText);
        _detail = VisualUi.Text("", 14, VisualUi.Muted, wrap: true);
        AddChild(_detail);
    }

    public void UpdateDisplay(string title, string detail, double progress, bool active)
    {
        _title.Text = title;
        _detail.Text = detail;
        _progress.Value = Mathf.Clamp(progress, 0, 1) * 100;
        _progressText.Text = active ? $"{_progress.Value:0}% COMPLETE" : "READY FOR YOUR COMMAND";
    }
}

/// <summary>Vector blueprint motif with a crisp SVG at its center.</summary>
public partial class ProjectEmblem : Control
{
    public Texture2D? Texture { get; set; }
    public ProjectEmblem() => MouseFilter = MouseFilterEnum.Ignore;
    public override void _Draw()
    {
        var center = Size / 2;
        var radius = Mathf.Min(Size.X * 0.32f, 68);
        DrawCircle(center, radius, new Color(0.045f, 0.095f, 0.14f, 0.9f));
        DrawArc(center, radius, 0, Mathf.Tau, 96, new Color("294e63"), 1, true);
        DrawArc(center, radius - 9, -Mathf.Pi * 0.55f, Mathf.Pi * 0.12f, 48, VisualUi.Accent, 2, true);
        DrawArc(center, radius - 9, Mathf.Pi * 0.45f, Mathf.Pi * 1.12f, 48, new Color("496f83"), 1, true);
        DrawLine(center + new Vector2(-radius - 22, 0), center + new Vector2(-radius + 7, 0), new Color("456678"), 1, true);
        DrawLine(center + new Vector2(radius - 7, 0), center + new Vector2(radius + 22, 0), new Color("456678"), 1, true);
        if (Texture is not null)
            DrawTextureRect(Texture, new Rect2(center - new Vector2(37, 37), new Vector2(74, 74)), false, Colors.White);
    }
    public override void _Notification(int what)
    {
        if (what == NotificationResized) QueueRedraw();
    }
}

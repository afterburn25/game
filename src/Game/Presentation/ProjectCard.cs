using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game.Presentation;

/// <summary>A visual project summary driven only by a display snapshot.</summary>
public partial class ProjectCard : VBoxContainer
{
    private Label _title = null!;
    private Label _detail = null!;
    private Label _progressText = null!;
    private ProgressBar _progress = null!;
    private string _costUnit = "";
    private VBoxContainer _choices = null!;
    private string _choiceSignature = "";

    public void Build(Texture2D icon, string category)
    {
        _costUnit = category == "RESEARCH" ? "SCIENCE" : "MATERIALS";
        var isResearch = category == "RESEARCH";
        var isShipyard = category == "SHIPYARD";
        AddThemeConstantOverride("separation", 8);
        var heading = new HBoxContainer { Name = "ProjectHeading" };
        heading.AddThemeConstantOverride("separation", 10);
        heading.AddChild(VisualUi.Icon(icon, 34));
        var headingCopy = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        headingCopy.AddThemeConstantOverride("separation", 0);
        headingCopy.AddChild(VisualUi.Text(category.ToUpperInvariant(), 10, VisualUi.Accent));
        _title = VisualUi.Text("Preparing…", 20, VisualUi.PrimaryText, wrap: true);
        headingCopy.AddChild(_title);
        heading.AddChild(headingCopy);
        AddChild(heading);
        _progress = new ProgressBar { MinValue = 0, MaxValue = 100, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 7) };
        var trough = new StyleBoxFlat { BgColor = new Color("142b3c"), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        var fill = (StyleBoxFlat)trough.Duplicate();
        fill.BgColor = VisualUi.Accent;
        _progress.AddThemeStyleboxOverride("background", trough);
        _progress.AddThemeStyleboxOverride("fill", fill);
        AddChild(_progress);
        _progressText = VisualUi.Text("", 12, VisualUi.Accent);
        AddChild(_progressText);
        _detail = VisualUi.Text("", 13, VisualUi.Muted, wrap: true);
        AddChild(_detail);
        _choices = new VBoxContainer { Name = "DirectChoices" };
        _choices.AddThemeConstantOverride("separation", 6);
        AddChild(_choices);
    }

    public void UpdateDisplay(UiProjectCard project)
    {
        _title.Text = project.Title;
        _detail.Text = project.Detail + (project.TimeRemaining is null ? "" : "\n" + project.TimeRemaining);
        _progress.Value = Mathf.Clamp(project.Progress, 0, 1) * 100;
        _progress.Visible = project.IsActive;
        _progressText.Text = project.IsActive
            ? _costUnit == "SCIENCE"
                ? $"{_progress.Value:0}% THROUGH CURRENT STAGE"
                : $"{_progress.Value:0}% COMPLETE · {project.Current:N0} / {project.Cost:N0}"
            : project.Cost > 0
                ? _costUnit == "SCIENCE" ? $"RECOMMENDED LABS {project.Cost:N0}" : $"TOTAL COST {project.Cost:N0} {_costUnit}"
                : "NO AVAILABLE PROJECT";
    }

    public void UpdateChoices(IReadOnlyList<UiOperationChoice> choices, Action<string> select)
    {
        var signature = string.Join("|", choices.Select(choice => $"{choice.Id}:{choice.CanAfford}:{choice.ArtworkPath}"));
        if (signature == _choiceSignature) return;
        _choiceSignature = signature;
        foreach (var child in _choices.GetChildren()) child.QueueFree();
        if (choices.Count == 0) return;
        _choices.AddChild(VisualUi.Text("AVAILABLE OPTIONS", 11, VisualUi.Accent));
        var grid = new ResponsiveGrid { Name = "OperationChoices", Columns = 2, ReferenceColumns = 3, CompactColumns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 8);
        _choices.AddChild(grid);
        foreach (var choice in choices)
        {
            var availability = choice.CanAfford ? "AVAILABLE" : "INSUFFICIENT FUNDS";
            var button = new Button
            {
                TooltipText = $"{availability}\n{choice.Detail}",
                // Two-column 720p layout still has room for a wrapped title, cost, detail,
                // and action line; the art is cropped, never the command text.
                CustomMinimumSize = new Vector2(220, choice.ArtworkPath is null ? 116 : 190),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Disabled = !choice.CanAfford,
                FocusMode = FocusModeEnum.All,
            };
            AudioDirector.Bind(button);
            button.Name = "Choose" + choice.Id;
            if (choice.CanAfford) button.Pressed += () => select(choice.Id);
            VisualUi.ApplyInteractiveStates(button, choice.CanAfford ? VisualUi.Gold : VisualPalette.Disabled);

            if (choice.ArtworkPath is not null)
            {
                var artwork = new TextureRect
                {
                    Name = "Artwork_" + choice.Id,
                    Texture = VisualIconLibrary.Get(choice.ArtworkPath),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                artwork.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                artwork.OffsetLeft = 2; artwork.OffsetRight = -2; artwork.OffsetTop = 2; artwork.OffsetBottom = -2;
                button.AddChild(artwork);
                var veil = new ColorRect
                {
                    Color = new Color(0.006f, 0.016f, 0.027f, .48f),
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                veil.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                button.AddChild(veil);
            }

            var body = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            body.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            body.OffsetLeft = 9; body.OffsetRight = -9;
            body.OffsetTop = choice.ArtworkPath is null ? 7 : 72;
            body.OffsetBottom = -7;
            body.AddThemeConstantOverride("separation", 3);
            button.AddChild(body);
            var title = VisualUi.Text(choice.Title, 14,
                choice.CanAfford ? VisualUi.PrimaryText : VisualPalette.TextSecondary, wrap: true);
            title.MaxLinesVisible = 2;
            title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            body.AddChild(title);
            var cost = new PanelContainer { Name = "Cost_" + choice.Id, MouseFilter = MouseFilterEnum.Ignore };
            var costSurface = VisualUi.Surface(margin: 4);
            costSurface.BgColor = VisualPalette.SurfacePrimary;
            costSurface.BorderColor = choice.CanAfford ? VisualUi.Gold : VisualPalette.Disabled;
            cost.AddThemeStyleboxOverride("panel", costSurface);
            cost.AddChild(VisualUi.Text("COST  " + choice.CostLabel.ToUpperInvariant(), 10,
                choice.CanAfford ? VisualUi.Gold : VisualUi.Muted, wrap: true));
            body.AddChild(cost);
            var detail = VisualUi.Text(choice.Detail, 10,
                choice.CanAfford ? VisualUi.Muted : VisualPalette.TextSecondary, wrap: true);
            detail.MaxLinesVisible = 2;
            detail.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            body.AddChild(detail);
            body.AddChild(VisualUi.Text(choice.CanAfford ? "AUTHORIZE  →" : "UNAVAILABLE · INSUFFICIENT FUNDS", 9,
                choice.CanAfford ? VisualUi.Accent : VisualPalette.Danger));
            grid.AddChild(button);
        }
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
        var radius = Mathf.Min(Size.X * 0.32f, Mathf.Min(Size.Y * 0.42f, 68));
        DrawCircle(center, radius, new Color(0.045f, 0.095f, 0.14f, 0.9f));
        DrawArc(center, radius, 0, Mathf.Tau, 96, new Color("294e63"), 1, true);
        DrawArc(center, radius - 9, -Mathf.Pi * 0.55f, Mathf.Pi * 0.12f, 48, VisualUi.Accent, 2, true);
        DrawArc(center, radius - 9, Mathf.Pi * 0.45f, Mathf.Pi * 1.12f, 48, new Color("496f83"), 1, true);
        DrawLine(center + new Vector2(-radius - 22, 0), center + new Vector2(-radius + 7, 0), new Color("456678"), 1, true);
        DrawLine(center + new Vector2(radius - 7, 0), center + new Vector2(radius + 22, 0), new Color("456678"), 1, true);
        if (Texture is not null)
            DrawTextureRect(Texture, new Rect2(center - new Vector2(21, 21), new Vector2(42, 42)), false, Colors.White);
    }
    public override void _Notification(int what)
    {
        if (what == NotificationResized) QueueRedraw();
    }
}

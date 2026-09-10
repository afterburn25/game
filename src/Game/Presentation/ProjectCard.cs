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
        AddThemeConstantOverride("separation", isResearch || isShipyard ? 8 : 12);
        var emblemHeight = isResearch ? 54 : isShipyard ? 70 : 120;
        var emblem = new ProjectEmblem { Texture = icon, CustomMinimumSize = new Vector2(0, emblemHeight) };
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
        _choices = new VBoxContainer { Name = "DirectChoices" };
        _choices.AddThemeConstantOverride("separation", 6);
        AddChild(_choices);
    }

    public void UpdateDisplay(UiProjectCard project)
    {
        _title.Text = project.Title;
        _detail.Text = project.Detail;
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
        var grid = new GridContainer { Name = "OperationChoices", Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 8);
        _choices.AddChild(grid);
        foreach (var choice in choices)
        {
            var availability = choice.CanAfford ? "AVAILABLE" : "INSUFFICIENT FUNDS";
            var button = new Button
            {
                TooltipText = $"{availability}\n{choice.Detail}",
                CustomMinimumSize = new Vector2(220, choice.ArtworkPath is null ? 108 : 196),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Disabled = !choice.CanAfford,
                FocusMode = FocusModeEnum.All,
            };
            AudioDirector.Bind(button);
            button.Name = "Choose" + choice.Id;
            if (choice.CanAfford) button.Pressed += () => select(choice.Id);
            var surface = VisualUi.Surface(highlighted: choice.CanAfford, margin: 9);
            surface.BgColor = new Color(0.030f, 0.061f, 0.086f, 0.98f);
            button.AddThemeStyleboxOverride("normal", surface);
            button.AddThemeStyleboxOverride("disabled", surface);
            var hover = (StyleBoxFlat)surface.Duplicate();
            hover.BorderColor = VisualUi.Gold;
            button.AddThemeStyleboxOverride("hover", hover);
            button.Modulate = choice.CanAfford ? Colors.White : new Color("70818d");

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
            body.OffsetTop = choice.ArtworkPath is null ? 7 : 103;
            body.OffsetBottom = -7;
            body.AddThemeConstantOverride("separation", 3);
            button.AddChild(body);
            var title = VisualUi.Text(choice.Title, 14, Colors.White, wrap: true);
            title.MaxLinesVisible = 2;
            title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            body.AddChild(title);
            body.AddChild(VisualUi.Text(choice.CostLabel.ToUpperInvariant(), 10, VisualUi.Gold, wrap: true));
            var detail = VisualUi.Text(choice.Detail, 10, VisualUi.Muted, wrap: true);
            detail.MaxLinesVisible = 2;
            detail.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            body.AddChild(detail);
            body.AddChild(VisualUi.Text(choice.CanAfford ? "AUTHORIZE  →" : "INSUFFICIENT FUNDS", 9,
                choice.CanAfford ? VisualUi.Accent : new Color("ee9a91")));
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
            DrawTextureRect(Texture, new Rect2(center - new Vector2(37, 37), new Vector2(74, 74)), false, Colors.White);
    }
    public override void _Notification(int what)
    {
        if (what == NotificationResized) QueueRedraw();
    }
}

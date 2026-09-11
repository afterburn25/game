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
    private ResponsiveGrid? _choiceGrid;
    private readonly Dictionary<string, ChoiceControls> _choiceControls = new(StringComparer.Ordinal);

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

    public void UpdateChoices(IReadOnlyList<UiOperationChoice> choices, Action<string> select, Action<string>? cancel = null)
    {
        EnsureChoiceGrid();
        var focusName = GetViewport().GuiGetFocusOwner()?.Name;
        var scroll = FindScrollAncestor();
        var scrollPosition = scroll?.ScrollVertical ?? 0;
        var currentKeys = choices.Select(ChoiceKey).ToHashSet(StringComparer.Ordinal);
        foreach (var (key, controls) in _choiceControls.Where(pair => !currentKeys.Contains(pair.Key)).ToArray())
        {
            _choiceControls.Remove(key);
            _choiceGrid!.RemoveChild(controls.Button);
            controls.Button.QueueFree();
        }

        for (var index = 0; index < choices.Count; index++)
        {
            var choice = choices[index];
            var key = ChoiceKey(choice);
            if (_choiceControls.TryGetValue(key, out var controls) &&
                !string.Equals(controls.ArtworkPath, choice.ArtworkPath, StringComparison.Ordinal))
            {
                _choiceGrid!.RemoveChild(controls.Button);
                controls.Button.QueueFree();
                _choiceControls.Remove(key);
                controls = null!;
            }
            if (controls is null)
            {
                controls = BuildChoice(choice, select, cancel);
                _choiceControls.Add(key, controls);
                _choiceGrid!.AddChild(controls.Button);
            }
            RefreshChoice(controls, choice, select, cancel);
            _choiceGrid!.MoveChild(controls.Button, index);
        }

        if (!string.IsNullOrEmpty(focusName) && FindChild(focusName, true, false) is Control focus)
            focus.CallDeferred(Control.MethodName.GrabFocus);
        if (scroll is not null)
            scroll.SetDeferred(ScrollContainer.PropertyName.ScrollVertical, scrollPosition);
    }

    private void EnsureChoiceGrid()
    {
        if (_choiceGrid is not null) return;
        _choices.AddChild(VisualUi.Text("PROJECTS", 11, VisualUi.Accent));
        _choiceGrid = new ResponsiveGrid { Name = "OperationChoices", Columns = 2, ReferenceColumns = 3, CompactColumns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _choiceGrid.AddThemeConstantOverride("h_separation", 8);
        _choiceGrid.AddThemeConstantOverride("v_separation", 8);
        _choices.AddChild(_choiceGrid);
    }

    private ChoiceControls BuildChoice(UiOperationChoice choice, Action<string> select, Action<string>? cancel)
    {
        var button = new Button
        {
            Name = ChoiceKey(choice),
            // Two-column 720p layout still has room for a wrapped title, cost, detail,
            // and action line; the art is cropped, never the command text.
            CustomMinimumSize = new Vector2(220, choice.ArtworkPath is null ? 116 : 190),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            FocusMode = FocusModeEnum.All,
        };
        AudioDirector.Bind(button);
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
            var veil = new ColorRect { Color = new Color(0.006f, 0.016f, 0.027f, .48f), MouseFilter = MouseFilterEnum.Ignore };
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
        var title = VisualUi.Text("", 14, wrap: true);
        title.Name = "ChoiceTitle"; title.MaxLinesVisible = 2;
        title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        body.AddChild(title);
        var cost = new PanelContainer { Name = "Cost_" + choice.Id, MouseFilter = MouseFilterEnum.Ignore };
        var costText = VisualUi.Text("", 10, wrap: true);
        costText.Name = "ChoiceCost"; cost.AddChild(costText); body.AddChild(cost);
        var detail = VisualUi.Text("", 10, wrap: true);
        detail.Name = "ChoiceDetail"; detail.MaxLinesVisible = 2;
        detail.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        body.AddChild(detail);
        var action = VisualUi.Text("", 9);
        action.Name = "ChoiceAction"; body.AddChild(action);

        var controls = new ChoiceControls(button, title, cost, costText, detail, action, choice.ArtworkPath, choice, select, cancel);
        button.Pressed += controls.Invoke;
        return controls;
    }

    private static void RefreshChoice(ChoiceControls controls, UiOperationChoice choice, Action<string> select, Action<string>? cancel)
    {
        controls.Choice = choice;
        controls.Select = select;
        controls.Cancel = cancel;
        controls.Button.Disabled = !choice.CanAfford;
        controls.Button.TooltipText = $"{(choice.CanAfford ? "AVAILABLE" : "UNAVAILABLE")}\n{choice.CostLabel}\n{choice.Detail}";
        VisualUi.ApplyInteractiveStates(controls.Button, choice.CanAfford ? VisualUi.Gold : VisualPalette.Disabled);
        controls.Title.Text = choice.Title;
        controls.Title.Modulate = choice.CanAfford ? VisualUi.PrimaryText : VisualPalette.TextSecondary;
        var costSurface = VisualUi.Surface(margin: 4);
        costSurface.BgColor = VisualPalette.SurfacePrimary;
        costSurface.BorderColor = choice.CanAfford ? VisualUi.Gold : VisualPalette.Disabled;
        controls.Cost.AddThemeStyleboxOverride("panel", costSurface);
        controls.CostText.Text = (choice.IsCancellation ? "" : "COST  ") + choice.CostLabel.ToUpperInvariant();
        controls.CostText.Modulate = choice.CanAfford ? VisualUi.Gold : VisualUi.Muted;
        controls.Detail.Text = choice.Detail;
        controls.Detail.Modulate = choice.CanAfford ? VisualUi.Muted : VisualPalette.TextSecondary;
        controls.Action.Text = choice.CanAfford ? choice.IsCancellation ? "CANCEL / REFUND  →" : "AUTHORIZE / QUEUE  →" : "UNAVAILABLE";
        controls.Action.Modulate = choice.CanAfford ? VisualUi.Accent : VisualPalette.Danger;
    }

    private static string ChoiceKey(UiOperationChoice choice) => choice.IsCancellation
        ? (choice.CancellationNodePrefix ?? "CancelConstruction_") + choice.Id
        : "Choose" + choice.Id;

    private ScrollContainer? FindScrollAncestor()
    {
        for (Node? current = GetParent(); current is not null; current = current.GetParent())
            if (current is ScrollContainer scroll) return scroll;
        return null;
    }

    private sealed class ChoiceControls
    {
        public ChoiceControls(Button button, Label title, PanelContainer cost, Label costText, Label detail,
            Label action, string? artworkPath, UiOperationChoice choice, Action<string> select, Action<string>? cancel)
        {
            Button = button; Title = title; Cost = cost; CostText = costText; Detail = detail; Action = action;
            ArtworkPath = artworkPath; Choice = choice; Select = select; Cancel = cancel;
        }

        public Button Button { get; }
        public Label Title { get; }
        public PanelContainer Cost { get; }
        public Label CostText { get; }
        public Label Detail { get; }
        public Label Action { get; }
        public string? ArtworkPath { get; }
        public UiOperationChoice Choice { get; set; }
        public Action<string> Select { get; set; }
        public Action<string>? Cancel { get; set; }

        public void Invoke()
        {
            if (!Choice.CanAfford) return;
            if (Choice.IsCancellation) Cancel?.Invoke(Choice.Id); else Select(Choice.Id);
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

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game.Presentation;

/// <summary>Renders only the civilization's legitimate research horizon. Unknown technology
/// never enters this presentation model, so the view can later consume Adaptive Research nodes.</summary>
public partial class ResearchHorizonView : VBoxContainer
{
    private readonly Dictionary<string, ProgressBar> _progress = new(StringComparer.Ordinal);
    private string _signature = string.Empty;

    public ResearchHorizonView()
    {
        AddThemeConstantOverride("separation", 8);
    }

    public void UpdateNodes(IReadOnlyList<UiResearchHorizonNode> nodes, Action<string> start)
    {
        var signature = string.Join('|', nodes.Select(node => $"{node.Id}:{node.State}:{node.CanStart}"));
        if (signature != _signature)
        {
            _signature = signature;
            _progress.Clear();
            foreach (var child in GetChildren()) child.QueueFree();
            AddChild(VisualUi.Text("VISIBLE RESEARCH HORIZON", 11, VisualUi.Accent));
            AddChild(VisualUi.Text("Established knowledge and possibilities your scientists can investigate now.",
                11, VisualUi.Muted, wrap: true));
            var flow = new GridContainer
            {
                Name = "ResearchNodes", Columns = 2,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            flow.AddThemeConstantOverride("h_separation", 10);
            flow.AddThemeConstantOverride("v_separation", 10);
            AddChild(flow);
            foreach (var node in nodes)
                flow.AddChild(BuildNode(node, start));
        }

        foreach (var node in nodes)
            if (_progress.TryGetValue(node.Id, out var bar))
                bar.Value = Math.Clamp(node.Progress, 0, 1) * 100;
    }

    private Control BuildNode(UiResearchHorizonNode node, Action<string> start)
    {
        var button = new Button
        {
            Name = "ResearchNode_" + node.Id,
            CustomMinimumSize = new Vector2(340, 96),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Disabled = !node.CanStart,
            TooltipText = node.CanStart ? $"Start {node.Title}.\n{node.Detail}" : node.Detail,
            FocusMode = FocusModeEnum.All,
        };
        if (node.CanStart) button.Pressed += () => start(node.Id);
        var surface = VisualUi.Surface(highlighted: node.CanStart, margin: 10);
        surface.BgColor = node.State switch
        {
            "MATURE" => new Color(0.025f, 0.105f, 0.085f, 0.98f),
            "ACTIVE PROGRAM" => new Color(0.025f, 0.095f, 0.125f, 0.98f),
            _ => new Color(0.035f, 0.060f, 0.085f, 0.98f),
        };
        button.AddThemeStyleboxOverride("normal", surface);
        button.AddThemeStyleboxOverride("disabled", surface);
        var hover = (StyleBoxFlat)surface.Duplicate();
        hover.BorderColor = VisualUi.Gold;
        hover.BorderWidthLeft = hover.BorderWidthTop = hover.BorderWidthRight = hover.BorderWidthBottom = 2;
        button.AddThemeStyleboxOverride("hover", hover);
        button.Modulate = node.State switch
        {
            "MATURE" => new Color("8fd7b0"),
            "ACTIVE PROGRAM" => VisualUi.Accent,
            _ => Colors.White,
        };

        var body = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        body.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        body.OffsetLeft = 9; body.OffsetRight = -9; body.OffsetTop = 5; body.OffsetBottom = -5;
        body.AddThemeConstantOverride("separation", 2);
        button.AddChild(body);
        var header = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        header.AddChild(VisualUi.Icon(VisualIconLibrary.Research, 20));
        var title = VisualUi.Text(node.Title, 15, Colors.White, wrap: true);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        var stateColor = node.State == "MATURE" ? new Color("8fd7b0") :
            node.State == "ACTIVE PROGRAM" ? VisualUi.Accent : VisualUi.Gold;
        header.AddChild(VisualUi.Text(node.State, 9, stateColor));
        body.AddChild(header);
        var detail = VisualUi.Text(node.Detail, 11, VisualUi.Muted, wrap: true);
        detail.MaxLinesVisible = 2;
        detail.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        body.AddChild(detail);
        var progress = new ProgressBar { MinValue = 0, MaxValue = 100, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 5) };
        progress.Value = node.Progress * 100;
        progress.Visible = node.State is "MATURE" or "ACTIVE PROGRAM";
        body.AddChild(progress);
        if (node.CanStart)
            body.AddChild(VisualUi.Text("BEGIN RESEARCH  →", 10, VisualUi.Gold));
        _progress[node.Id] = progress;
        return button;
    }
}

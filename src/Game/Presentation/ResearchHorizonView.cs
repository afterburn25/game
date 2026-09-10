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

    public void UpdateNodes(
        IReadOnlyList<UiResearchHorizonNode> nodes,
        Action<string> start,
        Action<string> pause,
        Action<string> resume)
    {
        var signature = string.Join('|', nodes.Select(node =>
            $"{node.Id}:{node.State}:{node.CanStart}:{node.Detail}"));
        if (signature != _signature)
        {
            _signature = signature;
            _progress.Clear();
            foreach (var child in GetChildren()) child.QueueFree();
            AddChild(VisualUi.Text("VISIBLE RESEARCH HORIZON", 11, VisualUi.Accent));
            AddChild(VisualUi.Text("Established knowledge and possibilities your scientists can investigate now.",
                11, VisualUi.Muted, wrap: true));
            var summary = new HBoxContainer { Name = "ResearchSummary" };
            summary.AddThemeConstantOverride("separation", 8);
            summary.AddChild(StatusChip($"{nodes.Count(node => node.State == "ACTIVE PROGRAM")} ACTIVE", VisualUi.Accent));
            summary.AddChild(StatusChip($"{nodes.Count(node => node.CanStart)} AVAILABLE", VisualUi.Gold));
            summary.AddChild(StatusChip($"{nodes.Count(node => node.State == "MATURE")} MATURE", new Color("8fd7b0")));
            AddChild(summary);
            var flow = new GridContainer
            {
                Name = "ResearchNodes", Columns = 2,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            flow.AddThemeConstantOverride("h_separation", 10);
            flow.AddThemeConstantOverride("v_separation", 10);
            AddChild(flow);
            foreach (var node in nodes)
                flow.AddChild(BuildNode(node, start, pause, resume));
        }

        foreach (var node in nodes)
            if (_progress.TryGetValue(node.Id, out var bar))
                bar.Value = Math.Clamp(node.Progress, 0, 1) * 100;
    }

    private Control BuildNode(
        UiResearchHorizonNode node,
        Action<string> start,
        Action<string> pause,
        Action<string> resume)
    {
        var hasAction = node.CanStart || node.CanPause || node.CanResume;
        var button = new Button
        {
            Name = "ResearchNode_" + node.Id,
            CustomMinimumSize = new Vector2(340, 112),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Disabled = !hasAction,
            TooltipText = node.CanStart ? $"Start {node.Title}.\n{node.Detail}" :
                node.CanPause ? $"Pause {node.Title} and stop its operating cost.\n{node.Detail}" :
                node.CanResume ? $"Resume {node.Title}.\n{node.Detail}" : node.Detail,
            FocusMode = FocusModeEnum.All,
        };
        if (node.CanStart) button.Pressed += () => start(node.Id);
        else if (node.CanPause) button.Pressed += () => pause(node.Id);
        else if (node.CanResume) button.Pressed += () => resume(node.Id);
        var surface = VisualUi.Surface(highlighted: hasAction, margin: 10);
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

        var body = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        body.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        body.OffsetLeft = 9; body.OffsetRight = -9; body.OffsetTop = 5; body.OffsetBottom = -5;
        body.AddThemeConstantOverride("separation", 9);
        button.AddChild(body);
        body.AddChild(new ResearchNodeSigil(node.Id, node.Detail, stateColor: node.State == "MATURE"
            ? new Color("8fd7b0") : node.State == "ACTIVE PROGRAM" ? VisualUi.Accent : VisualUi.Gold));
        var copy = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        copy.AddThemeConstantOverride("separation", 2);
        body.AddChild(copy);
        var header = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        header.AddChild(VisualUi.Icon(VisualIconLibrary.Research, 20));
        var title = VisualUi.Text(node.Title, 15, Colors.White, wrap: true);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        var stateColor = node.State == "MATURE" ? new Color("8fd7b0") :
            node.State == "ACTIVE PROGRAM" ? VisualUi.Accent : VisualUi.Gold;
        header.AddChild(VisualUi.Text(node.State, 9, stateColor));
        copy.AddChild(header);
        var detail = VisualUi.Text(node.Detail, 11, VisualUi.Muted, wrap: true);
        detail.MaxLinesVisible = 2;
        detail.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        copy.AddChild(detail);
        var progress = new ProgressBar { MinValue = 0, MaxValue = 100, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 5) };
        progress.Value = node.Progress * 100;
        progress.Visible = node.State is "MATURE" or "ACTIVE PROGRAM";
        copy.AddChild(progress);
        if (node.CanStart)
            copy.AddChild(VisualUi.Text("BEGIN RESEARCH  →", 10, VisualUi.Gold));
        else if (node.CanPause)
            copy.AddChild(VisualUi.Text("PAUSE PROGRAM", 10, VisualUi.Gold));
        else if (node.CanResume)
            copy.AddChild(VisualUi.Text("RESUME PROGRAM  →", 10, VisualUi.Gold));
        _progress[node.Id] = progress;
        return button;
    }

    private static PanelContainer StatusChip(string text, Color color)
    {
        var chip = new PanelContainer();
        var surface = VisualUi.Surface(margin: 7);
        surface.BgColor = new Color(color.R * .08f, color.G * .08f, color.B * .08f, .94f);
        surface.BorderColor = new Color(color.R, color.G, color.B, .38f);
        chip.AddThemeStyleboxOverride("panel", surface);
        chip.AddChild(VisualUi.Text(text, 10, color));
        return chip;
    }
}

/// <summary>A deterministic vector emblem for a visible research node. It gives each program
/// a recognizable map-like identity without importing art or exposing hidden graph data.</summary>
public partial class ResearchNodeSigil : Control
{
    private readonly int _seed;
    private readonly Color _color;

    public ResearchNodeSigil(string id, string detail, Color stateColor)
    {
        Name = "ResearchSigil_" + id;
        CustomMinimumSize = new Vector2(66, 82);
        MouseFilter = MouseFilterEnum.Ignore;
        _seed = id.Aggregate(17, (value, character) => unchecked(value * 31 + character));
        var domain = detail.Split('·', 2)[0].Trim();
        var domainColor = domain switch
        {
            "Computing" => new Color("8d8cff"),
            "Sensors Comms" => new Color("52d7ea"),
            "Space Industry" => new Color("e2aa58"),
            "Life Medicine" => new Color("73d69e"),
            _ => new Color("b9a6ff"),
        };
        _color = domainColor.Lerp(stateColor, .28f);
    }

    public override void _Draw()
    {
        var center = Size * .5f;
        DrawCircle(center, 28, new Color(_color.R, _color.G, _color.B, .07f));
        DrawCircle(center, 22, new Color(.008f, .025f, .04f, .9f));
        DrawArc(center, 22, -.9f, 4.7f, 40, new Color(_color.R, _color.G, _color.B, .72f), 1.4f, true);
        DrawArc(center, 15, .8f, 5.7f, 32, new Color(_color.R, _color.G, _color.B, .35f), 1, true);
        var spokes = 3 + Math.Abs(_seed % 3);
        for (var index = 0; index < spokes; index++)
        {
            var angle = (_seed % 29) * .07f + index * MathF.Tau / spokes;
            var inner = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 5;
            var outer = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (13 + index % 2 * 5);
            DrawLine(inner, outer, new Color(_color.R, _color.G, _color.B, .5f), 1, true);
            DrawCircle(outer, 2.2f, _color);
        }
        DrawCircle(center, 5.2f, _color);
        DrawCircle(center, 1.8f, Colors.White);
    }
}

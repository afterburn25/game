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
            AddChild(VisualUi.Text("Completed knowledge, active work and possibilities your scientists can investigate now. Unknown possibilities remain hidden.",
                12, VisualUi.Muted, wrap: true));
            var flow = new HFlowContainer { Name = "ResearchNodes" };
            flow.AddThemeConstantOverride("h_separation", 8);
            flow.AddThemeConstantOverride("v_separation", 8);
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
            CustomMinimumSize = new Vector2(245, 112),
            Disabled = !node.CanStart,
            TooltipText = node.CanStart ? $"Start {node.Title}.\n{node.Detail}" : node.Detail,
            FocusMode = FocusModeEnum.All,
        };
        if (node.CanStart) button.Pressed += () => start(node.Id);
        button.AddThemeStyleboxOverride("normal", VisualUi.Surface(margin: 10));
        button.AddThemeStyleboxOverride("disabled", VisualUi.Surface(margin: 10));
        button.Modulate = node.State switch
        {
            "MATURE" => new Color("8fd7b0"),
            "ACTIVE PROGRAM" => VisualUi.Accent,
            _ => Colors.White,
        };

        var body = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        body.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        body.OffsetLeft = 11; body.OffsetRight = -11; body.OffsetTop = 9; body.OffsetBottom = -9;
        body.AddThemeConstantOverride("separation", 5);
        button.AddChild(body);
        var header = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        header.AddChild(VisualUi.Icon(VisualIconLibrary.Research, 28));
        var title = VisualUi.Text(node.Title, 14, Colors.White, wrap: true);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        body.AddChild(header);
        body.AddChild(VisualUi.Text(node.State, 10,
            node.State == "MATURE" ? new Color("8fd7b0") : node.State == "ACTIVE PROGRAM" ? VisualUi.Accent : VisualUi.Gold));
        var progress = new ProgressBar { MinValue = 0, MaxValue = 100, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 5) };
        progress.Value = node.Progress * 100;
        progress.Visible = node.State is "MATURE" or "ACTIVE PROGRAM";
        body.AddChild(progress);
        _progress[node.Id] = progress;
        return button;
    }
}

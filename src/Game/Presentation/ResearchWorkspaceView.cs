using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game.Presentation;

/// <summary>Fullscreen, observer-safe research map. Structural refreshes are keyed by visible
/// nodes and edges; routine funding/progress refreshes update existing controls in place.</summary>
public partial class ResearchWorkspaceView : Control
{
    private static readonly (string Name, string[] Domains)[] Tabs =
    {
        ("ALL RESEARCH", Array.Empty<string>()), ("PHYSICS", new[]{"foundations","research_infrastructure"}),
        ("ENGINEERING", new[]{"propulsion","space_industry","planetary","military","logistics","stellar_engineering"}),
        ("ENERGY", new[]{"energy"}), ("COMPUTING", new[]{"computing","sensors_comms","synthetic_systems","cybernetics"}),
        ("MATERIALS", new[]{"materials"}), ("BIOLOGY & MEDICINE", new[]{"life_medicine","biotechnology","biosphere_agriculture","alternative_biochemistry"}),
        ("SOCIETY & ECONOMY", new[]{"social_admin","economic_trade"}), ("XENOSCIENCE", new[]{"xenoscience"})
    };
    private readonly Dictionary<string, Button> _buttons = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UiResearchHorizonNode> _nodes = new(StringComparer.Ordinal);
    private readonly FlowContainer _tabs = new();
    private readonly Control _graph = new();
    private readonly VBoxContainer _inspector = new();
    private LineEdit _search = null!;
    private string _tab = "ALL RESEARCH", _signature = string.Empty;
    private Vector2 _pan; private float _zoom = 1f; private bool _dragging; private Vector2 _dragStart;
    public event Action<string>? Start; public event Action<string>? Pause; public event Action<string>? Resume;
    public ResearchWorkspaceView()
    {
        Name = "ResearchWorkspace"; Visible = false; MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddThemeStyleboxOverride("panel", VisualUi.OperationSurface(12));
        var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        AddChild(root); var header = new HBoxContainer(); root.AddChild(header);
        _search = new LineEdit { PlaceholderText = "Search available and completed research", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _search.TextChanged += _ => Rebuild(); header.AddChild(_search);
        var close = VisualUi.Button("CLOSE", "Return to the map.", () => Visible = false); header.AddChild(close);
        root.AddChild(_tabs); foreach (var tab in Tabs) { var b=VisualUi.Button(tab.Name, $"Show {tab.Name.ToLowerInvariant()}.", ()=>{_tab=tab.Name;Rebuild();}); _tabs.AddChild(b); }
        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; root.AddChild(body);
        _graph.SizeFlagsHorizontal = SizeFlags.ExpandFill; _graph.SizeFlagsVertical = SizeFlags.ExpandFill; _graph.GuiInput += OnGraphInput; body.AddChild(_graph);
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(330,0) }; scroll.AddChild(_inspector); body.AddChild(scroll);
    }
    public void Open() { Visible = true; GrabFocus(); }
    public void UpdateWorkspace(IReadOnlyList<UiResearchHorizonNode> nodes, IReadOnlyList<UiResearchHorizonEdge> edges)
    {
        var shown = nodes.Where(n => n.State is "ACTIVE PROGRAM" or "MATURE" || n.CanStart || n.State == "INVESTIGABLE").ToArray();
        var signature = string.Join('|', shown.Select(n=>$"{n.Id}:{n.DomainId}:{n.State}").OrderBy(x=>x)) + "/" + string.Join('|',edges.Select(e=>$"{e.FromId}>{e.ToId}").OrderBy(x=>x));
        _nodes.Clear(); foreach(var n in shown) _nodes[n.Id]=n;
        if (signature != _signature) { _signature=signature; Rebuild(); } else Refresh();
    }
    private void Rebuild()
    {
        if (_graph is null) return; foreach(var child in _graph.GetChildren()) child.QueueFree(); _buttons.Clear();
        var allowed = Tabs.First(t=>t.Name==_tab).Domains; var q=_search?.Text.Trim() ?? "";
        var displayed=_nodes.Values.Where(n => (allowed.Length==0 || allowed.Contains(n.DomainId)) && (q.Length==0 || ($"{n.Title} {n.DomainId} {n.WhatItDoes} {n.Benefits}").Contains(q,StringComparison.OrdinalIgnoreCase))).OrderBy(n=>n.DomainId).ThenBy(n=>n.Title).ToArray();
        for(int i=0;i<displayed.Length;i++){ var n=displayed[i]; var b=VisualUi.Button(n.Title,$"Select {n.Title}.",()=>Select(n.Id)); b.Name="ResearchNode_"+n.Id; b.Position=_pan+new Vector2(28+(i%4)*190,28+(i/4)*116)*_zoom; b.Size=new Vector2(170,76)*_zoom; _graph.AddChild(b);_buttons[n.Id]=b; }
        if(displayed.Length==0) _graph.AddChild(VisualUi.Text("No visible research matches this search.",14,VisualUi.Muted));
    }
    private void Refresh(){ foreach(var pair in _buttons) if(_nodes.TryGetValue(pair.Key,out var n)) pair.Value.Text=n.Title; }
    private void Select(string id){ if(!_nodes.TryGetValue(id,out var n))return; foreach(var c in _inspector.GetChildren())c.QueueFree(); _inspector.AddChild(VisualUi.Text(n.Title,20,Colors.White,true)); _inspector.AddChild(VisualUi.Text($"WHAT IT DOES\n{n.WhatItDoes}\n\nBENEFITS / UNLOCKS\n{n.Benefits}\n\nCOST & TIME\n{n.CostAndTime}\n\nREQUIREMENTS / STATUS\n{n.RequirementsStatus}",12,VisualUi.Muted,true)); var action=VisualUi.Button(n.CanStart?"BEGIN RESEARCH":n.CanPause?"PAUSE PROGRAM":n.CanResume?"RESUME PROGRAM":"",n.Detail,()=>{if(n.CanStart)Start?.Invoke(id);else if(n.CanPause)Pause?.Invoke(id);else if(n.CanResume)Resume?.Invoke(id);}); action.Name="ResearchAction_"+id; action.Visible=n.CanStart||n.CanPause||n.CanResume;_inspector.AddChild(action); }
    private void OnGraphInput(InputEvent input)
    {
        if (input is InputEventMouseButton button && button.ButtonIndex == MouseButton.Left)
        { _dragging = button.Pressed; _dragStart = button.Position; return; }
        if (input is InputEventMouseMotion motion && _dragging)
        { _pan += motion.Relative; Rebuild(); return; }
        if (input is InputEventMouseButton wheel && wheel.Pressed &&
            (wheel.ButtonIndex == MouseButton.WheelUp || wheel.ButtonIndex == MouseButton.WheelDown))
        { _zoom = Math.Clamp(_zoom + (wheel.ButtonIndex == MouseButton.WheelUp ? .1f : -.1f), .65f, 1.4f); Rebuild(); }
    }

}

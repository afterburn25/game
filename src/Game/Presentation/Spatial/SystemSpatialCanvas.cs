using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game.Presentation.Spatial;

/// <summary>Mouse/UI adapter over a perspective 3D scene. Only observer-safe markers
/// enter the renderer; the simulation owns all orders and facts.</summary>
public partial class SystemSpatialCanvas : Control
{
    private SystemSpatialSnapshot? _snapshot;
    private IReadOnlyDictionary<int, SystemSpatialBodyMarker> _bodiesById = new Dictionary<int, SystemSpatialBodyMarker>();
    private SystemScene3D _scene = null!;
    private Font _font = null!;
    private int? _selectedBodyId, _hoveredBodyId;
    private bool _leftPanCandidate, _leftPanMoved, _rotating;
    private Vector2 _leftPanStart;
    private bool _descentRequested;
    public event Action? ReturnRequested;
    public event Action<int>? BodyOrderRequested;
    public event Action<int>? DescentRequested;
    public event Action<string>? InfrastructureRequested;
    public Func<bool>? IsObjectInspectorOpen { get; set; }
    public int? BackgroundSystemId => _snapshot?.SystemId;
    public int? SelectedBodyId => _selectedBodyId;
    internal SystemSpatialBodyMarker? GetBodyMarker(int bodyId) => _snapshot?.Bodies.FirstOrDefault(body => body.BodyId == bodyId);
    public int? HoveredBodyId => _hoveredBodyId ?? _focusedBodyId;
    public IReadOnlyList<SystemSpatialInfrastructureMarker> VisibleInfrastructure => _snapshot?.Infrastructure ?? Array.Empty<SystemSpatialInfrastructureMarker>();
    internal SystemScene3D Scene => _scene;
    public string? GetBodyLabel(int id) => _bodiesById.TryGetValue(id, out var body) ? body.Label : null;
    public Vector2? GetBodyScreenPosition(int id) => _snapshot is null ? null : _scene.ProjectBody(id);
    public Vector2? GetInfrastructureScreenPosition(string id) => _snapshot is null ? null : _scene.ProjectInfrastructure(id);

    public override void _Ready()
    {
        _font = ThemeDB.FallbackFont;
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        Size = GetViewport().GetVisibleRect().Size;
        GetViewport().SizeChanged += SyncCanvasSize;
        _scene = new SystemScene3D { Name = "SystemScene3D", ZIndex = -1 };
        AddChild(_scene);
        MouseExited += () => { _hoveredBodyId = null; QueueRedraw(); };
    }
    public override void _ExitTree() => GetViewport().SizeChanged -= SyncCanvasSize;
    private void SyncCanvasSize() => Size = GetViewport().GetVisibleRect().Size;
    public override void _Process(double delta)
    {
        if (_snapshot is null || !IsVisibleInTree()) return;
        if (IsNavigationBlocked?.Invoke() == true)
            _leftPanCandidate = _leftPanMoved = _rotating = false;
        else
        {
            _scene.Advance(delta);
            if (!_descentRequested && _focusedBodyId is int id && _scene.FocusAltitudeRatio < 1.3f && CanOpenSurface?.Invoke(id) == true)
            {
                _descentRequested = true;
                DescentRequested?.Invoke(id);
            }
        }
        UpdatePlanetInspector();
        UpdateLocalFleets();
        QueueRedraw();
    }
    public override void _GuiInput(InputEvent input)
    {
        if (_snapshot is null || IsNavigationBlocked?.Invoke() == true) { AcceptEvent(); return; }
        if (input is InputEventMouseMotion motion)
        {
            _rotating &= (motion.ButtonMask & MouseButtonMask.Middle) != 0;
            _leftPanCandidate &= (motion.ButtonMask & MouseButtonMask.Left) != 0;
            if (_rotating) _scene.Rotate(motion.Relative);
            else if (_leftPanCandidate)
            {
                _leftPanMoved |= motion.Position.DistanceTo(_leftPanStart) >= 5;
                if (_leftPanMoved) _scene.Pan(motion.Relative);
            }
            else _hoveredBodyId = _scene.HitBody(motion.Position);
            MouseDefaultCursorShape = _hoveredBodyId.HasValue ? CursorShape.PointingHand : CursorShape.Arrow;
        }
        if (input is InputEventMouseButton button)
        {
            if (button.ButtonIndex == MouseButton.Middle) _rotating = button.Pressed;
            if (button.ButtonIndex == MouseButton.Left)
            {
                if (button.Pressed && button.DoubleClick) { _leftPanCandidate = false; SelectAt(button.Position, true); }
                else if (button.Pressed) { _leftPanCandidate = true; _leftPanMoved = false; _leftPanStart = button.Position; }
                else if (_leftPanCandidate) { if (!_leftPanMoved) SelectAt(button.Position, false); _leftPanCandidate = _leftPanMoved = false; }
            }
            if (button.Pressed && button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
                ZoomAt(button.ButtonIndex == MouseButton.WheelUp ? 1.22f : 1f / 1.22f, button.Position);
            if (button.Pressed && button.ButtonIndex == MouseButton.Right && _scene.HitBody(button.Position) is int id)
                BodyOrderRequested?.Invoke(id);
        }
        AcceptEvent();
    }
    private void SelectAt(Vector2 point, bool focus)
    {
        var hit = _scene.HitBody(point);
        if (hit is int id)
        {
            _selectedBodyId = id;
            if (focus) FocusBody(id);
        }
        else if (_scene.HitInfrastructure(point) is string facility) { InfrastructureRequested?.Invoke(facility); return; }
        else if (!IsPlanetFocused) _selectedBodyId = null;
        UpdatePlanetInspector();
    }
    public void SetSnapshot(SystemSpatialSnapshot? snapshot)
    {
        if (snapshot?.SystemId != _snapshot?.SystemId || snapshot is null)
        {
            _selectedBodyId = _hoveredBodyId = _focusedBodyId = null;
            _descentRequested = false;
            _leftPanCandidate = _leftPanMoved = _rotating = false;
        }
        _snapshot = snapshot;
        _bodiesById = snapshot?.Bodies.ToDictionary(body => body.BodyId) ?? new Dictionary<int, SystemSpatialBodyMarker>();
        if (snapshot is not null) _scene.Present(snapshot);
        else _scene.Clear();
        if (_selectedBodyId is int id && !_bodiesById.ContainsKey(id)) _selectedBodyId = null;
        Visible = snapshot is not null;
        QueueRedraw();
    }
    public override void _Draw()
    {
        if (_snapshot is null) return;
        foreach (var body in _snapshot.Bodies)
        {
            if (_scene.ProjectBody(body.BodyId) is not Vector2 point || point.X < 65 || point.X > Size.X - 288 || point.Y < 110 || point.Y > Size.Y - 40) continue;
            var selected = body.BodyId == _selectedBodyId || body.BodyId == _focusedBodyId;
            var near = body.BodyId == _hoveredBodyId;
            var color = selected ? new Color("b1e8d8") : near ? Colors.White : new Color("a8b9ba");
            var labelWidth = _font.GetStringSize(body.Label, fontSize: 12).X;
            if (!IsPlanetFocused || body.BodyId != _focusedBodyId)
            {
                DrawStyleBox(VisualUi.Surface(margin: 3), new Rect2(point + new Vector2(-labelWidth/2-5, 15), new(labelWidth+10, 20)));
                DrawString(_font, point + new Vector2(-labelWidth/2, 30), body.Label, fontSize: 12, modulate: color);
            }
            if (selected || near) DrawArc(point, 14, 0, MathF.Tau, 64, color, 1, true);
        }
    }
}

using System;
using System.Collections.Generic;
using Godot;

namespace Game.Presentation.Spatial;

public partial class SystemSpatialCanvas
{
    private int? _focusedBodyId;
    public Func<bool>? IsNavigationBlocked { get; set; }
    public bool IsPlanetFocused => _focusedBodyId.HasValue;
    public int? FocusedBodyId => _focusedBodyId;
    public string SystemName => _snapshot?.CatalogName ?? "System";
    public IReadOnlyList<SystemSpatialBodyMarker> VisibleBodies => _snapshot?.Bodies ?? Array.Empty<SystemSpatialBodyMarker>();
    public int CachedSurfaceCount => _scene?.BodyCount ?? 0;
    public void BeginEntry(Vector2 previousStarScreen) => _scene.SetEntry(previousStarScreen);
    public void ZoomAt(float factor, Vector2 anchor)
    {
        if (_snapshot is null || IsNavigationBlocked?.Invoke() == true) return;
        if (factor < 1 && IsPlanetFocused && _scene.TargetDistance / factor > _scene.GetBodyRadius(_focusedBodyId!.Value) * 12)
        { ExitPlanetFocus(); return; }
        if (factor < 1 && !IsPlanetFocused && _scene.TargetDistance / factor > _scene.FitDistance * 1.75f)
        { ReturnRequested?.Invoke(); return; }
        if (factor > 1 && !IsPlanetFocused && _selectedBodyId.HasValue && _scene.TargetDistance < _scene.FitDistance * .56f)
        { FocusSelectedBody(); return; }
        _descentRequested = false;
        _scene.Zoom(factor, anchor);
    }
    public void FocusSelectedBody()
    {
        if (_selectedBodyId is int id) FocusBody(id);
    }
    public bool FocusBody(int id)
    {
        if (IsNavigationBlocked?.Invoke() == true || !_bodiesById.ContainsKey(id)) return false;
        _selectedBodyId = _focusedBodyId = id;
        _descentRequested = false;
        _scene.FocusBody(id);
        _leftPanCandidate = _leftPanMoved = _rotating = false;
        return true;
    }
    public void ExitPlanetFocus()
    {
        _focusedBodyId = null;
        _descentRequested = false;
        _scene.ExitFocus();
    }
    public void PrepareSurfaceReturn()
    {
        _descentRequested = true;
        if (_focusedBodyId is int id) _scene.FocusBody(id);
    }
}

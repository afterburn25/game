using System;
using System.Collections.Generic;
using Godot;
using Game.Simulation.Models;

namespace Game.Presentation.Spatial;

public partial class SystemSpatialCanvas
{
    private readonly SmoothSpatialCamera _camera = new();
    private bool _cameraReady;
    private bool _systemPanning;
    private bool _leftPanCandidate;
    private bool _leftPanMoved;
    private Vector2 _leftPanStart;
    private int? _focusedBodyId;
    private int? _renderedFocusBodyId;
    private float _focusMagnification = 1;
    private float _focusContextRadius;
    private SystemSpatialViewport _savedOrbitalCamera;
    private FocusedPlanetView? _focusedPlanetView;

    public Func<bool>? IsNavigationBlocked { get; set; }
    public bool IsPlanetFocused => _focusedBodyId.HasValue;
    public int? FocusedBodyId => _focusedBodyId;
    public string SystemName => _snapshot?.CatalogName ?? "System";
    public IReadOnlyList<SystemSpatialBodyMarker> VisibleBodies => _snapshot?.Bodies ?? Array.Empty<SystemSpatialBodyMarker>();
    public int CachedSurfaceCount => _surfaces.Count + (_focusedPlanetView is null ? 0 : 1);
    internal SmoothSpatialCamera Camera => _camera;
    private SystemSpatialViewport CurrentViewport => _cameraReady
        ? new(_camera.OriginX, _camera.OriginY, _camera.Scale)
        : _snapshot is null ? new(Size.X / 2, Size.Y / 2, 1) : SystemSpatialViewport.Fit(_snapshot, Size.X, Size.Y);
    private float OrbitalContextOpacity => _renderedFocusBodyId is int id && _bodiesById.TryGetValue(id, out var body)
        ? SpatialNavigationLayout.OrbitalContextOpacity(CurrentViewport.BodyRadius(body),
            _savedOrbitalCamera.BodyRadius(body), _focusContextRadius)
        : 1;

    public void BeginEntry(Vector2 previousStarScreen)
    {
        if (_snapshot is null) return;
        var fit = SystemSpatialViewport.Fit(_snapshot, Size.X, Size.Y);
        _camera.Snap(fit.Scale * 0.35f, previousStarScreen.X, previousStarScreen.Y);
        _camera.SetTarget(fit.Scale, fit.CenterX, fit.CenterY);
        _cameraReady = true;
    }

    public void ZoomAt(float factor, Vector2 anchor)
    {
        if (_snapshot is null || IsNavigationBlocked?.Invoke() == true) return;
        EnsureOrbitalCamera();
        if (IsPlanetFocused)
        {
            if (factor < 1 && _focusMagnification * factor < 0.85f)
                ExitPlanetFocus();
            else
            {
                _focusMagnification = Math.Clamp(_focusMagnification * factor, 0.85f, 1.65f);
                TargetPlanetFocus();
            }
            return;
        }
        var fit = SystemSpatialViewport.Fit(_snapshot, Size.X, Size.Y);
        if (factor < 1 && _camera.TargetScale * factor < fit.Scale * 0.58f)
        {
            ReturnRequested?.Invoke();
            return;
        }
        if (factor > 1 && _camera.TargetScale >= fit.Scale * 1.9f && _selectedBodyId.HasValue)
        {
            FocusSelectedBody();
            return;
        }
        _camera.ZoomAt(factor, anchor.X, anchor.Y, fit.Scale * 0.58f, fit.Scale * 4.4f);
    }

    public void FocusSelectedBody()
    {
        if (IsNavigationBlocked?.Invoke() == true || IsPlanetFocused ||
            _selectedBodyId is not int id || !_bodiesById.TryGetValue(id, out var body)) return;
        EnsureOrbitalCamera();
        _savedOrbitalCamera = CurrentViewport;
        _focusedBodyId = id;
        _renderedFocusBodyId = id;
        _focusMagnification = 1;
        _systemPanning = false;
        _leftPanCandidate = false;
        _leftPanMoved = false;
        ReleaseFocusedView();
        _focusedPlanetView = new FocusedPlanetView { Name = "FocusedPlanet", Visible = true };
        AddChild(_focusedPlanetView);
        _focusedPlanetView.SetBody(body);
        TargetPlanetFocus();
        UpdateFocusedDisc();
        QueueRedraw();
    }

    public bool FocusBody(int bodyId)
    {
        if (!_bodiesById.ContainsKey(bodyId)) return false;
        if (IsPlanetFocused) ExitPlanetFocus();
        _selectedBodyId = bodyId;
        FocusSelectedBody();
        return _focusedBodyId == bodyId;
    }

    public void ExitPlanetFocus()
    {
        if (!IsPlanetFocused) return;
        _focusedBodyId = null;
        _camera.SetTarget(_savedOrbitalCamera.Scale, _savedOrbitalCamera.CenterX, _savedOrbitalCamera.CenterY);
        _systemPanning = false;
        _leftPanCandidate = false;
        _leftPanMoved = false;
        QueueRedraw();
    }

    private void EnsureOrbitalCamera()
    {
        if (_cameraReady || _snapshot is null) return;
        var fit = SystemSpatialViewport.Fit(_snapshot, Size.X, Size.Y);
        _camera.Snap(fit.Scale, fit.CenterX, fit.CenterY);
        _cameraReady = true;
    }

    private void TargetPlanetFocus()
    {
        if (_focusedBodyId is not int id || !_bodiesById.TryGetValue(id, out var body)) return;
        var ringExtent = body.SurfaceKey == "saturn" && body.HasDetailedEnvironment ? 2.16f : 1;
        var radius = Math.Max(40, Math.Min((Size.Y - 310) * 0.5f, (Size.X - 370) * 0.36f / ringExtent));
        _focusContextRadius = radius;
        var bodyScale = body.DisplayRadius * (body.Kind == PlanetaryBodyKind.Moon ? 1 : 1.8f);
        var scale = radius * _focusMagnification / Math.Max(1, bodyScale);
        var center = new Vector2(Size.X * 0.60f, (Size.Y + 30) * 0.5f);
        _camera.SetTarget(scale, center.X - body.OffsetX * scale, center.Y - body.OffsetY * scale);
    }

    private void UpdateFocusedDisc()
    {
        if (_focusedPlanetView is null || _renderedFocusBodyId is not int id || !_bodiesById.TryGetValue(id, out var body)) return;
        var layout = CurrentViewport;
        var center = ToScreen(body, new Vector2(layout.CenterX, layout.CenterY), layout.Scale);
        var radius = layout.BodyRadius(body);
        _focusedPlanetView.SetDiscRect(new Rect2(center - Vector2.One * radius, Vector2.One * radius * 2));
    }

    private void ResetSpatialCamera()
    {
        _cameraReady = false;
        _focusedBodyId = null;
        _renderedFocusBodyId = null;
        _systemPanning = false;
        _leftPanCandidate = false;
        _leftPanMoved = false;
        ReleaseFocusedView();
    }

    private void ReleaseFocusedView()
    {
        if (_focusedPlanetView is null) return;
        RemoveChild(_focusedPlanetView);
        _focusedPlanetView.QueueFree();
        _focusedPlanetView = null;
    }
}

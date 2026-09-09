using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Game.Presentation.Spatial;
using Game.Simulation.Knowledge;

namespace Game.Presentation;

public sealed record SpatialCameraSnapshot(string Level, float Zoom, Vector2 Pan, float TargetZoom,
    Vector2 TargetPan, int? FocusedBodyId, bool IsTransitioning);
public sealed record SpatialCatalogEntry(int SystemId, SystemSurveyLevel SurveyLevel);

public partial class Main
{
    private readonly SmoothSpatialCamera _regionalCamera = new();
    private object? _regionalCameraCampaign;
    private Vector2 _regionalViewportSize;
    private bool _regionalCameraReady;
    private float _systemViewBlend;
    private bool _leavingSystem;
    private HBoxContainer? _spatialBreadcrumbs;
    private Button? _galaxyCrumb;
    private Button? _regionCrumb;
    private Button? _systemCrumb;
    private Button? _planetCrumb;
    private Button? _surfaceCrumb;

    public float UiMapZoom => _zoom;
    public Vector2 UiMapOriginScreen => GetViewportRect().Size * 0.5f + _pan;
    public float UiOverviewBlend => Math.Clamp((0.13f - _zoom) / 0.07f, 0, 1);
    public float UiSystemViewBlend => _systemViewBlend;
    public int? UiFocusedPlanetBodyId => _systemSpatialCanvas?.FocusedBodyId;
    public event Action<int>? PlanetSurfaceRequested;
    public Func<int, bool>? PlanetSurfaceAvailable { get; set; }
    public IReadOnlyList<SystemSpatialBodyMarker> UiSystemBodies =>
        _systemSpatialCanvas?.VisibleBodies ?? Array.Empty<SystemSpatialBodyMarker>();
    public IReadOnlyList<SystemSpatialInfrastructureMarker> UiSystemInfrastructure =>
        _systemSpatialCanvas?.VisibleInfrastructure ?? Array.Empty<SystemSpatialInfrastructureMarker>();
    public Vector2? UiGetInfrastructureScreenPosition(string projectId) =>
        _systemSpatialCanvas?.GetInfrastructureScreenPosition(projectId);
    public int UiCachedPlanetMaterialCount => _systemSpatialCanvas?.CachedSurfaceCount ?? 0;
    public IReadOnlyList<SpatialCatalogEntry> UiSpatialCatalog => _galaxy?.Systems
        .Select(system => new SpatialCatalogEntry(system.Id,
            _galaxy.Knowledge.GetSystemSurveyLevel(_galaxy.PlayerCivilizationId, system.Id))).ToArray()
        ?? Array.Empty<SpatialCatalogEntry>();
    public SpatialCameraSnapshot UiCameraSnapshot
    {
        get
        {
            var system = UiIsSystemSpatialView;
            var camera = system ? _systemSpatialCanvas!.Camera : _regionalCamera;
            return new(UiSpatialScale.ToString(), camera.Scale, new(camera.OriginX, camera.OriginY), camera.TargetScale,
                new(camera.TargetOriginX, camera.TargetOriginY), UiFocusedPlanetBodyId,
                camera.IsMoving || (system && (_systemViewBlend < 1 || _leavingSystem)));
        }
    }

    private void InitializeSpatialNavigation()
    {
        if (_spatialBreadcrumbs is not null) return;
        var layer = new CanvasLayer { Name = "SpatialNavigation", Layer = 4 };
        _spatialBreadcrumbs = new HBoxContainer { Position = new Vector2(118, 78) };
        VisualUi.ContainPointerInput(_spatialBreadcrumbs);
        _spatialBreadcrumbs.AddThemeConstantOverride("separation", 5);
        Button Crumb(string name, string text, Action action)
        {
            var button = new Button { Name = name, Text = text, CustomMinimumSize = new Vector2(0, 28), MouseFilter = Control.MouseFilterEnum.Stop };
            button.AddThemeFontSizeOverride("font_size", 12);
            button.Pressed += action;
            _spatialBreadcrumbs.AddChild(button);
            return button;
        }
        Crumb("SpatialBack", "‹ Back", UiNavigateBack);
        _galaxyCrumb = Crumb("SpatialOverview", "Milky Way", UiShowGalaxyOverview);
        _regionCrumb = Crumb("SpatialRegion", "Stellar region", UiShowStellarRegion);
        _systemCrumb = Crumb("SpatialSystem", "System", UiOpenSelectedSystem);
        _planetCrumb = Crumb("SpatialPlanet", "Planet", () => _systemSpatialCanvas?.FocusSelectedBody());
        _surfaceCrumb = Crumb("SpatialSurface", "Surface", () =>
        {
            if (UiFocusedPlanetBodyId is int bodyId && PlanetSurfaceAvailable?.Invoke(bodyId) == true)
                PlanetSurfaceRequested?.Invoke(bodyId);
        });
        layer.AddChild(_spatialBreadcrumbs);
        AddChild(layer);
        SynchronizeRegionalCamera();
    }

    private void RefreshSpatialNavigation(double delta)
    {
        if (!_regionalCameraReady || !ReferenceEquals(_regionalCameraCampaign, _galaxy))
            SynchronizeRegionalCamera();
        var size = GetViewportRect().Size;
        if (size != _regionalViewportSize)
        {
            var shift = (size - _regionalViewportSize) * 0.5f;
            _regionalCamera.Translate(shift.X, shift.Y);
            _regionalViewportSize = size;
            _pan = new Vector2(_regionalCamera.OriginX, _regionalCamera.OriginY) - size * 0.5f;
        }
        if (!(UiIsMenuOpen || UiIsDeveloperToolsOpen) && _regionalCamera.Advance(delta))
        {
            _zoom = _regionalCamera.Scale;
            _pan = new Vector2(_regionalCamera.OriginX, _regionalCamera.OriginY) - size * 0.5f;
            QueueRedraw();
        }
        if (!(UiIsMenuOpen || UiIsDeveloperToolsOpen) && _systemSpatialState.IsOpen && _systemSpatialCanvas is not null)
        {
            var goal = _leavingSystem ? 0f : 1f;
            _systemViewBlend = Mathf.MoveToward(_systemViewBlend, goal, (float)Math.Clamp(delta, 0, 0.1) * 5.5f);
            _systemSpatialCanvas.Modulate = new Color(1, 1, 1, _systemViewBlend);
            if (_leavingSystem && _systemViewBlend <= 0)
                ReturnToStellarView(announce: false);
        }
        if (_spatialBreadcrumbs is null) return;
        _spatialBreadcrumbs.Visible = !(UiIsMenuOpen || UiIsDeveloperToolsOpen);
        _galaxyCrumb!.Disabled = !UiIsSystemSpatialView && UiOverviewBlend > 0.9f;
        _regionCrumb!.Disabled = !UiIsSystemSpatialView && UiOverviewBlend < 0.1f;
        _systemCrumb!.Visible = _selectedSystemId >= 0;
        _systemCrumb.Disabled = UiIsSystemSpatialView && !_systemSpatialCanvas!.IsPlanetFocused;
        _systemCrumb.Text = UiIsSystemSpatialView ? _systemSpatialCanvas!.SystemName : "Open system";
        _planetCrumb!.Visible = _systemSpatialCanvas?.SelectedBodyId.HasValue == true && UiIsSystemSpatialView;
        _planetCrumb.Disabled = _systemSpatialCanvas?.IsPlanetFocused == true;
        _planetCrumb.Text = _systemSpatialCanvas?.SelectedBodyId is int selected ? _systemSpatialCanvas.GetBodyLabel(selected) ?? "Planet" : "Planet";
        _surfaceCrumb!.Visible = UiFocusedPlanetBodyId.HasValue;
        _surfaceCrumb.Disabled = PlanetSurfaceRequested is null || UiFocusedPlanetBodyId is not int surfaceBody ||
            PlanetSurfaceAvailable?.Invoke(surfaceBody) != true;
    }

    private void SynchronizeRegionalCamera()
    {
        _regionalViewportSize = GetViewportRect().Size;
        var origin = _regionalViewportSize * 0.5f + _pan;
        _regionalCamera.Snap(_zoom, origin.X, origin.Y);
        _regionalCameraCampaign = _galaxy;
        _regionalCameraReady = true;
    }

    private void PanRegionalCamera(Vector2 motion)
    {
        if (!_regionalCameraReady) SynchronizeRegionalCamera();
        _regionalCamera.Pan(motion.X, motion.Y);
        _pan = new Vector2(_regionalCamera.OriginX, _regionalCamera.OriginY) - GetViewportRect().Size * 0.5f;
    }

    private Vector2 SpatialZoomButtonAnchor()
    {
        if (UiIsSystemSpatialView)
            return _systemSpatialCanvas?.SelectedBodyId is int bodyId
                ? _systemSpatialCanvas.GetBodyScreenPosition(bodyId) ?? GetViewportRect().Size * 0.5f
                : GetViewportRect().Size * 0.5f;
        return _selectedSystemId >= 0 ? UiGetCatalogScreenPosition(_selectedSystemId) ?? UiMapOriginScreen : UiMapOriginScreen;
    }

    private void ZoomSpatialAt(float factor, Vector2 anchor)
    {
        if (UiIsMenuOpen || UiIsDeveloperToolsOpen) return;
        if (UiIsSystemSpatialView)
        {
            _systemSpatialCanvas?.ZoomAt(factor, anchor);
            return;
        }
        if (!_regionalCameraReady) SynchronizeRegionalCamera();
        if (factor > 1 && _regionalCamera.TargetScale >= 2.7f && _selectedSystemId >= 0)
        {
            EnterSelectedSystemView();
            return;
        }
        _regionalCamera.ZoomAt(factor, anchor.X, anchor.Y, 0.025f, 3.2f);
    }

    public void UiShowGalaxyOverview()
    {
        if (UiIsMenuOpen || UiIsDeveloperToolsOpen) return;
        ReturnToStellarView(announce: false);
        if (!_regionalCameraReady) SynchronizeRegionalCamera();
        var size = GetViewportRect().Size;
        var frame = SpatialNavigationLayout.FitGalaxyOverview(size.X, size.Y);
        _regionalCamera.SetTarget(frame.Scale, frame.CenterX, frame.CenterY);
        _panning = false;
    }

    public void UiShowStellarRegion()
    {
        if (UiIsMenuOpen || UiIsDeveloperToolsOpen) return;
        ReturnToStellarView(announce: false);
        if (!_regionalCameraReady) SynchronizeRegionalCamera();
        var size = GetViewportRect().Size;
        _regionalCamera.SetTarget(0.55f, size.X * 0.5f, size.Y * 0.5f);
        _panning = false;
    }

    public void UiNavigateBack()
    {
        if (UiIsMenuOpen || UiIsDeveloperToolsOpen) return;
        if (_systemSpatialCanvas?.IsPlanetFocused == true)
            _systemSpatialCanvas.ExitPlanetFocus();
        else if (UiIsSystemSpatialView)
            BeginReturnToRegion();
        else
            UiShowGalaxyOverview();
    }

    private void BeginReturnToRegion()
    {
        if (!_systemSpatialState.IsOpen) return;
        _leavingSystem = true;
        _panning = false;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Presentation.Spatial;

/// <summary>
/// Observer-safe orbital presentation. Surface textures are deterministic cosmetic illustrations
/// of confirmed broad classes, cached on snapshot changes; they are not new simulation facts.
/// </summary>
public partial class SystemSpatialCanvas : Control
{
    private static readonly Color CanvasColor = new(0.012f, 0.025f, 0.044f);
    private static readonly Color KeylineColor = new(0.22f, 0.36f, 0.48f);
    private static readonly Color PrimaryTextColor = new(0.90f, 0.95f, 0.98f);
    private static readonly Color SecondaryTextColor = new(0.66f, 0.76f, 0.84f);
    private static readonly Color MutedTextColor = new(0.40f, 0.52f, 0.62f);
    private static readonly Color SelectedColor = new(0.35f, 0.81f, 0.98f);
    private static readonly Color UnknownColor = new(0.55f, 0.59f, 0.66f);
    private static readonly Color ResourceColor = new(0.91f, 0.71f, 0.36f);
    private static readonly Color AnomalyColor = new(0.69f, 0.56f, 1.0f);
    private static readonly Color ActivityColor = new(0.37f, 0.82f, 0.75f);

    private SystemSpatialSnapshot? _snapshot;
    private IReadOnlyDictionary<int, SystemSpatialBodyMarker> _bodiesById = new Dictionary<int, SystemSpatialBodyMarker>();
    private readonly Dictionary<int, (SystemSpatialBodyMarker Marker, ImageTexture Texture)> _surfaces = new();
    private Font _font = null!;
    private Vector2 _lastViewportSize;
    private int? _hoveredBodyId;
    private int? _selectedBodyId;
    private float _drawOpacity = 1;

    public event Action? ReturnRequested;
    public event Action<string>? InfrastructureRequested;

    public int? SelectedBodyId => _selectedBodyId;
    public IReadOnlyList<SystemSpatialInfrastructureMarker> VisibleInfrastructure =>
        _snapshot?.Infrastructure ?? Array.Empty<SystemSpatialInfrastructureMarker>();
    public string? GetBodyLabel(int bodyId) => _bodiesById.TryGetValue(bodyId, out var body) ? body.Label : null;
    public Vector2? GetBodyScreenPosition(int bodyId)
    {
        if (_snapshot is null || !_bodiesById.TryGetValue(bodyId, out var body)) return null;
        var layout = CurrentViewport;
        return ToScreen(body, new Vector2(layout.CenterX, layout.CenterY), layout.Scale);
    }
    public Vector2? GetInfrastructureScreenPosition(string projectId)
    {
        if (_snapshot?.Infrastructure is not { } infrastructure) return null;
        var index = infrastructure.ToList().FindIndex(item => item.ProjectId == projectId);
        if (index < 0) return null;
        var layout = CurrentViewport;
        return InfrastructurePosition(new Vector2(layout.CenterX, layout.CenterY), layout.Scale, index);
    }

    public override void _Ready()
    {
        _font = ThemeDB.FallbackFont;
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.None;
        MouseExited += ClearHover;
        ResizeToViewport();
    }

    public override void _Process(double delta)
    {
        if (GetViewportRect().Size != _lastViewportSize)
        {
            var shift = (GetViewportRect().Size - _lastViewportSize) * 0.5f;
            if (_cameraReady)
            {
                _camera.Translate(shift.X, shift.Y);
                _savedOrbitalCamera = _savedOrbitalCamera with
                {
                    CenterX = _savedOrbitalCamera.CenterX + shift.X,
                    CenterY = _savedOrbitalCamera.CenterY + shift.Y,
                };
            }
            ResizeToViewport();
            if (IsPlanetFocused) TargetPlanetFocus();
            QueueRedraw();
        }
        if (_snapshot is null) return;
        EnsureOrbitalCamera();
        if (IsNavigationBlocked?.Invoke() == true)
        {
            _systemPanning = false;
            _leftPanCandidate = false;
            _leftPanMoved = false;
            return;
        }
        if (_camera.Advance(delta)) QueueRedraw();
        if (!IsPlanetFocused && !_camera.IsMoving && _focusedPlanetView is not null)
        {
            ReleaseFocusedView();
            _renderedFocusBodyId = null;
            QueueRedraw();
        }
        UpdateFocusedDisc();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (IsNavigationBlocked?.Invoke() == true) { AcceptEvent(); return; }
        if (_snapshot is not null)
        {
            var layout = CurrentViewport;
            if (@event is InputEventMouseMotion motion)
            {
                if (_leftPanCandidate && !IsPlanetFocused)
                {
                    if (!_leftPanMoved && motion.Position.DistanceTo(_leftPanStart) >= 5)
                        _leftPanMoved = true;
                    if (_leftPanMoved)
                    {
                        _camera.Pan(motion.Relative.X, motion.Relative.Y);
                        QueueRedraw();
                        AcceptEvent();
                        return;
                    }
                }
                if (_systemPanning && !IsPlanetFocused)
                {
                    _camera.Pan(motion.Relative.X, motion.Relative.Y);
                    QueueRedraw();
                    AcceptEvent();
                    return;
                }
                var hovered = layout.HitBody(_snapshot, motion.Position.X, motion.Position.Y);
                if (hovered != _hoveredBodyId)
                {
                    _hoveredBodyId = hovered;
                    MouseDefaultCursorShape = hovered.HasValue ? CursorShape.PointingHand : CursorShape.Arrow;
                    QueueRedraw();
                }
            }
            if (@event is InputEventMouseButton gesture)
            {
                if (gesture.ButtonIndex == MouseButton.Middle)
                    _systemPanning = gesture.Pressed && !IsPlanetFocused;
                if (gesture.ButtonIndex == MouseButton.Left && !gesture.DoubleClick)
                {
                    if (gesture.Pressed && !IsPlanetFocused)
                    {
                        _leftPanCandidate = true;
                        _leftPanMoved = false;
                        _leftPanStart = gesture.Position;
                    }
                    else if (!gesture.Pressed && _leftPanCandidate)
                    {
                        if (!_leftPanMoved) HandleLeftClick(gesture.Position, false);
                        _leftPanCandidate = false;
                        _leftPanMoved = false;
                    }
                }
                if (gesture.Pressed && gesture.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
                    ZoomAt(gesture.ButtonIndex == MouseButton.WheelUp ? 1.22f : 1f / 1.22f, gesture.Position);
            }
            if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left && mouse.DoubleClick)
            {
                _leftPanCandidate = false;
                _leftPanMoved = false;
                HandleLeftClick(mouse.Position, true);
            }
        }
        // The canvas owns system-space pointer input. Higher CanvasLayer controls retain their
        // events; hidden regional-map fleet orders must never fire through the orbital view.
        AcceptEvent();
    }

    private void HandleLeftClick(Vector2 position, bool doubleClick)
    {
        if (_snapshot is null) return;
        if (!IsPlanetFocused && HitInfrastructure(position) is { } infrastructure)
        {
            InfrastructureRequested?.Invoke(infrastructure.ProjectId);
            return;
        }
        if (!IsPlanetFocused && _selectedBodyId.HasValue && new Rect2(108, 235, 284, 225).HasPoint(position))
            return;
        var layout = CurrentViewport;
        var hit = layout.HitBody(_snapshot, position.X, position.Y);
        if (IsPlanetFocused)
        {
            if (doubleClick && hit != _focusedBodyId) ExitPlanetFocus();
            return;
        }
        _selectedBodyId = hit;
        QueueRedraw();
        if (!doubleClick) return;
        if (hit.HasValue) FocusSelectedBody();
        else if (!layout.HitsCelestialObject(_snapshot, position.X, position.Y)) ReturnRequested?.Invoke();
    }

    public void SetSnapshot(SystemSpatialSnapshot? snapshot)
    {
        if (snapshot?.SystemId != _snapshot?.SystemId || snapshot is null)
        {
            ResetSpatialCamera();
            _selectedBodyId = null;
            _hoveredBodyId = null;
            ClearSurfaces();
        }
        _snapshot = snapshot;
        _bodiesById = snapshot?.Bodies.ToDictionary(marker => marker.BodyId)
            ?? new Dictionary<int, SystemSpatialBodyMarker>();
        foreach (var staleId in _surfaces.Keys.Where(id => !_bodiesById.ContainsKey(id)).ToArray())
        {
            _surfaces[staleId].Texture.Dispose();
            _surfaces.Remove(staleId);
        }
        if (snapshot is not null)
        {
            foreach (var body in snapshot.Bodies)
            {
                if (_surfaces.TryGetValue(body.BodyId, out var existing) && existing.Marker == body)
                    continue;
                if (_surfaces.Remove(body.BodyId, out existing))
                    existing.Texture.Dispose();
                if (body.HasDetailedEnvironment && body.VisualClass is not SystemSpatialBodyVisualClass.UnknownPlanet and not SystemSpatialBodyVisualClass.UnknownMoon)
                    _surfaces.Add(body.BodyId, (body, CreateSurface(body)));
            }
        }
        if (_selectedBodyId.HasValue && !_bodiesById.ContainsKey(_selectedBodyId.Value))
            _selectedBodyId = null;
        if (_hoveredBodyId.HasValue && !_bodiesById.ContainsKey(_hoveredBodyId.Value))
            _hoveredBodyId = null;
        if (_focusedBodyId is int focused)
        {
            if (_bodiesById.TryGetValue(focused, out var body)) _focusedPlanetView?.SetBody(body);
            else ResetSpatialCamera();
        }
        Visible = snapshot is not null;
        QueueRedraw();
    }

    public override void _ExitTree() => ClearSurfaces();

    public override void _Draw()
    {
        if (_snapshot is null)
            return;
        var viewport = Size;
        DrawSpace(viewport);
        var layout = CurrentViewport;
        var center = new Vector2(layout.CenterX, layout.CenterY);
        DrawHeader(_snapshot);
        _drawOpacity = OrbitalContextOpacity;
        if (_drawOpacity > 0.001f)
        {
            DrawOrbits(_snapshot, center, layout.Scale);
            DrawStar(_snapshot, center, layout.Scale);
            DrawInfrastructure(_snapshot, center, layout.Scale);
            // Retain the orbital context as the selected GPU disc approaches; restore it
            // along the same camera path on Back rather than switching whole layers at once.
            foreach (var body in _snapshot.Bodies)
                if (body.Kind == PlanetaryBodyKind.Planet && body.BodyId != _renderedFocusBodyId)
                    DrawBody(body, center, layout);
            foreach (var body in _snapshot.Bodies)
                if (body.Kind == PlanetaryBodyKind.Moon && body.BodyId != _renderedFocusBodyId)
                    DrawBody(body, center, layout);
        }
        _drawOpacity = 1;
        DrawSelectionCaption(viewport);
        if (!IsPlanetFocused && _focusedPlanetView is null) DrawSelectedWorldPortrait();
    }

    private void DrawSpace(Vector2 size)
    {
        DrawRect(new Rect2(Vector2.Zero, size), CanvasColor);
        SpaceArtwork.DrawNebula(this, size, Vector2.Zero, .36f);
        for (var layer = 12; layer >= 1; layer--)
            DrawCircle(new Vector2(size.X * 0.55f, size.Y * 0.52f), (58.0f + layer * 24.0f), new Color(0.14f, 0.26f, 0.36f, 0.009f));
        for (uint index = 1; index <= 140; index++)
        {
            var hash = index * 2654435761u;
            var x = (hash & 0xFFFFu) / 65535.0f * size.X;
            hash = unchecked(hash * 2246822519u + 3266489917u);
            var y = (hash & 0xFFFFu) / 65535.0f * size.Y;
            DrawCircle(new Vector2(x, y), index % 7 == 0 ? 0.8f : 0.5f, new Color(0.61f, 0.71f, 0.88f, 0.18f));
        }
    }

    private void DrawHeader(SystemSpatialSnapshot snapshot)
    {
        DrawLine(new Vector2(112.0f, 130.0f), new Vector2(136.0f, 130.0f), SelectedColor, 2.0f, true);
        DrawString(_font, new Vector2(148.0f, 135.0f), IsPlanetFocused ? "PLANET FOCUS" : "ORBITAL SYSTEM", HorizontalAlignment.Left, -1, 10, SelectedColor);
        DrawString(_font, new Vector2(112.0f, 163.0f), snapshot.CatalogName, HorizontalAlignment.Left, -1, 24, PrimaryTextColor);
        var complete = snapshot.SurveyLevel == SystemSurveyLevel.FullySurveyed;
        DrawString(_font, new Vector2(112.0f, 185.0f), complete ? "SURVEY COMPLETE" : $"RECONNAISSANCE  ·  SURVEY {snapshot.SurveyProgress:P0}",
            HorizontalAlignment.Left, -1, 11, complete ? ActivityColor : UnknownColor);
    }

    private void DrawOrbits(SystemSpatialSnapshot snapshot, Vector2 center, float scale)
    {
        foreach (var body in snapshot.Bodies)
        {
            var highlighted = body.BodyId == _selectedBodyId || body.BodyId == _hoveredBodyId;
            if (body.Kind == PlanetaryBodyKind.Planet)
            {
                DrawCircle(center, body.OrbitRadius * scale, WithAlpha(highlighted ? SelectedColor : KeylineColor, highlighted ? 0.42f : 0.35f), false, 1.0f, true);
                continue;
            }
            if (scale < 0.40f || body.ParentBodyId is not int parentId || !_bodiesById.TryGetValue(parentId, out var parent))
                continue;
            DrawCircle(ToScreen(parent, center, scale), body.OrbitRadius * scale,
                WithAlpha(highlighted ? SelectedColor : KeylineColor, highlighted ? 0.44f : 0.20f), false, 0.75f, true);
        }
    }

    private void DrawStar(SystemSpatialSnapshot snapshot, Vector2 center, float scale)
    {
        var radius = Math.Max(18.0f, 27.0f * scale);
        if (snapshot.StarArchetype is null)
        {
            // Reconnaissance establishes an orbital center, never an undiscovered stellar class.
            DrawCircle(center, radius + 8.0f, WithAlpha(UnknownColor, 0.045f));
            DrawCircle(center, radius, Fade(new Color(0.07f, 0.09f, 0.12f)));
            DrawCircle(center, radius, WithAlpha(UnknownColor, 0.78f), false, 1.5f, true);
            DrawArc(center, radius + 5.0f, -0.7f, 0.7f, 20, WithAlpha(UnknownColor, 0.40f), 1.0f, true);
            DrawString(_font, center + new Vector2(-4.0f, 5.0f), "?", HorizontalAlignment.Left, -1, 15, Fade(UnknownColor));
            return;
        }
        if (snapshot.StarArchetype == StarArchetype.BlackHole)
        {
            for (var glow = 9; glow > 0; glow--)
                DrawCircle(center, radius + glow * 2.2f, Fade(new Color(0.58f, 0.67f, 0.85f, 0.025f)));
            DrawCircle(center, radius + 2.0f, Fade(new Color(0.74f, 0.79f, 0.94f)));
            DrawCircle(center, radius, Fade(new Color(0.003f, 0.006f, 0.014f)));
            DrawArc(center, radius + 8.0f, -0.3f, 2.7f, 48, Fade(new Color(0.73f, 0.79f, 0.92f, 0.55f)), 2.0f, true);
            return;
        }
        var color = snapshot.StarArchetype == StarArchetype.NeutronPulsar
            ? new Color(0.54f, 0.79f, 1.0f) : new Color(1.0f, 0.72f, 0.34f);
        for (var glow = 13; glow > 0; glow--)
            DrawCircle(center, radius + glow * 3.0f, WithAlpha(color, 0.010f + (13 - glow) * 0.003f));
        DrawCircle(center, radius, Fade(color));
        for (var layer = 8; layer > 0; layer--)
        {
            var amount = (9.0f - layer) / 9.0f;
            DrawCircle(center + new Vector2(-radius * 0.13f, -radius * 0.13f), radius * (0.20f + layer * 0.075f),
                Fade(color.Lerp(new Color(1.0f, 0.97f, 0.79f), amount)));
        }
        DrawArc(center, radius + 1.0f, 0.1f, 2.6f, 42, WithAlpha(new Color(1.0f, 0.83f, 0.52f), 0.80f), 1.0f, true);
    }

    private void DrawInfrastructure(SystemSpatialSnapshot snapshot, Vector2 center, float scale)
    {
        if (snapshot.Infrastructure is not { Count: > 0 } infrastructure) return;
        for (var index = 0; index < infrastructure.Count; index++)
        {
            var marker = infrastructure[index];
            var position = InfrastructurePosition(center, scale, index);
            var color = marker.State switch
            {
                SystemSpatialInfrastructureState.Complete => ActivityColor,
                SystemSpatialInfrastructureState.Active => SelectedColor,
                SystemSpatialInfrastructureState.Available => ResourceColor,
                _ => UnknownColor,
            };
            DrawLine(center + (position - center).Normalized() * 30.0f, position, WithAlpha(color, 0.24f), 1.0f, true);
            if (marker.ProjectId == "orbital_shipyard") DrawShipyard(position, color);
            else if (marker.ProjectId == "asteroid_resource_network") DrawResourceNetwork(position, color);
            else DrawLaunchComplex(position, color);
            if (marker.State == SystemSpatialInfrastructureState.Active)
                DrawArc(position, 13.0f, -MathF.PI / 2, -MathF.PI / 2 + MathF.Tau * (float)marker.Progress,
                    28, WithAlpha(color, 0.95f), 2.0f, true);
            var state = marker.State.ToString().ToUpperInvariant();
            var label = marker.Label.Replace("Orbital ", string.Empty, StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
            DrawString(_font, position + new Vector2(17, -1), label, HorizontalAlignment.Left, -1, 9, Fade(PrimaryTextColor));
            DrawString(_font, position + new Vector2(17, 11), state, HorizontalAlignment.Left, -1, 8, Fade(color));
        }
    }

    private SystemSpatialInfrastructureMarker? HitInfrastructure(Vector2 point)
    {
        if (_snapshot?.Infrastructure is not { } infrastructure) return null;
        var layout = CurrentViewport;
        var center = new Vector2(layout.CenterX, layout.CenterY);
        for (var index = 0; index < infrastructure.Count; index++)
            if (point.DistanceTo(InfrastructurePosition(center, layout.Scale, index)) <= 16.0f)
                return infrastructure[index];
        return null;
    }

    private static Vector2 InfrastructurePosition(Vector2 center, float scale, int index)
    {
        var radius = Math.Clamp(62.0f * scale, 48.0f, 82.0f);
        var angle = -0.78f + index * 1.18f;
        return center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
    }

    private void DrawLaunchComplex(Vector2 position, Color color)
    {
        DrawCircle(position, 9.0f, Fade(new Color(0.03f, 0.07f, 0.11f)));
        DrawArc(position, 9.0f, 0, MathF.Tau, 24, WithAlpha(color, 0.85f), 1.2f, true);
        DrawLine(position + new Vector2(-5, 5), position + new Vector2(0, -7), Fade(color), 1.5f, true);
        DrawLine(position + new Vector2(0, -7), position + new Vector2(5, 5), Fade(color), 1.5f, true);
        DrawLine(position + new Vector2(-6, 5), position + new Vector2(6, 5), Fade(color), 1.5f, true);
    }

    private void DrawShipyard(Vector2 position, Color color)
    {
        DrawRect(new Rect2(position - new Vector2(9, 6), new Vector2(18, 12)), Fade(new Color(0.03f, 0.07f, 0.11f)), true);
        DrawRect(new Rect2(position - new Vector2(9, 6), new Vector2(18, 12)), WithAlpha(color, 0.85f), false, 1.2f);
        DrawLine(position + new Vector2(-4, -9), position + new Vector2(-4, 9), Fade(color), 1.5f, true);
        DrawLine(position + new Vector2(4, -9), position + new Vector2(4, 9), Fade(color), 1.5f, true);
        DrawLine(position + new Vector2(-8, 0), position + new Vector2(8, 0), Fade(color), 1.2f, true);
    }

    private void DrawResourceNetwork(Vector2 position, Color color)
    {
        DrawCircle(position, 3.5f, Fade(color));
        for (var index = 0; index < 3; index++)
        {
            var angle = index * MathF.Tau / 3.0f - 0.4f;
            var asteroid = position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 9.0f;
            DrawLine(position, asteroid, WithAlpha(color, 0.65f), 1.1f, true);
            DrawCircle(asteroid, index == 0 ? 3.0f : 2.2f, Fade(new Color(color.R * 0.75f, color.G * 0.75f, color.B * 0.75f)));
        }
    }

    private void DrawBody(SystemSpatialBodyMarker body, Vector2 center, SystemSpatialViewport layout)
    {
        var position = ToScreen(body, center, layout.Scale);
        var radius = layout.BodyRadius(body);
        var selected = body.BodyId == _selectedBodyId;
        var hovered = body.BodyId == _hoveredBodyId;
        var known = _surfaces.TryGetValue(body.BodyId, out var surface);
        if (known && body.SurfaceKey == "saturn") DrawSaturnRings(position, radius, front: false);
        if (known)
        {
            if (body.VisualClass is SystemSpatialBodyVisualClass.Oceanic or SystemSpatialBodyVisualClass.GasGiant or SystemSpatialBodyVisualClass.IceGiant)
            {
                DrawCircle(position, radius + 3.0f, WithAlpha(ResolveBodyColor(body.VisualClass), 0.09f));
                DrawCircle(position, radius + 1.2f, WithAlpha(ResolveBodyColor(body.VisualClass), 0.27f), false, 1.1f, true);
            }
            DrawTextureRect(surface.Texture, new Rect2(position - Vector2.One * radius, Vector2.One * radius * 2.0f), false, Fade(Colors.White));
        }
        else
        {
            DrawCircle(position, radius, Fade(new Color(0.065f, 0.087f, 0.115f)));
            DrawCircle(position, radius, WithAlpha(UnknownColor, 0.72f), false, 1.1f, true);
            DrawArc(position, radius - 2.0f, 2.8f, 4.6f, 16, WithAlpha(UnknownColor, 0.27f), 1.0f, true);
        }
        if (known && body.SurfaceKey == "saturn") DrawSaturnRings(position, radius, front: true);
        if (selected || hovered)
        {
            var color = WithAlpha(SelectedColor, selected ? 1.0f : 0.62f);
            for (var side = 0; side < 4; side++)
                DrawArc(position, radius + 5.0f, side * MathF.PI * 0.5f + 0.18f, side * MathF.PI * 0.5f + 1.18f, 12, color, selected ? 1.8f : 1.2f, true);
        }
        DrawSignatures(body, position, radius);
        if (body.Kind == PlanetaryBodyKind.Planet || hovered || selected)
        {
            var textPosition = position + new Vector2(radius + 8.0f, -radius - 2.0f);
            var labelSize = selected || hovered ? 13 : 11;
            if (body.Kind == PlanetaryBodyKind.Planet && body.OrbitIndex == 0 && body.OffsetX < 0)
                textPosition.X = position.X - radius - 8 - _font.GetStringSize(body.Label, HorizontalAlignment.Left, -1, labelSize).X;
            DrawString(_font, textPosition + Vector2.One, body.Label, HorizontalAlignment.Left, -1, labelSize, Fade(CanvasColor));
            DrawString(_font, textPosition, body.Label, HorizontalAlignment.Left, -1, labelSize,
                Fade(selected || hovered ? PrimaryTextColor : SecondaryTextColor));
        }
    }

    private void DrawSelectionCaption(Vector2 viewport)
    {
        var id = _hoveredBodyId ?? _selectedBodyId;
        if (id is int bodyId && _bodiesById.TryGetValue(bodyId, out var body))
        {
            var className = body.VisualClass switch
            {
                SystemSpatialBodyVisualClass.UnknownPlanet => "Planet · environment unconfirmed",
                SystemSpatialBodyVisualClass.UnknownMoon => "Moon · environment unconfirmed",
                SystemSpatialBodyVisualClass.HotRocky => "Hot rocky world",
                SystemSpatialBodyVisualClass.GasGiant => "Gas giant",
                SystemSpatialBodyVisualClass.IceGiant => "Ice giant",
                _ => body.VisualClass.ToString(),
            };
            DrawString(_font, new Vector2(112.0f, viewport.Y - 154.0f), body.Label, HorizontalAlignment.Left, -1, 16, PrimaryTextColor);
            DrawString(_font, new Vector2(112.0f, viewport.Y - 135.0f), className, HorizontalAlignment.Left, -1, 11, SecondaryTextColor);
        }
        else
            DrawString(_font, new Vector2(112.0f, viewport.Y - 135.0f), "Select a world to inspect  ·  Orbital distances shown schematically",
                HorizontalAlignment.Left, -1, 11, MutedTextColor);
    }

    private void DrawSelectedWorldPortrait()
    {
        if (_selectedBodyId is not int id || !_bodiesById.TryGetValue(id, out var body)) return;
        var center = new Vector2(250, 296);
        const float radius = 52;
        if (_surfaces.TryGetValue(id, out var surface))
        {
            DrawCircle(center, radius + 7, WithAlpha(SelectedColor, 0.04f));
            if (body.SurfaceKey == "saturn") DrawSaturnRings(center, radius, front: false);
            DrawTextureRect(surface.Texture, new Rect2(center - Vector2.One * radius, Vector2.One * radius * 2), false);
            if (body.SurfaceKey == "saturn") DrawSaturnRings(center, radius, front: true);
        }
        else
        {
            DrawCircle(center, radius, CanvasColor);
            DrawCircle(center, radius, UnknownColor, false, 1.4f, true);
        }
        DrawString(_font, new Vector2(160, 367), body.Label, HorizontalAlignment.Left, 230, 22, PrimaryTextColor);
        var caption = body.SurfaceKey == "earth" ? "HUMAN HOMEWORLD" :
            body.SurfaceKey is not null ? "SOL SYSTEM" : body.HasDetailedEnvironment ? "SURVEYED WORLD" : "UNCONFIRMED ENVIRONMENT";
        DrawString(_font, new Vector2(160, 388), caption, HorizontalAlignment.Left, 230, 10, SelectedColor);
        var scale = body.MassEarth is double mass && body.GravityG is double gravity
            ? $"{body.RadiusEarth:0.00} R⊕  ·  {mass:0.00} M⊕  ·  {gravity:0.00} g"
            : $"{body.RadiusEarth:0.00} Earth radii  ·  detailed survey required";
        DrawString(_font, new Vector2(160, 409), scale, HorizontalAlignment.Left, 230, 10, SecondaryTextColor);
        var moonCount = _bodiesById.Values.Count(candidate => candidate.ParentBodyId == body.BodyId);
        var family = body.ParentBodyId is int parentId && _bodiesById.TryGetValue(parentId, out var parent)
            ? $"MOON OF {parent.Label.ToUpperInvariant()}"
            : moonCount == 1 ? "1 NATURAL SATELLITE" : $"{moonCount} NATURAL SATELLITES";
        DrawString(_font, new Vector2(160, 427), family, HorizontalAlignment.Left, 230, 10, MutedTextColor);
        if (body.TemperatureKelvin is double temperature && body.PressureKPa is double pressure)
        {
            var atmosphere = body.Atmosphere?.ToString().Replace("Rich", " rich", StringComparison.Ordinal) ?? "Unknown";
            DrawString(_font, new Vector2(160, 445), $"{temperature:0} K  ·  {pressure:0.#} kPa  ·  {atmosphere}",
                HorizontalAlignment.Left, 230, 10, SecondaryTextColor);
        }
    }

    private void DrawSaturnRings(Vector2 center, float radius, bool front)
    {
        const float tilt = -0.36f;
        for (var band = 0; band < 16; band++)
        {
            if (band is 9 or 10) continue; // Visible Cassini division in the schematic ring plane.
            var distance = radius * (1.25f + band * 0.065f);
            var points = new Vector2[49];
            for (var point = 0; point < points.Length; point++)
            {
                var angle = (front ? 0 : MathF.PI) + point / 48f * MathF.PI;
                points[point] = center + new Vector2(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance * 0.33f).Rotated(tilt);
            }
            DrawPolyline(points, Fade(new Color(0.76f, 0.70f, 0.55f, front ? 0.72f : 0.44f)), Math.Max(0.65f, radius * 0.055f), true);
        }
    }

    private void DrawSignatures(SystemSpatialBodyMarker body, Vector2 position, float radius)
    {
        var anchor = position + new Vector2(radius + 7.0f, radius + 4.0f);
        var index = 0;
        if (body.PositiveResourceSignature)
        {
            var point = anchor + new Vector2(index++ * 11.0f, 0.0f);
            DrawLine(point + new Vector2(0, -3), point + new Vector2(3, 0), Fade(ResourceColor), 1.3f, true);
            DrawLine(point + new Vector2(3, 0), point + new Vector2(0, 3), Fade(ResourceColor), 1.3f, true);
            DrawLine(point + new Vector2(0, 3), point + new Vector2(-3, 0), Fade(ResourceColor), 1.3f, true);
            DrawLine(point + new Vector2(-3, 0), point + new Vector2(0, -3), Fade(ResourceColor), 1.3f, true);
        }
        if (body.PositiveAnomalySignature)
        {
            var point = anchor + new Vector2(index++ * 11.0f, 0.0f);
            DrawLine(point + new Vector2(0, -3), point + new Vector2(3, 3), Fade(AnomalyColor), 1.3f, true);
            DrawLine(point + new Vector2(3, 3), point + new Vector2(-3, 3), Fade(AnomalyColor), 1.3f, true);
            DrawLine(point + new Vector2(-3, 3), point + new Vector2(0, -3), Fade(AnomalyColor), 1.3f, true);
        }
        if (body.PositiveActivitySignature)
            DrawRect(new Rect2(anchor + new Vector2(index * 11.0f - 3.0f, -3.0f), Vector2.One * 6.0f), Fade(ActivityColor), false, 1.2f);
    }

    private static ImageTexture CreateSurface(SystemSpatialBodyMarker body)
    {
        var resolution = body.SurfaceKey is null ? 96 : 256;
        using var image = Image.CreateEmpty(resolution, resolution, false, Image.Format.Rgba8);
        using var source = SolBodyMaterials.LoadColorSource(body.SurfaceKey);
        var baseColor = ResolveBodyColor(body.VisualClass);
        var towardStar = new Vector2(-body.OffsetX, -body.OffsetY).Normalized();
        var phase = (body.BodyId & 255) * 0.137f;
        for (var y = 0; y < resolution; y++)
        {
            for (var x = 0; x < resolution; x++)
            {
                var nx = ((x + 0.5f) / resolution - 0.5f) * 2.0f;
                var ny = ((y + 0.5f) / resolution - 0.5f) * 2.0f;
                var radial = nx * nx + ny * ny;
                if (radial > 1.0f)
                    continue;
                var nz = MathF.Sqrt(1.0f - radial);
                var light = Math.Max(0.0f, nx * towardStar.X * 0.85f + ny * towardStar.Y * 0.85f + nz * 0.53f);
                var color = baseColor;
                // Decorative class-level material, never a map of authoritative surface features.
                var detail = MathF.Sin(nx * 9.0f + phase) * MathF.Sin(ny * 8.0f - phase) + MathF.Sin((nx + ny) * 17.0f + phase) * 0.28f;
                if (source is not null)
                    color = SolBodyMaterials.Sample(source, body.SurfaceKey!, nx, ny, nz);
                else if (body.VisualClass is SystemSpatialBodyVisualClass.GasGiant or SystemSpatialBodyVisualClass.IceGiant)
                {
                    var band = MathF.Sin(ny * 27.0f + MathF.Sin(nx * 6.0f + phase) * 0.55f);
                    color = baseColor.Lerp(new Color(0.92f, 0.82f, 0.64f), Math.Max(0.0f, band) * 0.27f);
                    color = color.Darkened(Math.Max(0.0f, -band) * 0.25f);
                }
                else if (body.VisualClass == SystemSpatialBodyVisualClass.Oceanic)
                {
                    color = baseColor.Lightened(Math.Max(0.0f, detail) * 0.18f);
                    var cloud = MathF.Sin(ny * 19.0f + MathF.Sin(nx * 10.0f + phase) * 2.0f);
                    color = color.Lerp(new Color(0.78f, 0.89f, 0.94f), Math.Max(0.0f, cloud - 0.50f) * 0.50f);
                }
                else
                    color = baseColor.Lightened(detail * 0.09f);
                // Disc photographs already contain their observed illumination.
                var illumination = source is not null && body.SurfaceKey is ("earth" or "mercury" or "venus" or "uranus" or "moon")
                    ? 0.90f + nz * 0.10f : 0.18f + light * 0.87f;
                color = new Color(color.R * illumination, color.G * illumination, color.B * illumination,
                    Math.Clamp((1.0f - radial) * resolution * 0.55f, 0.0f, 1.0f));
                image.SetPixel(x, y, color);
            }
        }
        return ImageTexture.CreateFromImage(image);
    }

    private static Color ResolveBodyColor(SystemSpatialBodyVisualClass visualClass) => visualClass switch
    {
        SystemSpatialBodyVisualClass.Rocky => new Color(0.64f, 0.57f, 0.47f),
        SystemSpatialBodyVisualClass.Oceanic => new Color(0.16f, 0.48f, 0.75f),
        SystemSpatialBodyVisualClass.Frozen => new Color(0.69f, 0.84f, 0.89f),
        SystemSpatialBodyVisualClass.HotRocky => new Color(0.82f, 0.40f, 0.23f),
        SystemSpatialBodyVisualClass.GasGiant => new Color(0.80f, 0.63f, 0.43f),
        SystemSpatialBodyVisualClass.IceGiant => new Color(0.31f, 0.69f, 0.82f),
        SystemSpatialBodyVisualClass.Moon => new Color(0.64f, 0.65f, 0.67f),
        _ => UnknownColor,
    };

    private void ClearHover()
    {
        _hoveredBodyId = null;
        MouseDefaultCursorShape = CursorShape.Arrow;
        QueueRedraw();
    }

    private void ClearSurfaces()
    {
        foreach (var surface in _surfaces.Values)
            surface.Texture.Dispose();
        _surfaces.Clear();
    }

    private void ResizeToViewport()
    {
        _lastViewportSize = GetViewportRect().Size;
        Position = Vector2.Zero;
        Size = _lastViewportSize;
    }

    private static Vector2 ToScreen(SystemSpatialBodyMarker marker, Vector2 center, float scale) =>
        center + new Vector2(marker.OffsetX, marker.OffsetY) * scale;
    private Color WithAlpha(Color color, float alpha) => new(color.R, color.G, color.B, alpha * _drawOpacity);
    private Color Fade(Color color) => new(color.R, color.G, color.B, color.A * _drawOpacity);
}

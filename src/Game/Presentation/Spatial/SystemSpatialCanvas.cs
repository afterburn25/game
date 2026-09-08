using System;
using System.Linq;
using Godot;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Presentation.Spatial;

/// <summary>
/// Full-viewport schematic system canvas. It is deliberately presentation-only: all content
/// arrives as an observer-safe SystemSpatialSnapshot and all orbital spacing is display geometry.
/// </summary>
public partial class SystemSpatialCanvas : Control
{
    private static readonly Color CanvasColor = new(5f / 255f, 11f / 255f, 18f / 255f);
    private static readonly Color KeylineColor = new(39f / 255f, 67f / 255f, 89f / 255f);
    private static readonly Color PrimaryTextColor = new(230f / 255f, 240f / 255f, 246f / 255f);
    private static readonly Color SecondaryTextColor = new(169f / 255f, 187f / 255f, 200f / 255f);
    private static readonly Color MutedTextColor = new(111f / 255f, 132f / 255f, 148f / 255f);
    private static readonly Color SelectedColor = new(88f / 255f, 207f / 255f, 251f / 255f);
    private static readonly Color UnknownColor = new(139f / 255f, 130f / 255f, 162f / 255f);
    private static readonly Color ResourceColor = new(233f / 255f, 182f / 255f, 92f / 255f);
    private static readonly Color AnomalyColor = new(175f / 255f, 143f / 255f, 255f / 255f);
    private static readonly Color ActivityColor = new(95f / 255f, 210f / 255f, 192f / 255f);

    private SystemSpatialSnapshot? _snapshot;
    private Font _font = null!;
    private Vector2 _lastViewportSize;

    public event Action? ReturnRequested;

    public override void _Ready()
    {
        _font = ThemeDB.FallbackFont;
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.None;
        ResizeToViewport();
    }

    public override void _Process(double delta)
    {
        _ = delta;
        var viewportSize = GetViewportRect().Size;
        if (viewportSize != _lastViewportSize)
        {
            ResizeToViewport();
            QueueRedraw();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse &&
            mouse.Pressed &&
            mouse.ButtonIndex == MouseButton.Left &&
            mouse.DoubleClick)
        {
            ReturnRequested?.Invoke();
        }

        // While visible this canvas owns empty-space pointer input. Higher CanvasLayer UI controls
        // still receive their own GUI events, while hidden stellar-map orders cannot fire through it.
        AcceptEvent();
    }

    public void SetSnapshot(SystemSpatialSnapshot? snapshot)
    {
        _snapshot = snapshot;
        Visible = snapshot is not null;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_snapshot is null)
            return;

        var viewportSize = Size;
        DrawRect(new Rect2(Vector2.Zero, viewportSize), CanvasColor, true);

        var center = new Vector2(viewportSize.X * 0.50f, viewportSize.Y * 0.53f);
        var availableRadius = Math.Max(130.0f, Math.Min(viewportSize.X * 0.42f, viewportSize.Y * 0.37f));
        var scale = Math.Clamp(availableRadius / _snapshot.DesignRadius, 0.38f, 1.15f);

        DrawHeader(_snapshot, viewportSize);
        DrawPlanetOrbits(_snapshot, center, scale);
        DrawStar(_snapshot, center, scale);
        DrawMoonOrbits(_snapshot, center, scale);

        foreach (var body in _snapshot.Bodies.Where(marker => marker.Kind == PlanetaryBodyKind.Planet))
            DrawBody(body, center, scale);
        foreach (var body in _snapshot.Bodies.Where(marker => marker.Kind == PlanetaryBodyKind.Moon))
            DrawBody(body, center, scale);

        DrawFooter(_snapshot, viewportSize);
    }

    private void DrawHeader(SystemSpatialSnapshot snapshot, Vector2 viewportSize)
    {
        var y = Math.Clamp(viewportSize.Y * 0.18f, 106.0f, 174.0f);
        DrawString(
            _font,
            new Vector2(0.0f, y),
            $"SYSTEM VIEW  |  {snapshot.CatalogName}",
            HorizontalAlignment.Center,
            viewportSize.X,
            20,
            PrimaryTextColor);

        var surveyText = snapshot.SurveyLevel == SystemSurveyLevel.FullySurveyed
            ? "Science survey complete - confirmed environment/classification data enabled"
            : $"Reconnaissance catalog - survey {snapshot.SurveyProgress:P0}; detailed environment/classification remains withheld";
        DrawString(
            _font,
            new Vector2(0.0f, y + 24.0f),
            surveyText,
            HorizontalAlignment.Center,
            viewportSize.X,
            13,
            snapshot.SurveyLevel == SystemSurveyLevel.FullySurveyed ? SecondaryTextColor : UnknownColor);
    }

    private void DrawPlanetOrbits(SystemSpatialSnapshot snapshot, Vector2 center, float scale)
    {
        foreach (var body in snapshot.Bodies.Where(marker => marker.Kind == PlanetaryBodyKind.Planet))
        {
            DrawCircle(
                center,
                body.OrbitRadius * scale,
                WithAlpha(KeylineColor, 0.54f),
                false,
                Math.Max(0.7f, scale));
        }
    }

    private void DrawMoonOrbits(SystemSpatialSnapshot snapshot, Vector2 center, float scale)
    {
        if (scale < 0.52f)
            return;

        var byId = snapshot.Bodies.ToDictionary(marker => marker.BodyId);
        foreach (var moon in snapshot.Bodies.Where(marker => marker.Kind == PlanetaryBodyKind.Moon))
        {
            if (moon.ParentBodyId is not int parentId || !byId.TryGetValue(parentId, out var parent))
                continue;
            var parentPosition = ToScreen(parent, center, scale);
            DrawCircle(
                parentPosition,
                moon.OrbitRadius * scale,
                WithAlpha(KeylineColor, 0.40f),
                false,
                0.75f);
        }
    }

    private void DrawStar(SystemSpatialSnapshot snapshot, Vector2 center, float scale)
    {
        var selectedWidth = Math.Max(1.2f, 1.8f * scale);
        DrawCircle(center, Math.Max(26.0f, 42.0f * scale), WithAlpha(SelectedColor, 0.07f));
        DrawCircle(center, Math.Max(22.0f, 34.0f * scale), WithAlpha(SelectedColor, 0.13f));
        DrawCircle(center, Math.Max(31.0f, 48.0f * scale), WithAlpha(SelectedColor, 0.72f), false, selectedWidth);

        if (snapshot.StarArchetype == StarArchetype.BlackHole)
        {
            DrawCircle(center, Math.Max(15.0f, 21.0f * scale), new Color(0.005f, 0.008f, 0.012f));
            DrawCircle(center, Math.Max(19.0f, 25.0f * scale), new Color(0.68f, 0.76f, 0.86f, 0.40f), false, 2.0f);
            return;
        }

        var starColor = snapshot.StarArchetype == StarArchetype.NeutronPulsar
            ? new Color(0.74f, 0.90f, 1.0f)
            : new Color(1.0f, 0.86f, 0.58f);
        DrawCircle(center, Math.Max(14.0f, 22.0f * scale), WithAlpha(starColor, 0.22f));
        DrawCircle(center, Math.Max(10.0f, 16.0f * scale), starColor);
        DrawCircle(center, Math.Max(4.5f, 7.0f * scale), new Color(1.0f, 0.97f, 0.88f));
    }

    private void DrawBody(SystemSpatialBodyMarker body, Vector2 center, float scale)
    {
        var position = ToScreen(body, center, scale);
        var radius = Math.Max(2.5f, body.DisplayRadius * scale);
        var color = ResolveBodyColor(body.VisualClass);

        switch (body.VisualClass)
        {
            case SystemSpatialBodyVisualClass.UnknownPlanet:
                DrawCircle(position, radius, WithAlpha(UnknownColor, 0.28f));
                DrawCircle(position, radius, UnknownColor, false, 1.2f);
                DrawLine(position + new Vector2(-radius * 0.65f, 0.0f), position + new Vector2(radius * 0.65f, 0.0f), UnknownColor, 1.0f);
                break;
            case SystemSpatialBodyVisualClass.UnknownMoon:
                DrawCircle(position, radius, WithAlpha(UnknownColor, 0.14f));
                DrawCircle(position, radius, UnknownColor, false, 1.0f);
                break;
            default:
                DrawCircle(position, radius, color);
                DrawCircle(position, radius + 1.4f, WithAlpha(PrimaryTextColor, 0.28f), false, 0.8f);
                if (body.VisualClass is SystemSpatialBodyVisualClass.GasGiant or SystemSpatialBodyVisualClass.IceGiant)
                {
                    DrawLine(
                        position + new Vector2(-radius * 0.80f, 0.0f),
                        position + new Vector2(radius * 0.80f, 0.0f),
                        WithAlpha(PrimaryTextColor, 0.52f),
                        1.0f);
                }
                break;
        }

        DrawSignatures(body, position, radius);

        if (body.Kind == PlanetaryBodyKind.Planet || scale >= 0.66f)
        {
            DrawString(
                _font,
                position + new Vector2(radius + 6.0f, -radius - 2.0f),
                body.Label,
                HorizontalAlignment.Left,
                -1,
                body.Kind == PlanetaryBodyKind.Planet ? 12 : 11,
                body.Kind == PlanetaryBodyKind.Planet ? SecondaryTextColor : MutedTextColor);
        }
    }

    private void DrawSignatures(SystemSpatialBodyMarker body, Vector2 position, float bodyRadius)
    {
        var anchor = position + new Vector2(bodyRadius + 5.0f, bodyRadius + 5.0f);
        var index = 0;
        if (body.PositiveResourceSignature)
        {
            DrawDiamond(anchor + new Vector2(index++ * 10.0f, 0.0f), 3.2f, ResourceColor);
        }
        if (body.PositiveAnomalySignature)
        {
            DrawTriangle(anchor + new Vector2(index++ * 10.0f, 0.0f), 3.4f, AnomalyColor);
        }
        if (body.PositiveActivitySignature)
        {
            var point = anchor + new Vector2(index * 10.0f, 0.0f);
            DrawRect(new Rect2(point - new Vector2(3.0f, 3.0f), new Vector2(6.0f, 6.0f)), ActivityColor, false, 1.2f);
        }
    }

    private void DrawFooter(SystemSpatialSnapshot snapshot, Vector2 viewportSize)
    {
        var legend = snapshot.Bodies.Any(body => body.PositiveResourceSignature || body.PositiveAnomalySignature || body.PositiveActivitySignature)
            ? "Marker shapes: diamond resource signature | triangle anomaly | square activity"
            : "No positive resource / anomaly / activity signatures currently displayed";
        DrawString(_font, new Vector2(18.0f, viewportSize.Y - 42.0f), legend, HorizontalAlignment.Left, viewportSize.X - 36.0f, 12, MutedTextColor);
        DrawString(
            _font,
            new Vector2(18.0f, viewportSize.Y - 20.0f),
            "Double-click empty system space to return to the stellar map. Orbital spacing and object size are schematic display scales; simulation travel timing is unchanged.",
            HorizontalAlignment.Left,
            viewportSize.X - 36.0f,
            12,
            SecondaryTextColor);
    }

    private static Vector2 ToScreen(SystemSpatialBodyMarker marker, Vector2 center, float scale) =>
        center + new Vector2(marker.OffsetX, marker.OffsetY) * scale;

    private static Color ResolveBodyColor(SystemSpatialBodyVisualClass visualClass) => visualClass switch
    {
        SystemSpatialBodyVisualClass.Rocky => new Color(0.52f, 0.57f, 0.61f),
        SystemSpatialBodyVisualClass.Oceanic => new Color(0.24f, 0.55f, 0.78f),
        SystemSpatialBodyVisualClass.Frozen => new Color(0.66f, 0.82f, 0.90f),
        SystemSpatialBodyVisualClass.HotRocky => new Color(0.82f, 0.47f, 0.28f),
        SystemSpatialBodyVisualClass.GasGiant => new Color(0.74f, 0.62f, 0.46f),
        SystemSpatialBodyVisualClass.IceGiant => new Color(0.39f, 0.69f, 0.82f),
        SystemSpatialBodyVisualClass.Moon => new Color(0.60f, 0.63f, 0.66f),
        _ => UnknownColor,
    };

    private void DrawDiamond(Vector2 center, float radius, Color color)
    {
        var top = center + new Vector2(0.0f, -radius);
        var right = center + new Vector2(radius, 0.0f);
        var bottom = center + new Vector2(0.0f, radius);
        var left = center + new Vector2(-radius, 0.0f);
        DrawLine(top, right, color, 1.2f);
        DrawLine(right, bottom, color, 1.2f);
        DrawLine(bottom, left, color, 1.2f);
        DrawLine(left, top, color, 1.2f);
    }

    private void DrawTriangle(Vector2 center, float radius, Color color)
    {
        var top = center + new Vector2(0.0f, -radius);
        var right = center + new Vector2(radius, radius);
        var left = center + new Vector2(-radius, radius);
        DrawLine(top, right, color, 1.2f);
        DrawLine(right, left, color, 1.2f);
        DrawLine(left, top, color, 1.2f);
    }

    private void ResizeToViewport()
    {
        _lastViewportSize = GetViewportRect().Size;
        Position = Vector2.Zero;
        Size = _lastViewportSize;
    }

    private static Color WithAlpha(Color color, float alpha) =>
        new(color.R, color.G, color.B, alpha);
}

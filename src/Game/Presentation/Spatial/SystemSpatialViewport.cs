using System;
using Game.Simulation.Models;

namespace Game.Presentation.Spatial;

/// <summary>Shared schematic transform and visible object radii for drawing and pointer targeting.</summary>
public readonly record struct SystemSpatialViewport(float CenterX, float CenterY, float Scale)
{
    public static SystemSpatialViewport Fit(SystemSpatialSnapshot snapshot, float width, float height)
    {
        // Fit against the actual orbital-safe rectangle instead of moving its centre and
        // radius independently. The title/command strip ends above 170, while the status
        // band begins 130 px from the bottom. At 1280×720 this yields (580, 380), r=210.
        var safeLeft = 104.0f;
        var safeRight = Math.Max(safeLeft + 2.0f, width - 224.0f);
        var safeTop = Math.Min(170.0f, Math.Max(0.0f, height - 131.0f));
        var safeBottom = Math.Max(safeTop + 1.0f, height - 130.0f);
        var centerX = (safeLeft + safeRight) * .5f;
        var centerY = (safeTop + safeBottom) * .5f;
        var availableRadius = Math.Max(1.0f, Math.Min(
            Math.Min(centerX - safeLeft, safeRight - centerX),
            Math.Min(centerY - safeTop, safeBottom - centerY)));
        return new(centerX, centerY,
            Math.Min(availableRadius / snapshot.DesignRadius, 1.15f));
    }

    public (float X, float Y) WorldToScreen(float x, float y) => (CenterX + x * Scale, CenterY + y * Scale);
    public (float X, float Y) ScreenToWorld(float x, float y) => ((x - CenterX) / Scale, (y - CenterY) / Scale);

    public float BodyRadius(SystemSpatialBodyMarker body) => body.Kind == PlanetaryBodyKind.Moon
        ? Math.Max(3.6f, body.DisplayRadius * Scale * 1.12f)
        : Math.Max(9.0f, body.DisplayRadius * Scale * 2.25f);

    public int? HitBody(SystemSpatialSnapshot snapshot, float x, float y)
    {
        int? nearestId = null;
        var nearestDistance = float.MaxValue;
        foreach (var body in snapshot.Bodies)
        {
            var dx = x - CenterX - body.OffsetX * Scale;
            var dy = y - CenterY - body.OffsetY * Scale;
            var distance = dx * dx + dy * dy;
            var hitRadius = BodyRadius(body) * (body.SurfaceKey == "saturn" ? 2.25f : 1f) + 4.0f;
            if (distance <= hitRadius * hitRadius && distance < nearestDistance)
            {
                nearestId = body.BodyId;
                nearestDistance = distance;
            }
        }
        return nearestId;
    }

    public bool HitsCelestialObject(SystemSpatialSnapshot snapshot, float x, float y) =>
        Inside(x, y, CenterX, CenterY, Math.Max(31.0f, 48.0f * Scale)) || HitBody(snapshot, x, y).HasValue;

    private static bool Inside(float x, float y, float centerX, float centerY, float radius)
    {
        var dx = x - centerX;
        var dy = y - centerY;
        return dx * dx + dy * dy <= radius * radius;
    }
}

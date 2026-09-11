using System;
using Game.Simulation.Models;

namespace Game.Presentation.Spatial;

/// <summary>Shared schematic transform and visible object radii for drawing and pointer targeting.</summary>
public readonly record struct SystemSpatialViewport(float CenterX, float CenterY, float Scale)
{
    public static SystemSpatialViewport Fit(SystemSpatialSnapshot snapshot, float width, float height)
    {
        // The inspector occupies the right edge, while the system title only occupies the upper
        // left. Bias the orbit field left and let it use that otherwise empty lower-left space.
        var availableRadius = Math.Max(1.0f, Math.Min((width - 310.0f) * 0.47f, (height - 230.0f) * 0.53f));
        return new(width * 0.46f + 18.0f, height * 0.52f + 2.0f,
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

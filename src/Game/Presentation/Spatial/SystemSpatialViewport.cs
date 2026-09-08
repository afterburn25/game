using System;
using Game.Simulation.Models;

namespace Game.Presentation.Spatial;

/// <summary>Shared schematic transform and visible object radii for drawing and pointer targeting.</summary>
public readonly record struct SystemSpatialViewport(float CenterX, float CenterY, float Scale)
{
    public static SystemSpatialViewport Fit(SystemSpatialSnapshot snapshot, float width, float height)
    {
        // Keep the orbital field between the compact header and the command dock, beside the rail.
        var availableRadius = Math.Max(1.0f, Math.Min((width - 136.0f) * 0.46f, (height - 280.0f) * 0.5f));
        return new(width * 0.50f + 38.0f, height * 0.50f + 6.0f,
            Math.Min(availableRadius / snapshot.DesignRadius, 1.15f));
    }

    public float BodyRadius(SystemSpatialBodyMarker body) => body.Kind == PlanetaryBodyKind.Moon
        ? Math.Max(3.2f, body.DisplayRadius * Scale)
        : Math.Max(8.0f, body.DisplayRadius * Scale * 1.8f);

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

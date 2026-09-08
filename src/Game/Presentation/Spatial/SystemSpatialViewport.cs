using System;

namespace Game.Presentation.Spatial;

/// <summary>Shared schematic transform for drawing and celestial hit testing; no simulation units.</summary>
public readonly record struct SystemSpatialViewport(float CenterX, float CenterY, float Scale)
{
    public static SystemSpatialViewport Fit(SystemSpatialSnapshot snapshot, float width, float height)
    {
        var availableRadius = Math.Max(1.0f, Math.Min(width * 0.42f, height * 0.37f));
        return new(width * 0.50f, height * 0.53f, Math.Min(availableRadius / snapshot.DesignRadius, 1.15f));
    }

    public bool HitsCelestialObject(SystemSpatialSnapshot snapshot, float x, float y)
    {
        if (Inside(x, y, CenterX, CenterY, Math.Max(31.0f, 48.0f * Scale)))
            return true;

        foreach (var body in snapshot.Bodies)
        {
            if (Inside(x, y, CenterX + body.OffsetX * Scale, CenterY + body.OffsetY * Scale,
                    Math.Max(2.5f, body.DisplayRadius * Scale) + 4.0f))
                return true;
        }

        return false;
    }

    private static bool Inside(float x, float y, float centerX, float centerY, float radius)
    {
        var dx = x - centerX;
        var dy = y - centerY;
        return dx * dx + dy * dy <= radius * radius;
    }
}

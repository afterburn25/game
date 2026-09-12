using System;
using Game.Simulation.Models;

namespace Game.Presentation.Spatial;

/// <summary>Shared display units, not physical orbital distances. Preserve the visible size
/// hierarchy while allowing an entire system and its lane gates to fit on a strategy map.</summary>
public static class SystemCelestialScale
{
    public const float PrimaryStarRadius = 560f;

    public static float BodyRadius(double radiusEarth, PlanetaryBodyKind kind)
    {
        var radius = double.IsFinite(radiusEarth) ? Math.Clamp(radiusEarth, .01, 100) : .01;
        return kind == PlanetaryBodyKind.Moon
            ? Math.Clamp(14f * (float)radius, 1.8f, 24f)
            : Math.Clamp(14f * MathF.Pow((float)radius, .9f), 4f, 160f);
    }

    public static float MinimumScreenRadius(SystemSpatialBodyMarker body) =>
        body.Kind == PlanetaryBodyKind.Moon ? 2.6f :
            Math.Clamp(9.5f * MathF.Pow((float)Math.Max(.01, body.RadiusEarth), .55f), 9f, 30f);

    public static float StarScreenRadius(float scale) => Math.Max(44f, PrimaryStarRadius * scale);
}

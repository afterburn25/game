using System;
using System.Numerics;

namespace Game.Simulation.Models;

/// <summary>
/// Authoritative physical separation between star systems. Chart coordinates remain 2D for
/// presentation; older saves omit depth and therefore retain their original flat geometry.
/// </summary>
public static class InterstellarDistance
{
    public static double Between(StarSystemState first, StarSystemState second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        if (first.GalacticDepthLightYears is null && second.GalacticDepthLightYears is null)
            return Vector2.Distance(first.Position, second.Position);
        var dx = (double)first.Position.X - second.Position.X;
        var dy = (double)first.Position.Y - second.Position.Y;
        var dz = first.GalacticDepthLightYears.GetValueOrDefault() - second.GalacticDepthLightYears.GetValueOrDefault();
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static double SquaredBetween(StarSystemState first, StarSystemState second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        if (first.GalacticDepthLightYears is null && second.GalacticDepthLightYears is null)
            return Vector2.DistanceSquared(first.Position, second.Position);
        var dx = (double)first.Position.X - second.Position.X;
        var dy = (double)first.Position.Y - second.Position.Y;
        var dz = first.GalacticDepthLightYears.GetValueOrDefault() - second.GalacticDepthLightYears.GetValueOrDefault();
        return dx * dx + dy * dy + dz * dz;
    }

    public static Vector2 InterpolateChartPosition(StarSystemState origin, StarSystemState target, double progress) =>
        Vector2.Lerp(origin.Position, target.Position, (float)Math.Clamp(progress, 0.0, 1.0));
}

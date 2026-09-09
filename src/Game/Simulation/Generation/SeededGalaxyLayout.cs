using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Game.Simulation.Generation;

/// <summary>Version-one catalog positions shared by setup preview and the live generator.</summary>
public static class SeededGalaxyLayout
{
    public static IReadOnlyList<Vector2> Generate(long seed, GalaxySetupOptions options)
    {
        options.Validate();
        var random = new GalaxySeedRandom(seed, 0x4C41594F5554UL);
        var radius = options.ToSettings().Radius;
        var positions = new List<Vector2>(options.SystemCount);
        while (positions.Count < options.SystemCount)
        {
            var candidate = Sample(random, options.Shape, radius);
            // A finite attempt budget avoids hanging on unusually crowded seeds.
            for (var attempt = 0; attempt < 256 && positions.Any(p => Vector2.DistanceSquared(p, candidate) < 24 * 24); attempt++)
                candidate = Sample(random, options.Shape, radius);
            if (positions.Any(p => Vector2.DistanceSquared(p, candidate) < 24 * 24))
                throw new InvalidOperationException("This seed could not fit the requested star spacing. Try another seed.");
            positions.Add(candidate);
        }
        // Human starts keep Sol as coordinate origin while placing it within the chosen shape.
        // Pick a star with a nearby exploration target so a fresh campaign has somewhere to go.
        var home = Enumerable.Range(0, positions.Count)
            .Where(i => positions.Where((_, j) => i != j).Any(p => Vector2.DistanceSquared(p, positions[i]) <= 95 * 95))
            .OrderBy(i => MathF.Abs(positions[i].Length() - radius * .65f)).ThenBy(i => i).First();
        (positions[0], positions[home]) = (positions[home], positions[0]);
        var origin = positions[0];
        for (var i = 0; i < positions.Count; i++) positions[i] -= origin;
        return positions;
    }

    private static Vector2 Sample(Random random, GalaxyShape shape, float radius)
    {
        var radial = Math.Sqrt(random.NextDouble());
        var angle = random.NextDouble() * Math.Tau;
        if (shape == GalaxyShape.Ring) radial = .62 + radial * .38;
        if (shape == GalaxyShape.Spiral)
        {
            var arm = random.Next(4);
            angle = arm * Math.PI / 2 + radial * 5.2 + (random.NextDouble() - .5) * .55;
            radial = .10 + .90 * radial;
        }
        return new((float)(Math.Cos(angle) * radial * radius),
            (float)(Math.Sin(angle) * radial * radius * (shape == GalaxyShape.Elliptical ? .60 : 1)));
    }
}

/// <summary>Explicit SplitMix64 stream: consumes all seed bits and does not depend on Random's implementation.</summary>
internal sealed class GalaxySeedRandom : Random
{
    private ulong _state;
    public GalaxySeedRandom(long seed, ulong domain) => _state = unchecked((ulong)seed) ^ domain;
    private ulong NextValue()
    {
        unchecked
        {
            var value = (_state += 0x9E3779B97F4A7C15UL);
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
    public override double NextDouble() => (NextValue() >> 11) * (1.0 / 9007199254740992.0);
    public override int Next(int maxValue) => maxValue < 0 ? throw new ArgumentOutOfRangeException(nameof(maxValue)) : (int)(NextDouble() * maxValue);
    public override int Next() => Next(int.MaxValue);
}

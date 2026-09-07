using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

public sealed class GalaxyGenerator
{
    public GalaxyState Generate(long seed, GalaxyGenerationSettings? settings = null)
    {
        settings ??= new GalaxyGenerationSettings();
        if (settings.SystemCount < 8)
            throw new ArgumentOutOfRangeException(nameof(settings.SystemCount), "A galaxy needs at least 8 systems.");

        var random = new Random(unchecked((int)(seed ^ (seed >> 32))));
        var archetypes = BuildQuotaDeck(settings, random);
        var systems = new List<StarSystemState>(settings.SystemCount);

        for (var i = 0; i < settings.SystemCount; i++)
        {
            var angle = random.NextDouble() * Math.PI * 2.0;
            var radial = Math.Sqrt(random.NextDouble()) * settings.Radius;
            var jitter = 0.65 + random.NextDouble() * 0.35;
            var position = new Vector2(
                (float)(Math.Cos(angle) * radial * jitter),
                (float)(Math.Sin(angle) * radial * jitter));

            var archetype = archetypes[i];
            var habitable = archetype == StarArchetype.HabitableRich || random.NextDouble() < settings.HabitableChance;
            var anomaly = archetype == StarArchetype.AncientRuin || archetype == StarArchetype.Legendary || random.NextDouble() < settings.AnomalyChance;
            var rare = archetype == StarArchetype.ResourceRich || random.NextDouble() < settings.RareResourceChance;
            var preWarp = habitable && random.NextDouble() < settings.PreWarpChance;

            systems.Add(new StarSystemState(
                i,
                $"SYS-{i + 1:000}",
                position,
                archetype,
                habitable,
                anomaly,
                rare,
                preWarp));
        }

        return new GalaxyState { Seed = seed, Systems = systems };
    }

    private static List<StarArchetype> BuildQuotaDeck(GalaxyGenerationSettings settings, Random random)
    {
        var weights = settings.ArchetypeWeights;
        var sum = weights.Values.Sum();
        if (sum <= 0)
            throw new InvalidOperationException("Galaxy archetype weights must sum to more than zero.");

        var allocations = weights
            .Select(kv => new
            {
                kv.Key,
                Exact = settings.SystemCount * (kv.Value / sum),
            })
            .Select(x => new Allocation(x.Key, (int)Math.Floor(x.Exact), x.Exact - Math.Floor(x.Exact)))
            .ToList();

        var allocated = allocations.Sum(x => x.Count);
        foreach (var allocation in allocations.OrderByDescending(x => x.Remainder).Take(settings.SystemCount - allocated))
            allocation.Count++;

        var deck = allocations.SelectMany(x => Enumerable.Repeat(x.Archetype, x.Count)).ToList();
        for (var i = deck.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        return deck;
    }

    private sealed class Allocation
    {
        public Allocation(StarArchetype archetype, int count, double remainder)
        {
            Archetype = archetype;
            Count = count;
            Remainder = remainder;
        }

        public StarArchetype Archetype { get; }
        public int Count { get; set; }
        public double Remainder { get; }
    }
}

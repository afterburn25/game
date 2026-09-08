using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

public sealed class GalaxyGenerator
{
    public GalaxyState Generate(long seed, GalaxyGenerationSettings? settings = null)
    {
        settings ??= new GalaxyGenerationSettings();
        var civilizationCount = settings.PreWarpCivilizationCount + settings.AncientCivilizationCount;
        if (settings.SystemCount < 8)
            throw new ArgumentOutOfRangeException(nameof(settings.SystemCount), "A galaxy needs at least 8 systems.");
        if (civilizationCount < 1 || civilizationCount > settings.SystemCount)
            throw new ArgumentOutOfRangeException(nameof(settings.PreWarpCivilizationCount));

        var random = new Random(unchecked((int)(seed ^ (seed >> 32))));
        var archetypes = BuildQuotaDeck(settings, random);
        var systems = new List<StarSystemState>(settings.SystemCount);

        for (var i = 0; i < settings.SystemCount; i++)
        {
            var angle = random.NextDouble() * Math.PI * 2.0;
            var radial = Math.Sqrt(random.NextDouble()) * settings.Radius;
            var jitter = 0.65 + random.NextDouble() * 0.35;
            var position = new Vector2((float)(Math.Cos(angle) * radial * jitter), (float)(Math.Sin(angle) * radial * jitter));
            var archetype = archetypes[i];
            var habitable = archetype == StarArchetype.HabitableRich || random.NextDouble() < settings.HabitableChance;
            var anomaly = archetype == StarArchetype.AncientRuin || archetype == StarArchetype.Legendary || random.NextDouble() < settings.AnomalyChance;
            var rare = archetype == StarArchetype.ResourceRich || random.NextDouble() < settings.RareResourceChance;
            var independentPreWarp = habitable && random.NextDouble() < settings.IndependentPreWarpChance;
            systems.Add(new StarSystemState(i, $"SYS-{i + 1:000}", position, archetype, habitable, anomaly, rare, independentPreWarp));
        }

        // Generate physical planets/moons first, then apply a deterministic species-neutral
        // environmental diversity policy before any civilization/species assignment occurs.
        var proceduralBodies = new PlanetaryBodyGenerator().Generate(seed, systems);
        var planetaryBodies = new PlanetaryEnvironmentalDiversityPolicy().Apply(seed, systems, proceduralBodies);
        var civilizations = new CivilizationSeeder().Seed(systems, settings.PreWarpCivilizationCount, settings.AncientCivilizationCount, seed);
        var colonySeeder = new ColonySeeder();
        var colonies = colonySeeder.Seed(civilizations);
        // Starter colony vessels, where still required by seeded warp-capable civilizations,
        // reserve their colonists from these real source colonies instead of spawning people.
        var fleets = new FleetSeeder().Seed(systems, civilizations, colonies);
        var economies = colonySeeder.SeedEconomies(civilizations);
        var technologies = new TechnologySeeder().Seed(civilizations);
        var construction = new ConstructionSeeder().Seed(civilizations);
        var shipyards = new ShipyardSeeder().Seed(civilizations);
        var knowledge = new CivilizationKnowledgeState();

        foreach (var civilization in civilizations)
        {
            var range = civilization.IsSeededAncient ? settings.InitialAncientSensorRange : settings.InitialPreWarpSensorRange;
            // Civilizations begin with complete survey knowledge of their own home system;
            // nearby catalog/sensor contacts remain detection-level knowledge only.
            knowledge.MarkSystemFullySurveyed(civilization.Id, civilization.HomeSystemId);
            knowledge.RevealWithinSensorRange(civilization.Id, civilization.HomeSystemId, systems, range);
        }

        return new GalaxyState
        {
            Seed = seed,
            Systems = systems,
            PlanetaryBodies = planetaryBodies,
            Civilizations = civilizations,
            Fleets = fleets,
            Colonies = colonies,
            Economies = economies,
            Technologies = technologies,
            ConstructionStates = construction,
            ShipyardStates = shipyards,
            PlayerCivilizationId = civilizations.First(c => c.IsPlayer).Id,
            Knowledge = knowledge,
        };
    }

    private static List<StarArchetype> BuildQuotaDeck(GalaxyGenerationSettings settings, Random random)
    {
        var weights = settings.ArchetypeWeights;
        var sum = weights.Values.Sum();
        if (sum <= 0) throw new InvalidOperationException("Galaxy archetype weights must sum to more than zero.");
        var allocations = weights.Select(kv => new { kv.Key, Exact = settings.SystemCount * (kv.Value / sum) })
            .Select(x => new Allocation(x.Key, (int)Math.Floor(x.Exact), x.Exact - Math.Floor(x.Exact))).ToList();
        var allocated = allocations.Sum(x => x.Count);
        foreach (var allocation in allocations.OrderByDescending(x => x.Remainder).Take(settings.SystemCount - allocated)) allocation.Count++;
        var deck = allocations.SelectMany(x => Enumerable.Repeat(x.Archetype, x.Count)).ToList();
        for (var i = deck.Count - 1; i > 0; i--) { var j = random.Next(i + 1); (deck[i], deck[j]) = (deck[j], deck[i]); }
        return deck;
    }

    private sealed class Allocation
    {
        public Allocation(StarArchetype archetype, int count, double remainder) { Archetype = archetype; Count = count; Remainder = remainder; }
        public StarArchetype Archetype { get; }
        public int Count { get; set; }
        public double Remainder { get; }
    }
}

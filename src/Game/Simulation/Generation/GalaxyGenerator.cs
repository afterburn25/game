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
        // Spectral type is a physical property of every new generated star, regardless of
        // coordinate layout. It remains independent from survey-gated archetype information.
        var stellarClasses = BuildBalancedStellarDeck(settings.SystemCount, seed);
        var systemNames = settings.GalaxyShape == GalaxyShape.BarredSpiral
            ? ProceduralSystemNamer.Generate(seed, settings.SystemCount)
            : null;
        var standardStarIndex = archetypes.IndexOf(StarArchetype.Standard);
        if (standardStarIndex < 0)
            throw new InvalidOperationException("Fresh campaigns need one Standard star for the human Sol origin.");
        (archetypes[SolCatalogPreset.SystemId], archetypes[standardStarIndex]) =
            (archetypes[standardStarIndex], archetypes[SolCatalogPreset.SystemId]);
        var systems = new List<StarSystemState>(settings.SystemCount);

        for (var i = 0; i < settings.SystemCount; i++)
        {
            var position = GalaxySpatialLayout.NextPosition(settings.GalaxyShape, settings.Radius, random);
            if (settings.GalaxyShape == GalaxyShape.BarredSpiral)
                position -= GalaxySpatialLayout.SolOffset(settings.Radius);
            var archetype = archetypes[i];
            var habitable = archetype == StarArchetype.HabitableRich || random.NextDouble() < settings.HabitableChance;
            var anomaly = archetype == StarArchetype.AncientRuin || archetype == StarArchetype.Legendary || random.NextDouble() < settings.AnomalyChance;
            var rare = archetype == StarArchetype.ResourceRich || random.NextDouble() < settings.RareResourceChance;
            var independentPreWarp = habitable && random.NextDouble() < settings.IndependentPreWarpChance;
            systems.Add(new StarSystemState(i, systemNames?[i] ?? $"SYS-{i + 1:000}", position, archetype, habitable, anomaly, rare,
                independentPreWarp, StellarClass: stellarClasses?[i]));
        }

        // An explicit persisted catalog key, not a renamed random world, selects the human origin.
        systems[SolCatalogPreset.SystemId] = new StarSystemState(SolCatalogPreset.SystemId, "Sol", Vector2.Zero,
            StarArchetype.Standard, true, false, false, false, SolCatalogPreset.PresetId,
            StellarPrimaryClass.GYellowDwarf);

        // Planet/moon physical state, including the deterministic species-neutral
        // environmental diversity guarantee, is owned entirely by PlanetaryBodyGenerator.
        // Save/load reconstruction calls that same generator from seed + systems.
        var planetaryBodies = new PlanetaryBodyGenerator().Generate(seed, systems);

        // Species identity is assigned independently from AI archetype, then the homeworld
        // planner selects distinct naturally viable physical systems from the already-generated
        // planet catalog. The founding colony is anchored to the exact body inside that system.
        var civilizations = new CivilizationSeeder().Seed(
            systems,
            planetaryBodies,
            settings.PreWarpCivilizationCount,
            settings.AncientCivilizationCount,
            seed,
            settings.PlayerSpeciesId);
        // Each nonhuman faction keeps its own planned physical home/coordinates. Naming changes
        // no IDs or environments; regenerate once so persisted star names reproduce body names.
        foreach (var civilization in civilizations.Where(civilization => !civilization.IsPlayer))
        {
            var home = systems[civilization.HomeSystemId];
            systems[civilization.HomeSystemId] = home with { Name = civilization.Name.Split(' ')[0] };
        }
        EnsureUniqueSystemNames(systems);
        planetaryBodies = new PlanetaryBodyGenerator().Generate(seed, systems);
        if (settings.GalaxyShape == GalaxyShape.BarredSpiral)
        {
            planetaryBodies = new NearbyHabitableWorldGuaranteePolicy().Apply(
                seed, systems, planetaryBodies, civilizations, guaranteedPerMajorCivilization: 2);
        }
        var colonySeeder = new ColonySeeder();
        var colonies = colonySeeder.Seed(civilizations, planetaryBodies);

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

    private static void EnsureUniqueSystemNames(IList<StarSystemState> systems)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < systems.Count; index++)
        {
            var system = systems[index];
            var name = system.Name;
            var sequence = 2;
            while (!used.Add(name)) name = $"{system.Name} {sequence++}";
            if (name != system.Name) systems[index] = system with { Name = name };
        }
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

    private static List<StellarPrimaryClass> BuildBalancedStellarDeck(int systemCount, long seed)
    {
        var targets = new Dictionary<StellarPrimaryClass, double>
        {
            [StellarPrimaryClass.MRedDwarf] = 48,
            [StellarPrimaryClass.KOrangeDwarf] = 20,
            [StellarPrimaryClass.GYellowDwarf] = 11,
            [StellarPrimaryClass.FYellowWhiteDwarf] = 6,
            [StellarPrimaryClass.AWhiteStar] = 3,
            [StellarPrimaryClass.HotBlueStar] = 1,
            [StellarPrimaryClass.Giant] = 4,
            [StellarPrimaryClass.WhiteDwarf] = 3,
            [StellarPrimaryClass.NeutronStar] = 2,
            [StellarPrimaryClass.BlackHole] = 1,
            [StellarPrimaryClass.Protostar] = 1,
        };
        var allocations = targets.Select(item => new
            {
                item.Key,
                Exact = systemCount * item.Value / 100.0,
            })
            .Select(item => new StellarAllocation(item.Key, (int)Math.Floor(item.Exact),
                item.Exact - Math.Floor(item.Exact)))
            .ToList();
        var remaining = systemCount - allocations.Sum(item => item.Count);
        foreach (var allocation in allocations.OrderByDescending(item => item.Remainder)
                     .ThenBy(item => item.StellarClass).Take(remaining))
            allocation.Count++;
        var deck = allocations.SelectMany(item => Enumerable.Repeat(item.StellarClass, item.Count)).ToList();
        var random = new Random(unchecked((int)(seed ^ (seed >> 32) ^ 0x53544152)));
        for (var index = deck.Count - 1; index > 0; index--)
        {
            var swap = random.Next(index + 1);
            (deck[index], deck[swap]) = (deck[swap], deck[index]);
        }
        var solarIndex = deck.IndexOf(StellarPrimaryClass.GYellowDwarf);
        (deck[SolCatalogPreset.SystemId], deck[solarIndex]) = (deck[solarIndex], deck[SolCatalogPreset.SystemId]);
        return deck;
    }

    private sealed class Allocation
    {
        public Allocation(StarArchetype archetype, int count, double remainder) { Archetype = archetype; Count = count; Remainder = remainder; }
        public StarArchetype Archetype { get; }
        public int Count { get; set; }
        public double Remainder { get; }
    }

    private sealed class StellarAllocation
    {
        public StellarAllocation(StellarPrimaryClass stellarClass, int count, double remainder)
        { StellarClass = stellarClass; Count = count; Remainder = remainder; }
        public StellarPrimaryClass StellarClass { get; }
        public int Count { get; set; }
        public double Remainder { get; }
    }
}

public static class ProceduralSystemNamer
{
    private static readonly string[] Prefixes =
    {
        "Al", "An", "Ar", "Bel", "Cael", "Cer", "Cor", "Del", "Eri", "Gal", "Hal",
        "Io", "Ka", "Ke", "Ly", "Mar", "Mer", "Na", "Nex", "Ori", "Pel", "Pro", "Qua",
        "Rin", "Sa", "Ser", "Tal", "Tau", "Ul", "Va", "Vel", "Xi", "Za",
    };

    private static readonly string[] Suffixes =
    {
        "bara", "caris", "dara", "dos", "dris", "lia", "lion", "lora", "maris", "mora",
        "nara", "nor", "phos", "ra", "rian", "ris", "ron", "rus", "sara", "tar", "thera",
        "tis", "tor", "vara", "vega", "von", "xis", "yra", "zen", "zora",
    };

    public static IReadOnlyList<string> Generate(long seed, int count)
    {
        if (count < 1 || count > Prefixes.Length * Suffixes.Length)
            throw new ArgumentOutOfRangeException(nameof(count));
        var random = new Random(unchecked((int)(seed ^ (seed >> 32) ^ 0x4E414D45)));
        var names = new List<string>(count);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (names.Count < count)
        {
            var name = Prefixes[random.Next(Prefixes.Length)] + Suffixes[random.Next(Suffixes.Length)];
            if (used.Add(name)) names.Add(name);
        }
        return names;
    }
}

/// <summary>
/// Seeded coordinate field shared by galaxy generation and its visual profile. The legacy disk
/// remains available for existing numeric callers; new Sandbox campaigns use the compact barred
/// spiral so all 100 strategic systems occupy the core, arms and sparse outer edge.
/// </summary>
public static class GalaxySpatialLayout
{
    public static Vector2 SolOffset(float radius) => new(radius * 0.36f, radius * 0.144f);

    public static Vector2 NextPosition(GalaxyShape shape, float radius, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (!float.IsFinite(radius) || radius <= 0)
            throw new ArgumentOutOfRangeException(nameof(radius));

        var first = random.NextDouble();
        var second = random.NextDouble();
        var third = random.NextDouble();
        if (shape == GalaxyShape.LegacyDisk)
        {
            var angle = first * Math.PI * 2.0;
            var radial = Math.Sqrt(second) * radius;
            var jitter = 0.65 + third * 0.35;
            return new Vector2((float)(Math.Cos(angle) * radial * jitter),
                (float)(Math.Sin(angle) * radial * jitter));
        }

        const double tilt = -0.26;
        double x;
        double y;
        if (first < 0.18)
        {
            // A dense, elongated central bar rather than a circular blob.
            var along = (second * 2.0 - 1.0) * radius * 0.36;
            var across = (third + first / 0.18 - 1.0) * radius * 0.075;
            x = along * Math.Cos(tilt) - across * Math.Sin(tilt);
            y = (along * Math.Sin(tilt) + across * Math.Cos(tilt)) * 0.72;
        }
        else if (first < 0.93)
        {
            // Four broad Milky Way-inspired arms. Seeded phase noise prevents artificial rows.
            var armSample = second * 4.0;
            var arm = Math.Floor(armSample);
            var radialFraction = 0.20 + 0.72 * Math.Sqrt((first - 0.18) / 0.75);
            var angle = arm * Math.PI * 0.5 + radialFraction * Math.PI * 2.35 +
                (third - 0.5) * 0.62 + (armSample - arm - 0.5) * 0.18;
            var radial = radius * radialFraction;
            x = Math.Cos(angle) * radial;
            y = Math.Sin(angle) * radial * 0.72;
        }
        else
        {
            // Sparse outer systems make the edge readable without wasting most of the canvas.
            var angle = second * Math.PI * 2.0;
            var radial = radius * (0.86 + third * 0.12);
            x = Math.Cos(angle) * radial;
            y = Math.Sin(angle) * radial * 0.72;
        }

        return new Vector2((float)x, (float)y);
    }
}

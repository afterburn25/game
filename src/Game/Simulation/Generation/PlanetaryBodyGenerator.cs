using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

/// <summary>
/// Generates a bounded physical planet/moon catalog from campaign seed + star-system identity.
/// The generator deliberately does not evaluate species suitability. Existing system-level
/// HasHabitableWorld is used only to preserve one legacy colonization candidate while the
/// species-relative colony contract is still being integrated.
/// </summary>
public sealed class PlanetaryBodyGenerator
{
    private const int BodyIdStride = 1000;

    public IReadOnlyList<PlanetaryBodyState> Generate(long campaignSeed, IReadOnlyList<StarSystemState> systems)
    {
        ArgumentNullException.ThrowIfNull(systems);
        var result = new List<PlanetaryBodyState>(systems.Count * 8);

        foreach (var system in systems.OrderBy(system => system.Id))
        {
            var random = StableRandom.ForSystem(campaignSeed, system.Id);
            var planetCount = ResolvePlanetCount(system.Archetype, ref random);
            var legacyOrbit = system.HasHabitableWorld ? random.NextInt(planetCount) : -1;
            var rareOrbit = system.HasRareResource ? random.NextInt(planetCount) : -1;
            var anomalyOrbit = system.HasAnomaly ? random.NextInt(planetCount) : -1;
            var localId = 1;

            for (var orbit = 0; orbit < planetCount; orbit++)
            {
                var isLegacyCandidate = orbit == legacyOrbit;
                var planetId = checked(system.Id * BodyIdStride + localId++);
                var planet = CreatePlanet(
                    planetId,
                    system,
                    orbit,
                    isLegacyCandidate,
                    orbit == rareOrbit,
                    orbit == anomalyOrbit,
                    system.HasPreWarpCivilization && isLegacyCandidate,
                    ref random);
                result.Add(planet.Validated());

                var moonCount = ResolveMoonCount(planet, ref random);
                for (var moon = 0; moon < moonCount; moon++)
                {
                    var moonId = checked(system.Id * BodyIdStride + localId++);
                    var body = CreateMoon(moonId, planet, moon, ref random);
                    result.Add(body.Validated());
                }
            }
        }

        // Environmental diversity is part of the canonical deterministic catalog itself,
        // not a fresh-generation-only post-process. Save/load reconstruction calls this same
        // generator from seed + systems, so both paths must receive the identical conditioned
        // physical catalog before any civilization/species assignment is considered.
        var conditioned = new PlanetaryEnvironmentalDiversityPolicy().Apply(campaignSeed, systems, result);
        ValidateCatalog(conditioned, systems);
        return conditioned;
    }

    private static PlanetaryBodyState CreatePlanet(
        int id,
        StarSystemState system,
        int orbit,
        bool legacyCandidate,
        bool rareResource,
        bool anomaly,
        bool preWarp,
        ref StableRandom random)
    {
        var name = $"{system.Name} {(char)('b' + orbit)}";
        if (legacyCandidate)
        {
            var radius = random.Range(0.78, 1.28);
            var gravity = random.Range(0.72, 1.30);
            var mass = gravity * radius * radius;
            var environment = new PlanetaryEnvironmentState(
                gravity,
                random.Range(255.0, 310.0),
                random.Range(62.0, 155.0),
                random.NextDouble() < 0.82 ? PlanetaryAtmosphereRegime.OxygenNitrogen : PlanetaryAtmosphereRegime.OxygenRich,
                PlanetarySolventRegime.Water,
                Math.Clamp(BaseRadiation(system.Archetype) + random.Range(0.00, 0.10), 0.0, 0.35),
                IsImmersedEnvironment: random.NextDouble() < 0.18,
                HasSolidSurface: true);

            return new PlanetaryBodyState(
                id, system.Id, null, orbit, name, PlanetaryBodyKind.Planet,
                radius, mass, environment, true, rareResource, anomaly, preWarp);
        }

        var orbitFraction = orbit / 7.0;
        var gasGiant = random.NextDouble() < Math.Clamp(0.10 + orbitFraction * 0.36, 0.10, 0.46);
        if (gasGiant)
        {
            var radius = random.Range(3.4, 10.8);
            var mass = random.Range(18.0, 320.0);
            var gravity = Math.Clamp(mass / (radius * radius), 0.55, 3.8);
            var temperature = Math.Clamp(420.0 - orbit * 48.0 + random.Range(-55.0, 55.0), 35.0, 650.0);
            var gasAtmosphere = random.NextDouble() < 0.72
                ? PlanetaryAtmosphereRegime.Reducing
                : PlanetaryAtmosphereRegime.Inert;
            var gasSolvent = temperature < 135.0 && random.NextDouble() < 0.45
                ? PlanetarySolventRegime.Hydrocarbon
                : PlanetarySolventRegime.None;
            var environment = new PlanetaryEnvironmentState(
                gravity,
                temperature,
                random.Range(7000.0, 180000.0),
                gasAtmosphere,
                gasSolvent,
                Math.Clamp(BaseRadiation(system.Archetype) + random.Range(0.08, 0.32), 0.0, 1.0),
                IsImmersedEnvironment: false,
                HasSolidSurface: false);

            return new PlanetaryBodyState(
                id, system.Id, null, orbit, name, PlanetaryBodyKind.Planet,
                radius, mass, environment, false, rareResource, anomaly,
                HasPreWarpCivilization: false);
        }

        var rockyRadius = random.Range(0.30, 1.95);
        var densityFactor = random.Range(0.55, 1.55);
        var rockyMass = Math.Max(0.01, rockyRadius * rockyRadius * rockyRadius * densityFactor);
        var rockyGravity = Math.Clamp(rockyMass / (rockyRadius * rockyRadius), 0.03, 3.2);
        var rockyTemperature = Math.Clamp(445.0 - orbit * 54.0 + random.Range(-70.0, 70.0), 28.0, 760.0);
        var atmosphere = SelectRockyAtmosphere(rockyMass, rockyTemperature, ref random);
        var pressure = SelectRockyPressure(atmosphere, rockyMass, ref random);
        var solvent = SelectSolvent(rockyTemperature, pressure, ref random);
        var immersed = solvent != PlanetarySolventRegime.None && random.NextDouble() < 0.13;
        var rockyEnvironment = new PlanetaryEnvironmentState(
            rockyGravity,
            rockyTemperature,
            pressure,
            atmosphere,
            solvent,
            Math.Clamp(BaseRadiation(system.Archetype) + (pressure < 5.0 ? 0.20 : 0.04) + random.Range(0.00, 0.22), 0.0, 1.0),
            immersed,
            HasSolidSurface: true);

        return new PlanetaryBodyState(
            id, system.Id, null, orbit, name, PlanetaryBodyKind.Planet,
            rockyRadius, rockyMass, rockyEnvironment, false, rareResource, anomaly,
            HasPreWarpCivilization: false);
    }

    private static PlanetaryBodyState CreateMoon(
        int id,
        PlanetaryBodyState parent,
        int moonIndex,
        ref StableRandom random)
    {
        var radius = random.Range(0.07, Math.Min(0.78, Math.Max(0.13, parent.RadiusEarth * 0.22)));
        var mass = Math.Max(0.0005, radius * radius * radius * random.Range(0.55, 1.35));
        var gravity = Math.Clamp(mass / (radius * radius), 0.005, 0.75);
        var temperature = Math.Clamp(parent.Environment.TemperatureKelvin + random.Range(-22.0, 22.0), 18.0, 780.0);
        var atmosphere = random.NextDouble() < 0.78
            ? PlanetaryAtmosphereRegime.Vacuum
            : (random.NextDouble() < 0.55 ? PlanetaryAtmosphereRegime.Inert : PlanetaryAtmosphereRegime.Reducing);
        var pressure = atmosphere == PlanetaryAtmosphereRegime.Vacuum ? 0.0 : random.Range(0.05, 18.0);
        var solvent = SelectSolvent(temperature, pressure, ref random);
        var environment = new PlanetaryEnvironmentState(
            gravity,
            temperature,
            pressure,
            atmosphere,
            solvent,
            Math.Clamp(parent.Environment.RadiationHazard + random.Range(0.02, 0.24), 0.0, 1.0),
            IsImmersedEnvironment: false,
            HasSolidSurface: true);

        return new PlanetaryBodyState(
            id,
            parent.SystemId,
            parent.Id,
            moonIndex,
            $"{parent.Name}-{moonIndex + 1}",
            PlanetaryBodyKind.Moon,
            radius,
            mass,
            environment,
            LegacyColonizationCandidate: false,
            HasRareResource: false,
            HasAnomaly: false,
            HasPreWarpCivilization: false);
    }

    private static int ResolvePlanetCount(StarArchetype archetype, ref StableRandom random)
    {
        var min = archetype is StarArchetype.BlackHole or StarArchetype.NeutronPulsar ? 1 : 2;
        var maxExclusive = archetype == StarArchetype.Nebula ? 6 : 8;
        return min + random.NextInt(Math.Max(1, maxExclusive - min));
    }

    private static int ResolveMoonCount(PlanetaryBodyState planet, ref StableRandom random)
    {
        if (planet.Kind != PlanetaryBodyKind.Planet)
            return 0;
        if (!planet.Environment.HasSolidSurface)
            return random.NextInt(4);
        if (planet.MassEarth > 1.4 && random.NextDouble() < 0.55)
            return 1 + random.NextInt(2);
        return random.NextDouble() < 0.32 ? 1 : 0;
    }

    private static PlanetaryAtmosphereRegime SelectRockyAtmosphere(
        double massEarth,
        double temperatureKelvin,
        ref StableRandom random)
    {
        if (massEarth < 0.10 || (massEarth < 0.32 && random.NextDouble() < 0.72))
            return PlanetaryAtmosphereRegime.Vacuum;
        if (temperatureKelvin > 430.0)
            return random.NextDouble() < 0.70
                ? PlanetaryAtmosphereRegime.CarbonDioxideRich
                : PlanetaryAtmosphereRegime.Other;
        if (temperatureKelvin < 150.0)
            return random.NextDouble() < 0.62
                ? PlanetaryAtmosphereRegime.Inert
                : PlanetaryAtmosphereRegime.Reducing;

        var roll = random.NextDouble();
        if (roll < 0.34) return PlanetaryAtmosphereRegime.CarbonDioxideRich;
        if (roll < 0.57) return PlanetaryAtmosphereRegime.Reducing;
        if (roll < 0.78) return PlanetaryAtmosphereRegime.Inert;
        if (roll < 0.92) return PlanetaryAtmosphereRegime.Other;
        return PlanetaryAtmosphereRegime.OxygenNitrogen;
    }

    private static double SelectRockyPressure(
        PlanetaryAtmosphereRegime atmosphere,
        double massEarth,
        ref StableRandom random)
    {
        if (atmosphere == PlanetaryAtmosphereRegime.Vacuum)
            return 0.0;

        var retention = Math.Clamp(0.35 + massEarth * 0.55, 0.25, 2.8);
        return atmosphere switch
        {
            PlanetaryAtmosphereRegime.CarbonDioxideRich => random.Range(8.0, 1200.0) * retention,
            PlanetaryAtmosphereRegime.Reducing => random.Range(2.0, 420.0) * retention,
            PlanetaryAtmosphereRegime.Inert => random.Range(0.4, 260.0) * retention,
            PlanetaryAtmosphereRegime.OxygenNitrogen => random.Range(45.0, 165.0) * retention,
            PlanetaryAtmosphereRegime.OxygenRich => random.Range(35.0, 145.0) * retention,
            _ => random.Range(0.2, 310.0) * retention,
        };
    }

    private static PlanetarySolventRegime SelectSolvent(
        double temperatureKelvin,
        double pressureKPa,
        ref StableRandom random)
    {
        if (pressureKPa <= 0.01 || random.NextDouble() < 0.42)
            return PlanetarySolventRegime.None;
        if (temperatureKelvin >= 250.0 && temperatureKelvin <= 390.0)
            return random.NextDouble() < 0.58 ? PlanetarySolventRegime.Water : PlanetarySolventRegime.None;
        if (temperatureKelvin >= 165.0 && temperatureKelvin < 250.0)
            return random.NextDouble() < 0.42 ? PlanetarySolventRegime.Ammonia : PlanetarySolventRegime.None;
        if (temperatureKelvin >= 70.0 && temperatureKelvin < 165.0)
            return random.NextDouble() < 0.46 ? PlanetarySolventRegime.Hydrocarbon : PlanetarySolventRegime.None;
        return random.NextDouble() < 0.04 ? PlanetarySolventRegime.Other : PlanetarySolventRegime.None;
    }

    private static double BaseRadiation(StarArchetype archetype) => archetype switch
    {
        StarArchetype.NeutronPulsar => 0.62,
        StarArchetype.BlackHole => 0.45,
        StarArchetype.Dangerous => 0.36,
        StarArchetype.Nebula => 0.18,
        _ => 0.06,
    };

    private static void ValidateCatalog(
        IReadOnlyList<PlanetaryBodyState> bodies,
        IReadOnlyList<StarSystemState> systems)
    {
        var ids = new HashSet<int>();
        foreach (var body in bodies)
        {
            if (!ids.Add(body.Id))
                throw new InvalidOperationException($"Duplicate planetary body ID {body.Id}.");
            if (!systems.Any(system => system.Id == body.SystemId))
                throw new InvalidOperationException($"Planetary body {body.Id} references unknown system {body.SystemId}.");
        }

        var byId = bodies.ToDictionary(body => body.Id);
        foreach (var moon in bodies.Where(body => body.Kind == PlanetaryBodyKind.Moon))
        {
            if (moon.ParentBodyId is null || !byId.TryGetValue(moon.ParentBodyId.Value, out var parent))
                throw new InvalidOperationException($"Moon {moon.Id} has no valid parent body.");
            if (parent.Kind != PlanetaryBodyKind.Planet || parent.SystemId != moon.SystemId)
                throw new InvalidOperationException($"Moon {moon.Id} parent is not a planet in the same system.");
        }

        foreach (var system in systems.Where(system => system.HasHabitableWorld))
        {
            if (!bodies.Any(body => body.SystemId == system.Id && body.LegacyColonizationCandidate))
                throw new InvalidOperationException($"Legacy habitable system {system.Id} has no compatibility colony candidate.");
        }
    }

    private struct StableRandom
    {
        private ulong _state;

        private StableRandom(ulong state)
        {
            _state = state == 0 ? 0x9E3779B97F4A7C15UL : state;
        }

        public static StableRandom ForSystem(long campaignSeed, int systemId)
        {
            var seed = unchecked((ulong)campaignSeed);
            seed ^= unchecked((ulong)(systemId + 1)) * 0x9E3779B97F4A7C15UL;
            seed ^= seed >> 30;
            seed *= 0xBF58476D1CE4E5B9UL;
            seed ^= seed >> 27;
            seed *= 0x94D049BB133111EBUL;
            seed ^= seed >> 31;
            return new StableRandom(seed);
        }

        public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 1) return 0;
            return (int)(NextUInt64() % (uint)maxExclusive);
        }

        public double Range(double minimum, double maximum) =>
            minimum + (maximum - minimum) * NextDouble();

        private ulong NextUInt64()
        {
            var x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return x * 0x2545F4914F6CDD1DUL;
        }
    }
}

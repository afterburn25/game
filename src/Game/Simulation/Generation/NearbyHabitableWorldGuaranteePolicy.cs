using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Models;
using Game.Simulation.Species;

namespace Game.Simulation.Generation;

/// <summary>Creates fair, undiscovered expansion candidates using the real species environment model.</summary>
public sealed class NearbyHabitableWorldGuaranteePolicy
{
    public const float MaximumOpeningDistance = 340.0f;
    private readonly SpeciesPlanetaryHabitabilityEvaluator _habitability = new();

    public IReadOnlyList<PlanetaryBodyState> Apply(
        long seed,
        IList<StarSystemState> systems,
        IReadOnlyList<PlanetaryBodyState> bodies,
        IReadOnlyList<CivilizationState> civilizations,
        int guaranteedPerMajorCivilization)
    {
        if (guaranteedPerMajorCivilization <= 0) return bodies;
        var result = bodies.ToDictionary(body => body.Id);
        var homeSystemIds = civilizations.Select(civilization => civilization.HomeSystemId).ToHashSet();
        var reservedSystems = new HashSet<int>(homeSystemIds);

        foreach (var civilization in civilizations.Where(civilization => !civilization.IsSeededAncient)
                     .OrderBy(civilization => civilization.Id))
        {
            var homeSystem = systems.Single(system => system.Id == civilization.HomeSystemId);
            var homeBody = bodies.Where(body => body.SystemId == homeSystem.Id)
                .Select(body => (Body: body, Assessment: _habitability.Evaluate(body, civilization.SpeciesId)))
                .Where(item => item.Assessment.Viability == SpeciesColonizationViability.NaturallyViable)
                .OrderByDescending(item => item.Assessment.Environment.NaturalHabitability)
                .ThenBy(item => item.Body.Id)
                .FirstOrDefault().Body
                ?? throw new InvalidOperationException($"Civilization {civilization.Id} has no natural homeworld for nearby-world guarantees.");

            var candidates = systems
                .Where(system => !reservedSystems.Contains(system.Id) && IsStableCandidateStar(system.StellarClass))
                .Select(system => new
                {
                    System = system,
                    Distance = Vector2.Distance(homeSystem.Position, system.Position),
                    Planets = bodies.Where(body => body.SystemId == system.Id && body.Kind == PlanetaryBodyKind.Planet &&
                        body.Environment.HasSolidSurface && !body.HasPreWarpCivilization).OrderBy(body => body.Id).ToArray(),
                })
                .Where(candidate => candidate.Distance <= MaximumOpeningDistance && candidate.Planets.Length > 0)
                .OrderBy(candidate => candidate.Distance)
                .ThenBy(candidate => candidate.System.Id)
                .ToList();

            var accepted = 0;
            foreach (var candidate in candidates)
            {
                var natural = candidate.Planets.FirstOrDefault(body =>
                    _habitability.Evaluate(result[body.Id], civilization.SpeciesId).Viability ==
                    SpeciesColonizationViability.NaturallyViable);
                if (natural is null) continue;
                reservedSystems.Add(candidate.System.Id);
                accepted++;
                if (accepted == guaranteedPerMajorCivilization) break;
            }

            if (accepted < guaranteedPerMajorCivilization)
            {
                foreach (var candidate in candidates.Where(candidate => !reservedSystems.Contains(candidate.System.Id)))
                {
                    var target = candidate.Planets[(int)(Mix(seed, civilization.Id, candidate.System.Id) %
                        (uint)candidate.Planets.Length)];
                    var radius = target.RadiusEarth;
                    result[target.Id] = target with
                    {
                        MassEarth = Math.Max(0.0005, homeBody.Environment.GravityG * radius * radius),
                        Environment = homeBody.Environment,
                    };
                    var systemIndex = systems.IndexOf(candidate.System);
                    systems[systemIndex] = candidate.System with { HasHabitableWorld = true };
                    reservedSystems.Add(candidate.System.Id);
                    accepted++;
                    if (accepted == guaranteedPerMajorCivilization) break;
                }
            }

            if (accepted != guaranteedPerMajorCivilization)
                throw new InvalidOperationException(
                    $"Could not place {guaranteedPerMajorCivilization} nearby viable worlds for civilization {civilization.Id} within {MaximumOpeningDistance:0} map units.");
        }

        return bodies.Select(body => result[body.Id].Validated()).ToArray();
    }

    private static bool IsStableCandidateStar(StellarPrimaryClass? stellarClass) => stellarClass is not
        (StellarPrimaryClass.BlackHole or StellarPrimaryClass.NeutronStar or StellarPrimaryClass.HotBlueStar or
         StellarPrimaryClass.Giant or StellarPrimaryClass.Protostar);

    private static uint Mix(long seed, int civilizationId, int systemId)
    {
        var value = unchecked((ulong)seed) ^ (uint)civilizationId * 0x9E3779B9UL ^ (uint)systemId * 0x85EBCA6BUL;
        value ^= value >> 30;
        value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27;
        return (uint)(value ^ (value >> 32));
    }
}

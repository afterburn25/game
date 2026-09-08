using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Models;

namespace Game.Simulation.Species;

public sealed record SpeciesHomeworldAssignment(
    int CivilizationId,
    string SpeciesId,
    int SystemId,
    int PlanetaryBodyId,
    double NaturalHabitability,
    SpeciesSettlementSuitability Suitability);

/// <summary>
/// Selects distinct natural founding worlds from already-generated physical planetary facts.
/// It never rewrites a planet, invents habitat support, or consults civilization personality.
/// Species IDs are supplied explicitly so the assignment remains independent of AI archetype.
/// </summary>
public sealed class SpeciesHomeworldPlanner
{
    private readonly PlanetarySpeciesHabitabilityEvaluator _habitability = new();

    public IReadOnlyList<SpeciesHomeworldAssignment> Plan(
        IReadOnlyList<StarSystemState> systems,
        IReadOnlyList<PlanetaryBodyState> bodies,
        IReadOnlyList<string> speciesIds)
    {
        ArgumentNullException.ThrowIfNull(systems);
        ArgumentNullException.ThrowIfNull(bodies);
        ArgumentNullException.ThrowIfNull(speciesIds);

        if (speciesIds.Count == 0)
            return Array.Empty<SpeciesHomeworldAssignment>();
        if (systems.Count < speciesIds.Count)
            throw new InvalidOperationException("There are fewer star systems than founding civilizations.");

        var systemsById = systems.ToDictionary(system => system.Id);
        var candidateSets = speciesIds
            .Select((speciesId, civilizationId) => new
            {
                CivilizationId = civilizationId,
                Species = SpeciesCatalog.Get(speciesId),
            })
            .Select(entry => new CandidateSet(
                entry.CivilizationId,
                entry.Species.Id,
                BuildCandidates(entry.Species, bodies, systemsById)))
            .ToArray();

        var empty = candidateSets.Where(set => set.Candidates.Count == 0).ToArray();
        if (empty.Length > 0)
        {
            throw new InvalidOperationException(
                "Natural homeworld planning failed because no compatible uninhabited body exists for: " +
                string.Join(", ", empty.Select(set => $"civ {set.CivilizationId} / {set.SpeciesId}")) + ".");
        }

        // Assign the most constrained species first, then civilization ID for deterministic ties.
        // This avoids an abundant Terran-like species consuming the only suitable system for a
        // rarer environmental niche.
        var remaining = candidateSets
            .OrderBy(set => set.Candidates.Select(candidate => candidate.System.Id).Distinct().Count())
            .ThenBy(set => set.CivilizationId)
            .ToList();
        var chosen = new List<SpeciesHomeworldAssignment>(speciesIds.Count);
        var occupiedSystems = new HashSet<int>();

        while (remaining.Count > 0)
        {
            var set = remaining[0];
            remaining.RemoveAt(0);

            var candidate = set.Candidates
                .Where(item => !occupiedSystems.Contains(item.System.Id))
                .OrderByDescending(item => ScoreCandidate(item, chosen, systemsById))
                .ThenByDescending(item => item.Assessment.NaturalHabitability)
                .ThenBy(item => item.System.Id)
                .ThenBy(item => item.Body.Id)
                .FirstOrDefault();

            if (candidate is null)
            {
                var availableSystems = set.Candidates
                    .Select(item => item.System.Id)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToArray();
                throw new InvalidOperationException(
                    $"Natural homeworld planning exhausted distinct systems for civ {set.CivilizationId} / {set.SpeciesId}. " +
                    $"Compatible systems: [{string.Join(",", availableSystems)}].");
            }

            occupiedSystems.Add(candidate.System.Id);
            chosen.Add(new SpeciesHomeworldAssignment(
                set.CivilizationId,
                set.SpeciesId,
                candidate.System.Id,
                candidate.Body.Id,
                candidate.Assessment.NaturalHabitability,
                candidate.Assessment.Suitability));
        }

        return chosen.OrderBy(assignment => assignment.CivilizationId).ToArray();
    }

    /// <summary>
    /// Resolves the exact founding body after a civilization's home system has already been
    /// selected by <see cref="Plan"/>. This uses the same natural-colonizability and within-system
    /// ranking rules, allowing colony seeding to anchor itself without persisting a second
    /// homeworld field on CivilizationState.
    /// </summary>
    public SpeciesHomeworldAssignment ResolveWithinSystem(
        int civilizationId,
        string speciesId,
        int systemId,
        IReadOnlyList<PlanetaryBodyState> bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        var species = SpeciesCatalog.Get(speciesId);

        var candidate = bodies
            .Where(body => body.SystemId == systemId && !body.HasPreWarpCivilization)
            .Select(body => new
            {
                Body = body,
                Assessment = _habitability.Evaluate(species, body),
            })
            .Where(entry => entry.Assessment.NaturallyColonizable)
            .OrderByDescending(entry => WithinSystemScore(entry.Assessment))
            .ThenByDescending(entry => entry.Assessment.NaturalHabitability)
            .ThenBy(entry => entry.Body.Id)
            .FirstOrDefault();

        if (candidate is null)
        {
            throw new InvalidOperationException(
                $"Planned home system {systemId} has no naturally viable body for civ {civilizationId} / {speciesId}.");
        }

        return new SpeciesHomeworldAssignment(
            civilizationId,
            speciesId,
            systemId,
            candidate.Body.Id,
            candidate.Assessment.NaturalHabitability,
            candidate.Assessment.Suitability);
    }

    private IReadOnlyList<HomeworldCandidate> BuildCandidates(
        SpeciesDefinition species,
        IReadOnlyList<PlanetaryBodyState> bodies,
        IReadOnlyDictionary<int, StarSystemState> systemsById)
    {
        return bodies
            .Where(body => !body.HasPreWarpCivilization)
            .Select(body => new
            {
                Body = body,
                Assessment = _habitability.Evaluate(species, body),
            })
            .Where(entry => entry.Assessment.NaturallyColonizable)
            .Select(entry => new HomeworldCandidate(
                systemsById[entry.Body.SystemId],
                entry.Body,
                entry.Assessment))
            .ToArray();
    }

    private static double ScoreCandidate(
        HomeworldCandidate candidate,
        IReadOnlyList<SpeciesHomeworldAssignment> chosen,
        IReadOnlyDictionary<int, StarSystemState> systemsById)
    {
        // Habitability remains the dominant criterion. A smaller spread term preserves the
        // game's existing preference for geographically separated civilizations.
        var spread = chosen.Count == 0
            ? 0.0
            : chosen.Min(existing =>
                Vector2.DistanceSquared(candidate.System.Position, systemsById[existing.SystemId].Position));
        var normalizedSpread = Math.Min(1.0, Math.Sqrt(spread) / 500.0);

        return WithinSystemScore(candidate.Assessment) + normalizedSpread;
    }

    private static double WithinSystemScore(PlanetarySpeciesHabitabilityAssessment assessment)
    {
        var suitabilityBonus = assessment.Suitability == SpeciesSettlementSuitability.Comfortable
            ? 0.35
            : 0.0;
        return assessment.NaturalHabitability * 10.0 + suitabilityBonus;
    }

    private sealed record CandidateSet(
        int CivilizationId,
        string SpeciesId,
        IReadOnlyList<HomeworldCandidate> Candidates);

    private sealed record HomeworldCandidate(
        StarSystemState System,
        PlanetaryBodyState Body,
        PlanetarySpeciesHabitabilityAssessment Assessment);
}

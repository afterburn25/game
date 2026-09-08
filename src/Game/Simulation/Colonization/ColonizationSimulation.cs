using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Exploration;
using Game.Simulation.Models;
using Game.Simulation.Species;

namespace Game.Simulation.Colonization;

public sealed class ColonizationSimulation
{
    private readonly IInterstellarOperationalReachView _operationalReach;
    private readonly SpeciesPlanetaryHabitabilityEvaluator _habitability = new();

    public ColonizationSimulation(IInterstellarOperationalReachView? operationalReach = null)
    {
        _operationalReach = operationalReach ?? new PrototypeInterstellarOperationalReachView();
    }

    public IReadOnlyList<ColonizationEvent> Advance(GalaxyState galaxy)
    {
        var events = new List<ColonizationEvent>();

        foreach (var fleet in galaxy.Fleets.Where(fleet => fleet.IsActive && fleet.Role == FleetRole.Colony))
        {
            var civilization = galaxy.Civilizations.First(c => c.Id == fleet.CivilizationId);

            // Found first when a populated colony ship has already arrived. Body-aware v8
            // missions keep the exact target; legacy/in-memory missions without one select the
            // best currently viable surveyed body in the arrival system.
            if (fleet.DestinationSystemId is null &&
                fleet.CurrentSystemId is int currentSystemId &&
                fleet.EmbarkedPopulationMillions > 0.0)
            {
                var speciesId = RequireEmbarkedPopulationSpecies(fleet);
                var targetBody = ResolveArrivalBody(galaxy, fleet, currentSystemId, speciesId);
                if (targetBody is not null &&
                    IsColonizable(galaxy, fleet.CivilizationId, targetBody, speciesId))
                {
                    var colonists = fleet.EmbarkedPopulationMillions;
                    var assessment = _habitability.Evaluate(targetBody, speciesId);
                    var colony = new ColonyState
                    {
                        Id = galaxy.Colonies.Count == 0 ? 0 : galaxy.Colonies.Max(c => c.Id) + 1,
                        CivilizationId = fleet.CivilizationId,
                        SystemId = currentSystemId,
                        PlanetaryBodyId = targetBody.Id,
                        Name = $"{civilization.Name} Colony {galaxy.Colonies.Count(c => c.CivilizationId == civilization.Id) + 1}",
                        PopulationSpeciesId = speciesId,
                        PopulationMillions = colonists,
                        Infrastructure = assessment.Viability == SpeciesColonizationViability.NaturallyViable ? 0.35 : 0.42,
                        Stability = assessment.Viability == SpeciesColonizationViability.NaturallyViable ? 0.92 : 0.88,
                    };

                    galaxy.Colonies.Add(colony);
                    fleet.EmbarkedPopulationMillions = 0.0;
                    fleet.EmbarkedPopulationSpeciesId = null;
                    fleet.IsActive = false;
                    fleet.DestinationSystemId = null;
                    fleet.DestinationPlanetaryBodyId = null;

                    var speciesName = SpeciesCatalog.Get(speciesId).DisplayName;
                    var mode = assessment.Viability == SpeciesColonizationViability.NaturallyViable
                        ? "natural environmental viability"
                        : "prototype habitat support";
                    events.Add(new ColonizationEvent(
                        fleet.CivilizationId,
                        fleet.Id,
                        currentSystemId,
                        colony.Id,
                        $"{civilization.Name} established {colony.Name} on {targetBody.Name} with {colonists:0.0} million {speciesName} colonists using {mode}."));
                    continue;
                }
            }

            if (fleet.DestinationSystemId is null &&
                !civilization.IsPlayer &&
                fleet.EmbarkedPopulationMillions > 0.0)
            {
                AssignAiColonyDestination(galaxy, fleet, civilization);
            }
        }

        return events;
    }

    public ColonyOrderResult IssuePlayerColonyOrder(
        GalaxyState galaxy,
        int civilizationId,
        int destinationSystemId)
    {
        var validation = ValidateSurveyedSystem(galaxy, civilizationId, destinationSystemId);
        if (validation is not null)
            return validation;

        var fleet = FindAvailableColonyFleet(galaxy, civilizationId);
        if (fleet is null)
            return new ColonyOrderResult(false, "No colony ship carrying reserved colonists is available.");

        var speciesId = RequireEmbarkedPopulationSpeciesForOrder(fleet, out var speciesError);
        if (speciesId is null)
            return new ColonyOrderResult(false, speciesError!);

        var body = SelectBestBodyInSystem(galaxy, civilizationId, destinationSystemId, speciesId);
        return body is null
            ? new ColonyOrderResult(
                false,
                "No surveyed body in that system is currently viable for the colony ship's population. Additional environmental-support capability may make other worlds usable later.")
            : IssuePlayerColonyOrder(galaxy, civilizationId, destinationSystemId, body.Id);
    }

    /// <summary>
    /// Body-aware colony command. Directly viable worlds are determined from the embarked
    /// species and authoritative surveyed planetary physics. The deterministic legacy candidate
    /// remains a temporary habitat-supported fallback until research/construction/logistics own
    /// explicit support capability and operating cost.
    /// </summary>
    public ColonyOrderResult IssuePlayerColonyOrder(
        GalaxyState galaxy,
        int civilizationId,
        int destinationSystemId,
        int planetaryBodyId)
    {
        var validation = ValidateSurveyedSystem(galaxy, civilizationId, destinationSystemId);
        if (validation is not null)
            return validation;

        var system = galaxy.Systems.First(system => system.Id == destinationSystemId);
        var body = galaxy.PlanetaryBodies.FirstOrDefault(candidate =>
            candidate.Id == planetaryBodyId && candidate.SystemId == destinationSystemId);
        if (body is null)
            return new ColonyOrderResult(false, "That planetary body is not part of the surveyed destination system.");
        if (!body.Environment.HasSolidSurface)
            return new ColonyOrderResult(false, "The selected body has no solid settlement surface in the current colony model.");
        if (body.HasPreWarpCivilization)
            return new ColonyOrderResult(false, "A native pre-warp civilization already inhabits the selected world.");
        if (galaxy.Colonies.Any(c => c.SystemId == destinationSystemId))
            return new ColonyOrderResult(false, "That system already contains a founded colony in the current single-colony early-release model.");

        var fleet = FindAvailableColonyFleet(galaxy, civilizationId);
        if (fleet is null)
            return new ColonyOrderResult(false, "No colony ship carrying reserved colonists is available.");

        var speciesId = RequireEmbarkedPopulationSpeciesForOrder(fleet, out var speciesError);
        if (speciesId is null)
            return new ColonyOrderResult(false, speciesError!);

        var assessment = _habitability.Evaluate(body, speciesId);
        if (!assessment.CanFoundCurrentColony)
        {
            var speciesName = SpeciesCatalog.Get(speciesId).DisplayName;
            return new ColonyOrderResult(
                false,
                $"{body.Name} is not currently viable for {speciesName}: natural habitability {assessment.Environment.NaturalHabitability:P0}, unprotected operational capacity {assessment.Environment.UnprotectedOperationalCapacity:P0}. Required habitat-support capabilities are not yet connected to colony construction/logistics.");
        }

        var reach = AssessOperationalReach(galaxy, fleet, destinationSystemId);
        if (!reach.IsSupported)
            return new ColonyOrderResult(false, reach.Reason);

        fleet.DestinationSystemId = destinationSystemId;
        fleet.DestinationPlanetaryBodyId = body.Id;

        var speciesDisplayName = SpeciesCatalog.Get(speciesId).DisplayName;
        var supportNote = assessment.Viability == SpeciesColonizationViability.NaturallyViable
            ? "naturally viable"
            : "prototype habitat-supported fallback";
        return new ColonyOrderResult(
            true,
            $"{fleet.Name}: colony course set for {body.Name} in {system.Name} with {fleet.EmbarkedPopulationMillions:0.0} million {speciesDisplayName} colonists aboard ({supportNote}).");
    }

    public MissionReachAssessment AssessOperationalReach(
        GalaxyState galaxy,
        int fleetId,
        int destinationSystemId)
    {
        var fleet = galaxy.Fleets.FirstOrDefault(f =>
            f.Id == fleetId &&
            f.IsActive &&
            f.Role == FleetRole.Colony &&
            f.EmbarkedPopulationMillions > 0.0);
        return fleet is null
            ? MissionReachAssessment.Unsupported("No populated colony ship is available.")
            : AssessOperationalReach(galaxy, fleet, destinationSystemId);
    }

    /// <summary>
    /// Resolves the occupied physical body. New body-aware colonies use their explicit ID;
    /// legacy/home colonies fall back to the deterministic compatibility body.
    /// </summary>
    public PlanetaryBodyState? ResolveCompatibilityColonyWorld(GalaxyState galaxy, ColonyState colony)
    {
        if (colony.PlanetaryBodyId is int bodyId)
        {
            return galaxy.PlanetaryBodies.FirstOrDefault(body =>
                body.Id == bodyId && body.SystemId == colony.SystemId);
        }

        return SelectCompatibilityCandidate(galaxy, colony.SystemId);
    }

    private MissionReachAssessment AssessOperationalReach(
        GalaxyState galaxy,
        FleetState fleet,
        int destinationSystemId) =>
        _operationalReach.Assess(
            galaxy,
            fleet.CivilizationId,
            fleet,
            destinationSystemId,
            InterstellarMissionKind.Colony);

    private bool IsColonizable(
        GalaxyState galaxy,
        int civilizationId,
        PlanetaryBodyState body,
        string speciesId) =>
        galaxy.Knowledge.IsSystemFullySurveyed(civilizationId, body.SystemId) &&
        _habitability.Evaluate(body, speciesId).CanFoundCurrentColony &&
        !galaxy.Colonies.Any(c => c.SystemId == body.SystemId);

    private void AssignAiColonyDestination(
        GalaxyState galaxy,
        FleetState fleet,
        CivilizationState civilization)
    {
        var speciesId = RequireEmbarkedPopulationSpecies(fleet);
        var systemsById = galaxy.Systems.ToDictionary(system => system.Id);
        var candidate = galaxy.PlanetaryBodies
            .Where(body => IsColonizable(galaxy, civilization.Id, body, speciesId))
            .Where(body => AssessOperationalReach(galaxy, fleet, body.SystemId).IsSupported)
            .Select(body =>
            {
                var assessment = _habitability.Evaluate(body, speciesId);
                return new
                {
                    Body = body,
                    Assessment = assessment,
                    System = systemsById[body.SystemId],
                    Distance = Vector2.DistanceSquared(fleet.Position, systemsById[body.SystemId].Position),
                    Value =
                        (assessment.Viability == SpeciesColonizationViability.NaturallyViable ? 14000.0 : 3500.0) +
                        assessment.Environment.NaturalHabitability * 9000.0 +
                        (body.HasRareResource ? 14000.0 : 0.0) +
                        civilization.Traits.Territoriality * 6000.0 +
                        civilization.Traits.Greed * (body.HasRareResource ? 9000.0 : 1500.0),
                };
            })
            .OrderByDescending(candidate => candidate.Value - candidate.Distance)
            .ThenBy(candidate => candidate.System.Id)
            .ThenBy(candidate => candidate.Body.Id)
            .FirstOrDefault();

        if (candidate is not null)
        {
            fleet.DestinationSystemId = candidate.System.Id;
            fleet.DestinationPlanetaryBodyId = candidate.Body.Id;
        }
    }

    private PlanetaryBodyState? ResolveArrivalBody(
        GalaxyState galaxy,
        FleetState fleet,
        int currentSystemId,
        string speciesId)
    {
        if (fleet.DestinationPlanetaryBodyId is int bodyId)
        {
            return galaxy.PlanetaryBodies.FirstOrDefault(body =>
                body.Id == bodyId && body.SystemId == currentSystemId);
        }

        return SelectBestBodyInSystem(galaxy, fleet.CivilizationId, currentSystemId, speciesId);
    }

    private PlanetaryBodyState? SelectBestBodyInSystem(
        GalaxyState galaxy,
        int civilizationId,
        int systemId,
        string speciesId)
    {
        return galaxy.PlanetaryBodies
            .Where(body => body.SystemId == systemId)
            .Where(body => IsColonizable(galaxy, civilizationId, body, speciesId))
            .Select(body => new
            {
                Body = body,
                Assessment = _habitability.Evaluate(body, speciesId),
            })
            .OrderByDescending(candidate => candidate.Assessment.Viability)
            .ThenByDescending(candidate => candidate.Assessment.Environment.NaturalHabitability)
            .ThenByDescending(candidate => candidate.Assessment.Environment.UnprotectedOperationalCapacity)
            .ThenBy(candidate => candidate.Body.Id)
            .Select(candidate => candidate.Body)
            .FirstOrDefault();
    }

    private static FleetState? FindAvailableColonyFleet(GalaxyState galaxy, int civilizationId) =>
        galaxy.Fleets.FirstOrDefault(f =>
            f.IsActive &&
            f.CivilizationId == civilizationId &&
            f.Role == FleetRole.Colony &&
            f.EmbarkedPopulationMillions > 0.0);

    private static string RequireEmbarkedPopulationSpecies(FleetState fleet)
    {
        if (fleet.EmbarkedPopulationMillions <= 0.0)
            throw new InvalidOperationException($"Fleet {fleet.Id} has no embarked population to identify.");

        var speciesId = fleet.EmbarkedPopulationSpeciesId;
        if (string.IsNullOrWhiteSpace(speciesId) || !SpeciesCatalog.TryGet(speciesId, out _))
        {
            throw new InvalidOperationException(
                $"Fleet {fleet.Id} carries population without a valid species identity '{speciesId}'.");
        }

        return speciesId;
    }

    private static string? RequireEmbarkedPopulationSpeciesForOrder(
        FleetState fleet,
        out string? error)
    {
        var speciesId = fleet.EmbarkedPopulationSpeciesId;
        if (string.IsNullOrWhiteSpace(speciesId) || !SpeciesCatalog.TryGet(speciesId, out _))
        {
            error = "The colony ship's passenger species identity is invalid.";
            return null;
        }

        error = null;
        return speciesId;
    }

    private static PlanetaryBodyState? SelectCompatibilityCandidate(GalaxyState galaxy, int systemId) =>
        galaxy.PlanetaryBodies
            .Where(body => body.SystemId == systemId)
            .OrderBy(body => body.Id)
            .FirstOrDefault(body => body.LegacyColonizationCandidate && body.Environment.HasSolidSurface);

    private static ColonyOrderResult? ValidateSurveyedSystem(
        GalaxyState galaxy,
        int civilizationId,
        int destinationSystemId)
    {
        var system = galaxy.Systems.FirstOrDefault(candidate => candidate.Id == destinationSystemId);
        if (system is null)
            return new ColonyOrderResult(false, "Unknown destination.");
        if (!galaxy.Knowledge.IsSystemKnown(civilizationId, destinationSystemId))
            return new ColonyOrderResult(false, "That astronomical target has not been detected yet.");
        if (!galaxy.Knowledge.IsSystemFullySurveyed(civilizationId, destinationSystemId))
            return new ColonyOrderResult(false, "A completed science survey is required before a colony mission can be prepared.");
        if (galaxy.Colonies.Any(c => c.SystemId == destinationSystemId))
            return new ColonyOrderResult(false, "That system already contains a founded colony in the current single-colony early-release model.");

        return null;
    }
}

public sealed record ColonyOrderResult(bool Accepted, string Message);
public sealed record ColonizationEvent(int CivilizationId, int FleetId, int SystemId, int ColonyId, string Message);

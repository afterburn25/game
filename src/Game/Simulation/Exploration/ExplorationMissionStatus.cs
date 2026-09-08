using System;
using System.Linq;
using System.Numerics;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Simulation.Exploration;

public enum ExplorationMissionPhase
{
    AwaitingOrder,
    Traveling,
    ReconnaissanceReady,
    ScienceSurveying,
    ColonySettlementReady,
}

/// <summary>
/// Reconstructible player/AI-facing status for one owned exploration/colony fleet. Transit ETA
/// uses only the fleet's own represented speed and common astronomical coordinates. Detailed
/// science ETA is withheld until reconnaissance legitimately reveals survey complexity.
/// </summary>
public sealed record ExplorationMissionStatus(
    ExplorationMissionPhase Phase,
    double? EstimatedTransitDaysRemaining,
    double? EstimatedSurveyDaysRemaining,
    double? EstimatedMissionDaysRemaining,
    string Summary)
{
    public static ExplorationMissionStatus Awaiting(string summary) =>
        new(ExplorationMissionPhase.AwaitingOrder, null, null, null, summary);
}

public sealed class ExplorationMissionStatusEvaluator
{
    private readonly SurveyOperationsProfiler _surveyProfiler;

    public ExplorationMissionStatusEvaluator(SurveyOperationsProfiler? surveyProfiler = null)
    {
        _surveyProfiler = surveyProfiler ?? new SurveyOperationsProfiler();
    }

    public ExplorationMissionStatus Build(GalaxyState galaxy, FleetState fleet)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(fleet);

        if (!fleet.IsActive)
            return ExplorationMissionStatus.Awaiting($"{fleet.Name} is not an active mission fleet.");

        if (fleet.DestinationSystemId is int destinationSystemId)
            return BuildTravelStatus(galaxy, fleet, destinationSystemId);

        if (fleet.CurrentSystemId is not int currentSystemId)
            return ExplorationMissionStatus.Awaiting($"{fleet.Name} has no active destination and is not currently at a star system.");

        return fleet.Role switch
        {
            FleetRole.Scout => BuildLocalScoutStatus(galaxy, fleet, currentSystemId),
            FleetRole.Science => BuildLocalScienceStatus(galaxy, fleet, currentSystemId),
            FleetRole.Colony => BuildLocalColonyStatus(galaxy, fleet, currentSystemId),
            _ => ExplorationMissionStatus.Awaiting($"{fleet.Name} has no exploration mission order."),
        };
    }

    private ExplorationMissionStatus BuildTravelStatus(
        GalaxyState galaxy,
        FleetState fleet,
        int destinationSystemId)
    {
        var target = galaxy.Systems.FirstOrDefault(system => system.Id == destinationSystemId);
        if (target is null)
            return ExplorationMissionStatus.Awaiting($"{fleet.Name} references an unknown destination system.");

        var distance = Vector2.Distance(fleet.Position, target.Position);
        double? transitDays = fleet.StrategicSpeed > 0.0 && double.IsFinite(fleet.StrategicSpeed)
            ? Math.Max(0.0, distance / fleet.StrategicSpeed)
            : null;

        double? surveyDays = null;
        if (fleet.Role == FleetRole.Science)
        {
            var level = galaxy.Knowledge.GetSystemSurveyLevel(fleet.CivilizationId, target.Id);
            if (level >= SystemSurveyLevel.PartiallySurveyed)
            {
                var progress = galaxy.Knowledge.GetSystemSurveyProgress(fleet.CivilizationId, target.Id);
                var profile = _surveyProfiler.Build(galaxy, target.Id);
                surveyDays = Math.Max(0.0, profile.EstimatedScienceSurveyDays * (1.0 - progress));
            }
        }

        double? missionDays = fleet.Role switch
        {
            FleetRole.Science when surveyDays is double knownSurvey && transitDays is double knownTransit => knownTransit + knownSurvey,
            FleetRole.Science => null,
            _ => transitDays,
        };

        var eta = transitDays is double travel
            ? $"approximately {travel:0.#} transit days remain"
            : "transit ETA is unavailable";
        var followUp = surveyDays is double survey
            ? $"; approximately {survey:0.#} known detailed-survey days remain after arrival"
            : fleet.Role == FleetRole.Science
                ? "; detailed-survey duration remains unknown until reconnaissance establishes system complexity"
                : string.Empty;

        return new ExplorationMissionStatus(
            ExplorationMissionPhase.Traveling,
            transitDays,
            surveyDays,
            missionDays,
            $"{fleet.Name} is traveling to {target.Name}; {eta}{followUp}.");
    }

    private static ExplorationMissionStatus BuildLocalScoutStatus(
        GalaxyState galaxy,
        FleetState fleet,
        int systemId)
    {
        var system = galaxy.Systems.First(system => system.Id == systemId);
        var level = galaxy.Knowledge.GetSystemSurveyLevel(fleet.CivilizationId, systemId);
        if (level < SystemSurveyLevel.PartiallySurveyed)
        {
            return new ExplorationMissionStatus(
                ExplorationMissionPhase.ReconnaissanceReady,
                0.0,
                0.0,
                0.0,
                $"{fleet.Name} is in {system.Name} and ready to perform its reconnaissance pass.");
        }

        return ExplorationMissionStatus.Awaiting(
            $"{fleet.Name} has completed reconnaissance-grade work in {system.Name} and is awaiting another order.");
    }

    private ExplorationMissionStatus BuildLocalScienceStatus(
        GalaxyState galaxy,
        FleetState fleet,
        int systemId)
    {
        var system = galaxy.Systems.First(system => system.Id == systemId);
        var level = galaxy.Knowledge.GetSystemSurveyLevel(fleet.CivilizationId, systemId);
        if (level == SystemSurveyLevel.FullySurveyed)
        {
            return ExplorationMissionStatus.Awaiting(
                $"{fleet.Name} has completed the detailed survey of {system.Name} and is awaiting another order.");
        }

        double? surveyDays = null;
        if (level >= SystemSurveyLevel.PartiallySurveyed)
        {
            var progress = galaxy.Knowledge.GetSystemSurveyProgress(fleet.CivilizationId, systemId);
            var profile = _surveyProfiler.Build(galaxy, systemId);
            surveyDays = Math.Max(0.0, profile.EstimatedScienceSurveyDays * (1.0 - progress));
        }

        var estimate = surveyDays is double known
            ? $"approximately {known:0.#} detailed-survey days remain"
            : "survey duration is not yet known; the first science pass will establish reconnaissance-grade complexity";

        return new ExplorationMissionStatus(
            ExplorationMissionPhase.ScienceSurveying,
            0.0,
            surveyDays,
            surveyDays,
            $"{fleet.Name} is conducting a detailed survey of {system.Name}; {estimate}.");
    }

    private static ExplorationMissionStatus BuildLocalColonyStatus(
        GalaxyState galaxy,
        FleetState fleet,
        int systemId)
    {
        var system = galaxy.Systems.First(system => system.Id == systemId);
        if (fleet.EmbarkedPopulationMillions <= 0.0)
            return ExplorationMissionStatus.Awaiting($"{fleet.Name} is not carrying colonists and has no active colony mission.");

        var fullySurveyed = galaxy.Knowledge.IsSystemFullySurveyed(fleet.CivilizationId, systemId);
        var candidate = fullySurveyed
            ? galaxy.PlanetaryBodies
                .Where(body => body.SystemId == systemId)
                .OrderBy(body => body.Id)
                .FirstOrDefault(body =>
                    body.LegacyColonizationCandidate &&
                    body.Environment.HasSolidSurface &&
                    !body.HasPreWarpCivilization)
            : null;
        var occupied = galaxy.Colonies.Any(colony => colony.SystemId == systemId);

        if (candidate is not null && !occupied)
        {
            return new ExplorationMissionStatus(
                ExplorationMissionPhase.ColonySettlementReady,
                0.0,
                null,
                0.0,
                $"{fleet.Name} has arrived in {system.Name}; {candidate.Name} satisfies the current compatibility settlement rules and founding can proceed.");
        }

        return ExplorationMissionStatus.Awaiting(
            $"{fleet.Name} is carrying {fleet.EmbarkedPopulationMillions:0.#} million colonists in {system.Name} but has no valid settlement action under the current compatibility rules.");
    }
}

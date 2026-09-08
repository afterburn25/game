using System.Numerics;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class ExplorationAiDeconflictionValidation
{
    public static void ValidateDistinctTargetsWhenAlternativesExist()
    {
        var galaxy = CreateValidationGalaxy();
        var civilization = galaxy.Civilizations.First(candidate => !candidate.IsPlayer);
        DisableExistingSurveyFleets(galaxy, civilization.Id);

        var home = galaxy.Systems.First(system => system.Id == civilization.HomeSystemId);
        var openTargets = galaxy.Systems
            .Where(system => system.Id != home.Id)
            .OrderBy(system => system.Id)
            .Take(2)
            .ToArray();
        Require(openTargets.Length == 2, "validation galaxy did not provide two survey targets");
        MarkAllOtherSystemsFullySurveyed(galaxy, civilization.Id, openTargets.Select(system => system.Id).ToHashSet());

        var first = AddScienceFleet(galaxy, civilization.Id, home, "Deconfliction Science One");
        var second = AddScienceFleet(galaxy, civilization.Id, home, "Deconfliction Science Two");

        // Exercise the real AI assignment path. A very small step lets both fleets receive
        // destinations without materially changing which targets are available.
        var simulation = new ExplorationSimulation();
        simulation.Advance(galaxy, 0.000001);

        Require(first.DestinationSystemId is int firstTarget, "first AI science fleet received no destination");
        Require(second.DestinationSystemId is int secondTarget, "second AI science fleet received no destination");
        Require(firstTarget != secondTarget,
            "two AI science fleets redundantly selected the same target while another supported target was available");
        Require(openTargets.Any(system => system.Id == firstTarget) && openTargets.Any(system => system.Id == secondTarget),
            "AI deconfliction selected a system outside the controlled remaining-work set");
    }

    public static void ValidateSharedFallbackWhenOnlyOneTargetRemains()
    {
        var galaxy = CreateValidationGalaxy();
        var civilization = galaxy.Civilizations.First(candidate => !candidate.IsPlayer);
        DisableExistingSurveyFleets(galaxy, civilization.Id);

        var home = galaxy.Systems.First(system => system.Id == civilization.HomeSystemId);
        var onlyTarget = galaxy.Systems.First(system => system.Id != home.Id);
        MarkAllOtherSystemsFullySurveyed(galaxy, civilization.Id, new HashSet<int> { onlyTarget.Id });

        var first = AddScienceFleet(galaxy, civilization.Id, home, "Fallback Science One");
        var second = AddScienceFleet(galaxy, civilization.Id, home, "Fallback Science Two");

        var planner = new ExplorationMissionPlanner();
        var coordinator = new ExplorationAiMissionCoordinator(planner);

        var firstSelection = coordinator.SelectMission(galaxy, first);
        Require(firstSelection.Candidate?.SystemId == onlyTarget.Id,
            "first AI science fleet did not select the only remaining supported target");
        Require(!firstSelection.UsedSharedFallback,
            "first assignment incorrectly reported shared-target fallback before the target was reserved");
        first.DestinationSystemId = onlyTarget.Id;

        var secondSelection = coordinator.SelectMission(galaxy, second);
        Require(secondSelection.Candidate?.SystemId == onlyTarget.Id,
            "second AI science fleet idled instead of sharing the only remaining supported target");
        Require(secondSelection.UsedSharedFallback,
            "second assignment did not report shared-target fallback when every supported target was reserved");
        Require(secondSelection.ReservedSystemIds.Contains(onlyTarget.Id),
            "coordinator did not expose the live reservation that caused fallback");

        // Also prove the real simulation path applies that fallback rather than leaving the
        // second vessel destination-less.
        first.DestinationSystemId = null;
        second.DestinationSystemId = null;
        var simulation = new ExplorationSimulation();
        simulation.Advance(galaxy, 0.000001);
        Require(first.DestinationSystemId == onlyTarget.Id && second.DestinationSystemId == onlyTarget.Id,
            "simulation did not apply shared-target fallback when one survey target remained");
    }

    public static void ValidateLocalSurveyWorkActsAsReservation()
    {
        var galaxy = CreateValidationGalaxy();
        var civilization = galaxy.Civilizations.First(candidate => !candidate.IsPlayer);
        DisableExistingSurveyFleets(galaxy, civilization.Id);

        var home = galaxy.Systems.First(system => system.Id == civilization.HomeSystemId);
        var targets = galaxy.Systems
            .Where(system => system.Id != home.Id)
            .OrderBy(system => system.Id)
            .Take(2)
            .ToArray();
        Require(targets.Length == 2, "validation galaxy did not provide two local-reservation targets");
        MarkAllOtherSystemsFullySurveyed(galaxy, civilization.Id, targets.Select(system => system.Id).ToHashSet());

        // The first vessel is already physically at target A with work remaining and no
        // DestinationSystemId, so its live state must reserve A for coordination purposes.
        var localWorker = AddScienceFleet(galaxy, civilization.Id, targets[0], "Local Survey Worker");
        var requester = AddScienceFleet(galaxy, civilization.Id, home, "Reservation-Aware Science Vessel");
        var coordinator = new ExplorationAiMissionCoordinator(new ExplorationMissionPlanner());

        var selection = coordinator.SelectMission(galaxy, requester);
        Require(selection.ReservedSystemIds.Contains(targets[0].Id),
            "local in-place survey work was not represented as a live reservation");
        Require(selection.Candidate?.SystemId == targets[1].Id,
            "requesting fleet did not prefer the unreserved alternative to a locally worked target");
        Require(!selection.UsedSharedFallback,
            "requesting fleet incorrectly used shared fallback while an unreserved supported target existed");
    }

    private static void DisableExistingSurveyFleets(GalaxyState galaxy, int civilizationId)
    {
        foreach (var fleet in galaxy.Fleets.Where(fleet =>
                     fleet.CivilizationId == civilizationId &&
                     fleet.Role is FleetRole.Scout or FleetRole.Science))
        {
            fleet.IsActive = false;
        }
    }

    private static void MarkAllOtherSystemsFullySurveyed(
        GalaxyState galaxy,
        int civilizationId,
        IReadOnlySet<int> leaveOpenSystemIds)
    {
        foreach (var system in galaxy.Systems)
        {
            if (!leaveOpenSystemIds.Contains(system.Id))
                galaxy.Knowledge.MarkSystemFullySurveyed(civilizationId, system.Id);
        }
    }

    private static FleetState AddScienceFleet(
        GalaxyState galaxy,
        int civilizationId,
        StarSystemState system,
        string name)
    {
        var fleet = new FleetState
        {
            Id = galaxy.Fleets.Count == 0 ? 30000 : galaxy.Fleets.Max(existing => existing.Id) + 30000,
            CivilizationId = civilizationId,
            Name = name,
            Role = FleetRole.Science,
            Position = system.Position,
            CurrentSystemId = system.Id,
            StrategicSpeed = 18.0,
            SensorRange = 185.0f,
            IsActive = true,
        };
        galaxy.Fleets.Add(fleet);
        return fleet;
    }

    private static GalaxyState CreateValidationGalaxy() =>
        new GalaxyGenerator().Generate(
            0x4445_434F_4E46_4C49L,
            new GalaxyGenerationSettings
            {
                SystemCount = 48,
                PreWarpCivilizationCount = 5,
                AncientCivilizationCount = 1,
                Radius = 520.0f,
            });

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

using System;
using System.IO;
using System.Linq;
using Game.Persistence;
using Game.Simulation;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.CoreRuntime.Validation;

internal static class CivilianFleetHoldOrderValidation
{
    [Game.Validation.RegressionCheck]
    internal static void Run()
    {
        HoldCompletesOneLaneThenRetainsRoute();
        HeldLocalWorkAndResume();
        HeldColonyArrivalPersistsAuthorizationAndPassengers();
        RejectsForeignInactiveAndUnsupportedShips();
    }

    private static void HoldCompletesOneLaneThenRetainsRoute()
    {
        var galaxy = CreateGalaxy(); var player = galaxy.PlayerCivilizationId;
        var systems = galaxy.Systems.Take(3).ToArray();
        var scout = Fleet(player, 99001, FleetRole.Scout, systems[0]);
        scout.Position = (systems[0].Position + systems[1].Position) / 2;
        scout.CurrentSystemId = null; scout.DestinationSystemId = systems[2].Id;
        scout.PlannedRouteSystemIds.Add(systems[1].Id); scout.PlannedRouteSystemIds.Add(systems[2].Id);
        scout.FuelRemainingLightYears = scout.FuelCapacityLightYears;
        galaxy.Fleets.Add(scout);
        var coordinator = new GalaxySimulationStepCoordinator();
        Require(coordinator.IssueCivilianHoldOrder(galaxy, player, scout.Id).Accepted, "transit hold rejected");
        new ExplorationSimulation().Advance(galaxy, 10_000);
        Require(scout.CurrentSystemId == systems[1].Id && scout.DestinationSystemId == systems[2].Id &&
            scout.PlannedRouteSystemIds.SequenceEqual(new[] { systems[2].Id }), "hold crossed more than one lane or discarded the remaining route");
        Require(scout.FuelRemainingLightYears < scout.FuelCapacityLightYears || systems[1].Id == systems[0].Id, "completed held lane did not consume physical fuel");
    }

    private static void HeldLocalWorkAndResume()
    {
        var galaxy = CreateGalaxy(); var player = galaxy.PlayerCivilizationId; var system = galaxy.Systems[^1];
        var scout = Fleet(player, 99002, FleetRole.Scout, system); galaxy.Fleets.Add(scout);
        var coordinator = new GalaxySimulationStepCoordinator();
        Require(coordinator.IssueCivilianHoldOrder(galaxy, player, scout.Id).Accepted, "local hold rejected");
        new ExplorationSimulation().Advance(galaxy, 1);
        Require(scout.ReconnaissanceDaysCompleted == 0, "held scout performed local work");
        Require(coordinator.IssueCivilianResumeOrder(galaxy, player, scout.Id).Accepted, "resume rejected");
        new ExplorationSimulation().Advance(galaxy, 1);
        Require(scout.ReconnaissanceDaysCompleted > 0, "resumed scout did not continue local work");
    }

    private static void HeldColonyArrivalPersistsAuthorizationAndPassengers()
    {
        var galaxy = CreateGalaxy(); var player = galaxy.PlayerCivilizationId;
        var target = galaxy.Systems[1]; var body = galaxy.PlanetaryBodies.First(candidate => candidate.SystemId == target.Id);
        var colony = Fleet(player, 99003, FleetRole.Colony, target);
        colony.CurrentSystemId = null; colony.Position = target.Position; colony.DestinationSystemId = target.Id;
        colony.PlannedRouteSystemIds.Add(target.Id); colony.DestinationPlanetaryBodyId = body.Id;
        colony.EmbarkedPopulationMillions = 2.5;
        colony.EmbarkedPopulationSpeciesId = galaxy.Civilizations.Single(c => c.Id == player).SpeciesId;
        galaxy.Fleets.Add(colony);
        var coordinator = new GalaxySimulationStepCoordinator();
        Require(coordinator.IssueCivilianHoldOrder(galaxy, player, colony.Id).Accepted, "colony hold rejected");
        new ExplorationSimulation().Advance(galaxy, 1);
        Require(colony.CurrentSystemId == target.Id && colony.DestinationSystemId == target.Id && colony.DestinationPlanetaryBodyId == body.Id &&
            colony.EmbarkedPopulationMillions == 2.5, "held final arrival lost its pending colony authorization or passengers");
        var path = Path.Combine(Path.GetTempPath(), $"stellar-hold-{Guid.NewGuid():N}.json");
        try
        {
            new CampaignSaveService().Save(path, galaxy, 1);
            var restored = new CampaignSaveService().Load(path).Galaxy.Fleets.Single(fleet => fleet.Id == colony.Id);
            Require(restored.HoldRequested && restored.DestinationSystemId == target.Id && restored.DestinationPlanetaryBodyId == body.Id &&
                restored.EmbarkedPopulationMillions == 2.5, "held colony authorization did not survive save/load");
        }
        finally { if (File.Exists(path)) File.Delete(path); if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); }
    }

    private static void RejectsForeignInactiveAndUnsupportedShips()
    {
        var galaxy = CreateGalaxy(); var player = galaxy.PlayerCivilizationId; var system = galaxy.Systems[0];
        var foreign = Fleet(player + 999, 99004, FleetRole.Scout, system);
        var military = Fleet(player, 99005, FleetRole.Military, system);
        var inactive = Fleet(player, 99006, FleetRole.Science, system); inactive.IsActive = false;
        galaxy.Fleets.Add(foreign); galaxy.Fleets.Add(military); galaxy.Fleets.Add(inactive);
        var coordinator = new GalaxySimulationStepCoordinator();
        Require(!coordinator.IssueCivilianHoldOrder(galaxy, player, foreign.Id).Accepted && !foreign.HoldRequested, "foreign hold mutated state");
        Require(!coordinator.IssueCivilianHoldOrder(galaxy, player, military.Id).Accepted && !military.HoldRequested, "military hold mutated state");
        Require(!coordinator.IssueCivilianHoldOrder(galaxy, player, inactive.Id).Accepted && !inactive.HoldRequested, "inactive hold mutated state");
    }

    private static FleetState Fleet(int owner, int id, FleetRole role, StarSystemState system) => new()
    {
        Id = id, CivilizationId = owner, Name = $"Hold {role}", Role = role, Position = system.Position,
        CurrentSystemId = system.Id, StrategicSpeed = 1000, MaximumLegRangeLightYears = 10000,
        FuelCapacityLightYears = 10000, FuelRemainingLightYears = 10000, SensorRange = 100,
    };

    private static GalaxyState CreateGalaxy() => new GalaxyGenerator().Generate(91473,
        new GalaxyGenerationSettings { SystemCount = 12, PreWarpCivilizationCount = 1, AncientCivilizationCount = 0, Radius = 200 });
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

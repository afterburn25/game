using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Game.Persistence;
using Game.Simulation;
using Game.Simulation.Colonization;
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
        PaidColonyHoldPreservesStartedSettlementAndAuthorization();
        RejectsForeignInactiveAndUnsupportedShips();
    }

    private static void HoldCompletesOneLaneThenRetainsRoute()
    {
        var galaxy = CreateGalaxy(); var player = galaxy.PlayerCivilizationId;
        var homeId = galaxy.Colonies.First(colony => colony.CivilizationId == player).SystemId;
        var systems = galaxy.Systems.Where(system => system.Id != homeId).Take(3).ToArray();
        var scout = Fleet(player, 99001, FleetRole.Scout, systems[0], strategicSpeed: 10);
        scout.Position = (systems[0].Position + systems[1].Position) / 2;
        scout.CurrentSystemId = null; scout.DestinationSystemId = systems[2].Id;
        scout.PlannedRouteSystemIds.Add(systems[1].Id); scout.PlannedRouteSystemIds.Add(systems[2].Id);
        var initialFuel = scout.FuelRemainingLightYears = scout.FuelCapacityLightYears;
        var firstLeg = System.Numerics.Vector2.Distance(scout.Position, systems[1].Position);
        galaxy.Fleets.Add(scout);
        var coordinator = new GalaxySimulationStepCoordinator();
        new ExplorationSimulation().Advance(galaxy, firstLeg / scout.StrategicSpeed / 4);
        RequireNear(scout.FuelRemainingLightYears, initialFuel - firstLeg / 4, "partial transit did not debit exact fuel");
        Require(scout.CurrentSystemId is null, "partial transit reached the lane endpoint too soon");
        Require(coordinator.IssueCivilianHoldOrder(galaxy, player, scout.Id).Accepted, "transit hold rejected");
        AssertSaveState(galaxy, scout.Id, "mid-lane hold did not persist");
        new ExplorationSimulation().Advance(galaxy, firstLeg / scout.StrategicSpeed);
        Require(scout.CurrentSystemId == systems[1].Id && scout.DestinationSystemId == systems[2].Id &&
            scout.PlannedRouteSystemIds.SequenceEqual(new[] { systems[2].Id }), "hold crossed more than one lane or discarded the remaining route");
        RequireNear(scout.FuelRemainingLightYears, initialFuel - firstLeg, "held lane did not debit its exact physical fuel");
        AssertSaveState(galaxy, scout.Id, "intermediate held stop did not persist");
        Require(coordinator.IssueCivilianResumeOrder(galaxy, player, scout.Id).Accepted, "transit resume rejected");
        new ExplorationSimulation().Advance(galaxy, 10_000);
        Require(scout.CurrentSystemId == systems[2].Id && scout.DestinationSystemId is null, "resumed ship did not complete its retained route");
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

    private static void PaidColonyHoldPreservesStartedSettlementAndAuthorization()
    {
        var galaxy = CreateGalaxy(); var player = galaxy.PlayerCivilizationId;
        foreach (var system in galaxy.Systems) galaxy.Knowledge.MarkSystemFullySurveyed(player, system.Id);
        var home = galaxy.Systems.First(system => system.Id == galaxy.Colonies.First(colony => colony.CivilizationId == player).SystemId);
        var colony = Fleet(player, 99003, FleetRole.Colony, home);
        colony.EmbarkedPopulationMillions = 2.5;
        colony.EmbarkedPopulationSpeciesId = galaxy.Civilizations.Single(c => c.Id == player).SpeciesId;
        galaxy.Fleets.Add(colony);
        var economy = galaxy.Economies.Single(item => item.CivilizationId == player); economy.Credits = 10000;
        var planning = new ColonizationSimulation().GetOpportunityPlan(galaxy, colony.Id, 64);
        var target = planning.Candidates.First(candidate => candidate.CanOrder);
        var beforeCredits = economy.Credits;
        var coordinator = new GalaxySimulationStepCoordinator();
        Require(coordinator.IssueColonyFleetOrder(galaxy, player, colony.Id, target.SystemId, target.PlanetaryBodyId).Accepted,
            "paid colony authorization rejected");
        RequireNear(economy.Credits, beforeCredits - ColonizationSimulation.ColonyExpeditionCreditCost, "colony authorization did not debit its quoted fee");
        var targetSystem = galaxy.Systems.Single(system => system.Id == target.SystemId);
        colony.CurrentSystemId = null; colony.Position = targetSystem.Position;
        colony.PlannedRouteSystemIds.Clear(); colony.PlannedRouteSystemIds.Add(target.SystemId);
        Require(coordinator.IssueCivilianHoldOrder(galaxy, player, colony.Id).Accepted, "colony hold rejected");
        var exploration = new ExplorationSimulation(); var colonization = new ColonizationSimulation();
        exploration.Advance(galaxy, 1);
        Require(colony.CurrentSystemId == target.SystemId && colony.DestinationSystemId == target.SystemId && colony.DestinationPlanetaryBodyId == target.PlanetaryBodyId &&
            colony.EmbarkedPopulationMillions == 2.5, "held final arrival lost its pending colony authorization or passengers");
        AssertSaveState(galaxy, colony.Id, "held final colony arrival did not persist");
        Require(coordinator.IssueCivilianResumeOrder(galaxy, player, colony.Id).Accepted, "colony resume rejected");
        exploration.Advance(galaxy, 1); // consumes final arrival
        colonization.Advance(galaxy, 1); // establishes the local work site
        Require(colony.SettlementBodyId == target.PlanetaryBodyId && colony.SettlementDaysCompleted == 0, "resumed colony did not start its authorized settlement");
        Require(coordinator.IssueCivilianHoldOrder(galaxy, player, colony.Id).Accepted, "started settlement hold rejected");
        colonization.Advance(galaxy, 3);
        Require(colony.SettlementDaysCompleted == 0, "held settlement progressed");
        RequireNear(economy.Credits, beforeCredits - ColonizationSimulation.ColonyExpeditionCreditCost, "hold changed paid authorization treasury");
        Require(colony.EmbarkedPopulationMillions == 2.5, "hold changed embarked colonists");
        Require(coordinator.IssueCivilianResumeOrder(galaxy, player, colony.Id).Accepted, "started settlement resume rejected");
        colonization.Advance(galaxy, 3);
        Require(colony.SettlementDaysCompleted > 0, "resumed settlement did not progress");
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
        inactive.HoldRequested = true;
        AssertSaveState(galaxy, inactive.Id, "deactivated held civilian ship made the campaign unsaveable");
    }

    private static FleetState Fleet(int owner, int id, FleetRole role, StarSystemState system, double strategicSpeed = 1000) => new()
    {
        Id = id, CivilizationId = owner, Name = $"Hold {role}", Role = role, Position = system.Position,
        CurrentSystemId = system.Id, StrategicSpeed = strategicSpeed, MaximumLegRangeLightYears = 10000,
        FuelCapacityLightYears = 10000, FuelRemainingLightYears = 10000, SensorRange = 100,
    };

    private static GalaxyState CreateGalaxy() => new GalaxyGenerator().Generate(91473,
        new GalaxyGenerationSettings { SystemCount = 12, PreWarpCivilizationCount = 1, AncientCivilizationCount = 0, Radius = 200 });
    private static void AssertSaveState(GalaxyState galaxy, int fleetId, string message)
    {
        var path = Path.Combine(Path.GetTempPath(), $"stellar-hold-{Guid.NewGuid():N}.json");
        try
        {
            var saves = new CampaignSaveService(); saves.Save(path, galaxy, 1);
            var restored = saves.Load(path).Galaxy.Fleets.Single(fleet => fleet.Id == fleetId);
            Require(restored.HoldRequested == galaxy.Fleets.Single(fleet => fleet.Id == fleetId).HoldRequested, message);
            var legacy = JsonNode.Parse(File.ReadAllText(path))!;
            legacy["Galaxy"]!["Fleets"]!.AsArray().OfType<JsonObject>().Single(item => item["Id"]!.GetValue<int>() == fleetId).Remove("HoldRequested");
            File.WriteAllText(path, legacy.ToJsonString());
            Require(!saves.Load(path).Galaxy.Fleets.Single(fleet => fleet.Id == fleetId).HoldRequested, "missing legacy hold field did not default false");
        }
        finally { if (File.Exists(path)) File.Delete(path); if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); }
    }
    private static void RequireNear(double actual, double expected, string message)
    {
        if (Math.Abs(actual - expected) > .0001) throw new InvalidOperationException($"{message}: {actual} != {expected}");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

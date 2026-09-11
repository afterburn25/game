using System.Numerics;
using Game.Persistence;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class FleetLocalTransitValidation
{
    [Game.Validation.RegressionCheck]
    internal static void ValidatePersistedGateCrossingsAndPartitioning()
    {
        var first = CreateScenario();
        var fleet = first.Fleet;
        var simulation = new ExplorationSimulation();
        var initialPosition = fleet.Position;
        simulation.Advance(first.Galaxy, 0);
        Require(fleet.Position == initialPosition && fleet.TransitPhase == FleetTransitPhase.None,
            "zero delta changed an unstarted local transit");

        simulation.Advance(first.Galaxy, .2);
        Require(fleet.TransitPhase == FleetTransitPhase.LocalDeparture && fleet.Position == initialPosition,
            "local departure changed the strategic system coordinate");
        Require(fleet.LocalTransitPosition.Length() > 0 && fleet.LocalTransitPosition.Length() < FleetLocalTransit.GateRadius,
            "local departure did not advance through normalized chart space");
        Require(Vector2.Dot(Vector2.Normalize(fleet.LocalTransitTarget),
                Vector2.Normalize(first.Next.Position - first.Origin.Position)) > .999f,
            "outbound gate did not follow the next lane direction");

        var directory = Path.Combine(Path.GetTempPath(), "stellar-local-transit-" + Guid.NewGuid().ToString("N"));
        try
        {
            var savePath = Path.Combine(directory, "campaign.json");
            new CampaignSaveService().Save(savePath, first.Galaxy, 1);
            var restored = new CampaignSaveService().Load(savePath).Galaxy;
            var restoredFleet = restored.Fleets.Single(candidate => candidate.Id == fleet.Id);
            Require(restoredFleet.TransitPhase == FleetTransitPhase.LocalDeparture &&
                    Vector2.Distance(restoredFleet.LocalTransitPosition, fleet.LocalTransitPosition) < .000001f,
                "save/load lost a partial local crossing");

            CivilianFleetHoldOrders.Hold(restored, restoredFleet.CivilizationId, restoredFleet.Id);
            var held = restoredFleet.LocalTransitPosition;
            new ExplorationSimulation().Advance(restored, 3);
            Require(restoredFleet.LocalTransitPosition == held && restoredFleet.FuelRemainingLightYears == fleet.FuelRemainingLightYears,
                "a local hold changed chart position or light-year fuel");
            CivilianFleetHoldOrders.Resume(restored, restoredFleet.CivilizationId, restoredFleet.Id);
            new ExplorationSimulation().Advance(restored, 10_000);
            Require(restoredFleet.CurrentSystemId == first.Final.Id && restoredFleet.DestinationSystemId is null &&
                    restoredFleet.TransitPhase == FleetTransitPhase.None && restoredFleet.LocalTransitPosition == Vector2.Zero,
                "route did not complete the final inbound-gate-to-mission-centre crossing");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        var large = CreateScenario();
        new ExplorationSimulation().Advance(large.Galaxy, 10_000);
        var partitioned = CreateScenario();
        var partitionedSimulation = new ExplorationSimulation();
        for (var i = 0; i < 100; i++) partitionedSimulation.Advance(partitioned.Galaxy, 100);
        Require(large.Fleet.CurrentSystemId == partitioned.Fleet.CurrentSystemId &&
                large.Fleet.DestinationSystemId == partitioned.Fleet.DestinationSystemId &&
                large.Fleet.TransitPhase == partitioned.Fleet.TransitPhase &&
                Vector2.Distance(large.Fleet.LocalTransitPosition, partitioned.Fleet.LocalTransitPosition) < .000001f,
            "local and warp time budgets changed outcome when partitioned");
    }

    private static (GalaxyState Galaxy, FleetState Fleet, StarSystemState Origin, StarSystemState Next, StarSystemState Final) CreateScenario()
    {
        var galaxy = new GalaxyGenerator().Generate(0x10CA1_2026L, new GalaxyGenerationSettings
        { SystemCount = 48, PreWarpCivilizationCount = 5, AncientCivilizationCount = 1, Radius = 500 });
        var player = galaxy.Civilizations.Single(civilization => civilization.Id == galaxy.PlayerCivilizationId);
        var origin = galaxy.Systems.Single(system => system.Id == player.HomeSystemId);
        var network = new InterstellarLaneNetwork();
        var choice = galaxy.Systems.Select(system => (System: system, Route: network.FindShortestRoute(galaxy.Systems, origin.Id, system.Id, 360)))
            .Where(candidate => candidate.Route.Count >= 3).OrderByDescending(candidate => candidate.Route.Count).First();
        var fleet = new FleetState
        {
            Id = galaxy.Fleets.Select(existing => existing.Id).DefaultIfEmpty(0).Max() + 1000,
            CivilizationId = player.Id, Name = "Local gate validation", Role = FleetRole.Scout,
            Position = origin.Position, CurrentSystemId = origin.Id, StrategicSpeed = 22,
            MaximumLegRangeLightYears = 360, FuelCapacityLightYears = 10000, FuelRemainingLightYears = 10000,
            SensorRange = 135, IsActive = true,
        };
        galaxy.Fleets.Add(fleet);
        var reach = new LaneInterstellarOperationalReachView().Assess(galaxy, player.Id, fleet, choice.System.Id,
            InterstellarMissionKind.ScoutReconnaissance);
        FleetRouteOrders.Assign(galaxy, fleet, choice.System.Id, reach);
        return (galaxy, fleet, origin, galaxy.Systems.Single(system => system.Id == choice.Route[1]), choice.System);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

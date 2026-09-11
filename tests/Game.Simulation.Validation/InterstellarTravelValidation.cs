using Game.Persistence;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Models;
using Game.Simulation.Economy;

namespace Game.Simulation.Validation;

internal static class InterstellarTravelValidation
{
    public static void ValidateLaneRoutingAndPersistence()
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x4C41_4E45_2026L,
            new GalaxyGenerationSettings
            {
                SystemCount = 64,
                PreWarpCivilizationCount = 5,
                AncientCivilizationCount = 1,
                Radius = 560.0f,
            });
        var player = galaxy.Civilizations.First(civilization => civilization.Id == galaxy.PlayerCivilizationId);
        var origin = galaxy.Systems.First(system => system.Id == player.HomeSystemId);
        foreach (var system in galaxy.Systems)
            galaxy.Knowledge.RevealSystem(player.Id, system.Id);
        var network = new InterstellarLaneNetwork();
        var routedTarget = galaxy.Systems
            .Select(system => new
            {
                System = system,
                Route = network.FindShortestRoute(galaxy.Systems, origin.Id, system.Id, 360.0),
            })
            .Where(candidate => candidate.Route.Count >= 3)
            .OrderByDescending(candidate => candidate.Route.Count)
            .ThenBy(candidate => candidate.System.Id)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("validation galaxy did not provide a multi-leg route");

        var fleet = new FleetState
        {
            Id = galaxy.Fleets.Count == 0 ? 1000 : galaxy.Fleets.Max(existing => existing.Id) + 1000,
            CivilizationId = player.Id,
            Name = "Lane Persistence Validation Scout",
            Role = FleetRole.Scout,
            Position = origin.Position,
            CurrentSystemId = origin.Id,
            StrategicSpeed = 22.0,
            MaximumLegRangeLightYears = 360.0,
            FuelCapacityLightYears = 10000.0,
            FuelRemainingLightYears = 10000.0,
            SensorRange = 135.0f,
            IsActive = true,
        };
        galaxy.Fleets.Add(fleet);

        var reach = new LaneInterstellarOperationalReachView().Assess(
            galaxy,
            player.Id,
            fleet,
            routedTarget.System.Id,
            InterstellarMissionKind.ScoutReconnaissance);
        Require(reach.IsSupported && reach.IsAuthoritative, "multi-leg route was not authoritatively supported");
        Require(reach.Reason.Contains("km", StringComparison.Ordinal) && reach.Reason.Contains("ly", StringComparison.Ordinal) &&
                !reach.Reason.Contains("e+", StringComparison.Ordinal),
            "route confirmation did not lead with a readable metric distance");
        Require(reach.RouteSystemIds!.SequenceEqual(routedTarget.Route), "reach assessment returned a different deterministic route");
        FleetRouteOrders.Assign(galaxy, fleet, routedTarget.System.Id, reach);
        Require(fleet.DestinationSystemId == routedTarget.System.Id, "route replaced the final mission destination with a waypoint");
        Require(fleet.PlannedRouteSystemIds.Count == routedTarget.Route.Count - 1, "route did not retain every remaining waypoint");

        var economy = galaxy.Economies.First(state => state.CivilizationId == player.Id);
        economy.LastBaseOperationsFundingFraction = 0.0;
        var heldPosition = fleet.Position;
        var heldFuel = fleet.FuelRemainingLightYears;
        new ExplorationSimulation().Advance(galaxy, 0.1);
        Require(fleet.Position == heldPosition && fleet.FuelRemainingLightYears == heldFuel,
            "unfunded fleet continued moving or consuming fuel");
        var suspended = new ExplorationMissionStatusEvaluator().Build(galaxy, fleet);
        Require(suspended.Summary.Contains("operations are unfunded", StringComparison.Ordinal),
            "unfunded mission did not expose an actionable suspension reason");

        economy.LastBaseOperationsFundingFraction = 1.0;
        new ExplorationSimulation().Advance(galaxy, FleetLocalTransit.GateRadius / FleetLocalTransit.Rate(fleet) + 0.1);
        Require(fleet.CurrentSystemId is null && fleet.TransitPhase == FleetTransitPhase.InterstellarWarp,
            "elapsed local departure plus a small lane step did not leave the fleet between systems");
        Require(fleet.DestinationSystemId == routedTarget.System.Id, "mid-flight travel lost the final mission target");

        var directory = Path.Combine(Path.GetTempPath(), "stellar-lane-validation-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(directory, "campaign.json");
            var saves = new CampaignSaveService();
            saves.Save(path, galaxy, 12.5);
            var restored = saves.Load(path).Galaxy.Fleets.Single(candidate => candidate.Id == fleet.Id);
            Require(restored.DestinationSystemId == fleet.DestinationSystemId, "save/load changed the final route destination");
            Require(restored.PlannedRouteSystemIds.SequenceEqual(fleet.PlannedRouteSystemIds), "save/load changed remaining lane waypoints");
            Require(Math.Abs(restored.MaximumLegRangeLightYears - 360.0) < 0.000001, "save/load changed maximum leg range");
            Require(restored.FuelRemainingLightYears < restored.FuelCapacityLightYears,
                "travel did not consume persisted fuel endurance");

            var completedGalaxy = saves.Load(path).Galaxy;
            new ExplorationSimulation().Advance(completedGalaxy, 1000.0);
            var completed = completedGalaxy.Fleets.Single(candidate => candidate.Id == fleet.Id);
            Require(completed.CurrentSystemId == routedTarget.System.Id && completed.DestinationSystemId is null,
                "fleet did not finish its full persisted route at the final target");
            Require(completed.PlannedRouteSystemIds.Count == 0, "completed route retained stale waypoints");

            var returnReach = new LaneInterstellarOperationalReachView().Assess(
                completedGalaxy, player.Id, completed, origin.Id, InterstellarMissionKind.ScoutReconnaissance);
            Require(returnReach.IsSupported, "fleet could not plot a fueled return route to its home colony");
            FleetRouteOrders.Assign(completedGalaxy, completed, origin.Id, returnReach);
            new ExplorationSimulation().Advance(completedGalaxy, 1000.0);
            Require(completed.CurrentSystemId == origin.Id &&
                    Math.Abs(completed.FuelRemainingLightYears - completed.FuelCapacityLightYears) < 0.000001,
                "arrival at an owned colony did not refill fleet endurance");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

        var shortRangeFleet = new FleetState
        {
            Id = fleet.Id + 1,
            CivilizationId = player.Id,
            Name = "Short Range Validation Vessel",
            Role = FleetRole.Science,
            Position = origin.Position,
            CurrentSystemId = origin.Id,
            MaximumLegRangeLightYears = 0.01,
            IsActive = true,
        };
        var blocked = new LaneInterstellarOperationalReachView().Assess(
            galaxy,
            player.Id,
            shortRangeFleet,
            routedTarget.System.Id,
            InterstellarMissionKind.ScienceSurvey);
        Require(!blocked.IsSupported && blocked.Reason.Contains("maximum leg range", StringComparison.OrdinalIgnoreCase),
            "unreachable route did not return a useful leg-range rejection");
        Require(blocked.Reason.Contains("km", StringComparison.Ordinal) && blocked.Reason.Contains("ly", StringComparison.Ordinal),
            "leg-range rejection did not lead with a metric distance");

        var lowFuelFleet = new FleetState
        {
            Id = fleet.Id + 2,
            CivilizationId = player.Id,
            Name = "Low Fuel Validation Vessel",
            Role = FleetRole.Science,
            Position = origin.Position,
            CurrentSystemId = origin.Id,
            MaximumLegRangeLightYears = 10000.0,
            FuelCapacityLightYears = 0.01,
            FuelRemainingLightYears = 0.01,
            IsActive = true,
        };
        var fuelBlocked = new LaneInterstellarOperationalReachView().Assess(
            galaxy, player.Id, lowFuelFleet, routedTarget.System.Id, InterstellarMissionKind.ScienceSurvey);
        Require(!fuelBlocked.IsSupported && fuelBlocked.Reason.Contains("fuel endurance", StringComparison.OrdinalIgnoreCase),
            "fuel-limited route did not return a useful endurance rejection");
        Require(fuelBlocked.Reason.Contains("km", StringComparison.Ordinal) && fuelBlocked.Reason.Contains("ly", StringComparison.Ordinal),
            "fuel-endurance rejection did not lead with a metric distance");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

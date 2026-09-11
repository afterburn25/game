using System;
using System.IO;
using System.Linq;
using System.Numerics;
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
    internal static void Run()
    {
        HoldCompletesOneLaneThenRetainsRoute();
        HeldPartialScoutAndScienceWorkResume();
        PaidColonyHoldPreservesStartedSettlementAndAuthorization();
        RejectsForeignInactiveAndUnsupportedShips();
        ReturnToBaseUsesPhysicalRouteAndFailsSafe();
    }

    private static void HoldCompletesOneLaneThenRetainsRoute()
    {
        var galaxy = CreateGalaxy();
        var player = galaxy.PlayerCivilizationId;
        var homeId = galaxy.Colonies.First(colony => colony.CivilizationId == player).SystemId;
        var systems = galaxy.Systems.Where(system => system.Id != homeId).Take(3).ToArray();
        var scout = Fleet(player, 99001, FleetRole.Scout, systems[0], strategicSpeed: 10);
        scout.DestinationSystemId = systems[2].Id;
        scout.PlannedRouteSystemIds.Add(systems[1].Id);
        scout.PlannedRouteSystemIds.Add(systems[2].Id);
        var initialPosition = scout.Position;
        var initialFuel = scout.FuelRemainingLightYears;
        var firstLeg = Vector2.Distance(initialPosition, systems[1].Position);
        var secondLeg = Vector2.Distance(systems[1].Position, systems[2].Position);
        galaxy.Fleets.Add(scout);
        var exploration = new ExplorationSimulation();
        var coordinator = new GalaxySimulationStepCoordinator();

        exploration.Advance(galaxy, firstLeg / scout.StrategicSpeed / 4);
        RequirePosition(scout.Position, Vector2.Lerp(initialPosition, systems[1].Position, .25f),
            "partial transit did not move incrementally along the current lane");
        RequireNear(scout.FuelRemainingLightYears, initialFuel - firstLeg / 4,
            "partial transit did not debit exact fuel");
        Require(scout.CurrentSystemId is null && scout.DestinationSystemId == systems[2].Id &&
                scout.PlannedRouteSystemIds.SequenceEqual(new[] { systems[1].Id, systems[2].Id }),
            "partial transit changed the represented route before hold issuance");

        Require(coordinator.IssueCivilianHoldOrder(galaxy, player, scout.Id).Accepted, "transit hold rejected");
        var heldStatus = new ExplorationMissionStatusEvaluator().Build(galaxy, scout);
        Require(heldStatus.Phase == ExplorationMissionPhase.Traveling &&
                heldStatus.Summary.Contains("holding after reaching", StringComparison.Ordinal),
            "mid-lane hold status did not explain the one-lane stopping rule");
        AssertSaveState(galaxy, scout.Id, "mid-lane hold did not persist");
        AssertLegacyMissingHoldDefaultsFalse(galaxy, scout.Id);

        exploration.Advance(galaxy, firstLeg / scout.StrategicSpeed / 4);
        RequirePosition(scout.Position, Vector2.Lerp(initialPosition, systems[1].Position, .5f),
            "held in-transit scout stopped before reaching its lane endpoint");
        RequireNear(scout.FuelRemainingLightYears, initialFuel - firstLeg / 2,
            "held incremental movement did not debit exact fuel");
        Require(scout.CurrentSystemId is null && scout.HoldRequested &&
                scout.PlannedRouteSystemIds.SequenceEqual(new[] { systems[1].Id, systems[2].Id }),
            "held incremental movement changed the remaining route");

        var economy = galaxy.Economies.Single(item => item.CivilizationId == player);
        economy.LastBaseOperationsFundingFraction = 0;
        var unfundedStatus = new ExplorationMissionStatusEvaluator().Build(galaxy, scout);
        var unfundedSnapshot = Snapshot(galaxy, scout.Id);
        Require(unfundedStatus.Phase == ExplorationMissionPhase.AwaitingOrder &&
                unfundedStatus.Summary.Contains("unfunded", StringComparison.OrdinalIgnoreCase),
            "held mission status did not disclose the no-funding suspension");
        exploration.Advance(galaxy, 1000);
        AssertSnapshot(Snapshot(galaxy, scout.Id), unfundedSnapshot,
            "unfunded held transit mutated before operating funding was restored");
        economy.LastBaseOperationsFundingFraction = 1;
        Require(scout.HoldRequested, "restoring operating funding cancelled the civilian hold");

        exploration.Advance(galaxy, 10_000);
        RequirePosition(scout.Position, systems[1].Position,
            "held transit did not stop at the current lane endpoint");
        Require(scout.CurrentSystemId == systems[1].Id && scout.DestinationSystemId == systems[2].Id &&
                scout.PlannedRouteSystemIds.SequenceEqual(new[] { systems[2].Id }),
            "hold crossed more than one lane or discarded the retained final route");
        RequireNear(scout.FuelRemainingLightYears, initialFuel - firstLeg,
            "held lane completion did not debit exact physical fuel");
        AssertSaveState(galaxy, scout.Id, "intermediate held stop did not persist");

        Require(coordinator.IssueCivilianResumeOrder(galaxy, player, scout.Id).Accepted, "transit resume rejected");
        exploration.Advance(galaxy, 10_000);
        RequirePosition(scout.Position, systems[2].Position,
            "resumed scout did not reach its retained final destination");
        Require(scout.CurrentSystemId == systems[2].Id && scout.DestinationSystemId is null &&
                scout.PlannedRouteSystemIds.Count == 0 && !scout.HoldRequested,
            "resumed scout did not complete and clear its retained route");
        RequireNear(scout.FuelRemainingLightYears, initialFuel - firstLeg - secondLeg,
            "resumed route did not debit exact remaining physical fuel");
    }

    private static void HeldPartialScoutAndScienceWorkResume()
    {
        var galaxy = CreateGalaxy();
        var player = galaxy.PlayerCivilizationId;
        var homeId = galaxy.Colonies.First(colony => colony.CivilizationId == player).SystemId;
        var systems = galaxy.Systems.Where(system => system.Id != homeId).Take(2).ToArray();
        var scout = Fleet(player, 99002, FleetRole.Scout, systems[0]);
        var science = Fleet(player, 99003, FleetRole.Science, systems[1]);
        galaxy.Fleets.Add(scout);
        galaxy.Fleets.Add(science);
        var exploration = new ExplorationSimulation();
        var coordinator = new GalaxySimulationStepCoordinator();

        exploration.Advance(galaxy, .5);
        var scoutProgress = scout.ReconnaissanceDaysCompleted;
        var scienceProgress = galaxy.Knowledge.GetSystemSurveyProgress(player, systems[1].Id);
        Require(scout.ReconnaissanceSystemId == systems[0].Id && scoutProgress > 0 &&
                scoutProgress < ExplorationSimulation.ScoutReconnaissanceDays,
            "scout fixture did not establish nonzero partial reconnaissance");
        Require(scienceProgress > 0 && scienceProgress < 1,
            "science fixture did not establish nonzero partial survey progress");
        Require(coordinator.IssueCivilianHoldOrder(galaxy, player, scout.Id).Accepted &&
                coordinator.IssueCivilianHoldOrder(galaxy, player, science.Id).Accepted,
            "partial local-work hold was rejected");
        AssertSaveState(galaxy, scout.Id, "held partial scout work did not survive two save generations");
        AssertSaveState(galaxy, science.Id, "held partial science work did not survive two save generations");

        exploration.Advance(galaxy, 5);
        RequireNear(scout.ReconnaissanceDaysCompleted, scoutProgress, "held scout performed local work");
        RequireNear(galaxy.Knowledge.GetSystemSurveyProgress(player, systems[1].Id), scienceProgress,
            "held science vessel performed local work");

        Require(coordinator.IssueCivilianResumeOrder(galaxy, player, scout.Id).Accepted, "scout resume rejected");
        exploration.Advance(galaxy, .25);
        Require(scout.ReconnaissanceDaysCompleted > scoutProgress &&
                Math.Abs(galaxy.Knowledge.GetSystemSurveyProgress(player, systems[1].Id) - scienceProgress) < .0001,
            "resuming scout did not advance only its retained partial local work");
        Require(coordinator.IssueCivilianResumeOrder(galaxy, player, science.Id).Accepted, "science resume rejected");
        exploration.Advance(galaxy, .25);
        Require(galaxy.Knowledge.GetSystemSurveyProgress(player, systems[1].Id) > scienceProgress,
            "resumed science vessel did not continue its partial survey");
    }

    private static void PaidColonyHoldPreservesStartedSettlementAndAuthorization()
    {
        var galaxy = CreateGalaxy();
        var player = galaxy.PlayerCivilizationId;
        foreach (var system in galaxy.Systems) galaxy.Knowledge.MarkSystemFullySurveyed(player, system.Id);
        var home = galaxy.Systems.First(system => system.Id ==
            galaxy.Colonies.First(colony => colony.CivilizationId == player).SystemId);
        var colony = Fleet(player, 99004, FleetRole.Colony, home);
        colony.EmbarkedPopulationMillions = 2.5;
        colony.EmbarkedPopulationSpeciesId = galaxy.Civilizations.Single(c => c.Id == player).SpeciesId;
        galaxy.Fleets.Add(colony);
        var economy = galaxy.Economies.Single(item => item.CivilizationId == player);
        economy.Credits = 10000;
        var colonization = new ColonizationSimulation();
        var target = colonization.GetOpportunityPlan(galaxy, colony.Id, 64).Candidates.First(candidate => candidate.CanOrder);
        var beforeCredits = economy.Credits;
        var coordinator = new GalaxySimulationStepCoordinator();
        Require(coordinator.IssueColonyFleetOrder(galaxy, player, colony.Id, target.SystemId, target.PlanetaryBodyId).Accepted,
            "paid colony authorization rejected");
        RequireNear(economy.Credits, beforeCredits - ColonizationSimulation.ColonyExpeditionCreditCost,
            "colony authorization did not debit its quoted fee");

        var targetSystem = galaxy.Systems.Single(system => system.Id == target.SystemId);
        colony.CurrentSystemId = null;
        colony.Position = targetSystem.Position;
        colony.PlannedRouteSystemIds.Clear();
        colony.PlannedRouteSystemIds.Add(target.SystemId);
        Require(coordinator.IssueCivilianHoldOrder(galaxy, player, colony.Id).Accepted, "colony hold rejected");
        var exploration = new ExplorationSimulation();
        exploration.Advance(galaxy, 1);
        Require(colony.CurrentSystemId == target.SystemId && colony.DestinationSystemId == target.SystemId &&
                colony.DestinationPlanetaryBodyId == target.PlanetaryBodyId && colony.EmbarkedPopulationMillions == 2.5,
            "held final arrival lost its pending colony authorization or passengers");
        AssertSaveState(galaxy, colony.Id, "held final colony arrival did not persist");

        Require(coordinator.IssueCivilianResumeOrder(galaxy, player, colony.Id).Accepted, "colony resume rejected");
        exploration.Advance(galaxy, 1);
        colonization.Advance(galaxy, 1);
        colonization.Advance(galaxy, 4);
        Require(colony.SettlementBodyId == target.PlanetaryBodyId && colony.SettlementDaysCompleted > 0,
            "resumed colony did not establish nonzero paid settlement progress");
        var startedProgress = colony.SettlementDaysCompleted;
        Require(coordinator.IssueCivilianHoldOrder(galaxy, player, colony.Id).Accepted, "started settlement hold rejected");
        AssertSaveState(galaxy, colony.Id, "held nonzero settlement progress or paid mission state did not persist");
        colonization.Advance(galaxy, 3);
        RequireNear(colony.SettlementDaysCompleted, startedProgress, "held settlement progressed");
        RequireNear(economy.Credits, beforeCredits - ColonizationSimulation.ColonyExpeditionCreditCost,
            "hold changed paid authorization treasury");
        Require(colony.EmbarkedPopulationMillions == 2.5 &&
                colony.EmbarkedPopulationSpeciesId == galaxy.Civilizations.Single(c => c.Id == player).SpeciesId,
            "hold changed embarked colonists or their species");
        Require(coordinator.IssueCivilianResumeOrder(galaxy, player, colony.Id).Accepted, "started settlement resume rejected");
        colonization.Advance(galaxy, 3);
        RequireNear(colony.SettlementDaysCompleted, startedProgress + 3,
            "resumed settlement did not continue from its exact paid progress");
    }

    private static void RejectsForeignInactiveAndUnsupportedShips()
    {
        var galaxy = CreateGalaxy();
        var player = galaxy.PlayerCivilizationId;
        var system = galaxy.Systems[0];
        var foreign = Fleet(player + 999, 99005, FleetRole.Scout, system);
        var military = Fleet(player, 99006, FleetRole.Military, system);
        var inactive = Fleet(player, 99007, FleetRole.Science, system);
        inactive.IsActive = false;
        galaxy.Fleets.Add(foreign);
        galaxy.Fleets.Add(military);
        galaxy.Fleets.Add(inactive);
        var coordinator = new GalaxySimulationStepCoordinator();
        Require(!coordinator.IssueCivilianHoldOrder(galaxy, player, foreign.Id).Accepted && !foreign.HoldRequested,
            "foreign hold mutated state");
        Require(!coordinator.IssueCivilianHoldOrder(galaxy, player, military.Id).Accepted && !military.HoldRequested,
            "military hold mutated state");
        Require(!coordinator.IssueCivilianHoldOrder(galaxy, player, inactive.Id).Accepted && !inactive.HoldRequested,
            "inactive hold mutated state");

        var persistenceGalaxy = CreateGalaxy();
        var persistencePlayer = persistenceGalaxy.PlayerCivilizationId;
        var persistedInactive = Fleet(persistencePlayer, 99007, FleetRole.Science, persistenceGalaxy.Systems[0]);
        persistedInactive.IsActive = false;
        persistedInactive.HoldRequested = true;
        persistenceGalaxy.Fleets.Add(persistedInactive);
        AssertSaveState(persistenceGalaxy, persistedInactive.Id,
            "deactivated held civilian ship made the campaign unsaveable");
    }

    private static void ReturnToBaseUsesPhysicalRouteAndFailsSafe()
    {
        var galaxy = CreateGalaxy(); var player = galaxy.PlayerCivilizationId;
        var home = galaxy.Colonies.First(colony => colony.CivilizationId == player);
        var systems = galaxy.Systems.Where(system => system.Id != home.SystemId).Take(2).ToArray();
        var scout = Fleet(player, 99008, FleetRole.Scout, systems[0], strategicSpeed: 10);
        scout.Position = (systems[0].Position + systems[1].Position) / 2;
        scout.CurrentSystemId = null; scout.DestinationSystemId = systems[1].Id;
        scout.PlannedRouteSystemIds.Add(systems[1].Id);
        galaxy.Fleets.Add(scout);
        var coordinator = new GalaxySimulationStepCoordinator();
        Require(coordinator.IssueCivilianReturnToBaseOrder(galaxy, player, scout.Id).Accepted && scout.ReturnToBaseRequested,
            "mid-lane return intent was not accepted");
        var path = Path.Combine(Path.GetTempPath(), $"stellar-return-{Guid.NewGuid():N}.json");
        try
        {
            new CampaignSaveService().Save(path, galaxy, 1);
            Require(new CampaignSaveService().Load(path).Galaxy.Fleets.Single(fleet => fleet.Id == scout.Id).ReturnToBaseRequested,
                "queued return intent did not persist");
        }
        finally { DeleteSave(path); }
        new ExplorationSimulation().Advance(galaxy, 10000);
        Require(scout.CurrentSystemId == systems[1].Id && scout.DestinationSystemId == home.SystemId && scout.ReturnToBaseRequested,
            "queued return did not wait for the current lane then assign a physical base route");
        Require(coordinator.IssueCivilianReturnToBaseOrder(galaxy, player + 1, scout.Id).Accepted == false,
            "foreign return command was accepted");
        galaxy.Colonies.Where(colony => colony.CivilizationId == player).ToList().ForEach(colony => galaxy.Colonies.Remove(colony));
        scout.CurrentSystemId = systems[1].Id; scout.Position = systems[1].Position;
        scout.DestinationSystemId = systems[0].Id; scout.PlannedRouteSystemIds = new() { systems[0].Id };
        scout.ReturnToBaseRequested = true;
        new ExplorationSimulation().Advance(galaxy, 10000);
        Require(scout.HoldRequested && !scout.ReturnToBaseRequested && scout.ReturnToBaseFailureReason is not null,
            "lost base did not convert queued return into a safe held recovery state");
    }

    private static FleetState Fleet(int owner, int id, FleetRole role, StarSystemState system,
        double strategicSpeed = 1000) => new()
    {
        Id = id, CivilizationId = owner, Name = $"Hold {role}", Role = role, Position = system.Position,
        CurrentSystemId = system.Id, StrategicSpeed = strategicSpeed, MaximumLegRangeLightYears = 10000,
        FuelCapacityLightYears = 10000, FuelRemainingLightYears = 10000, SensorRange = 100,
    };

    private static GalaxyState CreateGalaxy() => new GalaxyGenerator().Generate(91473,
        new GalaxyGenerationSettings { SystemCount = 12, PreWarpCivilizationCount = 1, AncientCivilizationCount = 0, Radius = 200 });

    private static void AssertSaveState(GalaxyState galaxy, int fleetId, string message)
    {
        var firstPath = Path.Combine(Path.GetTempPath(), $"stellar-hold-first-{Guid.NewGuid():N}.json");
        var secondPath = Path.Combine(Path.GetTempPath(), $"stellar-hold-second-{Guid.NewGuid():N}.json");
        try
        {
            var expected = Snapshot(galaxy, fleetId);
            var saves = new CampaignSaveService();
            saves.Save(firstPath, galaxy, 1.25);
            var first = saves.Load(firstPath).Galaxy;
            AssertSnapshot(Snapshot(first, fleetId), expected, message + " (first generation)");
            saves.Save(secondPath, first, 2.5);
            var second = saves.Load(secondPath).Galaxy;
            AssertSnapshot(Snapshot(second, fleetId), expected, message + " (second generation)");
        }
        finally { DeleteSave(firstPath); DeleteSave(secondPath); }
    }

    private static void AssertLegacyMissingHoldDefaultsFalse(GalaxyState galaxy, int fleetId)
    {
        var path = Path.Combine(Path.GetTempPath(), $"stellar-hold-legacy-{Guid.NewGuid():N}.json");
        try
        {
            var expected = Snapshot(galaxy, fleetId) with { HoldRequested = false };
            var saves = new CampaignSaveService();
            saves.Save(path, galaxy, 3.75);
            var legacy = JsonNode.Parse(File.ReadAllText(path))!;
            legacy["Galaxy"]!["Fleets"]!.AsArray().OfType<JsonObject>()
                .Single(item => item["Id"]!.GetValue<int>() == fleetId).Remove("HoldRequested");
            File.WriteAllText(path, legacy.ToJsonString());
            AssertSnapshot(Snapshot(saves.Load(path).Galaxy, fleetId), expected,
                "missing legacy hold field did not default false without changing the mission");
        }
        finally { DeleteSave(path); }
    }

    private static PersistedFleetSnapshot Snapshot(GalaxyState galaxy, int fleetId)
    {
        var fleet = galaxy.Fleets.Single(candidate => candidate.Id == fleetId);
        var economy = galaxy.Economies.Single(item => item.CivilizationId == fleet.CivilizationId);
        var surveyProgress = fleet.CurrentSystemId is int systemId
            ? galaxy.Knowledge.GetSystemSurveyProgress(fleet.CivilizationId, systemId) : (double?)null;
        return new PersistedFleetSnapshot(fleet.Id, fleet.CivilizationId, fleet.Role, fleet.Position,
            fleet.CurrentSystemId, fleet.DestinationSystemId, fleet.PlannedRouteSystemIds.ToArray(),
            fleet.FuelRemainingLightYears, fleet.HoldRequested, fleet.IsActive, fleet.DestinationPlanetaryBodyId,
            fleet.PreventAutomaticSettlement, fleet.SettlementBodyId, fleet.SettlementDaysCompleted,
            fleet.ReconnaissanceSystemId, fleet.ReconnaissanceDaysCompleted, fleet.EmbarkedPopulationMillions,
            fleet.EmbarkedPopulationSpeciesId, surveyProgress, economy.Credits, economy.LastBaseOperationsFundingFraction);
    }

    private static void AssertSnapshot(PersistedFleetSnapshot actual, PersistedFleetSnapshot expected, string message)
    {
        Require(actual.Id == expected.Id && actual.CivilizationId == expected.CivilizationId &&
                actual.Role == expected.Role && actual.CurrentSystemId == expected.CurrentSystemId &&
                actual.DestinationSystemId == expected.DestinationSystemId &&
                actual.RouteSystemIds.SequenceEqual(expected.RouteSystemIds) &&
                actual.HoldRequested == expected.HoldRequested && actual.IsActive == expected.IsActive &&
                actual.DestinationPlanetaryBodyId == expected.DestinationPlanetaryBodyId &&
                actual.PreventAutomaticSettlement == expected.PreventAutomaticSettlement &&
                actual.SettlementBodyId == expected.SettlementBodyId &&
                actual.ReconnaissanceSystemId == expected.ReconnaissanceSystemId &&
                actual.EmbarkedPopulationSpeciesId == expected.EmbarkedPopulationSpeciesId,
            message + " (identity, route, hold, or pending mission fields differ)");
        RequirePosition(actual.Position, expected.Position, message + " (position differs)");
        RequireNear(actual.FuelRemainingLightYears, expected.FuelRemainingLightYears, message + " (fuel differs)");
        RequireNear(actual.SettlementDaysCompleted, expected.SettlementDaysCompleted, message + " (settlement progress differs)");
        RequireNear(actual.ReconnaissanceDaysCompleted, expected.ReconnaissanceDaysCompleted, message + " (scout progress differs)");
        RequireNear(actual.EmbarkedPopulationMillions, expected.EmbarkedPopulationMillions, message + " (passengers differ)");
        if (actual.LocalSurveyProgress is double actualSurvey && expected.LocalSurveyProgress is double expectedSurvey)
            RequireNear(actualSurvey, expectedSurvey, message + " (science progress differs)");
        else
            Require(actual.LocalSurveyProgress == expected.LocalSurveyProgress, message + " (science progress presence differs)");
        RequireNear(actual.TreasuryCredits, expected.TreasuryCredits, message + " (treasury differs)");
        RequireNear(actual.OperatingFundingFraction, expected.OperatingFundingFraction, message + " (operating funding differs)");
    }

    private static void DeleteSave(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
    }

    private static void RequirePosition(Vector2 actual, Vector2 expected, string message)
    {
        if (Vector2.Distance(actual, expected) > .0001f)
            throw new InvalidOperationException($"{message}: {actual} != {expected}");
    }

    private static void RequireNear(double actual, double expected, string message)
    {
        if (Math.Abs(actual - expected) > .0001)
            throw new InvalidOperationException($"{message}: {actual} != {expected}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed record PersistedFleetSnapshot(int Id, int CivilizationId, FleetRole Role, Vector2 Position,
        int? CurrentSystemId, int? DestinationSystemId, int[] RouteSystemIds, double FuelRemainingLightYears,
        bool HoldRequested, bool IsActive, int? DestinationPlanetaryBodyId, bool PreventAutomaticSettlement,
        int? SettlementBodyId, double SettlementDaysCompleted, int? ReconnaissanceSystemId,
        double ReconnaissanceDaysCompleted, double EmbarkedPopulationMillions, string? EmbarkedPopulationSpeciesId,
        double? LocalSurveyProgress, double TreasuryCredits, double OperatingFundingFraction);
}

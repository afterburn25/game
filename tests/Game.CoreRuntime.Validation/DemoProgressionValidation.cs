using Game.Simulation;
using Game.Simulation.Colonization;
using Game.Simulation.Construction;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;
using Game.Simulation.Research;
using Game.Simulation.Shipbuilding;

namespace Game.CoreRuntime.Validation;

internal static class DemoProgressionValidation
{
    // The same quarter-day upper bound used by the live clock, with no wall-clock sleeps.
    private const double StepDays = 0.25;
    private const double MaximumDays = 6000;

    public static void Run() => RunSeed(20260908);

    public static void RunSeed(long seed)
    {
        var galaxy = new GalaxyGenerator().Generate(seed);
        var player = galaxy.Civilizations.Single(c => c.IsPlayer);
        var playerId = player.Id;
        var technology = galaxy.Technologies.Single(t => t.CivilizationId == playerId);
        var constructionState = galaxy.ConstructionStates.Single(c => c.CivilizationId == playerId);
        var economy = galaxy.Economies.Single(e => e.CivilizationId == playerId);
        var source = galaxy.Colonies.Single(c => c.CivilizationId == playerId);
        var construction = new ConstructionSimulation();
        var research = new ResearchSimulation();
        var shipbuilding = new ShipbuildingSimulation();
        var exploration = new ExplorationSimulation();
        var coordinator = new GalaxySimulationStepCoordinator(
            construction: construction, research: research, shipbuilding: shipbuilding,
            exploration: exploration);
        var researchPriority = new[] { "fusion_propulsion", "deep_space_sensors", "orbital_industry",
            "exotic_field_theory", "warp_field_control", "prototype_warp_drive" };
        var constructionPriority = new[] { "research_network", "industrial_automation",
            "orbital_launch_complex", "orbital_shipyard", "warp_test_facility" };
        var elapsed = 0.0;
        var warpDay = 0.0;
        var shipsQueued = false;
        var reconCompleted = false;
        var surveysCompleted = 0;
        int? scienceTarget = null;
        int? scoutTarget = null;
        int? colonyFleetId = null;
        int? settlementBodyId = null;

        void Note(string message) => Console.WriteLine($"DEMO seed={seed} day={elapsed:0.##}: {message}");
        Require(!galaxy.Fleets.Any(f => f.CivilizationId == playerId), "new pre-warp player already has ships");
        Require(!shipbuilding.StartBuild(galaxy, playerId, "colony_ship").Accepted,
            "new campaign bypassed physical-ship prerequisites");
        Note($"start species={player.SpeciesId}; population={source.PopulationMillions:0.##}M; industry={economy.Industry:0.##}; science={economy.Science:0.##}");

        while (elapsed < MaximumDays)
        {
            if (constructionState.ActiveProjectId is null)
            {
                var available = ConstructionRegistry.GetAvailable(constructionState, technology);
                var next = constructionPriority.FirstOrDefault(id => available.Any(p => p.Id == id));
                if (next is not null)
                {
                    var order = construction.StartProject(galaxy, playerId, next);
                    Require(order.Accepted, order.Message);
                    Note(order.Message);
                }
            }
            if (technology.ActiveResearchId is null)
            {
                var available = TechnologyRegistry.GetAvailable(technology, constructionState);
                var next = researchPriority.FirstOrDefault(id => available.Any(t => t.Id == id));
                if (next is not null)
                {
                    var order = research.StartResearch(galaxy, playerId, next);
                    Require(order.Accepted, order.Message);
                    Note(order.Message);
                }
            }
            if (!shipsQueued && technology.CompletedTechnologyIds.Contains("prototype_warp_drive"))
            {
                warpDay = elapsed;
                foreach (var design in new[] { "warp_scout", "science_vessel", "colony_ship" })
                {
                    var populationBefore = source.PopulationMillions;
                    var order = shipbuilding.StartBuild(galaxy, playerId, design);
                    Require(order.Accepted, order.Message);
                    if (design == "colony_ship")
                        Require(Math.Abs(populationBefore - source.PopulationMillions - 250) < 0.000001,
                            "colony order did not reserve 250M actual source inhabitants");
                    Note(order.Message);
                }
                shipsQueued = true;
            }

            var own = galaxy.Fleets.Where(f => f.IsActive && f.CivilizationId == playerId).ToArray();
            var scout = own.SingleOrDefault(f => f.Role == FleetRole.Scout);
            var science = own.SingleOrDefault(f => f.Role == FleetRole.Science);
            var colony = own.SingleOrDefault(f => f.Role == FleetRole.Colony);
            if (scout is not null && scoutTarget is null)
            {
                var candidate = exploration.GetMissionPlan(galaxy, scout.Id).Candidates.First(c => c.Reach.IsSupported);
                var order = exploration.IssueSurveyOrder(galaxy, scout.Id, candidate.SystemId);
                Require(order.Accepted, order.Message);
                scoutTarget = candidate.SystemId;
                Note($"scout dispatched to system {scoutTarget}");
            }
            if (colony is not null && settlementBodyId is null)
            {
                colonyFleetId = colony.Id;
                var candidate = coordinator.GetColonyOpportunityPlan(galaxy, colony.Id).Candidates.FirstOrDefault(c => c.CanOrder);
                if (candidate is not null)
                {
                    Require(galaxy.Knowledge.IsSystemFullySurveyed(playerId, candidate.SystemId),
                        "settlement opportunity bypassed detailed survey");
                    var order = coordinator.IssueColonyFleetOrder(galaxy, playerId, colony.Id,
                        candidate.SystemId, candidate.PlanetaryBodyId);
                    Require(order.Accepted, order.Message);
                    settlementBodyId = candidate.PlanetaryBodyId;
                    Note($"colony mission accepted system={candidate.SystemId} body={candidate.PlanetaryBodyId}");
                }
            }

            if (science is not null && science.DestinationSystemId is null &&
                (scienceTarget is null || galaxy.Knowledge.IsSystemFullySurveyed(playerId, scienceTarget.Value)) &&
                settlementBodyId is null)
            {
                var candidate = exploration.GetMissionPlan(galaxy, science.Id).Candidates.FirstOrDefault(c => c.Reach.IsSupported);
                Require(candidate is not null, "no supported science mission before finding a settlement opportunity");
                var order = exploration.IssueSurveyOrder(galaxy, science.Id, candidate!.SystemId);
                Require(order.Accepted, order.Message);
                scienceTarget = candidate.SystemId;
                Note($"science vessel dispatched to system {scienceTarget}");
            }

            var step = coordinator.Advance(galaxy, StepDays);
            elapsed += StepDays;
            foreach (var e in step.ResearchEvents.Where(e => e.CivilizationId == playerId)) Note(e.Message);
            foreach (var e in step.ConstructionEvents.Where(e => e.CivilizationId == playerId)) Note(e.Message);
            foreach (var e in step.ShipbuildingEvents.Where(e => e.CivilizationId == playerId)) Note(e.Message);
            foreach (var e in step.ExplorationEvents.Where(e => e.CivilizationId == playerId))
            {
                if (e.Type == ExplorationEventType.SystemReconnoitered) reconCompleted = true;
                if (e.Type == ExplorationEventType.SystemSurveyed) surveysCompleted++;
                if (e.Type is ExplorationEventType.SystemReconnoitered or ExplorationEventType.SystemSurveyed) Note(e.Message);
            }
            Require(double.IsFinite(economy.Industry) && economy.Industry >= -0.000001 &&
                double.IsFinite(economy.Science) && economy.Science >= -0.000001 &&
                double.IsFinite(source.PopulationMillions) && source.PopulationMillions > 0,
                "progression produced invalid industry/science/population state");
            var settlement = galaxy.Colonies.FirstOrDefault(c => c.CivilizationId == playerId && c.Id != source.Id);
            if (settlement is null) continue;

            Require(shipsQueued && warpDay > 0 && reconCompleted && surveysCompleted > 0,
                "settlement did not traverse research, physical scout, reconnaissance and science survey");
            Require(settlement.PlanetaryBodyId == settlementBodyId && settlement.PopulationSpeciesId == source.PopulationSpeciesId,
                "settlement changed commanded body or passenger species");
            Require(Math.Abs(settlement.PopulationMillions - 250) < 0.000001,
                "settlement did not receive the actual 250M embarked passengers");
            Require(!galaxy.Fleets.Single(f => f.Id == colonyFleetId).IsActive,
                "settlement did not consume physical colony ship");
            Note($"PASS founded colony; warp={warpDay:0.##} days; settlement={elapsed:0.##} days; surveys={surveysCompleted}; fastest 4x active time={elapsed / 240:0.00} minutes; normal={elapsed / 60:0.00} minutes; industry={economy.Industry:0.##}; population={source.PopulationMillions:0.##}M");
            return;
        }
        throw new InvalidOperationException($"Demo stalled seed={seed} after {elapsed} days: research={technology.ActiveResearchId ?? "idle"} construction={constructionState.ActiveProjectId ?? "idle"}; shipsQueued={shipsQueued}; scienceTarget={scienceTarget}; surveys={surveysCompleted}; body={settlementBodyId}; industry={economy.Industry}; science={economy.Science}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

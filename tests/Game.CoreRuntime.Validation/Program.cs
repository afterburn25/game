using Game.Simulation;
using Game.Simulation.Construction;
using Game.Simulation.Generation;
using Game.Simulation.Industry;
using Game.Simulation.Research;
using Game.Simulation.Shipbuilding;

namespace Game.CoreRuntime.Validation;

internal static class Program
{
    private static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("balanced fair industry allocation", ValidateBalancedFairAllocation),
            ("weighted industry allocation", ValidateWeightedAllocation),
            ("zero-time simulation step is mutation-free", ValidateZeroTimeMutationFree),
            ("coordinator budgets construction and shipbuilding", ValidateCoordinatorIndustryBudgeting),
        };

        var failures = 0;
        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine($"PASS: {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.Error.WriteLine($"FAIL: {test.Name}: {ex.Message}");
            }
        }

        Console.WriteLine($"Core runtime validation: {tests.Length - failures}/{tests.Length} passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void ValidateBalancedFairAllocation()
    {
        var policy = new WeightedFairIndustryAllocationPolicy();
        var allocation = policy.Allocate(new IndustryAllocationContext(
            CivilizationId: 4,
            AvailableIndustry: 100.0,
            ConstructionDemand: 80.0,
            ShipbuildingDemand: 40.0));

        RequireNear(allocation.ConstructionAllocated, 60.0, "construction did not receive reflowed fair capacity");
        RequireNear(allocation.ShipbuildingAllocated, 40.0, "shipbuilding demand was not fully satisfied");
        RequireNear(allocation.TotalAllocated, 100.0, "available Industry was not fully allocated");
        RequireNear(allocation.ConstructionWeight, 1.0, "balanced construction weight changed");
        RequireNear(allocation.ShipbuildingWeight, 1.0, "balanced shipbuilding weight changed");
    }

    private static void ValidateWeightedAllocation()
    {
        var policy = new WeightedFairIndustryAllocationPolicy(new FixedIndustryPriorityProvider(2.0, 1.0));
        var allocation = policy.Allocate(new IndustryAllocationContext(
            CivilizationId: 7,
            AvailableIndustry: 90.0,
            ConstructionDemand: 200.0,
            ShipbuildingDemand: 200.0));

        RequireNear(allocation.ConstructionAllocated, 60.0, "2:1 construction priority did not receive two thirds of constrained Industry");
        RequireNear(allocation.ShipbuildingAllocated, 30.0, "2:1 shipbuilding priority did not receive one third of constrained Industry");
        RequireNear(allocation.TotalAllocated, 90.0, "weighted allocation lost Industry");
    }

    private static void ValidateZeroTimeMutationFree()
    {
        var galaxy = CreateGalaxy();
        var playerId = galaxy.PlayerCivilizationId;
        var economy = galaxy.Economies.First(state => state.CivilizationId == playerId);
        var construction = galaxy.ConstructionStates.First(state => state.CivilizationId == playerId);
        var shipyard = galaxy.ShipyardStates.First(state => state.CivilizationId == playerId);
        var research = galaxy.Technologies.First(state => state.CivilizationId == playerId);

        construction.ActiveProjectId = ConstructionRegistry.All.First().Id;
        construction.ActiveProjectProgress = 3.0;
        shipyard.ActiveDesignId = ShipDesignRegistry.All.First().Id;
        shipyard.ActiveBuildProgress = 2.0;
        research.ActiveResearchId = TechnologyRegistry.All.First().Id;
        research.ActiveResearchProgress = 4.0;
        economy.Industry = 17.0;
        economy.Science = 19.0;

        var beforeIndustry = economy.Industry;
        var beforeScience = economy.Science;
        var beforeConstruction = construction.ActiveProjectProgress;
        var beforeShipbuilding = shipyard.ActiveBuildProgress;
        var beforeResearch = research.ActiveResearchProgress;
        var beforeFleets = galaxy.Fleets.Count;
        var beforeColonies = galaxy.Colonies.Count;

        var result = new GalaxySimulationStepCoordinator().Advance(galaxy, 0.0);

        Require(result.SimulationDays == 0.0, "zero-time result reported simulation progress");
        Require(result.IndustryAllocations.Count == 0, "zero-time step performed Industry allocation");
        Require(result.ConstructionEvents.Count == 0 && result.ShipbuildingEvents.Count == 0 && result.ResearchEvents.Count == 0,
            "zero-time step emitted production/research events");
        RequireNear(economy.Industry, beforeIndustry, "zero-time step changed Industry");
        RequireNear(economy.Science, beforeScience, "zero-time step changed Science");
        RequireNear(construction.ActiveProjectProgress, beforeConstruction, "zero-time step advanced construction");
        RequireNear(shipyard.ActiveBuildProgress, beforeShipbuilding, "zero-time step advanced shipbuilding");
        RequireNear(research.ActiveResearchProgress, beforeResearch, "zero-time step advanced research");
        Require(galaxy.Fleets.Count == beforeFleets, "zero-time step changed fleet count");
        Require(galaxy.Colonies.Count == beforeColonies, "zero-time step changed colony count");
    }

    private static void ValidateCoordinatorIndustryBudgeting()
    {
        var galaxy = CreateGalaxy();
        var playerId = galaxy.PlayerCivilizationId;
        var economy = galaxy.Economies.First(state => state.CivilizationId == playerId);
        var construction = galaxy.ConstructionStates.First(state => state.CivilizationId == playerId);
        var shipyard = galaxy.ShipyardStates.First(state => state.CivilizationId == playerId);

        var constructionDefinition = ConstructionRegistry.All.OrderByDescending(definition => definition.IndustryCost).First();
        var shipDefinition = ShipDesignRegistry.All.OrderByDescending(definition => definition.IndustryCost).First();
        Require(constructionDefinition.IndustryCost > 5.0, "validation construction project is too small for constrained-allocation test");
        Require(shipDefinition.IndustryCost > 5.0, "validation ship design is too small for constrained-allocation test");

        construction.ActiveProjectId = constructionDefinition.Id;
        construction.ActiveProjectProgress = 0.0;
        shipyard.ActiveDesignId = shipDefinition.Id;
        shipyard.ActiveBuildProgress = 0.0;
        economy.Industry = 1.0;

        var result = new GalaxySimulationStepCoordinator().Advance(galaxy, 0.000001);
        var allocation = result.IndustryAllocations.Single(item => item.CivilizationId == playerId);

        Require(allocation.ConstructionDemand > allocation.ConstructionAllocated, "construction was not resource-constrained in validation step");
        Require(allocation.ShipbuildingDemand > allocation.ShipbuildingAllocated, "shipbuilding was not resource-constrained in validation step");
        RequireNear(allocation.ConstructionAllocated, allocation.ShipbuildingAllocated, "balanced simultaneous claims were not allocated equally");
        RequireNear(allocation.TotalAllocated, allocation.AvailableIndustry, "coordinator did not allocate the full constrained Industry stockpile");
        RequireNear(construction.ActiveProjectProgress, allocation.ConstructionAllocated, "construction spent a different amount than its Core budget");
        RequireNear(shipyard.ActiveBuildProgress, allocation.ShipbuildingAllocated, "shipbuilding spent a different amount than its Core budget");
        Require(Math.Abs(economy.Industry) < 0.000001, $"unaccounted Industry remained after fully constrained allocation: {economy.Industry}");
    }

    private static Game.Simulation.Models.GalaxyState CreateGalaxy() =>
        new GalaxyGenerator().Generate(
            0x434F_5245_5255_4EL,
            new GalaxyGenerationSettings
            {
                SystemCount = 40,
                PreWarpCivilizationCount = 4,
                AncientCivilizationCount = 1,
                Radius = 460.0f,
            });

    private static void RequireNear(double actual, double expected, string message, double tolerance = 0.000001)
    {
        if (Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"{message}: expected {expected:0.######}, got {actual:0.######}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

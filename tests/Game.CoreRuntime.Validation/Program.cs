using Game.Campaign;
using Game.Simulation;
using Game.Simulation.Combat;
using Game.Simulation.Construction;
using Game.Simulation.Generation;
using Game.Simulation.Industry;
using Game.Simulation.Models;
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
            ("coordinator executes authoritative combat", ValidateCoordinatorCombat),
            ("strategic AI drives bounded Core industry priorities", StrategicAiRuntimeValidation.Run),
            ("campaign session lifecycle and recovery", ValidateCampaignSessionLifecycle),
            ("new campaign reaches a real surveyed settlement without injected resources", DemoProgressionValidation.Run),
            ("optional demo clock reaches settlement within five active minutes", DemoProgressionValidation.RunDemo),
            ("demo configuration clock and separate-save continuity", PlayableDemoValidation.Run),
            ("human Earth origin and canonical Sol save continuity", SolStartingWorldValidation.Run),
            ("surface free placement authority and rejection", SurfaceConstructionValidation.ValidateFreePlacementAndAuthority),
            ("surface rate budget and pause", SurfaceConstructionValidation.ValidateRateBudgetAndPause),
            ("surface and regular project share industry", SurfaceConstructionValidation.ValidateSharedConstructionBudget),
            ("surface construction is independent of frame partition", SurfaceConstructionValidation.ValidateFramePartitionIndependence),
            ("surface power feeds authoritative economy", SurfaceConstructionValidation.ValidatePowerAndEconomy),
            ("surface positions and progress survive save resume", SurfaceConstructionValidation.ValidateSaveContinuity),
            ("invalid surface saves fail closed", SurfaceConstructionValidation.ValidateInvalidSurfaceSaves),
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
        Require(result.CombatEvents.Count == 0, "zero-time step emitted combat events");
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

    private static void ValidateCoordinatorCombat()
    {
        var galaxy = CreateGalaxy();
        galaxy.Fleets.Clear();

        var civilizations = galaxy.Civilizations.Where(civilization => !civilization.IsSeededAncient).Take(2).ToArray();
        Require(civilizations.Length == 2, "validation galaxy did not contain two ordinary civilizations");
        var system = galaxy.Systems[0];
        var first = CreatePatrolFleet(9001, civilizations[0].Id, "Core Combat One", system.Id, system.Position);
        var second = CreatePatrolFleet(9002, civilizations[1].Id, "Core Combat Two", system.Id, system.Position);
        galaxy.Fleets.Add(first);
        galaxy.Fleets.Add(second);

        var combat = new CombatSimulation(new DelegateCombatHostilityView((firstId, secondId) =>
            (firstId == first.CivilizationId && secondId == second.CivilizationId) ||
            (firstId == second.CivilizationId && secondId == first.CivilizationId)));
        var coordinator = new GalaxySimulationStepCoordinator(combat: combat);
        var order = coordinator.IssueMilitaryOrder(
            galaxy,
            first.CivilizationId,
            first.Id,
            new MilitaryOrder(MilitaryOrderType.Attack, second.Id));
        Require(order.Accepted, $"Core coordinator rejected a valid hostile combat order: {order.Message}");

        var targetBefore = CombatProfileRegistry.EnsureState(second).Shields;
        var result = coordinator.Advance(galaxy, 0.75);
        var targetAfter = CombatProfileRegistry.EnsureState(second).Shields;

        Require(result.CombatEvents.Any(combatEvent => combatEvent.Type == CombatEventType.EngagementStarted),
            "Core coordinator did not publish engagement start");
        Require(result.CombatEvents.Any(combatEvent => combatEvent.Type == CombatEventType.DamageApplied && combatEvent.TargetFleetId == second.Id),
            "Core coordinator did not publish authoritative damage");
        Require(targetAfter < targetBefore, "Core combat step did not mutate authoritative target defenses");
    }

    private static void ValidateCampaignSessionLifecycle()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"stellar-continuum-core-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var settings = new GalaxyGenerationSettings
            {
                SystemCount = 32,
                PreWarpCivilizationCount = 4,
                AncientCivilizationCount = 1,
                Radius = 360.0f,
            };
            var service = new CampaignSessionService();
            const long initialSeed = 0x5345_5353_494F_4EL;
            var fresh = service.CreateNew(initialSeed, settings);

            Require(fresh.Source == CampaignBootstrapSource.NewCampaign, "new session reported the wrong bootstrap source");
            Require(fresh.Seed == initialSeed, "new session changed the requested seed");
            RequireNear(fresh.SimulationDays, 0.0, "new session did not start at day zero");
            Require(!fresh.WasLoaded && !fresh.RecoveredFromInvalidSave, "new session reported load/recovery state");

            var savePath = Path.Combine(tempDirectory, "campaign.json");
            const double savedDays = 123.25;
            service.Save(savePath, fresh.Galaxy, savedDays);
            var loaded = service.LoadOrCreate(savePath, fallbackSeed: 99L, fallbackSettings: settings);

            Require(loaded.Source == CampaignBootstrapSource.LoadedSave && loaded.WasLoaded, "valid save did not reload as a loaded campaign");
            Require(loaded.Seed == initialSeed, "loaded campaign did not preserve its seed");
            RequireNear(loaded.SimulationDays, savedDays, "loaded campaign did not restore simulation time");
            Require(loaded.SavedAtUtc is not null, "loaded campaign omitted save timestamp");
            Require(string.IsNullOrWhiteSpace(loaded.LoadFailure), "successful load reported a load failure");

            const long missingSeed = 0x4D49_5353_494E_47L;
            var missing = service.LoadOrCreate(Path.Combine(tempDirectory, "missing.json"), missingSeed, settings);
            Require(missing.Source == CampaignBootstrapSource.NewCampaign, "missing save did not start a new campaign");
            Require(missing.Seed == missingSeed, "missing-save fallback changed the requested seed");

            var corruptPath = Path.Combine(tempDirectory, "corrupt.json");
            File.WriteAllText(corruptPath, "{ definitely-not-valid-json");
            const long recoverySeed = 0x5245_434F_5645_52L;
            var recovered = service.LoadOrCreate(corruptPath, recoverySeed, settings);

            Require(recovered.Source == CampaignBootstrapSource.RecoveredFromInvalidSave, "corrupt save did not enter explicit recovery state");
            Require(recovered.RecoveredFromInvalidSave, "corrupt-save recovery flag was false");
            Require(recovered.Seed == recoverySeed, "recovery campaign changed the requested fallback seed");
            RequireNear(recovered.SimulationDays, 0.0, "recovery campaign did not restart at day zero");
            Require(!string.IsNullOrWhiteSpace(recovered.LoadFailure), "corrupt-save recovery did not preserve the load failure for diagnostics");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static FleetState CreatePatrolFleet(int id, int civilizationId, string name, int systemId, System.Numerics.Vector2 position) => new()
    {
        Id = id,
        CivilizationId = civilizationId,
        Name = name,
        Role = FleetRole.Military,
        Position = position,
        CurrentSystemId = systemId,
        StrategicSpeed = 21.0,
        SensorRange = 125.0f,
        IsActive = true,
        Combat = CombatProfileRegistry.CreateInitialState(CombatProfileIds.PatrolCorvetteMk1, FleetRole.Military),
    };

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

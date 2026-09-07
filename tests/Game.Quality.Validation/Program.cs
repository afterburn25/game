using Game.Diagnostics;
using Game.Simulation.AI;
using Game.Simulation.Economy;
using Game.Simulation.Generation;

namespace Game.Quality.Validation;

internal static class Program
{
    private static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("knowledge freshness decays", ValidateKnowledgeFreshness),
            ("uncertain intelligence is conservative", ValidateUncertainIntelligenceConservatism),
            ("economy remains finite in long run", ValidateEconomyLongRunFinite),
            ("logistics summary is finite and read-only", ValidateLogisticsSummary),
            ("strategic planner respects scheduled cache", ValidateStrategicPlannerScheduling),
            ("diagnostics buffer stays bounded", ValidateDiagnosticsBufferBounded),
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

        Console.WriteLine($"Quality validation: {tests.Length - failures}/{tests.Length} passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void ValidateKnowledgeFreshness()
    {
        var known = CreateKnownCivilization(lastObservationTick: 100, confidence: 0.9);

        Require(Math.Abs(known.Freshness(100, 365) - 1.0) < 0.000001, "fresh observation was not fully fresh");
        var halfway = known.Freshness(282, 365);
        Require(halfway > 0.49 && halfway < 0.51, $"half-aged observation freshness was {halfway:0.000}");
        Require(known.Freshness(465, 365) == 0.0, "stale observation did not decay to zero freshness");
    }

    private static void ValidateUncertainIntelligenceConservatism()
    {
        var evaluator = new StrategicDecisionEvaluator();
        var traits = CivilizationTraits.Balanced;
        const double ownKnownMilitaryStrength = 130.0;
        const long nowTick = 365;

        var fresh = evaluator.EvaluateWar(
            traits,
            ownKnownMilitaryStrength,
            CreateKnownCivilization(lastObservationTick: nowTick, confidence: 1.0),
            nowTick);

        var stale = evaluator.EvaluateWar(
            traits,
            ownKnownMilitaryStrength,
            CreateKnownCivilization(lastObservationTick: 0, confidence: 1.0),
            nowTick);

        Require(stale.IntelligenceConfidence < fresh.IntelligenceConfidence, "stale intelligence did not reduce confidence");
        Require(stale.PerceivedStrengthRatio < fresh.PerceivedStrengthRatio, "uncertainty made the target appear safer instead of more dangerous");
        Require(stale.Score <= fresh.Score, "uncertainty increased willingness to start a war");
    }

    private static void ValidateEconomyLongRunFinite()
    {
        var galaxy = CreateQualityGalaxy();
        var economySimulation = new EconomySimulation();
        for (var day = 0; day < 3650; day++)
            economySimulation.Advance(galaxy, 1.0);

        foreach (var economy in galaxy.Economies)
        {
            Require(double.IsFinite(economy.Credits) && economy.Credits >= 0.0, "credits became invalid during long-run simulation");
            Require(double.IsFinite(economy.Industry) && economy.Industry >= 0.0, "industry became invalid during long-run simulation");
            Require(double.IsFinite(economy.Science) && economy.Science >= 0.0, "science became invalid during long-run simulation");
            Require(double.IsFinite(economy.LastCreditsPerSecond), "credit throughput became non-finite");
            Require(double.IsFinite(economy.LastIndustryPerSecond), "industry throughput became non-finite");
            Require(double.IsFinite(economy.LastSciencePerSecond), "science throughput became non-finite");
        }

        foreach (var colony in galaxy.Colonies)
            Require(double.IsFinite(colony.PopulationMillions) && colony.PopulationMillions > 0.0, "population became invalid during long-run simulation");
    }

    private static void ValidateLogisticsSummary()
    {
        var galaxy = CreateQualityGalaxy();
        new EconomySimulation().Advance(galaxy, 1.0);

        var civilizationId = galaxy.PlayerCivilizationId;
        var economy = galaxy.Economies.First(state => state.CivilizationId == civilizationId);
        var colonies = galaxy.Colonies.Where(colony => colony.CivilizationId == civilizationId).ToArray();
        var creditsBefore = economy.Credits;
        var industryBefore = economy.Industry;
        var scienceBefore = economy.Science;
        var populationsBefore = colonies.Select(colony => colony.PopulationMillions).ToArray();

        IEconomyLogisticsView view = new PrototypeEconomyLogisticsView();
        var first = view.GetSnapshot(galaxy, civilizationId);
        var second = view.GetSnapshot(galaxy, civilizationId);

        Require(
            first.CivilizationId == second.CivilizationId
            && first.TotalSupportDemandPerDay == second.TotalSupportDemandPerDay
            && first.TotalLocalSupportCapacityPerDay == second.TotalLocalSupportCapacityPerDay
            && first.ImportRequirementPerDay == second.ImportRequirementPerDay
            && first.CargoHandlingCapacityPerDay == second.CargoHandlingCapacityPerDay
            && first.EffectiveCoverageRatio == second.EffectiveCoverageRatio
            && first.Condition == second.Condition
            && first.Colonies.SequenceEqual(second.Colonies),
            "identical logistics inputs produced different strategic snapshots");
        Require(first.Colonies.Count == colonies.Length, "logistics summary did not include every owned colony exactly once");
        Require(double.IsFinite(first.TotalSupportDemandPerDay) && first.TotalSupportDemandPerDay >= 0.0, "support demand was invalid");
        Require(double.IsFinite(first.TotalLocalSupportCapacityPerDay) && first.TotalLocalSupportCapacityPerDay >= 0.0, "local support capacity was invalid");
        Require(double.IsFinite(first.ImportRequirementPerDay) && first.ImportRequirementPerDay >= 0.0, "import requirement was invalid");
        Require(double.IsFinite(first.CargoHandlingCapacityPerDay) && first.CargoHandlingCapacityPerDay >= 0.0, "cargo handling was invalid");
        Require(double.IsFinite(first.EffectiveCoverageRatio) && first.EffectiveCoverageRatio >= 0.0 && first.EffectiveCoverageRatio <= 2.0, "effective coverage was outside its bounded range");
        Require(economy.Credits == creditsBefore && economy.Industry == industryBefore && economy.Science == scienceBefore, "read-only logistics view mutated economy resources");

        for (var i = 0; i < colonies.Length; i++)
            Require(colonies[i].PopulationMillions == populationsBefore[i], "read-only logistics view mutated colony population");
    }

    private static void ValidateStrategicPlannerScheduling()
    {
        var planner = new CivilizationStrategicPlanner(reviewIntervalTicks: 30);
        var knowledge = new KnowledgeSnapshot
        {
            ObservedAtTick = 100,
            Civilizations = new Dictionary<int, KnownCivilization>(),
        };

        var shortage = new CivilizationOwnState(
            MilitaryStrength: 100.0,
            SupplyCoverageRatio: 0.40,
            IndustryReserve: 100.0,
            ResearchCapacity: 1.0,
            HasAvailableResearch: true,
            HasUnexploredReachableSystems: true,
            HasKnownColonizationOpportunity: true,
            CanBuildInterstellarShips: true,
            HasFleetCapacityShortfall: true);

        var healthy = shortage with
        {
            SupplyCoverageRatio = 1.10,
            IndustryReserve = 900.0,
            HasKnownColonizationOpportunity = false,
            HasFleetCapacityShortfall = false,
        };

        var first = planner.GetPlan(1, CivilizationTraits.Balanced, shortage, knowledge, nowTick: 100);
        Require(first.PrimaryPriority?.Type == StrategicPriorityType.StabilizeSupply, "severe own supply shortage was not the primary strategic priority");
        Require(first.ReviewAfterTick == 130, "strategic review interval was not applied deterministically");
        Require(first.Priorities.All(priority => priority.Type != StrategicPriorityType.Defend), "AI invented a foreign threat with no known civilizations");

        var cached = planner.GetPlan(1, CivilizationTraits.Balanced, healthy, knowledge, nowTick: 110);
        Require(ReferenceEquals(first, cached), "strategic planner recomputed before its scheduled review tick");

        var reviewed = planner.GetPlan(1, CivilizationTraits.Balanced, healthy, knowledge, nowTick: 130);
        Require(!ReferenceEquals(first, reviewed), "strategic planner failed to recompute at its review boundary");
        Require(reviewed.PrimaryPriority?.Type != StrategicPriorityType.StabilizeSupply, "resolved supply shortage remained the primary priority after scheduled review");
    }

    private static void ValidateDiagnosticsBufferBounded()
    {
        const int capacity = 128;
        var diagnostics = new DiagnosticsBuffer(capacity);
        for (var i = 0; i < 300; i++)
            diagnostics.Add("quality-test", $"event-{i}");

        var snapshot = diagnostics.Snapshot();
        Require(snapshot.Count == capacity, $"diagnostics retained {snapshot.Count} events instead of {capacity}");
        Require(snapshot[0].Message == "event-172", "diagnostics did not evict the oldest event first");
        Require(snapshot[^1].Message == "event-299", "diagnostics did not retain the newest event");
    }

    private static Game.Simulation.Models.GalaxyState CreateQualityGalaxy() =>
        new GalaxyGenerator().Generate(
            0x5155_414C_4954_59L,
            new GalaxyGenerationSettings
            {
                SystemCount = 48,
                PreWarpCivilizationCount = 5,
                AncientCivilizationCount = 1,
                Radius = 500.0f,
            });

    private static KnownCivilization CreateKnownCivilization(long lastObservationTick, double confidence) =>
        new(
            CivilizationId: 2,
            Trust: 0.0,
            EstimatedMilitaryLow: 100.0,
            EstimatedMilitaryHigh: 100.0,
            EstimateConfidence: confidence,
            LastMilitaryObservationTick: lastObservationTick,
            HasSharedBorder: true,
            KnownTradeDependence: 0.0,
            KnownWarExhaustion: 0.0,
            KnownToBeAtWar: false,
            HasDefenseTreatyWithObserver: false);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

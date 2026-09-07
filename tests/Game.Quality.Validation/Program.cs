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
        var galaxy = new GalaxyGenerator().Generate(
            0x5155_414C_4954_59L,
            new GalaxyGenerationSettings
            {
                SystemCount = 48,
                PreWarpCivilizationCount = 5,
                AncientCivilizationCount = 1,
                Radius = 500.0f,
            });

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
        {
            Require(double.IsFinite(colony.PopulationMillions) && colony.PopulationMillions > 0.0, "population became invalid during long-run simulation");
        }
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

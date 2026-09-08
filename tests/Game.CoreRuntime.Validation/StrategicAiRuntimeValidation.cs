using Game.Simulation;
using Game.Simulation.AI;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.CoreRuntime.Validation;

internal static class StrategicAiRuntimeValidation
{
    public static void Run()
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x5354_5241_5445_47L,
            new GalaxyGenerationSettings
            {
                SystemCount = 40,
                PreWarpCivilizationCount = 4,
                AncientCivilizationCount = 1,
                Radius = 460.0f,
            });

        var eligible = galaxy.Civilizations
            .Where(civilization => !civilization.IsPlayer && !civilization.IsSeededAncient)
            .OrderBy(civilization => civilization.Id)
            .ToArray();
        Require(eligible.Length > 0, "validation galaxy did not contain an ordinary non-player civilization");

        var runtime = new CivilizationStrategicRuntimeCoordinator();
        var firstReviews = runtime.Advance(galaxy, 1.0);
        Require(firstReviews.Count == eligible.Length, "initial strategic runtime pass did not review each ordinary AI civilization exactly once");
        Require(runtime.PublishedIntentCount == eligible.Length, "strategic runtime did not publish one intent per reviewed AI civilization");
        Require(firstReviews.All(review => review.Plan.Priorities.All(priority => priority.Type != StrategicPriorityType.Defend)),
            "own-state-only runtime invented a foreign defensive threat without intelligence input");

        var playerWeights = runtime.GetIndustryWeights(galaxy.PlayerCivilizationId);
        RequireNear(playerWeights.ConstructionWeight, 1.0, "player construction priority was changed by Civilization AI");
        RequireNear(playerWeights.ShipbuildingWeight, 1.0, "player shipbuilding priority was changed by Civilization AI");

        var aiCivilizationId = eligible[0].Id;
        var aiWeights = runtime.GetIndustryWeights(aiCivilizationId);
        Require(double.IsFinite(aiWeights.ConstructionWeight) && aiWeights.ConstructionWeight > 0.0,
            "AI construction weight was not finite and positive");
        Require(double.IsFinite(aiWeights.ShipbuildingWeight) && aiWeights.ShipbuildingWeight > 0.0,
            "AI shipbuilding weight was not finite and positive");
        Require(Math.Abs(aiWeights.ConstructionWeight - 1.0) > 0.000001 || Math.Abs(aiWeights.ShipbuildingWeight - 1.0) > 0.000001,
            "published strategic intent did not affect either Industry priority weight");

        var beforeBoundary = runtime.Advance(galaxy, 1.0);
        Require(beforeBoundary.Count == 0, "strategic runtime recomputed before its scheduled review boundary");

        var afterBoundary = runtime.Advance(galaxy, 30.0);
        Require(afterBoundary.Count == eligible.Length, "strategic runtime failed to recompute at the scheduled review boundary");

        // Use a fresh deterministic campaign so the Core step proves the provider is consumed by
        // the authoritative allocator rather than only by the standalone runtime coordinator.
        var integratedGalaxy = new GalaxyGenerator().Generate(
            0x5354_5241_5445_47L,
            new GalaxyGenerationSettings
            {
                SystemCount = 40,
                PreWarpCivilizationCount = 4,
                AncientCivilizationCount = 1,
                Radius = 460.0f,
            });
        var integratedRuntime = new CivilizationStrategicRuntimeCoordinator();
        var coordinator = new GalaxySimulationStepCoordinator(strategicAi: integratedRuntime);
        var result = coordinator.Advance(integratedGalaxy, 0.000001);

        var integratedAi = integratedGalaxy.Civilizations
            .Where(civilization => !civilization.IsPlayer && !civilization.IsSeededAncient)
            .OrderBy(civilization => civilization.Id)
            .First();
        var expectedWeights = integratedRuntime.GetIndustryWeights(integratedAi.Id);
        var allocation = result.IndustryAllocations.Single(item => item.CivilizationId == integratedAi.Id);
        RequireNear(allocation.ConstructionWeight, expectedWeights.ConstructionWeight,
            "Core Industry allocator did not consume Civilization AI construction priority");
        RequireNear(allocation.ShipbuildingWeight, expectedWeights.ShipbuildingWeight,
            "Core Industry allocator did not consume Civilization AI shipbuilding priority");
    }

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

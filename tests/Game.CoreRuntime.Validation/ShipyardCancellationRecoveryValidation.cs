using Game.Simulation.Generation;
using Game.Simulation.Shipbuilding;

namespace Game.CoreRuntime.Validation;

internal static class ShipyardCancellationRecoveryValidation
{
    [Game.Validation.RegressionCheck]
    internal static void Run()
    {
        var galaxy = new GalaxyGenerator().Generate(44091, new GalaxyGenerationSettings { SystemCount = 16, PreWarpCivilizationCount = 3, AncientCivilizationCount = 0, Radius = 200 });
        var player = galaxy.PlayerCivilizationId;
        var economy = galaxy.Economies.Single(e => e.CivilizationId == player); economy.Credits = 1000;
        var shipyard = galaxy.ShipyardStates.Single(s => s.CivilizationId == player);
        var design = ShipDesignRegistry.All.First();
        shipyard.ActiveDesignId = design.Id; shipyard.ActiveOrderId = "active"; shipyard.ActiveBuildProgress = design.IndustryCost / 2; shipyard.ActiveAuthorizationCredits = design.CreditCost;
        var simulation = new ShipbuildingSimulation(); var beforeIndustry = economy.Industry;
        var cancelled = simulation.CancelBuild(galaxy, player, "active");
        Require(cancelled.Accepted && Math.Abs(cancelled.RefundedCredits - design.CreditCost / 2) < .001, "active cancellation did not refund remaining paid quote");
        Require(Math.Abs(economy.Industry - beforeIndustry) < .001, "cancellation refunded consumed materials");
        shipyard.QueuedBuilds.Add(new ShipBuildOrderState { OrderId = "legacy-pop", DesignId = design.Id, AuthorizationCredits = design.CreditCost, ReservedPopulationMillions = 10, ReservedPopulationSpeciesId = "terran_baseline", ReservedPopulationSourceColonyId = null });
        var credits = economy.Credits;
        Require(!simulation.CancelBuild(galaxy, player, "legacy-pop").Accepted && Math.Abs(economy.Credits - credits) < .001 && shipyard.QueuedBuilds.Count == 1, "unprovable legacy population reservation mutated on cancellation");
        Console.WriteLine("PASS: shipyard cancellation preserves paid work and reserved people");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

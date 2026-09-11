using Game.Persistence;
using Game.Simulation.Construction;
using Game.Simulation.Generation;

namespace Game.CoreRuntime.Validation;

internal static class ConstructionQueueRecoveryValidation
{
    [Game.Validation.RegressionCheck]
    internal static void Run()
    {
        QueueDebitsOnceAndPromotesInOrder();
        CancellationPreservesMaterialAccounting();
        AutomaticOrdersPayAndSkipUnaffordableProjects();
        QueueStatePersistsAndInvalidPayloadFailsClosed();
        Console.WriteLine("PASS: construction queue authorization, recovery and AI fairness");
    }

    private static void QueueDebitsOnceAndPromotesInOrder()
    {
        var galaxy = CreateGalaxy(); var player = galaxy.PlayerCivilizationId;
        var economy = galaxy.Economies.Single(e => e.CivilizationId == player); economy.Credits = 1000;
        var simulation = new ConstructionSimulation();
        Require(simulation.StartProject(galaxy, player, "research_network").Accepted, "opening project rejected");
        var afterActive = economy.Credits;
        Require(simulation.QueueProject(galaxy, player, "industrial_automation").Accepted, "queue project rejected");
        RequireNear(economy.Credits, afterActive - ConstructionRegistry.Get("industrial_automation").CreditCost, "queued project was not debited exactly once");
        var state = galaxy.ConstructionStates.Single(s => s.CivilizationId == player);
        economy.Industry = 10_000;
        simulation.AdvanceForCivilization(galaxy, player, 10_000, 100);
        Require(state.CompletedProjectIds.Contains("research_network") && state.ActiveProjectId == "industrial_automation", "queue did not promote FIFO after completion");
        RequireNear(economy.Credits, afterActive - ConstructionRegistry.Get("industrial_automation").CreditCost, "promotion charged authorization a second time");
    }

    private static void CancellationPreservesMaterialAccounting()
    {
        var galaxy = CreateGalaxy(); var player = galaxy.PlayerCivilizationId;
        var economy = galaxy.Economies.Single(e => e.CivilizationId == player); economy.Credits = 1000;
        var simulation = new ConstructionSimulation();
        Require(simulation.StartProject(galaxy, player, "research_network").Accepted, "opening project rejected");
        var state = galaxy.ConstructionStates.Single(s => s.CivilizationId == player);
        state.ActiveProjectProgress = 350;
        var before = economy.Credits;
        var cancelled = simulation.CancelProject(galaxy, player, "research_network");
        Require(cancelled.Accepted, "active cancellation rejected");
        RequireNear(cancelled.RefundedCredits, 75, "partial active cancellation refunded the wrong authorization fraction");
        RequireNear(economy.Credits, before + 75, "active cancellation credit accounting drifted");
        Require(state.ActiveProjectId is null && state.ActiveProjectProgress == 0, "cancelled project remained active");
        Require(simulation.StartProject(galaxy, player, "research_network").Accepted, "restart rejected");
        state.ActiveProjectAuthorizationCredits = 0; state.ActiveProjectProgress = 350;
        RequireNear(simulation.CancelProject(galaxy, player, "research_network").RefundedCredits, 0, "legacy active project minted a refund");
    }

    private static void AutomaticOrdersPayAndSkipUnaffordableProjects()
    {
        var galaxy = CreateGalaxy();
        var civilization = galaxy.Civilizations.First(c => !c.IsPlayer && !c.IsSeededAncient);
        var economy = galaxy.Economies.Single(e => e.CivilizationId == civilization.Id);
        var state = galaxy.ConstructionStates.Single(s => s.CivilizationId == civilization.Id);
        economy.Credits = 0;
        var simulation = new ConstructionSimulation(); simulation.EnsureAutomaticOrders(galaxy);
        Require(state.ActiveProjectId is null, "AI received a free construction project without funds");
        economy.Credits = 200;
        simulation.EnsureAutomaticOrders(galaxy);
        Require(state.ActiveProjectId is not null && economy.Credits < 200, "AI did not authorize an affordable project through the paid path");
        RequireNear(state.ActiveProjectAuthorizationCredits, ConstructionRegistry.Get(state.ActiveProjectId!).CreditCost, "AI active order did not retain its exact paid quote");
    }

    private static void QueueStatePersistsAndInvalidPayloadFailsClosed()
    {
        var galaxy = CreateGalaxy(); var player = galaxy.PlayerCivilizationId;
        var economy = galaxy.Economies.Single(e => e.CivilizationId == player); economy.Credits = 1000;
        var simulation = new ConstructionSimulation();
        Require(simulation.StartProject(galaxy, player, "research_network").Accepted && simulation.QueueProject(galaxy, player, "industrial_automation").Accepted, "queue setup rejected");
        var path = Path.Combine(Path.GetTempPath(), $"stellar-construction-{Guid.NewGuid():N}.json");
        try
        {
            var saves = new CampaignSaveService(); saves.Save(path, galaxy, 4);
            var restored = saves.Load(path).Galaxy.ConstructionStates.Single(s => s.CivilizationId == player);
            Require(restored.QueuedProjects.Single().ProjectId == "industrial_automation" && restored.ActiveProjectAuthorizationCredits == 150, "queue authorization did not survive save/load");
            var payload = File.ReadAllText(path).Replace("industrial_automation", "unknown_project", StringComparison.Ordinal);
            File.WriteAllText(path, payload);
            try { saves.Load(path); throw new InvalidOperationException("invalid queued project loaded"); }
            catch (InvalidDataException) { }
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static Game.Simulation.Models.GalaxyState CreateGalaxy() => new GalaxyGenerator().Generate(77123,
        new GalaxyGenerationSettings { SystemCount = 18, PreWarpCivilizationCount = 4, AncientCivilizationCount = 0, Radius = 240 });
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void RequireNear(double actual, double expected, string message) { if (Math.Abs(actual - expected) > .0001) throw new InvalidOperationException($"{message}: {actual} != {expected}"); }
}

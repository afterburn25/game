using Game.Simulation.Generation;
using Game.Simulation.Research.Adaptive;

namespace Game.CoreRuntime.Validation;

internal static class AdaptiveResearchFundingValidation
{
    public static void Run()
    {
        var root = AdaptiveResearchDataLocator.FindDataRoot();
        var runtime = AdaptiveResearchStrategicRuntime.LoadFromDirectory(root);
        var galaxy = new GalaxyGenerator().Generate(62941, new GalaxyGenerationSettings());
        var campaign = new AdaptiveResearchCampaignFactory(runtime).Create(galaxy);
        var playerId = galaxy.PlayerCivilizationId;
        var state = campaign.GetCivilization(playerId);
        var economy = galaxy.Economies.Single(value => value.CivilizationId == playerId);
        var node = runtime.Authority.Catalog.GetNode("fusion_power");
        var labs = Math.Min(node.ProjectRequirements.RecommendedLabs, state.FreeEffectiveLabs);
        var quote = AdaptiveResearchFundingPolicy.Quote(node, labs, runtime.Authority.Catalog);

        Require(quote.OperatingCreditsPerDay > 0.0 && quote.EstimatedTotalOperatingCredits > 0.0,
            "research funding policy produced a free directed program");
        Require(runtime.Authority.StartDirectedResearch(state, node.Id, labs).Accepted,
            "funding validation could not start its visible research program");

        economy.Credits = quote.OperatingCreditsPerDay * 20.0;
        var startingCredits = economy.Credits;
        var startingRp = state.ActiveProjects[node.Id].TotalResearchPoints;
        _ = new AdaptiveResearchCampaignSimulation().Advance(
            galaxy, campaign, elapsedDays: 10.0, currentSimulationDay: 10.0);
        var fundedRp = state.ActiveProjects[node.Id].TotalResearchPoints;

        Near(economy.Credits, startingCredits - quote.OperatingCreditsPerDay * 10.0,
            "fully funded research did not deduct its exact operating expense");
        Near(economy.LastResearchSpendingPerDay, quote.OperatingCreditsPerDay,
            "economy did not expose the funded research burn rate");
        Near(economy.LastResearchFundingFraction, 1.0,
            "fully funded research did not report complete funding");
        Require(fundedRp > startingRp, "fully funded research did not advance RP");

        economy.Credits = 0.0;
        _ = new AdaptiveResearchCampaignSimulation().Advance(
            galaxy, campaign, elapsedDays: 10.0, currentSimulationDay: 20.0);
        Near(state.ActiveProjects[node.Id].TotalResearchPoints, fundedRp,
            "unfunded research advanced RP");
        Near(economy.LastResearchSpendingPerDay, 0.0,
            "unfunded research reported money it did not spend");
        Near(economy.LastResearchFundingFraction, 0.0,
            "unfunded research did not report its funding stall");

        economy.Credits = quote.OperatingCreditsPerDay * 5.0;
        _ = new AdaptiveResearchCampaignSimulation().Advance(
            galaxy, campaign, elapsedDays: 10.0, currentSimulationDay: 30.0);
        var partiallyFundedRp = state.ActiveProjects[node.Id].TotalResearchPoints;
        Require(partiallyFundedRp > fundedRp,
            "partially funded research made no progress");
        Require(partiallyFundedRp - fundedRp < fundedRp - startingRp,
            "partially funded research advanced as quickly as a fully funded interval");
        Near(economy.Credits, 0.0, "partial research funding overspent the treasury");
        Near(economy.LastResearchFundingFraction, 0.5,
            "partial research funding did not report the funded fraction");

        var foundation = AdaptiveResearchFundingPolicy.Quote(
            runtime.Authority.Catalog.GetNode("fusion_power"), 4, runtime.Authority.Catalog);
        var developing = AdaptiveResearchFundingPolicy.Quote(
            runtime.Authority.Catalog.GetNode("high_temp_superconductors"), 4, runtime.Authority.Catalog);
        var advanced = AdaptiveResearchFundingPolicy.Quote(
            runtime.Authority.Catalog.GetNode("aneutronic_fusion"), 4, runtime.Authority.Catalog);
        var frontier = AdaptiveResearchFundingPolicy.Quote(
            runtime.Authority.Catalog.GetNode("prototype_warp_drive"), 4, runtime.Authority.Catalog);
        Require(foundation.OperatingCreditsPerDay < developing.OperatingCreditsPerDay &&
                developing.OperatingCreditsPerDay < advanced.OperatingCreditsPerDay &&
                advanced.OperatingCreditsPerDay < frontier.OperatingCreditsPerDay,
            "research operating costs do not rise across complexity bands at equal lab allocation");
        Require(foundation.EstimatedTotalOperatingCredits < developing.EstimatedTotalOperatingCredits &&
                developing.EstimatedTotalOperatingCredits < advanced.EstimatedTotalOperatingCredits &&
                advanced.EstimatedTotalOperatingCredits < frontier.EstimatedTotalOperatingCredits,
            "estimated total research costs do not rise across representative complexity bands");
    }

    private static void Near(double actual, double expected, string message)
    {
        if (Math.Abs(actual - expected) > 0.000001)
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

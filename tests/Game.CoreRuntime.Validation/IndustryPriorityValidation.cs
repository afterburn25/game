using System.Text.Json.Nodes;
using Game.Persistence;
using Game.Simulation.Generation;
using Game.Simulation.Industry;
using Game.Simulation.Models;

namespace Game.CoreRuntime.Validation;

internal static class IndustryPriorityValidation
{
    public static void Run()
    {
        var galaxy = new GalaxyGenerator().Generate(20260911, new GalaxyGenerationSettings { SystemCount = 16, PreWarpCivilizationCount = 1, AncientCivilizationCount = 0, Radius = 200 });
        var player = galaxy.PlayerCivilizationId;
        var economy = galaxy.Economies.Single(e => e.CivilizationId == player);
        Require(economy.IndustryPriority is null, "new campaigns should preserve the strategic fallback until a player chooses a priority");
        var provider = new CampaignIndustryPriorityProvider(new FixedIndustryPriorityProvider(2, 1));
        provider.Bind(galaxy);
        Require(provider.GetWeights(player).ConstructionWeight == 2, "unset priority did not preserve injected fallback policy");
        Require(IndustryPriorityCommands.Set(galaxy, player + 999, player, IndustryPriority.InfrastructureFirst).Accepted == false,
            "foreign actor changed an industry priority");
        Require(IndustryPriorityCommands.Set(galaxy, player, player, IndustryPriority.InfrastructureFirst).Accepted,
            "owner could not set industry priority");
        var weights = provider.GetWeights(player);
        Require(weights.ConstructionWeight == 3 && weights.ShipbuildingWeight == 1, "infrastructure priority did not select 3:1 weights");
        var allocation = new WeightedFairIndustryAllocationPolicy(provider).Allocate(new(player, 40, 40, 40));
        Require(allocation.ConstructionAllocated == 30 && allocation.ShipbuildingAllocated == 10 && allocation.TotalAllocated == 40,
            "priority changed scarcity allocation or conservation incorrectly");
        var reflow = new WeightedFairIndustryAllocationPolicy(provider).Allocate(new(player, 40, 5, 40));
        Require(reflow.ConstructionAllocated == 5 && reflow.ShipbuildingAllocated == 35, "unused priority share did not reflow");

        var path = Path.Combine(Path.GetTempPath(), "stellar-industry-priority-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            new CampaignSaveService().Save(path, galaxy, 0);
            var restored = new CampaignSaveService().Load(path).Galaxy;
            Require(restored.Economies.Single(e => e.CivilizationId == player).IndustryPriority == IndustryPriority.InfrastructureFirst,
                "industry priority did not persist");
            var legacy = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            legacy["Galaxy"]!["Economies"]!.AsArray()[0]!.AsObject().Remove("IndustryPriority");
            File.WriteAllText(path, legacy.ToJsonString());
            Require(new CampaignSaveService().Load(path).Galaxy.Economies.All(e => e.IndustryPriority is null),
                "legacy economy priority did not preserve fallback state");
            legacy["Galaxy"]!["Economies"]!.AsArray()[0]!["IndustryPriority"] = 99;
            File.WriteAllText(path, legacy.ToJsonString());
            try { new CampaignSaveService().Load(path); throw new InvalidOperationException("unknown priority enum was accepted"); }
            catch (InvalidDataException) { }
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

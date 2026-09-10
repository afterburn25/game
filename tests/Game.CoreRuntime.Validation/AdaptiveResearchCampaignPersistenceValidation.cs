using System.Text.Json.Nodes;
using Game.Campaign;
using Game.Persistence;
using Game.Simulation.Construction;

namespace Game.CoreRuntime.Validation;

internal static class AdaptiveResearchCampaignPersistenceValidation
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"stellar-adaptive-campaign-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var persistence = new CampaignStatePersistenceService();
            var sessions = new CampaignSessionService(saveService: persistence);
            var campaign = sessions.CreateNew(20260908);
            var playerId = campaign.Galaxy.PlayerCivilizationId;
            var playerResearch = campaign.AdaptiveResearch.GetCivilization(playerId);
            var started = campaign.AdaptiveResearch.Runtime.Authority.StartDirectedResearch(
                playerResearch, "fusion_power", 4);
            Require(started.Accepted, $"could not establish persistence fixture: {started.Message}");
            var playerEconomy = campaign.Galaxy.Economies.Single(value => value.CivilizationId == playerId);
            playerEconomy.LastResearchSpendingPerDay = 0.75;
            playerEconomy.LastResearchFundingFraction = 0.625;
            var home = campaign.Galaxy.Colonies.First(value => value.CivilizationId == playerId);
            Require(SurfaceConstruction.Place(campaign.Galaxy, playerId, home.Id,
                    "science_lab", 120, 80, 0).Accepted,
                "could not establish a v12 surface payload for v13 migration coverage");

            var path = Path.Combine(directory, "campaign.json");
            sessions.Save(path, campaign.Galaxy, campaign.Diplomacy, campaign.AdaptiveResearch, 91.25);
            var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            Require(root["FormatVersion"]!.GetValue<int>() == CampaignStatePersistenceService.CurrentFormatVersion &&
                    root["AdaptiveResearch"] is JsonObject,
                "current campaign did not write the v15 Adaptive Research payload");

            var loaded = persistence.Load(path);
            var restored = loaded.AdaptiveResearch.GetCivilization(playerId);
            Require(loaded.SimulationDays == 91.25 && restored.ActiveProjects.ContainsKey("fusion_power") &&
                    Math.Abs(restored.ActiveProjects["fusion_power"].AssignedEffectiveLabs - 4) < 0.000001,
                "Adaptive Research project ownership or lab assignment did not survive save/load");
            var restoredEconomy = loaded.Galaxy.Economies.Single(value => value.CivilizationId == playerId);
            Require(Math.Abs(restoredEconomy.LastResearchSpendingPerDay - 0.75) < 0.000001 &&
                    Math.Abs(restoredEconomy.LastResearchFundingFraction - 0.625) < 0.000001,
                "research spending or funded fraction did not survive save/load");

            var invalidFundingPath = Path.Combine(directory, "invalid-funding.json");
            var invalidFundingRoot = (JsonObject)root.DeepClone();
            invalidFundingRoot["Galaxy"]!["Economies"]![0]!["LastResearchFundingFraction"] = 1.25;
            File.WriteAllText(invalidFundingPath, invalidFundingRoot.ToJsonString());
            Reject(() => persistence.Load(invalidFundingPath),
                "campaign load accepted an impossible research funding fraction");

            var migratedPath = Path.Combine(directory, "v13.json");
            var migratedRoot = (JsonObject)root.DeepClone();
            migratedRoot["FormatVersion"] = CampaignStatePersistenceService.SurfaceFormatVersion;
            migratedRoot.Remove("AdaptiveResearch");
            File.WriteAllText(migratedPath, migratedRoot.ToJsonString());
            var migrated = persistence.Load(migratedPath);
            Require(migrated.AdaptiveResearch.Civilizations.Count == migrated.Galaxy.Civilizations.Count &&
                    !migrated.AdaptiveResearch.GetCivilization(playerId).ActiveProjects.ContainsKey("fusion_power"),
                "v13 migration did not compose bounded starting research without inventing active work");

            var invalidPath = Path.Combine(directory, "invalid.json");
            var invalidRoot = (JsonObject)root.DeepClone();
            var entries = invalidRoot["AdaptiveResearch"]!["Civilizations"]!.AsArray();
            entries[0]!["SpeciesId"] = "wrong_species";
            File.WriteAllText(invalidPath, invalidRoot.ToJsonString());
            Reject(() => persistence.Load(invalidPath),
                "v15 load accepted Adaptive Research state attached to the wrong species");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void Reject(Action action, string message)
    {
        try { action(); }
        catch (Exception) { return; }
        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

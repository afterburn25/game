using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Campaign;
using Game.Persistence;
using Game.Simulation;
using Game.Simulation.Construction;
using Game.Simulation.Economy;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.CoreRuntime.Validation;

internal static class SurfaceConstructionValidation
{
    public static void ValidateFreePlacementAndAuthority()
    {
        var galaxy = CreateGalaxy();
        var colony = Home(galaxy);
        var economy = galaxy.Economies.Single(item => item.CivilizationId == galaxy.PlayerCivilizationId);
        var industry = economy.Industry;
        Place(galaxy, "science_lab", 100.125f, -80.375f, -37.5f);
        var placed = colony.SurfaceBuildings.Single();
        Require(placed.X == 100.125f && placed.Z == -80.375f && placed.RotationDegrees == 322.5f &&
            placed.IndustryProgress == 0 && !placed.IsComplete && economy.Industry == industry,
            "free placement snapped coordinates, completed instantly, or charged industry before construction");
        var snapshot = JsonSerializer.Serialize(colony.SurfaceBuildings);
        foreach (var invalid in new (string Type, float X, float Z, float Yaw)[]
        {
            ("missing_building", -100, 100, 0), ("science_lab", 0, 0, 0),
            ("science_lab", 510, 0, 0), ("science_lab", float.NaN, 90, 0),
            ("science_lab", 90, float.PositiveInfinity, 0), ("science_lab", 90, 90, float.NaN),
            ("power_generator", 100.25f, -80.25f, 15),
        })
            Require(!SurfaceConstruction.Place(galaxy, galaxy.PlayerCivilizationId, colony.Id,
                invalid.Type, invalid.X, invalid.Z, invalid.Yaw).Accepted, "invalid placement was accepted");
        var foreign = galaxy.Colonies.First(item => item.CivilizationId != galaxy.PlayerCivilizationId);
        Require(!SurfaceConstruction.Place(galaxy, galaxy.PlayerCivilizationId, foreign.Id,
            "power_generator", 100, 100, 0).Accepted && foreign.SurfaceBuildings.Count == 0,
            "player placed construction into another faction's colony");
        Require(!SurfaceConstruction.Place(galaxy, galaxy.PlayerCivilizationId, int.MaxValue,
            "power_generator", 100, 100, 0).Accepted, "missing colony accepted construction");
        var earthId = colony.PlanetaryBodyId;
        colony.PlanetaryBodyId = galaxy.PlanetaryBodies.Single(body => body.SystemId == SolCatalogPreset.SystemId && body.Name == "Jupiter").Id;
        Require(!SurfaceConstruction.Place(galaxy, galaxy.PlayerCivilizationId, colony.Id,
            "power_generator", -100, 100, 0).Accepted, "gas giant accepted a ground building");
        colony.PlanetaryBodyId = earthId;
        Require(JsonSerializer.Serialize(colony.SurfaceBuildings) == snapshot && economy.Industry == industry,
            "rejected placement changed an existing site or spent industry");
        SurfaceConstruction.Validate(colony);
    }

    public static void ValidateRateBudgetAndPause()
    {
        var galaxy = CreateGalaxy();
        var colony = Home(galaxy);
        var player = galaxy.PlayerCivilizationId;
        var economy = galaxy.Economies.Single(item => item.CivilizationId == player);
        Place(galaxy, "power_generator", 100.125f, 80.75f, 0);
        Place(galaxy, "science_lab", -100.5f, 85.25f, 15);
        economy.Industry = 20;
        SurfaceConstruction.Advance(galaxy, player, 1000, 0.5);
        Near(economy.Industry, 0, "construction overspent its available stock");
        Near(colony.SurfaceBuildings.Sum(item => item.IndustryProgress), 20, "spent industry vanished or multiplied");
        Require(colony.SurfaceBuildings.All(item => item.IndustryProgress > 0 && item.IndustryProgress <= 15 && !item.IsComplete),
            "one site starved or exceeded its half-day build rate");
        var beforePause = JsonSerializer.Serialize(colony.SurfaceBuildings);
        economy.Industry = 100;
        SurfaceConstruction.Advance(galaxy, player, 100, 0);
        new GalaxySimulationStepCoordinator().Advance(galaxy, 0);
        Require(JsonSerializer.Serialize(colony.SurfaceBuildings) == beforePause && economy.Industry == 100,
            "paused surface construction progressed or spent industry");
        SurfaceConstruction.Advance(galaxy, player, 7.5, 1);
        Near(economy.Industry, 92.5, "construction ignored a constrained shared budget");
        Near(colony.SurfaceBuildings.Sum(item => item.IndustryProgress), 27.5, "budget did not reach the sites exactly once");
        foreach (var invalid in new[] { (double.NaN, 1.0), (-1.0, 1.0), (1.0, double.PositiveInfinity), (1.0, -1.0) })
        {
            var before = JsonSerializer.Serialize(colony.SurfaceBuildings);
            Throws<ArgumentOutOfRangeException>(() => SurfaceConstruction.Advance(galaxy, player, invalid.Item1, invalid.Item2));
            Require(JsonSerializer.Serialize(colony.SurfaceBuildings) == before && economy.Industry == 92.5,
                "invalid construction time or budget corrupted the economy");
        }
    }

    public static void ValidateRemovalAuthorityAndEffects()
    {
        var galaxy = CreateGalaxy();
        var player = galaxy.PlayerCivilizationId;
        var colony = Home(galaxy);
        var economy = galaxy.Economies.Single(item => item.CivilizationId == player);
        var startingCredits = economy.Credits;

        Place(galaxy, "science_lab", 100, 100, 0);
        var cancelledId = colony.SurfaceBuildings.Single().Id;
        Require(!SurfaceConstruction.Remove(galaxy, player, int.MaxValue, cancelledId).Accepted,
            "a missing colony accepted surface removal");
        var foreign = galaxy.Colonies.First(item => item.CivilizationId != player);
        Require(!SurfaceConstruction.Remove(galaxy, player, foreign.Id, cancelledId).Accepted,
            "the player removed a building from a foreign colony");
        Require(!SurfaceConstruction.Remove(galaxy, player, colony.Id, int.MaxValue).Accepted,
            "a missing surface building accepted removal");
        var cancelled = SurfaceConstruction.Remove(galaxy, player, colony.Id, cancelledId);
        Require(cancelled.Accepted && colony.SurfaceBuildings.Count == 0,
            "an owned incomplete construction site could not be cancelled");
        Near(economy.Credits, startingCredits - 20,
            "cancelling a science lab did not retain half its authorization cost");

        Place(galaxy, "power_generator", -100, 100, 0);
        var generator = colony.SurfaceBuildings.Single();
        economy.Industry = 1000;
        SurfaceConstruction.Advance(galaxy, player, 1000, 100);
        Require(generator.IsComplete && SurfaceConstruction.GetOutput(colony).Supply == 6,
            "demolition fixture did not complete and power the generator");
        var creditsBeforeDemolition = economy.Credits;
        var demolished = SurfaceConstruction.Remove(galaxy, player, colony.Id, generator.Id);
        Require(demolished.Accepted && colony.SurfaceBuildings.Count == 0 && SurfaceConstruction.GetOutput(colony).Supply == 2,
            "demolishing a completed generator did not stop its output");
        Near(economy.Credits, creditsBeforeDemolition,
            "demolishing a completed building incorrectly refunded its authorization cost");
    }

    public static void ValidateSharedConstructionBudget()
    {
        var galaxy = CreateGalaxy();
        var player = galaxy.PlayerCivilizationId;
        var colony = Home(galaxy);
        var economy = galaxy.Economies.Single(item => item.CivilizationId == player);
        var construction = new ConstructionSimulation();
        Require(construction.StartProject(galaxy, player, "research_network").Accepted, "normal opening project unavailable");
        Place(galaxy, "science_lab", 100.25f, 100.5f, 0);
        Place(galaxy, "fabricator", -100.25f, 100.5f, 25);
        economy.Industry = 100;
        construction.Advance(galaxy, new Dictionary<int, double> { [player] = 75 }, 1);
        var project = galaxy.ConstructionStates.Single(item => item.CivilizationId == player);
        var surfaceProgress = colony.SurfaceBuildings.Sum(item => item.IndustryProgress);
        Near(economy.Industry, 25, "project and surface sites spent outside their shared allocation");
        Near(project.ActiveProjectProgress + surfaceProgress, 75, "project/site progress does not conserve industry");
        Require(project.ActiveProjectProgress > 0 && colony.SurfaceBuildings.All(item => item.IndustryProgress is > 0 and <= 30),
            "regular project or surface sites were starved, or a site exceeded its daily rate");
        Require(galaxy.ShipyardStates.Single(item => item.CivilizationId == player).ActiveDesignId is null,
            "ground construction bypassed shipyard prerequisites");
    }

    public static void ValidatePowerAndEconomy()
    {
        var galaxy = CreateGalaxy();
        var baseline = CreateGalaxy();
        var player = galaxy.PlayerCivilizationId;
        var colony = Home(galaxy);
        var economy = galaxy.Economies.Single(item => item.CivilizationId == player);
        var originalEconomy = baseline.Economies.Single(item => item.CivilizationId == player);
        Place(galaxy, "science_lab", 100, 100, 0);
        Place(galaxy, "fabricator", -100, 100, 0);
        Require(SurfaceConstruction.GetOutput(colony).SciencePerDay == 0 && SurfaceConstruction.GetOutput(colony).IndustryPerDay == 0,
            "unfinished sites produced resources");
        economy.Industry = 5000;
        SurfaceConstruction.Advance(galaxy, player, 5000, 100);
        var unpowered = SurfaceConstruction.GetOutput(colony);
        Require(unpowered.Supply == 2 && unpowered.Demand == 4 && unpowered.SciencePerDay == 1 &&
            unpowered.IndustryPerDay == 0 && unpowered.PoweredBuildingIds.SetEquals(new[] { 1 }),
            "limited hub power did not select completed consumers deterministically");
        Place(galaxy, "power_generator", 100, -100, 0);
        Require(SurfaceConstruction.GetOutput(colony).Supply == 2, "unfinished generator supplied power");
        SurfaceConstruction.Advance(galaxy, player, 5000, 100);
        var powered = SurfaceConstruction.GetOutput(colony);
        Require(powered.Supply == 6 && powered.Demand == 4 && powered.SciencePerDay == 1 && powered.IndustryPerDay == 1 &&
            powered.PoweredBuildingIds.SetEquals(new[] { 1, 2, 3 }), "generator did not power ordinary completed buildings");
        Place(galaxy, "power_generator", -100, -100, 0);
        Place(galaxy, "trade_hub", 180, 0, 0);
        SurfaceConstruction.Advance(galaxy, player, 5000, 100);
        powered = SurfaceConstruction.GetOutput(colony);
        Require(powered.Supply == 10 && powered.Demand == 6 && powered.CreditsPerDay == .08 &&
            powered.PoweredBuildingIds.SetEquals(new[] { 1, 2, 3, 4, 5 }), "trade hub did not join the powered colony economy");
        var creditFlow = EconomySimulation.GetCreditFlow(galaxy, player);
        Near(creditFlow.TradeRevenuePerDay, .08, "cash-flow breakdown omitted powered surface trade");
        Near(creditFlow.NetCreditsPerDay, creditFlow.GrossIncomePerDay - creditFlow.OperatingCostsPerDay,
            "cash-flow breakdown did not reconcile to its displayed net");
        var science = economy.Science;
        var industry = economy.Industry;
        var credits = economy.Credits;
        var originalScience = originalEconomy.Science;
        var originalIndustry = originalEconomy.Industry;
        var originalCredits = originalEconomy.Credits;
        new EconomySimulation().Advance(galaxy, 1);
        new EconomySimulation().Advance(baseline, 1);
        Near(economy.LastCreditsPerSecond, creditFlow.NetCreditsPerDay,
            "displayed cash-flow snapshot disagreed with authoritative economy output");
        Near((economy.Science - science) - (originalEconomy.Science - originalScience), 1,
            "completed powered lab failed to contribute through the authoritative economy");
        Near((economy.Industry - industry) - (originalEconomy.Industry - originalIndustry), 1,
            "completed powered fabricator failed to contribute through the authoritative economy");
        Near((economy.Credits - credits) - (originalEconomy.Credits - originalCredits), .08,
            "completed powered trade hub failed to contribute through the authoritative economy");
    }

    public static void ValidateFramePartitionIndependence()
    {
        (double Project, double Surface) AdvanceDay(int steps)
        {
            var galaxy = CreateGalaxy();
            var player = galaxy.PlayerCivilizationId;
            var economy = galaxy.Economies.Single(item => item.CivilizationId == player);
            var simulation = new ConstructionSimulation();
            Require(simulation.StartProject(galaxy, player, "research_network").Accepted, "opening project unavailable");
            Place(galaxy, "science_lab", 100, 100, 0);
            economy.Industry = 0;
            for (var step = 0; step < steps; step++)
            {
                economy.Industry += 10.0 / steps;
                simulation.Advance(galaxy, new Dictionary<int, double> { [player] = 10.0 / steps }, 1.0 / steps);
            }
            var project = galaxy.ConstructionStates.Single(item => item.CivilizationId == player).ActiveProjectProgress;
            var surface = Home(galaxy).SurfaceBuildings.Single().IndustryProgress;
            Near(project + surface + economy.Industry, 10, "partitioned construction lost its daily industry");
            return (project, surface);
        }
        var reference = AdvanceDay(1);
        Require(reference.Project > 0 && reference.Surface > 0, "daily construction starved a competing project");
        foreach (var steps in new[] { 4, 60 })
        {
            var partitioned = AdvanceDay(steps);
            Near(partitioned.Project, reference.Project, $"ordinary project progress changed with {steps} frames/day");
            Near(partitioned.Surface, reference.Surface, $"surface progress changed with {steps} frames/day");
        }
    }

    public static void ValidateSaveContinuity() => InTemporaryDirectory(directory =>
    {
        var sessions = new CampaignSessionService();
        var session = sessions.CreateNew(2026090817);
        var galaxy = session.Galaxy;
        var plain = new CampaignSaveService();
        var oldPath = Path.Combine(directory, "empty-sol.json");
        plain.Save(oldPath, galaxy, 17);
        Require(Version(oldPath) == 10, "empty Sol galaxy was needlessly promoted past its compatible format");
        var oldCampaign = Path.Combine(directory, "empty-sol-campaign.json");
        sessions.Save(oldCampaign, galaxy, session.Diplomacy, 17);
        Require(Version(oldCampaign) == 11, "empty Sol campaign was needlessly promoted past its compatible format");
        Place(galaxy, "science_lab", 113.125f, -87.375f, 32.5f);
        SurfaceConstruction.Advance(galaxy, galaxy.PlayerCivilizationId, 7.25, 0.5);
        var expected = JsonSerializer.Serialize(Home(galaxy).SurfaceBuildings);
        var surfacePath = Path.Combine(directory, "surface.json");
        plain.Save(surfacePath, galaxy, 17.5);
        var restored = plain.Load(surfacePath);
        Require(Version(surfacePath) == 12 && restored.SimulationDays == 17.5 &&
            JsonSerializer.Serialize(Home(restored.Galaxy).SurfaceBuildings) == expected,
            "standalone save lost exact placement, yaw, partial progress, or clock");
        var campaignPath = Path.Combine(directory, "surface-campaign.json");
        sessions.Save(campaignPath, galaxy, session.Diplomacy, 17.5);
        var campaign = new CampaignStatePersistenceService().Load(campaignPath);
        Require(Version(campaignPath) == 13 && campaign.SimulationDays == 17.5 &&
            JsonSerializer.Serialize(Home(campaign.Galaxy).SurfaceBuildings) == expected &&
            campaign.Galaxy.PlanetaryBodies.SequenceEqual(galaxy.PlanetaryBodies),
            "campaign wrapper lost surface placement or reconstructed a different body catalog");
        SurfaceConstruction.Advance(campaign.Galaxy, galaxy.PlayerCivilizationId, 10, 0.5);
        Near(Home(campaign.Galaxy).SurfaceBuildings.Single().IndustryProgress, 17.25,
            "loaded incomplete construction failed to resume from its saved progress");
    });

    public static void ValidateInvalidSurfaceSaves() => InTemporaryDirectory(directory =>
    {
        var galaxy = CreateGalaxy();
        Place(galaxy, "science_lab", 113.125f, -87.375f, 32.5f);
        SurfaceConstruction.Advance(galaxy, galaxy.PlayerCivilizationId, 7.25, 0.5);
        var path = Path.Combine(directory, "valid.json");
        var persistence = new CampaignSaveService();
        persistence.Save(path, galaxy, 1);
        var valid = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        JsonObject Colony(JsonObject root) => root["Galaxy"]!["Colonies"]!.AsArray()
            .Single(item => item!["Id"]!.GetValue<int>() == Home(galaxy).Id)!.AsObject();
        JsonObject Site(JsonObject root) => Colony(root)["SurfaceBuildings"]![0]!.AsObject();
        var corruptions = new (string Name, Action<JsonObject> Change)[]
        {
            ("missing-collection", root => Colony(root)["SurfaceBuildings"] = null),
            ("missing-x", root => Site(root).Remove("X")),
            ("missing-rotation", root => Site(root).Remove("RotationDegrees")),
            ("missing-progress", root => Site(root).Remove("IndustryProgress")),
            ("unknown-type", root => Site(root)["TypeId"] = "unknown"),
            ("outside-colony", root => Site(root)["X"] = 512),
            ("negative-progress", root => Site(root)["IndustryProgress"] = -1),
            ("overspent-progress", root => Site(root)["IndustryProgress"] = 401),
            ("false-completion", root => Site(root)["IsComplete"] = true),
            ("duplicate-id", root => Colony(root)["SurfaceBuildings"]!.AsArray().Add(Site(root).DeepClone())),
            ("overlapping-sites", root =>
            {
                var duplicate = (JsonObject)Site(root).DeepClone();
                duplicate["Id"] = 99;
                Colony(root)["SurfaceBuildings"]!.AsArray().Add(duplicate);
            }),
            ("missing-economies", root => root["Galaxy"]!.AsObject().Remove("Economies")),
            ("empty-economies", root => root["Galaxy"]!["Economies"] = new JsonArray()),
            ("downgraded-format", root => root["FormatVersion"] = 10),
        };
        var acceptedCorruptions = new List<string>();
        foreach (var corruption in corruptions)
        {
            var changed = (JsonObject)valid.DeepClone();
            corruption.Change(changed);
            var corruptPath = Path.Combine(directory, corruption.Name + ".json");
            File.WriteAllText(corruptPath, changed.ToJsonString());
            try { persistence.Load(corruptPath); acceptedCorruptions.Add(corruption.Name); }
            catch (InvalidDataException) { }
            catch (JsonException) { } // Required-field decoding is rejected before model validation.
        }
        Require(acceptedCorruptions.Count == 0, "Accepted malformed surface saves: " + string.Join(", ", acceptedCorruptions));
        Require(Home(persistence.Load(path).Galaxy).SurfaceBuildings.Count == 1,
            "negative validation damaged the valid checkpoint");
    });

    private static GalaxyState CreateGalaxy() => new GalaxyGenerator().Generate(2026090817,
        new GalaxyGenerationSettings { SystemCount = 48, PreWarpCivilizationCount = 4, AncientCivilizationCount = 1, Radius = 600 });
    private static ColonyState Home(GalaxyState galaxy) => galaxy.Colonies.Single(item => item.CivilizationId == galaxy.PlayerCivilizationId);
    private static void Place(GalaxyState galaxy, string type, float x, float z, float rotation) =>
        Require(SurfaceConstruction.Place(galaxy, galaxy.PlayerCivilizationId, Home(galaxy).Id, type, x, z, rotation).Accepted,
            $"valid free placement failed: {type} at {x},{z}");
    private static int Version(string path) => JsonNode.Parse(File.ReadAllText(path))!["FormatVersion"]!.GetValue<int>();
    private static void Near(double actual, double expected, string message) =>
        Require(Math.Abs(actual - expected) < 0.000001, $"{message}; expected={expected:R}, actual={actual:R}");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected rejection with {typeof(T).Name}.");
    }
    private static void InTemporaryDirectory(Action<string> verify)
    {
        var directory = Path.Combine(Path.GetTempPath(), "stellar-surface-validation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { verify(directory); }
        finally
        {
            var expectedParent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Require(string.Equals(Path.GetDirectoryName(Path.GetFullPath(directory)), expectedParent, StringComparison.OrdinalIgnoreCase),
                "refusing test cleanup outside the exact temporary directory");
            Directory.Delete(directory, recursive: true);
        }
    }
}

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Game.Campaign;
using Game.Simulation.Generation;
using Game.Simulation.Models;
using Game.Simulation.Species;
using Game.Simulation.Exploration;

namespace Game.CoreRuntime.Validation;

internal static class SandboxGenerationSetupValidation
{
    internal static void Run()
    {
        Require(CampaignSeed.Parse(long.MinValue.ToString(CultureInfo.InvariantCulture)) == long.MinValue,
            "minimum legacy numeric seed changed");
        Require(CampaignSeed.Parse(long.MaxValue.ToString(CultureInfo.InvariantCulture)) == long.MaxValue,
            "maximum legacy numeric seed changed");
        Require(CampaignSeed.Parse("  Sol-Ascendant-42  ") == CampaignSeed.Parse("sol-ascendant-42"),
            "text seed normalization is not stable across case and surrounding whitespace");
        Require(CampaignSeed.Parse("SOL-ASCENDANT-42") != CampaignSeed.Parse("SOL-ASCENDANT-43"),
            "distinct text seeds resolved to the same test seed");
        Require(long.TryParse(CampaignSeed.CreateRandomNumericText(), NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out _), "random seed was not a valid portable numeric seed");

        const string enteredSeed = "SOL-ASCENDANT-42";
        var sessions = new CampaignSessionService();
        var first = sessions.CreateNew(enteredSeed);
        var second = sessions.CreateNew(enteredSeed);
        var metadata = first.Galaxy.GenerationMetadata
            ?? throw new InvalidOperationException("new Sandbox omitted generation metadata");
        Require(metadata.EnteredSeed == enteredSeed && metadata.InternalSeed == first.Seed,
            "Sandbox did not retain both entered and internal seeds");
        Require(metadata.GeneratorVersion == GalaxyGenerationMetadata.CurrentGeneratorVersion &&
            metadata.SystemCount == 100 && metadata.OtherCivilizations == 5 &&
            metadata.AncientCivilizations == "Rare" && metadata.GalaxyShape == "Barred spiral" &&
            metadata.ArtProfileVersion == "milky-way-barred-v1",
            "recommended 100-system setup metadata changed");
        Require(first.Galaxy.Systems.Count == 100 && first.Galaxy.Civilizations.Count == 7,
            "recommended Sandbox did not create the expected player, ordinary and ancient civilizations");
        Require(first.Galaxy.Systems.Select(system => system.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 100 &&
            first.Galaxy.Systems.All(system => !system.Name.StartsWith("SYS-", StringComparison.OrdinalIgnoreCase)),
            "generated Sandbox retained placeholder or duplicate system names");
        foreach (var system in first.Galaxy.Systems.Where(system => system.CatalogPresetId is null))
        {
            var planets = first.Galaxy.PlanetaryBodies.Where(body => body.SystemId == system.Id &&
                body.Kind == PlanetaryBodyKind.Planet).ToArray();
            Require(planets.Select(body => body.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == planets.Length &&
                planets.All(body => !body.Name.StartsWith(system.Name + " ", StringComparison.OrdinalIgnoreCase)),
                $"system {system.Name} retained orbital placeholders or duplicate planet names");
            foreach (var moon in first.Galaxy.PlanetaryBodies.Where(body => body.SystemId == system.Id &&
                         body.Kind == PlanetaryBodyKind.Moon))
            {
                var parent = planets.Single(planet => planet.Id == moon.ParentBodyId);
                Require(moon.Name.StartsWith(parent.Name + " ", StringComparison.Ordinal),
                    $"moon {moon.Name} does not retain its named parent relationship");
            }
        }
        var stellarCounts = first.Galaxy.Systems.GroupBy(system => system.StellarClass)
            .ToDictionary(group => group.Key, group => group.Count());
        Require(stellarCounts[StellarPrimaryClass.MRedDwarf] == 48 &&
            stellarCounts[StellarPrimaryClass.KOrangeDwarf] == 20 &&
            stellarCounts[StellarPrimaryClass.GYellowDwarf] == 11 &&
            stellarCounts[StellarPrimaryClass.FYellowWhiteDwarf] == 6 &&
            stellarCounts[StellarPrimaryClass.AWhiteStar] == 3 &&
            stellarCounts[StellarPrimaryClass.HotBlueStar] == 1 &&
            stellarCounts[StellarPrimaryClass.Giant] == 4 &&
            stellarCounts[StellarPrimaryClass.WhiteDwarf] == 3 &&
            stellarCounts[StellarPrimaryClass.NeutronStar] == 2 &&
            stellarCounts[StellarPrimaryClass.BlackHole] == 1 &&
            stellarCounts[StellarPrimaryClass.Protostar] == 1,
            "balanced physical stellar deck does not total the agreed 100-system quotas");
        Require(first.Galaxy.Systems.Single(system => system.CatalogPresetId == SolCatalogPreset.PresetId).StellarClass ==
            StellarPrimaryClass.GYellowDwarf, "authored Sol was not retained as a G-type star");
        Require(first.Galaxy.Systems.Select(system => (system.Name, system.Position, system.Archetype))
            .SequenceEqual(second.Galaxy.Systems.Select(system => (system.Name, system.Position, system.Archetype))),
            "same text seed and setup did not reproduce system names, positions and star types");
        var planetCounts = first.Galaxy.Systems.Select(system => first.Galaxy.PlanetaryBodies.Count(body =>
            body.SystemId == system.Id && body.Kind == PlanetaryBodyKind.Planet)).ToArray();
        Require(planetCounts.Count(count => count == 0) == 18 &&
            planetCounts.Count(count => count is >= 1 and <= 2) == 22 &&
            planetCounts.Count(count => count is >= 3 and <= 6) == 42 &&
            planetCounts.Count(count => count is >= 7 and <= 10) == 14 &&
            planetCounts.Count(count => count is >= 11 and <= 14) == 4,
            "balanced planetary architecture deck does not match the agreed 100-system profile");
        Require(first.Galaxy.PlanetaryBodies.Any(body => body.Kind == PlanetaryBodyKind.Planet &&
                !first.Galaxy.PlanetaryBodies.Any(moon => moon.ParentBodyId == body.Id)) &&
            first.Galaxy.PlanetaryBodies.Any(body => body.Kind == PlanetaryBodyKind.Planet &&
                first.Galaxy.PlanetaryBodies.Count(moon => moon.ParentBodyId == body.Id) > 1),
            "balanced catalog lacks both moonless and multi-moon planets");
        var nonSol = first.Galaxy.Systems.Where(system => system.CatalogPresetId is null).ToArray();
        Require(nonSol.Min(system => system.Position.X) < -700 &&
            nonSol.Max(system => system.Position.X) > 350 &&
            nonSol.Min(system => system.Position.Y) < -450 &&
            nonSol.Max(system => system.Position.Y) > 300,
            "barred-spiral systems do not occupy the core, arms and outer map");
        Require(nonSol.Min(system => Vector2.Distance(system.Position, Vector2.Zero)) < 150 &&
            nonSol.Count(system => Vector2.Distance(system.Position, Vector2.Zero) < 300) >= 3,
            "the Sol start has no practical early exploration neighborhood");
        Require(metadata.SpoilerFreeSummary.Contains("100 systems", StringComparison.Ordinal) &&
            !metadata.SpoilerFreeSummary.Contains("Sol", StringComparison.OrdinalIgnoreCase),
            "setup summary is missing its size or reveals generated content");
        var laneNetwork = new InterstellarLaneNetwork();
        var lanes = laneNetwork.Build(first.Galaxy.Systems);
        Require(lanes.Count is >= 99 and <= 260 && lanes.All(lane => lane.LengthLightYears > 0) &&
            lanes.DistinctBy(lane => (lane.FirstSystemId, lane.SecondSystemId)).Count() == lanes.Count,
            "interstellar lane graph is disconnected, duplicated or too dense");
        Require(first.Galaxy.Systems.All(system =>
                laneNetwork.FindShortestRoute(first.Galaxy.Systems, SolCatalogPreset.SystemId, system.Id).Count > 0) &&
            first.Galaxy.Civilizations.All(civilization =>
                lanes.Count(lane => lane.Connects(civilization.HomeSystemId)) >= 2),
            "lane graph does not connect every system or leaves a starting system without alternatives");
        Require(lanes.SequenceEqual(laneNetwork.Build(second.Galaxy.Systems)),
            "same seed and coordinates did not reproduce the lane graph");
        Require(Math.Abs(AstronomicalDistance.LightYearsToParsecs(3.26156) - 1.0) < 1e-10 &&
            Math.Abs(AstronomicalDistance.AuToKilometres(1.0) - 149_597_870.7) < 1e-6,
            "maintained astronomical unit conversions changed");

        var root = Path.Combine(Path.GetTempPath(), $"stellar-continuum-sandbox-setup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "sandbox.json");
            sessions.Save(path, first.Galaxy, first.Diplomacy, first.AdaptiveResearch, 0.0);
            var loaded = sessions.LoadOrCreate(path, fallbackSeed: 1);
            Require(loaded.Galaxy.GenerationMetadata == metadata,
                "entered seed or generation option snapshot did not survive save and load");
            Require(loaded.Galaxy.Systems.Select(system => system.StellarClass)
                .SequenceEqual(first.Galaxy.Systems.Select(system => system.StellarClass)),
                "physical stellar classes did not survive save and load");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }

        var legacy = sessions.CreateNew(12345L);
        Require(legacy.Galaxy.Civilizations.Count == 10,
            "numeric campaign creation no longer preserves its established civilization defaults");

        for (var index = 0; index < 12; index++)
            ValidateNearbyWorldGuarantees(sessions.CreateNew($"FAIR-OPENING-{index}").Galaxy);
    }

    private static void ValidateNearbyWorldGuarantees(GalaxyState galaxy)
    {
        var evaluator = new SpeciesPlanetaryHabitabilityEvaluator();
        var homeIds = galaxy.Civilizations.Select(civilization => civilization.HomeSystemId).ToHashSet();
        foreach (var civilization in galaxy.Civilizations.Where(civilization => !civilization.IsSeededAncient))
        {
            var home = galaxy.Systems.Single(system => system.Id == civilization.HomeSystemId);
            var viableSystems = galaxy.Systems.Where(system => !homeIds.Contains(system.Id) &&
                    Vector2.Distance(home.Position, system.Position) <= NearbyHabitableWorldGuaranteePolicy.MaximumOpeningDistance)
                .Count(system => galaxy.PlanetaryBodies.Any(body => body.SystemId == system.Id &&
                    evaluator.Evaluate(body, civilization.SpeciesId).Viability == SpeciesColonizationViability.NaturallyViable));
            Require(viableSystems >= 2,
                $"civilization {civilization.Id} has only {viableSystems} nearby species-compatible expansion worlds");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

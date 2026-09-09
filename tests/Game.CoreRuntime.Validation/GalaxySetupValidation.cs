using System.Numerics;
using System.Text.Json.Nodes;
using Game.Campaign;
using Game.Persistence;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.CoreRuntime.Validation;

internal static class GalaxySetupValidation
{
    public static void Validate()
    {
        var generator = new GalaxyGenerator();
        var sizes = new[] { 50, 100, 200 };
        foreach (var shape in Enum.GetValues<GalaxyShape>())
        {
            var options = new GalaxySetupOptions { Shape = shape, SystemCount = sizes[(int)shape], RivalEmpires = (int)shape == 0 ? 0 : 7, AncientEmpires = (int)shape == 0 ? 0 : 3 };
            const long seed = -9223372036854775807;
            var first = generator.Generate(seed, options.ToSettings());
            var second = generator.Generate(seed, options.ToSettings());
            Require(first.Systems.SequenceEqual(second.Systems), "Identical recipe changed its star catalog.");
            Require(first.PlanetaryBodies.SequenceEqual(second.PlanetaryBodies), "Identical recipe changed its planets.");
            Require(first.Civilizations.SequenceEqual(second.Civilizations), "Identical recipe changed its empires.");
            Require(first.Systems.Select(s => s.Position).SequenceEqual(SeededGalaxyLayout.Generate(seed, options)), "Preview differs from the generated galaxy.");
            Require(first.Systems.Count == options.SystemCount && first.Civilizations.Count == options.RivalEmpires + options.AncientEmpires + 1, "The requested size or empire counts were ignored.");
            Require(first.Civilizations.Count(c => c.IsPlayer) == 1 && first.Civilizations.Count(c => c.IsSeededAncient) == options.AncientEmpires, "Player or ancient count is wrong.");
            Require(first.Civilizations.Select(c => c.HomeSystemId).Distinct().Count() == first.Civilizations.Count, "Empire home systems overlap.");
            Require(first.Systems[0].Name == "Sol" && first.Systems[0].Position == Vector2.Zero && first.PlanetaryBodies.Any(b => b.Name == "Earth"), "Human origin was lost.");
            Require(!first.Systems.SequenceEqual(generator.Generate(seed + 1, options.ToSettings()).Systems), "Changing the seed did not change the world.");
            RoundTrip(first, options);
        }
        foreach (var shape in Enum.GetValues<GalaxyShape>())
        foreach (var size in sizes)
        for (long seed = 0; seed < 80; seed++)
        {
            var positions = SeededGalaxyLayout.Generate(seed, new() { Shape = shape, SystemCount = size });
            Require(positions.Skip(1).Any(p => p.Length() <= 95), "Home has no nearby exploration target.");
            for (var i = 0; i < positions.Count; i++)
            for (var j = i + 1; j < positions.Count; j++)
                Require(Vector2.DistanceSquared(positions[i], positions[j]) >= 24 * 24 - .1f, "Stars overlap at generation spacing.");
        }
        var standard = new GalaxySetupOptions();
        Require(!SeededGalaxyLayout.Generate(0, standard).SequenceEqual(SeededGalaxyLayout.Generate(0x100000001L, standard)), "Seed high bits collapsed into the old 32-bit collision.");
        foreach (var seed in new[] { long.MinValue, -1, 0, 1, long.MaxValue })
        {
            Require(GalaxySetupOptions.TryParseCode(standard.ShareCode(seed), out var decodedSeed, out var decoded) && decodedSeed == seed && decoded == standard, "Share code did not round-trip.");
        }
        foreach (var invalid in new[] { "", "SCG2:1:0:100:7:2", "SCG1:1:9:100:7:2", "SCG1:1:0:999999:7:2", "SCG1:1:0:100:8:2", "SCG1:9223372036854775808:0:100:7:2" })
            Require(!GalaxySetupOptions.TryParseCode(invalid, out _, out _), "Invalid recipe was accepted.");
        Require(new CampaignSessionService().CreateNew(20260908).Galaxy.GenerationOptions is null, "Legacy/default campaign generation was silently replaced.");
    }

    private static void RoundTrip(GalaxyState galaxy, GalaxySetupOptions options)
    {
        var path = Path.Combine(Path.GetTempPath(), "stellar-galaxy-recipe-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var saves = new CampaignSaveService();
            saves.Save(path, galaxy, 17);
            var loaded = saves.Load(path);
            Require(loaded.Galaxy.GenerationOptions == options && loaded.Galaxy.Seed == galaxy.Seed, "Save/load lost the seed or settings.");
            Require(loaded.Galaxy.Systems.SequenceEqual(galaxy.Systems) && loaded.Galaxy.PlanetaryBodies.SequenceEqual(galaxy.PlanetaryBodies), "Save/load regenerated different worlds.");
            var json = JsonNode.Parse(File.ReadAllText(path))!;
            json["Galaxy"]!.AsObject().Remove("GenerationOptions");
            File.WriteAllText(path, json.ToJsonString());
            Require(saves.Load(path).Galaxy.GenerationOptions is null, "Saves without recipe metadata do not remain loadable.");
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

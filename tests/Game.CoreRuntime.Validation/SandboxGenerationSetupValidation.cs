using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Game.Campaign;
using Game.Simulation.Generation;

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
            metadata.AncientCivilizations == "Rare",
            "recommended 100-system setup metadata changed");
        Require(first.Galaxy.Systems.Count == 100 && first.Galaxy.Civilizations.Count == 7,
            "recommended Sandbox did not create the expected player, ordinary and ancient civilizations");
        Require(first.Galaxy.Systems.Select(system => (system.Name, system.Position, system.Archetype))
            .SequenceEqual(second.Galaxy.Systems.Select(system => (system.Name, system.Position, system.Archetype))),
            "same text seed and setup did not reproduce system names, positions and star types");
        Require(metadata.SpoilerFreeSummary.Contains("100 systems", StringComparison.Ordinal) &&
            !metadata.SpoilerFreeSummary.Contains("Sol", StringComparison.OrdinalIgnoreCase),
            "setup summary is missing its size or reveals generated content");

        var root = Path.Combine(Path.GetTempPath(), $"stellar-continuum-sandbox-setup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "sandbox.json");
            sessions.Save(path, first.Galaxy, first.Diplomacy, first.AdaptiveResearch, 0.0);
            var loaded = sessions.LoadOrCreate(path, fallbackSeed: 1);
            Require(loaded.Galaxy.GenerationMetadata == metadata,
                "entered seed or generation option snapshot did not survive save and load");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }

        var legacy = sessions.CreateNew(12345L);
        Require(legacy.Galaxy.Civilizations.Count == 10,
            "numeric campaign creation no longer preserves its established civilization defaults");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

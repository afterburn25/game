using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

public sealed class GalaxyGenerationSettings
{
    // The first playable map is intentionally compact: it gives scouting, colonization,
    // diplomacy, and the system view room to matter without becoming a wall of stars.
    public int SystemCount { get; init; } = 100;
    public int PreWarpCivilizationCount { get; init; } = 8;
    public int AncientCivilizationCount { get; init; } = 2;
    public float Radius { get; init; } = 900.0f;
    public float InitialPreWarpSensorRange { get; init; } = 95.0f;
    public float InitialAncientSensorRange { get; init; } = 420.0f;

    public IReadOnlyDictionary<StarArchetype, double> ArchetypeWeights { get; init; } =
        new Dictionary<StarArchetype, double>
        {
            [StarArchetype.Standard] = 46,
            [StarArchetype.ResourceRich] = 12,
            [StarArchetype.HabitableRich] = 10,
            [StarArchetype.BarrenFrontier] = 8,
            [StarArchetype.Nebula] = 6,
            [StarArchetype.NeutronPulsar] = 5,
            [StarArchetype.BlackHole] = 3,
            [StarArchetype.AncientRuin] = 4,
            [StarArchetype.Dangerous] = 4,
            [StarArchetype.Legendary] = 2,
        };

    public double HabitableChance { get; init; } = 0.18;
    public double AnomalyChance { get; init; } = 0.20;
    public double RareResourceChance { get; init; } = 0.12;
    public double IndependentPreWarpChance { get; init; } = 0.04;
}

public sealed record GalaxyGenerationMetadata(
    string EnteredSeed,
    long InternalSeed,
    string GeneratorVersion,
    DateTimeOffset CreatedAtUtc,
    int SystemCount,
    string GalaxyShape,
    string StellarVariety,
    string PlanetBearingSystems,
    string HabitableWorlds,
    int GuaranteedNearbyHabitableWorlds,
    int OtherCivilizations,
    string AncientCivilizations,
    string SpaceHazards,
    string StartingDevelopment,
    string Difficulty)
{
    public const string CurrentGeneratorVersion = "galaxy-v1";

    public string SpoilerFreeSummary =>
        $"{SystemCount} systems · {StellarVariety.ToLowerInvariant()} stellar variety · " +
        $"{HabitableWorlds.ToLowerInvariant()} habitable worlds · {OtherCivilizations} other civilizations · " +
        $"{AncientCivilizations.ToLowerInvariant()} ancient powers";

    public static GalaxyGenerationMetadata Standard100(string enteredSeed, long internalSeed) => new(
        enteredSeed,
        internalSeed,
        CurrentGeneratorVersion,
        DateTimeOffset.UtcNow,
        100,
        "Barred spiral",
        "Balanced",
        "Common",
        "Uncommon",
        2,
        5,
        "Rare",
        "Standard",
        "Early Space Age",
        "Standard");

    public GalaxyGenerationSettings ToSettings() => new()
    {
        SystemCount = SystemCount,
        PreWarpCivilizationCount = OtherCivilizations + 1,
        AncientCivilizationCount = AncientCivilizations == "None" ? 0 : AncientCivilizations == "Standard" ? 2 : 1,
        HabitableChance = HabitableWorlds == "Rare" ? 0.09 : HabitableWorlds == "Common" ? 0.25 : 0.16,
    };
}

public static class CampaignSeed
{
    public static long Parse(string enteredSeed)
    {
        if (string.IsNullOrWhiteSpace(enteredSeed))
            throw new ArgumentException("Enter a number or a memorable text seed.", nameof(enteredSeed));
        var trimmed = enteredSeed.Trim();
        if (long.TryParse(trimmed, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var numeric))
            return numeric;

        var normalized = trimmed.Normalize(NormalizationForm.FormKC).ToUpperInvariant();
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return BinaryPrimitives.ReadInt64LittleEndian(digest);
    }

    public static string CreateRandomNumericText()
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        RandomNumberGenerator.Fill(bytes);
        return BinaryPrimitives.ReadInt64LittleEndian(bytes)
            .ToString(CultureInfo.InvariantCulture);
    }
}

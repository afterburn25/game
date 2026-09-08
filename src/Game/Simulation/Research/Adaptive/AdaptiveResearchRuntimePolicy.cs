using System;
using System.IO;
using System.Text.Json;

namespace Game.Simulation.Research.Adaptive;

public sealed record ResearchMaturationThresholds(
    double ExperimentalEndFraction,
    double DemonstratedEndFraction,
    double EngineeringEndFraction)
{
    public double ThresholdFor(ResearchMaturity stage) => stage switch
    {
        ResearchMaturity.Experimental => ExperimentalEndFraction,
        ResearchMaturity.Demonstrated => DemonstratedEndFraction,
        ResearchMaturity.Engineering => EngineeringEndFraction,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "No directed-work threshold exists for this maturity state."),
    };
}

/// <summary>
/// Runtime-only progression policy not already owned by another immutable companion catalog.
/// Applicability scope is owned by AdaptiveResearchApplicabilityCatalog and facility requirements
/// by AdaptiveResearchFacilityCatalog so there is one machine source of truth for each concern.
/// </summary>
public sealed class AdaptiveResearchRuntimePolicy
{
    internal AdaptiveResearchRuntimePolicy(ResearchMaturationThresholds thresholds) => Thresholds = thresholds;

    public ResearchMaturationThresholds Thresholds { get; }
}

public static class AdaptiveResearchRuntimePolicyLoader
{
    public static AdaptiveResearchRuntimePolicy LoadFromDirectory(string rootPath, AdaptiveResearchCatalog catalog)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentNullException.ThrowIfNull(catalog);
        var path = Path.Combine(Path.GetFullPath(rootPath), "maturation_model.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Required Adaptive Research runtime policy file not found: {path}", path);

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var catalogId = RequiredString(root, "catalog_id", "maturation_model.json");
        if (!string.Equals(catalogId, catalog.Metadata.CatalogId, StringComparison.Ordinal))
            throw new InvalidDataException($"maturation_model.json catalog_id '{catalogId}' does not match '{catalog.Metadata.CatalogId}'.");

        var stages = RequiredProperty(root, "directed_project_stages", "maturation_model.json");
        var experimentalEnd = RequiredDouble(RequiredProperty(stages, "experimental", "maturation_model.json"), "typical_rp_fraction_end", "maturation_model.json:experimental");
        var demonstratedEnd = RequiredDouble(RequiredProperty(stages, "demonstrated", "maturation_model.json"), "typical_rp_fraction_end", "maturation_model.json:demonstrated");
        var engineeringEnd = RequiredDouble(RequiredProperty(stages, "engineering", "maturation_model.json"), "typical_rp_fraction_end", "maturation_model.json:engineering");

        if (!(0.0 < experimentalEnd && experimentalEnd < demonstratedEnd && demonstratedEnd < engineeringEnd && engineeringEnd <= 1.0))
            throw new InvalidDataException("Adaptive Research maturation fractions must increase from Experimental through Engineering and end at or below 1.0.");

        return new AdaptiveResearchRuntimePolicy(
            new ResearchMaturationThresholds(experimentalEnd, demonstratedEnd, engineeringEnd));
    }

    private static JsonElement RequiredProperty(JsonElement element, string name, string source) =>
        element.TryGetProperty(name, out var value)
            ? value
            : throw new InvalidDataException($"Adaptive Research runtime policy error in {source}: missing '{name}'.");

    private static string RequiredString(JsonElement element, string name, string source)
    {
        var value = RequiredProperty(element, name, source);
        return value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!
            : throw new InvalidDataException($"Adaptive Research runtime policy error in {source}: '{name}' must be a non-empty string.");
    }

    private static double RequiredDouble(JsonElement element, string name, string source)
    {
        var value = RequiredProperty(element, name, source);
        return value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var result)
            ? result
            : throw new InvalidDataException($"Adaptive Research runtime policy error in {source}: '{name}' must be numeric.");
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Game.Simulation.Research.Adaptive;

public enum ResearchTraitScope
{
    Civilization,
    PopulationOrSpecies,
}

public sealed record ResearchFacilityRequirement(
    IReadOnlyList<string> AllOf,
    IReadOnlyList<string> AnyOf);

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

public sealed class AdaptiveResearchRuntimePolicy
{
    internal AdaptiveResearchRuntimePolicy(
        ResearchMaturationThresholds thresholds,
        IReadOnlyDictionary<string, ResearchTraitScope> traitScopes,
        IReadOnlySet<string> facilityCapabilityIds,
        IReadOnlyDictionary<string, IReadOnlyDictionary<ResearchMaturity, ResearchFacilityRequirement>> stageFacilityRequirements)
    {
        Thresholds = thresholds;
        TraitScopes = traitScopes;
        FacilityCapabilityIds = facilityCapabilityIds;
        StageFacilityRequirements = stageFacilityRequirements;
    }

    public ResearchMaturationThresholds Thresholds { get; }
    public IReadOnlyDictionary<string, ResearchTraitScope> TraitScopes { get; }
    public IReadOnlySet<string> FacilityCapabilityIds { get; }
    public IReadOnlyDictionary<string, IReadOnlyDictionary<ResearchMaturity, ResearchFacilityRequirement>> StageFacilityRequirements { get; }

    public ResearchTraitScope GetTraitScope(string traitId) =>
        TraitScopes.TryGetValue(traitId, out var scope)
            ? scope
            : throw new KeyNotFoundException($"Unknown Adaptive Research applicability trait '{traitId}'.");

    public ResearchFacilityRequirement GetFacilityRequirement(string nodeId, ResearchMaturity stage)
    {
        if (!StageFacilityRequirements.TryGetValue(nodeId, out var stages) ||
            !stages.TryGetValue(stage, out var requirement))
        {
            return new ResearchFacilityRequirement(Array.Empty<string>(), Array.Empty<string>());
        }

        return requirement;
    }
}

public static class AdaptiveResearchRuntimePolicyLoader
{
    public static AdaptiveResearchRuntimePolicy LoadFromDirectory(string rootPath, AdaptiveResearchCatalog catalog)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentNullException.ThrowIfNull(catalog);
        var root = Path.GetFullPath(rootPath);

        using var maturationDoc = LoadJson(root, "maturation_model.json");
        ValidateCatalogId(maturationDoc.RootElement, catalog.Metadata.CatalogId, "maturation_model.json");
        var stages = RequiredProperty(maturationDoc.RootElement, "directed_project_stages", "maturation_model.json");
        var experimentalEnd = RequiredDouble(RequiredProperty(stages, "experimental", "maturation_model.json"), "typical_rp_fraction_end", "maturation_model.json:experimental");
        var demonstratedEnd = RequiredDouble(RequiredProperty(stages, "demonstrated", "maturation_model.json"), "typical_rp_fraction_end", "maturation_model.json:demonstrated");
        var engineeringEnd = RequiredDouble(RequiredProperty(stages, "engineering", "maturation_model.json"), "typical_rp_fraction_end", "maturation_model.json:engineering");
        if (!(0.0 < experimentalEnd && experimentalEnd < demonstratedEnd && demonstratedEnd < engineeringEnd && engineeringEnd <= 1.0))
            throw Invalid("maturation_model.json", "directed project stage fractions must increase and terminate at or below 1.0");

        using var traitDoc = LoadJson(root, "applicability_traits.json");
        ValidateCatalogId(traitDoc.RootElement, catalog.Metadata.CatalogId, "applicability_traits.json");
        var traitScopes = new Dictionary<string, ResearchTraitScope>(StringComparer.Ordinal);
        foreach (var trait in RequiredProperty(traitDoc.RootElement, "traits", "applicability_traits.json").EnumerateArray())
        {
            var id = RequiredString(trait, "id", "applicability_traits.json");
            var scope = RequiredString(trait, "scope", $"applicability_traits.json:{id}") switch
            {
                "civilization" => ResearchTraitScope.Civilization,
                "population_or_species" => ResearchTraitScope.PopulationOrSpecies,
                var unknown => throw Invalid("applicability_traits.json", $"trait '{id}' uses unsupported scope '{unknown}'"),
            };
            traitScopes.Add(id, scope);
        }
        if (!catalog.TraitIds.SetEquals(traitScopes.Keys))
            throw Invalid("applicability_traits.json", "runtime trait scope set does not match the immutable catalog trait set");

        using var facilityDoc = LoadJson(root, "research_facility_model.json");
        ValidateCatalogId(facilityDoc.RootElement, catalog.Metadata.CatalogId, "research_facility_model.json");
        var facilityCapabilityIds = RequiredProperty(facilityDoc.RootElement, "facility_capabilities", "research_facility_model.json")
            .EnumerateArray()
            .Select(row => RequiredString(row, "id", "research_facility_model.json:facility_capabilities"))
            .ToHashSet(StringComparer.Ordinal);

        var requirements = new Dictionary<string, IReadOnlyDictionary<ResearchMaturity, ResearchFacilityRequirement>>(StringComparer.Ordinal);
        foreach (var nodeProperty in RequiredProperty(facilityDoc.RootElement, "stage_requirements", "research_facility_model.json").EnumerateObject())
        {
            if (!catalog.Nodes.ContainsKey(nodeProperty.Name))
                throw Invalid("research_facility_model.json", $"stage requirement references unknown node '{nodeProperty.Name}'");

            var stageRequirements = new Dictionary<ResearchMaturity, ResearchFacilityRequirement>();
            foreach (var stageProperty in nodeProperty.Value.EnumerateObject())
            {
                var maturity = ParseStage(stageProperty.Name);
                var allOf = StringArray(stageProperty.Value, "all_of");
                var anyOf = StringArray(stageProperty.Value, "any_of");
                foreach (var capability in allOf.Concat(anyOf))
                    if (!facilityCapabilityIds.Contains(capability))
                        throw Invalid("research_facility_model.json", $"node '{nodeProperty.Name}' stage '{stageProperty.Name}' references unknown facility capability '{capability}'");
                stageRequirements.Add(maturity, new ResearchFacilityRequirement(allOf, anyOf));
            }

            requirements.Add(nodeProperty.Name, new ReadOnlyDictionary<ResearchMaturity, ResearchFacilityRequirement>(stageRequirements));
        }

        return new AdaptiveResearchRuntimePolicy(
            new ResearchMaturationThresholds(experimentalEnd, demonstratedEnd, engineeringEnd),
            new ReadOnlyDictionary<string, ResearchTraitScope>(traitScopes),
            facilityCapabilityIds,
            new ReadOnlyDictionary<string, IReadOnlyDictionary<ResearchMaturity, ResearchFacilityRequirement>>(requirements));
    }

    private static ResearchMaturity ParseStage(string stage) => stage switch
    {
        "experimental" => ResearchMaturity.Experimental,
        "demonstrated" => ResearchMaturity.Demonstrated,
        "engineering" => ResearchMaturity.Engineering,
        _ => throw Invalid("research_facility_model.json", $"unsupported directed facility stage '{stage}'"),
    };

    private static JsonDocument LoadJson(string root, string fileName)
    {
        var path = Path.Combine(root, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Required Adaptive Research runtime policy file not found: {path}", path);
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static void ValidateCatalogId(JsonElement root, string expected, string source)
    {
        var actual = RequiredString(root, "catalog_id", source);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw Invalid(source, $"catalog_id '{actual}' does not match '{expected}'");
    }

    private static JsonElement RequiredProperty(JsonElement element, string name, string source) =>
        element.TryGetProperty(name, out var value)
            ? value
            : throw Invalid(source, $"missing required property '{name}'");

    private static string RequiredString(JsonElement element, string name, string source)
    {
        var value = RequiredProperty(element, name, source);
        return value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!
            : throw Invalid(source, $"'{name}' must be a non-empty string");
    }

    private static double RequiredDouble(JsonElement element, string name, string source)
    {
        var value = RequiredProperty(element, name, source);
        return value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var result)
            ? result
            : throw Invalid(source, $"'{name}' must be numeric");
    }

    private static IReadOnlyList<string> StringArray(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            return Array.Empty<string>();
        if (value.ValueKind != JsonValueKind.Array)
            throw Invalid("research_facility_model.json", $"'{name}' must be an array");
        return value.EnumerateArray()
            .Select(row => row.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(row.GetString())
                ? row.GetString()!
                : throw Invalid("research_facility_model.json", $"'{name}' contains a non-string value"))
            .ToArray();
    }

    private static InvalidDataException Invalid(string source, string message) =>
        new($"Adaptive Research runtime policy error in {source}: {message}.");
}

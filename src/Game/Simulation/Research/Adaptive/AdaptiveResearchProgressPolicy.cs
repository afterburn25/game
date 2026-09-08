using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Game.Simulation.Research.Adaptive;

public sealed record ResearchStageWorkBand(
    ResearchMaturity Stage,
    double StartFraction,
    double EndFraction)
{
    public double WorkFraction => EndFraction - StartFraction;
}

public sealed record ResearchReadinessEfficiencyBand(
    double MinimumScore,
    double MaximumScore,
    double Efficiency);

/// <summary>
/// Immutable stage-work and readiness policy loaded from canonical research data.
/// </summary>
public sealed class AdaptiveResearchProgressPolicy
{
    private AdaptiveResearchProgressPolicy(
        IReadOnlyDictionary<ResearchMaturity, ResearchStageWorkBand> stageBands,
        IReadOnlyList<ResearchReadinessEfficiencyBand> readinessBands)
    {
        StageBands = stageBands;
        ReadinessBands = readinessBands;
    }

    public IReadOnlyDictionary<ResearchMaturity, ResearchStageWorkBand> StageBands { get; }
    public IReadOnlyList<ResearchReadinessEfficiencyBand> ReadinessBands { get; }

    public ResearchStageWorkBand GetStageBand(ResearchMaturity stage) =>
        StageBands.TryGetValue(stage, out var band)
            ? band
            : throw new InvalidOperationException($"Research maturity '{stage}' is not a directed-work stage.");

    public double GetStageWork(AdaptiveResearchNodeDefinition node, ResearchMaturity stage) =>
        node.ProjectRequirements.BaseResearchPoints * GetStageBand(stage).WorkFraction;

    public double GetReadinessEfficiency(double readinessScore)
    {
        readinessScore = Math.Clamp(readinessScore, 0.0, 100.0);
        foreach (var band in ReadinessBands)
            if (readinessScore + 0.000001 >= band.MinimumScore && readinessScore <= band.MaximumScore + 0.000001)
                return band.Efficiency;
        throw new InvalidOperationException($"No readiness efficiency band covers score {readinessScore}.");
    }

    public static AdaptiveResearchProgressPolicy LoadFromDirectory(
        string rootPath,
        AdaptiveResearchCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var root = Path.GetFullPath(rootPath);

        using var maturationDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "maturation_model.json")));
        ValidateCatalogId(maturationDoc.RootElement, catalog.Metadata.CatalogId, "maturation_model.json");
        var directedStages = maturationDoc.RootElement.GetProperty("directed_project_stages");

        var stageBands = new Dictionary<ResearchMaturity, ResearchStageWorkBand>
        {
            [ResearchMaturity.Experimental] = ParseStage(directedStages, "experimental", ResearchMaturity.Experimental),
            [ResearchMaturity.Demonstrated] = ParseStage(directedStages, "demonstrated", ResearchMaturity.Demonstrated),
            [ResearchMaturity.Engineering] = ParseStage(directedStages, "engineering", ResearchMaturity.Engineering),
        };
        ValidateStageBands(stageBands.Values);

        using var readinessDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "project_readiness_model.json")));
        ValidateCatalogId(readinessDoc.RootElement, catalog.Metadata.CatalogId, "project_readiness_model.json");
        var readinessBands = readinessDoc.RootElement.GetProperty("readiness_to_progress_efficiency")
            .EnumerateArray()
            .Select(element => new ResearchReadinessEfficiencyBand(
                element.GetProperty("min").GetDouble(),
                element.GetProperty("max").GetDouble(),
                element.GetProperty("efficiency").GetDouble()))
            .OrderBy(band => band.MinimumScore)
            .ToArray();
        ValidateReadinessBands(readinessBands);

        return new AdaptiveResearchProgressPolicy(stageBands, readinessBands);
    }

    private static ResearchStageWorkBand ParseStage(JsonElement stages, string propertyName, ResearchMaturity stage)
    {
        var element = stages.GetProperty(propertyName);
        return new ResearchStageWorkBand(
            stage,
            element.GetProperty("typical_rp_fraction_start").GetDouble(),
            element.GetProperty("typical_rp_fraction_end").GetDouble());
    }

    private static void ValidateStageBands(IEnumerable<ResearchStageWorkBand> values)
    {
        var bands = values.OrderBy(value => value.StartFraction).ToArray();
        if (bands.Length != 3)
            throw new InvalidDataException("Expected exactly three directed research work bands.");
        var expectedStart = 0.0;
        foreach (var band in bands)
        {
            if (Math.Abs(band.StartFraction - expectedStart) > 0.000001 ||
                band.EndFraction <= band.StartFraction || band.EndFraction > 1.0 + 0.000001)
                throw new InvalidDataException($"Invalid or non-contiguous research stage band {band.Stage}: {band.StartFraction}..{band.EndFraction}.");
            expectedStart = band.EndFraction;
        }
        if (Math.Abs(expectedStart - 1.0) > 0.000001)
            throw new InvalidDataException("Directed research stage work bands do not cover total project work 0..1.");
    }

    private static void ValidateReadinessBands(IReadOnlyList<ResearchReadinessEfficiencyBand> bands)
    {
        if (bands.Count == 0)
            throw new InvalidDataException("No readiness-to-progress bands are configured.");
        var expectedStart = 0.0;
        foreach (var band in bands)
        {
            if (Math.Abs(band.MinimumScore - expectedStart) > 0.000001 ||
                band.MaximumScore < band.MinimumScore ||
                band.Efficiency <= 0.0 || double.IsNaN(band.Efficiency) || double.IsInfinity(band.Efficiency))
                throw new InvalidDataException($"Invalid readiness band {band.MinimumScore}..{band.MaximumScore}.");
            expectedStart = band.MaximumScore + 1.0;
        }
        if (bands[0].MinimumScore > 0.0 || bands[^1].MaximumScore < 100.0)
            throw new InvalidDataException("Readiness bands must cover scores 0..100.");
    }

    private static void ValidateCatalogId(JsonElement root, string expected, string fileName)
    {
        var actual = root.GetProperty("catalog_id").GetString();
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new InvalidDataException($"{fileName} catalog_id '{actual}' does not match '{expected}'.");
    }
}

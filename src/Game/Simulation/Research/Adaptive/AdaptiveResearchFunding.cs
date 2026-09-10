using System;

namespace Game.Simulation.Research.Adaptive;

public sealed record AdaptiveResearchFundingQuote(
    double AssignedEffectiveLabs,
    double OperatingCreditsPerDay,
    double EstimatedTotalOperatingCredits,
    double EstimatedYearsAtFullFunding,
    string Complexity);

/// <summary>
/// Initial campaign funding policy for directed research. Credits pay the operating program while
/// RP remains the independent measure of completed scientific and engineering work.
/// </summary>
public static class AdaptiveResearchFundingPolicy
{
    // On the legacy prototype scale this represents $15M per effective lab-year before
    // complexity. Advanced and Frontier programs multiply the real staffing/equipment burden.
    public const double BaseAnnualCreditsPerEffectiveLab = 1.5;

    public static AdaptiveResearchFundingQuote Quote(
        AdaptiveResearchNodeDefinition node,
        double assignedEffectiveLabs,
        AdaptiveResearchCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(catalog);
        if (!double.IsFinite(assignedEffectiveLabs) || assignedEffectiveLabs < 0.0)
            throw new ArgumentOutOfRangeException(nameof(assignedEffectiveLabs));

        var complexityMultiplier = ComplexityMultiplier(node.Complexity);
        var annualCost = assignedEffectiveLabs * BaseAnnualCreditsPerEffectiveLab * complexityMultiplier;
        var operatingPerDay = annualCost / 365.25;
        var scaledLabs = catalog.LabScaling.ScaleAssignedLabs(
            assignedEffectiveLabs,
            node.ProjectRequirements.RecommendedLabs);
        var rpPerYear = scaledLabs * catalog.Metadata.BaseRpPerEffectiveLabPerYear;
        var estimatedYears = rpPerYear <= 0.0
            ? double.PositiveInfinity
            : node.ProjectRequirements.BaseResearchPoints / rpPerYear;
        var estimatedTotal = double.IsFinite(estimatedYears)
            ? annualCost * estimatedYears
            : double.PositiveInfinity;

        return new AdaptiveResearchFundingQuote(
            assignedEffectiveLabs,
            operatingPerDay,
            estimatedTotal,
            estimatedYears,
            node.Complexity);
    }

    public static double ComplexityMultiplier(string complexity) =>
        complexity.Trim().ToLowerInvariant() switch
        {
            "foundation" => 0.75,
            "developing" => 1.25,
            "advanced" => 2.50,
            "frontier" => 5.00,
            _ => throw new InvalidOperationException(
                $"Research complexity '{complexity}' has no financial cost policy."),
        };
}

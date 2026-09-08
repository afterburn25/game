using System;

namespace Game.Simulation.Species;

/// <summary>
/// Inherited biological plasticity controlling how readily a population can adjust
/// within the species' existing biochemical architecture. It does not permit passive
/// changes of atmosphere/solvent chemistry or grant arbitrary environmental immunity.
/// </summary>
public sealed record SpeciesAdaptationProfile(
    double AcclimatizationResponsiveness,
    double DevelopmentalPlasticity,
    double MultigenerationalAdaptability,
    double MaximumNaturalPreferenceShiftFraction,
    double MaximumNaturalToleranceExpansionFraction,
    double MaximumNaturalRadiationToleranceBonus)
{
    public SpeciesAdaptationProfile Validated()
    {
        if (!double.IsFinite(AcclimatizationResponsiveness) ||
            AcclimatizationResponsiveness <= 0.0 ||
            AcclimatizationResponsiveness > 3.0)
        {
            throw new InvalidOperationException("Acclimatization responsiveness must be finite, positive, and no greater than 3.");
        }

        ValidateUnit(DevelopmentalPlasticity, nameof(DevelopmentalPlasticity));
        ValidateUnit(MultigenerationalAdaptability, nameof(MultigenerationalAdaptability));
        ValidateUnit(MaximumNaturalPreferenceShiftFraction, nameof(MaximumNaturalPreferenceShiftFraction));
        ValidateUnit(MaximumNaturalToleranceExpansionFraction, nameof(MaximumNaturalToleranceExpansionFraction));
        ValidateUnit(MaximumNaturalRadiationToleranceBonus, nameof(MaximumNaturalRadiationToleranceBonus));
        return this;
    }

    private static void ValidateUnit(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
        {
            throw new InvalidOperationException($"{name} must be between 0 and 1.");
        }
    }
}

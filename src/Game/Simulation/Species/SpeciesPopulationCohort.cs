using System;

namespace Game.Simulation.Species;

/// <summary>
/// Aggregated population state for one species/adaptation band. This is deliberately
/// not one object per individual and does not own colony growth, migration, culture,
/// employment, or politics.
/// </summary>
public sealed record SpeciesPopulationCohort(
    string SpeciesId,
    double PopulationMillions,
    PopulationAdaptationState Adaptation,
    double ResidenceYears = 0.0,
    double GenerationsInEnvironment = 0.0,
    double LocalBornFraction = 0.0)
{
    public static SpeciesPopulationCohort Founding(string speciesId, double populationMillions) =>
        new SpeciesPopulationCohort(
            speciesId,
            populationMillions,
            PopulationAdaptationState.None(speciesId),
            ResidenceYears: 0.0,
            GenerationsInEnvironment: 0.0,
            LocalBornFraction: 0.0).Validated();

    public SpeciesPopulationCohort Validated()
    {
        if (!SpeciesCatalog.TryGet(SpeciesId, out _))
        {
            throw new InvalidOperationException($"Population cohort references unknown species '{SpeciesId}'.");
        }

        if (!double.IsFinite(PopulationMillions) || PopulationMillions <= 0.0)
        {
            throw new InvalidOperationException("Population cohort size must be finite and positive.");
        }

        Adaptation.Validated();
        if (!string.Equals(SpeciesId, Adaptation.SpeciesId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Population cohort species '{SpeciesId}' cannot use adaptation state for '{Adaptation.SpeciesId}'.");
        }

        ValidateNonNegative(ResidenceYears, nameof(ResidenceYears));
        ValidateNonNegative(GenerationsInEnvironment, nameof(GenerationsInEnvironment));

        if (!double.IsFinite(LocalBornFraction) || LocalBornFraction < 0.0 || LocalBornFraction > 1.0)
        {
            throw new InvalidOperationException("Local-born fraction must be between 0 and 1.");
        }

        return this;
    }

    public SpeciesPopulationCohort WithPopulation(double populationMillions) =>
        (this with { PopulationMillions = populationMillions }).Validated();

    private static void ValidateNonNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            throw new InvalidOperationException($"{name} must be finite and non-negative.");
        }
    }
}

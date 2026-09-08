using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Simulation.Species;

public sealed record PopulationCohortReductionResult(
    IReadOnlyList<SpeciesPopulationCohort> Cohorts,
    int MergeCount,
    double MaximumMergedAdaptationDistance)
{
    public double TotalPopulationMillions => Cohorts.Sum(cohort => cohort.PopulationMillions);
}

/// <summary>
/// Prevents adaptation history from creating unbounded micro-cohorts. Cohorts are
/// reduced only within the same species; different species are never blended into a
/// synthetic average biology.
/// </summary>
public sealed class PopulationCohortReducer
{
    public const int DefaultMaxAdaptationCohortsPerSpecies = 4;
    public const int HardMaxAdaptationCohortsPerSpecies = 8;

    public PopulationCohortReductionResult Reduce(
        IEnumerable<SpeciesPopulationCohort> cohorts,
        int maxAdaptationCohortsPerSpecies = DefaultMaxAdaptationCohortsPerSpecies)
    {
        ArgumentNullException.ThrowIfNull(cohorts);
        if (maxAdaptationCohortsPerSpecies < 1 ||
            maxAdaptationCohortsPerSpecies > HardMaxAdaptationCohortsPerSpecies)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxAdaptationCohortsPerSpecies),
                $"Detailed adaptation cohorts per species must be between 1 and {HardMaxAdaptationCohortsPerSpecies}.");
        }

        var validated = cohorts.Select(cohort => cohort.Validated()).ToArray();
        var output = new List<SpeciesPopulationCohort>(validated.Length);
        var mergeCount = 0;
        var maximumMergedDistance = 0.0;

        foreach (var speciesGroup in validated
                     .GroupBy(cohort => cohort.SpeciesId, StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var species = SpeciesCatalog.Get(speciesGroup.Key);
            var working = SortCohorts(speciesGroup).ToList();

            while (working.Count > maxAdaptationCohortsPerSpecies)
            {
                var bestI = 0;
                var bestJ = 1;
                var bestDistance = AdaptationDistance(species, working[0].Adaptation, working[1].Adaptation);

                for (var i = 0; i < working.Count - 1; i++)
                {
                    for (var j = i + 1; j < working.Count; j++)
                    {
                        var distance = AdaptationDistance(species, working[i].Adaptation, working[j].Adaptation);
                        if (distance < bestDistance - 1e-12)
                        {
                            bestDistance = distance;
                            bestI = i;
                            bestJ = j;
                        }
                    }
                }

                var merged = Merge(working[bestI], working[bestJ]);
                maximumMergedDistance = Math.Max(maximumMergedDistance, bestDistance);
                mergeCount++;

                working.RemoveAt(bestJ);
                working.RemoveAt(bestI);
                working.Add(merged);
                working = SortCohorts(working).ToList();
            }

            output.AddRange(working);
        }

        return new PopulationCohortReductionResult(
            output,
            mergeCount,
            maximumMergedDistance);
    }

    public double AdaptationDistance(
        SpeciesDefinition species,
        PopulationAdaptationState left,
        PopulationAdaptationState right)
    {
        species.Validated();
        left.Validated();
        right.Validated();

        if (!string.Equals(species.Id, left.SpeciesId, StringComparison.Ordinal) ||
            !string.Equals(species.Id, right.SpeciesId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Adaptation distance can only compare states from the requested species.");
        }

        var gravityScale = Math.Max(0.01, species.Environment.GravityG.SurvivableDeviation);
        var temperatureScale = Math.Max(0.01, species.Environment.TemperatureKelvin.SurvivableDeviation);
        var pressureScale = Math.Max(0.01, species.Environment.PressureKPa.SurvivableDeviation);

        var components = new[]
        {
            (left.GravityPreferenceShiftG - right.GravityPreferenceShiftG) / gravityScale,
            (left.GravityToleranceBonusG - right.GravityToleranceBonusG) / gravityScale,
            (left.TemperaturePreferenceShiftKelvin - right.TemperaturePreferenceShiftKelvin) / temperatureScale,
            (left.TemperatureToleranceBonusKelvin - right.TemperatureToleranceBonusKelvin) / temperatureScale,
            (left.PressurePreferenceShiftKPa - right.PressurePreferenceShiftKPa) / pressureScale,
            (left.PressureToleranceBonusKPa - right.PressureToleranceBonusKPa) / pressureScale,
            left.RadiationToleranceBonus - right.RadiationToleranceBonus,
            left.Acclimatization - right.Acclimatization,
        };

        return Math.Sqrt(components.Sum(component => component * component) / components.Length);
    }

    private static SpeciesPopulationCohort Merge(
        SpeciesPopulationCohort left,
        SpeciesPopulationCohort right)
    {
        if (!string.Equals(left.SpeciesId, right.SpeciesId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Population cohorts from different species cannot be merged.");
        }

        var total = left.PopulationMillions + right.PopulationMillions;
        if (!double.IsFinite(total) || total <= 0.0)
        {
            throw new InvalidOperationException("Merged population must remain finite and positive.");
        }

        double Weighted(double a, double b) =>
            ((a * left.PopulationMillions) + (b * right.PopulationMillions)) / total;

        var adaptation = new PopulationAdaptationState(
            left.SpeciesId,
            GravityPreferenceShiftG: Weighted(left.Adaptation.GravityPreferenceShiftG, right.Adaptation.GravityPreferenceShiftG),
            GravityToleranceBonusG: Weighted(left.Adaptation.GravityToleranceBonusG, right.Adaptation.GravityToleranceBonusG),
            TemperaturePreferenceShiftKelvin: Weighted(left.Adaptation.TemperaturePreferenceShiftKelvin, right.Adaptation.TemperaturePreferenceShiftKelvin),
            TemperatureToleranceBonusKelvin: Weighted(left.Adaptation.TemperatureToleranceBonusKelvin, right.Adaptation.TemperatureToleranceBonusKelvin),
            PressurePreferenceShiftKPa: Weighted(left.Adaptation.PressurePreferenceShiftKPa, right.Adaptation.PressurePreferenceShiftKPa),
            PressureToleranceBonusKPa: Weighted(left.Adaptation.PressureToleranceBonusKPa, right.Adaptation.PressureToleranceBonusKPa),
            RadiationToleranceBonus: Weighted(left.Adaptation.RadiationToleranceBonus, right.Adaptation.RadiationToleranceBonus),
            Acclimatization: Weighted(left.Adaptation.Acclimatization, right.Adaptation.Acclimatization)).Validated();

        return new SpeciesPopulationCohort(
            left.SpeciesId,
            total,
            adaptation,
            ResidenceYears: Weighted(left.ResidenceYears, right.ResidenceYears),
            GenerationsInEnvironment: Weighted(left.GenerationsInEnvironment, right.GenerationsInEnvironment),
            LocalBornFraction: Weighted(left.LocalBornFraction, right.LocalBornFraction)).Validated();
    }

    private static IOrderedEnumerable<SpeciesPopulationCohort> SortCohorts(
        IEnumerable<SpeciesPopulationCohort> cohorts) =>
        cohorts
            .OrderBy(cohort => cohort.Adaptation.GravityPreferenceShiftG)
            .ThenBy(cohort => cohort.Adaptation.GravityToleranceBonusG)
            .ThenBy(cohort => cohort.Adaptation.TemperaturePreferenceShiftKelvin)
            .ThenBy(cohort => cohort.Adaptation.TemperatureToleranceBonusKelvin)
            .ThenBy(cohort => cohort.Adaptation.PressurePreferenceShiftKPa)
            .ThenBy(cohort => cohort.Adaptation.PressureToleranceBonusKPa)
            .ThenBy(cohort => cohort.Adaptation.RadiationToleranceBonus)
            .ThenBy(cohort => cohort.Adaptation.Acclimatization)
            .ThenBy(cohort => cohort.ResidenceYears)
            .ThenBy(cohort => cohort.GenerationsInEnvironment)
            .ThenBy(cohort => cohort.LocalBornFraction)
            .ThenBy(cohort => cohort.PopulationMillions);
}

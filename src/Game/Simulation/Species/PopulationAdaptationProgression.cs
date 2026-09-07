using System;

namespace Game.Simulation.Species;

public sealed record NaturalAdaptationSettings(
    double AcclimatizationTimeYears = 2.0,
    double PreferenceShiftTimeGenerations = 12.0,
    double ToleranceExpansionTimeGenerations = 18.0,
    double MaxPreferenceShiftFractionOfSurvivableDeviation = 0.35,
    double MaxToleranceExpansionFractionOfSurvivableDeviation = 0.20,
    double MaxRadiationToleranceBonus = 0.20,
    double MinimumNaturalHabitabilityForLongTermAdaptation = 0.15)
{
    public NaturalAdaptationSettings Validated()
    {
        ValidatePositive(AcclimatizationTimeYears, nameof(AcclimatizationTimeYears));
        ValidatePositive(PreferenceShiftTimeGenerations, nameof(PreferenceShiftTimeGenerations));
        ValidatePositive(ToleranceExpansionTimeGenerations, nameof(ToleranceExpansionTimeGenerations));
        ValidateUnit(MaxPreferenceShiftFractionOfSurvivableDeviation, nameof(MaxPreferenceShiftFractionOfSurvivableDeviation));
        ValidateUnit(MaxToleranceExpansionFractionOfSurvivableDeviation, nameof(MaxToleranceExpansionFractionOfSurvivableDeviation));
        ValidateUnit(MaxRadiationToleranceBonus, nameof(MaxRadiationToleranceBonus));
        ValidateUnit(MinimumNaturalHabitabilityForLongTermAdaptation, nameof(MinimumNaturalHabitabilityForLongTermAdaptation));
        return this;
    }

    private static void ValidatePositive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new InvalidOperationException($"{name} must be finite and positive.");
        }
    }

    private static void ValidateUnit(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
        {
            throw new InvalidOperationException($"{name} must be between 0 and 1.");
        }
    }
}

/// <summary>
/// Low-frequency deterministic adaptation progression for a population cohort.
/// It models reversible acclimatization plus slow environmental preference/tolerance
/// drift. It never changes atmosphere/solvent/immersion compatibility; those require
/// explicit biological/technological systems rather than passive residence.
/// </summary>
public sealed class PopulationAdaptationProgression
{
    private readonly SpeciesEnvironmentEvaluator _environmentEvaluator = new();

    public SpeciesPopulationCohort Advance(
        SpeciesPopulationCohort cohort,
        HabitatEnvironment habitat,
        double elapsedYears,
        NaturalAdaptationSettings? settings = null)
    {
        cohort.Validated();
        habitat.Validated();
        if (!double.IsFinite(elapsedYears) || elapsedYears <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedYears), "Elapsed years must be finite and positive.");
        }

        settings = (settings ?? new NaturalAdaptationSettings()).Validated();
        var species = SpeciesCatalog.Get(cohort.SpeciesId);
        var current = cohort.Adaptation.Validated();
        var assessment = _environmentEvaluator.Evaluate(species, habitat, current);

        var generationDelta = elapsedYears / species.BaselineGenerationYears;
        var residenceYears = cohort.ResidenceYears + elapsedYears;
        var generations = cohort.GenerationsInEnvironment + generationDelta;
        var localBornFraction = 1.0 - ((1.0 - cohort.LocalBornFraction) * Math.Exp(-generationDelta));

        var physicalMinimum = Math.Min(
            Math.Min(assessment.GravitySuitability, assessment.TemperatureSuitability),
            Math.Min(assessment.PressureSuitability, assessment.RadiationSuitability));
        var chemistryCompatible =
            assessment.AtmosphereSuitability > 0.0 &&
            assessment.SolventSuitability > 0.0 &&
            assessment.ImmersionSuitability > 0.0;

        var acclimatizationTarget = chemistryCompatible && physicalMinimum > 0.0
            ? Math.Clamp((1.0 - physicalMinimum) * 0.75, 0.0, 1.0)
            : 0.0;
        var acclimatizationRate = 1.0 - Math.Exp(-elapsedYears / settings.AcclimatizationTimeYears);
        var acclimatization = MoveToward(current.Acclimatization, acclimatizationTarget, acclimatizationRate);

        var canDevelopLongTermAdaptation =
            chemistryCompatible &&
            assessment.NaturalHabitability >= settings.MinimumNaturalHabitabilityForLongTermAdaptation;

        var gravityShift = current.GravityPreferenceShiftG;
        var gravityBonus = current.GravityToleranceBonusG;
        var temperatureShift = current.TemperaturePreferenceShiftKelvin;
        var temperatureBonus = current.TemperatureToleranceBonusKelvin;
        var pressureShift = current.PressurePreferenceShiftKPa;
        var pressureBonus = current.PressureToleranceBonusKPa;
        var radiationBonus = current.RadiationToleranceBonus;

        if (canDevelopLongTermAdaptation)
        {
            var developmentalWeight = Math.Clamp(localBornFraction, 0.0, 1.0);
            var preferenceRate =
                (1.0 - Math.Exp(-generationDelta / settings.PreferenceShiftTimeGenerations)) * developmentalWeight;
            var toleranceRate =
                (1.0 - Math.Exp(-generationDelta / settings.ToleranceExpansionTimeGenerations)) * developmentalWeight;

            gravityShift = MoveToward(
                gravityShift,
                TargetPreferenceShift(
                    habitat.GravityG,
                    species.Environment.GravityG,
                    settings.MaxPreferenceShiftFractionOfSurvivableDeviation),
                preferenceRate);
            temperatureShift = MoveToward(
                temperatureShift,
                TargetPreferenceShift(
                    habitat.TemperatureKelvin,
                    species.Environment.TemperatureKelvin,
                    settings.MaxPreferenceShiftFractionOfSurvivableDeviation),
                preferenceRate);
            pressureShift = MoveToward(
                pressureShift,
                TargetPreferenceShift(
                    habitat.PressureKPa,
                    species.Environment.PressureKPa,
                    settings.MaxPreferenceShiftFractionOfSurvivableDeviation),
                preferenceRate);

            gravityBonus = IncreaseToward(
                gravityBonus,
                TargetToleranceBonus(
                    habitat.GravityG,
                    species.Environment.GravityG,
                    settings.MaxToleranceExpansionFractionOfSurvivableDeviation),
                toleranceRate);
            temperatureBonus = IncreaseToward(
                temperatureBonus,
                TargetToleranceBonus(
                    habitat.TemperatureKelvin,
                    species.Environment.TemperatureKelvin,
                    settings.MaxToleranceExpansionFractionOfSurvivableDeviation),
                toleranceRate);
            pressureBonus = IncreaseToward(
                pressureBonus,
                TargetToleranceBonus(
                    habitat.PressureKPa,
                    species.Environment.PressureKPa,
                    settings.MaxToleranceExpansionFractionOfSurvivableDeviation),
                toleranceRate);

            var radiationTarget = Math.Clamp(
                habitat.RadiationHazard - species.Physiology.RadiationTolerance,
                0.0,
                settings.MaxRadiationToleranceBonus);
            radiationBonus = IncreaseToward(radiationBonus, radiationTarget, toleranceRate);
        }

        var adaptation = new PopulationAdaptationState(
            cohort.SpeciesId,
            GravityPreferenceShiftG: gravityShift,
            GravityToleranceBonusG: gravityBonus,
            TemperaturePreferenceShiftKelvin: temperatureShift,
            TemperatureToleranceBonusKelvin: temperatureBonus,
            PressurePreferenceShiftKPa: pressureShift,
            PressureToleranceBonusKPa: pressureBonus,
            RadiationToleranceBonus: radiationBonus,
            Acclimatization: acclimatization).Validated();

        return cohort with
        {
            Adaptation = adaptation,
            ResidenceYears = residenceYears,
            GenerationsInEnvironment = generations,
            LocalBornFraction = localBornFraction,
        }.Validated();
    }

    private static double TargetPreferenceShift(
        double environmentalValue,
        ToleranceBand baseline,
        double maximumShiftFraction)
    {
        var maximumShift = baseline.SurvivableDeviation * maximumShiftFraction;
        return Math.Clamp(environmentalValue - baseline.Preferred, -maximumShift, maximumShift);
    }

    private static double TargetToleranceBonus(
        double environmentalValue,
        ToleranceBand baseline,
        double maximumExpansionFraction)
    {
        var deviation = Math.Abs(environmentalValue - baseline.Preferred);
        var desiredBeyondComfort = Math.Max(0.0, deviation - baseline.ComfortableDeviation);
        return Math.Min(
            desiredBeyondComfort,
            baseline.SurvivableDeviation * maximumExpansionFraction);
    }

    private static double MoveToward(double current, double target, double fraction)
    {
        return current + ((target - current) * Math.Clamp(fraction, 0.0, 1.0));
    }

    private static double IncreaseToward(double current, double target, double fraction)
    {
        if (target <= current)
        {
            return current;
        }

        return MoveToward(current, target, fraction);
    }
}

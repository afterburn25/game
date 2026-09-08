using System;
using System.Collections.Generic;

namespace Game.Simulation.Species;

public enum EnvironmentalLimitingFactor
{
    None,
    Gravity,
    Temperature,
    Pressure,
    Atmosphere,
    Solvent,
    Immersion,
    Radiation,
}

public sealed record SpeciesEnvironmentAssessment(
    double NaturalHabitability,
    double UnprotectedOperationalCapacity,
    double GravitySuitability,
    double TemperatureSuitability,
    double PressureSuitability,
    double AtmosphereSuitability,
    double SolventSuitability,
    double ImmersionSuitability,
    double RadiationSuitability,
    EnvironmentalLimitingFactor LimitingFactor,
    bool RequiresGravityMitigation,
    bool RequiresThermalControl,
    bool RequiresPressureControl,
    bool RequiresSealedHabitat,
    bool RequiresArtificialBiosphere,
    bool RequiresRadiationShielding)
{
    public double HealthStress => 1.0 - NaturalHabitability;
}

public sealed class SpeciesEnvironmentEvaluator
{
    private const double SupportThreshold = 0.95;

    public SpeciesEnvironmentAssessment Evaluate(
        SpeciesDefinition species,
        HabitatEnvironment habitat,
        PopulationAdaptationState? adaptation = null)
    {
        species.Validated();
        habitat.Validated();
        adaptation ??= PopulationAdaptationState.None(species.Id);
        adaptation.Validated();

        if (!string.Equals(species.Id, adaptation.SpeciesId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Adaptation state for species '{adaptation.SpeciesId}' cannot be applied to '{species.Id}'.");
        }

        var gravityBand = ShiftAndWiden(
            species.Environment.GravityG,
            adaptation.GravityPreferenceShiftG,
            adaptation.GravityToleranceBonusG);
        var temperatureBand = ShiftAndWiden(
            species.Environment.TemperatureKelvin,
            adaptation.TemperaturePreferenceShiftKelvin,
            adaptation.TemperatureToleranceBonusKelvin);
        var pressureBand = ShiftAndWiden(
            species.Environment.PressureKPa,
            adaptation.PressurePreferenceShiftKPa,
            adaptation.PressureToleranceBonusKPa);

        var gravity = ApplyAcclimatization(gravityBand.Evaluate(habitat.GravityG), adaptation.Acclimatization);
        var temperature = ApplyAcclimatization(temperatureBand.Evaluate(habitat.TemperatureKelvin), adaptation.Acclimatization);
        var pressure = ApplyAcclimatization(pressureBand.Evaluate(habitat.PressureKPa), adaptation.Acclimatization);

        var atmosphere = EvaluateAtmosphere(species, habitat);
        var solvent = EvaluateSolvent(species, habitat);
        var immersion = !species.Environment.RequiresImmersion || habitat.IsImmersed ? 1.0 : 0.0;
        var radiation = EvaluateRadiation(species, habitat, adaptation);

        var factors = new (EnvironmentalLimitingFactor Factor, double Score)[]
        {
            (EnvironmentalLimitingFactor.Gravity, gravity),
            (EnvironmentalLimitingFactor.Temperature, temperature),
            (EnvironmentalLimitingFactor.Pressure, pressure),
            (EnvironmentalLimitingFactor.Atmosphere, atmosphere),
            (EnvironmentalLimitingFactor.Solvent, solvent),
            (EnvironmentalLimitingFactor.Immersion, immersion),
            (EnvironmentalLimitingFactor.Radiation, radiation),
        };

        var limitingFactor = EnvironmentalLimitingFactor.None;
        var naturalHabitability = 1.0;
        foreach (var factor in factors)
        {
            if (factor.Score < naturalHabitability)
            {
                naturalHabitability = factor.Score;
                limitingFactor = factor.Factor;
            }
        }

        var unprotectedOperationalCapacity = GeometricMean(
            gravity,
            temperature,
            pressure,
            atmosphere,
            solvent,
            immersion,
            radiation);

        return new SpeciesEnvironmentAssessment(
            naturalHabitability,
            unprotectedOperationalCapacity,
            gravity,
            temperature,
            pressure,
            atmosphere,
            solvent,
            immersion,
            radiation,
            limitingFactor,
            gravity < SupportThreshold,
            temperature < SupportThreshold,
            pressure < SupportThreshold,
            atmosphere < SupportThreshold,
            solvent < SupportThreshold || immersion < SupportThreshold,
            radiation < SupportThreshold);
    }

    private static ToleranceBand ShiftAndWiden(ToleranceBand band, double shift, double toleranceBonus)
    {
        return new ToleranceBand(
            band.Preferred + shift,
            band.ComfortableDeviation + toleranceBonus,
            band.SurvivableDeviation + toleranceBonus);
    }

    private static double ApplyAcclimatization(double score, double acclimatization)
    {
        if (score <= 0.0 || score >= 1.0 || acclimatization <= 0.0)
        {
            return score;
        }

        return Math.Clamp(score + ((1.0 - score) * acclimatization * 0.20), 0.0, 1.0);
    }

    private static double EvaluateAtmosphere(SpeciesDefinition species, HabitatEnvironment habitat)
    {
        if (species.Biochemistry == BiochemicalBasis.Synthetic)
        {
            return 1.0;
        }

        if (habitat.Atmosphere == AtmosphereClass.Vacuum)
        {
            return species.Environment.CanOperateInVacuumUnprotected ? 1.0 : 0.0;
        }

        return species.BreathableAtmospheres.Contains(habitat.Atmosphere) ? 1.0 : 0.0;
    }

    private static double EvaluateSolvent(SpeciesDefinition species, HabitatEnvironment habitat)
    {
        if (species.Biochemistry == BiochemicalBasis.Synthetic)
        {
            return 1.0;
        }

        return species.CompatibleSolvents.Contains(habitat.AvailableSolvent) ? 1.0 : 0.0;
    }

    private static double EvaluateRadiation(
        SpeciesDefinition species,
        HabitatEnvironment habitat,
        PopulationAdaptationState adaptation)
    {
        var tolerance = Math.Clamp(
            species.Physiology.RadiationTolerance + adaptation.RadiationToleranceBonus,
            0.0,
            1.0);

        if (habitat.RadiationHazard <= tolerance)
        {
            return 1.0;
        }

        if (tolerance >= 1.0)
        {
            return 1.0;
        }

        var score = 1.0 - ((habitat.RadiationHazard - tolerance) / (1.0 - tolerance));
        return ApplyAcclimatization(Math.Clamp(score, 0.0, 1.0), adaptation.Acclimatization);
    }

    private static double GeometricMean(params double[] values)
    {
        var product = 1.0;
        foreach (var value in values)
        {
            product *= Math.Clamp(value, 0.0, 1.0);
        }

        return Math.Pow(product, 1.0 / values.Length);
    }
}

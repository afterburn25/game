using System;

namespace Game.Simulation.Species;

public sealed record PopulationAdaptationState(
    string SpeciesId,
    double GravityPreferenceShiftG = 0.0,
    double GravityToleranceBonusG = 0.0,
    double TemperaturePreferenceShiftKelvin = 0.0,
    double TemperatureToleranceBonusKelvin = 0.0,
    double PressurePreferenceShiftKPa = 0.0,
    double PressureToleranceBonusKPa = 0.0,
    double RadiationToleranceBonus = 0.0,
    double Acclimatization = 0.0)
{
    public static PopulationAdaptationState None(string speciesId) => new(speciesId);

    public PopulationAdaptationState Validated()
    {
        if (string.IsNullOrWhiteSpace(SpeciesId))
        {
            throw new InvalidOperationException("Population adaptation state must reference a species ID.");
        }

        ValidateFinite(GravityPreferenceShiftG, nameof(GravityPreferenceShiftG));
        ValidateNonNegative(GravityToleranceBonusG, nameof(GravityToleranceBonusG));
        ValidateFinite(TemperaturePreferenceShiftKelvin, nameof(TemperaturePreferenceShiftKelvin));
        ValidateNonNegative(TemperatureToleranceBonusKelvin, nameof(TemperatureToleranceBonusKelvin));
        ValidateFinite(PressurePreferenceShiftKPa, nameof(PressurePreferenceShiftKPa));
        ValidateNonNegative(PressureToleranceBonusKPa, nameof(PressureToleranceBonusKPa));
        ValidateNonNegative(RadiationToleranceBonus, nameof(RadiationToleranceBonus));

        if (!double.IsFinite(Acclimatization) || Acclimatization < 0.0 || Acclimatization > 1.0)
        {
            throw new InvalidOperationException("Acclimatization must be between 0 and 1.");
        }

        return this;
    }

    private static void ValidateFinite(double value, string name)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException($"{name} must be finite.");
        }
    }

    private static void ValidateNonNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            throw new InvalidOperationException($"{name} must be finite and non-negative.");
        }
    }
}

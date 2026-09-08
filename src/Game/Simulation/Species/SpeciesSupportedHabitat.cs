using System;

namespace Game.Simulation.Species;

/// <summary>
/// Functional environmental support available at one habitat/vehicle. These are not
/// technology IDs: research, construction and logistics determine whether a location
/// actually possesses these capabilities and what they cost to operate.
/// </summary>
public sealed record HabitatSupportCapabilities(
    double MaximumGravityCorrectionG = 0.0,
    double MaximumThermalCorrectionKelvin = 0.0,
    double MaximumPressureCorrectionKPa = 0.0,
    bool CanProvideSealedAtmosphere = false,
    bool CanProvideCompatibleSolventBiosphere = false,
    bool CanProvideImmersion = false,
    double RadiationHazardReduction = 0.0)
{
    public static HabitatSupportCapabilities None => new();

    public HabitatSupportCapabilities Validated()
    {
        ValidateNonNegative(MaximumGravityCorrectionG, nameof(MaximumGravityCorrectionG));
        ValidateNonNegative(MaximumThermalCorrectionKelvin, nameof(MaximumThermalCorrectionKelvin));
        ValidateNonNegative(MaximumPressureCorrectionKPa, nameof(MaximumPressureCorrectionKPa));

        if (!double.IsFinite(RadiationHazardReduction) || RadiationHazardReduction < 0.0 || RadiationHazardReduction > 1.0)
        {
            throw new InvalidOperationException("Radiation hazard reduction must be between 0 and 1.");
        }

        return this;
    }

    private static void ValidateNonNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            throw new InvalidOperationException($"{name} must be finite and non-negative.");
        }
    }
}

public sealed record SpeciesHabitatSupportAssessment(
    string SpeciesId,
    HabitatEnvironment ExternalEnvironment,
    HabitatEnvironment SupportedEnvironment,
    SpeciesEnvironmentAssessment NaturalAssessment,
    SpeciesEnvironmentAssessment SupportedAssessment,
    double GravityCorrectionUsedG,
    double ThermalCorrectionUsedKelvin,
    double PressureCorrectionUsedKPa,
    bool SealedAtmosphereUsed,
    bool ArtificialBiosphereUsed,
    bool ImmersionSupportUsed,
    double RadiationHazardReductionUsed)
{
    public bool FullySupported => SupportedAssessment.NaturalHabitability >= 0.999999;
    public bool SupportImprovesHabitability =>
        SupportedAssessment.NaturalHabitability > NaturalAssessment.NaturalHabitability + 1e-12;
}

/// <summary>
/// Resolves an external environment plus explicit support into the physical conditions
/// actually experienced by a population. Natural habitability is retained separately;
/// life support never rewrites what the species naturally tolerates.
/// </summary>
public sealed class SpeciesSupportedHabitatEvaluator
{
    private readonly SpeciesEnvironmentEvaluator _environmentEvaluator = new();

    public SpeciesHabitatSupportAssessment Evaluate(
        SpeciesPopulationCohort cohort,
        HabitatEnvironment externalEnvironment,
        HabitatSupportCapabilities support)
    {
        cohort.Validated();
        externalEnvironment.Validated();
        support.Validated();

        var species = SpeciesCatalog.Get(cohort.SpeciesId);
        var natural = _environmentEvaluator.Evaluate(species, externalEnvironment, cohort.Adaptation);

        var gravity = MoveToward(
            externalEnvironment.GravityG,
            species.Environment.GravityG.Preferred + cohort.Adaptation.GravityPreferenceShiftG,
            support.MaximumGravityCorrectionG,
            out var gravityUsed);
        var temperature = MoveToward(
            externalEnvironment.TemperatureKelvin,
            species.Environment.TemperatureKelvin.Preferred + cohort.Adaptation.TemperaturePreferenceShiftKelvin,
            support.MaximumThermalCorrectionKelvin,
            out var thermalUsed);
        var pressure = MoveToward(
            externalEnvironment.PressureKPa,
            species.Environment.PressureKPa.Preferred + cohort.Adaptation.PressurePreferenceShiftKPa,
            support.MaximumPressureCorrectionKPa,
            out var pressureUsed);

        var sealedAtmosphereUsed = support.CanProvideSealedAtmosphere && natural.AtmosphereSuitability <= 0.0;
        var atmosphere = sealedAtmosphereUsed
            ? species.Environment.PreferredAtmosphere
            : externalEnvironment.Atmosphere;

        var artificialBiosphereUsed =
            support.CanProvideCompatibleSolventBiosphere && natural.SolventSuitability <= 0.0;
        var solvent = artificialBiosphereUsed
            ? species.Environment.BiologicalSolvent
            : externalEnvironment.AvailableSolvent;

        var immersionSupportUsed =
            species.Environment.RequiresImmersion &&
            !externalEnvironment.IsImmersed &&
            support.CanProvideImmersion;
        var immersed = externalEnvironment.IsImmersed || immersionSupportUsed;

        var radiationReductionUsed = Math.Min(
            externalEnvironment.RadiationHazard,
            support.RadiationHazardReduction);
        var radiation = Math.Max(0.0, externalEnvironment.RadiationHazard - radiationReductionUsed);

        var supportedEnvironment = new HabitatEnvironment(
            gravity,
            temperature,
            pressure,
            atmosphere,
            solvent,
            radiation,
            immersed).Validated();
        var supported = _environmentEvaluator.Evaluate(species, supportedEnvironment, cohort.Adaptation);

        return new SpeciesHabitatSupportAssessment(
            species.Id,
            externalEnvironment,
            supportedEnvironment,
            natural,
            supported,
            gravityUsed,
            thermalUsed,
            pressureUsed,
            sealedAtmosphereUsed,
            artificialBiosphereUsed,
            immersionSupportUsed,
            radiationReductionUsed);
    }

    private static double MoveToward(
        double current,
        double target,
        double maximumCorrection,
        out double correctionUsed)
    {
        var difference = target - current;
        var correction = Math.Clamp(difference, -maximumCorrection, maximumCorrection);
        correctionUsed = Math.Abs(correction);
        return current + correction;
    }
}

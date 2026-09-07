using System;
using System.Collections.Generic;

namespace Game.Simulation.Species;

public enum BiochemicalBasis
{
    CarbonWater,
    CarbonAmmonia,
    CarbonHydrocarbon,
    SiliconChemistry,
    Synthetic,
}

public enum HabitatMode
{
    TerrestrialSurface,
    Aquatic,
    Amphibious,
    Subsurface,
    Synthetic,
}

public enum AtmosphereClass
{
    OxygenNitrogen,
    OxygenRich,
    CarbonDioxideRich,
    Reducing,
    Inert,
    Vacuum,
    Other,
}

public enum SolventClass
{
    Water,
    Ammonia,
    Hydrocarbon,
    Other,
    None,
}

public readonly record struct ToleranceBand(
    double Preferred,
    double ComfortableDeviation,
    double SurvivableDeviation)
{
    public double Evaluate(double value)
    {
        var deviation = Math.Abs(value - Preferred);
        if (deviation <= ComfortableDeviation)
        {
            return 1.0;
        }

        if (deviation >= SurvivableDeviation)
        {
            return 0.0;
        }

        var span = SurvivableDeviation - ComfortableDeviation;
        return 1.0 - ((deviation - ComfortableDeviation) / span);
    }

    public void Validate(string name)
    {
        if (!double.IsFinite(Preferred))
        {
            throw new InvalidOperationException($"{name} preferred value must be finite.");
        }

        if (!double.IsFinite(ComfortableDeviation) || ComfortableDeviation < 0.0)
        {
            throw new InvalidOperationException($"{name} comfortable deviation must be finite and non-negative.");
        }

        if (!double.IsFinite(SurvivableDeviation) || SurvivableDeviation <= ComfortableDeviation)
        {
            throw new InvalidOperationException($"{name} survivable deviation must be finite and greater than the comfortable deviation.");
        }
    }
}

public sealed record SpeciesPhysiology(
    double TypicalAdultMassKg,
    double BaselineLifespanYears,
    double MaturityAgeYears,
    double BaselineMetabolicDemand,
    double RadiationTolerance,
    double MusculoskeletalRobustness)
{
    public void Validate()
    {
        if (!double.IsFinite(TypicalAdultMassKg) || TypicalAdultMassKg <= 0.0)
        {
            throw new InvalidOperationException("Typical adult mass must be finite and positive.");
        }

        if (!double.IsFinite(BaselineLifespanYears) || BaselineLifespanYears <= 0.0)
        {
            throw new InvalidOperationException("Baseline lifespan must be finite and positive.");
        }

        if (!double.IsFinite(MaturityAgeYears) || MaturityAgeYears <= 0.0 || MaturityAgeYears >= BaselineLifespanYears)
        {
            throw new InvalidOperationException("Maturity age must be finite, positive, and less than baseline lifespan.");
        }

        if (!double.IsFinite(BaselineMetabolicDemand) || BaselineMetabolicDemand <= 0.0)
        {
            throw new InvalidOperationException("Baseline metabolic demand must be finite and positive.");
        }

        ValidateUnitInterval(RadiationTolerance, nameof(RadiationTolerance));
        ValidateUnitInterval(MusculoskeletalRobustness, nameof(MusculoskeletalRobustness));
    }

    private static void ValidateUnitInterval(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
        {
            throw new InvalidOperationException($"{name} must be between 0 and 1.");
        }
    }
}

public sealed record SpeciesEnvironmentalPreferences(
    ToleranceBand GravityG,
    ToleranceBand TemperatureKelvin,
    ToleranceBand PressureKPa,
    AtmosphereClass PreferredAtmosphere,
    SolventClass BiologicalSolvent,
    bool RequiresImmersion = false,
    bool CanOperateInVacuumUnprotected = false)
{
    public void Validate()
    {
        GravityG.Validate(nameof(GravityG));
        TemperatureKelvin.Validate(nameof(TemperatureKelvin));
        PressureKPa.Validate(nameof(PressureKPa));

        if (GravityG.Preferred < 0.0)
        {
            throw new InvalidOperationException("Preferred gravity cannot be negative.");
        }

        if (TemperatureKelvin.Preferred <= 0.0)
        {
            throw new InvalidOperationException("Preferred temperature must be above absolute zero.");
        }

        if (PressureKPa.Preferred < 0.0)
        {
            throw new InvalidOperationException("Preferred pressure cannot be negative.");
        }
    }
}

public sealed record SpeciesDefinition(
    string Id,
    string DisplayName,
    BiochemicalBasis Biochemistry,
    HabitatMode HabitatMode,
    SpeciesPhysiology Physiology,
    SpeciesEnvironmentalPreferences Environment,
    IReadOnlySet<AtmosphereClass> BreathableAtmospheres,
    IReadOnlySet<SolventClass> CompatibleSolvents,
    SpeciesLifeHistory LifeHistory)
{
    public double BaselineGenerationYears => LifeHistory.BaselineGenerationYears;

    public SpeciesDefinition Validated()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("Species ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            throw new InvalidOperationException($"Species '{Id}' must have a display name.");
        }

        Physiology.Validate();
        Environment.Validate();
        LifeHistory.Validated(Physiology.BaselineLifespanYears);

        if (Math.Abs(LifeHistory.ReproductiveMaturityYears - Physiology.MaturityAgeYears) > 0.000001)
        {
            throw new InvalidOperationException(
                $"Species '{Id}' physiology maturity age and life-history reproductive maturity must agree in the current model.");
        }

        if (BreathableAtmospheres.Count == 0 && Biochemistry != BiochemicalBasis.Synthetic)
        {
            throw new InvalidOperationException($"Biological species '{Id}' must define at least one breathable atmosphere.");
        }

        if (CompatibleSolvents.Count == 0 && Biochemistry != BiochemicalBasis.Synthetic)
        {
            throw new InvalidOperationException($"Biological species '{Id}' must define at least one compatible solvent.");
        }

        return this;
    }
}

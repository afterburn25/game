using System;

namespace Game.Simulation.Species;

public enum ThermoregulationStrategy
{
    InternalEndothermy,
    EnvironmentalCoupled,
    RegionalEndothermy,
    Mixed,
    Synthetic,
}

public enum DormancyMode
{
    None,
    Torpor,
    DeepDormancy,
    Cryptobiosis,
    SyntheticStandby,
}

public sealed record SpeciesDormancyProfile(
    DormancyMode Mode,
    double MetabolicDemandFraction,
    double MaximumContinuousDays,
    double TypicalRecoveryDays)
{
    public SpeciesDormancyProfile Validated(bool synthetic)
    {
        if (Mode == DormancyMode.None)
        {
            if (Math.Abs(MetabolicDemandFraction - 1.0) > 1e-9 ||
                MaximumContinuousDays != 0.0 ||
                TypicalRecoveryDays != 0.0)
            {
                throw new InvalidOperationException("Species without natural dormancy must use neutral dormancy values.");
            }

            return this;
        }

        if (Mode == DormancyMode.SyntheticStandby && !synthetic)
        {
            throw new InvalidOperationException("Synthetic standby is only valid for synthetic species.");
        }

        if (!synthetic && Mode == DormancyMode.SyntheticStandby)
        {
            throw new InvalidOperationException("Biological species cannot use synthetic standby.");
        }

        if (!double.IsFinite(MetabolicDemandFraction) ||
            MetabolicDemandFraction <= 0.0 ||
            MetabolicDemandFraction >= 1.0)
        {
            throw new InvalidOperationException("Dormancy metabolic fraction must be finite, positive, and below normal baseline demand.");
        }

        ValidatePositive(MaximumContinuousDays, nameof(MaximumContinuousDays));
        ValidateNonNegative(TypicalRecoveryDays, nameof(TypicalRecoveryDays));
        return this;
    }

    private static void ValidatePositive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new InvalidOperationException($"{name} must be finite and positive.");
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

/// <summary>
/// Biological energy-use envelope. BaselineMetabolicDemand in SpeciesPhysiology remains
/// the common reference unit; these values describe how that demand changes with rest,
/// activity and natural dormancy. They are logistics inputs, not productivity bonuses.
/// </summary>
public sealed record SpeciesMetabolicProfile(
    ThermoregulationStrategy Thermoregulation,
    double RestingMetabolicFraction,
    double PeakActivityMetabolicMultiplier,
    double TypicalRestFractionOfDay,
    SpeciesDormancyProfile Dormancy)
{
    public SpeciesMetabolicProfile Validated(bool synthetic)
    {
        if (Thermoregulation == ThermoregulationStrategy.Synthetic && !synthetic)
        {
            throw new InvalidOperationException("Biological species cannot use synthetic thermoregulation.");
        }

        if (!double.IsFinite(RestingMetabolicFraction) ||
            RestingMetabolicFraction <= 0.0 ||
            RestingMetabolicFraction > 1.0)
        {
            throw new InvalidOperationException("Resting metabolic fraction must be finite, positive, and no greater than 1.");
        }

        if (!double.IsFinite(PeakActivityMetabolicMultiplier) || PeakActivityMetabolicMultiplier < 1.0)
        {
            throw new InvalidOperationException("Peak activity metabolic multiplier must be finite and at least 1.");
        }

        if (!double.IsFinite(TypicalRestFractionOfDay) ||
            TypicalRestFractionOfDay < 0.0 ||
            TypicalRestFractionOfDay >= 1.0)
        {
            throw new InvalidOperationException("Typical rest fraction of day must be between 0 inclusive and 1 exclusive.");
        }

        Dormancy.Validated(synthetic);
        return this;
    }
}

public sealed record SpeciesMetabolicEnvelope(
    string SpeciesId,
    double PopulationMillions,
    double BaselineDemandMillions,
    double RestingDemandMillions,
    double PeakActivityDemandMillions,
    double TypicalDayAverageDemandMillions,
    DormancyMode NaturalDormancyMode,
    double DormantDemandMillions,
    double MaximumNaturalDormancyDays,
    double TypicalDormancyRecoveryDays)
{
    public bool HasNaturalDormancy => NaturalDormancyMode != DormancyMode.None;
}

public static class SpeciesMetabolicEnvelopeEvaluator
{
    public static SpeciesMetabolicEnvelope Evaluate(SpeciesPopulationCohort cohort)
    {
        cohort.Validated();
        var species = SpeciesCatalog.Get(cohort.SpeciesId);
        var metabolic = species.Metabolism.Validated(species.Biochemistry == BiochemicalBasis.Synthetic);
        var baseline = cohort.PopulationMillions * species.Physiology.BaselineMetabolicDemand;
        var resting = baseline * metabolic.RestingMetabolicFraction;
        var peak = baseline * metabolic.PeakActivityMetabolicMultiplier;
        var typicalDay =
            (resting * metabolic.TypicalRestFractionOfDay) +
            (baseline * (1.0 - metabolic.TypicalRestFractionOfDay));
        var dormant = metabolic.Dormancy.Mode == DormancyMode.None
            ? baseline
            : baseline * metabolic.Dormancy.MetabolicDemandFraction;

        return new SpeciesMetabolicEnvelope(
            species.Id,
            cohort.PopulationMillions,
            baseline,
            resting,
            peak,
            typicalDay,
            metabolic.Dormancy.Mode,
            dormant,
            metabolic.Dormancy.MaximumContinuousDays,
            metabolic.Dormancy.TypicalRecoveryDays);
    }
}

using System;

namespace Game.Simulation.Species;

public enum ReproductiveMode
{
    InternalGestation,
    ExternalEggOrEmbryo,
    BroadcastOrNestSpawning,
    BuddingOrClonal,
    FabricatedSynthetic,
    Other,
}

/// <summary>
/// Biological life-history facts. These describe physical demographic constraints,
/// not a civilization's chosen family policy, culture, migration policy, healthcare,
/// or actual population growth rate.
/// </summary>
public sealed record SpeciesLifeHistory(
    ReproductiveMode ReproductiveMode,
    double ReproductiveMaturityYears,
    double TypicalOffspringPerEvent,
    double MinimumInterEventYears,
    double DependentDevelopmentYears,
    double ReproductiveSpanYears,
    double BaselineGenerationYears)
{
    public SpeciesLifeHistory Validated(double baselineLifespanYears)
    {
        ValidatePositive(ReproductiveMaturityYears, nameof(ReproductiveMaturityYears));
        ValidatePositive(TypicalOffspringPerEvent, nameof(TypicalOffspringPerEvent));
        ValidatePositive(MinimumInterEventYears, nameof(MinimumInterEventYears));
        ValidateNonNegative(DependentDevelopmentYears, nameof(DependentDevelopmentYears));
        ValidatePositive(ReproductiveSpanYears, nameof(ReproductiveSpanYears));
        ValidatePositive(BaselineGenerationYears, nameof(BaselineGenerationYears));

        if (ReproductiveMaturityYears >= baselineLifespanYears)
        {
            throw new InvalidOperationException("Reproductive maturity must occur before baseline lifespan.");
        }

        if (ReproductiveMaturityYears + ReproductiveSpanYears > baselineLifespanYears * 1.10)
        {
            throw new InvalidOperationException("Reproductive span cannot substantially exceed the species baseline lifespan.");
        }

        if (BaselineGenerationYears < ReproductiveMaturityYears)
        {
            throw new InvalidOperationException("Baseline generation length cannot be shorter than reproductive maturity age.");
        }

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

public sealed record SpeciesDemographicEnvelope(
    string SpeciesId,
    double ReproductiveEventsPerIndividualLifetimeUpperBound,
    double BiologicalOffspringPerIndividualLifetimeUpperBound,
    double GenerationsPerCentury,
    double MaturityFractionOfLifespan,
    double DependencyFractionOfGeneration)
{
    public double ReplacementPressureIndex => 2.0 / Math.Max(0.000001, BiologicalOffspringPerIndividualLifetimeUpperBound);
}

/// <summary>
/// Produces biological upper envelopes for demographic systems. Actual growth remains
/// owned by population/colony simulation and must account for sex/reproductive roles,
/// mortality, health, policy, resources, housing, migration, social choices, and
/// environmental conditions.
/// </summary>
public static class SpeciesDemographicEnvelopeEvaluator
{
    public static SpeciesDemographicEnvelope Evaluate(SpeciesDefinition species)
    {
        species.Validated();
        var life = species.LifeHistory;
        var events = Math.Max(1.0, life.ReproductiveSpanYears / life.MinimumInterEventYears);
        var offspring = events * life.TypicalOffspringPerEvent;
        var generationsPerCentury = 100.0 / life.BaselineGenerationYears;
        var maturityFraction = life.ReproductiveMaturityYears / species.Physiology.BaselineLifespanYears;
        var dependencyFraction = life.DependentDevelopmentYears / life.BaselineGenerationYears;

        return new SpeciesDemographicEnvelope(
            species.Id,
            events,
            offspring,
            generationsPerCentury,
            maturityFraction,
            dependencyFraction);
    }
}

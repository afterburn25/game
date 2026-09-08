using System;

namespace Game.Simulation.Species;

/// <summary>
/// Dimensionless biological population-turnover inputs derived from authored life-history facts.
/// These are not a civilization policy, fertility order, healthcare model, mortality model, or
/// final population-growth rate. Economy/Population remains authoritative for the final outcome.
/// </summary>
public sealed record SpeciesDemographicPressure(
    string SpeciesId,
    double GenerationPaceFactor,
    double ReproductiveEventThroughputFactor,
    double MaturityPaceFactor,
    double IntrinsicGrowthPaceFactor)
{
    public SpeciesDemographicPressure Validated()
    {
        if (string.IsNullOrWhiteSpace(SpeciesId))
            throw new InvalidOperationException("A demographic pressure profile requires a species ID.");
        ValidatePositive(GenerationPaceFactor, nameof(GenerationPaceFactor));
        ValidatePositive(ReproductiveEventThroughputFactor, nameof(ReproductiveEventThroughputFactor));
        ValidatePositive(MaturityPaceFactor, nameof(MaturityPaceFactor));
        ValidatePositive(IntrinsicGrowthPaceFactor, nameof(IntrinsicGrowthPaceFactor));
        return this;
    }

    private static void ValidatePositive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0.0)
            throw new InvalidOperationException($"{name} must be finite and positive.");
    }
}

/// <summary>
/// Converts physical life-history timing into a bounded relative demographic pace. Terran
/// baseline is the normalization reference so the existing early-release growth constant keeps
/// its meaning for Terrans. Other species are not assigned named bonuses or penalties: their
/// factors emerge from generation length, reproductive-event throughput, and maturity timing.
///
/// The geometric mean prevents one authored dimension from overwhelming all others. The broad
/// clamp is only a numerical/gameplay safety rail for future catalog entries; current proving
/// species fall naturally inside it.
/// </summary>
public static class SpeciesDemographicPressureEvaluator
{
    public const double MinimumIntrinsicGrowthPace = 0.20;
    public const double MaximumIntrinsicGrowthPace = 1.80;

    public static SpeciesDemographicPressure Evaluate(SpeciesDefinition species)
    {
        ArgumentNullException.ThrowIfNull(species);
        species.Validated();

        var reference = SpeciesCatalog.Get(SpeciesCatalog.TerranBaselineId);
        var life = species.LifeHistory;
        var referenceLife = reference.LifeHistory;

        var generationPace = referenceLife.BaselineGenerationYears / life.BaselineGenerationYears;

        var eventThroughput = life.TypicalOffspringPerEvent / life.MinimumInterEventYears;
        var referenceEventThroughput =
            referenceLife.TypicalOffspringPerEvent / referenceLife.MinimumInterEventYears;
        var reproductiveThroughput = eventThroughput / referenceEventThroughput;

        var maturityPace = referenceLife.ReproductiveMaturityYears / life.ReproductiveMaturityYears;

        var geometricMean = Math.Cbrt(
            generationPace * reproductiveThroughput * maturityPace);
        var intrinsicGrowthPace = Math.Clamp(
            geometricMean,
            MinimumIntrinsicGrowthPace,
            MaximumIntrinsicGrowthPace);

        return new SpeciesDemographicPressure(
            species.Id,
            generationPace,
            reproductiveThroughput,
            maturityPace,
            intrinsicGrowthPace).Validated();
    }

    public static SpeciesDemographicPressure Evaluate(string speciesId) =>
        Evaluate(SpeciesCatalog.Get(speciesId));
}

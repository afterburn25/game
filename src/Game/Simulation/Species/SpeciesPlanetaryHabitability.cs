using System;
using Game.Simulation.Models;

namespace Game.Simulation.Species;

public enum SpeciesColonizationViability
{
    Unsuitable = 0,
    HabitatSupportedFallback = 1,
    NaturallyViable = 2,
}

public sealed record SpeciesPlanetaryHabitabilityAssessment(
    int PlanetaryBodyId,
    string SpeciesId,
    SpeciesEnvironmentAssessment Environment,
    SpeciesColonizationViability Viability,
    bool HasSolidSurface,
    bool HasNativePreWarpCivilization)
{
    public bool CanFoundCurrentColony =>
        !HasNativePreWarpCivilization &&
        Viability != SpeciesColonizationViability.Unsuitable;
}

/// <summary>
/// Colony-policy wrapper around the canonical <see cref="PlanetarySpeciesHabitabilityEvaluator"/>.
/// PlanetarySpeciesHabitabilityEvaluator owns the biological interpretation of physical world
/// conditions. This wrapper adds only the current colonization policy: native-civilization
/// exclusion plus the temporary legacy habitat-supported fallback used until explicit support
/// capabilities and operating costs are connected end-to-end.
/// </summary>
public sealed class SpeciesPlanetaryHabitabilityEvaluator
{
    private readonly PlanetarySpeciesHabitabilityEvaluator _planetary = new();

    public SpeciesPlanetaryHabitabilityAssessment Evaluate(
        PlanetaryBodyState body,
        string speciesId)
    {
        ArgumentNullException.ThrowIfNull(body);
        body.Validated();

        var species = SpeciesCatalog.Get(speciesId);
        var canonical = _planetary.Evaluate(
            species,
            body,
            PopulationAdaptationState.None(species.Id));

        var natural = canonical.NaturallyColonizable && !body.HasPreWarpCivilization;
        var fallback =
            !natural &&
            body.LegacyColonizationCandidate &&
            body.Environment.HasSolidSurface &&
            !body.HasPreWarpCivilization;

        var viability = natural
            ? SpeciesColonizationViability.NaturallyViable
            : fallback
                ? SpeciesColonizationViability.HabitatSupportedFallback
                : SpeciesColonizationViability.Unsuitable;

        return new SpeciesPlanetaryHabitabilityAssessment(
            body.Id,
            species.Id,
            canonical.Environment,
            viability,
            body.Environment.HasSolidSurface,
            body.HasPreWarpCivilization);
    }
}

/// <summary>
/// Compatibility facade for consumers introduced before the canonical planetary evaluator
/// exposed its mapping publicly. There is intentionally one mapping implementation.
/// </summary>
public static class PlanetaryHabitatEnvironmentMapper
{
    public static HabitatEnvironment Map(PlanetaryEnvironmentState environment) =>
        PlanetarySpeciesHabitabilityEvaluator.ToHabitatEnvironment(environment);
}

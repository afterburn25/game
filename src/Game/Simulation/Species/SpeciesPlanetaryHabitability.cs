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
        HasSolidSurface &&
        !HasNativePreWarpCivilization &&
        Viability != SpeciesColonizationViability.Unsuitable;
}

/// <summary>
/// Bridges authoritative planetary physics into species-relative biological suitability.
/// It does not own planet generation, survey knowledge, logistics, construction, or colony
/// economics. The legacy fallback preserves the current playable prototype until explicit
/// habitat-support capability is connected to research/construction/logistics.
/// </summary>
public sealed class SpeciesPlanetaryHabitabilityEvaluator
{
    private readonly SpeciesEnvironmentEvaluator _environment = new();

    public SpeciesPlanetaryHabitabilityAssessment Evaluate(
        PlanetaryBodyState body,
        string speciesId)
    {
        ArgumentNullException.ThrowIfNull(body);
        body.Validated();

        var species = SpeciesCatalog.Get(speciesId);
        var habitat = PlanetaryHabitatEnvironmentMapper.Map(body.Environment);
        var environment = _environment.Evaluate(species, habitat, PopulationAdaptationState.None(species.Id));

        var natural =
            body.Environment.HasSolidSurface &&
            !body.HasPreWarpCivilization &&
            environment.NaturalHabitability >= 0.72 &&
            environment.UnprotectedOperationalCapacity >= 0.35 &&
            environment.AtmosphereSuitability > 0.0 &&
            environment.SolventSuitability > 0.0;

        var viability = natural
            ? SpeciesColonizationViability.NaturallyViable
            : body.LegacyColonizationCandidate && body.Environment.HasSolidSurface && !body.HasPreWarpCivilization
                ? SpeciesColonizationViability.HabitatSupportedFallback
                : SpeciesColonizationViability.Unsuitable;

        return new SpeciesPlanetaryHabitabilityAssessment(
            body.Id,
            species.Id,
            environment,
            viability,
            body.Environment.HasSolidSurface,
            body.HasPreWarpCivilization);
    }
}

public static class PlanetaryHabitatEnvironmentMapper
{
    public static HabitatEnvironment Map(PlanetaryEnvironmentState environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        environment.Validated();

        return new HabitatEnvironment(
            environment.GravityG,
            environment.TemperatureKelvin,
            environment.PressureKPa,
            MapAtmosphere(environment.Atmosphere),
            MapSolvent(environment.AvailableSolvent),
            environment.RadiationHazard,
            environment.IsImmersedEnvironment).Validated();
    }

    private static AtmosphereClass MapAtmosphere(PlanetaryAtmosphereRegime atmosphere) => atmosphere switch
    {
        PlanetaryAtmosphereRegime.Vacuum => AtmosphereClass.Vacuum,
        PlanetaryAtmosphereRegime.OxygenNitrogen => AtmosphereClass.OxygenNitrogen,
        PlanetaryAtmosphereRegime.OxygenRich => AtmosphereClass.OxygenRich,
        PlanetaryAtmosphereRegime.CarbonDioxideRich => AtmosphereClass.CarbonDioxideRich,
        PlanetaryAtmosphereRegime.Reducing => AtmosphereClass.Reducing,
        PlanetaryAtmosphereRegime.Inert => AtmosphereClass.Inert,
        _ => AtmosphereClass.Other,
    };

    private static BiologicalSolvent MapSolvent(PlanetarySolventRegime solvent) => solvent switch
    {
        PlanetarySolventRegime.None => BiologicalSolvent.None,
        PlanetarySolventRegime.Water => BiologicalSolvent.Water,
        PlanetarySolventRegime.Ammonia => BiologicalSolvent.Ammonia,
        PlanetarySolventRegime.Hydrocarbon => BiologicalSolvent.Hydrocarbon,
        _ => BiologicalSolvent.Other,
    };
}

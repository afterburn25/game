using System;
using Game.Simulation.Models;

namespace Game.Simulation.Species;

public enum SpeciesSettlementSuitability
{
    Incompatible = 0,
    Marginal = 1,
    NaturallyViable = 2,
    Comfortable = 3,
}

/// <summary>
/// Species-relative interpretation of one authoritative planetary body. The body generator
/// owns physical facts; Species owns the contextual biological meaning of those facts.
/// </summary>
public sealed record PlanetarySpeciesHabitabilityAssessment(
    int PlanetaryBodyId,
    string SpeciesId,
    HabitatEnvironment Habitat,
    SpeciesEnvironmentAssessment Environment,
    SpeciesSettlementSuitability Suitability,
    bool HasPhysicalSettlementSite,
    bool NaturallyColonizable)
{
    public double NaturalHabitability => Environment.NaturalHabitability;
}

public sealed class PlanetarySpeciesHabitabilityEvaluator
{
    /// <summary>
    /// The minimum unprotected worst-axis suitability used for a naturally viable settlement.
    /// This is intentionally not "comfortable": marginal natural worlds remain possible but
    /// expose their physical stress/mitigation needs to population and logistics systems.
    /// </summary>
    public const double MinimumNaturalSettlementHabitability = 0.20;

    private readonly SpeciesEnvironmentEvaluator _environment = new();

    public PlanetarySpeciesHabitabilityAssessment Evaluate(
        SpeciesDefinition species,
        PlanetaryBodyState body,
        PopulationAdaptationState? adaptation = null)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(body);
        species.Validated();
        body.Validated();

        var habitat = ToHabitatEnvironment(body.Environment);
        var assessment = _environment.Evaluate(species, habitat, adaptation);

        // Surface species require an ordinary solid settlement site in the current colony
        // architecture. Species requiring immersion may use a naturally immersed environment;
        // later orbital/floating/subsurface settlement modes can broaden this explicitly.
        var hasPhysicalSettlementSite = species.Environment.RequiresImmersion
            ? body.Environment.IsImmersedEnvironment
            : body.Environment.HasSolidSurface;

        var naturallyColonizable =
            hasPhysicalSettlementSite &&
            assessment.NaturalHabitability >= MinimumNaturalSettlementHabitability;

        var suitability = !hasPhysicalSettlementSite || assessment.NaturalHabitability <= 0.0
            ? SpeciesSettlementSuitability.Incompatible
            : assessment.NaturalHabitability < MinimumNaturalSettlementHabitability
                ? SpeciesSettlementSuitability.Marginal
                : assessment.NaturalHabitability < 0.95
                    ? SpeciesSettlementSuitability.NaturallyViable
                    : SpeciesSettlementSuitability.Comfortable;

        return new PlanetarySpeciesHabitabilityAssessment(
            body.Id,
            species.Id,
            habitat,
            assessment,
            suitability,
            hasPhysicalSettlementSite,
            naturallyColonizable);
    }

    public static HabitatEnvironment ToHabitatEnvironment(PlanetaryEnvironmentState environment)
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
        PlanetaryAtmosphereRegime.Other => AtmosphereClass.Other,
        _ => throw new ArgumentOutOfRangeException(nameof(atmosphere)),
    };

    private static SolventClass MapSolvent(PlanetarySolventRegime solvent) => solvent switch
    {
        PlanetarySolventRegime.None => SolventClass.None,
        PlanetarySolventRegime.Water => SolventClass.Water,
        PlanetarySolventRegime.Ammonia => SolventClass.Ammonia,
        PlanetarySolventRegime.Hydrocarbon => SolventClass.Hydrocarbon,
        PlanetarySolventRegime.Other => SolventClass.Other,
        _ => throw new ArgumentOutOfRangeException(nameof(solvent)),
    };
}

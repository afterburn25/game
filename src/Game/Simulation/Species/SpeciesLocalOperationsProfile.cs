using System;

namespace Game.Simulation.Species;

/// <summary>
/// Physical inputs for ground-operation, EVA, medical, rescue, and other local-operation
/// consumers. This record intentionally does NOT compute combat effectiveness, morale,
/// discipline, weapon skill, doctrine, or tactical intelligence. Those belong to the
/// systems that consume these biological/environmental facts.
/// </summary>
public sealed record SpeciesLocalOperationsProfile(
    string SpeciesId,
    double PopulationMillions,
    double NaturalHabitability,
    double UnprotectedOperationalCapacity,
    double GravitySuitability,
    double TemperatureSuitability,
    double PressureSuitability,
    double RadiationSuitability,
    double TypicalAdultMassKg,
    double MusculoskeletalRobustness,
    double PeakActivityMetabolicDemandMillions,
    BodyPlan BodyPlan,
    LocomotionMode Locomotion,
    WorkOrientation WorkOrientation,
    int PrimaryManipulatorCount,
    int FineManipulatorCount,
    bool RequiresGravityMitigation,
    bool RequiresThermalControl,
    bool RequiresPressureControl,
    bool RequiresSealedHabitat,
    bool RequiresArtificialBiosphere,
    bool RequiresRadiationShielding)
{
    public bool RequiresEnvironmentalSupport =>
        RequiresGravityMitigation ||
        RequiresThermalControl ||
        RequiresPressureControl ||
        RequiresSealedHabitat ||
        RequiresArtificialBiosphere ||
        RequiresRadiationShielding;
}

public sealed class SpeciesLocalOperationsProfileEvaluator
{
    private readonly SpeciesEnvironmentEvaluator _environment = new();

    public SpeciesLocalOperationsProfile Evaluate(
        CurrentPopulationSpeciesSnapshot population,
        HabitatEnvironment habitat)
    {
        ArgumentNullException.ThrowIfNull(population);
        habitat.Validated();

        var cohort = population.AsUnadaptedCohort();
        var species = SpeciesCatalog.Get(population.SpeciesId);
        var assessment = _environment.Evaluate(species, habitat, cohort.Adaptation);
        var metabolism = SpeciesMetabolicEnvelopeEvaluator.Evaluate(cohort);

        return new SpeciesLocalOperationsProfile(
            species.Id,
            population.PopulationMillions,
            assessment.NaturalHabitability,
            assessment.UnprotectedOperationalCapacity,
            assessment.GravitySuitability,
            assessment.TemperatureSuitability,
            assessment.PressureSuitability,
            assessment.RadiationSuitability,
            species.Physiology.TypicalAdultMassKg,
            species.Physiology.MusculoskeletalRobustness,
            metabolism.PeakActivityDemandMillions,
            species.Morphology.BodyPlan,
            species.Morphology.LocomotionMode,
            species.Morphology.PreferredWorkOrientation,
            species.Morphology.PrimaryManipulatorCount,
            species.Morphology.FineManipulatorCount,
            assessment.RequiresGravityMitigation,
            assessment.RequiresThermalControl,
            assessment.RequiresPressureControl,
            assessment.RequiresSealedHabitat,
            assessment.RequiresArtificialBiosphere,
            assessment.RequiresRadiationShielding);
    }
}

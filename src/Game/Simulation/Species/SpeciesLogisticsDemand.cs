using System;

namespace Game.Simulation.Species;

public enum PopulationMetabolicOperatingState
{
    TypicalDay,
    Resting,
    PeakActivity,
    NaturalDormancy,
}

/// <summary>
/// Read-only physical demand contract for logistics/life-support consumers.
/// Values are normalized biological demand/biomass quantities, not credits, cargo units,
/// fuel units, routing capacity, or economic costs. The logistics system remains the owner
/// of converting these causes into actual resource flows and infrastructure requirements.
/// </summary>
public sealed record SpeciesLogisticsDemandProfile(
    string SpeciesId,
    double PopulationMillions,
    double AdultBiomassMillionKg,
    double ReferenceMetabolicDemandMillions,
    double SelectedMetabolicDemandMillions,
    PopulationMetabolicOperatingState OperatingState,
    DormancyMode NaturalDormancyMode,
    double MaximumNaturalDormancyDays,
    bool RequiresControlledHabitat,
    int RequiredEnvironmentalMitigationCategories,
    bool RequiresGravityMitigation,
    bool RequiresThermalControl,
    bool RequiresPressureControl,
    bool RequiresSealedHabitat,
    bool RequiresArtificialBiosphere,
    bool RequiresRadiationShielding,
    bool RequiresImmersion)
{
    public bool IsUsingNaturalDormancy => OperatingState == PopulationMetabolicOperatingState.NaturalDormancy;
}

public sealed class SpeciesLogisticsDemandEvaluator
{
    private readonly SpeciesPopulationRequirementsEvaluator _requirements = new();

    public SpeciesLogisticsDemandProfile Evaluate(
        CurrentPopulationSpeciesSnapshot population,
        HabitatEnvironment habitat,
        PopulationMetabolicOperatingState operatingState = PopulationMetabolicOperatingState.TypicalDay)
    {
        ArgumentNullException.ThrowIfNull(population);
        habitat.Validated();

        var cohort = population.AsUnadaptedCohort();
        var species = SpeciesCatalog.Get(population.SpeciesId);
        var requirements = _requirements.Evaluate(cohort, habitat);
        var metabolism = SpeciesMetabolicEnvelopeEvaluator.Evaluate(cohort);

        var selectedDemand = operatingState switch
        {
            PopulationMetabolicOperatingState.TypicalDay => metabolism.TypicalDayAverageDemandMillions,
            PopulationMetabolicOperatingState.Resting => metabolism.RestingDemandMillions,
            PopulationMetabolicOperatingState.PeakActivity => metabolism.PeakActivityDemandMillions,
            PopulationMetabolicOperatingState.NaturalDormancy when metabolism.HasNaturalDormancy => metabolism.DormantDemandMillions,
            PopulationMetabolicOperatingState.NaturalDormancy => throw new InvalidOperationException(
                $"Species '{species.Id}' has no natural dormancy mode; a technological/medical dormancy system must be modeled separately."),
            _ => throw new ArgumentOutOfRangeException(nameof(operatingState)),
        };

        if (!double.IsFinite(selectedDemand) || selectedDemand <= 0.0)
        {
            throw new InvalidOperationException("Selected species metabolic demand must remain finite and positive.");
        }

        return new SpeciesLogisticsDemandProfile(
            species.Id,
            population.PopulationMillions,
            requirements.AdultBiomassMillionKg,
            requirements.ReferenceMetabolicDemandMillions,
            selectedDemand,
            operatingState,
            metabolism.NaturalDormancyMode,
            metabolism.MaximumNaturalDormancyDays,
            requirements.RequiresControlledHabitat,
            requirements.RequiredEnvironmentalMitigationCategories,
            requirements.Environment.RequiresGravityMitigation,
            requirements.Environment.RequiresThermalControl,
            requirements.Environment.RequiresPressureControl,
            requirements.Environment.RequiresSealedHabitat,
            requirements.Environment.RequiresArtificialBiosphere,
            requirements.Environment.RequiresRadiationShielding,
            species.Environment.RequiresImmersion);
    }
}

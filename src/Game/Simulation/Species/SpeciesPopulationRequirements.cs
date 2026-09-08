using System;

namespace Game.Simulation.Species;

public sealed record SpeciesPopulationRequirements(
    string SpeciesId,
    double PopulationMillions,
    double ReferenceMetabolicDemandMillions,
    double AdultBiomassMillionKg,
    double TypicalAdultMassKg,
    double BaselineLifespanYears,
    double BaselineGenerationYears,
    double GenerationsPerCentury,
    SpeciesEnvironmentAssessment Environment,
    int RequiredEnvironmentalMitigationCategories)
{
    public bool RequiresControlledHabitat => RequiredEnvironmentalMitigationCategories > 0;
}

/// <summary>
/// Read-only translation from species/cohort biology into physical quantities that
/// other systems can consume. It does not assign economic, military, scientific, or
/// diplomatic percentage bonuses.
/// </summary>
public sealed class SpeciesPopulationRequirementsEvaluator
{
    private readonly SpeciesEnvironmentEvaluator _environmentEvaluator = new();

    public SpeciesPopulationRequirements Evaluate(
        SpeciesPopulationCohort cohort,
        HabitatEnvironment habitat)
    {
        cohort.Validated();
        habitat.Validated();

        var species = SpeciesCatalog.Get(cohort.SpeciesId);
        var environment = _environmentEvaluator.Evaluate(species, habitat, cohort.Adaptation);

        // PopulationMillions * kg/person is numerically million kg because the cohort
        // count itself is measured in millions of individuals.
        var adultBiomassMillionKg = cohort.PopulationMillions * species.Physiology.TypicalAdultMassKg;
        var metabolicDemandMillions = cohort.PopulationMillions * species.Physiology.BaselineMetabolicDemand;
        var generationsPerCentury = 100.0 / species.BaselineGenerationYears;

        var mitigationCategories = 0;
        if (environment.RequiresGravityMitigation) mitigationCategories++;
        if (environment.RequiresThermalControl) mitigationCategories++;
        if (environment.RequiresPressureControl) mitigationCategories++;
        if (environment.RequiresSealedHabitat) mitigationCategories++;
        if (environment.RequiresArtificialBiosphere) mitigationCategories++;
        if (environment.RequiresRadiationShielding) mitigationCategories++;

        if (!double.IsFinite(adultBiomassMillionKg) || adultBiomassMillionKg <= 0.0 ||
            !double.IsFinite(metabolicDemandMillions) || metabolicDemandMillions <= 0.0 ||
            !double.IsFinite(generationsPerCentury) || generationsPerCentury <= 0.0)
        {
            throw new InvalidOperationException("Derived species population requirements must remain finite and positive.");
        }

        return new SpeciesPopulationRequirements(
            cohort.SpeciesId,
            cohort.PopulationMillions,
            metabolicDemandMillions,
            adultBiomassMillionKg,
            species.Physiology.TypicalAdultMassKg,
            species.Physiology.BaselineLifespanYears,
            species.BaselineGenerationYears,
            generationsPerCentury,
            environment,
            mitigationCategories);
    }
}

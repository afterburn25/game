using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Colonization;
using Game.Simulation.Models;

namespace Game.Simulation.Species;

public sealed record ColonySpeciesEnvironmentSnapshot(
    int ColonyId,
    int CivilizationId,
    int SystemId,
    int? PlanetaryBodyId,
    string SpeciesId,
    double PopulationMillions,
    double NaturalHabitability,
    double UnprotectedOperationalCapacity,
    EnvironmentalLimitingFactor LimitingFactor,
    SpeciesColonizationViability ColonizationViability,
    double TypicalDayMetabolicDemandMillions,
    double AdultBiomassMillionKg,
    int RequiredEnvironmentalMitigationCategories,
    bool RequiresGravityMitigation,
    bool RequiresThermalControl,
    bool RequiresPressureControl,
    bool RequiresSealedHabitat,
    bool RequiresArtificialBiosphere,
    bool RequiresRadiationShielding)
{
    public bool RequiresEnvironmentalSupport => RequiredEnvironmentalMitigationCategories > 0;
}

/// <summary>
/// Read-only bridge from the current scalar colony population into biological/environmental
/// demands on the actual occupied world. It does not apply growth, mortality, productivity,
/// economic, logistics, medical, or political effects; owning systems may consume these facts.
/// </summary>
public sealed class ColonySpeciesEnvironmentView
{
    private readonly SpeciesPlanetaryHabitabilityEvaluator _habitability = new();
    private readonly SpeciesPopulationRequirementsEvaluator _requirements = new();
    private readonly ColonizationSimulation _colonization = new();

    public ColonySpeciesEnvironmentSnapshot Build(GalaxyState galaxy, int colonyId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        var colony = galaxy.Colonies.FirstOrDefault(candidate => candidate.Id == colonyId)
            ?? throw new InvalidOperationException($"Unknown colony {colonyId}.");

        if (colony.PopulationMillions <= 0.0 || !double.IsFinite(colony.PopulationMillions))
            throw new InvalidOperationException($"Colony {colony.Id} has no valid positive population to evaluate.");
        if (!SpeciesCatalog.TryGet(colony.PopulationSpeciesId, out _))
            throw new InvalidOperationException($"Colony {colony.Id} references unknown species '{colony.PopulationSpeciesId}'.");

        var body = _colonization.ResolveCompatibilityColonyWorld(galaxy, colony)
            ?? throw new InvalidOperationException($"Colony {colony.Id} cannot resolve an occupied planetary body.");
        var population = CurrentPopulationSpeciesView.ForColony(colony);
        var cohort = population.AsUnadaptedCohort();
        var habitat = PlanetaryHabitatEnvironmentMapper.Map(body.Environment);
        var habitability = _habitability.Evaluate(body, population.SpeciesId);
        var requirements = _requirements.Evaluate(cohort, habitat);
        var metabolic = SpeciesMetabolicEnvelopeEvaluator.Evaluate(cohort);

        return new ColonySpeciesEnvironmentSnapshot(
            colony.Id,
            colony.CivilizationId,
            colony.SystemId,
            body.Id,
            population.SpeciesId,
            population.PopulationMillions,
            habitability.Environment.NaturalHabitability,
            habitability.Environment.UnprotectedOperationalCapacity,
            habitability.Environment.LimitingFactor,
            habitability.Viability,
            metabolic.TypicalDayAverageDemandMillions,
            requirements.AdultBiomassMillionKg,
            requirements.RequiredEnvironmentalMitigationCategories,
            habitability.Environment.RequiresGravityMitigation,
            habitability.Environment.RequiresThermalControl,
            habitability.Environment.RequiresPressureControl,
            habitability.Environment.RequiresSealedHabitat,
            habitability.Environment.RequiresArtificialBiosphere,
            habitability.Environment.RequiresRadiationShielding);
    }

    public IReadOnlyList<ColonySpeciesEnvironmentSnapshot> BuildForCivilization(
        GalaxyState galaxy,
        int civilizationId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (!galaxy.Civilizations.Any(civilization => civilization.Id == civilizationId))
            throw new InvalidOperationException($"Unknown civilization {civilizationId}.");

        return galaxy.Colonies
            .Where(colony => colony.CivilizationId == civilizationId && colony.PopulationMillions > 0.0)
            .OrderBy(colony => colony.Id)
            .Select(colony => Build(galaxy, colony.Id))
            .ToArray();
    }
}

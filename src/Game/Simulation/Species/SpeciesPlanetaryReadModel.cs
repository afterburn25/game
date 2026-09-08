using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Simulation.Species;

public sealed record KnownSpeciesPlanetarySuitability(
    int PlanetaryBodyId,
    int SystemId,
    string SpeciesId,
    double NaturalHabitability,
    double UnprotectedOperationalCapacity,
    EnvironmentalLimitingFactor LimitingFactor,
    SpeciesColonizationViability ColonizationViability,
    bool RequiresGravityMitigation,
    bool RequiresThermalControl,
    bool RequiresPressureControl,
    bool RequiresSealedHabitat,
    bool RequiresArtificialBiosphere,
    bool RequiresRadiationShielding);

/// <summary>
/// Observer-local view of species suitability for planetary bodies whose detailed physical
/// environment is legitimately known. It never returns suitability for detection-only or
/// reconnaissance-only bodies, preventing biology calculations from becoming an information leak.
/// </summary>
public sealed class SpeciesPlanetaryReadModel
{
    private readonly SpeciesPlanetaryHabitabilityEvaluator _habitability = new();

    public IReadOnlyList<KnownSpeciesPlanetarySuitability> BuildForSpecies(
        GalaxyState galaxy,
        int observerCivilizationId,
        string speciesId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        _ = SpeciesCatalog.Get(speciesId);

        if (!galaxy.Civilizations.Any(civilization => civilization.Id == observerCivilizationId))
            throw new InvalidOperationException($"Unknown observer civilization {observerCivilizationId}.");

        return galaxy.PlanetaryBodies
            .Where(body => galaxy.Knowledge.GetSystemSurveyLevel(observerCivilizationId, body.SystemId) == SystemSurveyLevel.FullySurveyed)
            .OrderBy(body => body.SystemId)
            .ThenBy(body => body.Id)
            .Select(body =>
            {
                var assessment = _habitability.Evaluate(body, speciesId);
                var environment = assessment.Environment;
                return new KnownSpeciesPlanetarySuitability(
                    body.Id,
                    body.SystemId,
                    speciesId,
                    environment.NaturalHabitability,
                    environment.UnprotectedOperationalCapacity,
                    environment.LimitingFactor,
                    assessment.Viability,
                    environment.RequiresGravityMitigation,
                    environment.RequiresThermalControl,
                    environment.RequiresPressureControl,
                    environment.RequiresSealedHabitat,
                    environment.RequiresArtificialBiosphere,
                    environment.RequiresRadiationShielding);
            })
            .ToArray();
    }

    public IReadOnlyList<KnownSpeciesPlanetarySuitability> BuildForAvailablePopulations(
        GalaxyState galaxy,
        int observerCivilizationId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        var civilization = galaxy.Civilizations.FirstOrDefault(c => c.Id == observerCivilizationId)
            ?? throw new InvalidOperationException($"Unknown observer civilization {observerCivilizationId}.");

        var speciesIds = galaxy.Colonies
            .Where(colony => colony.CivilizationId == observerCivilizationId && colony.PopulationMillions > 0.0)
            .Select(colony => colony.PopulationSpeciesId)
            .Append(civilization.SpeciesId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        return speciesIds
            .SelectMany(speciesId => BuildForSpecies(galaxy, observerCivilizationId, speciesId))
            .OrderBy(view => view.SystemId)
            .ThenBy(view => view.PlanetaryBodyId)
            .ThenBy(view => view.SpeciesId, StringComparer.Ordinal)
            .ToArray();
    }
}

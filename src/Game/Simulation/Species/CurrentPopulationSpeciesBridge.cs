using System;
using Game.Simulation.Models;

namespace Game.Simulation.Species;

/// <summary>
/// Read-only species identity/amount snapshot for the current scalar population model.
/// It is intentionally smaller than SpeciesPopulationCohort because ColonyState does not
/// yet own authoritative adaptation history. Consumers must not mistake the bridge's
/// unadapted cohort conversion for persisted adaptation state.
/// </summary>
public sealed record CurrentPopulationSpeciesSnapshot(
    string SpeciesId,
    double PopulationMillions)
{
    public SpeciesDefinition Species => SpeciesCatalog.Get(SpeciesId);

    public SpeciesPopulationCohort AsUnadaptedCohort() =>
        SpeciesPopulationCohort.Founding(SpeciesId, PopulationMillions);
}

public static class CurrentPopulationSpeciesBridge
{
    public static CurrentPopulationSpeciesSnapshot FromColony(ColonyState colony)
    {
        ArgumentNullException.ThrowIfNull(colony);
        ValidatePopulation(colony.PopulationMillions, $"colony {colony.Id}");
        ValidateSpecies(colony.PopulationSpeciesId, $"colony {colony.Id}");
        return new CurrentPopulationSpeciesSnapshot(
            colony.PopulationSpeciesId,
            colony.PopulationMillions);
    }

    public static CurrentPopulationSpeciesSnapshot? FromEmbarkedFleet(FleetState fleet)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        if (!double.IsFinite(fleet.EmbarkedPopulationMillions) || fleet.EmbarkedPopulationMillions < 0.0)
        {
            throw new InvalidOperationException(
                $"Fleet {fleet.Id} has invalid embarked population {fleet.EmbarkedPopulationMillions}.");
        }

        if (fleet.EmbarkedPopulationMillions <= 0.0)
        {
            if (!string.IsNullOrWhiteSpace(fleet.EmbarkedPopulationSpeciesId))
            {
                throw new InvalidOperationException(
                    $"Fleet {fleet.Id} carries no population but retains species identity '{fleet.EmbarkedPopulationSpeciesId}'.");
            }
            return null;
        }

        ValidateSpecies(fleet.EmbarkedPopulationSpeciesId, $"fleet {fleet.Id}");
        return new CurrentPopulationSpeciesSnapshot(
            fleet.EmbarkedPopulationSpeciesId!,
            fleet.EmbarkedPopulationMillions);
    }

    private static void ValidatePopulation(double populationMillions, string owner)
    {
        if (!double.IsFinite(populationMillions) || populationMillions <= 0.0)
        {
            throw new InvalidOperationException(
                $"{owner} must have finite positive population for a species snapshot.");
        }
    }

    private static void ValidateSpecies(string? speciesId, string owner)
    {
        if (string.IsNullOrWhiteSpace(speciesId) || !SpeciesCatalog.TryGet(speciesId, out _))
        {
            throw new InvalidOperationException(
                $"{owner} references unknown population species '{speciesId}'.");
        }
    }
}

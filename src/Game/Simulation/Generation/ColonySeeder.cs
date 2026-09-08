using System;
using System.Collections.Generic;
using Game.Simulation.Models;
using Game.Simulation.Species;

namespace Game.Simulation.Generation;

public sealed class ColonySeeder
{
    /// <summary>
    /// Legacy-compatible scalar seeding path. Older migrations that do not provide a body
    /// catalog retain a null PlanetaryBodyId and can be resolved separately; new campaigns use
    /// the body-aware overload below.
    /// </summary>
    public IList<ColonyState> Seed(IList<CivilizationState> civilizations) =>
        SeedInternal(civilizations, planetaryBodies: null);

    public IList<ColonyState> Seed(
        IList<CivilizationState> civilizations,
        IReadOnlyList<PlanetaryBodyState> planetaryBodies)
    {
        ArgumentNullException.ThrowIfNull(planetaryBodies);
        return SeedInternal(civilizations, planetaryBodies);
    }

    private static IList<ColonyState> SeedInternal(
        IList<CivilizationState> civilizations,
        IReadOnlyList<PlanetaryBodyState>? planetaryBodies)
    {
        ArgumentNullException.ThrowIfNull(civilizations);

        var colonies = new List<ColonyState>(civilizations.Count);
        var homeworldPlanner = planetaryBodies is null ? null : new SpeciesHomeworldPlanner();
        var id = 0;
        foreach (var civilization in civilizations)
        {
            int? planetaryBodyId = null;
            if (homeworldPlanner is not null)
            {
                var home = homeworldPlanner.ResolveWithinSystem(
                    civilization.Id,
                    civilization.SpeciesId,
                    civilization.HomeSystemId,
                    planetaryBodies!);
                planetaryBodyId = home.PlanetaryBodyId;
            }

            colonies.Add(new ColonyState
            {
                Id = id++,
                CivilizationId = civilization.Id,
                SystemId = civilization.HomeSystemId,
                PlanetaryBodyId = planetaryBodyId,
                Name = $"{civilization.Name} Prime",
                PopulationSpeciesId = civilization.SpeciesId,
                PopulationMillions = civilization.IsSeededAncient ? 12000.0 : 9500.0,
                Infrastructure = civilization.IsSeededAncient ? 3.0 : 1.0,
                Stability = 1.0,
            });
        }
        return colonies;
    }

    public IReadOnlyList<CivilizationEconomyState> SeedEconomies(IList<CivilizationState> civilizations)
    {
        var economies = new List<CivilizationEconomyState>(civilizations.Count);
        foreach (var civilization in civilizations)
        {
            economies.Add(new CivilizationEconomyState
            {
                CivilizationId = civilization.Id,
                Credits = civilization.IsSeededAncient ? 50000.0 : 500.0,
                Industry = civilization.IsSeededAncient ? 25000.0 : 200.0,
                Science = civilization.IsSeededAncient ? 10000.0 : 0.0,
            });
        }
        return economies;
    }
}

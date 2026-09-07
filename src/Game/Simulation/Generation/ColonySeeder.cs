using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

public sealed class ColonySeeder
{
    public IList<ColonyState> Seed(
        IList<CivilizationState> civilizations,
        IReadOnlyList<PlanetaryBodyState>? planetaryBodies = null)
    {
        var colonies = new List<ColonyState>(civilizations.Count);
        var id = 0;
        foreach (var civilization in civilizations)
        {
            var homeBody = planetaryBodies is null
                ? null
                : SelectHomeBody(planetaryBodies, civilization.HomeSystemId);

            colonies.Add(new ColonyState
            {
                Id = id++,
                CivilizationId = civilization.Id,
                SystemId = civilization.HomeSystemId,
                PlanetaryBodyId = homeBody?.Id,
                Name = $"{civilization.Name} Prime",
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

    public static PlanetaryBodyState? SelectHomeBody(
        IReadOnlyList<PlanetaryBodyState> planetaryBodies,
        int systemId)
    {
        var bodies = planetaryBodies.Where(body => body.SystemId == systemId).ToArray();
        return bodies.FirstOrDefault(body => body.LegacyColonizationCandidate)
            ?? bodies.FirstOrDefault(body => body.Kind == PlanetaryBodyKind.Planet && body.Environment.HasSolidSurface)
            ?? bodies.FirstOrDefault();
    }
}

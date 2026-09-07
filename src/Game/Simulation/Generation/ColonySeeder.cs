using System.Collections.Generic;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

public sealed class ColonySeeder
{
    public IList<ColonyState> Seed(IList<CivilizationState> civilizations)
    {
        var colonies = new List<ColonyState>(civilizations.Count);
        var id = 0;
        foreach (var civilization in civilizations)
        {
            colonies.Add(new ColonyState
            {
                Id = id++, CivilizationId = civilization.Id, SystemId = civilization.HomeSystemId, Name = $"{civilization.Name} Prime",
                PopulationMillions = civilization.IsSeededAncient ? 12000.0 : 9500.0,
                Infrastructure = civilization.IsSeededAncient ? 3.0 : 1.0, Stability = 1.0,
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

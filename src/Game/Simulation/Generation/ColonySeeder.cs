using System.Collections.Generic;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

public sealed class ColonySeeder
{
    public IReadOnlyList<ColonyState> Seed(IReadOnlyList<CivilizationState> civilizations)
    {
        var colonies = new List<ColonyState>(civilizations.Count);
        var id = 0;
        foreach (var civilization in civilizations)
        {
            colonies.Add(new ColonyState
            {
                Id = id++,
                CivilizationId = civilization.Id,
                SystemId = civilization.HomeSystemId,
                Name = $"{civilization.Name} Prime",
                PopulationMillions = 4200.0,
                Infrastructure = 1.0,
                Stability = 1.0,
            });
        }
        return colonies;
    }

    public IReadOnlyList<CivilizationEconomyState> SeedEconomies(IReadOnlyList<CivilizationState> civilizations)
    {
        var economies = new List<CivilizationEconomyState>(civilizations.Count);
        foreach (var civilization in civilizations)
        {
            economies.Add(new CivilizationEconomyState
            {
                CivilizationId = civilization.Id,
                Credits = 500.0,
                Industry = 200.0,
                Science = 0.0,
            });
        }
        return economies;
    }
}

using System.Collections.Generic;
using Game.Simulation.Models;
using Game.Simulation.Research;

namespace Game.Simulation.Generation;

public sealed class TechnologySeeder
{
    public IList<TechnologyState> Seed(IList<CivilizationState> civilizations)
    {
        var result = new List<TechnologyState>(civilizations.Count);
        foreach (var civilization in civilizations)
        {
            var state = new TechnologyState { CivilizationId = civilization.Id };
            if (civilization.DevelopmentStage == CivilizationDevelopmentStage.AncientSpacefaring)
                foreach (var technology in TechnologyRegistry.All) state.CompletedTechnologyIds.Add(technology.Id);
            result.Add(state);
        }
        return result;
    }
}

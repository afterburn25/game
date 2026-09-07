using System.Collections.Generic;
using Game.Simulation.Construction;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

public sealed class ConstructionSeeder
{
    public IList<ConstructionState> Seed(IList<CivilizationState> civilizations)
    {
        var states = new List<ConstructionState>(civilizations.Count);
        foreach (var civilization in civilizations)
        {
            var state = new ConstructionState { CivilizationId = civilization.Id };
            if (civilization.IsSeededAncient)
            {
                foreach (var project in ConstructionRegistry.All)
                    state.CompletedProjectIds.Add(project.Id);
            }
            states.Add(state);
        }
        return states;
    }
}

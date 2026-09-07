using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;
using Game.Simulation.Shipbuilding;

namespace Game.Simulation.Generation;

public sealed class ShipyardSeeder
{
    public IList<ShipyardState> Seed(IEnumerable<CivilizationState> civilizations) =>
        civilizations.Select(civilization => new ShipyardState
        {
            CivilizationId = civilization.Id,
        }).ToList();
}

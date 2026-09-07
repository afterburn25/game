using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

public sealed class FleetSeeder
{
    public IReadOnlyList<FleetState> Seed(
        IReadOnlyList<StarSystemState> systems,
        IReadOnlyList<CivilizationState> civilizations)
    {
        var fleets = new List<FleetState>(civilizations.Count * 2);
        var id = 0;

        foreach (var civilization in civilizations)
        {
            var home = systems.First(system => system.Id == civilization.HomeSystemId);
            fleets.Add(new FleetState
            {
                Id = id++,
                CivilizationId = civilization.Id,
                Name = civilization.IsPlayer ? "Pathfinder One" : $"{civilization.Name} Scout",
                Role = FleetRole.Scout,
                Position = home.Position,
                CurrentSystemId = home.Id,
                DestinationSystemId = null,
                StrategicSpeed = 22.0,
                SensorRange = 135.0f,
                IsActive = true,
            });

            fleets.Add(new FleetState
            {
                Id = id++,
                CivilizationId = civilization.Id,
                Name = civilization.IsPlayer ? "Pioneer One" : $"{civilization.Name} Pioneer",
                Role = FleetRole.Colony,
                Position = home.Position,
                CurrentSystemId = home.Id,
                DestinationSystemId = null,
                StrategicSpeed = 13.5,
                SensorRange = 75.0f,
                IsActive = true,
            });
        }

        return fleets;
    }
}

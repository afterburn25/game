using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

public sealed class FleetSeeder
{
    public IList<FleetState> Seed(IReadOnlyList<StarSystemState> systems, IList<CivilizationState> civilizations)
    {
        var fleets = new List<FleetState>();
        foreach (var civilization in civilizations)
            if (civilization.DevelopmentStage == CivilizationDevelopmentStage.WarpCapable) AddStarterFleets(fleets, systems, civilization);
        return fleets;
    }

    public void EnsureStarterFleets(GalaxyState galaxy, int civilizationId)
    {
        var civilization = galaxy.Civilizations.First(c => c.Id == civilizationId);
        if (galaxy.Fleets.Any(f => f.IsActive && f.CivilizationId == civilizationId && f.Role == FleetRole.Scout)) return;
        AddStarterFleets(galaxy.Fleets, galaxy.Systems, civilization);
    }

    private static void AddStarterFleets(IList<FleetState> fleets, IReadOnlyList<StarSystemState> systems, CivilizationState civilization)
    {
        var home = systems.First(system => system.Id == civilization.HomeSystemId);
        var nextId = fleets.Count == 0 ? 0 : fleets.Max(f => f.Id) + 1;
        fleets.Add(new FleetState
        {
            Id = nextId++, CivilizationId = civilization.Id, Name = civilization.IsPlayer ? "Pathfinder One" : $"{civilization.Name} Scout",
            Role = FleetRole.Scout, Position = home.Position, CurrentSystemId = home.Id, StrategicSpeed = 22.0, SensorRange = 135.0f, IsActive = true,
        });
        if (!civilization.ExpansionAllowed) return;
        fleets.Add(new FleetState
        {
            Id = nextId, CivilizationId = civilization.Id, Name = civilization.IsPlayer ? "Pioneer One" : $"{civilization.Name} Pioneer",
            Role = FleetRole.Colony, Position = home.Position, CurrentSystemId = home.Id, StrategicSpeed = 13.5, SensorRange = 75.0f, IsActive = true,
        });
    }
}

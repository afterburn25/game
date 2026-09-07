using Game.Simulation.Models;

namespace Game.Simulation.Shipbuilding;

public sealed record ShipDesignDefinition(
    string Id,
    string Name,
    string Description,
    FleetRole Role,
    double IndustryCost,
    double StrategicSpeed,
    float SensorRange,
    double PopulationCostMillions = 0.0);

using System.Numerics;
using Game.Simulation.Combat;

namespace Game.Simulation.Models;

public sealed class FleetState
{
    public required int Id { get; init; }
    public required int CivilizationId { get; init; }
    public required string Name { get; init; }
    public required FleetRole Role { get; init; }
    public required Vector2 Position { get; set; }
    public int? CurrentSystemId { get; set; }
    public int? DestinationSystemId { get; set; }
    public double StrategicSpeed { get; init; } = 22.0;
    public float SensorRange { get; init; } = 135.0f;
    public bool IsActive { get; set; } = true;
    public FleetCombatState? Combat { get; set; }
}

public enum FleetRole
{
    Scout,
    Science,
    Colony,
    Military,
}

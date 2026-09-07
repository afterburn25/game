using System.Numerics;

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

    /// <summary>
    /// Real population physically committed to this fleet. For early-release colony ships,
    /// shipbuilding removes these colonists from a source colony and colonization transfers
    /// exactly this amount into the destination settlement.
    /// </summary>
    public double EmbarkedPopulationMillions { get; set; }
}

public enum FleetRole
{
    Scout,
    Science,
    Colony,
    Military,
}

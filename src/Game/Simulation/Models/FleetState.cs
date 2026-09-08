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

    /// <summary>
    /// Exact planetary-body target for a body-aware colony mission. Null is normal for
    /// non-colony fleets and idle colony ships. Keeping the body ID beside the system target
    /// avoids guessing after save/load once more than one body can be species-suitable.
    /// </summary>
    public int? DestinationPlanetaryBodyId { get; set; }

    public double StrategicSpeed { get; init; } = 22.0;
    public float SensorRange { get; init; } = 135.0f;
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Real population physically committed to this fleet. For early-release colony ships,
    /// shipbuilding removes these colonists from a source colony and colonization transfers
    /// exactly this amount into the destination settlement.
    /// </summary>
    public double EmbarkedPopulationMillions { get; set; }

    /// <summary>
    /// Species identity of the embarked scalar population. Null is valid only when no
    /// population is aboard; future multi-species transport should replace this bridge with
    /// a bounded manifest rather than parallel unbounded passenger objects.
    /// </summary>
    public string? EmbarkedPopulationSpeciesId { get; set; }

    /// <summary>
    /// Compact persistent vessel combat state. The current strategic model represents each
    /// constructed vessel directly as a FleetState; future multi-vessel composition can wrap
    /// this state without making presentation authoritative.
    /// </summary>
    public FleetCombatState? Combat { get; set; }
}

public enum FleetRole
{
    Scout,
    Science,
    Colony,
    Military,
}

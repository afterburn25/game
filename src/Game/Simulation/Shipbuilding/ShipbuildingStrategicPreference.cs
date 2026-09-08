using Game.Simulation.Models;

namespace Game.Simulation.Shipbuilding;

/// <summary>
/// Optional high-level preference supplied to the shipbuilding-owned automatic selector.
/// It does not grant design eligibility, reserve population, spend Industry, or start builds.
/// </summary>
public sealed record ShipbuildingStrategicPreference(
    FleetRole? PreferredNewFleetRole,
    bool DeferNewColonization)
{
    public static ShipbuildingStrategicPreference None { get; } = new(null, false);
}

public interface IShipbuildingStrategicPreferenceView
{
    ShipbuildingStrategicPreference GetPreference(int civilizationId);
}

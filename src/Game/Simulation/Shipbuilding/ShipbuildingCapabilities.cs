using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Shipbuilding;

public static class ShipbuildingCapabilityIds
{
    public const string SpacecraftConstruction = "spacecraft_construction";
    public const string ExperimentalInterstellarTransit = "experimental_interstellar_transit";
}

public interface IShipbuildingCapabilityView
{
    bool HasCivilizationCapability(GalaxyState galaxy, int civilizationId, string capabilityId);
}

/// <summary>
/// Temporary bridge from the current fixed prototype research state into the capability-oriented
/// shipbuilding contract. Replace this adapter when the Adaptive Research runtime publishes
/// materialized civilization capabilities; the shipbuilding simulation itself should not need to change.
/// </summary>
public sealed class PrototypeShipbuildingCapabilityView : IShipbuildingCapabilityView
{
    public bool HasCivilizationCapability(GalaxyState galaxy, int civilizationId, string capabilityId)
    {
        var technology = galaxy.Technologies.FirstOrDefault(state => state.CivilizationId == civilizationId);
        if (technology is null)
            return false;

        return capabilityId switch
        {
            ShipbuildingCapabilityIds.SpacecraftConstruction => technology.CompletedTechnologyIds.Contains("orbital_industry"),
            ShipbuildingCapabilityIds.ExperimentalInterstellarTransit => technology.CompletedTechnologyIds.Contains("prototype_warp_drive"),
            _ => false,
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Colonization;

/// <summary>
/// Reconstructs same-civilization colony mission reservations from live fleet state.
/// It never includes foreign missions, preventing hidden opponent destination intent from
/// affecting player/AI eligibility. No reservation state is persisted independently.
/// </summary>
public static class FriendlyColonyMissionReservations
{
    public static IReadOnlyDictionary<int, int> BuildBySystem(
        GalaxyState galaxy,
        FleetState requestingFleet)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(requestingFleet);

        return galaxy.Fleets
            .Where(other =>
                other.Id != requestingFleet.Id &&
                other.IsActive &&
                other.CivilizationId == requestingFleet.CivilizationId &&
                other.Role == FleetRole.Colony &&
                other.EmbarkedPopulationMillions > 0.0)
            .Select(other => new
            {
                Fleet = other,
                SystemId = other.DestinationSystemId ?? other.CurrentSystemId,
            })
            .Where(entry => entry.SystemId is not null)
            .OrderBy(entry => entry.Fleet.Id)
            .GroupBy(entry => entry.SystemId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.First().Fleet.Id);
    }

    public static bool TryGetReservingFleetId(
        GalaxyState galaxy,
        FleetState requestingFleet,
        int systemId,
        out int reservingFleetId)
    {
        var reservations = BuildBySystem(galaxy, requestingFleet);
        return reservations.TryGetValue(systemId, out reservingFleetId);
    }
}

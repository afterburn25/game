using System.Linq;
using System.Text;
using Game.Simulation.Economy;
using Game.Simulation.Exploration;
using Game.Simulation.Models;

namespace Game.Presentation;

public sealed record UiOwnedFleetSnapshot(int FleetId, FleetRole Role, string Name, string Location,
    string Activity, double OperatingCostPerDay);

/// <summary>
/// Player-facing exploration adapter. All mission phase/ETA calculations come from the
/// observer-safe ExplorationReadModel; presentation code does not recompute travel or survey math.
/// </summary>
public partial class Main
{
    private readonly ExplorationReadModel _explorationReadModel = new();

    public UiOwnedFleetSnapshot[] UiOwnedFleets => _galaxy is null
        ? System.Array.Empty<UiOwnedFleetSnapshot>()
        : _galaxy.Fleets
            .Where(fleet => fleet.IsActive && fleet.CivilizationId == _galaxy.PlayerCivilizationId)
            .OrderBy(fleet => fleet.Role).ThenBy(fleet => fleet.Id)
            .Select(fleet =>
            {
                var locationId = fleet.CurrentSystemId ?? fleet.DestinationSystemId;
                var location = locationId is int id
                    ? _galaxy.Systems.FirstOrDefault(system => system.Id == id)?.Name ?? "Deep space"
                    : "Deep space";
                var activity = fleet.DestinationSystemId is int destination
                    ? $"En route to {_galaxy.Systems.First(system => system.Id == destination).Name}"
                    : fleet.CurrentSystemId.HasValue ? "Awaiting orders" : "In transit";
                return new UiOwnedFleetSnapshot(fleet.Id, fleet.Role, fleet.Name, location, activity,
                    EconomySimulation.GetFleetOperatingCost(fleet.Role));
            }).ToArray();

    public string UiExplorationMissionDetails
    {
        get
        {
            if (_galaxy is null)
                return "Exploration missions are initializing…";

            var view = _explorationReadModel.Build(_galaxy, _galaxy.PlayerCivilizationId);
            if (view.ActiveMissions.Count == 0)
                return "No active scout, science, or colony missions.";

            var builder = new StringBuilder();
            builder.Append("Active missions: ").AppendLine(view.ActiveMissions.Count.ToString());

            foreach (var mission in view.ActiveMissions.Take(4))
            {
                builder.Append("• ").Append(mission.FleetName)
                    .Append(" — ").AppendLine(FormatMissionPhase(mission.Status.Phase));
                builder.Append("  ").AppendLine(mission.Status.Summary);
            }

            if (view.ActiveMissions.Count > 4)
                builder.Append("• +").Append(view.ActiveMissions.Count - 4).Append(" more active missions");

            return builder.ToString().TrimEnd();
        }
    }

    private static string FormatMissionPhase(ExplorationMissionPhase phase) => phase switch
    {
        ExplorationMissionPhase.AwaitingOrder => "Awaiting order",
        ExplorationMissionPhase.Traveling => "Traveling",
        ExplorationMissionPhase.ReconnaissanceReady => "Reconnaissance ready",
        ExplorationMissionPhase.ScienceSurveying => "Science survey",
        ExplorationMissionPhase.ColonySettlementReady => "Settlement ready",
        _ => phase.ToString(),
    };
}

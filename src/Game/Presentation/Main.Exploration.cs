using System.Linq;
using System.Text;
using Game.Simulation.Economy;
using Game.Simulation.Exploration;
using Game.Simulation.Models;
using Game.Simulation.Shipbuilding;

namespace Game.Presentation;

public sealed record UiOwnedFleetSnapshot(int FleetId, FleetRole Role, string Name, string Location,
    string Activity, string DesignName, double StrategicSpeed, double MaximumLegRangeLightYears,
    int RemainingRouteLegs, double RemainingRouteDistanceLightYears,
    double OperatingCostPerDay, bool IsArmed, double Integrity, string MilitaryOrder);
public sealed record UiExplorationMissionSnapshot(int FleetId, FleetRole Role, string FleetName,
    string Phase, string Destination, string Eta, string Summary);

/// <summary>
/// Player-facing exploration adapter. All mission phase/ETA calculations come from the
/// observer-safe ExplorationReadModel; presentation code does not recompute travel or survey math.
/// </summary>
public partial class Main
{
    private readonly ExplorationReadModel _explorationReadModel = new();
    private readonly ExplorationMissionStatusEvaluator _missionStatusEvaluator = new();

    public UiOwnedFleetSnapshot[] UiOwnedFleets
    {
        get
        {
            if (_galaxy is null) return System.Array.Empty<UiOwnedFleetSnapshot>();
            var combat = _coreSimulation.GetOwnCombatFleetStatus(_galaxy, _galaxy.PlayerCivilizationId)
                .Fleets.ToDictionary(status => status.FleetId);
            return _galaxy.Fleets
                .Where(fleet => fleet.IsActive && fleet.CivilizationId == _galaxy.PlayerCivilizationId)
                .OrderBy(fleet => fleet.Role).ThenBy(fleet => fleet.Id)
                .Select(fleet =>
                {
                    var location = fleet.CurrentSystemId is int id
                        ? _galaxy.Systems.FirstOrDefault(system => system.Id == id)?.Name ?? "Deep space"
                        : "Deep space";
                    var activity = fleet.Role == FleetRole.Military && fleet.DestinationSystemId is int deployment
                        ? $"Deploying to {_galaxy.Systems.First(system => system.Id == deployment).Name}"
                        : fleet.Role is FleetRole.Scout or FleetRole.Science or FleetRole.Colony
                        ? FormatMissionPhase(_missionStatusEvaluator.Build(_galaxy, fleet).Phase)
                        : fleet.CurrentSystemId.HasValue ? "On station" : "In transit";
                    var combatStatus = combat[fleet.Id];
                    var route = FleetRouteMetrics.Measure(_galaxy, fleet);
                    var designName = ShipDesignRegistry.TryGet(fleet.DesignId, out var design)
                        ? design!.Name
                        : "Legacy vessel";
                    return new UiOwnedFleetSnapshot(fleet.Id, fleet.Role, fleet.Name, location, activity,
                        designName, fleet.StrategicSpeed, fleet.MaximumLegRangeLightYears,
                        route.RemainingLegs, route.DistanceLightYears,
                        EconomySimulation.GetFleetOperatingCost(fleet.Role), combatStatus.IsArmed,
                        combatStatus.DurabilityRatio, combatStatus.CurrentOrder.ToString());
                }).ToArray();
        }
    }

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

    public UiExplorationMissionSnapshot[] UiExplorationMissions
    {
        get
        {
            if (_galaxy is null) return System.Array.Empty<UiExplorationMissionSnapshot>();
            var view = _explorationReadModel.Build(_galaxy, _galaxy.PlayerCivilizationId);
            return view.ActiveMissions.Take(8).Select(mission =>
            {
                var targetId = mission.DestinationSystemId ?? mission.CurrentSystemId;
                var destination = targetId is int id
                    ? _galaxy.Systems.FirstOrDefault(system => system.Id == id)?.Name ?? "Deep space"
                    : "Awaiting destination";
                var eta = mission.Status.EstimatedMissionDaysRemaining is double days
                    ? $"{days:0.0} days remaining"
                    : mission.Status.Phase == ExplorationMissionPhase.AwaitingOrder ? "Ready for orders" : "ETA unavailable";
                return new UiExplorationMissionSnapshot(mission.FleetId, mission.Role, mission.FleetName,
                    FormatMissionPhase(mission.Status.Phase), destination, eta, mission.Status.Summary);
            }).ToArray();
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

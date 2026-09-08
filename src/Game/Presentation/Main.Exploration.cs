using System.Linq;
using System.Text;
using Game.Simulation.Exploration;

namespace Game.Presentation;

/// <summary>
/// Player-facing exploration adapter. All mission phase/ETA calculations come from the
/// observer-safe ExplorationReadModel; presentation code does not recompute travel or survey math.
/// </summary>
public partial class Main
{
    private readonly ExplorationReadModel _explorationReadModel = new();

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

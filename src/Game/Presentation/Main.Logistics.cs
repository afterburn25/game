using System.Linq;
using System.Text;
using Game.Simulation.Economy;

namespace Game.Presentation;

/// <summary>
/// Read-only player-facing logistics presentation adapter. Authoritative logistics
/// calculations stay in the economy/logistics subsystem.
/// </summary>
public partial class Main
{
    private readonly IEconomyLogisticsView _economyLogisticsView = new PrototypeEconomyLogisticsView();
    private readonly IHomeSystemLogisticsNetworkView _homeSystemLogisticsView = new PrototypeHomeSystemLogisticsNetworkView();

    public string UiLogisticsSummary
    {
        get
        {
            if (_galaxy is null)
                return "Supply initializing…";

            var logistics = _economyLogisticsView.GetSnapshot(_galaxy, _galaxy.PlayerCivilizationId);
            var localCoverage = logistics.TotalSupportDemandPerDay <= 0.0
                ? 1.0
                : logistics.TotalLocalSupportCapacityPerDay / logistics.TotalSupportDemandPerDay;

            return $"Supply {logistics.Condition} · effective {logistics.EffectiveCoverageRatio:P0} · "
                 + $"local {localCoverage:P0} · imports {logistics.ImportRequirementPerDay:0.00}/day · "
                 + $"cargo {logistics.CargoHandlingCapacityPerDay:0.00}/day · "
                 + $"strained {logistics.StrainedColonyCount} · critical {logistics.CriticalColonyCount}";
        }
    }

    public string UiHomeSystemLogisticsDetails
    {
        get
        {
            if (_galaxy is null)
                return "Campaign logistics are initializing…";

            var network = _homeSystemLogisticsView.Build(_galaxy, _galaxy.PlayerCivilizationId);
            var systemName = _galaxy.Systems.FirstOrDefault(system => system.Id == network.HomeSystemId)?.Name
                ?? $"System {network.HomeSystemId}";

            var builder = new StringBuilder();
            builder.AppendLine(systemName);
            builder.Append("Nodes: ").Append(network.Nodes.Count)
                .Append(" · corridors: ").AppendLine(network.Links.Count.ToString());
            builder.Append("Supply offered: ").Append(network.TotalSupplyOfferedPerDay.ToString("0.00"))
                .Append("/day · demand: ").Append(network.TotalDemandPerDay.ToString("0.00")).AppendLine("/day");
            builder.Append("Allocated: ").Append(network.TotalAllocatedPerDay.ToString("0.00"))
                .Append("/day · unmet: ").Append(network.TotalUnmetDemandPerDay.ToString("0.00")).AppendLine("/day");

            if (network.Nodes.Count == 0)
            {
                builder.Append("No represented logistics nodes.");
                return builder.ToString();
            }

            builder.AppendLine("Network:");
            foreach (var node in network.Nodes.Take(6))
                builder.Append("• ").Append(node.Kind).Append(" — ").AppendLine(node.Name);

            if (network.Nodes.Count > 6)
                builder.Append("• +").Append(network.Nodes.Count - 6).Append(" more nodes");

            return builder.ToString().TrimEnd();
        }
    }
}

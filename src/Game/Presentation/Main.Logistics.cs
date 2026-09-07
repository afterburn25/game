using Game.Simulation.Economy;

namespace Game.Presentation;

/// <summary>
/// Read-only player-facing logistics presentation adapter. Authoritative logistics
/// calculations stay in the economy/logistics subsystem.
/// </summary>
public partial class Main
{
    private readonly IEconomyLogisticsView _economyLogisticsView = new PrototypeEconomyLogisticsView();

    public string UiLogisticsSummary
    {
        get
        {
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
}

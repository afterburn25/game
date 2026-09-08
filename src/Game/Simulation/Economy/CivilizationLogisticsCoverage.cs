using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Economy;

/// <summary>
/// Conservative read model for an owned colony system outside the civilization's represented
/// home-system logistics network. No interstellar freight corridor is inferred merely because
/// the civilization owns colonies in two systems.
/// </summary>
public sealed record ExternalSystemLogisticsStatus(
    int CivilizationId,
    int SystemId,
    int ColonyCount,
    double SupportDemandPerDay,
    double LocalSupportCapacityPerDay,
    double ImportRequirementPerDay,
    double LocalSurplusPerDay,
    SupplyCondition Condition,
    bool HasRepresentedInterstellarFreightCorridor = false
);

public sealed record CivilizationLogisticsCoverage(
    int CivilizationId,
    HomeSystemLogisticsNetwork HomeSystem,
    IReadOnlyList<ExternalSystemLogisticsStatus> ExternalSystems
)
{
    public int OwnedSystemCount => 1 + ExternalSystems.Count;
    public int ExternalSystemCount => ExternalSystems.Count;
    public double ExternalImportRequirementPerDay => ExternalSystems.Sum(system => system.ImportRequirementPerDay);
    public double ExternalLocalSurplusPerDay => ExternalSystems.Sum(system => system.LocalSurplusPerDay);

    /// <summary>
    /// Imported support requested by out-system colonies for which the current simulation has
    /// no represented interstellar freight corridor. This is a visible modeling/operational gap,
    /// not silently assumed delivered cargo.
    /// </summary>
    public double UnrepresentedInterstellarSupportPerDay => ExternalSystems
        .Where(system => !system.HasRepresentedInterstellarFreightCorridor)
        .Sum(system => system.ImportRequirementPerDay);

    public bool HasUnrepresentedInterstellarSupportGap => UnrepresentedInterstellarSupportPerDay > 0.000001;
}

public interface ICivilizationLogisticsCoverageView
{
    CivilizationLogisticsCoverage Build(GalaxyState galaxy, int civilizationId);
}

/// <summary>
/// Empire-scale logistics read model layered on the detailed home-system network. Remote colony
/// systems are summarized from authoritative colony logistics only; this adapter deliberately
/// creates no cross-system links until a transport/freight owner can supply real corridors.
/// </summary>
public sealed class PrototypeCivilizationLogisticsCoverageView : ICivilizationLogisticsCoverageView
{
    private readonly IEconomyLogisticsView _logisticsView;
    private readonly IHomeSystemLogisticsNetworkView _homeSystemView;

    public PrototypeCivilizationLogisticsCoverageView(
        IEconomyLogisticsView? logisticsView = null,
        IHomeSystemLogisticsNetworkView? homeSystemView = null)
    {
        _logisticsView = logisticsView ?? new PrototypeEconomyLogisticsView();
        _homeSystemView = homeSystemView ?? new PrototypeHomeSystemLogisticsNetworkView(_logisticsView);
    }

    public CivilizationLogisticsCoverage Build(GalaxyState galaxy, int civilizationId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);

        var civilization = galaxy.Civilizations.FirstOrDefault(candidate => candidate.Id == civilizationId)
            ?? throw new InvalidOperationException($"Unknown civilization {civilizationId}.");
        var logistics = _logisticsView.GetSnapshot(galaxy, civilizationId);
        var logisticsByColony = logistics.Colonies.ToDictionary(colony => colony.ColonyId);
        var home = _homeSystemView.Build(galaxy, civilizationId);

        var externalSystems = galaxy.Colonies
            .Where(colony => colony.CivilizationId == civilizationId && colony.SystemId != civilization.HomeSystemId)
            .GroupBy(colony => colony.SystemId)
            .OrderBy(group => group.Key)
            .Select(group => BuildExternalSystem(civilizationId, group.Key, group, logisticsByColony))
            .ToArray();

        return new CivilizationLogisticsCoverage(civilizationId, home, externalSystems);
    }

    private static ExternalSystemLogisticsStatus BuildExternalSystem(
        int civilizationId,
        int systemId,
        IEnumerable<ColonyState> colonies,
        IReadOnlyDictionary<int, ColonyLogisticsSnapshot> logisticsByColony)
    {
        var colonyArray = colonies.OrderBy(colony => colony.Id).ToArray();
        var snapshots = colonyArray.Select(colony => logisticsByColony[colony.Id]).ToArray();
        var demand = snapshots.Sum(snapshot => snapshot.SupportDemandPerDay);
        var capacity = snapshots.Sum(snapshot => snapshot.LocalSupportCapacityPerDay);
        var imports = snapshots.Sum(snapshot => snapshot.ImportedSupportRequiredPerDay);
        var localSurplus = Math.Max(0.0, capacity - demand);
        var condition = snapshots.Select(snapshot => snapshot.Condition).Aggregate(MoreSevere);

        return new ExternalSystemLogisticsStatus(
            civilizationId,
            systemId,
            colonyArray.Length,
            demand,
            capacity,
            imports,
            localSurplus,
            condition,
            HasRepresentedInterstellarFreightCorridor: false);
    }

    private static SupplyCondition MoreSevere(SupplyCondition left, SupplyCondition right) =>
        Severity(right) > Severity(left) ? right : left;

    private static int Severity(SupplyCondition condition) => condition switch
    {
        SupplyCondition.Critical => 3,
        SupplyCondition.Strained => 2,
        SupplyCondition.Supported => 1,
        _ => 0,
    };
}

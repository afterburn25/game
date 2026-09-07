using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Economy;

/// <summary>
/// Coarse early-release logistics health. These are intentionally broad strategic states,
/// not a per-item freight simulation.
/// </summary>
public enum SupplyCondition
{
    Healthy,
    Strained,
    Critical,
}

/// <summary>
/// Read-only logistics summary for one colony. SupportDemand and LocalSupportCapacity are
/// abstract daily strategic supply units used to express logistics pressure without
/// simulating individual cargo items or people.
/// </summary>
public sealed record ColonyLogisticsSnapshot(
    int ColonyId,
    int SystemId,
    double SupportDemandPerDay,
    double LocalSupportCapacityPerDay,
    double ImportedSupportRequiredPerDay,
    double CoverageRatio,
    SupplyCondition Condition
);

/// <summary>
/// Civilization-wide logistics view intended for UI and strategic AI consumers.
/// Authoritative calculations remain in the economy/logistics subsystem.
/// </summary>
public sealed record CivilizationLogisticsSnapshot(
    int CivilizationId,
    double TotalSupportDemandPerDay,
    double TotalLocalSupportCapacityPerDay,
    double ImportRequirementPerDay,
    double CargoHandlingCapacityPerDay,
    double EffectiveCoverageRatio,
    SupplyCondition Condition,
    IReadOnlyList<ColonyLogisticsSnapshot> Colonies
)
{
    public int CriticalColonyCount => Colonies.Count(colony => colony.Condition == SupplyCondition.Critical);
    public int StrainedColonyCount => Colonies.Count(colony => colony.Condition == SupplyCondition.Strained);
}

public interface IEconomyLogisticsView
{
    CivilizationLogisticsSnapshot GetSnapshot(GalaxyState galaxy, int civilizationId);
}

/// <summary>
/// First playable-release logistics adapter over the existing colony/economy/construction
/// prototype. It deliberately derives a bounded summary instead of introducing a second
/// persistent economy while save-format work is active on other branches.
///
/// Future resource stockpiles, routes, depots and species-specific life-support requirements
/// can replace these prototype formulas behind the same read-only contract.
/// </summary>
public sealed class PrototypeEconomyLogisticsView : IEconomyLogisticsView
{
    private const double MinimumPopulationScale = 0.001;
    private const double OrbitalHandlingBonus = 1.45;
    private const double IndustrialAutomationHandlingBonus = 1.20;

    public CivilizationLogisticsSnapshot GetSnapshot(GalaxyState galaxy, int civilizationId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);

        var economy = galaxy.Economies.FirstOrDefault(candidate => candidate.CivilizationId == civilizationId)
            ?? throw new InvalidOperationException($"Civilization {civilizationId} has no economy state.");
        var construction = galaxy.ConstructionStates.FirstOrDefault(candidate => candidate.CivilizationId == civilizationId)
            ?? throw new InvalidOperationException($"Civilization {civilizationId} has no construction state.");

        var colonySnapshots = new List<ColonyLogisticsSnapshot>();
        double totalDemand = 0.0;
        double totalLocalCapacity = 0.0;
        double totalImportRequirement = 0.0;

        foreach (var colony in galaxy.Colonies)
        {
            if (colony.CivilizationId != civilizationId)
                continue;

            var populationScale = Math.Max(MinimumPopulationScale, colony.PopulationMillions / 1000.0);
            var infrastructure = Math.Clamp(colony.Infrastructure, 0.10, 5.0);
            var stability = Math.Clamp(colony.Stability, 0.10, 1.20);

            // Larger populations require more consumables, maintenance and cargo throughput.
            // Better infrastructure reduces the support burden per unit of population.
            var supportDemand = populationScale * (0.95 + 0.30 / infrastructure);

            // Local capacity represents food/basic consumables/maintenance that can be
            // sustained locally with current infrastructure and social stability.
            var localCapacity = populationScale * 0.72 * infrastructure * stability;
            var importRequirement = Math.Max(0.0, supportDemand - localCapacity);
            var coverage = supportDemand <= 0.0 ? 1.0 : Math.Clamp(localCapacity / supportDemand, 0.0, 2.0);
            var condition = ClassifyCoverage(coverage);

            totalDemand += supportDemand;
            totalLocalCapacity += localCapacity;
            totalImportRequirement += importRequirement;
            colonySnapshots.Add(new ColonyLogisticsSnapshot(
                colony.Id,
                colony.SystemId,
                supportDemand,
                localCapacity,
                importRequirement,
                coverage,
                condition));
        }

        // Cargo handling is intentionally tied to real productive throughput instead of a
        // free empire-wide range stat. Physical orbital infrastructure improves throughput.
        var cargoHandling = Math.Max(0.10, economy.LastIndustryPerSecond * 1.35);
        if (construction.CompletedProjectIds.Contains("orbital_launch_complex"))
            cargoHandling *= OrbitalHandlingBonus;
        if (construction.CompletedProjectIds.Contains("industrial_automation"))
            cargoHandling *= IndustrialAutomationHandlingBonus;

        var coveredImports = Math.Min(totalImportRequirement, cargoHandling);
        var effectiveCoverage = totalDemand <= 0.0
            ? 1.0
            : Math.Clamp((totalLocalCapacity + coveredImports) / totalDemand, 0.0, 2.0);

        return new CivilizationLogisticsSnapshot(
            civilizationId,
            totalDemand,
            totalLocalCapacity,
            totalImportRequirement,
            cargoHandling,
            effectiveCoverage,
            ClassifyCoverage(effectiveCoverage),
            colonySnapshots);
    }

    private static SupplyCondition ClassifyCoverage(double coverageRatio)
    {
        if (coverageRatio < 0.70)
            return SupplyCondition.Critical;
        if (coverageRatio < 0.95)
            return SupplyCondition.Strained;
        return SupplyCondition.Healthy;
    }
}

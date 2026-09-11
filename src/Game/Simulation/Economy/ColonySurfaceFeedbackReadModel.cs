using System;
using System.Linq;
using Game.Simulation.Construction;
using Game.Simulation.Models;

namespace Game.Simulation.Economy;

/// <summary>Read-only explanations of the same finite inventories and proportional site
/// allocation used by surface construction and sustenance simulation.</summary>
public sealed record ColonySustenanceFeedbackSnapshot(
    double EffectiveSupportRatio, string LimitingSupply, bool IsBuffered,
    bool IsDeclining, double FoodDaysUntilDepletion, double WaterDaysUntilDepletion,
    string Status, string RecoveryAction);

public sealed record SurfaceConstructionFeedbackSnapshot(
    double StoredMaterials, double SharedSiteDemand, double ProjectedDailyMaterials,
    double ProjectedSiteDailyMaterials, double MinimumDaysRemaining, bool IsWaitingForMaterials,
    string Status, string RecoveryAction);

public static class ColonySurfaceFeedbackReadModel
{
    public static ColonySustenanceFeedbackSnapshot GetSustenance(GalaxyState galaxy, ColonyState colony,
        ColonySustenanceCapacitySnapshot? capacity = null)
    {
        var support = capacity ?? ColonySustenanceCapacity.GetSnapshot(galaxy, colony);
        var population = Math.Max(.001, colony.PopulationMillions);
        var foodDeficit = Math.Max(0, population - support.FoodCapacityMillions);
        var waterDeficit = Math.Max(0, population - support.WaterCapacityMillions);
        var foodDays = foodDeficit <= .0000001 ? double.PositiveInfinity :
            Math.Max(0, colony.StoredFoodPopulationDaysMillions) / foodDeficit;
        var waterDays = waterDeficit <= .0000001 ? double.PositiveInfinity :
            Math.Max(0, colony.StoredWaterPopulationDaysMillions) / waterDeficit;
        var housingShort = support.HousingCapacityMillions + .0000001 < population;
        var depletedFood = foodDeficit > .0000001 && foodDays <= .0000001;
        var depletedWater = waterDeficit > .0000001 && waterDays <= .0000001;
        // EconomySimulation applies this demographic consequence only to ordinary colonies.
        // Outposts still expose their support constraint, but this observer must not promise a
        // population change the authoritative loop does not make.
        var declining = colony.Kind == SettlementKind.Colony && (housingShort || depletedFood || depletedWater);
        var buffered = !declining && (foodDeficit > .0000001 || waterDeficit > .0000001);
        var effectiveFood = depletedFood ? support.FoodCapacityMillions : population;
        var effectiveWater = depletedWater ? support.WaterCapacityMillions : population;
        var effective = Math.Min(effectiveFood, Math.Min(effectiveWater, support.HousingCapacityMillions));
        var limiter = new[]
        {
            (Name: "food", Value: effectiveFood), (Name: "potable water", Value: effectiveWater),
            (Name: "housing", Value: support.HousingCapacityMillions),
        }.Where(x => Math.Abs(x.Value - effective) <= .001).Select(x => x.Name);
        var limiting = string.Join(" and ", limiter);
        var horizon = Math.Min(foodDays, waterDays);
        var status = colony.Kind == SettlementKind.ResourceOutpost
            ? $"Outpost support constraint: {support.LimitingSupply}; ordinary colony population decline does not apply here."
            : declining
            ? $"Population is declining: {limiting} is below current need."
            : buffered
                ? $"Capacity deficit buffered by reserves; {support.LimitingSupply} limits long-term support."
                : $"Population support stable; {support.LimitingSupply} is the current capacity limit.";
        var action = declining || buffered
            ? support.LimitingSupply.Contains("water", StringComparison.Ordinal) ? "Build Water reclamation, then prioritize its workers and power."
                : support.LimitingSupply.Contains("food", StringComparison.Ordinal) ? "Build Controlled agriculture, then prioritize its workers and power."
                : "Build Habitat complex to raise supported housing."
            : "Keep essential food, water and habitat services powered and staffed.";
        // While buffers still cover a production deficit, the prospective capacity limiter is
        // more useful than the tied immediate supply values (both temporarily meet demand).
        var displayLimiter = buffered ? support.LimitingSupply : limiting;
        return new(Math.Max(0, effective / population), displayLimiter, buffered, declining, foodDays, waterDays, status, action);
    }

    public static SurfaceConstructionFeedbackSnapshot GetConstruction(GalaxyState galaxy, int civilizationId,
        SurfaceBuildingState building, double storedMaterials)
    {
        var definition = SurfaceBuildingCatalog.Find(building.TypeId) ?? throw new ArgumentException("Unknown surface building.");
        var remaining = Math.Max(0, definition.IndustryCost - building.IndustryProgress);
        var sites = galaxy.Colonies.Where(c => c.CivilizationId == civilizationId).SelectMany(c => c.SurfaceBuildings)
            .Where(b => !b.IsComplete).Select(b => Math.Min(SurfaceConstruction.IndustryPerSitePerDay,
                Math.Max(0, SurfaceBuildingCatalog.Find(b.TypeId)!.IndustryCost - b.IndustryProgress))).ToArray();
        var sharedDemand = sites.Sum();
        var siteDemand = Math.Min(SurfaceConstruction.IndustryPerSitePerDay, remaining);
        var projectedDaily = Math.Min(sharedDemand, Math.Max(0, storedMaterials));
        var projectedSite = sharedDemand <= .0000001 ? 0 : projectedDaily * siteDemand / sharedDemand;
        var waiting = remaining > .0000001 && projectedSite <= .0000001;
        var minimumDays = remaining / SurfaceConstruction.IndustryPerSitePerDay;
        var status = waiting ? "Waiting for materials: shared construction allocation has none available."
            : projectedSite + .0000001 < siteDemand ? "Building slowly: materials are shared across active construction sites."
            : "Construction supplied at this site's maximum rate.";
        var action = waiting ? "Recover materials through industry or finish/cancel another authorized site."
            : projectedSite + .0000001 < siteDemand ? "Increase material availability or reduce competing active sites."
            : "Keep material availability steady to meet the minimum time.";
        return new(Math.Max(0, storedMaterials), sharedDemand, projectedDaily, projectedSite, minimumDays, waiting, status, action);
    }
}

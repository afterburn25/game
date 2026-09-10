using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Game.Simulation.Models;
using Game.Simulation.Economy;

namespace Game.Simulation.Construction;

public sealed class SurfaceBuildingState
{
    [JsonRequired] public int Id { get; set; }
    [JsonRequired] public string TypeId { get; set; } = string.Empty;
    [JsonRequired] public float X { get; set; }
    [JsonRequired] public float Z { get; set; }
    [JsonRequired] public float RotationDegrees { get; set; }
    [JsonRequired] public double IndustryProgress { get; set; }
    [JsonRequired] public bool IsComplete { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int OperatingPriority { get; set; }
    public double Condition { get; set; } = 1.0;
}

public sealed record SurfaceBuildingDefinition(string Id, string Name, string Description,
    double IndustryCost, float FootprintRadius, double PowerSupply, double PowerDemand,
    double SciencePerDay, double IndustryPerDay, double CreditCost = 0.0,
    double CreditsPerDay = 0.0, double UpkeepCreditsPerDay = 0.0,
    bool AvailableForPlacement = true, string? UpgradeTypeId = null,
    double UpgradeCreditCost = 0.0, double UpgradeIndustryCost = 0.0,
    double HabitatSupportReduction = 0.0, double FoodCapacityMillions = 0.0,
    double WaterCapacityMillions = 0.0, double HousingCapacityMillions = 0.0,
    double WorkforceRequiredMillions = 0.0, string? UpgradeRequirementId = null,
    string? UpgradeRequirementName = null);

public static class SurfaceBuildingCatalog
{
    public static IReadOnlyList<SurfaceBuildingDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        new SurfaceBuildingDefinition("power_generator", "Power generator", "+4 colony power · 20,000 workers · operating upkeep", 300, 12, 4, 0, 0, 0, 25, 0, .02,
            UpgradeTypeId: "advanced_power_generator", UpgradeCreditCost: 30, UpgradeIndustryCost: 240, WorkforceRequiredMillions: .020,
            UpgradeRequirementId: "fusion_power", UpgradeRequirementName: "Practical Fusion Power"),
        new SurfaceBuildingDefinition("science_lab", "Science lab", "+1 Effective Research Lab · 50,000 workers · uses 2 power · operating upkeep", 400, 15, 0, 2, 1, 0, 40, 0, .04,
            UpgradeTypeId: "advanced_science_lab", UpgradeCreditCost: 50, UpgradeIndustryCost: 320, WorkforceRequiredMillions: .050),
        new SurfaceBuildingDefinition("fabricator", "Fabricator", "+1 industry/day · 40,000 workers · uses 2 power · operating upkeep", 450, 17, 0, 2, 0, 1, 50, 0, .05,
            UpgradeTypeId: "advanced_fabricator", UpgradeCreditCost: 60, UpgradeIndustryCost: 360, WorkforceRequiredMillions: .040,
            UpgradeRequirementId: "additive_manufacturing", UpgradeRequirementName: "Advanced Additive Manufacturing"),
        new SurfaceBuildingDefinition("trade_hub", "Trade hub", "Adds local revenue · 30,000 workers · uses 2 power · operating upkeep", 380, 15, 0, 2, 0, 0, 45, .08, .03,
            UpgradeTypeId: "advanced_trade_hub", UpgradeCreditCost: 55, UpgradeIndustryCost: 300, WorkforceRequiredMillions: .030,
            UpgradeRequirementId: "interplanetary_trade_standards", UpgradeRequirementName: "Interplanetary Trade Standards"),
        new SurfaceBuildingDefinition("habitat_complex", "Habitat complex", "Reduces local life-support cost 20% · 15,000 workers · uses 2 power · operating upkeep", 350, 15, 0, 2, 0, 0, 45, 0, .04,
            UpgradeTypeId: "advanced_habitat_complex", UpgradeCreditCost: 50, UpgradeIndustryCost: 300, HabitatSupportReduction: .20,
            HousingCapacityMillions: 1000.0, WorkforceRequiredMillions: .015,
            UpgradeRequirementId: "closed_loop_recycling", UpgradeRequirementName: "Closed-Loop Recycling"),
        new SurfaceBuildingDefinition("controlled_agriculture", "Controlled agriculture", "+2B food support · 35,000 workers · uses 2 power · operating upkeep", 420, 17, 0, 2, 0, 0, 50, 0, .05,
            FoodCapacityMillions: 2000.0, WorkforceRequiredMillions: .035),
        new SurfaceBuildingDefinition("water_reclamation", "Water reclamation", "+2B potable-water support · 25,000 workers · uses 2 power · operating upkeep", 360, 15, 0, 2, 0, 0, 40, 0, .04,
            WaterCapacityMillions: 2000.0, WorkforceRequiredMillions: .025),
        new SurfaceBuildingDefinition("advanced_power_generator", "Fusion power complex", "+8 colony power · operating upkeep", 300, 12, 8, 0, 0, 0, 55, 0, .04, false, WorkforceRequiredMillions: .035),
        new SurfaceBuildingDefinition("advanced_science_lab", "Advanced science campus", "+2.5 Effective Research Labs · uses 3 power · operating upkeep", 400, 15, 0, 3, 2.5, 0, 90, 0, .08, false, WorkforceRequiredMillions: .080),
        new SurfaceBuildingDefinition("advanced_fabricator", "Automated fabrication arcology", "+2.5 industry/day · uses 3 power · operating upkeep", 450, 17, 0, 3, 0, 2.5, 110, 0, .10, false, WorkforceRequiredMillions: .060),
        new SurfaceBuildingDefinition("advanced_trade_hub", "Interstellar trade exchange", "Adds major local revenue · uses 3 power · operating upkeep", 380, 15, 0, 3, 0, 0, 100, .18, .06, false, WorkforceRequiredMillions: .050),
        new SurfaceBuildingDefinition("advanced_habitat_complex", "Closed-loop habitat arcology", "Reduces local life-support cost 40% · uses 3 power · operating upkeep", 350, 15, 0, 3, 0, 0, 95, 0, .08, false,
            HabitatSupportReduction: .40, HousingCapacityMillions: 3000.0, WorkforceRequiredMillions: .025),
    });

    public static SurfaceBuildingDefinition? Find(string id) => All.FirstOrDefault(item => item.Id == id);
    public static string FunctionalFamily(string id) => id.StartsWith("advanced_", StringComparison.Ordinal)
        ? id["advanced_".Length..] : id;
}

public sealed record SurfaceColonyOutput(double Supply, double Demand, double SciencePerDay,
    double IndustryPerDay, double CreditsPerDay, double UpkeepCreditsPerDay,
    IReadOnlySet<int> PoweredBuildingIds, double HabitatSupportReduction,
    double FoodCapacityMillions, double WaterCapacityMillions, double HousingCapacityMillions,
    double WorkforceAvailableMillions, double WorkforceDemandMillions,
    IReadOnlySet<int> StaffedBuildingIds);
public sealed record SurfaceColonySpecialization(string Id, string Name, string Description,
    int CompletedComplexes, bool Active);

/// <summary>Authoritative free placement and local power. Terrain coordinates are metres within
/// a bounded colony area, independent of stellar coordinates and orbital presentation.</summary>
public static class SurfaceConstruction
{
    public const float AreaHalfSize = 512;
    public const int MaximumBuildings = 64;
    public const float HubRadius = 24;
    public const double IndustryPerSitePerDay = 30;
    public const double WorkforceParticipationRate = .45;
    public const double MinimumOperationalCondition = .15;
    public const double DailyConditionLossAtZeroFunding = .002;
    public const double RepairMaterialFraction = .25;

    public static int GetBuildingCapacity(ColonyState colony) =>
        colony.Kind == SettlementKind.ResourceOutpost ? 8 : colony.SurfaceHubLevel switch
        {
            1 => 16,
            2 => 32,
            _ => MaximumBuildings,
        };

    public static (double CreditCost, double IndustryCost)? GetHubUpgradeCost(
        GalaxyState galaxy, ColonyState colony)
    {
        if (colony.Kind == SettlementKind.ResourceOutpost || colony.SurfaceHubLevel >= 3) return null;
        (double CreditCost, double IndustryCost) baseCost = colony.SurfaceHubLevel == 1
            ? (60.0, 250.0) : (140.0, 600.0);
        var multiplier = GetConstructionCostMultiplier(galaxy, colony);
        return (Math.Round(baseCost.CreditCost * multiplier, 2, MidpointRounding.AwayFromZero),
            Math.Ceiling(baseCost.IndustryCost * multiplier));
    }

    public static double GetConstructionCostMultiplier(GalaxyState galaxy, ColonyState colony)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(colony);
        var body = galaxy.PlanetaryBodies.FirstOrDefault(item => item.Id == colony.PlanetaryBodyId &&
            item.SystemId == colony.SystemId);
        if (body is null) return 1.0;
        var environment = body.Environment;
        var multiplier = 1.0;
        multiplier += Math.Min(0.35, Math.Abs(environment.GravityG - 1.0) * 0.20);
        if (environment.Atmosphere == PlanetaryAtmosphereRegime.Vacuum) multiplier += 0.20;
        if (environment.PressureKPa < 20 || environment.PressureKPa > 300) multiplier += 0.15;
        if (environment.TemperatureKelvin < 240 || environment.TemperatureKelvin > 330) multiplier += 0.15;
        if (environment.RadiationHazard > 0.10)
            multiplier += Math.Min(0.15, (environment.RadiationHazard - 0.10) * 0.30);
        return Math.Round(Math.Clamp(multiplier, 1.0, 2.0), 2, MidpointRounding.AwayFromZero);
    }

    public static double GetAuthorizationCost(GalaxyState galaxy, ColonyState colony,
        SurfaceBuildingDefinition definition) =>
        definition.CreditCost * GetConstructionCostMultiplier(galaxy, colony);

    public static double GetUpgradeAuthorizationCost(GalaxyState galaxy, ColonyState colony,
        SurfaceBuildingDefinition definition) =>
        definition.UpgradeCreditCost * GetConstructionCostMultiplier(galaxy, colony);

    public static string? GetBuildingUpgradeLockReason(GalaxyState galaxy, int civilizationId,
        SurfaceBuildingDefinition definition, IConstructionCapabilityView capabilities) =>
        definition.UpgradeRequirementId is { } requirementId &&
        !capabilities.HasCivilizationCapability(galaxy, civilizationId, requirementId)
            ? $"Research {definition.UpgradeRequirementName ?? requirementId} before authorizing this upgrade."
            : null;

    public static string? GetHubUpgradeLockReason(GalaxyState galaxy, int civilizationId,
        ColonyState colony, IConstructionCapabilityView capabilities)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(colony);
        ArgumentNullException.ThrowIfNull(capabilities);
        if (colony.SurfaceHubLevel == 1)
        {
            var construction = galaxy.ConstructionStates.FirstOrDefault(item => item.CivilizationId == civilizationId);
            if (construction?.CompletedProjectIds.Contains("industrial_automation") != true)
                return "Complete the Industrial Automation Program before expanding this command center.";
        }
        if (colony.SurfaceHubLevel == 2 &&
            !capabilities.HasCivilizationCapability(galaxy, civilizationId, "orbital_industry"))
            return "Establish Orbital Manufacturing before expanding to a level-3 planetary hub.";
        if (colony.SurfaceHubLevel == 2 && colony.PlanetaryBodyId is int bodyId)
        {
            var body = galaxy.PlanetaryBodies.FirstOrDefault(item => item.Id == bodyId && item.SystemId == colony.SystemId);
            if (body is not null && body.RadiusEarth < 0.35)
                return $"{body.Name} is too small for a 64-module regional hub. Keep this settlement at level 2 or expand through orbital infrastructure.";
        }
        return null;
    }

    public static ConstructionOrderResult UpgradeHub(GalaxyState galaxy, int civilizationId, int colonyId,
        IConstructionCapabilityView capabilities)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        var colony = galaxy.Colonies.FirstOrDefault(item => item.Id == colonyId && item.CivilizationId == civilizationId);
        if (colony is null) return new(false, "You can upgrade only a colony you own.");
        var cost = GetHubUpgradeCost(galaxy, colony);
        if (cost is null)
            return new(false, colony.Kind == SettlementKind.ResourceOutpost
                ? "A sealed resource outpost must be terraformed before it can become a full colony command center."
                : "This planetary hub is already at maximum capacity.");
        var lockReason = GetHubUpgradeLockReason(galaxy, civilizationId, colony, capabilities);
        if (lockReason is not null) return new(false, lockReason);
        var economy = galaxy.Economies.FirstOrDefault(item => item.CivilizationId == civilizationId);
        if (economy is null) return new(false, "The colony has no construction economy.");
        var currency = SovereignCurrencyCatalog.ForCivilization(galaxy, civilizationId);
        if (economy.Credits + 0.0001 < cost.Value.CreditCost || economy.Industry + 0.0001 < cost.Value.IndustryCost)
            return new(false, $"Hub expansion requires {currency.Format(cost.Value.CreditCost)} and {cost.Value.IndustryCost:N0} materials.");

        economy.Credits -= cost.Value.CreditCost;
        economy.Industry -= cost.Value.IndustryCost;
        colony.SurfaceHubLevel++;
        return new(true, $"Surface administration upgraded to level {colony.SurfaceHubLevel}; module capacity is now {GetBuildingCapacity(colony)}.");
    }

    public static bool IsAvailableForSettlement(ColonyState colony, SurfaceBuildingDefinition definition) =>
        definition.AvailableForPlacement &&
        (colony.Kind != SettlementKind.ResourceOutpost || definition.Id != "trade_hub");

    public static float TerrainHeight(float x, float z)
    {
        var distance = MathF.Sqrt(x * x + z * z);
        var rise = Math.Clamp((distance - 190) / 280, 0, 1);
        return rise * (18 + 12 * MathF.Sin(x * .012f) * MathF.Cos(z * .014f))
            + 1.4f * MathF.Sin(x * .025f) * MathF.Sin(z * .019f);
    }

    public static string? PlacementError(IEnumerable<SurfaceBuildingState> buildings,
        string typeId, float x, float z, float rotationDegrees)
    {
        var definition = SurfaceBuildingCatalog.Find(typeId);
        if (definition is null) return "Unknown building type.";
        if (!float.IsFinite(x) || !float.IsFinite(z) || !float.IsFinite(rotationDegrees))
            return "Building position and rotation must be finite.";
        var radius = definition.FootprintRadius;
        if (Math.Abs(x) + radius > AreaHalfSize || Math.Abs(z) + radius > AreaHalfSize)
            return "Choose a position inside the colony boundary.";
        if (x * x + z * z < MathF.Pow(HubRadius + radius + 3, 2))
            return "Leave room around the colony hub.";
        var slopeX = Math.Abs(TerrainHeight(x + radius, z) - TerrainHeight(x - radius, z)) / (2 * radius);
        var slopeZ = Math.Abs(TerrainHeight(x, z + radius) - TerrainHeight(x, z - radius)) / (2 * radius);
        if (Math.Max(slopeX, slopeZ) > .28f) return "This slope is too steep. Choose flatter ground.";
        var count = 0;
        foreach (var building in buildings)
        {
            count++;
            var other = SurfaceBuildingCatalog.Find(building.TypeId);
            if (other is null) return "The colony contains an unknown building type.";
            var dx = building.X - x;
            var dz = building.Z - z;
            if (dx * dx + dz * dz < MathF.Pow(radius + other.FootprintRadius + 3, 2))
                return "That position overlaps another building or construction site.";
        }
        return count >= MaximumBuildings ? "This demo colony has reached its 64-building limit." : null;
    }

    public static ConstructionOrderResult Place(GalaxyState galaxy, int civilizationId, int colonyId,
        string typeId, float x, float z, float rotationDegrees)
    {
        var colony = galaxy.Colonies.FirstOrDefault(item => item.Id == colonyId && item.CivilizationId == civilizationId);
        if (colony is null) return new(false, "You can build only in a colony you own.");
        var body = galaxy.PlanetaryBodies.FirstOrDefault(item => item.Id == colony.PlanetaryBodyId && item.SystemId == colony.SystemId);
        if (body is null || !body.Environment.HasSolidSurface)
            return new(false, "A surveyed colony on a solid planetary surface is required.");
        if (!galaxy.Economies.Any(item => item.CivilizationId == civilizationId))
            return new(false, "The colony has no construction economy.");
        var definition = SurfaceBuildingCatalog.Find(typeId);
        if (definition is null || !IsAvailableForSettlement(colony, definition))
        {
            if (colony.Kind == SettlementKind.ResourceOutpost && definition?.Id == "trade_hub")
                return new(false, "A sealed resource outpost cannot support a civilian trade hub. Deliver extracted material by freighter.");
            return new(false, "That building type is available only as an upgrade.");
        }
        var capacity = GetBuildingCapacity(colony);
        if (colony.SurfaceBuildings.Count >= capacity)
            return new(false, $"This settlement hub has reached its {capacity}-module capacity.");
        var error = PlacementError(colony.SurfaceBuildings, typeId, x, z, rotationDegrees);
        if (error is not null) return new(false, error);
        var economy = galaxy.Economies.First(item => item.CivilizationId == civilizationId);
        var currency = SovereignCurrencyCatalog.ForCivilization(galaxy, civilizationId);
        var authorizationCost = GetAuthorizationCost(galaxy, colony, definition!);
        if (economy.Credits + 0.0001 < authorizationCost)
            return new(false, $"{currency.Format(authorizationCost)} is required to authorize this {definition.Name} here.");
        var nextId = colony.SurfaceBuildings.Count == 0 ? 1 : colony.SurfaceBuildings.Max(item => item.Id) + 1;
        if (nextId <= 0) return new(false, "No building identifier is available.");
        economy.Credits -= authorizationCost;
        colony.SurfaceBuildings.Add(new SurfaceBuildingState
        {
            Id = nextId, TypeId = typeId, X = x, Z = z,
            RotationDegrees = ((rotationDegrees % 360) + 360) % 360,
        });
        return new(true, $"{definition.Name} placed and authorized for {currency.Format(authorizationCost)}. Construction uses available materials.");
    }

    public static ConstructionOrderResult Remove(GalaxyState galaxy, int civilizationId, int colonyId, int buildingId)
    {
        var colony = galaxy.Colonies.FirstOrDefault(item => item.Id == colonyId && item.CivilizationId == civilizationId);
        if (colony is null) return new(false, "You can remove buildings only from a colony you own.");
        var building = colony.SurfaceBuildings.FirstOrDefault(item => item.Id == buildingId);
        if (building is null) return new(false, "That surface building no longer exists.");
        var definition = SurfaceBuildingCatalog.Find(building.TypeId);
        if (definition is null) return new(false, "That surface building has an unknown type and cannot be removed safely.");
        var economy = galaxy.Economies.FirstOrDefault(item => item.CivilizationId == civilizationId);
        if (economy is null) return new(false, "The colony has no construction economy.");

        colony.SurfaceBuildings.Remove(building);
        if (building.IsComplete)
            return new(true, $"{definition.Name} demolished. Its power use and production have stopped.");

        var refund = GetAuthorizationCost(galaxy, colony, definition) * 0.5;
        economy.Credits += refund;
        return new(true, $"{definition.Name} construction cancelled. {SovereignCurrencyCatalog.ForCivilization(galaxy, civilizationId).Format(refund)} was recovered; spent industry was not recoverable.");
    }

    public static ConstructionOrderResult Upgrade(GalaxyState galaxy, int civilizationId, int colonyId, int buildingId,
        IConstructionCapabilityView capabilities)
    {
        var colony = galaxy.Colonies.FirstOrDefault(item => item.Id == colonyId && item.CivilizationId == civilizationId);
        if (colony is null) return new(false, "You can upgrade buildings only in a colony you own.");
        var building = colony.SurfaceBuildings.FirstOrDefault(item => item.Id == buildingId);
        if (building is null) return new(false, "That surface building no longer exists.");
        if (!building.IsComplete) return new(false, "Complete construction before upgrading this building.");
        var current = SurfaceBuildingCatalog.Find(building.TypeId);
        var upgrade = current?.UpgradeTypeId is null ? null : SurfaceBuildingCatalog.Find(current.UpgradeTypeId);
        if (current is null || upgrade is null) return new(false, "This building has no further upgrade available.");
        var lockReason = GetBuildingUpgradeLockReason(galaxy, civilizationId, current, capabilities);
        if (lockReason is not null) return new(false, lockReason);
        var economy = galaxy.Economies.FirstOrDefault(item => item.CivilizationId == civilizationId);
        if (economy is null) return new(false, "The colony has no construction economy.");
        var upgradeCreditCost = GetUpgradeAuthorizationCost(galaxy, colony, current);
        if (economy.Credits + 0.0001 < upgradeCreditCost || economy.Industry + 0.0001 < current.UpgradeIndustryCost)
            return new(false, $"Upgrading to {upgrade.Name} requires {SovereignCurrencyCatalog.ForCivilization(galaxy, civilizationId).Format(upgradeCreditCost)} and {current.UpgradeIndustryCost:N0} available materials.");

        economy.Credits -= upgradeCreditCost;
        economy.Industry -= current.UpgradeIndustryCost;
        building.TypeId = upgrade.Id;
        building.IndustryProgress = upgrade.IndustryCost;
        building.IsComplete = true;
        building.Condition = 1.0;
        return new(true, $"{upgrade.Name} is operational. Upgrade consumed {SovereignCurrencyCatalog.ForCivilization(galaxy, civilizationId).Format(upgradeCreditCost)} and {current.UpgradeIndustryCost:N0} materials.");
    }

    public static double GetRepairIndustryCost(SurfaceBuildingState building)
    {
        ArgumentNullException.ThrowIfNull(building);
        var definition = SurfaceBuildingCatalog.Find(building.TypeId);
        if (definition is null || !building.IsComplete || building.Condition >= 1.0 - .0000001) return 0.0;
        return Math.Max(1.0, Math.Ceiling(definition.IndustryCost * RepairMaterialFraction * (1.0 - building.Condition)));
    }

    public static ConstructionOrderResult Repair(
        GalaxyState galaxy, int civilizationId, int colonyId, int buildingId)
    {
        var colony = galaxy.Colonies.FirstOrDefault(item => item.Id == colonyId && item.CivilizationId == civilizationId);
        if (colony is null) return new(false, "You can repair buildings only in a colony you own.");
        var building = colony.SurfaceBuildings.FirstOrDefault(item => item.Id == buildingId);
        if (building is null) return new(false, "That surface building no longer exists.");
        if (!building.IsComplete) return new(false, "Complete construction before repairing this building.");
        var definition = SurfaceBuildingCatalog.Find(building.TypeId);
        if (definition is null) return new(false, "That surface building has an unknown type and cannot be repaired safely.");
        var cost = GetRepairIndustryCost(building);
        if (cost <= 0) return new(false, $"{definition.Name} is already at full condition.");
        var economy = galaxy.Economies.FirstOrDefault(item => item.CivilizationId == civilizationId);
        if (economy is null) return new(false, "The colony has no maintenance economy.");
        if (economy.Industry + .0001 < cost)
            return new(false, $"Repairing {definition.Name} requires {cost:N0} stored materials; {economy.Industry:N0} are available.");
        economy.Industry -= cost;
        building.Condition = 1.0;
        return new(true, $"{definition.Name} restored to full condition using {cost:N0} materials.");
    }

    public static void AdvanceCondition(ColonyState colony, double fundingFraction, double simulationDays)
    {
        ArgumentNullException.ThrowIfNull(colony);
        if (!double.IsFinite(fundingFraction) || fundingFraction is < 0.0 or > 1.0 ||
            !double.IsFinite(simulationDays) || simulationDays < 0.0)
            throw new ArgumentOutOfRangeException(nameof(fundingFraction),
                "Surface maintenance requires finite elapsed days and a funding fraction from zero to one.");
        if (simulationDays <= 0.0 || fundingFraction >= 1.0 - .0000001) return;
        var loss = DailyConditionLossAtZeroFunding * (1.0 - fundingFraction) * simulationDays;
        foreach (var building in colony.SurfaceBuildings.Where(item => item.IsComplete && item.IsEnabled))
            building.Condition = Math.Max(0.0, building.Condition - loss);
    }

    public static ConstructionOrderResult SetEnabled(
        GalaxyState galaxy, int civilizationId, int colonyId, int buildingId, bool enabled)
    {
        var colony = galaxy.Colonies.FirstOrDefault(item => item.Id == colonyId && item.CivilizationId == civilizationId);
        if (colony is null) return new(false, "You can manage buildings only in a colony you own.");
        var building = colony.SurfaceBuildings.FirstOrDefault(item => item.Id == buildingId);
        if (building is null) return new(false, "That surface building no longer exists.");
        if (!building.IsComplete) return new(false, "Complete construction before changing operating status.");
        var definition = SurfaceBuildingCatalog.Find(building.TypeId);
        if (definition is null) return new(false, "That surface building has an unknown type and cannot be managed safely.");
        if (building.IsEnabled == enabled)
            return new(false, $"{definition.Name} is already {(enabled ? "operating" : "shut down")}.");
        building.IsEnabled = enabled;
        return new(true, enabled
            ? $"{definition.Name} restarted. Staffing, power demand, output and upkeep resume when capacity is available."
            : $"{definition.Name} shut down. Its staffing, power demand, output and upkeep are suspended.");
    }

    public static ConstructionOrderResult SetOperatingPriority(
        GalaxyState galaxy, int civilizationId, int colonyId, int buildingId, bool prioritized)
    {
        var colony = galaxy.Colonies.FirstOrDefault(item => item.Id == colonyId && item.CivilizationId == civilizationId);
        if (colony is null) return new(false, "You can prioritize buildings only in a colony you own.");
        var building = colony.SurfaceBuildings.FirstOrDefault(item => item.Id == buildingId);
        if (building is null) return new(false, "That surface building no longer exists.");
        if (!building.IsComplete) return new(false, "Complete construction before assigning operating priority.");
        var desired = prioritized ? 1 : 0;
        if (building.OperatingPriority == desired)
            return new(false, $"This building already has {(prioritized ? "priority" : "normal priority")}.");
        building.OperatingPriority = desired;
        var name = SurfaceBuildingCatalog.Find(building.TypeId)?.Name ?? "Surface building";
        return new(true, prioritized
            ? $"{name} prioritized. It receives available workers and power before normal buildings."
            : $"{name} returned to normal operating priority.");
    }

    public static SurfaceColonyOutput GetOutput(ColonyState colony)
    {
        double supply = 2, demand = 0, science = 0, industry = 0, credits = 0, upkeep = 0, habitatReduction = 0;
        double foodCapacity = 0, waterCapacity = 0, housingCapacity = 0;
        var completed = colony.SurfaceBuildings.Where(item => item.IsComplete && item.IsEnabled &&
                item.Condition > MinimumOperationalCondition)
            .OrderByDescending(item => item.OperatingPriority).ThenBy(item => item.Id).ToArray();
        var specialization = GetSpecialization(colony);
        var workforceAvailable = Math.Max(0.0, colony.PopulationMillions * WorkforceParticipationRate);
        var workforceRemaining = workforceAvailable;
        var workforceDemand = 0.0;
        var staffed = new HashSet<int>();
        foreach (var building in completed)
        {
            var definition = SurfaceBuildingCatalog.Find(building.TypeId)!;
            var efficiency = .5 + .5 * building.Condition;
            workforceDemand += definition.WorkforceRequiredMillions;
            upkeep += definition.UpkeepCreditsPerDay;
            if (definition.WorkforceRequiredMillions > workforceRemaining + 0.0000001) continue;
            workforceRemaining -= definition.WorkforceRequiredMillions;
            staffed.Add(building.Id);
            supply += definition.PowerSupply * efficiency *
                (specialization.Active && specialization.Id == "power_generator" ? 1.25 : 1);
            demand += definition.PowerDemand;
        }
        var available = supply;
        var powered = new HashSet<int>();
        foreach (var building in completed)
        {
            var definition = SurfaceBuildingCatalog.Find(building.TypeId)!;
            var efficiency = .5 + .5 * building.Condition;
            if (!staffed.Contains(building.Id)) continue;
            if (definition.PowerDemand > available) continue;
            available -= definition.PowerDemand;
            powered.Add(building.Id);
            science += definition.SciencePerDay * efficiency;
            industry += definition.IndustryPerDay * efficiency;
            credits += definition.CreditsPerDay * efficiency;
            habitatReduction += definition.HabitatSupportReduction * efficiency;
            foodCapacity += definition.FoodCapacityMillions * efficiency;
            waterCapacity += definition.WaterCapacityMillions * efficiency;
            housingCapacity += definition.HousingCapacityMillions * efficiency;
        }
        if (specialization.Active)
        {
            if (specialization.Id == "science_lab") science *= 1.25;
            if (specialization.Id == "fabricator") industry *= 1.25;
            if (specialization.Id == "trade_hub") credits *= 1.25;
        }
        return new(supply, demand, science, industry, credits, upkeep, powered,
            Math.Min(.75, habitatReduction), foodCapacity, waterCapacity, housingCapacity,
            workforceAvailable, workforceDemand, staffed);
    }

    public static SurfaceColonySpecialization GetSpecialization(ColonyState colony)
    {
        var families = new[]
        {
            (Id: "science_lab", Name: "Research district", Output: "research capacity"),
            (Id: "fabricator", Name: "Industrial district", Output: "industry"),
            (Id: "trade_hub", Name: "Commercial district", Output: "trade revenue"),
            (Id: "power_generator", Name: "Energy district", Output: "generator supply"),
        };
        var selected = families.Select((family, priority) => new
            {
                family.Id, family.Name, family.Output, Priority = priority,
                Count = colony.SurfaceBuildings.Count(building => building.IsComplete && building.IsEnabled &&
                    building.Condition > MinimumOperationalCondition &&
                    SurfaceBuildingCatalog.FunctionalFamily(building.TypeId) == family.Id),
            })
            .OrderByDescending(item => item.Count).ThenBy(item => item.Priority).First();
        if (selected.Count == 0)
            return new("general", "General settlement", "Complete matching complexes to develop a specialized district.", 0, false);
        var active = selected.Count >= 3;
        return new(selected.Id, selected.Name,
            active ? $"Active · +25% {selected.Output} from the completed district."
                : $"Developing · {selected.Count}/3 matching completed complexes.", selected.Count, active);
    }

    public static double GetIndustryDemand(GalaxyState galaxy, int civilizationId, double simulationDays = double.PositiveInfinity) =>
        galaxy.Colonies.Where(colony => colony.CivilizationId == civilizationId)
            .SelectMany(colony => colony.SurfaceBuildings).Where(building => !building.IsComplete)
            .Sum(building => Math.Min(IndustryPerSitePerDay * Math.Max(0, simulationDays),
                Math.Max(0, SurfaceBuildingCatalog.Find(building.TypeId)!.IndustryCost - building.IndustryProgress)));

    /// <summary>Consumes only the budget already reserved for surface construction; no separate
    /// draw on shipbuilding's allocation. Proportional progress gives every placed site a share.</summary>
    public static void Advance(GalaxyState galaxy, int civilizationId, double budget, double simulationDays = 1)
    {
        if (!double.IsFinite(budget) || budget < 0 || !double.IsFinite(simulationDays) || simulationDays < 0)
            throw new ArgumentOutOfRangeException(nameof(budget), "Surface construction requires finite nonnegative industry and elapsed days.");
        if (budget <= 0) return;
        var economy = galaxy.Economies.First(item => item.CivilizationId == civilizationId);
        var demand = GetIndustryDemand(galaxy, civilizationId, simulationDays);
        if (demand <= 0) return;
        var available = Math.Min(demand, Math.Min(economy.Industry, budget));
        double spent = 0;
        foreach (var building in galaxy.Colonies.Where(colony => colony.CivilizationId == civilizationId)
                     .OrderBy(colony => colony.Id).SelectMany(colony => colony.SurfaceBuildings.OrderBy(item => item.Id)))
        {
            if (building.IsComplete) continue;
            var cost = SurfaceBuildingCatalog.Find(building.TypeId)!.IndustryCost;
            var remaining = Math.Max(0, cost - building.IndustryProgress);
            var siteDemand = Math.Min(remaining, IndustryPerSitePerDay * simulationDays);
            var spend = Math.Min(siteDemand, available * (siteDemand / demand));
            building.IndustryProgress += spend;
            spent += spend;
            if (building.IndustryProgress + .0000001 >= cost)
            {
                building.IndustryProgress = cost;
                building.IsComplete = true;
            }
        }
        economy.Industry = Math.Max(0, economy.Industry - spent);
    }

    public static void Validate(ColonyState colony)
    {
        if (colony.SurfaceBuildings is null) throw new InvalidDataException($"Colony {colony.Id} has a null surface building collection.");
        var accepted = new List<SurfaceBuildingState>();
        var ids = new HashSet<int>();
        foreach (var building in colony.SurfaceBuildings)
        {
            if (building is null || building.Id <= 0 || !ids.Add(building.Id))
                throw new InvalidDataException($"Colony {colony.Id} has a missing or duplicate surface building identifier.");
            var error = PlacementError(accepted, building.TypeId, building.X, building.Z, building.RotationDegrees);
            if (error is not null) throw new InvalidDataException($"Colony {colony.Id}, surface building {building.Id}: {error}");
            var cost = SurfaceBuildingCatalog.Find(building.TypeId)!.IndustryCost;
            if (!double.IsFinite(building.IndustryProgress) || building.IndustryProgress < 0 || building.IndustryProgress > cost ||
                building.IsComplete != (building.IndustryProgress >= cost))
                throw new InvalidDataException($"Colony {colony.Id}, surface building {building.Id} has inconsistent construction progress.");
            if (building.OperatingPriority is < 0 or > 1)
                throw new InvalidDataException($"Colony {colony.Id}, surface building {building.Id} has an invalid operating priority.");
            if (!double.IsFinite(building.Condition) || building.Condition is < 0.0 or > 1.0)
                throw new InvalidDataException($"Colony {colony.Id}, surface building {building.Id} has invalid physical condition.");
            accepted.Add(building);
        }
    }
}

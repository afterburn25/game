using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Game.Simulation.Models;

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
}

public sealed record SurfaceBuildingDefinition(string Id, string Name, string Description,
    double IndustryCost, float FootprintRadius, double PowerSupply, double PowerDemand,
    double SciencePerDay, double IndustryPerDay, double CreditCost = 0.0,
    double CreditsPerDay = 0.0, double UpkeepCreditsPerDay = 0.0,
    bool AvailableForPlacement = true, string? UpgradeTypeId = null,
    double UpgradeCreditCost = 0.0, double UpgradeIndustryCost = 0.0,
    double HabitatSupportReduction = 0.0);

public static class SurfaceBuildingCatalog
{
    public static IReadOnlyList<SurfaceBuildingDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        new SurfaceBuildingDefinition("power_generator", "Power generator", "+4 colony power · 0.02 C/day upkeep", 300, 12, 4, 0, 0, 0, 25, 0, .02,
            UpgradeTypeId: "advanced_power_generator", UpgradeCreditCost: 30, UpgradeIndustryCost: 240),
        new SurfaceBuildingDefinition("science_lab", "Science lab", "+1 science/day · uses 2 power · 0.04 C/day upkeep", 400, 15, 0, 2, 1, 0, 40, 0, .04,
            UpgradeTypeId: "advanced_science_lab", UpgradeCreditCost: 50, UpgradeIndustryCost: 320),
        new SurfaceBuildingDefinition("fabricator", "Fabricator", "+1 industry/day · uses 2 power · 0.05 C/day upkeep", 450, 17, 0, 2, 0, 1, 50, 0, .05,
            UpgradeTypeId: "advanced_fabricator", UpgradeCreditCost: 60, UpgradeIndustryCost: 360),
        new SurfaceBuildingDefinition("trade_hub", "Trade hub", "+0.08 credits/day · uses 2 power · 0.03 C/day upkeep", 380, 15, 0, 2, 0, 0, 45, .08, .03,
            UpgradeTypeId: "advanced_trade_hub", UpgradeCreditCost: 55, UpgradeIndustryCost: 300),
        new SurfaceBuildingDefinition("habitat_complex", "Habitat complex", "Reduces local life-support cost 20% · uses 2 power · 0.04 C/day upkeep", 350, 15, 0, 2, 0, 0, 45, 0, .04,
            UpgradeTypeId: "advanced_habitat_complex", UpgradeCreditCost: 50, UpgradeIndustryCost: 300, HabitatSupportReduction: .20),
        new SurfaceBuildingDefinition("advanced_power_generator", "Fusion power complex", "+8 colony power · 0.04 C/day upkeep", 300, 12, 8, 0, 0, 0, 55, 0, .04, false),
        new SurfaceBuildingDefinition("advanced_science_lab", "Advanced science campus", "+2.5 science/day · uses 3 power · 0.08 C/day upkeep", 400, 15, 0, 3, 2.5, 0, 90, 0, .08, false),
        new SurfaceBuildingDefinition("advanced_fabricator", "Automated fabrication arcology", "+2.5 industry/day · uses 3 power · 0.10 C/day upkeep", 450, 17, 0, 3, 0, 2.5, 110, 0, .10, false),
        new SurfaceBuildingDefinition("advanced_trade_hub", "Interstellar trade exchange", "+0.18 credits/day · uses 3 power · 0.06 C/day upkeep", 380, 15, 0, 3, 0, 0, 100, .18, .06, false),
        new SurfaceBuildingDefinition("advanced_habitat_complex", "Closed-loop habitat arcology", "Reduces local life-support cost 40% · uses 3 power · 0.08 C/day upkeep", 350, 15, 0, 3, 0, 0, 95, 0, .08, false,
            HabitatSupportReduction: .40),
    });

    public static SurfaceBuildingDefinition? Find(string id) => All.FirstOrDefault(item => item.Id == id);
    public static string FunctionalFamily(string id) => id.StartsWith("advanced_", StringComparison.Ordinal)
        ? id["advanced_".Length..] : id;
}

public sealed record SurfaceColonyOutput(double Supply, double Demand, double SciencePerDay,
    double IndustryPerDay, double CreditsPerDay, double UpkeepCreditsPerDay,
    IReadOnlySet<int> PoweredBuildingIds, double HabitatSupportReduction);
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
        if (definition?.AvailableForPlacement != true)
            return new(false, "That building type is available only as an upgrade.");
        var error = PlacementError(colony.SurfaceBuildings, typeId, x, z, rotationDegrees);
        if (error is not null) return new(false, error);
        var economy = galaxy.Economies.First(item => item.CivilizationId == civilizationId);
        if (economy.Credits + 0.0001 < definition!.CreditCost)
            return new(false, $"{definition.CreditCost:N0} credits are required to authorize this {definition.Name}.");
        var nextId = colony.SurfaceBuildings.Count == 0 ? 1 : colony.SurfaceBuildings.Max(item => item.Id) + 1;
        if (nextId <= 0) return new(false, "No building identifier is available.");
        economy.Credits -= definition.CreditCost;
        colony.SurfaceBuildings.Add(new SurfaceBuildingState
        {
            Id = nextId, TypeId = typeId, X = x, Z = z,
            RotationDegrees = ((rotationDegrees % 360) + 360) % 360,
        });
        return new(true, $"{definition.Name} placed and authorized for {definition.CreditCost:N0} credits. Construction uses available industry.");
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

        var refund = definition.CreditCost * 0.5;
        economy.Credits += refund;
        return new(true, $"{definition.Name} construction cancelled. {refund:N1} credits were recovered; spent industry was not recoverable.");
    }

    public static ConstructionOrderResult Upgrade(GalaxyState galaxy, int civilizationId, int colonyId, int buildingId)
    {
        var colony = galaxy.Colonies.FirstOrDefault(item => item.Id == colonyId && item.CivilizationId == civilizationId);
        if (colony is null) return new(false, "You can upgrade buildings only in a colony you own.");
        var building = colony.SurfaceBuildings.FirstOrDefault(item => item.Id == buildingId);
        if (building is null) return new(false, "That surface building no longer exists.");
        if (!building.IsComplete) return new(false, "Complete construction before upgrading this building.");
        var current = SurfaceBuildingCatalog.Find(building.TypeId);
        var upgrade = current?.UpgradeTypeId is null ? null : SurfaceBuildingCatalog.Find(current.UpgradeTypeId);
        if (current is null || upgrade is null) return new(false, "This building has no further upgrade available.");
        var economy = galaxy.Economies.FirstOrDefault(item => item.CivilizationId == civilizationId);
        if (economy is null) return new(false, "The colony has no construction economy.");
        if (economy.Credits + 0.0001 < current.UpgradeCreditCost || economy.Industry + 0.0001 < current.UpgradeIndustryCost)
            return new(false, $"Upgrading to {upgrade.Name} requires {current.UpgradeCreditCost:N0} credits and {current.UpgradeIndustryCost:N0} available industry.");

        economy.Credits -= current.UpgradeCreditCost;
        economy.Industry -= current.UpgradeIndustryCost;
        building.TypeId = upgrade.Id;
        building.IndustryProgress = upgrade.IndustryCost;
        building.IsComplete = true;
        return new(true, $"{upgrade.Name} is operational. Upgrade consumed {current.UpgradeCreditCost:N0} credits and {current.UpgradeIndustryCost:N0} industry.");
    }

    public static SurfaceColonyOutput GetOutput(ColonyState colony)
    {
        double supply = 2, demand = 0, science = 0, industry = 0, credits = 0, upkeep = 0, habitatReduction = 0;
        var completed = colony.SurfaceBuildings.Where(item => item.IsComplete).OrderBy(item => item.Id).ToArray();
        var specialization = GetSpecialization(colony);
        foreach (var building in completed)
        {
            var definition = SurfaceBuildingCatalog.Find(building.TypeId)!;
            supply += definition.PowerSupply * (specialization.Active && specialization.Id == "power_generator" ? 1.25 : 1);
            demand += definition.PowerDemand;
            upkeep += definition.UpkeepCreditsPerDay;
        }
        var available = supply;
        var powered = new HashSet<int>();
        foreach (var building in completed)
        {
            var definition = SurfaceBuildingCatalog.Find(building.TypeId)!;
            if (definition.PowerDemand > available) continue;
            available -= definition.PowerDemand;
            powered.Add(building.Id);
            science += definition.SciencePerDay;
            industry += definition.IndustryPerDay;
            credits += definition.CreditsPerDay;
            habitatReduction += definition.HabitatSupportReduction;
        }
        if (specialization.Active)
        {
            if (specialization.Id == "science_lab") science *= 1.25;
            if (specialization.Id == "fabricator") industry *= 1.25;
            if (specialization.Id == "trade_hub") credits *= 1.25;
        }
        return new(supply, demand, science, industry, credits, upkeep, powered, Math.Min(.75, habitatReduction));
    }

    public static SurfaceColonySpecialization GetSpecialization(ColonyState colony)
    {
        var families = new[]
        {
            (Id: "science_lab", Name: "Research district", Output: "science"),
            (Id: "fabricator", Name: "Industrial district", Output: "industry"),
            (Id: "trade_hub", Name: "Commercial district", Output: "trade revenue"),
            (Id: "power_generator", Name: "Energy district", Output: "generator supply"),
        };
        var selected = families.Select((family, priority) => new
            {
                family.Id, family.Name, family.Output, Priority = priority,
                Count = colony.SurfaceBuildings.Count(building => building.IsComplete &&
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
            accepted.Add(building);
        }
    }
}

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
    double SciencePerDay, double IndustryPerDay, double CreditCost = 0.0);

public static class SurfaceBuildingCatalog
{
    public static IReadOnlyList<SurfaceBuildingDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        new SurfaceBuildingDefinition("power_generator", "Power generator", "+4 colony power", 300, 12, 4, 0, 0, 0, 25),
        new SurfaceBuildingDefinition("science_lab", "Science lab", "+1 science/day · uses 2 power", 400, 15, 0, 2, 1, 0, 40),
        new SurfaceBuildingDefinition("fabricator", "Fabricator", "+1 industry/day · uses 2 power", 450, 17, 0, 2, 0, 1, 50),
    });

    public static SurfaceBuildingDefinition? Find(string id) => All.FirstOrDefault(item => item.Id == id);
}

public sealed record SurfaceColonyOutput(double Supply, double Demand, double SciencePerDay,
    double IndustryPerDay, IReadOnlySet<int> PoweredBuildingIds);

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

    public static SurfaceColonyOutput GetOutput(ColonyState colony)
    {
        double supply = 2, demand = 0, science = 0, industry = 0;
        var completed = colony.SurfaceBuildings.Where(item => item.IsComplete).OrderBy(item => item.Id).ToArray();
        foreach (var building in completed)
        {
            var definition = SurfaceBuildingCatalog.Find(building.TypeId)!;
            supply += definition.PowerSupply;
            demand += definition.PowerDemand;
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
        }
        return new(supply, demand, science, industry, powered);
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

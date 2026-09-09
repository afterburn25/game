using System;
using System.Linq;
using Game.Simulation.Construction;
using Game.Simulation.Models;

namespace Game.Presentation;

/// <summary>Read-only what-if using the same power allocation and district rules as construction.
/// Pending neighbours remain pending. This is not a promised completion date or an order.</summary>
public sealed record SurfaceOrderPreview(double CreditCost, double IndustryCost, double MinimumDays,
    double Supply, double Demand, bool Powered, double HabitatReduction, string ResultName)
{
    public double PowerBalance => Supply - Demand;

    public static SurfaceOrderPreview? Create(UiSurfaceSnapshot snapshot, string typeId, int? upgradeId = null)
    {
        var definition = SurfaceBuildingCatalog.Find(typeId);
        if (definition is null) return null;
        var colony = new ColonyState { Id = snapshot.ColonyId, CivilizationId = 0, SystemId = 0, Name = snapshot.ColonyName };
        foreach (var building in snapshot.Buildings)
            colony.SurfaceBuildings.Add(new SurfaceBuildingState
            {
                Id = building.Id, TypeId = building.TypeId, X = building.X, Z = building.Z,
                RotationDegrees = building.RotationDegrees, IndustryProgress = building.Progress * building.Cost,
                IsComplete = building.Complete,
            });
        var credits = definition.CreditCost;
        var industry = definition.IndustryCost;
        var result = definition;
        int id;
        if (upgradeId is int existingId)
        {
            var existing = colony.SurfaceBuildings.FirstOrDefault(item => item.Id == existingId);
            if (existing is null || !existing.IsComplete || existing.TypeId != typeId) return null;
            var upgraded = definition.UpgradeTypeId is null ? null : SurfaceBuildingCatalog.Find(definition.UpgradeTypeId);
            if (upgraded is null) return null;
            credits = definition.UpgradeCreditCost;
            industry = definition.UpgradeIndustryCost;
            result = upgraded;
            id = existingId;
            existing.TypeId = result.Id;
            existing.IndustryProgress = result.IndustryCost;
        }
        else
        {
            if (!definition.AvailableForPlacement) return null;
            id = colony.SurfaceBuildings.Count == 0 ? 1 : colony.SurfaceBuildings.Max(item => item.Id) + 1;
            colony.SurfaceBuildings.Add(new SurfaceBuildingState
            { Id = id, TypeId = typeId, IndustryProgress = definition.IndustryCost, IsComplete = true });
        }
        var output = SurfaceConstruction.GetOutput(colony);
        return new(credits, industry, upgradeId is null ? industry / SurfaceConstruction.IndustryPerSitePerDay : 0,
            output.Supply, output.Demand, output.PoweredBuildingIds.Contains(id), output.HabitatSupportReduction, result.Name);
    }
}

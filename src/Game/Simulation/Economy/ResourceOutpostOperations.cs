using System;
using System.Linq;
using Game.Simulation.Construction;
using Game.Simulation.Models;

namespace Game.Simulation.Economy;

public sealed record ResourceOutpostOperationsSnapshot(
    bool IsResourceOutpost,
    bool HasConfirmedDeposit,
    double ExtractionPerDay,
    double StoredMaterials,
    double StorageCapacity,
    string Status);

public static class ResourceOutpostOperations
{
    public const double SealedHubStorageCapacity = 25.0;
    public const double StorageCapacityPerFabricationComplex = 100.0;

    public static ResourceOutpostOperationsSnapshot GetSnapshot(GalaxyState galaxy, ColonyState settlement)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(settlement);
        if (settlement.Kind != SettlementKind.ResourceOutpost)
            return new(false, false, 0.0, 0.0, 0.0, "Ordinary colony");

        var body = settlement.PlanetaryBodyId is int bodyId
            ? galaxy.PlanetaryBodies.FirstOrDefault(candidate => candidate.Id == bodyId && candidate.SystemId == settlement.SystemId)
            : null;
        var hasDeposit = body?.HasRareResource == true;
        var surface = SurfaceConstruction.GetOutput(settlement);
        var completedFabricators = settlement.SurfaceBuildings.Count(building => building.IsComplete &&
            SurfaceBuildingCatalog.FunctionalFamily(building.TypeId) == "fabricator");
        var capacity = SealedHubStorageCapacity + completedFabricators * StorageCapacityPerFabricationComplex;
        var extraction = hasDeposit ? surface.IndustryPerDay : 0.0;
        var status = !hasDeposit
            ? "No confirmed extractable deposit"
            : completedFabricators == 0
                ? "Build a fabrication complex to begin extraction"
                : extraction <= 0.0
                    ? "Extraction offline: processing complex lacks power"
                    : settlement.StoredExtractedMaterials + 0.0001 >= capacity
                        ? "Storage full: freight service required"
                        : "Extracting to local storage; freight service not yet established";
        return new(true, hasDeposit, extraction, settlement.StoredExtractedMaterials, capacity, status);
    }

    public static void Advance(GalaxyState galaxy, ColonyState settlement, double simulationDays)
    {
        if (settlement.Kind != SettlementKind.ResourceOutpost || simulationDays <= 0.0)
            return;
        var snapshot = GetSnapshot(galaxy, settlement);
        settlement.StoredExtractedMaterials = Math.Clamp(
            settlement.StoredExtractedMaterials + snapshot.ExtractionPerDay * simulationDays,
            0.0,
            snapshot.StorageCapacity);
    }
}

using System.Collections.Generic;
using Game.Simulation.Economy;

namespace Game.Presentation;

public sealed record UiSurfaceSnapshot(int ColonyId, int BodyId, string PlanetName, string ColonyName,
    SovereignCurrencyDefinition Currency, double Credits, double Industry, double PowerSupply, double PowerDemand, IReadOnlyList<UiSurfaceBuilding> Buildings,
    IReadOnlyList<UiSurfaceBuildOption> BuildOptions, double CreditsPerDay, double UpkeepCreditsPerDay,
    double BaseOperationsFundingFraction,
    double IndustryPerDay, double SciencePerDay, string SpecializationName, string SpecializationDescription,
    int SpecializationComplexes, bool SpecializationActive, string SurfaceVisualClass,
    double PopulationMillions, int RequiredHabitatSystems, double HabitatSupportReduction, int BuildingCapacity,
    string HubName, int HubLevel, bool IsCapitalHub, bool CanUpgradeHub,
    double HubUpgradeCreditCost, double HubUpgradeIndustryCost, bool CanAffordHubUpgrade,
    bool IsResourceOutpost, double ExtractionPerDay, double StoredExtractedMaterials,
    double ExtractedMaterialCapacity, string OutpostOperationsStatus,
    double FoodCapacityMillions, double WaterCapacityMillions, double HousingCapacityMillions, double SupportedPopulationMillions,
    double SustenanceSupportRatio, string LimitingSustenanceSupply,
    double WorkforceAvailableMillions, double WorkforceDemandMillions,
    double WorkingAgePopulationMillions, double EmployedPopulationMillions, double EmploymentRate,
    double FoodReserveDays, double WaterReserveDays);
public sealed record UiSurfaceBuilding(int Id, string TypeId, string Name, float X, float Z,
    float RotationDegrees, double Progress, double Cost, bool Complete, bool Powered,
    bool CanUpgrade = false, string? UpgradeName = null, double UpgradeCreditCost = 0,
    double UpgradeIndustryCost = 0, bool CanAffordUpgrade = false, bool Staffed = true,
    bool Enabled = true);
public sealed record UiSurfaceBuildOption(string Id, string Name, string Description, double IndustryCost,
    double CreditCost, float FootprintRadius, bool CanAfford);
public sealed record UiSurfaceOrderResult(bool Accepted, string Message);

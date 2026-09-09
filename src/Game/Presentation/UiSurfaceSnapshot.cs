using System.Collections.Generic;

namespace Game.Presentation;

public sealed record UiSurfaceSnapshot(int ColonyId, int BodyId, string PlanetName, string ColonyName,
    double Credits, double Industry, double PowerSupply, double PowerDemand, IReadOnlyList<UiSurfaceBuilding> Buildings,
    IReadOnlyList<UiSurfaceBuildOption> BuildOptions, double CreditsPerDay, double UpkeepCreditsPerDay,
    double IndustryPerDay, double SciencePerDay, string SpecializationName, string SpecializationDescription,
    int SpecializationComplexes, bool SpecializationActive, string SurfaceVisualClass);
public sealed record UiSurfaceBuilding(int Id, string TypeId, string Name, float X, float Z,
    float RotationDegrees, double Progress, double Cost, bool Complete, bool Powered,
    bool CanUpgrade = false, string? UpgradeName = null, double UpgradeCreditCost = 0,
    double UpgradeIndustryCost = 0, bool CanAffordUpgrade = false);
public sealed record UiSurfaceBuildOption(string Id, string Name, string Description, double IndustryCost,
    double CreditCost, float FootprintRadius, bool CanAfford);
public sealed record UiSurfaceOrderResult(bool Accepted, string Message);

using System;
using System.Linq;
using Game.Simulation.Construction;
using Game.Simulation.Economy;
using Game.Simulation.Models;
using Game.Simulation.Species;
using Game.Simulation.Shipbuilding;
using Godot;

namespace Game.Presentation;

public sealed record UiOwnedColonySnapshot(int ColonyId, int BodyId, string ColonyName, string PlanetName,
    string SystemName, double PopulationMillions, int BuildingCount, bool CanLand,
    string SpecializationName, string SpecializationDescription, string SettlementScale,
    double AdministrationCreditsPerDay, double HabitatSupportCreditsPerDay, double GrossHabitatSupportCreditsPerDay,
    double HabitatSupportReduction, double SurfacePowerSupply, double SurfacePowerDemand, string HabitatNeeds,
    double ExtractionPerDay, double StoredExtractedMaterials, double ExtractedMaterialCapacity, string OutpostOperationsStatus,
    bool CanRequestFreight, string FreightActionReason,
    double FoodCapacityMillions, double WaterCapacityMillions, double SupportedPopulationMillions,
    double SustenanceSupportRatio, string LimitingSustenanceSupply,
    double WorkforceAvailableMillions, double WorkforceDemandMillions);

public partial class Main
{
    private PlanetSurfaceView? _planetSurfaceView;
    private GalaxyState? _surfaceGalaxy;
    private int? _surfaceColonyId;
    private int? _surfaceBodyId;
    public bool UiIsSurfaceOpen => _planetSurfaceView?.IsOpen == true;
    public UiSurfaceSnapshot? UiCurrentSurface => BuildSurfaceSnapshot();
    public UiOwnedColonySnapshot[] UiOwnedColonies => _galaxy is null
        ? Array.Empty<UiOwnedColonySnapshot>()
        : _galaxy.Colonies
            .Where(colony => colony.CivilizationId == _galaxy.PlayerCivilizationId)
            .OrderBy(colony => colony.Id)
            .Select(colony =>
            {
                var body = _galaxy.PlanetaryBodies.FirstOrDefault(item => item.Id == colony.PlanetaryBodyId);
                var system = _galaxy.Systems.First(item => item.Id == colony.SystemId);
                var specialization = SurfaceConstruction.GetSpecialization(colony);
                var surface = SurfaceConstruction.GetOutput(colony);
                var support = new CurrentColonyHabitatSupportBurdenView().Build(_galaxy, colony.Id);
                var needs = support.Environment is not { } environment ? "Environment unresolved" :
                    environment.RequiredMitigationCategories == 0 ? "Natural environment" :
                    $"{environment.RequiredMitigationCategories} habitat systems required";
                var grossSupport = EconomySimulation.GetHabitatSupportCost(support);
                var outpost = ResourceOutpostOperations.GetSnapshot(_galaxy, colony);
                var sustenance = ColonySustenanceCapacity.GetSnapshot(_galaxy, colony);
                var freight = FindAvailableFreighter();
                var canRequestFreight = outpost.IsResourceOutpost && freight is not null &&
                    (outpost.StoredMaterials > 0.0 || outpost.ExtractionPerDay > 0.0);
                var freightReason = !outpost.IsResourceOutpost ? string.Empty
                    : freight is null ? "Build an Interstellar Bulk Freighter and station it at a developed colony."
                    : outpost.StoredMaterials <= 0.0 && outpost.ExtractionPerDay <= 0.0 ? outpost.Status
                    : $"Dispatch {freight.Name} to collect up to {freight.CargoMaterialCapacity:0.#} material units.";
                return new UiOwnedColonySnapshot(colony.Id, colony.PlanetaryBodyId ?? -1, colony.Name,
                    body?.Name ?? "Orbital habitat", system.Name, colony.PopulationMillions,
                    colony.SurfaceBuildings.Count, body?.Environment.HasSolidSurface == true,
                    specialization.Name, specialization.Description,
                    colony.Kind == SettlementKind.ResourceOutpost ? "Staffed resource outpost" :
                        colony.PopulationMillions < 250 ? "Growing settlement" : "Colony",
                    EconomySimulation.GetAdministrationCost(colony.PopulationMillions),
                    grossSupport * (1 - surface.HabitatSupportReduction), grossSupport,
                    surface.HabitatSupportReduction, surface.Supply, surface.Demand, needs,
                    outpost.ExtractionPerDay, outpost.StoredMaterials, outpost.StorageCapacity, outpost.Status,
                    canRequestFreight, freightReason, sustenance.FoodCapacityMillions,
                    sustenance.WaterCapacityMillions, sustenance.SupportedPopulationMillions,
                    sustenance.SupportRatio, sustenance.LimitingSupply,
                    surface.WorkforceAvailableMillions, surface.WorkforceDemandMillions);
            }).ToArray();

    public string UiRequestOutpostFreight(int outpostId)
    {
        if (_galaxy is null) return "Freight control is unavailable while the campaign initializes.";
        var fleet = FindAvailableFreighter();
        if (fleet is null) return "No idle Interstellar Bulk Freighter is stationed at one of your developed colonies.";
        return _coreSimulation.IssueFreightCollectionOrder(
            _galaxy, _galaxy.PlayerCivilizationId, fleet.Id, outpostId).Message;
    }

    private FleetState? FindAvailableFreighter()
    {
        if (_galaxy is null) return null;
        var developedSystems = _galaxy.Colonies.Where(colony => colony.CivilizationId == _galaxy.PlayerCivilizationId &&
            colony.Kind == SettlementKind.Colony).Select(colony => colony.SystemId).ToHashSet();
        return _galaxy.Fleets.Where(fleet => fleet.IsActive && fleet.CivilizationId == _galaxy.PlayerCivilizationId &&
                fleet.Role == FleetRole.Logistics && fleet.DesignId == ShipDesignRegistry.BulkFreighterId &&
                fleet.DestinationSystemId is null && fleet.FreightHomeColonyId is null && fleet.FreightTargetOutpostId is null &&
                fleet.CargoMaterials <= 0.0 && fleet.CurrentSystemId is int systemId && developedSystems.Contains(systemId))
            .OrderBy(fleet => fleet.Id).FirstOrDefault();
    }

    protected void InitializeSurfacePresentation()
    {
        if (_planetSurfaceView is not null) return;
        var layer = new CanvasLayer { Name = "PlanetSurfaceLayer", Layer = 20 };
        _planetSurfaceView = new PlanetSurfaceView { Name = "PlanetSurfaceView" };
        _planetSurfaceView.Configure(BuildSurfaceSnapshot, UiPlaceSurfaceBuilding, UiRemoveSurfaceBuilding,
            UiUpgradeSurfaceBuilding);
        _planetSurfaceView.IsInputBlocked = () => (UiIsMenuOpen || UiIsDeveloperToolsOpen);
        _planetSurfaceView.SaveRequested += UiSave;
        _planetSurfaceView.PauseRequested += UiTogglePause;
        _planetSurfaceView.SpeedRequested += UiSetSpeed;
        _planetSurfaceView.ReadTimeLabel = () => UiModeLabel + " · " + (UiDeveloperToolsUsed ? "Tools used · " : "") + UiSpeedLabel;
        _planetSurfaceView.ReadSpeedLevel = () => (int)UiCurrentSpeed;
        _planetSurfaceView.ReturnToOrbit += UiReturnToOrbit;
        AddChild(layer);
        layer.AddChild(_planetSurfaceView);
        PlanetSurfaceAvailable = CanOpenPlanetSurface;
        PlanetSurfaceRequested += UiOpenPlanetSurface;
    }

    protected void RefreshSurfacePresentation()
    {
        if (UiIsSurfaceOpen && BuildSurfaceSnapshot() is null) UiReturnToOrbit();
    }

    private bool CanOpenPlanetSurface(int bodyId) => _galaxy is not null &&
        _galaxy.Colonies.Any(colony => colony.CivilizationId == _galaxy.PlayerCivilizationId &&
            colony.PlanetaryBodyId == bodyId && colony.SystemId == _selectedSystemId) &&
        _galaxy.PlanetaryBodies.Any(body => body.Id == bodyId && body.SystemId == _selectedSystemId && body.Environment.HasSolidSurface);

    public void UiOpenPlanetSurface(int bodyId)
    {
        if ((UiIsMenuOpen || UiIsDeveloperToolsOpen) || UiFocusedPlanetBodyId != bodyId || !CanOpenPlanetSurface(bodyId))
        {
            SetStatus("Focus a planet with one of your surface colonies to land.", 5);
            return;
        }
        InitializeSurfacePresentation();
        var colony = _galaxy.Colonies.First(item => item.CivilizationId == _galaxy.PlayerCivilizationId && item.PlanetaryBodyId == bodyId);
        _surfaceGalaxy = _galaxy;
        _surfaceColonyId = colony.Id;
        _surfaceBodyId = bodyId;
        _panning = false;
        _planetSurfaceView!.Open();
    }

    public void UiReturnToOrbit()
    {
        _planetSurfaceView?.Close();
        _surfaceGalaxy = null;
        _surfaceColonyId = null;
        _surfaceBodyId = null;
        _panning = false;
    }

    public void UiOpenOwnedColony(int colonyId, bool land)
    {
        if (_galaxy is null || UiIsMenuOpen || UiIsDeveloperToolsOpen) return;
        var colony = _galaxy.Colonies.FirstOrDefault(item => item.Id == colonyId &&
            item.CivilizationId == _galaxy.PlayerCivilizationId);
        if (colony?.PlanetaryBodyId is not int bodyId)
        {
            SetStatus("This colony has no surface destination.", 5);
            return;
        }

        GetNode<CampaignSidebar>("CampaignSidebar").CloseDrawer();
        UiReturnToOrbit();
        if (UiIsSystemSpatialView) ReturnToStellarView(announce: false);
        _selectedSystemId = colony.SystemId;
        EnterSelectedSystemView();
        if (_systemSpatialCanvas?.FocusBody(bodyId) != true)
        {
            SetStatus("The colony world is not available in the current orbital survey.", 6);
            return;
        }
        if (land) UiOpenPlanetSurface(bodyId);
    }

    private UiSurfaceSnapshot? BuildSurfaceSnapshot()
    {
        if (_galaxy is null || !ReferenceEquals(_surfaceGalaxy, _galaxy) || _surfaceBodyId is not int bodyId ||
            UiFocusedPlanetBodyId != bodyId || !CanOpenPlanetSurface(bodyId)) return null;
        var colony = _galaxy.Colonies.FirstOrDefault(item => item.Id == _surfaceColonyId && item.CivilizationId == _galaxy.PlayerCivilizationId &&
            item.PlanetaryBodyId == bodyId && item.SystemId == _selectedSystemId);
        if (colony is null) return null;
        var output = SurfaceConstruction.GetOutput(colony);
        var specialization = SurfaceConstruction.GetSpecialization(colony);
        var body = _galaxy.PlanetaryBodies.First(item => item.Id == bodyId);
        var habitat = new CurrentColonyHabitatSupportBurdenView().Build(_galaxy, colony.Id);
        var outpost = ResourceOutpostOperations.GetSnapshot(_galaxy, colony);
        var sustenance = ColonySustenanceCapacity.GetSnapshot(_galaxy, colony);
        return new(colony.Id, bodyId, body.Name, colony.Name, PlayerEconomy.Credits, PlayerEconomy.Industry, output.Supply, output.Demand,
            colony.SurfaceBuildings.OrderBy(item => item.Id).Select(item =>
            {
                var definition = SurfaceBuildingCatalog.Find(item.TypeId)!;
                var upgrade = definition.UpgradeTypeId is null ? null : SurfaceBuildingCatalog.Find(definition.UpgradeTypeId);
                return new UiSurfaceBuilding(item.Id, item.TypeId, definition.Name, item.X, item.Z, item.RotationDegrees,
                    item.IndustryProgress / definition.IndustryCost, definition.IndustryCost, item.IsComplete,
                    output.PoweredBuildingIds.Contains(item.Id), item.IsComplete && upgrade is not null, upgrade?.Name,
                    definition.UpgradeCreditCost, definition.UpgradeIndustryCost,
                    item.IsComplete && upgrade is not null && PlayerEconomy.Credits + 0.0001 >= definition.UpgradeCreditCost &&
                    PlayerEconomy.Industry + 0.0001 >= definition.UpgradeIndustryCost,
                    output.StaffedBuildingIds.Contains(item.Id));
            }).ToArray(),
            SurfaceBuildingCatalog.All.Where(item => SurfaceConstruction.IsAvailableForSettlement(colony, item)).Select(item => new UiSurfaceBuildOption(item.Id, item.Name, item.Description,
                item.IndustryCost, item.CreditCost, item.FootprintRadius,
                PlayerEconomy.Credits + 0.0001 >= item.CreditCost)).ToArray(),
            colony.Kind == SettlementKind.Colony ? output.CreditsPerDay : 0.0,
            output.UpkeepCreditsPerDay,
            colony.Kind == SettlementKind.Colony ? output.IndustryPerDay : 0.0,
            output.SciencePerDay,
            specialization.Name, specialization.Description, specialization.CompletedComplexes, specialization.Active,
            SurfaceVisualClass(body), colony.PopulationMillions,
            habitat.Environment?.RequiredMitigationCategories ?? 0, output.HabitatSupportReduction,
            SurfaceConstruction.GetBuildingCapacity(colony), outpost.IsResourceOutpost,
            outpost.ExtractionPerDay, outpost.StoredMaterials, outpost.StorageCapacity, outpost.Status,
            sustenance.FoodCapacityMillions, sustenance.WaterCapacityMillions,
            sustenance.SupportedPopulationMillions, sustenance.SupportRatio, sustenance.LimitingSupply,
            output.WorkforceAvailableMillions, output.WorkforceDemandMillions);
    }

    private static string SurfaceVisualClass(PlanetaryBodyState body)
    {
        var environment = body.Environment;
        if (environment.IsImmersedEnvironment) return "oceanic";
        if (environment.TemperatureKelvin < 200) return "frozen";
        if (environment.TemperatureKelvin > 410) return "hot";
        if (environment.Atmosphere == PlanetaryAtmosphereRegime.Vacuum) return "airless";
        if (environment.AvailableSolvent == PlanetarySolventRegime.Water &&
            environment.Atmosphere is PlanetaryAtmosphereRegime.OxygenNitrogen or PlanetaryAtmosphereRegime.OxygenRich)
            return "temperate";
        if (environment.Atmosphere == PlanetaryAtmosphereRegime.Reducing) return "reducing";
        return "rocky";
    }

    public UiSurfaceOrderResult UiPlaceSurfaceBuilding(string typeId, float x, float z, float rotationDegrees)
    {
        var snapshot = BuildSurfaceSnapshot();
        if (!UiIsSurfaceOpen || (UiIsMenuOpen || UiIsDeveloperToolsOpen) || snapshot is null)
            return new(false, "Open an owned colony surface before placing a building.");
        var result = SurfaceConstruction.Place(_galaxy, _galaxy.PlayerCivilizationId, snapshot.ColonyId, typeId, x, z, rotationDegrees);
        SetStatus(result.Message, 5);
        return new(result.Accepted, result.Message);
    }

    public UiSurfaceOrderResult UiRemoveSurfaceBuilding(int buildingId)
    {
        var snapshot = BuildSurfaceSnapshot();
        if (!UiIsSurfaceOpen || (UiIsMenuOpen || UiIsDeveloperToolsOpen) || snapshot is null)
            return new(false, "Open an owned colony surface before removing a building.");
        var result = SurfaceConstruction.Remove(_galaxy, _galaxy.PlayerCivilizationId, snapshot.ColonyId, buildingId);
        SetStatus(result.Message, 6);
        return new(result.Accepted, result.Message);
    }

    public UiSurfaceOrderResult UiUpgradeSurfaceBuilding(int buildingId)
    {
        var snapshot = BuildSurfaceSnapshot();
        if (!UiIsSurfaceOpen || (UiIsMenuOpen || UiIsDeveloperToolsOpen) || snapshot is null)
            return new(false, "Open an owned colony surface before upgrading a building.");
        var result = SurfaceConstruction.Upgrade(_galaxy, _galaxy.PlayerCivilizationId, snapshot.ColonyId, buildingId);
        SetStatus(result.Message, 6);
        return new(result.Accepted, result.Message);
    }
}

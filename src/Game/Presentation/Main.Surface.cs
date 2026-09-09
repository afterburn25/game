using System;
using System.Linq;
using Game.Simulation.Construction;
using Game.Simulation.Economy;
using Game.Simulation.Models;
using Game.Simulation.Species;
using Godot;

namespace Game.Presentation;

public sealed record UiOwnedColonySnapshot(int ColonyId, int BodyId, string ColonyName, string PlanetName,
    string SystemName, double PopulationMillions, int BuildingCount, bool CanLand,
    string SpecializationName, string SpecializationDescription, string SettlementScale,
    double AdministrationCreditsPerDay, double HabitatSupportCreditsPerDay, string HabitatNeeds);

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
                var support = new CurrentColonyHabitatSupportBurdenView().Build(_galaxy, colony.Id);
                var needs = support.Environment is not { } environment ? "Environment unresolved" :
                    environment.RequiredMitigationCategories == 0 ? "Natural environment" :
                    $"{environment.RequiredMitigationCategories} habitat systems required";
                return new UiOwnedColonySnapshot(colony.Id, colony.PlanetaryBodyId ?? -1, colony.Name,
                    body?.Name ?? "Orbital habitat", system.Name, colony.PopulationMillions,
                    colony.SurfaceBuildings.Count, body?.Environment.HasSolidSurface == true,
                    specialization.Name, specialization.Description,
                    colony.PopulationMillions < 1 ? "Dependent outpost" : colony.PopulationMillions < 250 ? "Growing settlement" : "Colony",
                    EconomySimulation.GetAdministrationCost(colony.PopulationMillions),
                    EconomySimulation.GetHabitatSupportCost(support), needs);
            }).ToArray();

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
                    PlayerEconomy.Industry + 0.0001 >= definition.UpgradeIndustryCost);
            }).ToArray(),
            SurfaceBuildingCatalog.All.Where(item => item.AvailableForPlacement).Select(item => new UiSurfaceBuildOption(item.Id, item.Name, item.Description,
                item.IndustryCost, item.CreditCost, item.FootprintRadius,
                PlayerEconomy.Credits + 0.0001 >= item.CreditCost)).ToArray(),
            output.CreditsPerDay, output.UpkeepCreditsPerDay, output.IndustryPerDay, output.SciencePerDay,
            specialization.Name, specialization.Description, specialization.CompletedComplexes, specialization.Active,
            SurfaceVisualClass(body), colony.PopulationMillions,
            habitat.Environment?.RequiredMitigationCategories ?? 0, output.HabitatSupportReduction);
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

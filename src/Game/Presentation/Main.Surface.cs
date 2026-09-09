using System;
using System.Linq;
using Game.Simulation.Construction;
using Game.Simulation.Models;
using Godot;

namespace Game.Presentation;

public partial class Main
{
    private PlanetSurfaceView? _planetSurfaceView;
    private GalaxyState? _surfaceGalaxy;
    private int? _surfaceColonyId;
    private int? _surfaceBodyId;
    public bool UiIsSurfaceOpen => _planetSurfaceView?.IsOpen == true;
    public UiSurfaceSnapshot? UiCurrentSurface => BuildSurfaceSnapshot();

    protected void InitializeSurfacePresentation()
    {
        if (_planetSurfaceView is not null) return;
        var layer = new CanvasLayer { Name = "PlanetSurfaceLayer", Layer = 20 };
        _planetSurfaceView = new PlanetSurfaceView { Name = "PlanetSurfaceView" };
        _planetSurfaceView.Configure(BuildSurfaceSnapshot, UiPlaceSurfaceBuilding);
        _planetSurfaceView.IsInputBlocked = () => (UiIsMenuOpen || UiIsDeveloperToolsOpen);
        _planetSurfaceView.SaveRequested += UiSave;
        _planetSurfaceView.PauseRequested += UiTogglePause;
        _planetSurfaceView.ReadTimeLabel = () => UiModeLabel + " · " + (UiDeveloperToolsUsed ? "Tools used · " : "") + UiSpeedLabel;
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

    private UiSurfaceSnapshot? BuildSurfaceSnapshot()
    {
        if (_galaxy is null || !ReferenceEquals(_surfaceGalaxy, _galaxy) || _surfaceBodyId is not int bodyId ||
            UiFocusedPlanetBodyId != bodyId || !CanOpenPlanetSurface(bodyId)) return null;
        var colony = _galaxy.Colonies.FirstOrDefault(item => item.Id == _surfaceColonyId && item.CivilizationId == _galaxy.PlayerCivilizationId &&
            item.PlanetaryBodyId == bodyId && item.SystemId == _selectedSystemId);
        if (colony is null) return null;
        var output = SurfaceConstruction.GetOutput(colony);
        var body = _galaxy.PlanetaryBodies.First(item => item.Id == bodyId);
        return new(colony.Id, bodyId, body.Name, colony.Name, PlayerEconomy.Industry, output.Supply, output.Demand,
            colony.SurfaceBuildings.OrderBy(item => item.Id).Select(item =>
            {
                var definition = SurfaceBuildingCatalog.Find(item.TypeId)!;
                return new UiSurfaceBuilding(item.Id, item.TypeId, definition.Name, item.X, item.Z, item.RotationDegrees,
                    item.IndustryProgress / definition.IndustryCost, definition.IndustryCost, item.IsComplete,
                    output.PoweredBuildingIds.Contains(item.Id));
            }).ToArray(),
            SurfaceBuildingCatalog.All.Select(item => new UiSurfaceBuildOption(item.Id, item.Name, item.Description,
                item.IndustryCost, item.FootprintRadius)).ToArray());
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
}

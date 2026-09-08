using System;
using System.Linq;
using Godot;
using Game.Diagnostics;
using Game.Presentation.Spatial;
using Game.Simulation.Exploration;
using Game.Simulation.Knowledge;

namespace Game.Presentation;

public enum SpatialPresentationScale
{
    StellarRegion,
    StarSystem,
}

/// <summary>
/// Integrated scale/navigation adapter for the map. It consumes the observer-safe exploration
/// read model, preserves stellar-map selection/pan/zoom, and never mutates simulation state.
/// </summary>
public partial class Main
{
    private readonly ExplorationReadModel _spatialExplorationReadModel = new();
    private readonly SystemSpatialProjection _systemSpatialProjection = new();
    private readonly SystemSpatialViewState _systemSpatialState = new();
    private SystemSpatialCanvas? _systemSpatialCanvas;

    public SpatialPresentationScale UiSpatialScale =>
        _systemSpatialState.IsOpen ? SpatialPresentationScale.StarSystem : SpatialPresentationScale.StellarRegion;

    public bool UiIsSystemSpatialView => _systemSpatialState.IsOpen;

    public string UiSpatialScaleLabel => UiSpatialScale switch
    {
        SpatialPresentationScale.StarSystem => "Star system",
        _ => "Stellar region",
    };

    protected void InitializeSpatialPresentation()
    {
        if (_systemSpatialCanvas is not null)
            return;

        _systemSpatialCanvas = new SystemSpatialCanvas
        {
            Name = "SystemSpatialCanvas",
            ZIndex = 100,
        };
        _systemSpatialCanvas.ReturnRequested += () => ReturnToStellarView(announce: true);
        AddChild(_systemSpatialCanvas);
        _systemSpatialCanvas.SetSnapshot(null);
    }

    protected void RefreshSpatialPresentation(double delta)
    {
        if (_systemSpatialCanvas is null)
            InitializeSpatialPresentation();
        if (!_systemSpatialState.IsOpen)
            return;

        if (_galaxy is null ||
            !_systemSpatialState.MatchesContext(_galaxy, _galaxy.PlayerCivilizationId, _selectedSystemId))
        {
            ReturnToStellarView(announce: false);
            return;
        }

        var surveyLevel = _galaxy.Knowledge.GetSystemSurveyLevel(_galaxy.PlayerCivilizationId, _systemSpatialState.SystemId);
        if (surveyLevel < SystemSurveyLevel.PartiallySurveyed)
        {
            ReturnToStellarView(announce: false);
            SetStatus("System view closed because reconnaissance-grade orbital knowledge is no longer available.", 6.0);
            return;
        }

        if (!_systemSpatialState.NeedsRefresh(surveyLevel, delta))
            return;

        if (!_galaxy.Systems.Any(system => system.Id == _systemSpatialState.SystemId))
        {
            ReturnToStellarView(announce: false);
            return;
        }

        RebuildSystemSpatialSnapshot();
    }

    protected bool HandleSpatialPresentationInput(InputEvent @event)
    {
        if (_systemSpatialState.IsOpen ||
            @event is not InputEventMouseButton mouse ||
            !mouse.Pressed ||
            mouse.ButtonIndex != MouseButton.Left ||
            !mouse.DoubleClick)
        {
            return false;
        }

        if (_galaxy is null || _selectedSystemId < 0)
            return false;

        if (FindNearestCatalogSystem(mouse.Position, 14.0f)?.Id != _selectedSystemId)
            return false;

        var surveyLevel = _galaxy.Knowledge.GetSystemSurveyLevel(_galaxy.PlayerCivilizationId, _selectedSystemId);
        if (surveyLevel < SystemSurveyLevel.PartiallySurveyed)
        {
            SetStatus("A detected star needs scout reconnaissance before its orbital system can be opened.", 6.0);
            return true;
        }

        EnterSelectedSystemView();
        return true;
    }

    private void EnterSelectedSystemView()
    {
        if (_systemSpatialCanvas is null)
            InitializeSpatialPresentation();
        if (_galaxy is null || _selectedSystemId < 0)
        {
            SetStatus("Select a star on the regional map first.", 6.0);
            return;
        }

        // Every entry point (button or double-click) shares the same knowledge gate.
        if (_galaxy.Knowledge.GetSystemSurveyLevel(_galaxy.PlayerCivilizationId, _selectedSystemId) < SystemSurveyLevel.PartiallySurveyed)
        {
            SetStatus("Send a scout to reconnoitre this star before opening its orbital system.", 7.0);
            return;
        }

        _systemSpatialState.Open(_galaxy, _galaxy.PlayerCivilizationId, _selectedSystemId);
        _panning = false;
        RebuildSystemSpatialSnapshot();

        if (!_systemSpatialState.IsOpen)
            return;

        foreach (var marker in _scienceFleetMarkers.Values)
            marker.Visible = false;

        var selected = _galaxy.Systems.First(system => system.Id == _systemSpatialState.SystemId);
        SetStatus($"Opened {selected.Name} system view. Double-click empty system space to return.", 6.0);
        SupportLogger.Log("spatial-view", $"entered system={selected.Id} survey={_systemSpatialState.SurveyLevel} progress={_systemSpatialState.SurveyProgress:0.000}");
    }

    private void RebuildSystemSpatialSnapshot()
    {
        if (_systemSpatialCanvas is null || _galaxy is null || !_systemSpatialState.IsOpen)
            return;

        var exploration = _spatialExplorationReadModel.Build(_galaxy, _galaxy.PlayerCivilizationId);
        var system = exploration.KnownSystems.FirstOrDefault(candidate => candidate.SystemId == _systemSpatialState.SystemId);
        if (system is null || !system.HasReconnaissanceCatalog)
        {
            ReturnToStellarView(announce: false);
            return;
        }

        var snapshot = _systemSpatialProjection.Build(system);
        _systemSpatialCanvas.SetSnapshot(snapshot);
        _systemSpatialState.Refreshed(system.SurveyLevel, system.SurveyProgress);
    }

    private void ReturnToStellarView(bool announce)
    {
        if (!_systemSpatialState.IsOpen)
            return;

        var previousSystemId = _systemSpatialState.SystemId;
        _systemSpatialCanvas?.SetSnapshot(null);
        _systemSpatialState.Close();
        _panning = false;
        foreach (var marker in _scienceFleetMarkers.Values)
            marker.Visible = true;
        QueueRedraw();

        if (announce)
            SetStatus("Returned to the stellar map with the previous system selection preserved.", 5.0);
        SupportLogger.Log("spatial-view", $"returned-to-stellar-map system={previousSystemId}");
    }
}

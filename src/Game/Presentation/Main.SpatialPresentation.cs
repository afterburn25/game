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
    private SystemSpatialCanvas? _systemSpatialCanvas;
    private int _systemSpatialSystemId = -1;
    private SystemSurveyLevel _systemSpatialSurveyLevel = SystemSurveyLevel.Unknown;
    private double _systemSpatialSurveyProgress = -1.0;
    private double _systemSpatialRefreshCountdown;

    public SpatialPresentationScale UiSpatialScale =>
        _systemSpatialSystemId >= 0 ? SpatialPresentationScale.StarSystem : SpatialPresentationScale.StellarRegion;

    public bool UiIsSystemSpatialView => _systemSpatialSystemId >= 0;

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
        if (_systemSpatialSystemId < 0 || _galaxy is null)
            return;

        if (_selectedSystemId != _systemSpatialSystemId ||
            !_galaxy.Systems.Any(system => system.Id == _systemSpatialSystemId))
        {
            ReturnToStellarView(announce: false);
            return;
        }

        var surveyLevel = _galaxy.Knowledge.GetSystemSurveyLevel(_galaxy.PlayerCivilizationId, _systemSpatialSystemId);
        if (surveyLevel < SystemSurveyLevel.PartiallySurveyed)
        {
            ReturnToStellarView(announce: false);
            SetStatus("System view closed because reconnaissance-grade orbital knowledge is no longer available.", 6.0);
            return;
        }

        _systemSpatialRefreshCountdown -= Math.Max(0.0, delta);
        if (_systemSpatialRefreshCountdown > 0.0 &&
            surveyLevel == _systemSpatialSurveyLevel &&
            Math.Abs(_galaxy.Knowledge.GetSystemSurveyProgress(_galaxy.PlayerCivilizationId, _systemSpatialSystemId) - _systemSpatialSurveyProgress) < 0.000001)
        {
            return;
        }

        _systemSpatialRefreshCountdown = 0.20;
        var surveyProgress = _galaxy.Knowledge.GetSystemSurveyProgress(_galaxy.PlayerCivilizationId, _systemSpatialSystemId);
        if (surveyLevel == _systemSpatialSurveyLevel && Math.Abs(surveyProgress - _systemSpatialSurveyProgress) < 0.000001)
            return;

        RebuildSystemSpatialSnapshot();
    }

    protected bool HandleSpatialPresentationInput(InputEvent @event)
    {
        if (_systemSpatialSystemId >= 0 ||
            @event is not InputEventMouseButton mouse ||
            !mouse.Pressed ||
            mouse.ButtonIndex != MouseButton.Left ||
            !mouse.DoubleClick)
        {
            return false;
        }

        if (_galaxy is null || _selectedSystemId < 0)
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
            return;

        _systemSpatialSystemId = _selectedSystemId;
        _systemSpatialSurveyLevel = SystemSurveyLevel.Unknown;
        _systemSpatialSurveyProgress = -1.0;
        _systemSpatialRefreshCountdown = 0.0;
        RebuildSystemSpatialSnapshot();

        var selected = _galaxy.Systems.First(system => system.Id == _systemSpatialSystemId);
        SetStatus($"Opened {selected.Name} system view. Double-click empty system space to return.", 6.0);
        SupportLogger.Log("spatial-view", $"entered system={selected.Id} survey={_systemSpatialSurveyLevel} progress={_systemSpatialSurveyProgress:0.000}");
    }

    private void RebuildSystemSpatialSnapshot()
    {
        if (_systemSpatialCanvas is null || _galaxy is null || _systemSpatialSystemId < 0)
            return;

        var exploration = _spatialExplorationReadModel.Build(_galaxy, _galaxy.PlayerCivilizationId);
        var system = exploration.KnownSystems.FirstOrDefault(candidate => candidate.SystemId == _systemSpatialSystemId);
        if (system is null || !system.HasReconnaissanceCatalog)
        {
            ReturnToStellarView(announce: false);
            return;
        }

        var snapshot = _systemSpatialProjection.Build(system);
        _systemSpatialCanvas.SetSnapshot(snapshot);
        _systemSpatialSurveyLevel = system.SurveyLevel;
        _systemSpatialSurveyProgress = system.SurveyProgress;
    }

    private void ReturnToStellarView(bool announce)
    {
        if (_systemSpatialSystemId < 0)
            return;

        var previousSystemId = _systemSpatialSystemId;
        _systemSpatialCanvas?.SetSnapshot(null);
        _systemSpatialSystemId = -1;
        _systemSpatialSurveyLevel = SystemSurveyLevel.Unknown;
        _systemSpatialSurveyProgress = -1.0;
        _systemSpatialRefreshCountdown = 0.0;
        QueueRedraw();

        if (announce)
            SetStatus("Returned to the stellar map with the previous system selection preserved.", 5.0);
        SupportLogger.Log("spatial-view", $"returned-to-stellar-map system={previousSystemId}");
    }
}

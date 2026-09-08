using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Game.Diagnostics;
using Game.Simulation;
using Game.Simulation.Models;
using Game.Simulation.Shipbuilding;

namespace Game.Presentation;

public partial class Main
{
    private readonly ShipbuildingSimulation _shipbuilding = new();
    private readonly Dictionary<int, Label> _scienceFleetMarkers = new();
    private int _shipDesignCandidateIndex;
    private CanvasLayer? _shipbuildingUiLayer;
    public string UiShipbuildingSummary { get; private set; } = "Shipyard initializing…";

    private ShipyardState PlayerShipyard => _galaxy.ShipyardStates.First(state => state.CivilizationId == _galaxy.PlayerCivilizationId);
    private FleetState? PlayerScienceVessel => _galaxy.Fleets.FirstOrDefault(fleet => fleet.IsActive && fleet.CivilizationId == _galaxy.PlayerCivilizationId && fleet.Role == FleetRole.Science);

    public override void _PhysicsProcess(double delta)
    {
        _ = delta;
        if (_galaxy is null)
            return;

        if (_clock.Speed != SimulationClock.SpeedLevel.Paused)
            HandleShipbuildingEvents(_shipbuilding.Advance(_galaxy));

        EnsureScienceFleetMarkerLayer();
        UpdateShipbuildingSummary();
        UpdateScienceFleetMarkers();
    }

    public override void _Input(InputEvent @event)
    {
        if (_galaxy is null)
            return;

        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.V)
            {
                CycleShipDesignCandidate();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (key.Keycode == Key.Y)
            {
                StartSelectedShipBuild();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event is InputEventMouseButton mouseButton &&
            mouseButton.Pressed &&
            mouseButton.ButtonIndex == MouseButton.Right &&
            mouseButton.CtrlPressed)
        {
            IssueScienceOrderAt(mouseButton.Position);
            GetViewport().SetInputAsHandled();
        }
    }

    private void CycleShipDesignCandidate()
    {
        var available = _shipbuilding.GetAvailableDesigns(_galaxy, _galaxy.PlayerCivilizationId);
        if (available.Count == 0)
        {
            SetStatus("No interstellar ship designs are available yet. Develop compatible spacecraft-construction and interstellar-transit capability, and complete an Orbital Shipyard.", 7.0);
            return;
        }

        _shipDesignCandidateIndex = (_shipDesignCandidateIndex + 1) % available.Count;
        SetStatus($"Shipyard candidate: {available[_shipDesignCandidateIndex].Name}");
    }

    private void StartSelectedShipBuild()
    {
        var candidate = GetShipDesignCandidate();
        if (candidate is null)
        {
            SetStatus("No ship design is currently available. Develop compatible spacecraft-construction and interstellar-transit capability, and complete an Orbital Shipyard.", 7.0);
            return;
        }

        var result = _shipbuilding.StartBuild(_galaxy, _galaxy.PlayerCivilizationId, candidate.Id);
        SetStatus(result.Message, result.Accepted ? 6.0 : 7.0);
        SupportLogger.Log("shipbuilding-order", $"design={candidate.Id} accepted={result.Accepted} message={result.Message}");
    }

    private ShipDesignDefinition? GetShipDesignCandidate()
    {
        var available = _shipbuilding.GetAvailableDesigns(_galaxy, _galaxy.PlayerCivilizationId);
        if (available.Count == 0)
            return null;

        _shipDesignCandidateIndex = Math.Clamp(_shipDesignCandidateIndex, 0, available.Count - 1);
        return available[_shipDesignCandidateIndex];
    }

    private void HandleShipbuildingEvents(IReadOnlyList<ShipbuildingEvent> events)
    {
        foreach (var e in events)
        {
            SupportLogger.Log("shipbuilding", $"civilization={e.CivilizationId} fleet={e.FleetId} design={e.DesignId} message={e.Message}");
            if (e.CivilizationId != _galaxy.PlayerCivilizationId)
                continue;

            _shipDesignCandidateIndex = 0;
            SetStatus(e.Message, 7.0);
        }
    }

    private void IssueScienceOrderAt(Vector2 mousePosition)
    {
        var science = PlayerScienceVessel;
        if (science is null)
        {
            SetStatus("No active science vessel is available. Build one in the Orbital Shipyard.", 7.0);
            return;
        }

        var target = FindNearestCatalogSystem(mousePosition, 16.0f);
        if (target is null)
            return;

        if (_exploration.IssueMoveOrder(_galaxy, science.Id, target.Id))
        {
            var known = _galaxy.Knowledge.IsSystemKnown(_galaxy.PlayerCivilizationId, target.Id);
            SetStatus($"{science.Name}: science course set for {(known ? target.Name : $"astronomical target {target.Id + 1:000}")}.");
        }
    }

    private void EnsureScienceFleetMarkerLayer()
    {
        if (_shipbuildingUiLayer is not null)
            return;

        _shipbuildingUiLayer = new CanvasLayer
        {
            Name = "ShipbuildingUiLayer",
            Layer = 20,
        };
        AddChild(_shipbuildingUiLayer);
    }

    private void UpdateShipbuildingSummary()
    {
        var state = PlayerShipyard;
        var candidate = GetShipDesignCandidate();
        if (state.ActiveDesignId is { } activeDesignId)
        {
            var design = ShipDesignRegistry.Get(activeDesignId);
            var percent = design.IndustryCost <= 0.0 ? 100.0 : state.ActiveBuildProgress / design.IndustryCost * 100.0;
            var population = state.ReservedPopulationMillions > 0.0 ? $" | Colonists reserved {state.ReservedPopulationMillions:0}M" : string.Empty;
            var next = candidate is null ? string.Empty : $" | Selected {candidate.Name} — V cycle, Y queue";
            UiShipbuildingSummary = $"Shipyard: {design.Name} — {state.ActiveBuildProgress:0}/{design.IndustryCost:0} ({percent:0.0}%){population} | Queue {state.PendingBuildCount}/{ShipyardState.MaxPendingBuilds}{next}";
            return;
        }

        UiShipbuildingSummary = candidate is null
            ? "Shipyard: interstellar designs locked — develop compatible shipbuilding/transit capability + Orbital Shipyard | V cycle, Y build"
            : $"Shipyard candidate: {candidate.Name} ({candidate.IndustryCost:0} industry) | Queue {state.PendingBuildCount}/{ShipyardState.MaxPendingBuilds} — V cycle, Y build | Ctrl+Right click: science vessel";
    }

    private void UpdateScienceFleetMarkers()
    {
        if (_shipbuildingUiLayer is null)
            return;

        var fleets = _galaxy.Fleets
            .Where(fleet => fleet.IsActive && fleet.CivilizationId == _galaxy.PlayerCivilizationId && fleet.Role == FleetRole.Science)
            .ToArray();
        var activeIds = fleets.Select(fleet => fleet.Id).ToHashSet();

        foreach (var fleetId in _scienceFleetMarkers.Keys.Where(id => !activeIds.Contains(id)).ToArray())
        {
            _scienceFleetMarkers[fleetId].QueueFree();
            _scienceFleetMarkers.Remove(fleetId);
        }

        var center = GetViewportRect().Size * 0.5f + _pan;
        foreach (var fleet in fleets)
        {
            if (!_scienceFleetMarkers.TryGetValue(fleet.Id, out var marker))
            {
                marker = new Label
                {
                    Text = "◆",
                    Size = new Vector2(20, 20),
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    TooltipText = fleet.Name,
                };
                marker.AddThemeFontSizeOverride("font_size", 16);
                marker.AddThemeColorOverride("font_color", new Color(0.40f, 0.86f, 1.0f));
                _shipbuildingUiLayer.AddChild(marker);
                _scienceFleetMarkers[fleet.Id] = marker;
            }

            marker.Position = ToScreen(fleet.Position, center) - new Vector2(7, 11);
            marker.Visible = !UiIsSystemSpatialView;
        }
    }
}

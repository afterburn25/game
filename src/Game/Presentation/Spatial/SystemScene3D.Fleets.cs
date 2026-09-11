using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Game.Presentation;

namespace Game.Presentation.Spatial;

/// <summary>Native local-fleet models. Positions are presentation-only offsets around the fleet's actual system host.</summary>
public partial class SystemScene3D
{
    private readonly Dictionary<int, Node3D> _localFleetModels = new();
    private readonly Dictionary<int, Vector3> _localFleetPositions = new();
    private readonly Dictionary<int, string> _localFleetDesigns = new();
    private readonly Dictionary<int, (MeshInstance3D[] Nozzles, MeshInstance3D[] Plumes)> _localFleetEngines = new();
    private readonly Dictionary<int, bool> _localFleetPower = new();
    private readonly HashSet<int> _detailedLocalFleets = new();
    private int? _focusedLocalFleetId;
    private float _fleetMotionTime;

    public void PresentLocalFleets(IReadOnlyList<LocalFleetMarker> fleets, CivilizationVisualStyle style)
    {
        var ids = fleets.Take(64).Select(f => f.Id).ToHashSet();
        foreach (var stale in _localFleetModels.Keys.Where(id => !ids.Contains(id)).ToArray())
        {
            _localFleetModels[stale].QueueFree(); _localFleetModels.Remove(stale); _localFleetPositions.Remove(stale);
            _localFleetDesigns.Remove(stale); _localFleetEngines.Remove(stale); _localFleetPower.Remove(stale); _detailedLocalFleets.Remove(stale);
            if (_focusedLocalFleetId == stale) _focusedLocalFleetId = null;
        }
        var host = _bodies.Values.FirstOrDefault(b => b.Marker.SurfaceKey == "earth") ?? _bodies.Values.FirstOrDefault(b => b.Marker.Kind == Game.Simulation.Models.PlanetaryBodyKind.Planet);
        var center = Vector3.Zero;
        for (var i = 0; i < fleets.Count && i < 64; i++)
        {
            var fleet = fleets[i];
            var heading = fleet.ChartTarget - fleet.ChartPosition;
            var angle = heading.LengthSquared() > .0001f ? Mathf.Atan2(heading.X, heading.Y) : i * Mathf.Tau / Math.Max(1, Math.Min(fleets.Count, 8));
            // The same normalized chart coordinates used by the timed local gate leg drive
            // the close renderer; this is never a cosmetic orbit around Earth or another host.
            var at = center + new Vector3(fleet.ChartPosition.X * 76f, 2.4f + (i % 3) * .8f, fleet.ChartPosition.Y * 76f);
            _localFleetPositions[fleet.Id] = at;
            if (!_localFleetModels.TryGetValue(fleet.Id, out var model))
            {
                // Orbital overview keeps a real role silhouette at a cheap LOD. The selected
                // ship is promoted to the complete inspector geometry before close focus.
                model = ShipGeometry.Create(fleet.DesignId, style, highDetail: false);
                model.Name = "LocalFleet_" + fleet.Id; model.Scale = new(.88f, .88f, .88f);
                _world.AddChild(model); _localFleetModels.Add(fleet.Id, model);
                _localFleetDesigns[fleet.Id] = fleet.DesignId;
                CacheFleetEngines(fleet.Id, model);
            }
            model.Position = at; model.RotationDegrees = new(0, -Mathf.RadToDeg(angle) + 90, 0);
            SetThrusters(fleet.Id, fleet.IsMoving && !fleet.IsHeld);
            if (_focusedLocalFleetId == fleet.Id)
                _targetTarget = at;
        }
    }

    public Vector2? ProjectFleet(int fleetId) => _localFleetPositions.TryGetValue(fleetId, out var at) ? ProjectPoint(at) : null;

    public bool FocusFleet(int fleetId)
    {
        if (!_localFleetModels.TryGetValue(fleetId, out var vessel)) return false;
        if (!_detailedLocalFleets.Contains(fleetId) && _localFleetDesigns.TryGetValue(fleetId, out var design))
        {
            var powered = _localFleetPower.GetValueOrDefault(fleetId);
            var replacement = ShipGeometry.Create(design, _visualStyle, highDetail: true);
            replacement.Name = vessel.Name; replacement.Scale = vessel.Scale;
            replacement.Position = vessel.Position; replacement.Rotation = vessel.Rotation;
            _world.AddChild(replacement); vessel.QueueFree();
            _localFleetModels[fleetId] = vessel = replacement;
            _detailedLocalFleets.Add(fleetId);
            CacheFleetEngines(fleetId, vessel);
            _localFleetPower.Remove(fleetId);
            SetThrusters(fleetId, powered);
        }
        _focusedBodyId = null;
        _focusedLocalFleetId = fleetId;
        _savedPose = null;
        _target = _targetTarget = vessel.Position;
        _distance = _targetDistance = 18f;
        _yaw = _targetYaw = -.72f;
        _pitch = _targetPitch = .22f;
        UpdateCamera();
        return true;
    }

    private void AdvanceLocalFleetModels(double delta)
    {
        _fleetMotionTime += (float)delta;
        foreach (var pair in _localFleetModels)
            if (_localFleetPositions.TryGetValue(pair.Key, out var at))
            {
                var displayed = at + new Vector3(0, Mathf.Sin(_fleetMotionTime * .8f + pair.Key) * .14f, 0);
                pair.Value.Position = displayed;
                if (_focusedLocalFleetId == pair.Key)
                    _targetTarget = displayed;
            }
    }

    private void ClearLocalFleetModels()
    {
        foreach (var model in _localFleetModels.Values) model.QueueFree();
        _localFleetModels.Clear(); _localFleetPositions.Clear();
        _localFleetDesigns.Clear(); _localFleetEngines.Clear(); _localFleetPower.Clear(); _detailedLocalFleets.Clear();
        _focusedLocalFleetId = null;
    }

    private void CacheFleetEngines(int fleetId, Node3D model)
    {
        _localFleetEngines[fleetId] = (
            model.FindChildren("EngineNozzle", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray(),
            model.FindChildren("EnginePlume", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray());
    }

    private void SetThrusters(int fleetId, bool powered)
    {
        if (_localFleetPower.TryGetValue(fleetId, out var previous) && previous == powered) return;
        _localFleetPower[fleetId] = powered;
        if (!_localFleetEngines.TryGetValue(fleetId, out var engines)) return;
        foreach (var nozzle in engines.Nozzles)
        {
            if (nozzle.MaterialOverride is not StandardMaterial3D material) continue;
            material.EmissionEnabled = powered;
            material.EmissionEnergyMultiplier = powered ? 4.5f : .35f;
        }
        foreach (var plume in engines.Plumes)
            plume.Visible = powered;
    }
}

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
    private float _fleetMotionTime;

    public void PresentLocalFleets(IReadOnlyList<LocalFleetMarker> fleets, CivilizationVisualStyle style)
    {
        var ids = fleets.Take(64).Select(f => f.Id).ToHashSet();
        foreach (var stale in _localFleetModels.Keys.Where(id => !ids.Contains(id)).ToArray())
        { _localFleetModels[stale].QueueFree(); _localFleetModels.Remove(stale); _localFleetPositions.Remove(stale); }
        var host = _bodies.Values.FirstOrDefault(b => b.Marker.SurfaceKey == "earth") ?? _bodies.Values.FirstOrDefault(b => b.Marker.Kind == Game.Simulation.Models.PlanetaryBodyKind.Planet);
        var center = host?.Root.Position ?? Vector3.Zero;
        for (var i = 0; i < fleets.Count && i < 64; i++)
        {
            var fleet = fleets[i];
            var angle = i * Mathf.Tau / Math.Max(1, Math.Min(fleets.Count, 8));
            var radius = (host?.Radius ?? 24) + 12f + (i / 8) * 3.2f;
            var at = center + new Vector3(Mathf.Cos(angle) * radius, 2.4f + (i % 3) * .8f, Mathf.Sin(angle) * radius);
            _localFleetPositions[fleet.Id] = at;
            if (!_localFleetModels.TryGetValue(fleet.Id, out var model))
            {
                model = ShipGeometry.Create(fleet.DesignId, style, highDetail: false);
                model.Name = "LocalFleet_" + fleet.Id; model.Scale = new(.72f, .72f, .72f);
                _world.AddChild(model); _localFleetModels.Add(fleet.Id, model);
            }
            model.Position = at; model.RotationDegrees = new(0, -Mathf.RadToDeg(angle) + 90, 0);
        }
    }

    public Vector2? ProjectFleet(int fleetId) => _localFleetPositions.TryGetValue(fleetId, out var at) ? ProjectPoint(at) : null;

    private void AdvanceLocalFleetModels(double delta)
    {
        _fleetMotionTime += (float)delta;
        foreach (var pair in _localFleetModels)
            if (_localFleetPositions.TryGetValue(pair.Key, out var at))
                pair.Value.Position = at + new Vector3(0, Mathf.Sin(_fleetMotionTime * .8f + pair.Key) * .14f, 0);
    }

    private void ClearLocalFleetModels()
    {
        foreach (var model in _localFleetModels.Values) model.QueueFree();
        _localFleetModels.Clear(); _localFleetPositions.Clear();
    }
}

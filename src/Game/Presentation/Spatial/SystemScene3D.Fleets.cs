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
                // This is the finished role-specific vessel geometry used by the inspector,
                // not a map proxy. Keep it cached per fleet so panel seams, equipment and
                // engine hardware remain visible when the local camera approaches.
                model = ShipGeometry.Create(fleet.DesignId, style, highDetail: true);
                model.Name = "LocalFleet_" + fleet.Id; model.Scale = new(.88f, .88f, .88f);
                _world.AddChild(model); _localFleetModels.Add(fleet.Id, model);
            }
            model.Position = at; model.RotationDegrees = new(0, -Mathf.RadToDeg(angle) + 90, 0);
            SetThrusters(model, fleet.IsMoving && !fleet.IsHeld);
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

    private static void SetThrusters(Node3D model, bool powered)
    {
        foreach (var nozzle in model.FindChildren("EngineNozzle", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        {
            if (nozzle.MaterialOverride is not StandardMaterial3D material) continue;
            material.EmissionEnabled = powered;
            material.EmissionEnergyMultiplier = powered ? 4.5f : .35f;
        }
    }
}

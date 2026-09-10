using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Game.Simulation.Models;

namespace Game.Presentation.Spatial;

public sealed record LocalFleetMarker(int Id, string Name, FleetRole Role);

public partial class SystemSpatialCanvas
{
    public Func<IReadOnlyList<LocalFleetMarker>>? GetLocalFleets { get; set; }
    public event Action<int>? FleetSelected;
    private readonly Dictionary<int, Button> _fleetIcons = new();

    private void UpdateLocalFleets()
    {
        var fleets = GetLocalFleets?.Invoke() ?? Array.Empty<LocalFleetMarker>();
        foreach (var stale in _fleetIcons.Keys.Where(id => !fleets.Any(f => f.Id == id)).ToArray())
        { _fleetIcons[stale].QueueFree(); _fleetIcons.Remove(stale); }
        var layout = CurrentViewport;
        for (var i = 0; i < fleets.Count; i++)
        {
            var fleet = fleets[i];
            if (!_fleetIcons.TryGetValue(fleet.Id, out var button))
            {
                var icon = fleet.Role switch { FleetRole.Scout => VisualIconLibrary.Scout, FleetRole.Science => VisualIconLibrary.ScienceVessel,
                    FleetRole.Colony => VisualIconLibrary.ColonyShip, FleetRole.Military => VisualIconLibrary.PatrolCorvette, _ => VisualIconLibrary.NavShips };
                button = VisualUi.Button("", fleet.Name + " · select this ship", () => FleetSelected?.Invoke(fleet.Id), icon);
                button.Name = "SystemFleet" + fleet.Id; button.ZIndex = 18; button.CustomMinimumSize = new(30, 30);
                button.Size = new(30, 30); AddChild(button); _fleetIcons.Add(fleet.Id, button);
            }
            button.Visible = !IsPlanetFocused && _snapshot is not null;
            button.Position = new Vector2(layout.CenterX - 90 + (i % 4) * 34, layout.CenterY - 82 - (i / 4) * 34);
        }
    }
}

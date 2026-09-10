using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Game.Presentation;
using Game.Simulation.Models;

namespace Game.Presentation.Spatial;

public sealed record LocalFleetMarker(int Id, string Name, FleetRole Role, string DesignId);

public partial class SystemSpatialCanvas
{
    public Func<IReadOnlyList<LocalFleetMarker>>? GetLocalFleets { get; set; }
    public Func<CivilizationVisualStyle>? GetVisualStyle { get; set; }
    public Func<ShipyardBuildActivity>? GetShipyardActivity { get; set; }
    public event Action<int>? FleetSelected;
    private readonly Dictionary<int, Button> _fleetIcons = new();

    private void UpdateLocalFleets()
    {
        IReadOnlyList<LocalFleetMarker> fleets = (GetLocalFleets?.Invoke() ?? Array.Empty<LocalFleetMarker>()).Take(64).ToArray();
        var style = GetVisualStyle?.Invoke() ?? CivilizationVisualStyles.Terran;
        _scene.SetVisualStyle(style);
        _scene.PresentLocalFleets(fleets, style);
        if (GetShipyardActivity?.Invoke() is { } activity)
            _scene.PresentShipyardActivity(activity, style);
        foreach (var stale in _fleetIcons.Keys.Where(id => !fleets.Any(f => f.Id == id)).ToArray())
        { _fleetIcons[stale].QueueFree(); _fleetIcons.Remove(stale); }
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
            var anchor = _scene.ProjectFleet(fleet.Id);
            button.Visible = _snapshot is not null && anchor.HasValue && !IsPlanetFocused;
            if (anchor.HasValue) button.Position = anchor.Value - new Vector2(15, 15);
        }
    }
}

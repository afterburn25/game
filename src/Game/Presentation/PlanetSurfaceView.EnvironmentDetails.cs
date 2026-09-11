using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Construction;
using Godot;

namespace Game.Presentation;

public partial class PlanetSurfaceView
{
    private Node3D? _environmentDetails;
    private string _environmentDetailsKey = string.Empty;

    public int EnvironmentDetailCount => _environmentDetails?.GetChildCount() ?? 0;
    public int SurfaceTrafficCount => _environmentDetails?.GetChildren()
        .Count(node => node.Name.ToString().StartsWith("GroundTraffic", StringComparison.Ordinal)) ?? 0;

    private void RefreshEnvironmentDetails(UiSurfaceSnapshot snapshot)
    {
        var layout = string.Join(';', snapshot.Buildings.OrderBy(building => building.Id)
            .Select(building => $"{building.Id}:{building.TypeId}:{building.X:0.0}:{building.Z:0.0}"));
        var populationBand = Math.Clamp((int)Math.Floor(Math.Log10(Math.Max(1, snapshot.PopulationMillions))), 0, 5);
        var key = $"{snapshot.ColonyId}:{snapshot.BodyId}:{snapshot.SurfaceVisualClass}:{VisualStyle.SpeciesId}:{populationBand}:{layout}";
        if (_environmentDetailsKey == key) return;
        _environmentDetailsKey = key;
        if (_environmentDetails is not null)
        {
            _world.RemoveChild(_environmentDetails);
            _environmentDetails.QueueFree();
        }
        _environmentDetails = new SurfaceEnvironmentDetails(snapshot, VisualStyle);
        _world.AddChild(_environmentDetails);
    }
}

/// <summary>Bounded presentation-only surface infrastructure. It reads colony state to avoid
/// construction footprints but never participates in placement, collision, saves, or economy.</summary>
internal partial class SurfaceEnvironmentDetails : Node3D
{
    private readonly List<(Node3D Vehicle, Vector3 Start, Vector3 End, float Phase)> _traffic = new();
    private double _elapsed;

    public SurfaceEnvironmentDetails(UiSurfaceSnapshot snapshot, CivilizationVisualStyle style)
    {
        Name = "SurfaceEnvironmentDetails";
        var road = SurfaceBuildingVisuals.Material("151d21", .89f, .08f);
        var roadEdge = SurfaceBuildingVisuals.Material("59636a", .66f, .48f);
        var concrete = SurfaceBuildingVisuals.Material("4b5659", .86f, .12f);
        var utility = SurfaceBuildingVisuals.Material("303d43", .55f, .72f);
        var accent = new StandardMaterial3D
        {
            AlbedoColor = style.AccentColor.Darkened(.16f), Roughness = .36f, Metallic = .52f,
            EmissionEnabled = true, Emission = style.AccentColor, EmissionEnergyMultiplier = .18f,
        };
        var foliage = SurfaceBuildingVisuals.Material(
            snapshot.SurfaceVisualClass == "reducing" ? "34452d" : "173c2c", .96f);
        var bark = SurfaceBuildingVisuals.Material("332b23", .98f);

        var pad = ChooseClearPoint(snapshot.Buildings, new[]
        {
            new Vector2(172, -116), new Vector2(-178, 124), new Vector2(146, 168), new Vector2(-155, -172),
        }, 24);
        AddLandingPad(pad, concrete, road, accent);

        // A route is omitted if any authoritative structure footprint crosses it.
        // Short conforming segments keep the roadway seated on the deterministic height field.
        var routeStart = new Vector2(76, -44);
        AddRoute(snapshot.Buildings, routeStart, pad, road, roadEdge);
        AddRoute(snapshot.Buildings, new Vector2(-82, 65), new Vector2(82, 65), road, roadEdge);
        AddUtilities(snapshot.Buildings, routeStart, pad, utility, accent);
        AddServiceLinks(snapshot.Buildings, routeStart, road, roadEdge, utility, accent);

        var canGrow = snapshot.SurfaceVisualClass is "temperate" or "oceanic" or "reducing";
        var random = new Random(unchecked(snapshot.BodyId * 7919 + snapshot.ColonyId * 104729));
        for (var index = 0; index < (canGrow ? 24 : 10); index++)
        {
            var angle = (float)random.NextDouble() * MathF.Tau;
            var radius = 112 + (float)random.NextDouble() * 245;
            var point = new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
            if (!IsClear(snapshot.Buildings, point, canGrow ? 7 : 12)) continue;
            if (canGrow) AddTree(point, 4.5f + (float)random.NextDouble() * 5.5f, bark, foliage);
            else AddRock(point, 2.5f + (float)random.NextDouble() * 5, concrete, random);
        }

        var trafficCount = Math.Clamp(1 + snapshot.Buildings.Count / 4, 1, 5);
        if (RouteIsClear(snapshot.Buildings, routeStart, pad, 4.5f))
            for (var index = 0; index < trafficCount; index++)
                AddVehicle(index, trafficCount, routeStart, pad, utility, accent);
    }

    public override void _Process(double delta)
    {
        _elapsed += Math.Min(delta, .1);
        foreach (var (vehicle, start, end, phase) in _traffic)
        {
            var cycle = (float)((_elapsed * .045 + phase) % 1.0);
            var forward = cycle < .5f;
            var t = forward ? cycle * 2 : (1 - cycle) * 2;
            t = t * t * (3 - 2 * t);
            var point = start.Lerp(end, t);
            point.Y = SurfaceConstruction.TerrainHeight(point.X, point.Z) + .65f;
            vehicle.Position = point;
            var direction = forward ? end - start : start - end;
            if (direction.LengthSquared() > .01f) vehicle.LookAt(vehicle.Position + direction, Vector3.Up);
        }
    }

    private void AddLandingPad(Vector2 point, Material concrete, Material road, Material accent)
    {
        var ground = SurfaceConstruction.TerrainHeight(point.X, point.Y);
        SurfaceBuildingVisuals.Cylinder(this, 15.5f, 16.5f, .5f,
            new(point.X, ground + .28f, point.Y), concrete, 48).Name = "CivicLandingPad";
        SurfaceBuildingVisuals.Cylinder(this, 11.5f, 11.5f, .08f,
            new(point.X, ground + .57f, point.Y), road, 48);
        for (var arm = 0; arm < 4; arm++)
        {
            var angle = arm * MathF.Tau / 4;
            var marker = new Vector3(point.X + MathF.Cos(angle) * 10, ground + .68f,
                point.Y + MathF.Sin(angle) * 10);
            SurfaceBuildingVisuals.Box(this, new(4.5f, .08f, .32f), marker, accent).Rotation = new(0, -angle, 0);
        }
    }

    private void AddRoute(IReadOnlyList<UiSurfaceBuilding> buildings, Vector2 start, Vector2 end,
        Material road, Material edge)
    {
        if (!RouteIsClear(buildings, start, end, 4.5f)) return;
        var distance = start.DistanceTo(end);
        var segments = Math.Max(1, (int)MathF.Ceiling(distance / 9));
        var angle = -MathF.Atan2(end.Y - start.Y, end.X - start.X) + MathF.PI * .5f;
        for (var index = 0; index < segments; index++)
        {
            var point = start.Lerp(end, (index + .5f) / segments);
            var height = SurfaceConstruction.TerrainHeight(point.X, point.Y);
            var slab = SurfaceBuildingVisuals.Box(this, new(5.4f, .10f, distance / segments + .18f),
                new(point.X, height + .12f, point.Y), road);
            slab.Rotation = new(0, angle, 0);
            if (index % 3 == 0)
            {
                var stripe = SurfaceBuildingVisuals.Box(this, new(.11f, .035f, 2.7f),
                    new(point.X, height + .19f, point.Y), edge);
                stripe.Rotation = new(0, angle, 0);
            }
        }
    }

    private void AddUtilities(IReadOnlyList<UiSurfaceBuilding> buildings, Vector2 start, Vector2 end,
        Material utility, Material accent)
    {
        if (!RouteIsClear(buildings, start, end, 7)) return;
        var direction = (end - start).Normalized();
        var side = new Vector2(-direction.Y, direction.X) * 5.2f;
        for (var index = 1; index < 7; index++)
        {
            var point = start.Lerp(end, index / 7f) + side;
            var ground = SurfaceConstruction.TerrainHeight(point.X, point.Y);
            SurfaceBuildingVisuals.Cylinder(this, .10f, .18f, 5.5f,
                new(point.X, ground + 2.8f, point.Y), utility, 8);
            var crossbar = SurfaceBuildingVisuals.Box(this, new(2.7f, .15f, .15f),
                new(point.X, ground + 5.5f, point.Y), utility);
            crossbar.Rotation = new(0, -MathF.Atan2(direction.Y, direction.X), 0);
            SurfaceBuildingVisuals.Sphere(this, .18f,
                new(point.X, ground + 5.75f, point.Y), accent);
        }
    }

    private void AddServiceLinks(IReadOnlyList<UiSurfaceBuilding> buildings, Vector2 hub, Material road,
        Material edge, Material utility, Material accent)
    {
        // These are visual access spurs only. Each terminates outside the real footprint,
        // so it can reinforce a logical service network without changing placement or collision.
        foreach (var building in buildings.OrderBy(building => building.Id).Take(12))
        {
            var center = new Vector2(building.X, building.Z);
            var direction = center - hub;
            if (direction.LengthSquared() < .01f) continue;
            direction = direction.Normalized();
            var radius = SurfaceBuildingCatalog.Find(building.TypeId)?.FootprintRadius ?? 15;
            var endpoint = center - direction * (radius + 2.8f);
            if (!RouteIsClear(buildings.Where(other => other.Id != building.Id).ToArray(), hub, endpoint, 4.5f)) continue;
            AddRoute(Array.Empty<UiSurfaceBuilding>(), hub, endpoint, road, edge);
            if (building.Id % 2 == 0)
                AddUtilities(Array.Empty<UiSurfaceBuilding>(), hub, endpoint, utility, accent);
        }
    }

    private void AddVehicle(int index, int count, Vector2 from, Vector2 to, Material shell, Material accent)
    {
        var vehicle = new Node3D { Name = $"GroundTraffic{index + 1}" };
        AddChild(vehicle);
        SurfaceBuildingVisuals.Box(vehicle, new(1.35f, .58f, 2.8f), Vector3.Zero, shell);
        SurfaceBuildingVisuals.Box(vehicle, new(1.05f, .42f, 1.2f), new(0, .47f, -.15f), SurfaceBuildingVisuals.Glass);
        SurfaceBuildingVisuals.Box(vehicle, new(.18f, .13f, 2.2f), new(0, .08f, 0), accent);
        _traffic.Add((vehicle, new(from.X, 0, from.Y), new(to.X, 0, to.Y), index / (float)count));
    }

    private void AddTree(Vector2 point, float height, Material bark, Material foliage)
    {
        var ground = SurfaceConstruction.TerrainHeight(point.X, point.Y);
        SurfaceBuildingVisuals.Cylinder(this, .25f, .48f, height * .55f,
            new(point.X, ground + height * .275f, point.Y), bark, 8);
        for (var layer = 0; layer < 3; layer++)
        {
            var crown = SurfaceBuildingVisuals.Sphere(this, height * (.27f - layer * .025f),
                new(point.X, ground + height * (.48f + layer * .17f), point.Y), foliage);
            crown.Scale = new(1.25f, .72f, 1.0f);
        }
    }

    private void AddRock(Vector2 point, float size, Material stone, Random random)
    {
        var ground = SurfaceConstruction.TerrainHeight(point.X, point.Y);
        var rock = SurfaceBuildingVisuals.Sphere(this, 1, new(point.X, ground + size * .35f, point.Y), stone);
        rock.Scale = new(size, size * .7f, size * .82f);
        rock.RotationDegrees = new(11, (float)random.NextDouble() * 360, 7);
    }

    private static Vector2 ChooseClearPoint(IReadOnlyList<UiSurfaceBuilding> buildings,
        IEnumerable<Vector2> candidates, float margin) =>
        candidates.FirstOrDefault(point => IsClear(buildings, point, margin), new Vector2(172, -116));

    private static bool IsClear(IReadOnlyList<UiSurfaceBuilding> buildings, Vector2 point, float margin) =>
        buildings.All(building => point.DistanceTo(new Vector2(building.X, building.Z)) >=
            (SurfaceBuildingCatalog.Find(building.TypeId)?.FootprintRadius ?? 15) + margin);

    private static bool RouteIsClear(IReadOnlyList<UiSurfaceBuilding> buildings, Vector2 start, Vector2 end, float margin)
    {
        var length = start.DistanceTo(end);
        var samples = Math.Max(2, (int)MathF.Ceiling(length / 5));
        for (var index = 0; index <= samples; index++)
            if (!IsClear(buildings, start.Lerp(end, index / (float)samples), margin)) return false;
        return true;
    }
}

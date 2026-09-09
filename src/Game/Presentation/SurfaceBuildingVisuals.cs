using System;
using System.Collections.Generic;
using Game.Simulation.Construction;
using Godot;

namespace Game.Presentation;

/// <summary>Demo architecture, intentionally independent of simulation and saved state.</summary>
public static class SurfaceBuildingVisuals
{
    internal static readonly StandardMaterial3D Shell = Material("b6c3bd", .7f);
    internal static readonly StandardMaterial3D Metal = Material("343f47", .46f, .55f);
    internal static readonly StandardMaterial3D Bronze = Material("9f7950", .6f, .4f);
    internal static readonly StandardMaterial3D Solar = Material("183c6b", .25f, .55f);
    internal static readonly StandardMaterial3D Glass = Material("327e8b", .2f, .35f);
    internal static readonly StandardMaterial3D Light = Material("74e2e0", .3f, .15f, true);
    internal static readonly StandardMaterial3D Amber = Material("e8ac55", .5f, .1f, true);

    public static SurfaceBuildingVisual Create(string typeId) => new(typeId);

    public static Node3D CreateHub()
    {
        var root = new Node3D { Name = "ColonyHub" };
        Cylinder(root, 19, 20, 1.2f, new(0, .6f, 0), Metal, 8);
        Cylinder(root, 12, 14, 5, new(0, 3.7f, 0), Shell, 8);
        Cylinder(root, 8, 11, 1, new(0, 6.7f, 0), Metal, 8);
        Cylinder(root, 5, 7, 3, new(0, 8.7f, 0), Glass, 8);
        Cylinder(root, 5.8f, 5.8f, .35f, new(0, 10.4f, 0), Light, 8);
        Cylinder(root, .35f, .65f, 11, new(0, 15.7f, 0), Shell, 12);
        Box(root, new(7, .4f, .4f), new(0, 19, 0), Metal);
        Sphere(root, 1.5f, new(0, 22, 0), Light);
        for (var i = 0; i < 4; i++)
        {
            var angle = i * MathF.PI * .5f;
            Box(root, new(4.5f, .22f, 8), new(MathF.Sin(angle) * 16, 1.35f, MathF.Cos(angle) * 16), Bronze)
                .Rotation = new(0, angle, 0);
        }
        return root;
    }

    internal static StandardMaterial3D Material(string color, float roughness, float metallic = 0, bool glow = false)
    {
        var value = new Color(color);
        return new StandardMaterial3D
        {
            AlbedoColor = value, Roughness = roughness, Metallic = metallic,
            EmissionEnabled = glow, Emission = value, EmissionEnergyMultiplier = glow ? .65f : 0,
        };
    }

    internal static MeshInstance3D Box(Node3D parent, Vector3 size, Vector3 at, Material material) =>
        Mesh(parent, new BoxMesh { Size = size }, at, material);
    internal static MeshInstance3D Cylinder(Node3D parent, float top, float bottom, float height,
        Vector3 at, Material material, int segments = 24) => Mesh(parent, new CylinderMesh
        { TopRadius = top, BottomRadius = bottom, Height = height, RadialSegments = segments, Rings = 1 }, at, material);
    internal static MeshInstance3D Sphere(Node3D parent, float radius, Vector3 at, Material material) =>
        Mesh(parent, new SphereMesh { Radius = radius, Height = radius * 2, RadialSegments = 24, Rings = 12 }, at, material);
    internal static MeshInstance3D Mesh(Node3D parent, Godot.Mesh mesh, Vector3 at, Material material)
    {
        var node = new MeshInstance3D { Mesh = mesh, Position = at, MaterialOverride = material };
        parent.AddChild(node);
        return node;
    }
}

public partial class SurfaceBuildingVisual : Node3D
{
    public string TypeId { get; }
    private readonly Node3D _structure = new();
    private readonly Node3D _scaffold = new();
    private readonly Node3D _supports = new();
    private readonly List<(MeshInstance3D Mesh, Vector2 Offset)> _supportPosts = new();
    private readonly List<(MeshInstance3D Mesh, Vector2 Offset)> _scaffoldPosts = new();
    private readonly List<(MeshInstance3D Mesh, Material? Material)> _surfaces = new();
    private readonly StandardMaterial3D _preview = new()
    {
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        AlbedoColor = new Color(.25f, .9f, .72f, .5f),
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
    };
    private readonly Label3D _status;
    private readonly MeshInstance3D _beacon;
    private readonly MeshInstance3D _footprint;
    private readonly Node3D _scanner = new();
    private bool _isPreview;
    private bool _previewValid;
    private bool _complete;
    private double _shownProgress;
    private double _targetProgress;
    private float _phase;
    private readonly float _radius;
    private Vector3? _groundingKey;

    public SurfaceBuildingVisual(string typeId)
    {
        TypeId = typeId;
        Name = "Building_" + typeId;
        AddChild(_structure);
        AddChild(_scaffold);
        AddChild(_supports);
        var baseType = typeId.StartsWith("advanced_", StringComparison.Ordinal)
            ? typeId["advanced_".Length..] : typeId;
        var radius = baseType == "fabricator" ? 17f : baseType is "science_lab" or "trade_hub" ? 15f : 12f;
        _radius = radius;
        SurfaceBuildingVisuals.Cylinder(_structure, radius * .85f, radius * .91f, 1.4f,
            new(0, .7f, 0), SurfaceBuildingVisuals.Metal, 8);
        switch (baseType)
        {
            case "power_generator": BuildGenerator(); break;
            case "science_lab": BuildLab(); break;
            case "fabricator": BuildFabricator(); break;
            case "trade_hub": BuildTradeHub(); break;
        }
        if (baseType != typeId)
        {
            SurfaceBuildingVisuals.Mesh(_structure, new TorusMesh
            {
                InnerRadius = radius * .43f, OuterRadius = radius * .52f, Rings = 36, RingSegments = 8,
            }, new(0, 12.4f, 0), SurfaceBuildingVisuals.Light);
            for (var index = 0; index < 4; index++)
            {
                var angle = index * MathF.Tau / 4;
                SurfaceBuildingVisuals.Sphere(_structure, .7f,
                    new(MathF.Cos(angle) * radius * .62f, 10.6f, MathF.Sin(angle) * radius * .62f),
                    SurfaceBuildingVisuals.Amber);
            }
        }
        _beacon = SurfaceBuildingVisuals.Sphere(_structure, .6f, new(0, 13, 0), SurfaceBuildingVisuals.Light);
        for (var index = 0; index < 8; index++)
        {
            var angle = index * MathF.Tau / 8;
            var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius * .8f;
            var post = SurfaceBuildingVisuals.Cylinder(_supports, .75f, 1.05f, 1,
                new(offset.X, 0, offset.Y), SurfaceBuildingVisuals.Metal, 8);
            _supportPosts.Add((post, offset));
        }
        RememberSurfaces(_structure);
        RememberSurfaces(_supports);
        var scaffoldMaterial = SurfaceBuildingVisuals.Material("bd954c", .8f);
        foreach (var x in new[] { -radius * .72f, radius * .72f })
        foreach (var z in new[] { -radius * .72f, radius * .72f })
        {
            var post = SurfaceBuildingVisuals.Box(_scaffold, new(.24f, 1, .24f), new(x, 6.5f, z), scaffoldMaterial);
            post.Scale = new(1, 13, 1);
            _scaffoldPosts.Add((post, new(x, z)));
        }
        for (var level = 1; level <= 3; level++)
        {
            foreach (var side in new[] { -1, 1 })
            {
                SurfaceBuildingVisuals.Box(_scaffold, new(radius * 1.44f, .2f, .2f),
                    new(0, level * 4, side * radius * .72f), scaffoldMaterial);
                SurfaceBuildingVisuals.Box(_scaffold, new(.2f, .2f, radius * 1.44f),
                    new(side * radius * .72f, level * 4, 0), scaffoldMaterial);
            }
        }
        _scaffold.AddChild(_scanner);
        SurfaceBuildingVisuals.Box(_scanner, new(radius * 1.45f, .12f, .3f), Vector3.Zero, SurfaceBuildingVisuals.Light);
        _status = new Label3D
        {
            Position = new(0, 17, 0), FontSize = 34, PixelSize = .025f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, NoDepthTest = false,
            Modulate = new Color("dcecea"), OutlineSize = 7,
        };
        AddChild(_status);
        _footprint = SurfaceBuildingVisuals.Mesh(this, new TorusMesh
        { InnerRadius = radius - .2f, OuterRadius = radius + .2f, Rings = 48, RingSegments = 6 },
            new(0, .18f, 0), _preview);
        _footprint.Visible = false;
        _supports.Visible = false; // Palette thumbnails have no terrain to support against.
    }

    /// <summary>Level the whole visual above the highest terrain under its footprint. The saved
    /// X/Z and simulation placement stay exact; supports extend down to the shared heightfield.</summary>
    public void PlaceOnTerrain(float x, float z, float rotationDegrees)
    {
        var key = new Vector3(x, z, rotationDegrees);
        if (_groundingKey == key) return;
        _groundingKey = key;
        var radians = Mathf.DegToRad(rotationDegrees);
        var cosine = MathF.Cos(radians); var sine = MathF.Sin(radians);
        float GroundAt(Vector2 local) => SurfaceConstruction.TerrainHeight(
            x + cosine * local.X + sine * local.Y, z - sine * local.X + cosine * local.Y);
        var highest = GroundAt(Vector2.Zero);
        // Include the outer scaffold corners, plus the interior, so neither walls nor scaffolds
        // begin below a hill. A small margin covers the 4 m terrain triangles between samples.
        for (var ring = 1; ring <= 3; ring++)
        for (var sample = 0; sample < 32; sample++)
        {
            var angle = sample * MathF.Tau / 32;
            highest = Math.Max(highest, GroundAt(new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * _radius * 1.04f * ring / 3));
        }
        highest += .2f;
        Position = new(x, highest, z);
        RotationDegrees = new(0, rotationDegrees, 0);
        _supports.Visible = true;
        foreach (var (post, offset) in _supportPosts)
        {
            var drop = Math.Max(.1f, highest - GroundAt(offset) + .2f);
            post.Scale = new(1, drop + .6f, 1);
            post.Position = new(offset.X, (.6f - drop) * .5f, offset.Y);
        }
        foreach (var (post, offset) in _scaffoldPosts)
        {
            var drop = Math.Max(.1f, highest - GroundAt(offset) + .2f);
            post.Scale = new(1, 13 + drop, 1);
            post.Position = new(offset.X, (13 - drop) * .5f, offset.Y);
        }
    }

    public void ShowPreview(bool valid)
    {
        if (_isPreview && _previewValid == valid) return;
        _isPreview = true;
        _previewValid = valid;
        _preview.AlbedoColor = valid ? new(.23f, .95f, .7f, .55f) : new(1, .24f, .18f, .57f);
        foreach (var part in _surfaces) part.Mesh.MaterialOverride = _preview;
        _structure.Scale = Vector3.One;
        _scaffold.Visible = false;
        _status.Visible = false;
        _footprint.Visible = true;
    }

    public void UpdateState(UiSurfaceBuilding building)
    {
        _isPreview = false;
        _complete = building.Complete;
        _targetProgress = Math.Clamp(building.Progress, 0, 1);
        // Progress only rises in authoritative snapshots; ease the visual between ticks.
        if (_shownProgress > _targetProgress) _shownProgress = _targetProgress;
        _scaffold.Visible = !_complete;
        _footprint.Visible = false;
        foreach (var part in _surfaces) part.Mesh.MaterialOverride = part.Material;
        _beacon.MaterialOverride = building.Powered ? SurfaceBuildingVisuals.Light : SurfaceBuildingVisuals.Amber;
        _status.Text = building.Complete ? (building.Powered ? building.Name : building.Name + " · needs power")
            : $"{building.Name}  {building.Progress:P0}";
        _status.Modulate = building.Complete && !building.Powered ? new Color("e8b463") : new Color("dcecea");
        if (_complete) _structure.Scale = Vector3.One;
    }

    public void SetSelected(bool selected)
    {
        if (_isPreview) return;
        _preview.AlbedoColor = new Color(.3f, .86f, 1f, .72f);
        _footprint.Visible = selected;
    }

    public override void _Process(double delta)
    {
        if (_isPreview || _complete) return;
        _shownProgress = Math.Min(_targetProgress, _shownProgress + delta * .35);
        _structure.Scale = new(1, .08f + .92f * (float)_shownProgress, 1);
        _phase += (float)delta * 1.7f;
        _scanner.Position = new(0, 2 + (MathF.Sin(_phase) * .5f + .5f) * 10, 0);
    }

    private void RememberSurfaces(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is MeshInstance3D mesh) _surfaces.Add((mesh, mesh.MaterialOverride));
            RememberSurfaces(child);
        }
    }

    private void BuildGenerator()
    {
        SurfaceBuildingVisuals.Cylinder(_structure, 3.1f, 3.8f, 7, new(0, 5, 0), SurfaceBuildingVisuals.Shell);
        for (var i = 0; i < 3; i++)
            SurfaceBuildingVisuals.Cylinder(_structure, 3.35f, 3.35f, .38f, new(0, 3.4f + i * 2, 0), SurfaceBuildingVisuals.Light);
        foreach (var side in new[] { -1, 1 })
        {
            SurfaceBuildingVisuals.Box(_structure, new(1, 3.6f, 1), new(side * 7, 3, 0), SurfaceBuildingVisuals.Bronze);
            var panel = new Node3D { Position = new(side * 7, 5, 0), Rotation = new(0, 0, side * -.22f) };
            _structure.AddChild(panel);
            SurfaceBuildingVisuals.Box(panel, new(6.2f, .25f, 10), Vector3.Zero, SurfaceBuildingVisuals.Solar);
            for (var line = -2; line <= 2; line++)
                SurfaceBuildingVisuals.Box(panel, new(.07f, .07f, 10), new(line * 1.16f, .17f, 0), SurfaceBuildingVisuals.Shell);
            for (var line = -3; line <= 3; line++)
                SurfaceBuildingVisuals.Box(panel, new(6.2f, .07f, .06f), new(0, .17f, line * 1.4f), SurfaceBuildingVisuals.Shell);
        }
    }

    private void BuildLab()
    {
        SurfaceBuildingVisuals.Cylinder(_structure, 8, 9, 3.5f, new(0, 3, 0), SurfaceBuildingVisuals.Shell);
        var dome = SurfaceBuildingVisuals.Sphere(_structure, 7.4f, new(0, 4.2f, 0), SurfaceBuildingVisuals.Glass);
        dome.Scale = new(1, .75f, 1);
        SurfaceBuildingVisuals.Cylinder(_structure, 8, 8, .3f, new(0, 4.6f, 0), SurfaceBuildingVisuals.Light);
        foreach (var side in new[] { -1, 1 })
        {
            SurfaceBuildingVisuals.Box(_structure, new(5, 3.4f, 8), new(side * 9, 3.1f, 0), SurfaceBuildingVisuals.Shell);
            SurfaceBuildingVisuals.Box(_structure, new(.15f, 1.4f, 5), new(side * 11.55f, 3.8f, 0), SurfaceBuildingVisuals.Glass);
        }
        SurfaceBuildingVisuals.Cylinder(_structure, .2f, .35f, 6, new(5, 10, 5), SurfaceBuildingVisuals.Metal, 12);
        var dish = SurfaceBuildingVisuals.Sphere(_structure, 2.2f, new(5, 13, 5), SurfaceBuildingVisuals.Shell);
        dish.Scale = new(1, .25f, 1);
        dish.Rotation = new(.35f, 0, .3f);
    }

    private void BuildFabricator()
    {
        SurfaceBuildingVisuals.Box(_structure, new(20, 6, 15), new(0, 4.5f, 0), SurfaceBuildingVisuals.Shell);
        SurfaceBuildingVisuals.Box(_structure, new(21, .7f, 16), new(0, 7.85f, 0), SurfaceBuildingVisuals.Metal);
        SurfaceBuildingVisuals.Box(_structure, new(9, 4.5f, .25f), new(0, 3.8f, 7.65f), SurfaceBuildingVisuals.Glass);
        for (var i = -2; i <= 2; i++)
            SurfaceBuildingVisuals.Box(_structure, new(.3f, 4.5f, .4f), new(i * 1.8f, 3.8f, 7.85f), SurfaceBuildingVisuals.Bronze);
        foreach (var side in new[] { -1, 1 })
        {
            SurfaceBuildingVisuals.Box(_structure, new(.8f, 13, .8f), new(side * 12, 7.2f, 0), SurfaceBuildingVisuals.Bronze);
            SurfaceBuildingVisuals.Cylinder(_structure, 1.7f, 1.7f, 4, new(side * 5, 10, -4), SurfaceBuildingVisuals.Metal);
        }
        SurfaceBuildingVisuals.Box(_structure, new(25, 1, 1.8f), new(0, 13.6f, 0), SurfaceBuildingVisuals.Bronze);
        SurfaceBuildingVisuals.Box(_structure, new(3, 1, 2.3f), new(2, 12.7f, 0), SurfaceBuildingVisuals.Metal);
        SurfaceBuildingVisuals.Box(_structure, new(.15f, 3, .15f), new(2, 10.7f, 0), SurfaceBuildingVisuals.Light);
    }

    private void BuildTradeHub()
    {
        SurfaceBuildingVisuals.Cylinder(_structure, 9, 10, 2.2f, new(0, 1.7f, 0), SurfaceBuildingVisuals.Shell, 12);
        for (var level = 0; level < 3; level++)
        {
            var radius = 7.2f - level * 1.25f;
            SurfaceBuildingVisuals.Cylinder(_structure, radius, radius + .45f, 2.4f,
                new(0, 4.2f + level * 2.35f, 0), level == 1 ? SurfaceBuildingVisuals.Glass : SurfaceBuildingVisuals.Metal, 12);
        }
        for (var side = 0; side < 4; side++)
        {
            var angle = side * MathF.Tau / 4;
            var position = new Vector3(MathF.Cos(angle) * 10.5f, 3.2f, MathF.Sin(angle) * 10.5f);
            SurfaceBuildingVisuals.Box(_structure, new(5.5f, 3.8f, 3.2f), position, SurfaceBuildingVisuals.Glass);
        }
        SurfaceBuildingVisuals.Cylinder(_structure, .35f, .5f, 7, new(0, 12, 0), SurfaceBuildingVisuals.Bronze, 10);
        SurfaceBuildingVisuals.Sphere(_structure, 1.25f, new(0, 16, 0), SurfaceBuildingVisuals.Light);
    }
}

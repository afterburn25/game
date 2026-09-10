using System;
using Godot;

namespace Game.Presentation.Spatial;

/// <summary>Lit, depth-tested orbital geometry. The snapshot alone controls construction state.</summary>
public partial class OrbitalStructureView : TextureRect
{
    private SubViewport _viewport = null!;
    private Node3D _model = null!;
    private string _key = "";
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.KeepAspectCentered;
        _viewport = new SubViewport { Size = new(256, 256), TransparentBg = true,
            OwnWorld3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.WhenVisible,
            Msaa3D = Viewport.Msaa.Msaa4X };
        AddChild(_viewport); Texture = _viewport.GetTexture();
        var camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 23, Position = new(13, 16, 21) };
        _viewport.AddChild(camera); camera.LookAt(Vector3.Zero);
        _viewport.AddChild(new DirectionalLight3D { RotationDegrees = new(-40, -28, 0), LightColor = new("fff0d8"), LightEnergy = 2.2f });
        _viewport.AddChild(new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = Colors.Transparent,
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new("7594b3"), AmbientLightEnergy = .65f } });
    }
    public void Present(string project, double progress)
    {
        var phase = Math.Clamp((int)Math.Ceiling(progress * 4), 1, 4);
        var key = $"{project}:{phase}";
        if (_key == key) return;
        _key = key;
        if (_model is not null) { _viewport.RemoveChild(_model); _model.QueueFree(); }
        _model = new Node3D(); _viewport.AddChild(_model);
        var hull = MakeMaterial("71858d", .72f, .33f);
        var dark = MakeMaterial("263845", .68f, .40f);
        var glass = MakeMaterial("164d75", .5f, .20f);
        var beacon = MakeMaterial("8bdaca", .2f, .3f, true);
        if (project == "asteroid_resource_network")
        {
            var rock = Add(new SphereMesh { Radius = 4, Height = 7, RadialSegments = 12, Rings = 8 }, new(-2, 0, 0), MakeMaterial("6e6254", .03f, .97f));
            rock.Scale = new(1.2f, .75f, 1.1f); rock.RotationDegrees = new(17, 22, 31);
            Box(new(3, 1, 0), new(9, 1, 2), hull);
            if (phase >= 2) Box(new(5, 0, 1), new(3, 3, 4), dark);
            if (phase >= 3) for (var i = 0; i < 3; i++) Box(new(4 + i * 2, 1.8f, -4), new(1.7f, .2f, 5), glass);
            if (phase == 4) for (var i = 0; i < 4; i++) Add(new CylinderMesh { TopRadius = .7f, BottomRadius = .7f, Height = 2.8f }, new(4 + i * 1.4f, -2, 1), hull);
        }
        else
        {
            Box(Vector3.Zero, new(15, 1.2f, 2), hull);
            Add(new CylinderMesh { TopRadius = 2, BottomRadius = 2.5f, Height = 3, RadialSegments = 48 }, Vector3.Zero, dark);
            if (phase >= 2)
                for (var i = -1; i <= 1; i += 2)
                {
                    Box(new(i * 5.5f, 0, 0), new(1.2f, 1, 12), hull);
                    Box(new(i * 5.5f, .7f, 0), new(.25f, .18f, 10), beacon);
                }
            if (phase >= 3)
                for (var i = -1; i <= 1; i += 2)
                {
                    Box(new(i * 8, 0, -3.5f), new(4, .18f, 5), glass);
                    for (var line = 0; line < 5; line++) Box(new(i * 8, .13f, -5.5f + line), new(4, .05f, .05f), hull);
                }
            if (phase == 4)
            {
                Box(new(0, 2.5f, 0), new(3, 2, 2), hull);
                Box(new(0, 3, 1.05f), new(2.4f, .3f, .15f), beacon);
                Add(new CylinderMesh { TopRadius = .10f, BottomRadius = .2f, Height = 3 }, new(0, 4.5f, 0), hull);
                for (var i = -1; i <= 1; i += 2) Box(new(i * 4, -1.1f, 0), new(1.5f, 1.1f, 6), dark);
            }
        }
    }
    private static StandardMaterial3D MakeMaterial(string color, float metallic, float roughness, bool emissive = false) => new()
    { AlbedoColor = new(color), Metallic = metallic, Roughness = roughness,
        EmissionEnabled = emissive, Emission = new(color), EmissionEnergyMultiplier = 1.2f };
    private void Box(Vector3 position, Vector3 size, Material material) => Add(new BoxMesh { Size = size }, position, material);
    private MeshInstance3D Add(Mesh mesh, Vector3 position, Material material)
    {
        var instance = new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = position };
        _model.AddChild(instance); return instance;
    }
}

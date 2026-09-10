using System;
using Godot;

namespace Game.Presentation;

/// <summary>Renders decorative galactic dust at the current physical pixel size. The
/// strategic catalogue and all input remain in Main's authoritative map projection.</summary>
internal static class GalaxyCloudRenderer
{
    private static SubViewport? _viewport;
    private static ColorRect? _clouds;
    private static ShaderMaterial? _material;
    private static long? _seed;

    public static Texture2D? Render(CanvasItem owner, Rect2 frame, long seed)
    {
        if (_viewport is null || !GodotObject.IsInstanceValid(_viewport))
        {
            _viewport = new SubViewport { Name = "GalaxyDustRenderer", TransparentBg = true,
                Disable3D = true, GuiDisableInput = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Once };
            _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/visual/shaders/galaxy_dust.gdshader") };
            _clouds = new ColorRect { Material = _material, Color = Colors.White, MouseFilter = Control.MouseFilterEnum.Ignore };
            _viewport.AddChild(_clouds);
            owner.GetTree().Root.CallDeferred(Node.MethodName.AddChild, _viewport);
        }
        var scale = owner.GetViewport().GetFinalTransform().Scale;
        var pixels = new Vector2I(Math.Clamp(Mathf.CeilToInt(frame.Size.X * scale.X), 64, 4096),
            Math.Clamp(Mathf.CeilToInt(frame.Size.Y * scale.Y), 64, 4096));
        if (_viewport.Size != pixels || _seed != seed)
        {
            _viewport.Size = pixels; _clouds!.Size = pixels;
            _material!.SetShaderParameter("aspect", new Vector2(frame.Size.X, frame.Size.Y) / MathF.Min(frame.Size.X, frame.Size.Y));
            _material.SetShaderParameter("seed", (float)(seed % 8192));
            _seed = seed;
            _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        }
        return _viewport.IsInsideTree() ? _viewport.GetTexture() : null;
    }
}

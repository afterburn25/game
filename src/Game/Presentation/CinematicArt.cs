using Godot;
using System.Collections.Generic;

namespace Game.Presentation;

/// <summary>Resolution-independent light and restrained metal framing for the game UI.</summary>
public static class CinematicArt
{
    private static readonly Dictionary<string, Texture2D> Frames = new();
    private static Texture2D? _glow;
    public static Texture2D Glow => _glow ??= new GradientTexture2D
    {
        Width = 256, Height = 256, Fill = GradientTexture2D.FillEnum.Radial,
        FillFrom = new(.5f, .5f), FillTo = new(1, .5f),
        Gradient = new Gradient
        {
            Offsets = new[] { 0f, .045f, .12f, .28f, .60f, 1f },
            Colors = new[] { Colors.White, new Color(1,1,1,.94f), new Color(1,1,1,.44f),
                new Color(1,1,1,.13f), new Color(1,1,1,.035f), new Color(1,1,1,0) }
        }
    };

    public static void DrawStarlight(CanvasItem canvas, Vector2 at, float radius, Color color, float opacity = 1)
    {
        var extent = radius * 7;
        canvas.DrawTextureRect(Glow, new(at - Vector2.One * extent, Vector2.One * extent * 2), false,
            new Color(color.R, color.G, color.B, opacity));
        canvas.DrawCircle(at, radius * .34f, new Color(1,.97f,.91f,opacity), true, -1, true);
        canvas.DrawLine(at - new Vector2(radius * 2.8f,0), at + new Vector2(radius * 2.8f,0),
            new Color(color.R,color.G,color.B,opacity * .22f), .65f, true);
    }

    public static StyleBoxTexture Frame(string name = "panel", int margin = 14) => new()
    {
        Texture = FrameTexture(name),
        TextureMarginLeft = 18, TextureMarginRight = 18, TextureMarginTop = 18, TextureMarginBottom = 18,
        ContentMarginLeft = margin, ContentMarginRight = margin,
        ContentMarginTop = margin, ContentMarginBottom = margin
    };

    private static Texture2D FrameTexture(string name)
    {
        if (Frames.TryGetValue(name, out var texture)) return texture;
        // Nine-slice coordinates are authored in pixels. The project's 4x icon
        // importer must not magnify these margins or turn borders into brackets.
        var path = $"res://assets/visual/ui/{name}-frame.svg";
        using var source = GD.Load<Texture2D>(path).GetImage();
        source.Resize(128, 128, Image.Interpolation.Lanczos);
        texture = ImageTexture.CreateFromImage(source);
        Frames.Add(name, texture);
        return texture;
    }
}

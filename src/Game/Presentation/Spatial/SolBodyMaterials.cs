using System;
using Godot;

namespace Game.Presentation.Spatial;

/// <summary>Projects attributed planetary imagery; gameplay never reads image pixels.</summary>
public static class SolBodyMaterials
{
    public static Image? LoadColorSource(string? key)
    {
        if (key is not ("mercury" or "venus" or "earth" or "mars" or "jupiter" or "saturn" or "uranus" or "neptune" or "moon"))
            return null;
        foreach (var extension in new[] { ".jpg", ".png", ".webp" })
        {
            var path = $"res://assets/visual/sol/{key}{extension}";
            if (!ResourceLoader.Exists(path)) continue;
            var texture = GD.Load<Texture2D>(path)
                ?? throw new InvalidOperationException($"Planetary appearance asset failed to load: {path}");
            var source = texture.GetImage()
                ?? throw new InvalidOperationException($"Planetary appearance asset has no image: {path}");
            if (source.IsCompressed() && source.Decompress() != Error.Ok)
                throw new InvalidOperationException($"Planetary appearance asset could not decompress: {path}");
            return source;
        }
        if (key == "moon") return null;
        throw new InvalidOperationException($"Missing Sol planetary appearance asset for {key} in res://assets/visual/sol/.");
    }

    public static Color Sample(Image source, string key, float nx, float ny, float nz)
    {
        // Sample the documented disc in each original image; never include the
        // false-color comparison disc beside the natural-color Uranus portrait.
        var disc = key switch
        {
            "earth" => new Vector4(0.5280f, 0.5836f, 0.1851f, 0.1968f),
            "mercury" => new Vector4(0.5f, 0.5f, 0.454f, 0.454f),
            "uranus" => new Vector4(0.25f, 0.501f, 0.176f, 0.352f),
            "moon" => new Vector4(0.529f, 0.503f, 0.398f, 0.398f),
            "venus" => new Vector4(0.741f, 0.487f, 0.050f, 0.106f),
            _ => Vector4.Zero,
        };
        var u = disc.Z > 0 ? disc.X + nx * disc.Z : 0.5f + MathF.Atan2(nx, nz) / MathF.Tau;
        var v = disc.W > 0 ? disc.Y + ny * disc.W : 0.5f + MathF.Asin(Math.Clamp(ny, -1, 1)) / MathF.PI;
        var x = Math.Clamp(u * (source.GetWidth() - 1), 0, source.GetWidth() - 1);
        var y = Math.Clamp(v * (source.GetHeight() - 1), 0, source.GetHeight() - 1);
        var x0 = (int)x;
        var y0 = (int)y;
        var x1 = Math.Min(x0 + 1, source.GetWidth() - 1);
        var y1 = Math.Min(y0 + 1, source.GetHeight() - 1);
        return source.GetPixel(x0, y0).Lerp(source.GetPixel(x1, y0), x - x0)
            .Lerp(source.GetPixel(x0, y1).Lerp(source.GetPixel(x1, y1), x - x0), y - y0);
    }
}

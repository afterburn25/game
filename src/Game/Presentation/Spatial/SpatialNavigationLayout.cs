using System;

namespace Game.Presentation.Spatial;

/// <summary>Pure presentation geometry for the fixed galaxy artwork and focus transition.</summary>
public static class SpatialNavigationLayout
{
    public static SystemSpatialViewport FitGalaxyOverview(float width, float height)
    {
        const float top = 112, bottom = 128, left = 112, right = 16;
        var centerX = width * 0.5f + 34;
        var centerY = (top + height - bottom) * 0.5f;
        var availableWidth = Math.Max(1, 2 * Math.Min(centerX - left, width - right - centerX));
        var availableHeight = Math.Max(1, height - top - bottom);
        var scale = Math.Max(0.001f, Math.Min(0.045f, Math.Min(availableWidth / 32000, availableHeight / 18000)));
        // Art center is UV (.5,.5); the unchanged catalog origin/Sol is UV (.68,.60).
        return new(centerX + 5760 * scale, centerY + 1800 * scale, scale);
    }

    public static float OrbitalContextOpacity(float currentRadius, float orbitalRadius, float focusRadius)
    {
        var progress = Math.Clamp((currentRadius - orbitalRadius) / Math.Max(1, focusRadius - orbitalRadius), 0, 1);
        var fade = Math.Clamp((progress - 0.15f) / 0.65f, 0, 1);
        return 1 - fade * fade * (3 - 2 * fade);
    }
}

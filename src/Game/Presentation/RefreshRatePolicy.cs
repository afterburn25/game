using System;

namespace Game.Presentation;

/// <summary>Pure refresh-rate policy. Hardware discovery stays at the display boundary so this
/// policy remains safe for headless runs and straightforward to verify.</summary>
public static class RefreshRatePolicy
{
    public const int FallbackHz = 60;

    public static int Normalize(double detectedHz)
    {
        if (!double.IsFinite(detectedHz) || detectedHz < 30 || detectedHz > 1000) return FallbackHz;
        return Math.Clamp((int)Math.Round(detectedHz, MidpointRounding.AwayFromZero), 30, 1000);
    }

    /// <returns>Zero means no engine frame cap.</returns>
    public static int ResolveFrameCap(VideoSettingsService.FrameCap preference, int activeMonitorHz) => preference switch
    {
        VideoSettingsService.FrameCap.Automatic => Normalize(activeMonitorHz),
        VideoSettingsService.FrameCap.Fps60 => 60,
        VideoSettingsService.FrameCap.Fps120 => 120,
        VideoSettingsService.FrameCap.Fps144 => 144,
        VideoSettingsService.FrameCap.Unlimited => 0,
        _ => Normalize(activeMonitorHz),
    };

    /// <summary>Selects only a progressive mode for the already-active desktop dimensions.
    /// The caller owns testing/applying the temporary OS mode and must restore it.</summary>
    public static int HighestSupportedAtCurrentResolution(
        System.Collections.Generic.IEnumerable<(int Width, int Height, int RefreshHz, bool Progressive)> modes,
        int currentWidth, int currentHeight, int fallbackHz)
    {
        var best = Normalize(fallbackHz);
        foreach (var mode in modes)
        {
            if (mode.Width == currentWidth && mode.Height == currentHeight && mode.Progressive && mode.RefreshHz > best)
                best = Normalize(mode.RefreshHz);
        }
        return best;
    }
}

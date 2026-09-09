using System;

namespace Game.Simulation.Economy;

/// <summary>Human-opening reference only. Credits remain the shared strategic currency.</summary>
public static class EarthDollarReference
{
    public const double DollarsPerCredit = 10_000_000.0;

    public static string Format(double credits)
    {
        var dollars = Math.Max(0.0, credits) * DollarsPerCredit;
        return dollars >= 1_000_000_000_000.0 ? $"${dollars / 1_000_000_000_000.0:0.##}T" :
            dollars >= 1_000_000_000.0 ? $"${dollars / 1_000_000_000.0:0.##}B" :
            $"${dollars / 1_000_000.0:0.##}M";
    }
}

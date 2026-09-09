using System;
using System.Globalization;

namespace Game.Simulation.Generation;

public enum GalaxyShape { Spiral, Elliptical, Ring }

/// <summary>Persisted recipe for player-configured worlds. Missing recipes identify older campaigns.</summary>
public sealed record GalaxySetupOptions
{
    public int Version { get; init; } = 1;
    public GalaxyShape Shape { get; init; } = GalaxyShape.Spiral;
    public int SystemCount { get; init; } = 100;
    public int RivalEmpires { get; init; } = 7;
    public int AncientEmpires { get; init; } = 2;

    public void Validate()
    {
        if (Version != 1) throw new ArgumentException("This galaxy recipe needs a different generator version.");
        if (!Enum.IsDefined(Shape)) throw new ArgumentException("Choose a supported galaxy shape.");
        if (SystemCount is not (50 or 100 or 200)) throw new ArgumentException("Choose 50, 100 or 200 star systems.");
        if (RivalEmpires is < 0 or > 7) throw new ArgumentException("Choose between 0 and 7 rival empires.");
        if (AncientEmpires is < 0 or > 3) throw new ArgumentException("Choose between 0 and 3 ancient empires.");
    }

    public GalaxyGenerationSettings ToSettings()
    {
        Validate();
        return new GalaxyGenerationSettings
        {
            SystemCount = SystemCount, PreWarpCivilizationCount = RivalEmpires + 1,
            AncientCivilizationCount = AncientEmpires,
            Radius = 900f * MathF.Sqrt(SystemCount / 100f), PlayerSetup = this,
        };
    }

    public string Summary => $"{Shape} · {SystemCount} systems · {RivalEmpires} rivals · {AncientEmpires} ancient empires";
    public string ShareCode(long seed) => FormattableString.Invariant($"SCG{Version}:{seed}:{(int)Shape}:{SystemCount}:{RivalEmpires}:{AncientEmpires}");

    public static bool TryParseCode(string text, out long seed, out GalaxySetupOptions options)
    {
        seed = 0; options = new();
        var parts = text.Trim().Split(':');
        if (parts.Length != 6 || parts[0] != "SCG1" ||
            !long.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out seed) ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var shape) ||
            !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var count) ||
            !int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out var rivals) ||
            !int.TryParse(parts[5], NumberStyles.None, CultureInfo.InvariantCulture, out var ancients)) return false;
        options = new() { Shape = (GalaxyShape)shape, SystemCount = count, RivalEmpires = rivals, AncientEmpires = ancients };
        try { options.Validate(); return true; }
        catch (ArgumentException) { return false; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Simulation.Research;

public static class TechnologyRegistry
{
    public static readonly IReadOnlyList<TechnologyDefinition> All = new[]
    {
        new TechnologyDefinition(
            "orbital_industry",
            "Orbital Industry",
            "Large-scale orbital construction, automated fabrication, and sustained off-world infrastructure.",
            900.0,
            Array.Empty<string>(),
            TechnologyCategory.Industry),
        new TechnologyDefinition(
            "fusion_propulsion",
            "Fusion Propulsion",
            "High-efficiency fusion drives capable of sustained deep-space operations.",
            1300.0,
            Array.Empty<string>(),
            TechnologyCategory.Propulsion),
        new TechnologyDefinition(
            "deep_space_sensors",
            "Deep-Space Sensor Networks",
            "Long-baseline arrays and autonomous observatories for detecting distant objects and field effects.",
            1100.0,
            Array.Empty<string>(),
            TechnologyCategory.Sensors),
        new TechnologyDefinition(
            "exotic_field_theory",
            "Exotic Field Theory",
            "Experimental physics describing controllable spacetime and subspace field interactions.",
            2200.0,
            new[] { "fusion_propulsion", "deep_space_sensors" },
            TechnologyCategory.Physics),
        new TechnologyDefinition(
            "warp_field_control",
            "Warp Field Control",
            "Stable laboratory-scale distortion fields and the control systems required to shape them.",
            3200.0,
            new[] { "orbital_industry", "exotic_field_theory" },
            TechnologyCategory.Ftl),
        new TechnologyDefinition(
            "prototype_warp_drive",
            "Prototype Warp Drive",
            "A vessel-scale drive capable of sustained faster-than-light travel between star systems.",
            4800.0,
            new[] { "warp_field_control" },
            TechnologyCategory.Ftl),
    };

    public static TechnologyDefinition Get(string id) =>
        All.First(definition => string.Equals(definition.Id, id, StringComparison.Ordinal));

    public static IReadOnlyList<TechnologyDefinition> GetAvailable(TechnologyState state) =>
        All.Where(definition =>
                !state.CompletedTechnologyIds.Contains(definition.Id) &&
                definition.Prerequisites.All(state.CompletedTechnologyIds.Contains))
            .ToArray();
}

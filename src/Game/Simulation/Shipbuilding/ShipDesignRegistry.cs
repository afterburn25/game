using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Shipbuilding;

public static class ShipDesignRegistry
{
    public static readonly IReadOnlyList<ShipDesignDefinition> All = new[]
    {
        new ShipDesignDefinition(
            "warp_scout",
            "Pathfinder Scout",
            "Fast first-generation interstellar survey ship optimized for rapid reconnaissance.",
            FleetRole.Scout,
            650.0,
            24.0,
            140.0f),
        new ShipDesignDefinition(
            "science_vessel",
            "Deep-Space Science Vessel",
            "Long-range research platform with enhanced sensors for anomalies and unusual systems.",
            FleetRole.Science,
            850.0,
            18.0,
            185.0f),
        new ShipDesignDefinition(
            "colony_ship",
            "Interstellar Colony Ship",
            "Large settlement vessel carrying industrial seed equipment and a founding population.",
            FleetRole.Colony,
            1500.0,
            13.5,
            80.0f,
            250.0),
    };

    public static ShipDesignDefinition Get(string id) =>
        All.First(design => string.Equals(design.Id, id, StringComparison.Ordinal));
}

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Combat;
using Game.Simulation.Models;

namespace Game.Simulation.Shipbuilding;

public static class ShipDesignRegistry
{
    private static readonly ShipDesignPrerequisites FirstGenerationInterstellarPrerequisites = new(
        new[]
        {
            ShipbuildingCapabilityIds.SpacecraftConstruction,
            ShipbuildingCapabilityIds.ExperimentalInterstellarTransit,
        },
        Array.Empty<string>(),
        new[] { "orbital_shipyard" });

    public static readonly IReadOnlyList<ShipDesignDefinition> All = new[]
    {
        new ShipDesignDefinition(
            "warp_scout",
            "Pathfinder Scout",
            "Fast first-generation interstellar survey ship optimized for rapid reconnaissance.",
            FleetRole.Scout,
            650.0,
            24.0,
            140.0f,
            FirstGenerationInterstellarPrerequisites,
            CrewComplementIndividuals: 24),
        new ShipDesignDefinition(
            "science_vessel",
            "Deep-Space Science Vessel",
            "Long-range research platform with enhanced sensors for anomalies and unusual systems.",
            FleetRole.Science,
            850.0,
            18.0,
            185.0f,
            FirstGenerationInterstellarPrerequisites,
            CrewComplementIndividuals: 72),
        new ShipDesignDefinition(
            "patrol_corvette",
            "Patrol Corvette",
            "First-generation armed patrol and escort vessel for local defense and early fleet combat.",
            FleetRole.Military,
            1000.0,
            21.0,
            125.0f,
            FirstGenerationInterstellarPrerequisites,
            PopulationCostMillions: 0.0,
            CombatProfileId: CombatProfileIds.PatrolCorvetteMk1,
            CrewComplementIndividuals: 85),
        new ShipDesignDefinition(
            "colony_ship",
            "Interstellar Colony Ship",
            "Large settlement vessel carrying industrial seed equipment and a founding population.",
            FleetRole.Colony,
            1500.0,
            13.5,
            80.0f,
            FirstGenerationInterstellarPrerequisites,
            PopulationCostMillions: 250.0,
            CrewComplementIndividuals: 320),
    };

    public static ShipDesignDefinition Get(string id) =>
        All.First(design => string.Equals(design.Id, id, StringComparison.Ordinal));

    /// <summary>
    /// Transitional resolver for current FleetState, which stores role but not design ID.
    /// The early-release registry intentionally has one active design per role. If that stops
    /// being true, callers must add persistent fleet design identity rather than guessing.
    /// </summary>
    public static ShipDesignDefinition GetCurrentDesignForRole(FleetRole role)
    {
        var matches = All.Where(design => design.Role == role).Take(2).ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                $"Fleet role {role} maps to {matches.Length} current designs; persistent fleet design identity is required before role-based reconstruction can continue.");
        }

        if (matches[0].CrewComplementIndividuals <= 0)
        {
            throw new InvalidOperationException(
                $"Ship design {matches[0].Id} does not define a positive crew complement.");
        }

        return matches[0];
    }
}

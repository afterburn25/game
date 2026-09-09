using System.Collections.Generic;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

public sealed class GalaxyGenerationSettings
{
    // The first playable map is intentionally compact: it gives scouting, colonization,
    // diplomacy, and the system view room to matter without becoming a wall of stars.
    public int SystemCount { get; init; } = 100;
    public int PreWarpCivilizationCount { get; init; } = 8;
    public int AncientCivilizationCount { get; init; } = 2;
    public float Radius { get; init; } = 900.0f;
    public float InitialPreWarpSensorRange { get; init; } = 95.0f;
    public float InitialAncientSensorRange { get; init; } = 420.0f;

    public IReadOnlyDictionary<StarArchetype, double> ArchetypeWeights { get; init; } =
        new Dictionary<StarArchetype, double>
        {
            [StarArchetype.Standard] = 46,
            [StarArchetype.ResourceRich] = 12,
            [StarArchetype.HabitableRich] = 10,
            [StarArchetype.BarrenFrontier] = 8,
            [StarArchetype.Nebula] = 6,
            [StarArchetype.NeutronPulsar] = 5,
            [StarArchetype.BlackHole] = 3,
            [StarArchetype.AncientRuin] = 4,
            [StarArchetype.Dangerous] = 4,
            [StarArchetype.Legendary] = 2,
        };

    public double HabitableChance { get; init; } = 0.18;
    public double AnomalyChance { get; init; } = 0.20;
    public double RareResourceChance { get; init; } = 0.12;
    public double IndependentPreWarpChance { get; init; } = 0.04;
}

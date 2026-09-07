using System.Collections.Generic;
using Game.Simulation.Models;

namespace Game.Simulation.Generation;

public sealed class GalaxyGenerationSettings
{
    public int SystemCount { get; init; } = 120;
    public int CivilizationCount { get; init; } = 8;
    public float Radius { get; init; } = 900.0f;
    public float InitialSensorRange { get; init; } = 230.0f;

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
    public double PreWarpChance { get; init; } = 0.04;
}

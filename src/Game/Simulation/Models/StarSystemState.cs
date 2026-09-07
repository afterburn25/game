using System.Numerics;

namespace Game.Simulation.Models;

public sealed record StarSystemState(
    int Id,
    string Name,
    Vector2 Position,
    StarArchetype Archetype,
    bool HasHabitableWorld,
    bool HasAnomaly,
    bool HasRareResource,
    bool HasPreWarpCivilization
);

public enum StarArchetype
{
    Standard,
    ResourceRich,
    HabitableRich,
    BarrenFrontier,
    Nebula,
    NeutronPulsar,
    BlackHole,
    AncientRuin,
    Dangerous,
    Legendary,
}

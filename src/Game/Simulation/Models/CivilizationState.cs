using Game.Simulation.AI;

namespace Game.Simulation.Models;

public sealed record CivilizationState(
    int Id,
    string Name,
    int HomeSystemId,
    CivilizationArchetype Archetype,
    CivilizationTraits Traits,
    bool IsPlayer
);

public enum CivilizationArchetype
{
    Adaptive,
    Mercantile,
    Scientific,
    Militarist,
    Isolationist,
    Territorial,
    Diplomatic,
    HonorBound,
}

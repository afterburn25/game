using System.Collections.Generic;
using Game.Simulation.Knowledge;

namespace Game.Simulation.Models;

public sealed class GalaxyState
{
    public required long Seed { get; init; }
    public required IReadOnlyList<StarSystemState> Systems { get; init; }
    public required IReadOnlyList<CivilizationState> Civilizations { get; init; }
    public required int PlayerCivilizationId { get; init; }
    public required CivilizationKnowledgeState Knowledge { get; init; }
}

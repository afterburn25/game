using System.Collections.Generic;

namespace Game.Simulation.Models;

public sealed class GalaxyState
{
    public required long Seed { get; init; }
    public required IReadOnlyList<StarSystemState> Systems { get; init; }
}

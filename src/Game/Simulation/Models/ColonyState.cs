namespace Game.Simulation.Models;

public sealed class ColonyState
{
    public required int Id { get; init; }
    public required int CivilizationId { get; init; }
    public required int SystemId { get; init; }

    /// <summary>
    /// Physical world occupied by this settlement. Null is supported only for legacy saves
    /// while migration resolves an appropriate body from the deterministic catalog.
    /// </summary>
    public int? PlanetaryBodyId { get; set; }

    public required string Name { get; init; }
    public double PopulationMillions { get; set; }
    public double Infrastructure { get; set; } = 1.0;
    public double Stability { get; set; } = 1.0;
}

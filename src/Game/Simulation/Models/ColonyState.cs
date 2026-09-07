namespace Game.Simulation.Models;

public sealed class ColonyState
{
    public required int Id { get; init; }
    public required int CivilizationId { get; init; }
    public required int SystemId { get; init; }
    public required string Name { get; init; }
    public double PopulationMillions { get; set; }
    public double Infrastructure { get; set; } = 1.0;
    public double Stability { get; set; } = 1.0;
}

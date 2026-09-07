using Game.Simulation.Species;

namespace Game.Simulation.Models;

public sealed class ColonyState
{
    public required int Id { get; init; }
    public required int CivilizationId { get; init; }
    public required int SystemId { get; init; }
    public required string Name { get; init; }

    /// <summary>
    /// Current early-release population owner. Until colonies gain bounded multi-species
    /// cohorts, the scalar population represents one species and this stable ID identifies it.
    /// </summary>
    public string PopulationSpeciesId { get; set; } = SpeciesCatalog.TerranBaselineId;

    public double PopulationMillions { get; set; }
    public double Infrastructure { get; set; } = 1.0;
    public double Stability { get; set; } = 1.0;
}

namespace Game.Simulation.Models;

public sealed class CivilizationEconomyState
{
    public required int CivilizationId { get; init; }
    public double Credits { get; set; } = 500.0;
    public double Industry { get; set; } = 200.0;
    public double Science { get; set; }
    public double LastCreditsPerSecond { get; set; }
    public double LastIndustryPerSecond { get; set; }
    public double LastSciencePerSecond { get; set; }
}

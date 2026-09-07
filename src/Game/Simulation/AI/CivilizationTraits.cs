namespace Game.Simulation.AI;

public sealed record CivilizationTraits(
    double Aggression,
    double Territoriality,
    double Greed,
    double ScientificCuriosity,
    double RiskTolerance,
    double SurvivalPriority,
    bool HonorBound = false
)
{
    public static CivilizationTraits Balanced => new(0.45, 0.35, 0.35, 0.55, 0.45, 1.0);
}

using System;
using System.Linq;
using Game.Simulation.Models;
using Game.Simulation.Species;

namespace Game.Simulation.Economy;

public sealed class EconomySimulation
{
    public const double BaselineDailyPopulationGrowthRate = 0.000055;

    private readonly IColonyDemographicPressureView _demographicPressure;

    public EconomySimulation(IColonyDemographicPressureView? demographicPressure = null)
    {
        _demographicPressure = demographicPressure ?? new CurrentColonyDemographicPressureView();
    }

    public void Advance(GalaxyState galaxy, double simulationDelta)
    {
        if (simulationDelta <= 0.0)
            return;

        foreach (var economy in galaxy.Economies)
        {
            var colonies = galaxy.Colonies.Where(colony => colony.CivilizationId == economy.CivilizationId).ToArray();
            var construction = galaxy.ConstructionStates.First(c => c.CivilizationId == economy.CivilizationId);

            double creditsPerDay = 0.0;
            double industryPerDay = 0.0;
            double sciencePerDay = 0.0;

            foreach (var colony in colonies)
            {
                var populationFactor = Math.Max(0.01, colony.PopulationMillions / 1000.0);
                var infrastructure = Math.Clamp(colony.Infrastructure, 0.1, 5.0);
                var stability = Math.Clamp(colony.Stability, 0.1, 1.2);
                var demographic = _demographicPressure.Build(colony);

                creditsPerDay += populationFactor * 0.70 * infrastructure * stability;
                industryPerDay += populationFactor * 0.42 * infrastructure * stability;
                sciencePerDay += populationFactor * 0.25 * infrastructure * stability;

                // Economy owns the final population update. Species contributes only the
                // dimensionless intrinsic biological pace derived from authored life history.
                // Planetary stress/support is intentionally not folded into growth here until
                // habitat-support capability and operating costs are authoritative end-to-end.
                colony.PopulationMillions *= Math.Exp(
                    BaselineDailyPopulationGrowthRate *
                    stability *
                    demographic.IntrinsicGrowthPaceFactor *
                    simulationDelta);
            }

            if (construction.CompletedProjectIds.Contains("industrial_automation"))
                industryPerDay *= 1.35;
            if (construction.CompletedProjectIds.Contains("research_network"))
                sciencePerDay *= 1.30;

            economy.Credits += creditsPerDay * simulationDelta;
            economy.Industry += industryPerDay * simulationDelta;
            economy.Science += sciencePerDay * simulationDelta;
            economy.LastCreditsPerSecond = creditsPerDay;
            economy.LastIndustryPerSecond = industryPerDay;
            economy.LastSciencePerSecond = sciencePerDay;
        }
    }
}

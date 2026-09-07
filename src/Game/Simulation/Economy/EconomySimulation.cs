using System;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Economy;

public sealed class EconomySimulation
{
    public void Advance(GalaxyState galaxy, double simulationDelta)
    {
        if (simulationDelta <= 0.0)
            return;

        foreach (var economy in galaxy.Economies)
        {
            var colonies = galaxy.Colonies
                .Where(colony => colony.CivilizationId == economy.CivilizationId)
                .ToArray();

            double creditsPerSecond = 0.0;
            double industryPerSecond = 0.0;
            double sciencePerSecond = 0.0;

            foreach (var colony in colonies)
            {
                var populationFactor = Math.Max(0.01, colony.PopulationMillions / 1000.0);
                var infrastructure = Math.Clamp(colony.Infrastructure, 0.1, 5.0);
                var stability = Math.Clamp(colony.Stability, 0.1, 1.2);

                creditsPerSecond += populationFactor * 0.70 * infrastructure * stability;
                industryPerSecond += populationFactor * 0.42 * infrastructure * stability;
                sciencePerSecond += populationFactor * 0.25 * infrastructure * stability;

                // Slow compounding growth; later systems will make this species/environment dependent.
                colony.PopulationMillions *= Math.Exp(0.000055 * stability * simulationDelta);
            }

            economy.Credits += creditsPerSecond * simulationDelta;
            economy.Industry += industryPerSecond * simulationDelta;
            economy.Science += sciencePerSecond * simulationDelta;
            economy.LastCreditsPerSecond = creditsPerSecond;
            economy.LastIndustryPerSecond = industryPerSecond;
            economy.LastSciencePerSecond = sciencePerSecond;
        }
    }
}

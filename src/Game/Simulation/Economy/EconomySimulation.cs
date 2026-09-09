using System;
using System.Linq;
using Game.Simulation.Models;
using Game.Simulation.Species;
using Game.Simulation.Construction;

namespace Game.Simulation.Economy;

public sealed class EconomySimulation
{
    public const double BaselineDailyPopulationGrowthRate = 0.000055;
    public const double ColonyAdministrationCreditsPerDay = 1.0;
    public const double PopulationServicesCreditsPerBillionPerDay = 0.50;

    private readonly IColonyPopulationTurnoverPressureView _turnoverPressure;

    public EconomySimulation(IColonyPopulationTurnoverPressureView? turnoverPressure = null)
    {
        _turnoverPressure = turnoverPressure ?? new CurrentColonyPopulationTurnoverPressureView();
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
            double creditUpkeepPerDay = 0.0;
            double industryPerDay = 0.0;
            double sciencePerDay = 0.0;

            foreach (var colony in colonies)
            {
                var populationFactor = Math.Max(0.01, colony.PopulationMillions / 1000.0);
                var infrastructure = Math.Clamp(colony.Infrastructure, 0.1, 5.0);
                var stability = Math.Clamp(colony.Stability, 0.1, 1.2);
                var demographic = _turnoverPressure.Build(galaxy, colony);

                creditsPerDay += populationFactor * 0.70 * infrastructure * stability;
                creditUpkeepPerDay += ColonyAdministrationCreditsPerDay +
                    populationFactor * PopulationServicesCreditsPerBillionPerDay * infrastructure;
                industryPerDay += populationFactor * 0.42 * infrastructure * stability;
                sciencePerDay += populationFactor * 0.25 * infrastructure * stability;
                var surface = SurfaceConstruction.GetOutput(colony);
                creditsPerDay += surface.CreditsPerDay;
                sciencePerDay += surface.SciencePerDay;
                industryPerDay += surface.IndustryPerDay;

                // Economy remains authoritative for the final population mutation and the
                // Terran-normalized base rate. Species supplies a dimensionless effective pace
                // composed from authored life history and, only when authoritative, the exact
                // naturally viable occupied environment. Habitat-supported fallback and legacy
                // null-body colonies remain environmentally neutral until support is modeled.
                colony.PopulationMillions *= Math.Exp(
                    BaselineDailyPopulationGrowthRate *
                    stability *
                    demographic.EffectiveGrowthPaceFactor *
                    simulationDelta);
            }

            if (construction.CompletedProjectIds.Contains("industrial_automation"))
                industryPerDay *= 1.35;
            if (construction.CompletedProjectIds.Contains("research_network"))
                sciencePerDay *= 1.30;

            creditUpkeepPerDay += galaxy.Fleets
                .Where(fleet => fleet.IsActive && fleet.CivilizationId == economy.CivilizationId)
                .Sum(fleet => fleet.Role switch
                {
                    FleetRole.Scout => 0.35,
                    FleetRole.Science => 0.55,
                    FleetRole.Colony => 0.75,
                    FleetRole.Military => 1.10,
                    _ => 0.50,
                });

            var netCreditsPerDay = creditsPerDay - creditUpkeepPerDay;

            economy.Credits = Math.Max(0.0, economy.Credits + netCreditsPerDay * simulationDelta);
            economy.Industry += industryPerDay * simulationDelta;
            economy.Science += sciencePerDay * simulationDelta;
            economy.LastCreditsPerSecond = netCreditsPerDay;
            economy.LastIndustryPerSecond = industryPerDay;
            economy.LastSciencePerSecond = sciencePerDay;
        }
    }
}

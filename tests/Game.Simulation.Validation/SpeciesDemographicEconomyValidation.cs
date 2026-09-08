using System.Runtime.CompilerServices;
using Game.Simulation.Economy;
using Game.Simulation.Generation;
using Game.Simulation.Species;

namespace Game.Simulation.Validation;

internal static class SpeciesDemographicEconomyValidation
{
    [ModuleInitializer]
    internal static void Run()
    {
        ValidateLifeHistoryChangesPopulationTurnoverWithoutDirectProductivityBonus();
        Console.WriteLine("PASS: Species life history drives population pace without direct economic bonuses");
    }

    private static void ValidateLifeHistoryChangesPopulationTurnoverWithoutDirectProductivityBonus()
    {
        var terranGalaxy = CreateValidationGalaxy();
        var cryogenicGalaxy = CreateValidationGalaxy();

        var playerId = terranGalaxy.PlayerCivilizationId;
        Require(playerId == cryogenicGalaxy.PlayerCivilizationId, "deterministic validation galaxies disagreed on the player civilization");

        var terranColony = terranGalaxy.Colonies
            .Where(colony => colony.CivilizationId == playerId)
            .OrderBy(colony => colony.Id)
            .First();
        var cryogenicColony = cryogenicGalaxy.Colonies
            .Where(colony => colony.CivilizationId == playerId)
            .OrderBy(colony => colony.Id)
            .First(colony => colony.Id == terranColony.Id);

        const double initialPopulationMillions = 1000.0;
        const double simulationDays = 365.0;
        terranColony.PopulationMillions = initialPopulationMillions;
        cryogenicColony.PopulationMillions = initialPopulationMillions;
        terranColony.Infrastructure = cryogenicColony.Infrastructure = 1.0;
        terranColony.Stability = cryogenicColony.Stability = 1.0;
        terranColony.PopulationSpeciesId = SpeciesCatalog.TerranBaselineId;
        cryogenicColony.PopulationSpeciesId = SpeciesCatalog.CryogenicHydrocarbonId;

        var terranPressure = SpeciesDemographicPressureEvaluator.Evaluate(terranColony.PopulationSpeciesId);
        var cryogenicPressure = SpeciesDemographicPressureEvaluator.Evaluate(cryogenicColony.PopulationSpeciesId);

        new EconomySimulation().Advance(terranGalaxy, simulationDays);
        new EconomySimulation().Advance(cryogenicGalaxy, simulationDays);

        var expectedTerranPopulation = initialPopulationMillions * Math.Exp(
            EconomySimulation.BaselineDailyPopulationGrowthRate *
            terranColony.Stability *
            terranPressure.IntrinsicGrowthPaceFactor *
            simulationDays);
        var expectedCryogenicPopulation = initialPopulationMillions * Math.Exp(
            EconomySimulation.BaselineDailyPopulationGrowthRate *
            cryogenicColony.Stability *
            cryogenicPressure.IntrinsicGrowthPaceFactor *
            simulationDays);

        RequireClose(
            terranColony.PopulationMillions,
            expectedTerranPopulation,
            "Terran colony did not use the normalized life-history growth pace");
        RequireClose(
            cryogenicColony.PopulationMillions,
            expectedCryogenicPopulation,
            "cryogenic colony did not use its authored life-history growth pace");
        Require(
            terranColony.PopulationMillions > cryogenicColony.PopulationMillions,
            "long-generation cryogenic biology did not grow more slowly than the Terran baseline under identical scalar conditions");

        var terranEconomy = terranGalaxy.Economies.First(economy => economy.CivilizationId == playerId);
        var cryogenicEconomy = cryogenicGalaxy.Economies.First(economy => economy.CivilizationId == playerId);

        // Population growth occurs after this tick's production inputs are computed. With the
        // same starting population/infrastructure/stability, changing only species identity must
        // not create a hidden Credits, Industry, or Science racial modifier.
        RequireClose(
            terranEconomy.LastCreditsPerSecond,
            cryogenicEconomy.LastCreditsPerSecond,
            "species identity directly changed same-tick Credits productivity");
        RequireClose(
            terranEconomy.LastIndustryPerSecond,
            cryogenicEconomy.LastIndustryPerSecond,
            "species identity directly changed same-tick Industry productivity");
        RequireClose(
            terranEconomy.LastSciencePerSecond,
            cryogenicEconomy.LastSciencePerSecond,
            "species identity directly changed same-tick Science productivity");
    }

    private static Game.Simulation.Models.GalaxyState CreateValidationGalaxy() =>
        new GalaxyGenerator().Generate(
            0x4445_4D4F_4752_4F57L,
            new GalaxyGenerationSettings
            {
                SystemCount = 36,
                PreWarpCivilizationCount = 4,
                AncientCivilizationCount = 1,
                Radius = 420.0f,
            });

    private static void RequireClose(double actual, double expected, string message)
    {
        if (Math.Abs(actual - expected) > 0.000000001)
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

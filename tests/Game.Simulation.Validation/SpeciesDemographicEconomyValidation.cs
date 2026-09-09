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
        ValidateEffectiveSpeciesPressureChangesPopulationWithoutDirectProductivityBonus();
        Console.WriteLine("PASS: Species life history and exact natural environment drive population pace without direct economic bonuses");
    }

    private static void ValidateEffectiveSpeciesPressureChangesPopulationWithoutDirectProductivityBonus()
    {
        var terranGalaxy = CreateValidationGalaxy();
        var cryogenicGalaxy = CreateValidationGalaxy();

        var playerId = terranGalaxy.PlayerCivilizationId;
        Require(playerId == cryogenicGalaxy.PlayerCivilizationId,
            "deterministic validation galaxies disagreed on the player civilization");

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

        var pressureView = new CurrentColonyPopulationTurnoverPressureView();
        var terranPressure = pressureView.Build(terranGalaxy, terranColony);
        var cryogenicPressure = pressureView.Build(cryogenicGalaxy, cryogenicColony);
        var terranFlow = EconomySimulation.GetCreditFlow(terranGalaxy, playerId);
        var cryogenicFlow = EconomySimulation.GetCreditFlow(cryogenicGalaxy, playerId);

        RequireClose(
            terranPressure.EffectiveGrowthPaceFactor,
            terranPressure.IntrinsicGrowthPaceFactor * terranPressure.NaturalEnvironmentTurnoverFactor,
            "Terran effective demographic pace did not conserve its Species pressure inputs");
        RequireClose(
            cryogenicPressure.EffectiveGrowthPaceFactor,
            cryogenicPressure.IntrinsicGrowthPaceFactor * cryogenicPressure.NaturalEnvironmentTurnoverFactor,
            "cryogenic effective demographic pace did not conserve its Species pressure inputs");

        new EconomySimulation().Advance(terranGalaxy, simulationDays);
        new EconomySimulation().Advance(cryogenicGalaxy, simulationDays);

        var expectedTerranPopulation = initialPopulationMillions * Math.Exp(
            EconomySimulation.BaselineDailyPopulationGrowthRate *
            terranColony.Stability *
            terranPressure.EffectiveGrowthPaceFactor *
            simulationDays);
        var expectedCryogenicPopulation = initialPopulationMillions * Math.Exp(
            EconomySimulation.BaselineDailyPopulationGrowthRate *
            cryogenicColony.Stability *
            cryogenicPressure.EffectiveGrowthPaceFactor *
            simulationDays);

        RequireClose(
            terranColony.PopulationMillions,
            expectedTerranPopulation,
            "Terran colony did not use the Species-owned effective population-turnover pace");
        RequireClose(
            cryogenicColony.PopulationMillions,
            expectedCryogenicPopulation,
            "cryogenic colony did not use the Species-owned effective population-turnover pace");

        var terranEconomy = terranGalaxy.Economies.First(economy => economy.CivilizationId == playerId);
        var cryogenicEconomy = cryogenicGalaxy.Economies.First(economy => economy.CivilizationId == playerId);

        // Population growth occurs after this tick's production inputs are computed. Species
        // identity still creates no hidden productivity modifier; its exact environmental
        // requirements may now create an explicit, separately reported habitat-support cost.
        RequireClose(
            terranEconomy.LastCreditsPerSecond + terranFlow.HabitatSupportPerDay,
            cryogenicEconomy.LastCreditsPerSecond + cryogenicFlow.HabitatSupportPerDay,
            "Species identity changed same-tick Credits beyond the explicit habitat-support cost");
        Require(Math.Abs(terranFlow.HabitatSupportPerDay - cryogenicFlow.HabitatSupportPerDay) > 0.000001,
            "different exact environmental requirements did not produce distinct visible habitat costs");
        RequireClose(
            terranEconomy.LastIndustryPerSecond,
            cryogenicEconomy.LastIndustryPerSecond,
            "Species identity/environmental turnover directly changed same-tick Industry productivity");
        RequireClose(
            terranEconomy.LastSciencePerSecond,
            cryogenicEconomy.LastSciencePerSecond,
            "Species identity/environmental turnover directly changed same-tick Science productivity");
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

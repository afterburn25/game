using System.Runtime.CompilerServices;
using Game.Simulation.Species;

internal static class SpeciesLogisticsDemandChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var evaluator = new SpeciesLogisticsDemandEvaluator();
        var terran = SpeciesCatalog.Get(SpeciesCatalog.TerranBaselineId);
        var terranPopulation = new CurrentPopulationSpeciesSnapshot(terran.Id, 100.0);
        var terranHome = new HabitatEnvironment(
            terran.Environment.GravityG.Preferred,
            terran.Environment.TemperatureKelvin.Preferred,
            terran.Environment.PressureKPa.Preferred,
            terran.Environment.PreferredAtmosphere,
            terran.Environment.BiologicalSolvent,
            RadiationHazard: 0.0,
            IsImmersed: terran.Environment.RequiresImmersion);

        var typical = evaluator.Evaluate(terranPopulation, terranHome);
        var rest = evaluator.Evaluate(terranPopulation, terranHome, PopulationMetabolicOperatingState.Resting);
        var peak = evaluator.Evaluate(terranPopulation, terranHome, PopulationMetabolicOperatingState.PeakActivity);

        Assert(typical.SpeciesId == terran.Id, "Logistics demand changed Terran species identity.");
        AssertNear(typical.PopulationMillions, 100.0, "Logistics demand changed Terran population.");
        AssertNear(typical.AdultBiomassMillionKg, 7000.0, "Terran biomass did not flow into logistics demand.");
        Assert(!typical.RequiresControlledHabitat,
            "Terran preferred environment should not create artificial habitat-support demand.");
        Assert(rest.SelectedMetabolicDemandMillions < typical.SelectedMetabolicDemandMillions,
            "Resting Terran demand should be below typical-day demand.");
        Assert(peak.SelectedMetabolicDemandMillions > typical.SelectedMetabolicDemandMillions,
            "Peak Terran demand should exceed typical-day demand.");

        var terranDormancyRejected = false;
        try
        {
            evaluator.Evaluate(
                terranPopulation,
                terranHome,
                PopulationMetabolicOperatingState.NaturalDormancy);
        }
        catch (InvalidOperationException)
        {
            terranDormancyRejected = true;
        }
        Assert(terranDormancyRejected,
            "Terran population must not receive natural dormancy logistics savings it does not biologically possess.");

        var cryogenic = SpeciesCatalog.Get(SpeciesCatalog.CryogenicHydrocarbonId);
        var cryogenicPopulation = new CurrentPopulationSpeciesSnapshot(cryogenic.Id, 100.0);
        var cryogenicHome = new HabitatEnvironment(
            cryogenic.Environment.GravityG.Preferred,
            cryogenic.Environment.TemperatureKelvin.Preferred,
            cryogenic.Environment.PressureKPa.Preferred,
            cryogenic.Environment.PreferredAtmosphere,
            cryogenic.Environment.BiologicalSolvent,
            RadiationHazard: 0.0,
            IsImmersed: cryogenic.Environment.RequiresImmersion);
        var cryogenicTypical = evaluator.Evaluate(cryogenicPopulation, cryogenicHome);
        var cryogenicDormant = evaluator.Evaluate(
            cryogenicPopulation,
            cryogenicHome,
            PopulationMetabolicOperatingState.NaturalDormancy);

        Assert(cryogenicDormant.IsUsingNaturalDormancy,
            "Cryogenic proving species should expose its natural dormancy state.");
        Assert(cryogenicDormant.MaximumNaturalDormancyDays > 0.0,
            "Natural dormancy must expose a bounded duration rather than unlimited free life support savings.");
        Assert(cryogenicDormant.SelectedMetabolicDemandMillions < cryogenicTypical.SelectedMetabolicDemandMillions,
            "Cryogenic natural dormancy should reduce physical metabolic demand while active.");

        var hostileTerran = evaluator.Evaluate(
            terranPopulation,
            terranHome with
            {
                GravityG = 1.30,
                Atmosphere = AtmosphereClass.Reducing,
                AvailableSolvent = SolventClass.Hydrocarbon,
                RadiationHazard = 0.50,
            });
        Assert(hostileTerran.RequiresControlledHabitat,
            "Hostile Terran environment should expose controlled-habitat demand.");
        Assert(hostileTerran.RequiresGravityMitigation,
            "Hostile Terran environment should expose gravity mitigation demand.");
        Assert(hostileTerran.RequiresSealedHabitat,
            "Incompatible atmosphere should expose sealed-habitat demand.");
        Assert(hostileTerran.RequiresArtificialBiosphere,
            "Incompatible solvent should expose artificial-biosphere demand.");
        Assert(hostileTerran.RequiresRadiationShielding,
            "Radiation hazard above Terran tolerance should expose shielding demand.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void AssertNear(double actual, double expected, string message, double epsilon = 1e-9)
    {
        if (Math.Abs(actual - expected) > epsilon)
            throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
    }
}

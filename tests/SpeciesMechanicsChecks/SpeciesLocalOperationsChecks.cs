using System.Runtime.CompilerServices;
using Game.Simulation.Species;

internal static class SpeciesLocalOperationsChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var evaluator = new SpeciesLocalOperationsProfileEvaluator();
        var terran = SpeciesCatalog.Get(SpeciesCatalog.TerranBaselineId);
        var highGravity = SpeciesCatalog.Get(SpeciesCatalog.CompactHighGravityId);
        var cryogenic = SpeciesCatalog.Get(SpeciesCatalog.CryogenicHydrocarbonId);

        var highGravityHabitat = new HabitatEnvironment(
            highGravity.Environment.GravityG.Preferred,
            highGravity.Environment.TemperatureKelvin.Preferred,
            highGravity.Environment.PressureKPa.Preferred,
            highGravity.Environment.PreferredAtmosphere,
            highGravity.Environment.BiologicalSolvent,
            RadiationHazard: 0.0,
            IsImmersed: highGravity.Environment.RequiresImmersion);

        var highGravityLocal = evaluator.Evaluate(
            new CurrentPopulationSpeciesSnapshot(highGravity.Id, 10.0),
            highGravityHabitat);
        var terranOnHighGravity = evaluator.Evaluate(
            new CurrentPopulationSpeciesSnapshot(terran.Id, 10.0),
            highGravityHabitat);

        AssertNear(highGravityLocal.NaturalHabitability, 1.0,
            "High-gravity species should be naturally suited to its preferred local environment.");
        Assert(!highGravityLocal.RequiresEnvironmentalSupport,
            "High-gravity species should not need artificial support in its preferred environment.");
        Assert(terranOnHighGravity.UnprotectedOperationalCapacity < highGravityLocal.UnprotectedOperationalCapacity,
            "Terran unprotected operation should be physically worse than high-gravity biology in the latter's preferred environment.");
        Assert(terranOnHighGravity.RequiresEnvironmentalSupport,
            "Terran local operations in the high-gravity proving environment should expose support requirements.");
        Assert(terranOnHighGravity.RequiresGravityMitigation,
            "Terran local operations at 1.75g should expose gravity mitigation rather than a generic combat penalty.");

        var terranHome = new HabitatEnvironment(
            terran.Environment.GravityG.Preferred,
            terran.Environment.TemperatureKelvin.Preferred,
            terran.Environment.PressureKPa.Preferred,
            terran.Environment.PreferredAtmosphere,
            terran.Environment.BiologicalSolvent,
            RadiationHazard: 0.0,
            IsImmersed: terran.Environment.RequiresImmersion);
        var terranLocal = evaluator.Evaluate(
            new CurrentPopulationSpeciesSnapshot(terran.Id, 10.0),
            terranHome);
        var highGravityOnTerran = evaluator.Evaluate(
            new CurrentPopulationSpeciesSnapshot(highGravity.Id, 10.0),
            terranHome);

        AssertNear(terranLocal.NaturalHabitability, 1.0,
            "Terran baseline should be naturally suited to its own environment.");
        Assert(highGravityOnTerran.UnprotectedOperationalCapacity < terranLocal.UnprotectedOperationalCapacity,
            "High-gravity biology should itself be disadvantaged away from its preferred gravity instead of owning a universal advantage.");

        var cryogenicOnTerran = evaluator.Evaluate(
            new CurrentPopulationSpeciesSnapshot(cryogenic.Id, 10.0),
            terranHome);
        Assert(cryogenicOnTerran.RequiresSealedHabitat,
            "Cryogenic hydrocarbon biology should require environmental separation in Terran atmosphere.");
        Assert(cryogenicOnTerran.RequiresArtificialBiosphere,
            "Cryogenic hydrocarbon biology should require compatible solvent/biosphere support in Terran conditions.");
        Assert(cryogenicOnTerran.PeakActivityMetabolicDemandMillions > 0.0,
            "Local-operations profile must expose a finite physical peak metabolic load.");

        Assert(highGravityLocal.MusculoskeletalRobustness > terranLocal.MusculoskeletalRobustness,
            "The proving catalog should expose the high-gravity species' stronger musculoskeletal baseline as a physical fact.");
        Assert(highGravityLocal.TypicalAdultMassKg > terranLocal.TypicalAdultMassKg,
            "The proving catalog should expose body-mass differences as physical facts rather than hiding them in a combat score.");
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

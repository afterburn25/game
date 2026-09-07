using Game.Simulation.Species;

var evaluator = new SpeciesEnvironmentEvaluator();

foreach (var species in SpeciesCatalog.All)
{
    var home = new HabitatEnvironment(
        species.Environment.GravityG.Preferred,
        species.Environment.TemperatureKelvin.Preferred,
        species.Environment.PressureKPa.Preferred,
        species.Environment.PreferredAtmosphere,
        species.Environment.BiologicalSolvent,
        RadiationHazard: 0.0,
        IsImmersed: species.Environment.RequiresImmersion);

    var first = evaluator.Evaluate(species, home);
    var second = evaluator.Evaluate(species, home);

    AssertNear(1.0, first.NaturalHabitability, $"{species.Id} should be naturally habitable at its preferred environment.");
    AssertNear(1.0, first.UnprotectedOperationalCapacity, $"{species.Id} should operate naturally at its preferred environment.");
    Assert(first == second, $"{species.Id} assessment must be deterministic for identical inputs.");
    Assert(first.LimitingFactor == EnvironmentalLimitingFactor.None, $"{species.Id} home environment should have no limiting factor.");
}

var terran = SpeciesCatalog.Get(SpeciesCatalog.TerranBaselineId);
var highGravity = SpeciesCatalog.Get(SpeciesCatalog.CompactHighGravityId);
var pelagic = SpeciesCatalog.Get(SpeciesCatalog.PelagicHighPressureId);

var terranOnHighGravityWorld = evaluator.Evaluate(
    terran,
    new HabitatEnvironment(
        GravityG: 1.75,
        TemperatureKelvin: 288.0,
        PressureKPa: 101.3,
        AtmosphereClass.OxygenNitrogen,
        SolventClass.Water,
        RadiationHazard: 0.0));
AssertNear(0.0, terranOnHighGravityWorld.NaturalHabitability, "Terran baseline should not be naturally habitable at 1.75g without mitigation.");
Assert(terranOnHighGravityWorld.RequiresGravityMitigation, "Terran baseline should require gravity mitigation at 1.75g.");
Assert(terranOnHighGravityWorld.LimitingFactor == EnvironmentalLimitingFactor.Gravity, "Gravity should be the limiting factor for the Terran 1.75g check.");

var highGravityOnLowGravityWorld = evaluator.Evaluate(
    highGravity,
    new HabitatEnvironment(
        GravityG: 1.0,
        TemperatureKelvin: 300.0,
        PressureKPa: 160.0,
        AtmosphereClass.OxygenRich,
        SolventClass.Water,
        RadiationHazard: 0.0));
Assert(highGravityOnLowGravityWorld.NaturalHabitability > 0.0 && highGravityOnLowGravityWorld.NaturalHabitability < 0.5,
    "High-gravity species should be disadvantaged, but not instantly impossible, at the test 1g environment.");
Assert(highGravityOnLowGravityWorld.LimitingFactor == EnvironmentalLimitingFactor.Gravity,
    "Gravity should be the limiting factor for the high-gravity species at 1g.");

var adaptedHighGravityPopulation = new PopulationAdaptationState(
    SpeciesCatalog.CompactHighGravityId,
    GravityPreferenceShiftG: -0.50,
    GravityToleranceBonusG: 0.20);
var adaptedHighGravityAssessment = evaluator.Evaluate(
    highGravity,
    new HabitatEnvironment(
        GravityG: 1.0,
        TemperatureKelvin: 300.0,
        PressureKPa: 160.0,
        AtmosphereClass.OxygenRich,
        SolventClass.Water,
        RadiationHazard: 0.0),
    adaptedHighGravityPopulation);
Assert(adaptedHighGravityAssessment.NaturalHabitability > highGravityOnLowGravityWorld.NaturalHabitability,
    "Population-scoped adaptation should improve suitability without altering the base species definition.");
AssertNear(1.75, highGravity.Environment.GravityG.Preferred,
    "Population adaptation must not mutate the immutable species preference.");

var dryPelagicHome = evaluator.Evaluate(
    pelagic,
    new HabitatEnvironment(
        pelagic.Environment.GravityG.Preferred,
        pelagic.Environment.TemperatureKelvin.Preferred,
        pelagic.Environment.PressureKPa.Preferred,
        pelagic.Environment.PreferredAtmosphere,
        pelagic.Environment.BiologicalSolvent,
        RadiationHazard: 0.0,
        IsImmersed: false));
AssertNear(0.0, dryPelagicHome.NaturalHabitability, "Pelagic species should require immersion even when other environmental variables match.");
Assert(dryPelagicHome.RequiresArtificialBiosphere, "Pelagic dry habitat should require an artificial biosphere/immersion solution.");
Assert(dryPelagicHome.LimitingFactor == EnvironmentalLimitingFactor.Immersion,
    "Immersion should be the limiting factor for the dry pelagic check.");

var incompatibleTerranChemistry = evaluator.Evaluate(
    terran,
    new HabitatEnvironment(
        GravityG: 1.0,
        TemperatureKelvin: 288.0,
        PressureKPa: 101.3,
        AtmosphereClass.Reducing,
        SolventClass.Hydrocarbon,
        RadiationHazard: 0.0));
AssertNear(0.0, incompatibleTerranChemistry.NaturalHabitability,
    "Incompatible atmosphere/solvent chemistry must not be averaged away by otherwise comfortable conditions.");
Assert(incompatibleTerranChemistry.RequiresSealedHabitat,
    "Terran population should require a sealed habitat in an incompatible atmosphere.");
Assert(incompatibleTerranChemistry.RequiresArtificialBiosphere,
    "Terran population should require an artificial biosphere with incompatible solvent chemistry.");

var wrongSpeciesAdaptationRejected = false;
try
{
    evaluator.Evaluate(terran,
        new HabitatEnvironment(1.0, 288.0, 101.3, AtmosphereClass.OxygenNitrogen, SolventClass.Water, 0.0),
        PopulationAdaptationState.None(SpeciesCatalog.PelagicHighPressureId));
}
catch (InvalidOperationException)
{
    wrongSpeciesAdaptationRejected = true;
}
Assert(wrongSpeciesAdaptationRejected, "Adaptation state must not be transferable between species IDs.");

Console.WriteLine($"Species mechanics checks passed for {SpeciesCatalog.All.Count} prototype species.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertNear(double expected, double actual, string message, double epsilon = 1e-9)
{
    if (Math.Abs(expected - actual) > epsilon)
    {
        throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
    }
}

using System.Text.Json.Nodes;
using Game.Persistence;
using Game.Simulation.Generation;
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

const long campaignSeed = 42;
var generated = new GalaxyGenerator().Generate(campaignSeed);
var regenerated = new GalaxyGenerator().Generate(campaignSeed);
var generatedSpecies = generated.Civilizations.Select(civilization => civilization.SpeciesId).ToArray();
var regeneratedSpecies = regenerated.Civilizations.Select(civilization => civilization.SpeciesId).ToArray();

Assert(generatedSpecies.SequenceEqual(regeneratedSpecies),
    "Species assignment must be deterministic for the same campaign seed and civilization IDs.");
Assert(generatedSpecies.Distinct(StringComparer.Ordinal).Count() == SpeciesCatalog.All.Count,
    "The deterministic test campaign should exercise every prototype species.");
Assert(generated.Civilizations.All(civilization => SpeciesCatalog.TryGet(civilization.SpeciesId, out _)),
    "Every generated civilization must reference a known species definition.");

var saveService = new CampaignSaveService();
var savePath = Path.Combine(Path.GetTempPath(), $"stellar-continuum-species-{Guid.NewGuid():N}.json");
var legacyPath = savePath + ".v7";
var invalidPath = savePath + ".invalid";

try
{
    saveService.Save(savePath, generated, simulationDays: 123.5);
    Assert(CampaignSaveService.CurrentFormatVersion == 8, "Species identity persistence must use save format v8.");

    var roundTrip = saveService.Load(savePath);
    Assert(roundTrip.Galaxy.Civilizations.Select(civilization => civilization.SpeciesId).SequenceEqual(generatedSpecies),
        "Save v8 must round-trip civilization species IDs exactly.");

    var legacyRoot = JsonNode.Parse(File.ReadAllText(savePath))?.AsObject()
        ?? throw new InvalidOperationException("Could not parse generated v8 save for migration check.");
    legacyRoot["FormatVersion"] = 7;
    foreach (var civilization in legacyRoot["Galaxy"]?["Civilizations"]?.AsArray()
                 ?? throw new InvalidOperationException("Generated save lacks civilization data."))
    {
        civilization?.AsObject().Remove("SpeciesId");
    }
    File.WriteAllText(legacyPath, legacyRoot.ToJsonString());

    var migrated = saveService.Load(legacyPath);
    foreach (var civilization in migrated.Galaxy.Civilizations)
    {
        Assert(civilization.SpeciesId == SpeciesAssignmentPolicy.Assign(campaignSeed, civilization.Id),
            $"Legacy v7 migration must deterministically assign species for civilization {civilization.Id}.");
    }

    var invalidRoot = JsonNode.Parse(File.ReadAllText(savePath))?.AsObject()
        ?? throw new InvalidOperationException("Could not parse generated v8 save for invalid-species check.");
    var firstCivilization = invalidRoot["Galaxy"]?["Civilizations"]?.AsArray().FirstOrDefault()?.AsObject()
        ?? throw new InvalidOperationException("Generated save lacks a first civilization.");
    firstCivilization["SpeciesId"] = "missing_species_definition";
    File.WriteAllText(invalidPath, invalidRoot.ToJsonString());

    var invalidSpeciesRejected = false;
    try
    {
        saveService.Load(invalidPath);
    }
    catch (InvalidDataException)
    {
        invalidSpeciesRejected = true;
    }
    Assert(invalidSpeciesRejected, "Save v8 must reject unknown species IDs instead of silently changing biology.");
}
finally
{
    DeleteIfPresent(savePath);
    DeleteIfPresent(savePath + ".bak");
    DeleteIfPresent(legacyPath);
    DeleteIfPresent(invalidPath);
}

Console.WriteLine($"Species mechanics checks passed for {SpeciesCatalog.All.Count} prototype species, deterministic assignment, and save v8 migration.");

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

static void DeleteIfPresent(string path)
{
    if (File.Exists(path))
    {
        File.Delete(path);
    }
}

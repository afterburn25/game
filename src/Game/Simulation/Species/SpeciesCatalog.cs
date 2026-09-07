using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Simulation.Species;

public static class SpeciesCatalog
{
    public const string TerranBaselineId = "terran_baseline";
    public const string PelagicHighPressureId = "pelagic_high_pressure";
    public const string CompactHighGravityId = "compact_high_gravity";
    public const string CryogenicHydrocarbonId = "cryogenic_hydrocarbon";

    private static readonly SpeciesDefinition[] Definitions =
    {
        new SpeciesDefinition(
            TerranBaselineId,
            "Terran Baseline",
            BiochemicalBasis.CarbonWater,
            HabitatMode.TerrestrialSurface,
            new SpeciesPhysiology(
                TypicalAdultMassKg: 70.0,
                BaselineLifespanYears: 82.0,
                MaturityAgeYears: 18.0,
                BaselineMetabolicDemand: 1.0,
                RadiationTolerance: 0.10,
                MusculoskeletalRobustness: 0.55),
            new SpeciesEnvironmentalPreferences(
                GravityG: new ToleranceBand(1.00, 0.15, 0.55),
                TemperatureKelvin: new ToleranceBand(288.0, 15.0, 55.0),
                PressureKPa: new ToleranceBand(101.3, 35.0, 85.0),
                PreferredAtmosphere: AtmosphereClass.OxygenNitrogen,
                BiologicalSolvent: SolventClass.Water),
            new HashSet<AtmosphereClass>
            {
                AtmosphereClass.OxygenNitrogen,
                AtmosphereClass.OxygenRich,
            },
            new HashSet<SolventClass> { SolventClass.Water },
            BaselineGenerationYears: 28.0).Validated(),

        new SpeciesDefinition(
            PelagicHighPressureId,
            "Pelagic High-Pressure",
            BiochemicalBasis.CarbonWater,
            HabitatMode.Aquatic,
            new SpeciesPhysiology(
                TypicalAdultMassKg: 110.0,
                BaselineLifespanYears: 140.0,
                MaturityAgeYears: 24.0,
                BaselineMetabolicDemand: 0.85,
                RadiationTolerance: 0.18,
                MusculoskeletalRobustness: 0.48),
            new SpeciesEnvironmentalPreferences(
                GravityG: new ToleranceBand(0.85, 0.22, 0.62),
                TemperatureKelvin: new ToleranceBand(282.0, 12.0, 42.0),
                PressureKPa: new ToleranceBand(350.0, 125.0, 300.0),
                PreferredAtmosphere: AtmosphereClass.OxygenNitrogen,
                BiologicalSolvent: SolventClass.Water,
                RequiresImmersion: true),
            new HashSet<AtmosphereClass>
            {
                AtmosphereClass.OxygenNitrogen,
                AtmosphereClass.OxygenRich,
            },
            new HashSet<SolventClass> { SolventClass.Water },
            BaselineGenerationYears: 40.0).Validated(),

        new SpeciesDefinition(
            CompactHighGravityId,
            "Compact High-Gravity",
            BiochemicalBasis.CarbonWater,
            HabitatMode.TerrestrialSurface,
            new SpeciesPhysiology(
                TypicalAdultMassKg: 125.0,
                BaselineLifespanYears: 96.0,
                MaturityAgeYears: 20.0,
                BaselineMetabolicDemand: 1.20,
                RadiationTolerance: 0.26,
                MusculoskeletalRobustness: 0.90),
            new SpeciesEnvironmentalPreferences(
                GravityG: new ToleranceBand(1.75, 0.30, 0.90),
                TemperatureKelvin: new ToleranceBand(300.0, 16.0, 52.0),
                PressureKPa: new ToleranceBand(160.0, 60.0, 140.0),
                PreferredAtmosphere: AtmosphereClass.OxygenRich,
                BiologicalSolvent: SolventClass.Water),
            new HashSet<AtmosphereClass>
            {
                AtmosphereClass.OxygenRich,
                AtmosphereClass.OxygenNitrogen,
            },
            new HashSet<SolventClass> { SolventClass.Water },
            BaselineGenerationYears: 31.0).Validated(),

        new SpeciesDefinition(
            CryogenicHydrocarbonId,
            "Cryogenic Hydrocarbon",
            BiochemicalBasis.CarbonHydrocarbon,
            HabitatMode.TerrestrialSurface,
            new SpeciesPhysiology(
                TypicalAdultMassKg: 90.0,
                BaselineLifespanYears: 360.0,
                MaturityAgeYears: 55.0,
                BaselineMetabolicDemand: 0.22,
                RadiationTolerance: 0.38,
                MusculoskeletalRobustness: 0.42),
            new SpeciesEnvironmentalPreferences(
                GravityG: new ToleranceBand(0.14, 0.08, 0.25),
                TemperatureKelvin: new ToleranceBand(94.0, 12.0, 35.0),
                PressureKPa: new ToleranceBand(150.0, 65.0, 135.0),
                PreferredAtmosphere: AtmosphereClass.Reducing,
                BiologicalSolvent: SolventClass.Hydrocarbon),
            new HashSet<AtmosphereClass> { AtmosphereClass.Reducing },
            new HashSet<SolventClass> { SolventClass.Hydrocarbon },
            BaselineGenerationYears: 82.0).Validated(),
    };

    private static readonly IReadOnlyDictionary<string, SpeciesDefinition> DefinitionsById =
        Definitions.ToDictionary(species => species.Id, StringComparer.Ordinal);

    public static IReadOnlyList<SpeciesDefinition> All => Definitions;

    public static SpeciesDefinition Get(string id)
    {
        if (!DefinitionsById.TryGetValue(id, out var species))
        {
            throw new KeyNotFoundException($"Unknown species ID '{id}'.");
        }

        return species;
    }

    public static bool TryGet(string id, out SpeciesDefinition? species)
    {
        return DefinitionsById.TryGetValue(id, out species);
    }
}

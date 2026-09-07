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
            new SpeciesXenobiologyProfile(
                MolecularChirality.LeftHanded,
                HereditarySystem.NucleicAcidLike,
                CellularOrganization.Cellular,
                UsesProteinLikeCatalysts: true,
                SupportsSelfReplicatingMicroscopicParasites: true),
            new SpeciesPerceptionProfile(
                SensoryModality.VisibleLight |
                SensoryModality.AirborneSound |
                SensoryModality.Vibration |
                SensoryModality.Chemoreception,
                CommunicationModality.AirborneVocal |
                CommunicationModality.VisualGesture),
            new SpeciesMorphology(
                BodyPlan.UprightBilateral,
                LocomotionMode.Bipedal,
                WorkOrientation.Upright,
                TypicalBodyLengthMeters: 1.70,
                TypicalBodyWidthMeters: 0.50,
                TypicalReachMeters: 0.75,
                PrimaryManipulatorCount: 2,
                FineManipulatorCount: 2),
            new SpeciesLifeHistory(
                ReproductiveMode.InternalGestation,
                ReproductiveMaturityYears: 18.0,
                TypicalOffspringPerEvent: 1.05,
                MinimumInterEventYears: 1.5,
                DependentDevelopmentYears: 16.0,
                ReproductiveSpanYears: 32.0,
                BaselineGenerationYears: 28.0)).Validated(),

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
            new SpeciesXenobiologyProfile(
                MolecularChirality.RightHanded,
                HereditarySystem.NucleicAcidLike,
                CellularOrganization.Cellular,
                UsesProteinLikeCatalysts: true,
                SupportsSelfReplicatingMicroscopicParasites: true),
            new SpeciesPerceptionProfile(
                SensoryModality.VisibleLight |
                SensoryModality.WaterborneSound |
                SensoryModality.PressureSense |
                SensoryModality.Chemoreception,
                CommunicationModality.WaterborneVocal |
                CommunicationModality.VisualGesture |
                CommunicationModality.Bioluminescent),
            new SpeciesMorphology(
                BodyPlan.Radial,
                LocomotionMode.AquaticSwimming,
                WorkOrientation.FreeSwimming,
                TypicalBodyLengthMeters: 2.10,
                TypicalBodyWidthMeters: 0.80,
                TypicalReachMeters: 0.90,
                PrimaryManipulatorCount: 4,
                FineManipulatorCount: 4,
                RequiresBuoyantWorkspace: true),
            new SpeciesLifeHistory(
                ReproductiveMode.ExternalEggOrEmbryo,
                ReproductiveMaturityYears: 24.0,
                TypicalOffspringPerEvent: 2.0,
                MinimumInterEventYears: 3.0,
                DependentDevelopmentYears: 10.0,
                ReproductiveSpanYears: 80.0,
                BaselineGenerationYears: 40.0)).Validated(),

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
            new SpeciesXenobiologyProfile(
                MolecularChirality.LeftHanded,
                HereditarySystem.NucleicAcidLike,
                CellularOrganization.Cellular,
                UsesProteinLikeCatalysts: true,
                SupportsSelfReplicatingMicroscopicParasites: true),
            new SpeciesPerceptionProfile(
                SensoryModality.VisibleLight |
                SensoryModality.NearInfrared |
                SensoryModality.AirborneSound |
                SensoryModality.Vibration,
                CommunicationModality.AirborneVocal |
                CommunicationModality.VisualGesture |
                CommunicationModality.Vibration),
            new SpeciesMorphology(
                BodyPlan.HorizontalBilateral,
                LocomotionMode.Quadrupedal,
                WorkOrientation.Horizontal,
                TypicalBodyLengthMeters: 1.35,
                TypicalBodyWidthMeters: 0.75,
                TypicalReachMeters: 0.55,
                PrimaryManipulatorCount: 2,
                FineManipulatorCount: 2),
            new SpeciesLifeHistory(
                ReproductiveMode.InternalGestation,
                ReproductiveMaturityYears: 20.0,
                TypicalOffspringPerEvent: 1.0,
                MinimumInterEventYears: 2.4,
                DependentDevelopmentYears: 15.0,
                ReproductiveSpanYears: 50.0,
                BaselineGenerationYears: 31.0)).Validated(),

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
            new SpeciesXenobiologyProfile(
                MolecularChirality.RightHanded,
                HereditarySystem.AlternativeBiopolymer,
                CellularOrganization.Colonial,
                UsesProteinLikeCatalysts: false,
                SupportsSelfReplicatingMicroscopicParasites: true),
            new SpeciesPerceptionProfile(
                SensoryModality.NearInfrared |
                SensoryModality.Ultraviolet |
                SensoryModality.Vibration |
                SensoryModality.Chemoreception |
                SensoryModality.Electrosense,
                CommunicationModality.Bioluminescent |
                CommunicationModality.Chemical |
                CommunicationModality.Vibration),
            new SpeciesMorphology(
                BodyPlan.Radial,
                LocomotionMode.Multipedal,
                WorkOrientation.Radial,
                TypicalBodyLengthMeters: 1.20,
                TypicalBodyWidthMeters: 1.00,
                TypicalReachMeters: 0.65,
                PrimaryManipulatorCount: 6,
                FineManipulatorCount: 4),
            new SpeciesLifeHistory(
                ReproductiveMode.ExternalEggOrEmbryo,
                ReproductiveMaturityYears: 55.0,
                TypicalOffspringPerEvent: 1.5,
                MinimumInterEventYears: 8.0,
                DependentDevelopmentYears: 30.0,
                ReproductiveSpanYears: 220.0,
                BaselineGenerationYears: 82.0)).Validated(),
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

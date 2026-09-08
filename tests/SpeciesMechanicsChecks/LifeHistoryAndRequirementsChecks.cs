using System.Runtime.CompilerServices;
using Game.Simulation.Species;

internal static class LifeHistoryAndRequirementsChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        foreach (var species in SpeciesCatalog.All)
        {
            var envelope = SpeciesDemographicEnvelopeEvaluator.Evaluate(species);
            Assert(envelope.SpeciesId == species.Id, $"Demographic envelope changed species identity for {species.Id}.");
            Assert(envelope.ReproductiveEventsPerIndividualLifetimeUpperBound > 0.0,
                $"{species.Id} must have a positive biological reproductive-event envelope.");
            Assert(envelope.BiologicalOffspringPerIndividualLifetimeUpperBound > 0.0,
                $"{species.Id} must have a positive biological offspring envelope.");
            Assert(envelope.GenerationsPerCentury > 0.0,
                $"{species.Id} must have a positive generation turnover rate.");
            AssertNear(species.BaselineGenerationYears, species.LifeHistory.BaselineGenerationYears,
                $"{species.Id} generation length must come from life history.");
            AssertNear(species.Physiology.MaturityAgeYears, species.LifeHistory.ReproductiveMaturityYears,
                $"{species.Id} maturity fields must remain internally consistent.");

            var selfCompatibility = SpeciesEquipmentCompatibilityEvaluator.Evaluate(species, species);
            AssertNear(1.0, selfCompatibility.DirectUseCompatibility,
                $"{species.Id} should be fully compatible with equipment designed for itself.");
            Assert(!selfCompatibility.RequiresControlAdaptation,
                $"{species.Id} should not require control adaptation for its own equipment baseline.");
            Assert(!selfCompatibility.RequiresWorkspaceAdaptation,
                $"{species.Id} should not require workspace adaptation for its own equipment baseline.");
            Assert(!selfCompatibility.RequiresEnvironmentalEnclosure,
                $"{species.Id} should not require a cross-species environmental enclosure for its own equipment baseline.");

            var selfCommunication = SpeciesCommunicationCompatibilityEvaluator.Evaluate(species, species);
            AssertNear(1.0, selfCommunication.SharedSensoryCompatibility,
                $"{species.Id} should share its own sensory modalities.");
            AssertNear(1.0, selfCommunication.SharedCommunicationCompatibility,
                $"{species.Id} should share its own natural communication channels.");
            AssertNear(1.0, selfCommunication.NaturalCommunicationCompatibility,
                $"{species.Id} should have full natural channel compatibility with itself.");
            Assert(!selfCommunication.RequiresSensoryTranslation,
                $"{species.Id} should not require sensory translation for itself.");
            Assert(!selfCommunication.RequiresCommunicationMediation,
                $"{species.Id} should not require communication mediation for itself.");
        }

        var terran = SpeciesCatalog.Get(SpeciesCatalog.TerranBaselineId);
        var pelagic = SpeciesCatalog.Get(SpeciesCatalog.PelagicHighPressureId);
        var highGravity = SpeciesCatalog.Get(SpeciesCatalog.CompactHighGravityId);
        var cryogenic = SpeciesCatalog.Get(SpeciesCatalog.CryogenicHydrocarbonId);

        var terranEnvelope = SpeciesDemographicEnvelopeEvaluator.Evaluate(terran);
        var cryogenicEnvelope = SpeciesDemographicEnvelopeEvaluator.Evaluate(cryogenic);
        Assert(terranEnvelope.GenerationsPerCentury > cryogenicEnvelope.GenerationsPerCentury,
            "Shorter-generation Terran baseline should cycle more generations per century than the cryogenic proving species.");

        var requirementsEvaluator = new SpeciesPopulationRequirementsEvaluator();
        var terranCohort = SpeciesPopulationCohort.Founding(SpeciesCatalog.TerranBaselineId, 100.0);
        var terranHome = new HabitatEnvironment(
            terran.Environment.GravityG.Preferred,
            terran.Environment.TemperatureKelvin.Preferred,
            terran.Environment.PressureKPa.Preferred,
            terran.Environment.PreferredAtmosphere,
            terran.Environment.BiologicalSolvent,
            RadiationHazard: 0.0,
            IsImmersed: terran.Environment.RequiresImmersion);
        var terranRequirements = requirementsEvaluator.Evaluate(terranCohort, terranHome);

        AssertNear(100.0, terranRequirements.PopulationMillions,
            "Requirements evaluation must not change population size.");
        AssertNear(100.0, terranRequirements.ReferenceMetabolicDemandMillions,
            "Terran reference metabolic demand should equal population at baseline metabolic demand 1.0.");
        AssertNear(7000.0, terranRequirements.AdultBiomassMillionKg,
            "Adult biomass must be derived from population and typical adult mass.");
        Assert(terranRequirements.RequiredEnvironmentalMitigationCategories == 0,
            "Preferred Terran environment should not require mitigation categories.");
        Assert(!terranRequirements.RequiresControlledHabitat,
            "Preferred Terran environment should not require a controlled habitat.");

        var elevatedGravityRequirements = requirementsEvaluator.Evaluate(
            terranCohort,
            terranHome with { GravityG = 1.30 });
        Assert(elevatedGravityRequirements.Environment.RequiresGravityMitigation,
            "A survivable but stressful elevated-gravity Terran habitat should expose gravity mitigation demand.");
        Assert(elevatedGravityRequirements.RequiresControlledHabitat,
            "Environmental mismatch must flow into a controlled-habitat requirement summary.");

        var cryogenicCohort = SpeciesPopulationCohort.Founding(SpeciesCatalog.CryogenicHydrocarbonId, 100.0);
        var highGravityCohort = SpeciesPopulationCohort.Founding(SpeciesCatalog.CompactHighGravityId, 100.0);
        var highGravityHome = new HabitatEnvironment(
            highGravity.Environment.GravityG.Preferred,
            highGravity.Environment.TemperatureKelvin.Preferred,
            highGravity.Environment.PressureKPa.Preferred,
            highGravity.Environment.PreferredAtmosphere,
            highGravity.Environment.BiologicalSolvent,
            RadiationHazard: 0.0);
        var cryogenicHome = new HabitatEnvironment(
            cryogenic.Environment.GravityG.Preferred,
            cryogenic.Environment.TemperatureKelvin.Preferred,
            cryogenic.Environment.PressureKPa.Preferred,
            cryogenic.Environment.PreferredAtmosphere,
            cryogenic.Environment.BiologicalSolvent,
            RadiationHazard: 0.0);

        var highGravityRequirements = requirementsEvaluator.Evaluate(highGravityCohort, highGravityHome);
        var cryogenicRequirements = requirementsEvaluator.Evaluate(cryogenicCohort, cryogenicHome);
        Assert(highGravityRequirements.ReferenceMetabolicDemandMillions > cryogenicRequirements.ReferenceMetabolicDemandMillions,
            "Different species should expose their biological metabolic requirements without converting them into an arbitrary economic bonus.");

        var terranUsingPelagic = SpeciesEquipmentCompatibilityEvaluator.Evaluate(terran, pelagic);
        var pelagicUsingTerran = SpeciesEquipmentCompatibilityEvaluator.Evaluate(pelagic, terran);
        Assert(terranUsingPelagic.ManipulatorCompatibility < pelagicUsingTerran.ManipulatorCompatibility,
            "Equipment compatibility must be directional when a design assumes more manipulators than the user possesses.");
        Assert(terranUsingPelagic.RequiresEnvironmentalEnclosure,
            "Dry Terran biology should not directly use an immersed Pelagic workspace without environmental adaptation.");
        Assert(pelagicUsingTerran.RequiresEnvironmentalEnclosure,
            "Immersed Pelagic biology should not directly use a dry Terran workspace without environmental adaptation.");

        var highGravityUsingTerran = SpeciesEquipmentCompatibilityEvaluator.Evaluate(highGravity, terran);
        Assert(highGravityUsingTerran.RequiresWorkspaceAdaptation,
            "Horizontal compact high-gravity morphology should require workspace adaptation for an upright Terran baseline.");
        Assert(highGravityUsingTerran.OrientationCompatibility < 1.0,
            "Different working orientation should remain visible in the ergonomic assessment.");

        var cryogenicUsingTerran = SpeciesEquipmentCompatibilityEvaluator.Evaluate(cryogenic, terran);
        Assert(cryogenicUsingTerran.RequiresEnvironmentalEnclosure,
            "Hydrocarbon/reducing-environment biology should require environmental adaptation for Terran-designed crew space.");
        Assert(cryogenicUsingTerran.DirectUseCompatibility < 1.0,
            "Radial multipedal morphology should not be treated as directly identical to Terran ergonomics.");

        var terranHighGravityCommunication = SpeciesCommunicationCompatibilityEvaluator.Evaluate(terran, highGravity);
        var highGravityTerranCommunication = SpeciesCommunicationCompatibilityEvaluator.Evaluate(highGravity, terran);
        Assert(terranHighGravityCommunication.HasAnyNaturalCommunicationChannel,
            "Terran and high-gravity proving species should share at least one natural communication channel.");
        Assert(terranHighGravityCommunication.NaturalCommunicationCompatibility > 0.0,
            "Shared visual/vocal channels should produce nonzero natural physical communication compatibility.");
        AssertNear(terranHighGravityCommunication.SharedSensoryCompatibility,
            highGravityTerranCommunication.SharedSensoryCompatibility,
            "Physical sensory compatibility must be symmetric.");
        AssertNear(terranHighGravityCommunication.NaturalCommunicationCompatibility,
            highGravityTerranCommunication.NaturalCommunicationCompatibility,
            "Natural communication-channel compatibility must be symmetric.");

        var terranCryogenicCommunication = SpeciesCommunicationCompatibilityEvaluator.Evaluate(terran, cryogenic);
        Assert(!terranCryogenicCommunication.HasAnyNaturalCommunicationChannel,
            "Terran and cryogenic proving species should have no direct shared natural communication modality in this mechanical catalog.");
        AssertNear(0.0, terranCryogenicCommunication.NaturalCommunicationCompatibility,
            "No shared communication modality must produce zero natural channel compatibility.");
        Assert(terranCryogenicCommunication.RequiresCommunicationMediation,
            "Species without a shared natural signal channel should require instrumentation/translation mediation rather than a diplomacy penalty.");

        var terranPelagicCommunication = SpeciesCommunicationCompatibilityEvaluator.Evaluate(terran, pelagic);
        Assert(terranPelagicCommunication.HasAnyNaturalCommunicationChannel,
            "Terran and Pelagic proving species should share visual gesture as a natural channel.");
        Assert(terranPelagicCommunication.NaturalCommunicationCompatibility > 0.0 &&
               terranPelagicCommunication.NaturalCommunicationCompatibility < 1.0,
            "Terran/Pelagic communication should be physically possible but incomplete because their acoustic media differ.");
        Assert(terranHighGravityCommunication.NaturalCommunicationCompatibility >
               terranCryogenicCommunication.NaturalCommunicationCompatibility,
            "A pair sharing ordinary visual/vocal channels should have greater natural channel compatibility than a pair sharing none.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertNear(double expected, double actual, string message, double epsilon = 1e-9)
    {
        if (Math.Abs(expected - actual) > epsilon)
        {
            throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
        }
    }
}

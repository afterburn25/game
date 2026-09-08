using System.Runtime.CompilerServices;
using Game.Simulation.Species;

internal static class SupportedHabitatChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var evaluator = new SpeciesSupportedHabitatEvaluator();
        var terran = SpeciesCatalog.Get(SpeciesCatalog.TerranBaselineId);
        var terranCohort = SpeciesPopulationCohort.Founding(terran.Id, 25.0);

        var hostileChemistry = new HabitatEnvironment(
            GravityG: 1.0,
            TemperatureKelvin: 288.0,
            PressureKPa: 101.3,
            AtmosphereClass.Reducing,
            SolventClass.Hydrocarbon,
            RadiationHazard: 0.0);
        var unsupported = evaluator.Evaluate(terranCohort, hostileChemistry, HabitatSupportCapabilities.None);
        AssertNear(0.0, unsupported.NaturalAssessment.NaturalHabitability,
            "Terran natural habitability must remain zero in incompatible atmosphere/solvent chemistry.");
        AssertNear(unsupported.NaturalAssessment.NaturalHabitability,
            unsupported.SupportedAssessment.NaturalHabitability,
            "No support capabilities must not silently improve habitability.");
        Assert(!unsupported.SupportImprovesHabitability,
            "No-support assessment must report no support improvement.");

        var sealedBiosphere = evaluator.Evaluate(
            terranCohort,
            hostileChemistry,
            new HabitatSupportCapabilities(
                CanProvideSealedAtmosphere: true,
                CanProvideCompatibleSolventBiosphere: true));
        Assert(sealedBiosphere.SealedAtmosphereUsed,
            "Incompatible atmosphere should consume sealed-atmosphere support when available.");
        Assert(sealedBiosphere.ArtificialBiosphereUsed,
            "Incompatible solvent should consume artificial-biosphere support when available.");
        AssertNear(1.0, sealedBiosphere.SupportedAssessment.NaturalHabitability,
            "Complete chemistry support should make the otherwise preferred Terran physical environment fully supported.");
        AssertNear(0.0, sealedBiosphere.NaturalAssessment.NaturalHabitability,
            "Providing habitat technology must not rewrite the species' natural habitability.");
        Assert(sealedBiosphere.SupportImprovesHabitability,
            "Complete chemistry support should report an improvement over natural conditions.");

        var highGravityExternal = new HabitatEnvironment(
            GravityG: 1.75,
            TemperatureKelvin: 288.0,
            PressureKPa: 101.3,
            AtmosphereClass.OxygenNitrogen,
            SolventClass.Water,
            RadiationHazard: 0.0);
        var partialGravity = evaluator.Evaluate(
            terranCohort,
            highGravityExternal,
            new HabitatSupportCapabilities(MaximumGravityCorrectionG: 0.30));
        AssertNear(0.30, partialGravity.GravityCorrectionUsedG,
            "Habitat support should never exceed its maximum gravity-correction capability.");
        Assert(partialGravity.SupportedEnvironment.GravityG < highGravityExternal.GravityG,
            "Gravity support should move experienced gravity toward the population preference.");
        Assert(partialGravity.SupportedAssessment.NaturalHabitability > partialGravity.NaturalAssessment.NaturalHabitability,
            "Partial gravity support should improve a gravity-limited habitat even when it does not fully solve it.");

        var fullGravity = evaluator.Evaluate(
            terranCohort,
            highGravityExternal,
            new HabitatSupportCapabilities(MaximumGravityCorrectionG: 0.75));
        AssertNear(1.0, fullGravity.SupportedEnvironment.GravityG,
            "Sufficient gravity support should reach the population's preferred baseline gravity.");
        AssertNear(1.0, fullGravity.SupportedAssessment.NaturalHabitability,
            "Sufficient gravity support should fully remove this isolated gravity mismatch.");

        var pelagic = SpeciesCatalog.Get(SpeciesCatalog.PelagicHighPressureId);
        var pelagicCohort = SpeciesPopulationCohort.Founding(pelagic.Id, 12.0);
        var dryPelagicEnvironment = new HabitatEnvironment(
            pelagic.Environment.GravityG.Preferred,
            pelagic.Environment.TemperatureKelvin.Preferred,
            pelagic.Environment.PressureKPa.Preferred,
            pelagic.Environment.PreferredAtmosphere,
            pelagic.Environment.BiologicalSolvent,
            RadiationHazard: 0.0,
            IsImmersed: false);
        var immersionSupported = evaluator.Evaluate(
            pelagicCohort,
            dryPelagicEnvironment,
            new HabitatSupportCapabilities(CanProvideImmersion: true));
        Assert(immersionSupported.ImmersionSupportUsed,
            "An aquatic species should consume immersion support in an otherwise dry workspace.");
        AssertNear(0.0, immersionSupported.NaturalAssessment.NaturalHabitability,
            "Dry external conditions must remain naturally unsuitable for an immersion-dependent species.");
        AssertNear(1.0, immersionSupported.SupportedAssessment.NaturalHabitability,
            "Explicit immersion support should make the preferred Pelagic environment usable.");

        var radioactive = new HabitatEnvironment(
            1.0,
            288.0,
            101.3,
            AtmosphereClass.OxygenNitrogen,
            SolventClass.Water,
            RadiationHazard: 0.65);
        var shielded = evaluator.Evaluate(
            terranCohort,
            radioactive,
            new HabitatSupportCapabilities(RadiationHazardReduction: 0.60));
        AssertNear(0.60, shielded.RadiationHazardReductionUsed,
            "Radiation support should report the amount of hazard reduction actually used.");
        Assert(shielded.SupportedAssessment.RadiationSuitability > shielded.NaturalAssessment.RadiationSuitability,
            "Radiation shielding should improve physical radiation suitability without changing natural tolerance.");
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

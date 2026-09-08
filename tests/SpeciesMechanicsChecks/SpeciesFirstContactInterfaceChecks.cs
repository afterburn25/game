using System.Runtime.CompilerServices;
using Game.Simulation.Species;

internal static class SpeciesFirstContactInterfaceChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        foreach (var species in SpeciesCatalog.All)
        {
            var self = SpeciesFirstContactInterfaceEvaluator.Evaluate(species, species);
            AssertNear(self.NaturalCommunicationCompatibility, 1.0,
                $"{species.Id} should have full natural communication-channel compatibility with itself.");
            Assert(!self.RequiresSensoryTranslation,
                $"{species.Id} should not require sensory translation with itself.");
            Assert(!self.RequiresCommunicationMediation,
                $"{species.Id} should not require communication mediation with itself.");
            Assert(!self.RequiresMutualEnvironmentalAccommodation,
                $"{species.Id} should not require cross-species environmental accommodation with itself.");
            Assert(!self.RequiresDedicatedNutrition,
                $"{species.Id} should not require xenobiological nutrition separation from itself.");
            Assert(!self.RequiresXenomedicalInterfaceAdaptation,
                $"{species.Id} should not require xenomedical adaptation for itself.");
        }

        var terran = SpeciesCatalog.Get(SpeciesCatalog.TerranBaselineId);
        var highGravity = SpeciesCatalog.Get(SpeciesCatalog.CompactHighGravityId);
        var pelagic = SpeciesCatalog.Get(SpeciesCatalog.PelagicHighPressureId);
        var cryogenic = SpeciesCatalog.Get(SpeciesCatalog.CryogenicHydrocarbonId);

        var terranHighGravity = SpeciesFirstContactInterfaceEvaluator.Evaluate(terran, highGravity);
        var highGravityTerran = SpeciesFirstContactInterfaceEvaluator.Evaluate(highGravity, terran);
        Assert(terranHighGravity.HasAnyNaturalCommunicationChannel,
            "Terran/high-gravity pair should share natural physical signaling channels.");
        Assert(terranHighGravity.NaturalCommunicationCompatibility > 0.0,
            "Terran/high-gravity pair should have nonzero natural channel compatibility.");
        AssertNear(
            terranHighGravity.NaturalCommunicationCompatibility,
            highGravityTerran.NaturalCommunicationCompatibility,
            "Natural communication compatibility should be symmetric between the pair.");
        Assert(terranHighGravity.CrossPathogenTransmissionPotential > 0.0,
            "Shared molecular architecture should expose nonzero cross-pathogen potential rather than assuming aliens are biologically isolated.");
        Assert(!terranHighGravity.NaturalHybridizationPossible,
            "Shared water/carbon molecular architecture must not imply natural reproductive compatibility.");

        var terranPelagic = SpeciesFirstContactInterfaceEvaluator.Evaluate(terran, pelagic);
        Assert(terranPelagic.RequiresMutualEnvironmentalAccommodation,
            "Dry Terran and immersed Pelagic visitors should require environmental accommodation for direct co-presence.");
        Assert(terranPelagic.HasAnyNaturalCommunicationChannel,
            "Terran/Pelagic pair should retain visual gesture as a natural signal channel.");

        var terranCryogenic = SpeciesFirstContactInterfaceEvaluator.Evaluate(terran, cryogenic);
        Assert(!terranCryogenic.HasAnyNaturalCommunicationChannel,
            "Terran/cryogenic proving pair should have no direct shared natural signaling modality.");
        Assert(terranCryogenic.RequiresCommunicationMediation,
            "No natural signaling overlap should expose a translation/instrumentation requirement, not a diplomacy penalty.");
        Assert(terranCryogenic.RequiresMutualEnvironmentalAccommodation,
            "Terran/cryogenic pair should require separated or adapted physical environments.");
        Assert(terranCryogenic.RequiresDedicatedNutrition,
            "Incompatible biochemistry should expose dedicated nutrition requirements.");
        Assert(terranCryogenic.RequiresXenomedicalInterfaceAdaptation,
            "Distant xenobiology should expose xenomedical interface requirements.");
        Assert(!terranCryogenic.NaturalHybridizationPossible,
            "Terran/cryogenic natural hybridization should remain impossible absent an explicit relationship.");
        Assert(terranCryogenic.RequiresTechnicalMediation,
            "Terran/cryogenic first contact should expose technical mediation needs without encoding political attitude.");
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

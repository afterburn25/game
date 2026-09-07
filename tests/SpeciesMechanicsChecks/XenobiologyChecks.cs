using System.Runtime.CompilerServices;
using Game.Simulation.Species;

internal static class XenobiologyChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        foreach (var species in SpeciesCatalog.All)
        {
            var self = SpeciesXenobiologyCompatibilityEvaluator.Evaluate(species, species);
            AssertNear(1.0, self.BiochemicalInteroperability,
                $"{species.Id} must be biochemically interoperable with itself.");
            AssertNear(1.0, self.NutritionalCrossCompatibility,
                $"{species.Id} must be nutritionally compatible with its own baseline biology.");
            AssertNear(1.0, self.TissueIntegrationPotential,
                $"{species.Id} must have full same-species tissue compatibility potential at the abstract baseline.");
            Assert(self.NaturalHybridizationPossible,
                $"Same-species baseline must not be rejected by the explicit reproductive-relationship layer.");
        }

        var terran = SpeciesCatalog.Get(SpeciesCatalog.TerranBaselineId);
        var pelagic = SpeciesCatalog.Get(SpeciesCatalog.PelagicHighPressureId);
        var highGravity = SpeciesCatalog.Get(SpeciesCatalog.CompactHighGravityId);
        var cryogenic = SpeciesCatalog.Get(SpeciesCatalog.CryogenicHydrocarbonId);

        var terranHighGravity = SpeciesXenobiologyCompatibilityEvaluator.Evaluate(terran, highGravity);
        var highGravityTerran = SpeciesXenobiologyCompatibilityEvaluator.Evaluate(highGravity, terran);
        AssertNear(1.0, terranHighGravity.BiochemicalInteroperability,
            "Terran and compact high-gravity proving species intentionally share broad carbon/water chemistry.");
        Assert(terranHighGravity.CrossPathogenTransmissionPotential > 0.0,
            "Shared molecular/cellular architecture should expose nonzero cross-pathogen potential when both ecologies support microscopic parasites.");
        Assert(terranHighGravity.RequiresCrossSpeciesQuarantineAssessment,
            "Nonzero plausible cross-pathogen potential should trigger quarantine assessment, not claim an outbreak already exists.");
        AssertNear(0.0, terranHighGravity.NaturalReproductiveCompatibility,
            "Shared carbon/water chemistry must never infer natural cross-species reproductive compatibility.");
        Assert(!terranHighGravity.NaturalHybridizationPossible,
            "Natural hybridization must remain impossible without an explicit authored pair relationship.");
        AssertNear(terranHighGravity.CrossPathogenTransmissionPotential,
            highGravityTerran.CrossPathogenTransmissionPotential,
            "Cross-pathogen compatibility potential should be symmetric for this molecular model.");

        var terranPelagic = SpeciesXenobiologyCompatibilityEvaluator.Evaluate(terran, pelagic);
        Assert(terranPelagic.BiochemicalInteroperability > 0.0,
            "Opposite-chirality water/carbon proving species may share broad chemistry while remaining molecularly difficult.");
        Assert(terranPelagic.NutritionalCrossCompatibility < terranHighGravity.NutritionalCrossCompatibility,
            "Opposite nutrient chirality should reduce nutritional cross-compatibility relative to similar molecular architecture.");
        AssertNear(0.0, terranPelagic.NaturalReproductiveCompatibility,
            "Opposite chirality is not the reason for zero reproductive compatibility; the absence of an explicit species-pair relationship is.");

        var terranCryogenic = SpeciesXenobiologyCompatibilityEvaluator.Evaluate(terran, cryogenic);
        Assert(terranCryogenic.BiochemicalInteroperability < terranHighGravity.BiochemicalInteroperability,
            "Water biology and cryogenic hydrocarbon biology should expose much weaker biochemical interoperability.");
        Assert(terranCryogenic.RequiresDedicatedNutrition,
            "Strongly different solvent/macromolecular biology should require dedicated nutrition/feedstock handling.");
        Assert(terranCryogenic.RequiresXenomedicalInterfaceAdaptation,
            "Strongly different molecular organization should require xenomedical interface adaptation.");
        AssertNear(0.0, terranCryogenic.NaturalReproductiveCompatibility,
            "Distant xenobiology must not be assigned reproductive compatibility without explicit relationship data.");

        var relationForward = SpeciesBiologicalRelationshipCatalog.Get(terran.Id, highGravity.Id);
        var relationReverse = SpeciesBiologicalRelationshipCatalog.Get(highGravity.Id, terran.Id);
        AssertNear(relationForward.NaturalReproductiveCompatibility,
            relationReverse.NaturalReproductiveCompatibility,
            "Explicit biological relationship lookup must be pair-order independent.");
        Assert(relationForward.NaturalViableHybridOffspringPossible == relationReverse.NaturalViableHybridOffspringPossible,
            "Explicit biological relationship lookup must preserve pair-order-independent hybrid viability.");
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

using System.Runtime.CompilerServices;
using Game.Simulation.Species;

internal static class MetabolismChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        foreach (var species in SpeciesCatalog.All)
        {
            var cohort = SpeciesPopulationCohort.Founding(species.Id, 100.0);
            var envelope = SpeciesMetabolicEnvelopeEvaluator.Evaluate(cohort);

            AssertNear(100.0, envelope.PopulationMillions,
                $"{species.Id} metabolic evaluation must preserve population size.");
            Assert(envelope.RestingDemandMillions <= envelope.BaselineDemandMillions,
                $"{species.Id} resting demand must not exceed baseline demand.");
            Assert(envelope.PeakActivityDemandMillions >= envelope.BaselineDemandMillions,
                $"{species.Id} peak activity demand must not fall below baseline demand.");
            Assert(envelope.TypicalDayAverageDemandMillions >= envelope.RestingDemandMillions &&
                   envelope.TypicalDayAverageDemandMillions <= envelope.BaselineDemandMillions,
                $"{species.Id} typical day demand should remain between rest and baseline demand.");

            if (envelope.HasNaturalDormancy)
            {
                Assert(envelope.DormantDemandMillions < envelope.BaselineDemandMillions,
                    $"{species.Id} natural dormancy must materially reduce metabolic demand.");
                Assert(envelope.MaximumNaturalDormancyDays > 0.0,
                    $"{species.Id} dormancy must have a finite positive maximum natural duration.");
            }
            else
            {
                AssertNear(envelope.BaselineDemandMillions, envelope.DormantDemandMillions,
                    $"{species.Id} without natural dormancy must not receive a fake dormant-demand discount.");
                AssertNear(0.0, envelope.MaximumNaturalDormancyDays,
                    $"{species.Id} without natural dormancy must not have a natural dormancy duration.");
            }
        }

        var terran = SpeciesCatalog.Get(SpeciesCatalog.TerranBaselineId);
        var pelagic = SpeciesCatalog.Get(SpeciesCatalog.PelagicHighPressureId);
        var highGravity = SpeciesCatalog.Get(SpeciesCatalog.CompactHighGravityId);
        var cryogenic = SpeciesCatalog.Get(SpeciesCatalog.CryogenicHydrocarbonId);

        var terranEnvelope = SpeciesMetabolicEnvelopeEvaluator.Evaluate(
            SpeciesPopulationCohort.Founding(terran.Id, 100.0));
        var pelagicEnvelope = SpeciesMetabolicEnvelopeEvaluator.Evaluate(
            SpeciesPopulationCohort.Founding(pelagic.Id, 100.0));
        var highGravityEnvelope = SpeciesMetabolicEnvelopeEvaluator.Evaluate(
            SpeciesPopulationCohort.Founding(highGravity.Id, 100.0));
        var cryogenicEnvelope = SpeciesMetabolicEnvelopeEvaluator.Evaluate(
            SpeciesPopulationCohort.Founding(cryogenic.Id, 100.0));

        Assert(!terranEnvelope.HasNaturalDormancy,
            "Terran baseline should not receive a natural long-duration dormancy capability in the proving catalog.");
        Assert(!highGravityEnvelope.HasNaturalDormancy,
            "Compact high-gravity proving species should not receive a natural dormancy capability by default.");
        Assert(pelagicEnvelope.NaturalDormancyMode == DormancyMode.Torpor,
            "Pelagic proving species should exercise bounded natural torpor mechanics.");
        Assert(cryogenicEnvelope.NaturalDormancyMode == DormancyMode.DeepDormancy,
            "Cryogenic proving species should exercise deep natural dormancy mechanics.");
        Assert(cryogenicEnvelope.DormantDemandMillions < pelagicEnvelope.DormantDemandMillions,
            "Deep-dormancy proving biology should expose lower dormant metabolic demand than ordinary torpor at equal population.");
        Assert(highGravityEnvelope.BaselineDemandMillions > terranEnvelope.BaselineDemandMillions,
            "Higher baseline metabolic demand should create a larger life-support demand at equal population without becoming a productivity penalty.");
        Assert(cryogenicEnvelope.BaselineDemandMillions < terranEnvelope.BaselineDemandMillions,
            "Low-rate cryogenic metabolism should expose lower reference life-support demand at equal population.");
        Assert(cryogenicEnvelope.MaximumNaturalDormancyDays < 365.0,
            "Natural dormancy in the proving catalog remains bounded and is not indefinite stasis.");
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

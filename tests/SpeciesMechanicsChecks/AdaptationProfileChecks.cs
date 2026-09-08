using System.Runtime.CompilerServices;
using Game.Simulation.Species;

internal static class AdaptationProfileChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var progression = new PopulationAdaptationProgression();
        var terran = SpeciesCatalog.Get(SpeciesCatalog.TerranBaselineId);
        var cryogenic = SpeciesCatalog.Get(SpeciesCatalog.CryogenicHydrocarbonId);

        Assert(terran.AdaptationProfile.MultigenerationalAdaptability > cryogenic.AdaptationProfile.MultigenerationalAdaptability,
            "Mechanical proving catalog should exercise different multigenerational adaptability values.");
        Assert(terran.AdaptationProfile.AcclimatizationResponsiveness > cryogenic.AdaptationProfile.AcclimatizationResponsiveness,
            "Mechanical proving catalog should exercise different acclimatization responsiveness values.");

        var terranHabitat = AnalogousGravityStressHabitat(terran, normalizedDistanceBeyondComfort: 0.25);
        var cryogenicHabitat = AnalogousGravityStressHabitat(cryogenic, normalizedDistanceBeyondComfort: 0.25);

        var terranStart = SpeciesPopulationCohort.Founding(terran.Id, 50.0);
        var cryogenicStart = SpeciesPopulationCohort.Founding(cryogenic.Id, 50.0);

        var terranAfterTenGenerations = progression.Advance(
            terranStart,
            terranHabitat,
            terran.BaselineGenerationYears * 10.0);
        var cryogenicAfterTenGenerations = progression.Advance(
            cryogenicStart,
            cryogenicHabitat,
            cryogenic.BaselineGenerationYears * 10.0);

        var terranNormalizedShift =
            Math.Abs(terranAfterTenGenerations.Adaptation.GravityPreferenceShiftG) /
            terran.Environment.GravityG.SurvivableDeviation;
        var cryogenicNormalizedShift =
            Math.Abs(cryogenicAfterTenGenerations.Adaptation.GravityPreferenceShiftG) /
            cryogenic.Environment.GravityG.SurvivableDeviation;

        Assert(terranNormalizedShift > cryogenicNormalizedShift,
            "With equivalent normalized stress and equal local generations, the higher-plasticity proving species should shift farther.");
        Assert(terranNormalizedShift <= terran.AdaptationProfile.MaximumNaturalPreferenceShiftFraction + 1e-12,
            "Terran natural preference shift must remain within its species-specific ceiling.");
        Assert(cryogenicNormalizedShift <= cryogenic.AdaptationProfile.MaximumNaturalPreferenceShiftFraction + 1e-12,
            "Cryogenic natural preference shift must remain within its species-specific ceiling.");

        var terranOneYear = progression.Advance(terranStart, terranHabitat, 1.0);
        var cryogenicOneYear = progression.Advance(cryogenicStart, cryogenicHabitat, 1.0);
        Assert(terranOneYear.Adaptation.Acclimatization > cryogenicOneYear.Adaptation.Acclimatization,
            "Higher acclimatization responsiveness should produce a larger short-term response to analogous physical stress.");

        var impossibleTerran = progression.Advance(
            terranStart,
            new HabitatEnvironment(
                terran.Environment.GravityG.Preferred,
                terran.Environment.TemperatureKelvin.Preferred,
                terran.Environment.PressureKPa.Preferred,
                AtmosphereClass.Reducing,
                SolventClass.Hydrocarbon,
                RadiationHazard: 0.0),
            terran.BaselineGenerationYears * 100.0);
        AssertNear(0.0, impossibleTerran.Adaptation.Acclimatization,
            "Even a relatively plastic species must not acclimatize through incompatible atmospheric/solvent chemistry.");
        AssertNear(0.0, impossibleTerran.Adaptation.GravityToleranceBonusG,
            "Biological plasticity must not act when the habitat is chemically incompatible and naturally uninhabitable.");
    }

    private static HabitatEnvironment AnalogousGravityStressHabitat(
        SpeciesDefinition species,
        double normalizedDistanceBeyondComfort)
    {
        var gravityBand = species.Environment.GravityG;
        var gravity = gravityBand.Preferred + gravityBand.ComfortableDeviation +
                      ((gravityBand.SurvivableDeviation - gravityBand.ComfortableDeviation) * normalizedDistanceBeyondComfort);
        return new HabitatEnvironment(
            gravity,
            species.Environment.TemperatureKelvin.Preferred,
            species.Environment.PressureKPa.Preferred,
            species.Environment.PreferredAtmosphere,
            species.Environment.BiologicalSolvent,
            RadiationHazard: 0.0,
            IsImmersed: species.Environment.RequiresImmersion);
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

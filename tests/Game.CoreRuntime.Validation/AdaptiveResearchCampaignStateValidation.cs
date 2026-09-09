using Game.Simulation.Generation;
using Game.Simulation.Research.Adaptive;
using Game.Simulation.Species;

namespace Game.CoreRuntime.Validation;

internal static class AdaptiveResearchCampaignStateValidation
{
    public static void Run()
    {
        var root = AdaptiveResearchDataLocator.FindDataRoot();
        var runtime = AdaptiveResearchStrategicRuntime.LoadFromDirectory(root);
        var galaxy = new GalaxyGenerator().Generate(94217, new GalaxyGenerationSettings());
        var campaign = new AdaptiveResearchCampaignFactory(runtime).Create(galaxy);

        Require(campaign.Civilizations.Count == galaxy.Civilizations.Count,
            "not every campaign civilization received Adaptive Research state");
        foreach (var civilization in galaxy.Civilizations)
        {
            var state = campaign.GetCivilization(civilization.Id);
            var start = campaign.Starts[civilization.Id];
            Require(state.CivilizationId == $"civilization:{civilization.Id}",
                "research state used an unstable civilization identity");
            Require(start.SpeciesId == civilization.SpeciesId,
                "research start lost its owning species identity");
            Require(state.TotalEffectiveResearchLabs > 0 && state.NodeStates.Count > 0,
                "starting research history did not compose a playable state");
            Require(state.ApplicabilityContexts.ContainsKey($"species:{civilization.SpeciesId}"),
                "starting state omitted its primary species context");
        }

        var profileBySpecies = galaxy.Civilizations
            .GroupBy(value => value.SpeciesId)
            .ToDictionary(group => group.Key, group => campaign.Starts[group.First().Id].ReferenceProfileId);
        Require(profileBySpecies[SpeciesCatalog.TerranBaselineId] == AdaptiveResearchCampaignFactory.TerranProfileId,
            "Terran civilization did not receive the humanlike Sol history");
        Require(profileBySpecies[SpeciesCatalog.PelagicHighPressureId] == AdaptiveResearchCampaignFactory.PelagicProfileId,
            "pelagic civilization did not receive pressure/liquid-medium history");
        Require(profileBySpecies[SpeciesCatalog.CompactHighGravityId] == AdaptiveResearchCampaignFactory.HighGravityProfileId,
            "high-gravity civilization did not receive high-gravity history");
        Require(profileBySpecies[SpeciesCatalog.CryogenicHydrocarbonId] == AdaptiveResearchCampaignFactory.CryogenicHydrocarbonProfileId,
            "hydrocarbon civilization did not receive compatible biochemical history");

        var pelagic = galaxy.Civilizations.First(value => value.SpeciesId == SpeciesCatalog.PelagicHighPressureId);
        var pelagicState = campaign.GetCivilization(pelagic.Id);
        var pelagicContext = $"species:{pelagic.SpeciesId}";
        Require(pelagicState.HasApplicabilityTrait(pelagicContext, "liquid_medium_native") &&
                pelagicState.GetPressure("high_pressure_environment") >= 28,
            "pelagic start omitted its defining medium and pressure conditions");

        var cryogenic = galaxy.Civilizations.First(value => value.SpeciesId == SpeciesCatalog.CryogenicHydrocarbonId);
        var cryogenicState = campaign.GetCivilization(cryogenic.Id);
        var cryogenicContext = $"species:{cryogenic.SpeciesId}";
        Require(cryogenicState.HasApplicabilityTrait(cryogenicContext, "hydrocarbon_solvent_biology") &&
                !cryogenicState.HasApplicabilityTrait(cryogenicContext, "water_solvent_biology"),
            "cryogenic start inherited incompatible water-based biology");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

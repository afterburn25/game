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

        var playerId = galaxy.PlayerCivilizationId;
        var player = campaign.GetCivilization(playerId);
        Require(runtime.Authority.StartDirectedResearch(player, "fusion_power", 6).Accepted,
            "player could not start a visible Adaptive Research program");
        var events = new AdaptiveResearchCampaignSimulation().Advance(
            galaxy, campaign, elapsedDays: 36525, currentSimulationDay: 36525);
        Require(events.Any(value => value.CivilizationId == playerId && value.NodeId == "fusion_power") &&
                player.HasEstablishedKnowledge("fusion_power"),
            "campaign time did not advance Adaptive Research to mature knowledge");
        var legacy = galaxy.Technologies.Single(value => value.CivilizationId == playerId);
        Require(!legacy.CompletedTechnologyIds.Contains("orbital_industry") &&
                legacy.CompletedTechnologyIds.Contains("deep_space_sensors"),
            "Adaptive Research skipped the intended In-Space Assembly gate or lost mature sensor knowledge");
        Require(runtime.Authority.StartDirectedResearch(player, "fusion_propulsion", 6).Accepted,
            "mature fusion power did not expose the propulsion program");
        _ = new AdaptiveResearchCampaignSimulation().Advance(
            galaxy, campaign, elapsedDays: 36525, currentSimulationDay: 73050);
        Require(legacy.CompletedTechnologyIds.Contains("fusion_propulsion"),
            "mature Adaptive fusion propulsion did not satisfy its transitional gameplay gate");
        Require(runtime.Authority.StartDirectedResearch(player, "in_space_assembly", 6).Accepted,
            "starting orbital history did not expose In-Space Assembly");
        _ = new AdaptiveResearchCampaignSimulation().Advance(
            galaxy, campaign, elapsedDays: 36525, currentSimulationDay: 109575);
        Require(legacy.CompletedTechnologyIds.Contains("orbital_industry"),
            "mature In-Space Assembly did not satisfy the Orbital Industry gameplay gate");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

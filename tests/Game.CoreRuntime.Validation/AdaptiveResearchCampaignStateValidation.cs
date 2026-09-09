using Game.Campaign;
using Game.Simulation.Generation;
using Game.Simulation.Research.Adaptive;
using Game.Simulation.Species;
using Game.Simulation;

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
        var startingPlayerLabs = player.TotalEffectiveResearchLabs;
        var legacyStep = new GalaxySimulationStepCoordinator(advanceLegacyResearch: false)
            .Advance(galaxy, 1);
        Require(legacyStep.ResearchEvents.Count == 0 &&
                galaxy.Technologies.All(value => value.ActiveResearchId is null),
            "integrated campaign advanced the retired linear research loop");
        var aiCivilization = galaxy.Civilizations.First(value => !value.IsPlayer && !value.IsSeededAncient);
        var aiState = campaign.GetCivilization(aiCivilization.Id);
        Require(aiState.ActiveProjects.Count == 0 && player.ActiveProjects.Count == 0,
            "fresh Adaptive Research campaign invented active work");
        _ = new AdaptiveResearchCampaignSimulation().Advance(
            galaxy, campaign, elapsedDays: 1, currentSimulationDay: 1);
        Require(aiState.ActiveProjects.Values.Any(value => !value.Paused) && player.ActiveProjects.Count == 0,
            "Adaptive Research AI did not choose work or auto-selected for the player");
        galaxy.ConstructionStates.Single(value => value.CivilizationId == playerId)
            .CompletedProjectIds.Add("research_network");
        _ = new AdaptiveResearchCampaignSimulation().Advance(
            galaxy, campaign, elapsedDays: 1, currentSimulationDay: 2);
        Require(Math.Abs(player.TotalEffectiveResearchLabs -
                         (startingPlayerLabs + AdaptiveResearchCampaignSimulation.PlanetaryResearchNetworkLabCount)) < 0.000001,
            "completed Planetary Research Network did not add its physical Effective Research Labs");
        var networkLabs = player.Expertise.Institutions.Values.Count(value =>
            value.InstitutionInstanceId == "construction:research_network");
        _ = new AdaptiveResearchCampaignSimulation().Advance(
            galaxy, campaign, elapsedDays: 1, currentSimulationDay: 3);
        Require(player.Expertise.Institutions.Values.Count(value =>
                    value.InstitutionInstanceId == "construction:research_network") == networkLabs &&
                Math.Abs(player.TotalEffectiveResearchLabs -
                         (startingPlayerLabs + AdaptiveResearchCampaignSimulation.PlanetaryResearchNetworkLabCount)) < 0.000001,
            "Planetary Research Network duplicated laboratory capacity on a later simulation step");
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

        VerifyPlayableWarpPath(runtime);
    }

    private static void VerifyPlayableWarpPath(AdaptiveResearchStrategicRuntime runtime)
    {
        var galaxy = new GalaxyGenerator().Generate(20260908, new GalaxyGenerationSettings());
        var campaign = new AdaptiveResearchCampaignFactory(runtime).Create(galaxy);
        var civilization = galaxy.Civilizations.Single(value => value.Id == galaxy.PlayerCivilizationId);
        var state = campaign.GetCivilization(civilization.Id);
        var construction = galaxy.ConstructionStates.Single(value => value.CivilizationId == civilization.Id);
        var simulation = new AdaptiveResearchCampaignSimulation();
        var elapsedDays = 0.0;

        for (var step = 0; step < EarlyCampaignResearchPlan.WarpCapabilityPath.Count + 3 &&
             !state.HasCapability("experimental_interstellar_transit"); step++)
        {
            var view = runtime.Authority.Kernel.BuildView(state, $"species:{civilization.SpeciesId}");
            if (state.ActiveProjects.Count > 0)
            {
                elapsedDays += 36525;
                _ = simulation.Advance(galaxy, campaign, 36525, elapsedDays);
                continue;
            }
            if (state.HasEstablishedKnowledge("warp_field_control") &&
                !construction.CompletedProjectIds.Contains("warp_test_facility"))
            {
                var blockedPrototype = view.VisibleNodes.Single(value => value.NodeId == "prototype_warp_drive");
                Require(blockedPrototype.Blockers.Any(value => value.Code == ResearchBlockerCode.MissingFacilityCapability),
                    "Prototype Warp bypassed its physical test-facility requirement");
                construction.CompletedProjectIds.Add("warp_test_facility");
                elapsedDays += 1;
                _ = simulation.Advance(galaxy, campaign, 1, elapsedDays);
                continue;
            }
            var next = EarlyCampaignResearchPlan.WarpCapabilityPath
                .Select(id => view.VisibleNodes.FirstOrDefault(value => value.NodeId == id))
                .FirstOrDefault(value => value is { State: ResearchMaturity.Investigable } &&
                    value.Blockers.Count == 0);
            Require(next is not null,
                "playable Adaptive Research path stalled before experimental interstellar transit: " +
                string.Join("; ", EarlyCampaignResearchPlan.WarpCapabilityPath.Select(id =>
                {
                    var visible = view.VisibleNodes.FirstOrDefault(value => value.NodeId == id);
                    return visible is null ? $"{id}=unknown" :
                        $"{id}={visible.State}[{string.Join(',', visible.Blockers.Select(value => value.Code))}]";
                })));
            var node = runtime.Authority.Catalog.GetNode(next!.NodeId);
            var labs = Math.Min(node.ProjectRequirements.RecommendedLabs, state.FreeEffectiveLabs);
            Require(runtime.Authority.StartDirectedResearch(
                    state, node.Id, labs, targetApplicabilityContextId: next.TargetApplicabilityContextId).Accepted,
                $"playable Adaptive Research path could not start {node.Name}");
            elapsedDays += 36525;
            _ = simulation.Advance(galaxy, campaign, 36525, elapsedDays);
        }

        Require(state.HasCapability("experimental_interstellar_transit") &&
                galaxy.Technologies.Single(value => value.CivilizationId == civilization.Id)
                    .CompletedTechnologyIds.Contains("prototype_warp_drive"),
            "playable Adaptive Research path did not reach the campaign's warp capability");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

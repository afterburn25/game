using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Research.Adaptive;

public sealed record AdaptiveResearchCampaignEvent(
    int CivilizationId,
    string NodeId,
    string Message,
    bool IsOutcome);

/// <summary>Advances the live campaign sidecar from accepted simulation time.</summary>
public sealed class AdaptiveResearchCampaignSimulation
{
    public const int PlanetaryResearchNetworkLabCount = 4;
    private const string PlanetaryResearchNetworkInstitutionId = "construction:research_network";

    public IReadOnlyList<AdaptiveResearchCampaignEvent> Advance(
        GalaxyState galaxy,
        AdaptiveResearchCampaignState campaign,
        double elapsedDays,
        double currentSimulationDay)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(campaign);
        if (!double.IsFinite(elapsedDays) || elapsedDays < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedDays));
        if (!double.IsFinite(currentSimulationDay) || currentSimulationDay < 0)
            throw new ArgumentOutOfRangeException(nameof(currentSimulationDay));
        if (elapsedDays == 0) return Array.Empty<AdaptiveResearchCampaignEvent>();

        var elapsedYears = elapsedDays / 365.25;
        var currentYear = 2050.0 + currentSimulationDay / 365.25;
        var events = new List<AdaptiveResearchCampaignEvent>();
        foreach (var civilization in galaxy.Civilizations.Where(value => !value.IsSeededAncient).OrderBy(value => value.Id))
        {
            var state = campaign.GetCivilization(civilization.Id);
            SynchronizeResearchFacilities(galaxy, civilization.Id, campaign, state);
            if (civilization.DevelopmentStage == CivilizationDevelopmentStage.PreWarp &&
                state.GetPressure("interstellar_distance") < 45)
            {
                var pressureEvents = campaign.Runtime.Authority.SetPressure(
                    state, "interstellar_distance", 45);
                events.AddRange(pressureEvents.Where(value => value.NodeId is not null).Select(value =>
                    new AdaptiveResearchCampaignEvent(civilization.Id, value.NodeId!, value.Message, false)));
            }
            if (!civilization.IsPlayer && state.ActiveProjects.Values.All(value => value.Paused))
            {
                var candidate = campaign.Runtime.Agenda.BuildVisibleShortlist(state)
                    .FirstOrDefault(value => value.CanStart);
                if (candidate is not null)
                {
                    var start = campaign.Runtime.Authority.StartDirectedResearch(
                        state,
                        candidate.NodeId,
                        candidate.RequestedEffectiveLabs);
                    if (!start.Accepted)
                        throw new InvalidOperationException(
                            $"Adaptive Research AI selected invalid project '{candidate.NodeId}': {start.Message}");
                    events.AddRange(start.Events.Where(value => value.NodeId is not null).Select(value =>
                        new AdaptiveResearchCampaignEvent(civilization.Id, value.NodeId!, value.Message, false)));
                }
            }
            var runtimeEvents = campaign.Runtime.Authority.AdvanceProjects(state, elapsedYears, currentYear);
            events.AddRange(runtimeEvents.Where(value => value.NodeId is not null).Select(value =>
                new AdaptiveResearchCampaignEvent(civilization.Id, value.NodeId!, value.Message, false)));

            foreach (var pending in state.ActiveProjects.Values
                         .Where(value => value.Paused && value.PauseReason == "hypothesis_resolution_required")
                         .OrderBy(value => value.NodeId, StringComparer.Ordinal)
                         .ToArray())
            {
                var resolution = campaign.Runtime.Outcomes.ResolvePendingHypothesis(
                    state,
                    pending.NodeId,
                    galaxy.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    currentYear,
                    pending.TargetApplicabilityContextId);
                if (!resolution.Accepted)
                    throw new InvalidOperationException(
                        $"Pending hypothesis '{pending.NodeId}' could not resolve: {resolution.Message}");
                events.AddRange(resolution.ResearchEvents.Where(value => value.NodeId is not null).Select(value =>
                    new AdaptiveResearchCampaignEvent(civilization.Id, value.NodeId!, value.Message, false)));
                events.AddRange(resolution.OutcomeEvents.Select(value =>
                    new AdaptiveResearchCampaignEvent(civilization.Id, value.NodeId, value.Message, true)));
            }
        }

        AdaptiveResearchLegacyCapabilityBridge.Synchronize(galaxy, campaign);
        return events;
    }

    private static void SynchronizeResearchFacilities(
        GalaxyState galaxy,
        int civilizationId,
        AdaptiveResearchCampaignState campaign,
        AdaptiveResearchCivilizationState state)
    {
        var construction = galaxy.ConstructionStates.First(value => value.CivilizationId == civilizationId);
        var networkComplete = construction.CompletedProjectIds.Contains("research_network");
        var networkInstitution = state.Expertise.Institutions.GetValueOrDefault(
            PlanetaryResearchNetworkInstitutionId);
        if (networkComplete && (networkInstitution is null ||
                                networkInstitution.InstitutionArchetypeId != "general_research_laboratory" ||
                                networkInstitution.TotalCount != PlanetaryResearchNetworkLabCount ||
                                networkInstitution.ActiveCount != PlanetaryResearchNetworkLabCount))
        {
            campaign.Runtime.Authority.Expertise.SetInstitution(
                state,
                PlanetaryResearchNetworkInstitutionId,
                "general_research_laboratory",
                PlanetaryResearchNetworkLabCount,
                PlanetaryResearchNetworkLabCount);
        }
        else if (!networkComplete && networkInstitution is not null)
        {
            campaign.Runtime.Authority.Expertise.RemoveInstitution(
                state, PlanetaryResearchNetworkInstitutionId);
        }

        if (!construction.CompletedProjectIds.Contains("warp_test_facility")) return;
        foreach (var capability in new[]
                 {
                     "precision_measurement",
                     "high_energy_experimentation",
                     "field_physics_experimentation",
                     "large_scale_prototyping",
                 })
            campaign.Runtime.Authority.Kernel.AddFacilityCapability(state, capability);
    }
}

/// <summary>
/// Temporary compatibility projection for construction, shipbuilding, demo objectives and old saves.
/// Adaptive Research remains authoritative; this bridge only grants matching legacy flags and never
/// feeds legacy completion back into the Adaptive graph.
/// </summary>
public static class AdaptiveResearchLegacyCapabilityBridge
{
    private static readonly IReadOnlyDictionary<string, string> LegacyToAdaptiveNode =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["orbital_industry"] = "in_space_assembly",
            ["fusion_propulsion"] = "fusion_propulsion",
            ["deep_space_sensors"] = "deep_space_radar",
            ["exotic_field_theory"] = "field_theory",
            ["warp_field_control"] = "warp_field_control",
            ["prototype_warp_drive"] = "prototype_warp_drive",
        };

    public static void Synchronize(GalaxyState galaxy, AdaptiveResearchCampaignState campaign)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(campaign);
        foreach (var civilization in galaxy.Civilizations.ToArray())
        {
            var adaptive = campaign.GetCivilization(civilization.Id);
            var legacy = galaxy.Technologies.First(value => value.CivilizationId == civilization.Id);
            foreach (var pair in LegacyToAdaptiveNode)
                if (adaptive.HasEstablishedKnowledge(pair.Value))
                    legacy.CompletedTechnologyIds.Add(pair.Key);

            if (adaptive.HasCapability("experimental_interstellar_transit") &&
                civilization.DevelopmentStage == CivilizationDevelopmentStage.PreWarp)
            {
                var index = galaxy.Civilizations.IndexOf(civilization);
                galaxy.Civilizations[index] = civilization with { DevelopmentStage = CivilizationDevelopmentStage.WarpCapable };
            }
        }
    }
}

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

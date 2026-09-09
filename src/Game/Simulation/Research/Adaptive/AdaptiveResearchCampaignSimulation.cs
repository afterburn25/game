using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Construction;
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
    private const string SurfaceInstitutionPrefix = "construction:surface:";

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

        AdaptiveResearchCampaignProgression.SynchronizeDevelopmentStages(galaxy, campaign);
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
            campaign.Runtime.Authority.SetResearchInstitution(
                state,
                PlanetaryResearchNetworkInstitutionId,
                "general_research_laboratory",
                PlanetaryResearchNetworkLabCount,
                PlanetaryResearchNetworkLabCount);
        }
        else if (!networkComplete && networkInstitution is not null)
        {
            campaign.Runtime.Authority.SetResearchInstitution(
                state, PlanetaryResearchNetworkInstitutionId,
                networkInstitution.InstitutionArchetypeId, 0, 0,
                networkInstitution.ContextId);
        }

        SynchronizeSurfaceResearchFacilities(galaxy, civilizationId, campaign, state);

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

    private static void SynchronizeSurfaceResearchFacilities(
        GalaxyState galaxy,
        int civilizationId,
        AdaptiveResearchCampaignState campaign,
        AdaptiveResearchCivilizationState state)
    {
        var desired = new Dictionary<string, (string ArchetypeId, string ContextId)>(StringComparer.Ordinal);
        foreach (var colony in galaxy.Colonies
                     .Where(value => value.CivilizationId == civilizationId)
                     .OrderBy(value => value.Id))
        {
            var output = SurfaceConstruction.GetOutput(colony);
            var researchDistrict = SurfaceConstruction.GetSpecialization(colony) is
                { Id: "science_lab", Active: true };
            foreach (var building in colony.SurfaceBuildings
                         .Where(value => value.IsComplete &&
                                         output.PoweredBuildingIds.Contains(value.Id) &&
                                         SurfaceBuildingCatalog.FunctionalFamily(value.TypeId) == "science_lab")
                         .OrderBy(value => value.Id))
            {
                var advanced = building.TypeId == "advanced_science_lab";
                var archetype = (advanced, researchDistrict) switch
                {
                    (false, false) => "surface_science_laboratory",
                    (false, true) => "surface_science_laboratory_district",
                    (true, false) => "advanced_surface_science_campus",
                    (true, true) => "advanced_surface_science_campus_district",
                };
                desired[$"{SurfaceInstitutionPrefix}{colony.Id}:{building.Id}"] =
                    (archetype, $"colony:{colony.Id}");
            }
        }

        foreach (var institution in state.Expertise.Institutions.Values
                     .Where(value => value.InstitutionInstanceId.StartsWith(
                         SurfaceInstitutionPrefix, StringComparison.Ordinal))
                     .ToArray())
        {
            if (desired.ContainsKey(institution.InstitutionInstanceId)) continue;
            campaign.Runtime.Authority.SetResearchInstitution(
                state, institution.InstitutionInstanceId,
                institution.InstitutionArchetypeId, 0, 0, institution.ContextId);
        }

        foreach (var pair in desired)
        {
            var current = state.Expertise.Institutions.GetValueOrDefault(pair.Key);
            if (current is not null &&
                current.InstitutionArchetypeId == pair.Value.ArchetypeId &&
                current.TotalCount == 1 && current.ActiveCount == 1 &&
                current.ContextId == pair.Value.ContextId)
                continue;
            campaign.Runtime.Authority.SetResearchInstitution(
                state, pair.Key, pair.Value.ArchetypeId, 1, 1, pair.Value.ContextId);
        }
    }
}

/// <summary>
/// Projects the authoritative interstellar-transit capability into the civilization's broad
/// development stage. This stage remains shared gameplay state used outside Research.
/// </summary>
public static class AdaptiveResearchCampaignProgression
{
    public static void SynchronizeDevelopmentStages(
        GalaxyState galaxy,
        AdaptiveResearchCampaignState campaign)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(campaign);
        foreach (var civilization in galaxy.Civilizations.ToArray())
        {
            var adaptive = campaign.GetCivilization(civilization.Id);
            if (adaptive.HasCapability("experimental_interstellar_transit") &&
                civilization.DevelopmentStage == CivilizationDevelopmentStage.PreWarp)
            {
                var index = galaxy.Civilizations.IndexOf(civilization);
                galaxy.Civilizations[index] = civilization with { DevelopmentStage = CivilizationDevelopmentStage.WarpCapable };
            }
        }
    }
}

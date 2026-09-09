using System;
using System.Linq;
using Game.Simulation;
using Game.Simulation.Construction;
using Game.Simulation.Models;
using Game.Simulation.Research;
using Game.Simulation.Research.Adaptive;
using Game.Simulation.Shipbuilding;

namespace Game.Campaign;

public sealed record DemoObjectiveSnapshot(string Objective, string Research, string Construction);

/// <summary>Read-only guidance from the player's existing orders, prerequisites and production.</summary>
public static class DemoObjectiveView
{
    private static readonly string[] ResearchPriority = { "fusion_propulsion", "deep_space_sensors", "orbital_industry", "exotic_field_theory", "warp_field_control", "prototype_warp_drive" };
    private static readonly string[] ConstructionPriority = { "research_network", "industrial_automation", "orbital_launch_complex", "orbital_shipyard", "warp_test_facility" };

    public static DemoObjectiveSnapshot Build(
        GalaxyState galaxy,
        double requestedSpeed,
        AdaptiveResearchCampaignState? adaptiveResearch = null)
    {
        var player = galaxy.PlayerCivilizationId;
        var technology = galaxy.Technologies.Single(t => t.CivilizationId == player);
        var construction = galaxy.ConstructionStates.Single(t => t.CivilizationId == player);
        var economy = galaxy.Economies.Single(t => t.CivilizationId == player);
        var researchChoices = TechnologyRegistry.GetAvailable(technology, construction);
        var constructionSimulation = adaptiveResearch is null
            ? new ConstructionSimulation()
            : new ConstructionSimulation(new AdaptiveResearchConstructionCapabilityView(adaptiveResearch));
        var constructionChoices = constructionSimulation.GetAvailableProjects(galaxy, player);
        var researchNext = ResearchPriority.FirstOrDefault(id => researchChoices.Any(t => t.Id == id));
        var constructionNext = ConstructionPriority.FirstOrDefault(id => constructionChoices.Any(t => t.Id == id));
        var optionalExtraction = constructionChoices.FirstOrDefault(project => project.Id == "asteroid_resource_network");
        var researchText = adaptiveResearch is null
            ? technology.ActiveResearchId is string activeResearch
                ? $"Research: {TechnologyRegistry.Get(activeResearch).Name} · {Eta(TechnologyRegistry.Get(activeResearch).ResearchCost - technology.ActiveResearchProgress - economy.Science, economy.LastSciencePerSecond, requestedSpeed)}"
                : researchNext is not null ? $"Next research: {TechnologyRegistry.Get(researchNext).Name}. Choose it directly in Research."
                : technology.CompletedTechnologyIds.Contains("prototype_warp_drive") ? "Research path complete."
                : "Research waits for the required construction project."
            : AdaptiveResearchGuidance(galaxy, adaptiveResearch);
        var constructionText = construction.ActiveProjectId is string activeConstruction
            ? $"Construction: {ConstructionRegistry.Get(activeConstruction).Name} · {Eta(ConstructionRegistry.Get(activeConstruction).IndustryCost - construction.ActiveProjectProgress - economy.Industry, economy.LastIndustryPerSecond, requestedSpeed)}"
            : constructionNext is not null ? $"Next build: {ConstructionRegistry.Get(constructionNext).Name}. Choose it directly in Industry."
            : optionalExtraction is not null ?
                $"Optional build: {optionalExtraction.Name}. Add {optionalExtraction.IndustryPerDay:0.00} Industry/day for {optionalExtraction.UpkeepCreditsPerDay:0.00} Credits/day upkeep."
            : "Construction prerequisites complete; keep research running.";
        var ownFleets = galaxy.Fleets.Where(f => f.CivilizationId == player && f.IsActive).ToArray();
        var homeSystemId = galaxy.Civilizations.Single(c => c.Id == player).HomeSystemId;
        var hasExperimentalTransit = adaptiveResearch?.GetCivilization(player)
            .HasCapability(ShipbuildingCapabilityIds.ExperimentalInterstellarTransit)
            ?? technology.CompletedTechnologyIds.Contains("prototype_warp_drive");
        var objective = galaxy.Colonies.Any(c => c.CivilizationId == player && c.SystemId != homeSystemId)
            ? "First-colony milestone complete. Save or continue building your civilization."
            : !hasExperimentalTransit
                ? "Objective 1/3: achieve warp flight. Run research and construction together."
                : !ownFleets.Any(f => f.Role == FleetRole.Scout) || !ownFleets.Any(f => f.Role == FleetRole.Science) || !ownFleets.Any(f => f.Role == FleetRole.Colony)
                    ? "Objective 2/3: build a Pathfinder Scout, Science Vessel and Colony Ship in the shipyard."
                    : "Objective 3/3: scout a nearby star, complete its science survey, then settle an available world using the colony mission panel. Survey another star if no suitable world is available.";
        return new DemoObjectiveSnapshot(objective, researchText, constructionText);
    }

    private static string AdaptiveResearchGuidance(
        GalaxyState galaxy,
        AdaptiveResearchCampaignState campaign)
    {
        var civilization = galaxy.Civilizations.Single(value => value.Id == galaxy.PlayerCivilizationId);
        var state = campaign.GetCivilization(civilization.Id);
        var view = campaign.Runtime.Authority.Kernel.BuildView(state, $"species:{civilization.SpeciesId}");
        var active = view.ActiveProjects.FirstOrDefault(value => !value.Paused);
        if (active is not null)
        {
            var node = campaign.Runtime.Authority.Catalog.GetNode(active.NodeId);
            return $"Research: {node.Name} · {active.StageProgress:P0} through {active.Stage.ToString().ToLowerInvariant()} · {active.AssignedEffectiveLabs:0.#} labs.";
        }

        if (state.HasCapability("experimental_interstellar_transit"))
            return "Research path complete.";

        var candidate = view.VisibleNodes
            .Where(value => value.State == ResearchMaturity.Investigable && value.Blockers.Count == 0 && value.MinimumLabs is int minimum &&
                minimum <= view.DirectedProgramCapacity.FreeEffectiveLabs + 0.000001)
            .OrderBy(value => EarlyCampaignResearchPlan.Rank(value.NodeId))
            .ThenBy(value => campaign.Runtime.Authority.Catalog.GetNode(value.NodeId).GraphDepth)
            .ThenBy(value => value.DisplayName, StringComparer.Ordinal)
            .FirstOrDefault();
        return candidate is null
            ? "Research waits for new evidence, facilities or free laboratory capacity."
            : $"Next research: {candidate.DisplayName}. Choose it directly in Research.";
    }

    private static string Eta(double work, double perDay, double speed)
    {
        if (work <= 0) return "ready on the next simulation step";
        if (perDay <= 0) return "estimating after production starts";
        var days = work / perDay;
        return speed <= 0 ? $"{days:0} days remaining · paused" : $"~{days:0} days / {days / speed:0} seconds at current speed";
    }
}

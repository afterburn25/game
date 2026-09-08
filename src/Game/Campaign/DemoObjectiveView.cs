using System;
using System.Linq;
using Game.Simulation;
using Game.Simulation.Construction;
using Game.Simulation.Models;
using Game.Simulation.Research;

namespace Game.Campaign;

public sealed record DemoObjectiveSnapshot(string Objective, string Research, string Construction);

/// <summary>Read-only guidance from the player's existing orders, prerequisites and production.</summary>
public static class DemoObjectiveView
{
    private static readonly string[] ResearchPriority = { "fusion_propulsion", "deep_space_sensors", "orbital_industry", "exotic_field_theory", "warp_field_control", "prototype_warp_drive" };
    private static readonly string[] ConstructionPriority = { "research_network", "industrial_automation", "orbital_launch_complex", "orbital_shipyard", "warp_test_facility" };

    public static DemoObjectiveSnapshot Build(GalaxyState galaxy, double requestedSpeed)
    {
        var player = galaxy.PlayerCivilizationId;
        var technology = galaxy.Technologies.Single(t => t.CivilizationId == player);
        var construction = galaxy.ConstructionStates.Single(t => t.CivilizationId == player);
        var economy = galaxy.Economies.Single(t => t.CivilizationId == player);
        var researchChoices = TechnologyRegistry.GetAvailable(technology, construction);
        var constructionChoices = ConstructionRegistry.GetAvailable(construction, technology);
        var researchNext = ResearchPriority.FirstOrDefault(id => researchChoices.Any(t => t.Id == id));
        var constructionNext = ConstructionPriority.FirstOrDefault(id => constructionChoices.Any(t => t.Id == id));
        var researchText = technology.ActiveResearchId is string activeResearch
            ? $"Research: {TechnologyRegistry.Get(activeResearch).Name} · {Eta(TechnologyRegistry.Get(activeResearch).ResearchCost - technology.ActiveResearchProgress - economy.Science, economy.LastSciencePerSecond, requestedSpeed)}"
            : researchNext is not null ? $"Next research: {TechnologyRegistry.Get(researchNext).Name}. Select it with Next Research, then Start Research."
            : technology.CompletedTechnologyIds.Contains("prototype_warp_drive") ? "Research path complete."
            : "Research waits for the required construction project.";
        var constructionText = construction.ActiveProjectId is string activeConstruction
            ? $"Construction: {ConstructionRegistry.Get(activeConstruction).Name} · {Eta(ConstructionRegistry.Get(activeConstruction).IndustryCost - construction.ActiveProjectProgress - economy.Industry, economy.LastIndustryPerSecond, requestedSpeed)}"
            : constructionNext is not null ? $"Next build: {ConstructionRegistry.Get(constructionNext).Name}. Select it with Next Build, then Start Build."
            : "Construction prerequisites complete; keep research running.";
        var ownFleets = galaxy.Fleets.Where(f => f.CivilizationId == player && f.IsActive).ToArray();
        var objective = galaxy.Colonies.Count(c => c.CivilizationId == player) > 1
            ? "Demo complete: you founded a second colony. Save or keep exploring."
            : !technology.CompletedTechnologyIds.Contains("prototype_warp_drive")
                ? "Objective 1/3: achieve warp flight. Run research and construction together."
                : !ownFleets.Any(f => f.Role == FleetRole.Scout) || !ownFleets.Any(f => f.Role == FleetRole.Science) || !ownFleets.Any(f => f.Role == FleetRole.Colony)
                    ? "Objective 2/3: build a Pathfinder Scout, Science Vessel and Colony Ship in the shipyard."
                    : "Objective 3/3: scout a nearby star, complete its science survey, then settle an available world using the colony mission panel. Survey another star if no suitable world is available.";
        return new DemoObjectiveSnapshot(objective, researchText, constructionText);
    }

    private static string Eta(double work, double perDay, double speed)
    {
        if (work <= 0) return "ready on the next simulation step";
        if (perDay <= 0) return "estimating after production starts";
        var days = work / perDay;
        return speed <= 0 ? $"{days:0} days remaining · paused" : $"~{days:0} days / {days / speed:0} seconds at current speed";
    }
}

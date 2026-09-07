using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Simulation.Research;

public sealed class ResearchSimulation
{
    public IReadOnlyList<ResearchEvent> Advance(GalaxyState galaxy)
    {
        var events = new List<ResearchEvent>();

        foreach (var civilization in galaxy.Civilizations.ToArray())
        {
            if (civilization.DevelopmentStage == CivilizationDevelopmentStage.AncientSpacefaring)
                continue;

            var state = galaxy.Technologies.First(technology => technology.CivilizationId == civilization.Id);
            var economy = galaxy.Economies.First(e => e.CivilizationId == civilization.Id);

            if (state.ActiveResearchId is null && !civilization.IsPlayer)
                state.ActiveResearchId = SelectAiResearch(civilization, state)?.Id;

            if (state.ActiveResearchId is null || economy.Science <= 0.0)
                continue;

            var definition = TechnologyRegistry.Get(state.ActiveResearchId);
            var remaining = Math.Max(0.0, definition.ResearchCost - state.ActiveResearchProgress);
            var spend = Math.Min(remaining, economy.Science);
            economy.Science -= spend;
            state.ActiveResearchProgress += spend;

            if (state.ActiveResearchProgress + 0.0001 < definition.ResearchCost)
                continue;

            state.CompletedTechnologyIds.Add(definition.Id);
            state.ActiveResearchId = null;
            state.ActiveResearchProgress = 0.0;
            events.Add(new ResearchEvent(civilization.Id, definition.Id, $"{civilization.Name} completed {definition.Name}."));

            if (definition.Id == "prototype_warp_drive" && civilization.DevelopmentStage == CivilizationDevelopmentStage.PreWarp)
            {
                var replacement = civilization with { DevelopmentStage = CivilizationDevelopmentStage.WarpCapable };
                ReplaceCivilization(galaxy, replacement);
                new FleetSeeder().EnsureStarterFleets(galaxy, civilization.Id);
                events.Add(new ResearchEvent(civilization.Id, definition.Id, $"{civilization.Name} has become warp-capable."));
            }
        }

        return events;
    }

    public ResearchOrderResult StartResearch(GalaxyState galaxy, int civilizationId, string technologyId)
    {
        var civilization = galaxy.Civilizations.FirstOrDefault(c => c.Id == civilizationId);
        if (civilization is null)
            return new ResearchOrderResult(false, "Unknown civilization.");
        if (civilization.DevelopmentStage == CivilizationDevelopmentStage.AncientSpacefaring)
            return new ResearchOrderResult(false, "This civilization has already mastered interstellar flight.");

        var state = galaxy.Technologies.First(t => t.CivilizationId == civilizationId);
        if (state.ActiveResearchId is not null)
            return new ResearchOrderResult(false, "Research is already in progress.");

        var definition = TechnologyRegistry.GetAvailable(state).FirstOrDefault(t => t.Id == technologyId);
        if (definition is null)
            return new ResearchOrderResult(false, "That technology is not currently available.");

        state.ActiveResearchId = definition.Id;
        state.ActiveResearchProgress = 0.0;
        return new ResearchOrderResult(true, $"Research started: {definition.Name}.");
    }

    private static TechnologyDefinition? SelectAiResearch(CivilizationState civilization, TechnologyState state) =>
        TechnologyRegistry.GetAvailable(state)
            .OrderByDescending(definition => Score(definition, civilization))
            .ThenBy(definition => definition.ResearchCost)
            .FirstOrDefault();

    private static double Score(TechnologyDefinition definition, CivilizationState civilization)
    {
        var traits = civilization.Traits;
        return definition.Category switch
        {
            TechnologyCategory.Industry => 1.0 + traits.Greed * 0.50 + traits.Territoriality * 0.20,
            TechnologyCategory.Propulsion => 1.0 + traits.Aggression * 0.35 + traits.RiskTolerance * 0.20,
            TechnologyCategory.Sensors => 1.0 + traits.ScientificCuriosity * 0.55 + (1.0 - traits.RiskTolerance) * 0.15,
            TechnologyCategory.Physics => 1.0 + traits.ScientificCuriosity * 0.80,
            TechnologyCategory.Ftl => 1.2 + traits.ScientificCuriosity * 0.40 + traits.Aggression * 0.20 + traits.Territoriality * 0.20,
            _ => 1.0,
        };
    }

    private static void ReplaceCivilization(GalaxyState galaxy, CivilizationState replacement)
    {
        for (var i = 0; i < galaxy.Civilizations.Count; i++)
        {
            if (galaxy.Civilizations[i].Id != replacement.Id) continue;
            galaxy.Civilizations[i] = replacement;
            return;
        }
    }
}

public sealed record ResearchEvent(int CivilizationId, string TechnologyId, string Message);
public sealed record ResearchOrderResult(bool Accepted, string Message);

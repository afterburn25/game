using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Construction;

public sealed class ConstructionSimulation
{
    public IReadOnlyList<ConstructionEvent> Advance(GalaxyState galaxy)
    {
        var events = new List<ConstructionEvent>();

        foreach (var civilization in galaxy.Civilizations)
        {
            if (civilization.IsSeededAncient)
                continue;

            var state = galaxy.ConstructionStates.First(c => c.CivilizationId == civilization.Id);
            var technology = galaxy.Technologies.First(t => t.CivilizationId == civilization.Id);
            var economy = galaxy.Economies.First(e => e.CivilizationId == civilization.Id);

            if (state.ActiveProjectId is null && !civilization.IsPlayer)
                state.ActiveProjectId = SelectAiProject(civilization, state, technology)?.Id;

            if (state.ActiveProjectId is null || economy.Industry <= 0.0)
                continue;

            var project = ConstructionRegistry.Get(state.ActiveProjectId);
            var remaining = Math.Max(0.0, project.IndustryCost - state.ActiveProjectProgress);
            var spend = Math.Min(remaining, economy.Industry);
            economy.Industry -= spend;
            state.ActiveProjectProgress += spend;

            if (state.ActiveProjectProgress + 0.0001 < project.IndustryCost)
                continue;

            state.CompletedProjectIds.Add(project.Id);
            state.ActiveProjectId = null;
            state.ActiveProjectProgress = 0.0;
            events.Add(new ConstructionEvent(civilization.Id, project.Id, $"{civilization.Name} completed {project.Name}."));
        }

        return events;
    }

    public ConstructionOrderResult StartProject(GalaxyState galaxy, int civilizationId, string projectId)
    {
        var civilization = galaxy.Civilizations.FirstOrDefault(c => c.Id == civilizationId);
        if (civilization is null)
            return new ConstructionOrderResult(false, "Unknown civilization.");

        var state = galaxy.ConstructionStates.First(c => c.CivilizationId == civilizationId);
        if (state.ActiveProjectId is not null)
            return new ConstructionOrderResult(false, "A construction project is already in progress.");

        var technology = galaxy.Technologies.First(t => t.CivilizationId == civilizationId);
        var project = ConstructionRegistry.GetAvailable(state, technology).FirstOrDefault(p => p.Id == projectId);
        if (project is null)
            return new ConstructionOrderResult(false, "That construction project is not currently available.");

        state.ActiveProjectId = project.Id;
        state.ActiveProjectProgress = 0.0;
        return new ConstructionOrderResult(true, $"Construction started: {project.Name}.");
    }

    private static ConstructionProjectDefinition? SelectAiProject(
        CivilizationState civilization,
        ConstructionState state,
        Research.TechnologyState technology)
    {
        return ConstructionRegistry.GetAvailable(state, technology)
            .OrderByDescending(project => Score(project, civilization))
            .ThenBy(project => project.IndustryCost)
            .FirstOrDefault();
    }

    private static double Score(ConstructionProjectDefinition project, CivilizationState civilization)
    {
        var traits = civilization.Traits;
        return project.Category switch
        {
            ConstructionCategory.Science => 1.0 + traits.ScientificCuriosity * 0.90,
            ConstructionCategory.Industry => 1.0 + traits.Greed * 0.55 + traits.Territoriality * 0.20,
            ConstructionCategory.Orbital => 1.15 + traits.Territoriality * 0.30 + traits.ScientificCuriosity * 0.20,
            ConstructionCategory.Ftl => 1.30 + traits.ScientificCuriosity * 0.40 + traits.Aggression * 0.20,
            _ => 1.0,
        };
    }
}

public sealed record ConstructionEvent(int CivilizationId, string ProjectId, string Message);
public sealed record ConstructionOrderResult(bool Accepted, string Message);

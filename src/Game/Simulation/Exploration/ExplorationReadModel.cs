using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Simulation.Exploration;

/// <summary>
/// Builds observer-local exploration state for presentation and strategic consumers.
/// Detailed authoritative system facts are absent until the observing civilization has
/// legitimately completed a science survey.
/// </summary>
public sealed class ExplorationReadModel
{
    public CivilizationExplorationView Build(GalaxyState galaxy, int civilizationId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (!galaxy.Civilizations.Any(civilization => civilization.Id == civilizationId))
            throw new InvalidOperationException($"Unknown civilization {civilizationId}.");

        var systemsById = galaxy.Systems.ToDictionary(system => system.Id);
        var knownSystems = galaxy.Knowledge.GetSystemSurveyKnowledge(civilizationId)
            .Where(knowledge => knowledge.Level != SystemSurveyLevel.Unknown)
            .OrderBy(knowledge => knowledge.SystemId)
            .Select(knowledge => BuildSystemView(systemsById[knowledge.SystemId], knowledge))
            .ToArray();

        var missions = galaxy.Fleets
            .Where(fleet => fleet.IsActive && fleet.CivilizationId == civilizationId && fleet.Role is FleetRole.Scout or FleetRole.Science or FleetRole.Colony)
            .OrderBy(fleet => fleet.Id)
            .Select(fleet => new ExplorationMissionView(
                fleet.Id,
                fleet.Name,
                fleet.Role,
                fleet.CurrentSystemId,
                fleet.DestinationSystemId,
                fleet.Role == FleetRole.Colony ? fleet.EmbarkedPopulationMillions : 0.0))
            .ToArray();

        return new CivilizationExplorationView(civilizationId, knownSystems, missions);
    }

    private static KnownSystemExplorationView BuildSystemView(
        StarSystemState system,
        SystemSurveyKnowledgeView knowledge)
    {
        var detailed = knowledge.Level == SystemSurveyLevel.FullySurveyed;
        return new KnownSystemExplorationView(
            system.Id,
            system.Name,
            knowledge.Level,
            knowledge.Progress,
            detailed ? system.Archetype : null,
            detailed ? system.HasHabitableWorld : null,
            detailed ? system.HasAnomaly : null,
            detailed ? system.HasRareResource : null,
            detailed ? system.HasPreWarpCivilization : null);
    }
}

public sealed record CivilizationExplorationView(
    int CivilizationId,
    IReadOnlyList<KnownSystemExplorationView> KnownSystems,
    IReadOnlyList<ExplorationMissionView> ActiveMissions);

public sealed record KnownSystemExplorationView(
    int SystemId,
    string CatalogName,
    SystemSurveyLevel SurveyLevel,
    double SurveyProgress,
    StarArchetype? Archetype,
    bool? HasHabitableWorld,
    bool? HasAnomaly,
    bool? HasRareResource,
    bool? HasPreWarpCivilization)
{
    public bool HasDetailedSurvey => SurveyLevel == SystemSurveyLevel.FullySurveyed;
}

public sealed record ExplorationMissionView(
    int FleetId,
    string FleetName,
    FleetRole Role,
    int? CurrentSystemId,
    int? DestinationSystemId,
    double EmbarkedPopulationMillions);

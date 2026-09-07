using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Simulation.Exploration;

/// <summary>
/// Builds observer-local exploration state for presentation and strategic consumers.
/// Detection exposes only the star target. Scout reconnaissance can expose a basic orbital
/// catalog. Precise world environment/resource/native facts remain absent until the observing
/// civilization has legitimately completed a detailed science survey.
/// </summary>
public sealed class ExplorationReadModel
{
    public CivilizationExplorationView Build(GalaxyState galaxy, int civilizationId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (!galaxy.Civilizations.Any(civilization => civilization.Id == civilizationId))
            throw new InvalidOperationException($"Unknown civilization {civilizationId}.");

        var systemsById = galaxy.Systems.ToDictionary(system => system.Id);
        var bodiesBySystem = galaxy.PlanetaryBodies
            .GroupBy(body => body.SystemId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<PlanetaryBodyState>)group.OrderBy(body => body.Id).ToArray());

        var knownSystems = galaxy.Knowledge.GetSystemSurveyKnowledge(civilizationId)
            .Where(knowledge => knowledge.Level != SystemSurveyLevel.Unknown)
            .OrderBy(knowledge => knowledge.SystemId)
            .Select(knowledge => BuildSystemView(
                systemsById[knowledge.SystemId],
                bodiesBySystem.TryGetValue(knowledge.SystemId, out var bodies) ? bodies : Array.Empty<PlanetaryBodyState>(),
                knowledge))
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
                ResolveCompatibilityMissionBody(galaxy, fleet),
                fleet.Role == FleetRole.Colony ? fleet.EmbarkedPopulationMillions : 0.0))
            .ToArray();

        return new CivilizationExplorationView(civilizationId, knownSystems, missions);
    }

    private static KnownSystemExplorationView BuildSystemView(
        StarSystemState system,
        IReadOnlyList<PlanetaryBodyState> bodies,
        SystemSurveyKnowledgeView knowledge)
    {
        var reconnaissance = knowledge.Level >= SystemSurveyLevel.PartiallySurveyed;
        var detailed = knowledge.Level == SystemSurveyLevel.FullySurveyed;
        var visibleBodies = reconnaissance
            ? bodies.Select(body => BuildBodyView(body, detailed)).ToArray()
            : Array.Empty<PlanetaryBodyExplorationView>();

        return new KnownSystemExplorationView(
            system.Id,
            system.Name,
            knowledge.Level,
            knowledge.Progress,
            detailed ? system.Archetype : null,
            detailed ? system.HasHabitableWorld : null,
            detailed ? system.HasAnomaly : null,
            detailed ? system.HasRareResource : null,
            detailed ? system.HasPreWarpCivilization : null,
            visibleBodies);
    }

    private static PlanetaryBodyExplorationView BuildBodyView(PlanetaryBodyState body, bool detailed)
    {
        // A rapid scout pass can establish the large-scale orbital catalog and approximate
        // radius. Mass/gravity and environmental chemistry/hazards require a detailed survey.
        return new PlanetaryBodyExplorationView(
            body.Id,
            body.ParentBodyId,
            body.OrbitIndex,
            body.Name,
            body.Kind,
            body.RadiusEarth,
            detailed ? body.MassEarth : null,
            detailed ? body.Environment.GravityG : null,
            detailed ? body.Environment.TemperatureKelvin : null,
            detailed ? body.Environment.PressureKPa : null,
            detailed ? body.Environment.Atmosphere : null,
            detailed ? body.Environment.AvailableSolvent : null,
            detailed ? body.Environment.RadiationHazard : null,
            detailed ? body.Environment.IsImmersedEnvironment : null,
            detailed ? body.Environment.HasSolidSurface : null,
            detailed ? body.HasRareResource : null,
            detailed ? body.HasAnomaly : null,
            detailed ? body.HasPreWarpCivilization : null);
    }

    private static int? ResolveCompatibilityMissionBody(GalaxyState galaxy, FleetState fleet)
    {
        if (fleet.Role != FleetRole.Colony || fleet.DestinationSystemId is not int systemId)
            return null;

        return galaxy.PlanetaryBodies
            .Where(body => body.SystemId == systemId)
            .OrderBy(body => body.Id)
            .FirstOrDefault(body => body.LegacyColonizationCandidate && body.Environment.HasSolidSurface)
            ?.Id;
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
    bool? HasPreWarpCivilization,
    IReadOnlyList<PlanetaryBodyExplorationView> PlanetaryBodies)
{
    public bool HasReconnaissanceCatalog => SurveyLevel >= SystemSurveyLevel.PartiallySurveyed;
    public bool HasDetailedSurvey => SurveyLevel == SystemSurveyLevel.FullySurveyed;
}

public sealed record PlanetaryBodyExplorationView(
    int BodyId,
    int? ParentBodyId,
    int OrbitIndex,
    string Name,
    PlanetaryBodyKind Kind,
    double RadiusEarth,
    double? MassEarth,
    double? GravityG,
    double? TemperatureKelvin,
    double? PressureKPa,
    PlanetaryAtmosphereRegime? Atmosphere,
    PlanetarySolventRegime? AvailableSolvent,
    double? RadiationHazard,
    bool? IsImmersedEnvironment,
    bool? HasSolidSurface,
    bool? HasRareResource,
    bool? HasAnomaly,
    bool? HasPreWarpCivilization)
{
    public bool HasDetailedEnvironment => GravityG is not null;
}

public sealed record ExplorationMissionView(
    int FleetId,
    string FleetName,
    FleetRole Role,
    int? CurrentSystemId,
    int? DestinationSystemId,
    int? TargetPlanetaryBodyId,
    double EmbarkedPopulationMillions);
using Game.Simulation.Colonization;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class OperationalReachValidation
{
    public static void ValidateSharedMissionReachGate()
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x5245_4143_485F_4741L,
            new GalaxyGenerationSettings
            {
                SystemCount = 48,
                PreWarpCivilizationCount = 5,
                AncientCivilizationCount = 1,
                Radius = 520.0f,
            });

        var player = galaxy.Civilizations.First(civilization => civilization.Id == galaxy.PlayerCivilizationId);
        var home = galaxy.Systems.First(system => system.Id == player.HomeSystemId);
        var target = galaxy.Systems.FirstOrDefault(system =>
            system.Id != player.HomeSystemId &&
            system.HasHabitableWorld &&
            !system.HasPreWarpCivilization &&
            !galaxy.Colonies.Any(colony => colony.SystemId == system.Id))
            ?? throw new InvalidOperationException("validation galaxy did not contain an unoccupied habitable target");

        var reach = new RejectAllOperationalReachView();
        var exploration = new ExplorationSimulation(reach);
        var nextFleetId = galaxy.Fleets.Count == 0 ? 3000 : galaxy.Fleets.Max(fleet => fleet.Id) + 3000;

        var scout = new FleetState
        {
            Id = nextFleetId,
            CivilizationId = player.Id,
            Name = "Reach Validation Scout",
            Role = FleetRole.Scout,
            Position = home.Position,
            CurrentSystemId = home.Id,
            StrategicSpeed = 22.0,
            SensorRange = 135.0f,
            IsActive = true,
        };
        galaxy.Fleets.Add(scout);

        var scoutAccepted = exploration.IssueMoveOrder(galaxy, scout.Id, target.Id);
        Require(!scoutAccepted, "scout order ignored the operational reach rejection");
        Require(scout.DestinationSystemId is null, "rejected scout order still changed fleet destination");
        Require(reach.MissionKinds.Contains(InterstellarMissionKind.ScoutReconnaissance), "scout order did not query the shared reach contract");

        var science = new FleetState
        {
            Id = nextFleetId + 1,
            CivilizationId = player.Id,
            Name = "Reach Validation Science",
            Role = FleetRole.Science,
            Position = home.Position,
            CurrentSystemId = home.Id,
            StrategicSpeed = 18.0,
            SensorRange = 185.0f,
            IsActive = true,
        };
        galaxy.Fleets.Add(science);

        var scienceAccepted = exploration.IssueMoveOrder(galaxy, science.Id, target.Id);
        Require(!scienceAccepted, "science-survey order ignored the operational reach rejection");
        Require(science.DestinationSystemId is null, "rejected science order still changed fleet destination");
        Require(reach.MissionKinds.Contains(InterstellarMissionKind.ScienceSurvey), "science order did not query the shared reach contract");

        galaxy.Knowledge.MarkSystemFullySurveyed(player.Id, target.Id);
        var colonyFleet = new FleetState
        {
            Id = nextFleetId + 2,
            CivilizationId = player.Id,
            Name = "Reach Validation Colony Ship",
            Role = FleetRole.Colony,
            Position = home.Position,
            CurrentSystemId = home.Id,
            StrategicSpeed = 13.5,
            SensorRange = 80.0f,
            IsActive = true,
            EmbarkedPopulationMillions = 250.0,
        };
        galaxy.Fleets.Add(colonyFleet);

        var colonization = new ColonizationSimulation(reach);
        var colonyOrder = colonization.IssuePlayerColonyOrder(galaxy, player.Id, target.Id);
        Require(!colonyOrder.Accepted, "colony order ignored the operational reach rejection");
        Require(colonyFleet.DestinationSystemId is null, "rejected colony order still changed fleet destination");
        Require(colonyOrder.Message.Contains("outside validated support reach", StringComparison.OrdinalIgnoreCase), "colony rejection did not preserve the authoritative reach reason");
        Require(reach.MissionKinds.Contains(InterstellarMissionKind.Colony), "colony order did not query the shared reach contract");

        var ai = galaxy.Civilizations.First(civilization => !civilization.IsPlayer && !civilization.IsSeededAncient);
        var aiHome = galaxy.Systems.First(system => system.Id == ai.HomeSystemId);
        var aiScout = new FleetState
        {
            Id = nextFleetId + 3,
            CivilizationId = ai.Id,
            Name = "AI Reach Validation Scout",
            Role = FleetRole.Scout,
            Position = aiHome.Position,
            CurrentSystemId = aiHome.Id,
            StrategicSpeed = 22.0,
            SensorRange = 135.0f,
            IsActive = true,
        };
        galaxy.Fleets.Add(aiScout);
        exploration.Advance(galaxy, 1.0);
        Require(aiScout.DestinationSystemId is null, "AI exploration bypassed the same operational reach gate used by player orders");

        ValidateFogSafeReadModel(galaxy, player.Id, aiScout.Id);
    }

    private static void ValidateFogSafeReadModel(GalaxyState galaxy, int civilizationId, int foreignFleetId)
    {
        var target = galaxy.Systems.FirstOrDefault(system =>
            galaxy.Knowledge.GetSystemSurveyLevel(civilizationId, system.Id) != SystemSurveyLevel.FullySurveyed)
            ?? throw new InvalidOperationException("validation galaxy did not contain an incompletely surveyed system");
        var readModel = new ExplorationReadModel();

        galaxy.Knowledge.RevealSystem(civilizationId, target.Id);
        var detected = readModel.Build(galaxy, civilizationId).KnownSystems.Single(system => system.SystemId == target.Id);
        Require(detected.SurveyLevel == SystemSurveyLevel.Detected, "read model did not expose detection state");
        Require(!detected.HasDetailedSurvey, "detected system was marked as detailed survey knowledge");
        Require(
            detected.Archetype is null &&
            detected.HasHabitableWorld is null &&
            detected.HasAnomaly is null &&
            detected.HasRareResource is null &&
            detected.HasPreWarpCivilization is null,
            "fog-safe read model leaked authoritative system facts at detection level");

        galaxy.Knowledge.RecordReconnaissance(civilizationId, target.Id);
        var partial = readModel.Build(galaxy, civilizationId).KnownSystems.Single(system => system.SystemId == target.Id);
        Require(partial.SurveyLevel == SystemSurveyLevel.PartiallySurveyed, "read model did not expose reconnaissance state");
        Require(!partial.HasDetailedSurvey && partial.HasHabitableWorld is null, "partial survey leaked colonization-grade facts");

        galaxy.Knowledge.MarkSystemFullySurveyed(civilizationId, target.Id);
        var fullView = readModel.Build(galaxy, civilizationId);
        var fullySurveyed = fullView.KnownSystems.Single(system => system.SystemId == target.Id);
        Require(fullySurveyed.HasDetailedSurvey, "completed survey was not marked detailed in the read model");
        Require(fullySurveyed.Archetype == target.Archetype, "full survey read model did not expose the known star archetype");
        Require(fullySurveyed.HasHabitableWorld == target.HasHabitableWorld, "full survey read model did not expose known habitability");
        Require(fullySurveyed.HasAnomaly == target.HasAnomaly, "full survey read model did not expose known anomaly state");
        Require(fullySurveyed.HasRareResource == target.HasRareResource, "full survey read model did not expose known resource state");
        Require(fullySurveyed.HasPreWarpCivilization == target.HasPreWarpCivilization, "full survey read model did not expose known native-civilization state");
        Require(fullView.ActiveMissions.All(mission => mission.FleetId != foreignFleetId), "observer-local exploration view leaked a foreign fleet mission");
    }

    private sealed class RejectAllOperationalReachView : IInterstellarOperationalReachView
    {
        public HashSet<InterstellarMissionKind> MissionKinds { get; } = new();

        public MissionReachAssessment Assess(
            GalaxyState galaxy,
            int civilizationId,
            FleetState fleet,
            int targetSystemId,
            InterstellarMissionKind missionKind)
        {
            MissionKinds.Add(missionKind);
            return MissionReachAssessment.Unsupported("Target is outside validated support reach for this mission.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

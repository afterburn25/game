using Game.Simulation.Colonization;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class ExplorationColonizationValidation
{
    public static void ValidateScoutAndScienceSurveyRoles()
    {
        var galaxy = CreateValidationGalaxy();
        var player = galaxy.Civilizations.First(civilization => civilization.Id == galaxy.PlayerCivilizationId);
        var observer = galaxy.Civilizations.First(civilization => civilization.Id != player.Id);
        var target = galaxy.Systems.FirstOrDefault(system =>
            system.Id != player.HomeSystemId &&
            !galaxy.Knowledge.IsSystemFullySurveyed(player.Id, system.Id) &&
            !galaxy.Knowledge.IsSystemKnown(observer.Id, system.Id))
            ?? throw new InvalidOperationException("validation galaxy did not contain a suitable survey target");

        galaxy.Knowledge.RevealSystem(player.Id, target.Id);
        Require(
            galaxy.Knowledge.GetSystemSurveyLevel(player.Id, target.Id) == SystemSurveyLevel.Detected,
            "sensor detection was incorrectly treated as a completed survey");
        Require(
            galaxy.Knowledge.GetSystemSurveyProgress(player.Id, target.Id) == 0.0,
            "newly detected system started with invented survey progress");

        var nextFleetId = galaxy.Fleets.Count == 0 ? 1000 : galaxy.Fleets.Max(fleet => fleet.Id) + 1000;
        var scout = new FleetState
        {
            Id = nextFleetId,
            CivilizationId = player.Id,
            Name = "Validation Scout",
            Role = FleetRole.Scout,
            Position = target.Position,
            CurrentSystemId = target.Id,
            StrategicSpeed = 24.0,
            SensorRange = 140.0f,
            IsActive = true,
        };
        galaxy.Fleets.Add(scout);

        var exploration = new ExplorationSimulation();
        var scoutEvents = exploration.Advance(galaxy, 1.0);

        Require(
            galaxy.Knowledge.GetSystemSurveyLevel(player.Id, target.Id) == SystemSurveyLevel.PartiallySurveyed,
            "scout did not create first-pass reconnaissance knowledge");
        var scoutProgress = galaxy.Knowledge.GetSystemSurveyProgress(player.Id, target.Id);
        Require(
            scoutProgress >= ExplorationSimulation.ScoutReconnaissanceProgress && scoutProgress < 1.0,
            "scout reconnaissance incorrectly completed the detailed survey");
        Require(
            scoutEvents.Any(evt => evt.Type == ExplorationEventType.SystemReconnoitered && evt.SystemId == target.Id),
            "scout reconnaissance did not emit a reconnaissance event");

        scout.IsActive = false;
        var science = new FleetState
        {
            Id = nextFleetId + 1,
            CivilizationId = player.Id,
            Name = "Validation Science Vessel",
            Role = FleetRole.Science,
            Position = target.Position,
            CurrentSystemId = target.Id,
            StrategicSpeed = 18.0,
            SensorRange = 185.0f,
            IsActive = true,
        };
        galaxy.Fleets.Add(science);

        var scienceEvents = exploration.Advance(galaxy, 20.0);
        Require(
            galaxy.Knowledge.IsSystemFullySurveyed(player.Id, target.Id),
            "science vessel did not complete a detailed survey after sufficient deterministic survey time");
        Require(
            Math.Abs(galaxy.Knowledge.GetSystemSurveyProgress(player.Id, target.Id) - 1.0) < 0.0000001,
            "completed science survey did not clamp progress to 100 percent");
        Require(
            scienceEvents.Any(evt => evt.Type == ExplorationEventType.SystemSurveyed && evt.SystemId == target.Id),
            "science survey completion event was not emitted");

        Require(
            galaxy.Knowledge.GetSystemSurveyLevel(observer.Id, target.Id) == SystemSurveyLevel.Unknown,
            "one civilization's survey leaked into another civilization's knowledge");
    }

    public static void ValidateColonizationRequiresFullSurvey()
    {
        var galaxy = CreateValidationGalaxy();
        var player = galaxy.Civilizations.First(civilization => civilization.Id == galaxy.PlayerCivilizationId);
        var home = galaxy.Systems.First(system => system.Id == player.HomeSystemId);
        var target = galaxy.Systems.FirstOrDefault(system =>
            system.Id != player.HomeSystemId &&
            system.HasHabitableWorld &&
            !system.HasPreWarpCivilization &&
            !galaxy.Colonies.Any(colony => colony.SystemId == system.Id))
            ?? throw new InvalidOperationException("validation galaxy did not contain an unoccupied habitable target");

        galaxy.Knowledge.RevealSystem(player.Id, target.Id);
        var colonyFleet = new FleetState
        {
            Id = galaxy.Fleets.Count == 0 ? 2000 : galaxy.Fleets.Max(fleet => fleet.Id) + 2000,
            CivilizationId = player.Id,
            Name = "Validation Colony Ship",
            Role = FleetRole.Colony,
            Position = home.Position,
            CurrentSystemId = home.Id,
            StrategicSpeed = 13.5,
            SensorRange = 80.0f,
            IsActive = true,
        };
        galaxy.Fleets.Add(colonyFleet);

        var colonization = new ColonizationSimulation();
        var rejected = colonization.IssuePlayerColonyOrder(galaxy, player.Id, target.Id);
        Require(!rejected.Accepted, "colonization accepted a merely detected target");
        Require(colonyFleet.DestinationSystemId is null, "rejected colonization order still changed fleet destination");

        galaxy.Knowledge.RecordReconnaissance(player.Id, target.Id);
        var stillRejected = colonization.IssuePlayerColonyOrder(galaxy, player.Id, target.Id);
        Require(!stillRejected.Accepted, "colonization accepted scout reconnaissance as a full science survey");

        galaxy.Knowledge.MarkSystemFullySurveyed(player.Id, target.Id);
        var accepted = colonization.IssuePlayerColonyOrder(galaxy, player.Id, target.Id);
        Require(accepted.Accepted, "colonization rejected a valid fully surveyed target with an available colony ship");
        Require(colonyFleet.DestinationSystemId == target.Id, "accepted colony order did not assign the surveyed target");
    }

    private static GalaxyState CreateValidationGalaxy() =>
        new GalaxyGenerator().Generate(
            0x4558_504C_4F52_45L,
            new GalaxyGenerationSettings
            {
                SystemCount = 48,
                PreWarpCivilizationCount = 5,
                AncientCivilizationCount = 1,
                Radius = 520.0f,
            });

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

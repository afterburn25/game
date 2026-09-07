using Game.Persistence;
using Game.Simulation.Colonization;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class PlanetaryBodyValidation
{
    public static void ValidateDeterministicPhysicalCatalogAndSaveReconstruction()
    {
        const long seed = 0x504C_414E_4554_5359L;
        var settings = new GalaxyGenerationSettings
        {
            SystemCount = 52,
            PreWarpCivilizationCount = 5,
            AncientCivilizationCount = 1,
            Radius = 560.0f,
        };

        var generator = new GalaxyGenerator();
        var first = generator.Generate(seed, settings);
        var second = generator.Generate(seed, settings);

        Require(first.PlanetaryBodies.Count > first.Systems.Count, "planetary catalog did not produce a useful bounded body population");
        Require(first.PlanetaryBodies.SequenceEqual(second.PlanetaryBodies), "identical campaign seed/system catalog produced different planet/moon state");
        Require(first.PlanetaryBodies.Select(body => body.Id).Distinct().Count() == first.PlanetaryBodies.Count, "planetary body IDs are not globally unique");

        var systemsById = first.Systems.ToDictionary(system => system.Id);
        var bodiesById = first.PlanetaryBodies.ToDictionary(body => body.Id);
        foreach (var body in first.PlanetaryBodies)
        {
            body.Validated();
            Require(systemsById.ContainsKey(body.SystemId), $"body {body.Id} references an unknown star system");
            Require(body.Id / 1000 == body.SystemId, $"body {body.Id} does not preserve its stable system ID namespace");

            if (body.Kind != PlanetaryBodyKind.Moon)
                continue;

            Require(body.ParentBodyId is int parentId, $"moon {body.Id} has no parent body");
            Require(bodiesById.TryGetValue(parentId, out var parent), $"moon {body.Id} parent is missing");
            Require(parent!.Kind == PlanetaryBodyKind.Planet, $"moon {body.Id} parent is not a planet");
            Require(parent.SystemId == body.SystemId, $"moon {body.Id} parent is in a different star system");
        }

        foreach (var system in first.Systems)
        {
            var candidates = first.PlanetaryBodies
                .Where(body => body.SystemId == system.Id && body.LegacyColonizationCandidate)
                .ToArray();
            Require(
                candidates.Length == (system.HasHabitableWorld ? 1 : 0),
                $"system {system.Id} did not preserve exactly the expected legacy compatibility candidate count");
            if (candidates.Length == 1)
            {
                Require(candidates[0].Kind == PlanetaryBodyKind.Planet, "compatibility colony candidate was not a planet");
                Require(candidates[0].Environment.HasSolidSurface, "compatibility colony candidate had no solid surface");
            }
        }

        WithTemporaryDirectory(directory =>
        {
            var path = Path.Combine(directory, "planetary-catalog-roundtrip.json");
            var service = new CampaignSaveService();
            service.Save(path, first, 288.0);

            // Physical worlds are reconstructible from the existing v7 Seed + Systems state.
            // Do not add a duplicate world catalog to every save while the shared v8 boundary
            // is owned by Species.
            var json = File.ReadAllText(path);
            Require(!json.Contains("PlanetaryBodies", StringComparison.Ordinal), "v7 save redundantly serialized reconstructible planetary catalog state");

            var loaded = service.Load(path);
            Require(
                first.PlanetaryBodies.SequenceEqual(loaded.Galaxy.PlanetaryBodies),
                "save/load did not reconstruct the exact deterministic planet/moon catalog");
        });
    }

    public static void ValidateSurveyVisibilityAndBodyLevelColonization()
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x574F_524C_4453_5552L,
            new GalaxyGenerationSettings
            {
                SystemCount = 48,
                PreWarpCivilizationCount = 5,
                AncientCivilizationCount = 1,
                Radius = 520.0f,
            });

        var player = galaxy.Civilizations.First(civilization => civilization.Id == galaxy.PlayerCivilizationId);
        var home = galaxy.Systems.First(system => system.Id == player.HomeSystemId);
        var surveyTarget = galaxy.Systems.First(system =>
            system.Id != player.HomeSystemId &&
            !galaxy.Knowledge.IsSystemFullySurveyed(player.Id, system.Id));

        galaxy.Knowledge.RevealSystem(player.Id, surveyTarget.Id);
        var readModel = new ExplorationReadModel();
        var detected = readModel.Build(galaxy, player.Id).KnownSystems.First(system => system.SystemId == surveyTarget.Id);
        Require(detected.SurveyLevel == SystemSurveyLevel.Detected, "validation system was not detection-level before reconnaissance");
        Require(detected.PlanetaryBodies.Count == 0, "mere star detection leaked the planetary catalog");

        galaxy.Knowledge.RecordReconnaissance(player.Id, surveyTarget.Id);
        var partial = readModel.Build(galaxy, player.Id).KnownSystems.First(system => system.SystemId == surveyTarget.Id);
        var authoritativeBodies = galaxy.PlanetaryBodies.Where(body => body.SystemId == surveyTarget.Id).OrderBy(body => body.Id).ToArray();
        Require(partial.PlanetaryBodies.Count == authoritativeBodies.Length, "scout reconnaissance did not reveal the basic orbital catalog");
        Require(partial.PlanetaryBodies.All(body => body.RadiusEarth > 0.0), "reconnaissance body catalog did not expose rough body size");
        Require(partial.PlanetaryBodies.All(body => body.MassEarth is null && body.GravityG is null && body.Atmosphere is null && body.HasRareResource is null), "scout reconnaissance leaked detailed physical/resource facts");

        galaxy.Knowledge.MarkSystemFullySurveyed(player.Id, surveyTarget.Id);
        var detailed = readModel.Build(galaxy, player.Id).KnownSystems.First(system => system.SystemId == surveyTarget.Id);
        Require(detailed.PlanetaryBodies.Count == authoritativeBodies.Length, "full survey changed orbital catalog membership");
        foreach (var bodyView in detailed.PlanetaryBodies)
        {
            var authoritative = authoritativeBodies.First(body => body.Id == bodyView.BodyId);
            Require(bodyView.HasDetailedEnvironment, $"full survey did not expose environment for body {bodyView.BodyId}");
            Require(Math.Abs(bodyView.GravityG!.Value - authoritative.Environment.GravityG) < 0.0000001, "body gravity view diverged from authoritative physical state");
            Require(Math.Abs(bodyView.TemperatureKelvin!.Value - authoritative.Environment.TemperatureKelvin) < 0.0000001, "body temperature view diverged from authoritative physical state");
            Require(Math.Abs(bodyView.PressureKPa!.Value - authoritative.Environment.PressureKPa) < 0.0000001, "body pressure view diverged from authoritative physical state");
            Require(bodyView.Atmosphere == authoritative.Environment.Atmosphere, "body atmosphere view diverged from authoritative physical state");
            Require(bodyView.AvailableSolvent == authoritative.Environment.AvailableSolvent, "body solvent view diverged from authoritative physical state");
        }

        var colonySystem = galaxy.Systems.First(system =>
            system.Id != player.HomeSystemId &&
            system.HasHabitableWorld &&
            !system.HasPreWarpCivilization &&
            !galaxy.Colonies.Any(colony => colony.SystemId == system.Id));
        galaxy.Knowledge.MarkSystemFullySurveyed(player.Id, colonySystem.Id);
        var compatibilityBody = galaxy.PlanetaryBodies.Single(body =>
            body.SystemId == colonySystem.Id && body.LegacyColonizationCandidate);
        var rejectedBody = galaxy.PlanetaryBodies.First(body =>
            body.SystemId == colonySystem.Id && !body.LegacyColonizationCandidate);

        var fleet = new FleetState
        {
            Id = galaxy.Fleets.Count == 0 ? 6000 : galaxy.Fleets.Max(candidate => candidate.Id) + 6000,
            CivilizationId = player.Id,
            Name = "Planetary Target Validation Pioneer",
            Role = FleetRole.Colony,
            Position = home.Position,
            CurrentSystemId = home.Id,
            StrategicSpeed = 14.0,
            SensorRange = 80.0f,
            IsActive = true,
            EmbarkedPopulationMillions = 250.0,
        };
        galaxy.Fleets.Add(fleet);

        var colonization = new ColonizationSimulation();
        var rejected = colonization.IssuePlayerColonyOrder(galaxy, player.Id, colonySystem.Id, rejectedBody.Id);
        Require(!rejected.Accepted, "temporary compatibility model incorrectly approved an arbitrary surveyed body before species-relative habitability exists");
        Require(fleet.DestinationSystemId is null, "rejected body-level colony order still changed fleet destination");

        var accepted = colonization.IssuePlayerColonyOrder(galaxy, player.Id, colonySystem.Id, compatibilityBody.Id);
        Require(accepted.Accepted, "body-level colony order rejected the deterministic compatibility candidate");
        Require(fleet.DestinationSystemId == colonySystem.Id, "accepted body-level order did not set the physical destination system");
        Require(accepted.Message.Contains(compatibilityBody.Name, StringComparison.Ordinal), "accepted colony order did not identify the selected physical world");

        var mission = readModel.Build(galaxy, player.Id).ActiveMissions.First(candidate => candidate.FleetId == fleet.Id);
        Require(mission.TargetPlanetaryBodyId == compatibilityBody.Id, "observer-local mission view did not resolve the physical colony target");

        fleet.Position = colonySystem.Position;
        fleet.CurrentSystemId = colonySystem.Id;
        fleet.DestinationSystemId = null;
        var events = colonization.Advance(galaxy);
        var founded = galaxy.Colonies.FirstOrDefault(colony =>
            colony.CivilizationId == player.Id && colony.SystemId == colonySystem.Id)
            ?? throw new InvalidOperationException("arrival at the selected physical world did not found a colony");

        Require(Math.Abs(founded.PopulationMillions - 250.0) < 0.0000001, "body-level settlement founding changed the carried population");
        Require(!fleet.IsActive && Math.Abs(fleet.EmbarkedPopulationMillions) < 0.0000001, "body-level founding did not consume the colony ship payload");
        Require(events.Any(evt => evt.ColonyId == founded.Id && evt.Message.Contains(compatibilityBody.Name, StringComparison.Ordinal)), "colony founding event did not identify the physical world");
        Require(colonization.ResolveCompatibilityColonyWorld(galaxy, founded)?.Id == compatibilityBody.Id, "founded colony did not deterministically resolve to the selected physical world");
    }

    private static void WithTemporaryDirectory(Action<string> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "stellar-continuum-planetary-validation", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            action(directory);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

using System.Numerics;
using Game.Simulation.Colonization;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Models;
using Game.Simulation.Species;

namespace Game.Simulation.Validation;

internal static class BodylessColonySettlementResolverValidation
{
    public static void ValidateReadStatusAndFoundingUseSameSpeciesRelativeBody()
    {
        var generated = new GalaxyGenerator().Generate(
            0x424F_4459_4C45_5353L,
            new GalaxyGenerationSettings
            {
                SystemCount = 48,
                PreWarpCivilizationCount = 4,
                AncientCivilizationCount = 1,
                Radius = 520.0f,
            });
        var player = generated.Civilizations.First(civilization => civilization.Id == generated.PlayerCivilizationId);
        var home = generated.Systems.First(system => system.Id == player.HomeSystemId);
        var occupied = generated.Colonies.Select(colony => colony.SystemId).ToHashSet();
        var target = generated.Systems
            .Where(system => system.Id != home.Id && !occupied.Contains(system.Id))
            .OrderBy(system => system.Id)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("validation galaxy had no unoccupied target system");

        var nextBodyId = generated.PlanetaryBodies.Max(body => body.Id) + 1000;
        var legacyFallback = new PlanetaryBodyState(
            nextBodyId,
            target.Id,
            null,
            0,
            "Legacy Fallback",
            PlanetaryBodyKind.Planet,
            1.1,
            1.2,
            new PlanetaryEnvironmentState(
                2.8,
                510.0,
                4.0,
                PlanetaryAtmosphereRegime.CarbonDioxideRich,
                PlanetarySolventRegime.None,
                0.85,
                false,
                true),
            true,
            false,
            false,
            false).Validated();
        var naturalWorld = new PlanetaryBodyState(
            nextBodyId + 1,
            target.Id,
            null,
            1,
            "Natural Haven",
            PlanetaryBodyKind.Planet,
            1.0,
            1.0,
            new PlanetaryEnvironmentState(
                1.0,
                288.0,
                101.3,
                PlanetaryAtmosphereRegime.OxygenNitrogen,
                PlanetarySolventRegime.Water,
                0.05,
                false,
                true),
            false,
            false,
            false,
            false).Validated();

        var customBodies = generated.PlanetaryBodies
            .Where(body => body.SystemId != target.Id)
            .Concat(new[] { legacyFallback, naturalWorld })
            .OrderBy(body => body.Id)
            .ToArray();
        var galaxy = new GalaxyState
        {
            Seed = generated.Seed,
            Systems = generated.Systems,
            PlanetaryBodies = customBodies,
            Civilizations = generated.Civilizations,
            Fleets = generated.Fleets,
            Colonies = generated.Colonies,
            Economies = generated.Economies,
            Technologies = generated.Technologies,
            ConstructionStates = generated.ConstructionStates,
            ShipyardStates = generated.ShipyardStates,
            PlayerCivilizationId = generated.PlayerCivilizationId,
            Knowledge = generated.Knowledge,
        };

        var habitability = new SpeciesPlanetaryHabitabilityEvaluator();
        var fallbackAssessment = habitability.Evaluate(legacyFallback, player.SpeciesId);
        var naturalAssessment = habitability.Evaluate(naturalWorld, player.SpeciesId);
        Require(fallbackAssessment.Viability == SpeciesColonizationViability.HabitatSupportedFallback,
            "fixture legacy body did not produce the intended fallback viability");
        Require(naturalAssessment.Viability == SpeciesColonizationViability.NaturallyViable,
            "fixture natural world did not produce natural viability");

        var oldCompatibilityChoice = customBodies
            .Where(body => body.SystemId == target.Id)
            .OrderBy(body => body.Id)
            .FirstOrDefault(body => body.LegacyColonizationCandidate && body.Environment.HasSolidSurface);
        Require(oldCompatibilityChoice?.Id == legacyFallback.Id,
            "fixture did not reproduce the old low-ID legacy compatibility choice");

        var independentBest = customBodies
            .Where(body => body.SystemId == target.Id)
            .Select(body => new
            {
                Body = body,
                Assessment = habitability.Evaluate(body, player.SpeciesId),
            })
            .Where(candidate => candidate.Assessment.CanFoundCurrentColony)
            .OrderByDescending(candidate => candidate.Assessment.Viability)
            .ThenByDescending(candidate => candidate.Assessment.Environment.NaturalHabitability)
            .ThenByDescending(candidate => candidate.Assessment.Environment.UnprotectedOperationalCapacity)
            .ThenBy(candidate => candidate.Body.Id)
            .Select(candidate => candidate.Body)
            .FirstOrDefault();
        Require(independentBest?.Id == naturalWorld.Id,
            "fixture did not establish a species-relative best body distinct from the old compatibility choice");

        var fleet = AddBodylessColonyFleet(galaxy, player.Id, home.Id, home.Position, player.SpeciesId);
        fleet.DestinationSystemId = target.Id;

        var readModel = new ExplorationReadModel();
        var beforeSurvey = readModel.Build(galaxy, player.Id).ActiveMissions
            .First(mission => mission.FleetId == fleet.Id);
        Require(beforeSurvey.TargetPlanetaryBodyId is null,
            "body-less mission exposed a planetary target before the destination was fully surveyed");

        galaxy.Knowledge.MarkSystemFullySurveyed(player.Id, target.Id);
        var traveling = readModel.Build(galaxy, player.Id).ActiveMissions
            .First(mission => mission.FleetId == fleet.Id);
        Require(traveling.TargetPlanetaryBodyId == naturalWorld.Id,
            "body-less traveling mission read model did not select the species-relative best world");

        fleet.Position = target.Position;
        fleet.CurrentSystemId = target.Id;
        fleet.DestinationSystemId = null;

        var arrivedView = readModel.Build(galaxy, player.Id).ActiveMissions
            .First(mission => mission.FleetId == fleet.Id);
        Require(arrivedView.TargetPlanetaryBodyId == naturalWorld.Id,
            "arrived body-less mission read model disagreed with the species-relative best world");

        var status = new ExplorationMissionStatusEvaluator().Build(galaxy, fleet);
        Require(status.Phase == ExplorationMissionPhase.ColonySettlementReady,
            "body-less arrival did not report settlement readiness on the shared best world");
        Require(status.Summary.Contains(naturalWorld.Name, StringComparison.Ordinal),
            "body-less mission status named a different settlement world than the read model");
        Require(!status.Summary.Contains(legacyFallback.Name, StringComparison.Ordinal),
            "body-less mission status regressed to the old legacy compatibility world");

        var events = new ColonizationSimulation().Advance(galaxy);
        var foundedEvent = events.FirstOrDefault(entry => entry.FleetId == fleet.Id)
            ?? throw new InvalidOperationException("body-less arrival did not found a colony");
        var founded = galaxy.Colonies.FirstOrDefault(colony => colony.Id == foundedEvent.ColonyId)
            ?? throw new InvalidOperationException("founding event referenced no colony");
        Require(founded.PlanetaryBodyId == naturalWorld.Id,
            "actual founding chose a different world than read model and mission status");
        Require(foundedEvent.Message.Contains(naturalWorld.Name, StringComparison.Ordinal),
            "founding event named a different world than the shared body-less resolver");
        Require(!fleet.IsActive && fleet.EmbarkedPopulationMillions == 0.0,
            "successful body-less founding did not consume/deactivate the colony fleet");
    }

    private static FleetState AddBodylessColonyFleet(
        GalaxyState galaxy,
        int civilizationId,
        int systemId,
        Vector2 position,
        string speciesId)
    {
        var fleet = new FleetState
        {
            Id = galaxy.Fleets.Count == 0 ? 88000 : galaxy.Fleets.Max(existing => existing.Id) + 88000,
            CivilizationId = civilizationId,
            Name = "Shared Resolver Colony Ship",
            Role = FleetRole.Colony,
            Position = position,
            CurrentSystemId = systemId,
            DestinationSystemId = null,
            DestinationPlanetaryBodyId = null,
            StrategicSpeed = 13.5,
            SensorRange = 135.0f,
            IsActive = true,
            EmbarkedPopulationMillions = 180.0,
            EmbarkedPopulationSpeciesId = speciesId,
        };
        galaxy.Fleets.Add(fleet);
        return fleet;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

using System.Numerics;
using Game.Simulation.Combat;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class CombatCasualtyValidation
{
    public static void ValidateEmbarkedPopulationCasualties()
    {
        const double embarkedPopulationMillions = 250.0;

        var galaxy = new GalaxyGenerator().Generate(
            0x4341_5355_414C_5459L,
            new GalaxyGenerationSettings
            {
                SystemCount = 24,
                PreWarpCivilizationCount = 3,
                AncientCivilizationCount = 0,
                Radius = 320.0f,
            });

        galaxy.Fleets.Clear();
        var system = galaxy.Systems[0];
        var destinationSystem = galaxy.Systems.First(candidate =>
            candidate.Id != system.Id &&
            galaxy.PlanetaryBodies.Any(body => body.SystemId == candidate.Id));
        var destinationBody = galaxy.PlanetaryBodies
            .Where(body => body.SystemId == destinationSystem.Id)
            .OrderBy(body => body.Id)
            .First();
        var attackerCivilization = galaxy.Civilizations[0];
        var targetCivilization = galaxy.Civilizations[1];

        var attacker = new FleetState
        {
            Id = 9100,
            CivilizationId = attackerCivilization.Id,
            Name = "Casualty Test Sentinel",
            Role = FleetRole.Military,
            Position = system.Position,
            CurrentSystemId = system.Id,
            StrategicSpeed = 21.0,
            SensorRange = 125.0f,
            IsActive = true,
            Combat = CombatProfileRegistry.CreateInitialState(CombatProfileIds.PatrolCorvetteMk1, FleetRole.Military),
        };

        var transport = new FleetState
        {
            Id = 9101,
            CivilizationId = targetCivilization.Id,
            Name = "Casualty Test Colony Transport",
            Role = FleetRole.Colony,
            Position = system.Position,
            CurrentSystemId = system.Id,
            DestinationSystemId = destinationSystem.Id,
            DestinationPlanetaryBodyId = destinationBody.Id,
            StrategicSpeed = 13.5,
            SensorRange = 75.0f,
            IsActive = true,
            EmbarkedPopulationMillions = embarkedPopulationMillions,
            EmbarkedPopulationSpeciesId = targetCivilization.SpeciesId,
            Combat = CombatProfileRegistry.CreateInitialState(CombatProfileIds.CivilianHeavy, FleetRole.Colony),
        };
        var transportCombat = CombatProfileRegistry.EnsureState(transport);
        transportCombat.Shields = 0.0;
        transportCombat.Armor = 0.0;
        transportCombat.Hull = 1.0;

        galaxy.Fleets.Add(attacker);
        galaxy.Fleets.Add(transport);

        var combat = new CombatSimulation(new DelegateCombatHostilityView((first, second) =>
            first == attackerCivilization.Id && second == targetCivilization.Id));
        var order = combat.IssueOrder(
            galaxy,
            attackerCivilization.Id,
            attacker.Id,
            new MilitaryOrder(MilitaryOrderType.Attack, transport.Id));
        Require(order.Accepted, $"valid hostile attack on populated colony transport was rejected: {order.Message}");

        var events = combat.Advance(galaxy, 0.25);
        var destruction = events.Single(evt =>
            evt.Type == CombatEventType.FleetDestroyed &&
            evt.TargetFleetId == transport.Id);

        Require(!transport.IsActive, "destroyed colony transport remained active");
        Require(transport.EmbarkedPopulationMillions == 0.0,
            "destroyed colony transport retained physically embarked population");
        Require(transport.EmbarkedPopulationSpeciesId is null,
            "destroyed colony transport retained stale passenger species identity");
        Require(transport.DestinationSystemId is null,
            "destroyed colony transport retained a stale destination system");
        Require(transport.DestinationPlanetaryBodyId is null,
            "destroyed colony transport retained a stale planetary-body mission target");
        Require(Math.Abs(destruction.EmbarkedPopulationCasualtiesMillions - embarkedPopulationMillions) < 0.000001,
            "fleet destruction event did not report the exact embarked population loss");
        Require(events.Where(evt => evt.Type == CombatEventType.DamageApplied)
                .All(evt => evt.EmbarkedPopulationCasualtiesMillions == 0.0),
            "ordinary damage events incorrectly reported population casualties before vessel destruction");

        var laterEvents = combat.Advance(galaxy, 1.0);
        Require(laterEvents.All(evt => evt.EmbarkedPopulationCasualtiesMillions == 0.0),
            "embarked population casualties were reported more than once after destruction");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

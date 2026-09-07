using System.Numerics;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class FirstContactValidation
{
    public static void ValidateDirectionalContactRequiresPresence()
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x434F_4E54_4143_5453L,
            new GalaxyGenerationSettings
            {
                SystemCount = 40,
                PreWarpCivilizationCount = 5,
                AncientCivilizationCount = 1,
                Radius = 480.0f,
            });

        var player = galaxy.Civilizations.First(civilization => civilization.Id == galaxy.PlayerCivilizationId);
        var foreign = galaxy.Civilizations.First(civilization => civilization.Id != player.Id);
        var neutral = galaxy.Systems.First(system =>
            system.Id != player.HomeSystemId &&
            system.Id != foreign.HomeSystemId &&
            !galaxy.Colonies.Any(colony => colony.SystemId == system.Id));

        var neutralIndex = galaxy.Systems.ToList().FindIndex(system => system.Id == neutral.Id);
        var foreignHomeIndex = galaxy.Systems.ToList().FindIndex(system => system.Id == foreign.HomeSystemId);
        galaxy.Systems[neutralIndex] = neutral with { Position = Vector2.Zero };
        galaxy.Systems[foreignHomeIndex] = galaxy.Systems[foreignHomeIndex] with { Position = new Vector2(10.0f, 0.0f) };
        neutral = galaxy.Systems[neutralIndex];
        var foreignHome = galaxy.Systems[foreignHomeIndex];

        var scout = new FleetState
        {
            Id = galaxy.Fleets.Count == 0 ? 4000 : galaxy.Fleets.Max(fleet => fleet.Id) + 4000,
            CivilizationId = player.Id,
            Name = "Contact Validation Scout",
            Role = FleetRole.Scout,
            Position = neutral.Position,
            CurrentSystemId = null,
            DestinationSystemId = neutral.Id,
            StrategicSpeed = 22.0,
            SensorRange = 135.0f,
            IsActive = true,
        };
        galaxy.Fleets.Add(scout);

        var exploration = new ExplorationSimulation();
        var remoteEvents = exploration.Advance(galaxy, 1.0);

        Require(galaxy.Knowledge.IsSystemKnown(player.Id, foreign.HomeSystemId), "validation scout did not sensor-detect the nearby foreign home star");
        Require(!galaxy.Knowledge.IsCivilizationKnown(player.Id, foreign.Id), "sensor knowledge of a foreign home star incorrectly revealed the civilization without presence");
        Require(!remoteEvents.Any(evt => evt.Type == ExplorationEventType.FirstContact && evt.CivilizationId == player.Id), "remote star detection incorrectly emitted first contact");

        Require(exploration.IssueMoveOrder(galaxy, scout.Id, foreignHome.Id), "validation scout could not be ordered into the foreign system");
        var contactEvents = exploration.Advance(galaxy, 1.0);

        Require(galaxy.Knowledge.IsCivilizationKnown(player.Id, foreign.Id), "same-system foreign presence did not create legitimate first contact knowledge");
        Require(contactEvents.Any(evt => evt.Type == ExplorationEventType.FirstContact && evt.CivilizationId == player.Id && evt.SystemId == foreignHome.Id), "same-system encounter did not emit first contact");
        Require(!galaxy.Knowledge.IsCivilizationKnown(foreign.Id, player.Id), "one-way exploration contact incorrectly granted reciprocal civilization knowledge");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

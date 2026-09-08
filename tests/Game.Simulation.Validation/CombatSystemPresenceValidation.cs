using System.Numerics;
using Game.Simulation.Combat;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class CombatSystemPresenceValidation
{
    public static void ValidateAuthoritativeSystemMilitaryPresence()
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x5052_4553_454E_4345L,
            new GalaxyGenerationSettings
            {
                SystemCount = 24,
                PreWarpCivilizationCount = 3,
                AncientCivilizationCount = 0,
                Radius = 320.0f,
            });

        galaxy.Fleets.Clear();
        var owner = galaxy.Civilizations[0];
        var hostileCivilization = galaxy.Civilizations[1];
        var neutralCivilization = galaxy.Civilizations[2];
        var system = galaxy.Systems[0];

        // Missing Combat state is evaluated from the role default without mutating the fleet.
        var ownPatrol = CreateFleet(
            1000,
            owner.Id,
            "Presence Owner Patrol",
            FleetRole.Military,
            system.Id,
            system.Position,
            combat: null);
        galaxy.Fleets.Add(ownPatrol);

        var hostilePatrols = new List<FleetState>();
        for (var i = 0; i < 17; i++)
        {
            var patrol = CreateFleet(
                2000 + i,
                hostileCivilization.Id,
                $"Presence Hostile Patrol {i}",
                FleetRole.Military,
                system.Id,
                system.Position,
                CombatProfileRegistry.CreateInitialState(CombatProfileIds.PatrolCorvetteMk1, FleetRole.Military));
            hostilePatrols.Add(patrol);
            galaxy.Fleets.Add(patrol);
        }

        var retreating = CreateFleet(
            2100,
            hostileCivilization.Id,
            "Presence Retreating Patrol",
            FleetRole.Military,
            system.Id,
            system.Position,
            CombatProfileRegistry.CreateInitialState(CombatProfileIds.PatrolCorvetteMk1, FleetRole.Military));
        retreating.Combat!.Order = MilitaryOrderType.Retreat;
        galaxy.Fleets.Add(retreating);

        var disengaged = CreateFleet(
            2101,
            hostileCivilization.Id,
            "Presence Disengaged Patrol",
            FleetRole.Military,
            system.Id,
            system.Position,
            CombatProfileRegistry.CreateInitialState(CombatProfileIds.PatrolCorvetteMk1, FleetRole.Military));
        disengaged.Combat!.IsDisengaged = true;
        disengaged.Combat.DisengagedSystemId = system.Id;
        galaxy.Fleets.Add(disengaged);

        galaxy.Fleets.Add(CreateFleet(
            2102,
            hostileCivilization.Id,
            "Presence Unarmed Scout",
            FleetRole.Scout,
            system.Id,
            system.Position,
            CombatProfileRegistry.CreateInitialState(CombatProfileIds.CivilianLight, FleetRole.Scout)));

        galaxy.Fleets.Add(CreateFleet(
            3000,
            neutralCivilization.Id,
            "Presence Neutral Patrol",
            FleetRole.Military,
            system.Id,
            system.Position,
            CombatProfileRegistry.CreateInitialState(CombatProfileIds.PatrolCorvetteMk1, FleetRole.Military)));

        var hostilityEnabled = true;
        var hostilityCalls = 0;
        var hostility = new DelegateCombatHostilityView((first, second) =>
        {
            hostilityCalls++;
            return hostilityEnabled && first == hostileCivilization.Id && second == owner.Id;
        });

        var contested = CombatSystemPresenceCalculator.Assess(galaxy, hostility, owner.Id, system.Id);
        Require(contested.Posture == SystemMilitaryPosture.Contested, "own and hostile armed presence did not produce Contested posture");
        Require(contested.OwnArmedVessels == 1, "own armed presence count was incorrect");
        Require(contested.HostileArmedVessels == hostilePatrols.Count, "hostile armed presence included or lost ineligible vessels");
        Require(contested.NonHostileForeignArmedVessels == 1, "neutral armed presence count was incorrect");
        Require(contested.HostileCivilizations == 1, "hostile civilization aggregation was incorrect");
        Require(contested.OwnCurrentStrength > 0.0 && contested.HostileCurrentStrength > contested.OwnCurrentStrength,
            "system presence did not expose finite positive authoritative strength");
        Require(contested.HasHostileInterdiction && contested.IsContested, "contested system did not expose hostile interdiction flags");
        Require(hostilityCalls == 2, $"hostility policy was evaluated per vessel instead of per foreign civilization: {hostilityCalls} calls");
        Require(ownPatrol.Combat is null, "authoritative presence read mutated missing Combat state");

        hostilityEnabled = false;
        hostilityCalls = 0;
        var secured = CombatSystemPresenceCalculator.Assess(galaxy, hostility, owner.Id, system.Id);
        Require(secured.Posture == SystemMilitaryPosture.Secured, "peaceful foreign armed presence incorrectly interdicted a secured system");
        Require(secured.HostileArmedVessels == 0 && secured.NonHostileForeignArmedVessels == hostilePatrols.Count + 1,
            "peaceful foreign forces were misclassified as hostile");
        Require(!secured.HasHostileInterdiction && hostilityCalls == 2,
            "peaceful presence produced hostile interdiction or repeated hostility scans");

        hostilityEnabled = true;
        ownPatrol.IsActive = false;
        hostilityCalls = 0;
        var interdicted = CombatSystemPresenceCalculator.Assess(galaxy, hostility, owner.Id, system.Id);
        Require(interdicted.Posture == SystemMilitaryPosture.Interdicted && interdicted.OwnArmedVessels == 0,
            "hostile-only armed presence did not produce Interdicted posture");
        Require(interdicted.HasHostileInterdiction && !interdicted.IsContested,
            "hostile-only system reported incorrect interdiction/contested flags");

        foreach (var patrol in hostilePatrols)
            patrol.Combat!.Order = MilitaryOrderType.Retreat;
        hostilityCalls = 0;
        var clear = CombatSystemPresenceCalculator.Assess(galaxy, hostility, owner.Id, system.Id);
        Require(clear.Posture == SystemMilitaryPosture.Clear && clear.HostileArmedVessels == 0,
            "retreating hostile vessels continued to exert system military control");
        Require(clear.NonHostileForeignArmedVessels == 1,
            "neutral patrol disappeared when hostile retreating vessels stopped exerting control");
        Require(hostilityCalls == 1, "only the remaining neutral foreign civilization should require a hostility evaluation");

        RequireThrows(
            () => CombatSystemPresenceCalculator.Assess(galaxy, hostility, owner.Id, int.MaxValue),
            "unknown system presence request was not rejected");
        RequireThrows(
            () => CombatSystemPresenceCalculator.Assess(galaxy, hostility, int.MaxValue, system.Id),
            "unknown civilization presence request was not rejected");
    }

    private static FleetState CreateFleet(
        int id,
        int civilizationId,
        string name,
        FleetRole role,
        int systemId,
        Vector2 position,
        FleetCombatState? combat) => new()
    {
        Id = id,
        CivilizationId = civilizationId,
        Name = name,
        Role = role,
        Position = position,
        CurrentSystemId = systemId,
        StrategicSpeed = role == FleetRole.Military ? 21.0 : 18.0,
        SensorRange = role == FleetRole.Military ? 125.0f : 80.0f,
        IsActive = true,
        Combat = combat,
    };

    private static void RequireThrows(Action action, string message)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

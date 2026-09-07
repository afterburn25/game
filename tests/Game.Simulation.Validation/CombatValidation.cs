using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Persistence;
using Game.Simulation.AI;
using Game.Simulation.Combat;
using Game.Simulation.Generation;
using Game.Simulation.Models;
using Game.Simulation.Shipbuilding;

namespace Game.Simulation.Validation;

internal static class CombatValidation
{
    public static void ValidateMilitaryShipConstruction()
    {
        var galaxy = CreateDuelGalaxy(clearFleets: true);
        var civilization = galaxy.Civilizations[0];
        var technology = galaxy.Technologies.First(state => state.CivilizationId == civilization.Id);
        technology.CompletedTechnologyIds.Add("orbital_industry");
        technology.CompletedTechnologyIds.Add("prototype_warp_drive");
        var construction = galaxy.ConstructionStates.First(state => state.CivilizationId == civilization.Id);
        construction.CompletedProjectIds.Add("orbital_shipyard");

        var simulation = new ShipbuildingSimulation();
        var design = simulation.GetAvailableDesigns(galaxy, civilization.Id)
            .Single(candidate => candidate.Id == "patrol_corvette");
        Require(design.Role == FleetRole.Military, "patrol corvette was not registered as a military vessel");
        Require(design.CombatProfileId == CombatProfileIds.PatrolCorvetteMk1, "patrol corvette did not use the stable early combat profile");

        var order = simulation.StartBuild(galaxy, civilization.Id, design.Id);
        Require(order.Accepted, $"military ship build was rejected: {order.Message}");
        galaxy.Economies.First(state => state.CivilizationId == civilization.Id).Industry += design.IndustryCost + 10.0;
        simulation.Advance(galaxy);

        var military = galaxy.Fleets.Single(fleet => fleet.CivilizationId == civilization.Id && fleet.Role == FleetRole.Military);
        var combat = CombatProfileRegistry.EnsureState(military);
        Require(combat.ProfileId == CombatProfileIds.PatrolCorvetteMk1, "constructed military vessel lost its combat profile");
        Require(combat.Hull > 0.0 && combat.Armor > 0.0 && combat.Shields > 0.0, "constructed military vessel did not initialize defenses");
    }

    public static void ValidatePeacefulFleetsDoNotFight()
    {
        var galaxy = CreateDuelGalaxy();
        var attacker = galaxy.Fleets[0];
        var target = galaxy.Fleets[1];
        var targetState = CombatProfileRegistry.EnsureState(target);
        var before = (targetState.Shields, targetState.Armor, targetState.Hull);

        var combat = new CombatSimulation();
        var order = combat.IssueOrder(galaxy, attacker.CivilizationId, attacker.Id, new MilitaryOrder(MilitaryOrderType.Attack, target.Id));
        Require(!order.Accepted, "peaceful hostility policy accepted an attack order");
        var events = combat.Advance(galaxy, 3.0);

        var after = CombatProfileRegistry.EnsureState(target);
        Require(before == (after.Shields, after.Armor, after.Hull), "peaceful fleets took combat damage");
        Require(events.All(evt => evt.Type != CombatEventType.DamageApplied), "peaceful fleets emitted a damage event");
    }

    public static void ValidateDeterministicEngagementAndDestruction()
    {
        var first = CreateDuelGalaxy();
        var second = CreateDuelGalaxy();
        WeakenSecondFleet(first);
        WeakenSecondFleet(second);

        var firstOutcome = RunDuel(first);
        var secondOutcome = RunDuel(second);

        Require(firstOutcome == secondOutcome, "identical combat state/input produced different outcomes");
        Require(first.Fleets[0].IsActive, "stronger patrol unexpectedly failed to survive deterministic duel");
        Require(!first.Fleets[1].IsActive, "weaker patrol was not destroyed");
        Require(firstOutcome.DestroyedEvents == 1, "deterministic duel did not emit exactly one destruction event");
        Require(firstOutcome.DamageEvents > 0, "deterministic duel emitted no damage events");
    }

    public static void ValidateRetreatDisengagesSurvivor()
    {
        var galaxy = CreateDuelGalaxy();
        var attacker = galaxy.Fleets[0];
        var defender = galaxy.Fleets[1];
        var combat = CreateMutualHostilityCombat(galaxy);

        Require(combat.IssueOrder(galaxy, attacker.CivilizationId, attacker.Id, new MilitaryOrder(MilitaryOrderType.Attack, defender.Id)).Accepted,
            "attacker order was rejected under explicit hostility");
        Require(combat.IssueOrder(galaxy, defender.CivilizationId, defender.Id, new MilitaryOrder(MilitaryOrderType.Retreat)).Accepted,
            "retreat order was rejected");

        var events = new List<CombatEvent>();
        for (var i = 0; i < 4 && !CombatProfileRegistry.EnsureState(defender).IsDisengaged; i++)
            events.AddRange(combat.Advance(galaxy, 0.5));

        var state = CombatProfileRegistry.EnsureState(defender);
        Require(defender.IsActive, "retreating patrol was destroyed before the expected escape window");
        Require(state.IsDisengaged, "retreating patrol did not enter tactical disengagement state");
        Require(state.DisengagedSystemId == defender.CurrentSystemId, "disengagement did not retain its system boundary");
        Require(events.Any(evt => evt.Type == CombatEventType.FleetRetreatInitiated), "retreat did not emit an initiation event");
        Require(events.Any(evt => evt.Type == CombatEventType.FleetEscaped), "successful retreat did not emit an escape event");
        Require(CombatProfileRegistry.EnsureState(attacker).Order == MilitaryOrderType.Hold, "attacker kept a stale attack order after target escaped");
    }

    public static void ValidateCombatSaveRoundTripAndLegacyDefault()
    {
        var galaxy = CreateDuelGalaxy();
        var fleet = galaxy.Fleets[0];
        var state = CombatProfileRegistry.EnsureState(fleet);
        state.Shields = 11.0;
        state.Armor = 22.0;
        state.Hull = 33.0;
        state.WeaponCooldownRemainingDays = 0.4;
        state.Order = MilitaryOrderType.Retreat;
        state.RetreatProgressDays = 0.6;
        state.RetreatStarted = true;

        WithTemporaryDirectory(directory =>
        {
            var service = new CampaignSaveService();
            var path = Path.Combine(directory, "combat-v7.json");
            service.Save(path, galaxy, 800.25);
            var loaded = service.Load(path);
            var loadedFleet = loaded.Galaxy.Fleets.Single(candidate => candidate.Id == fleet.Id);
            var loadedState = CombatProfileRegistry.EnsureState(loadedFleet);

            Require(CampaignSaveService.CurrentFormatVersion == 7, "combat additive state unexpectedly changed the shared save format version");
            Require(loadedState.ProfileId == state.ProfileId, "save/load changed combat profile identity");
            Require(Math.Abs(loadedState.Shields - 11.0) < 0.000001, "save/load changed shield damage state");
            Require(Math.Abs(loadedState.Armor - 22.0) < 0.000001, "save/load changed armor damage state");
            Require(Math.Abs(loadedState.Hull - 33.0) < 0.000001, "save/load changed hull damage state");
            Require(Math.Abs(loadedState.WeaponCooldownRemainingDays - 0.4) < 0.000001, "save/load changed weapon cooldown");
            Require(loadedState.Order == MilitaryOrderType.Retreat && loadedState.RetreatStarted, "save/load changed retreat order state");
            Require(Math.Abs(loadedState.RetreatProgressDays - 0.6) < 0.000001, "save/load changed retreat progress");

            var root = JsonNode.Parse(File.ReadAllText(path))?.AsObject()
                ?? throw new InvalidOperationException("could not parse generated combat save");
            var fleets = root["Galaxy"]?["Fleets"]?.AsArray()
                ?? throw new InvalidOperationException("generated combat save had no fleets");
            fleets[0]?.AsObject().Remove("Combat");
            var legacyPath = Path.Combine(directory, "combatless-v7.json");
            File.WriteAllText(legacyPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            var migrated = service.Load(legacyPath);
            var migratedFleet = migrated.Galaxy.Fleets.Single(candidate => candidate.Id == fleet.Id);
            var migratedState = CombatProfileRegistry.EnsureState(migratedFleet);
            Require(migratedState.ProfileId == CombatProfileIds.PatrolCorvetteMk1, "combatless v7 fleet did not receive role-based combat defaults");
            Require(migratedState.Hull > 0.0, "combatless v7 fleet migrated as destroyed");
        });
    }

    public static void ValidateFairInformationMilitarySummary()
    {
        var known = new KnownCivilization(
            CivilizationId: 77,
            Trust: 0.2,
            EstimatedMilitaryLow: 120.0,
            EstimatedMilitaryHigh: 310.0,
            EstimateConfidence: 0.43,
            LastMilitaryObservationTick: 1234,
            HasSharedBorder: true,
            KnownTradeDependence: 0.1,
            KnownWarExhaustion: 0.0,
            KnownToBeAtWar: false,
            HasDefenseTreatyWithObserver: false);

        var summary = KnownMilitaryForceSummary.FromKnowledge(known);
        Require(summary.CivilizationId == 77, "known military summary changed civilization identity");
        Require(summary.EstimatedStrengthLow == 120.0 && summary.EstimatedStrengthHigh == 310.0, "known military summary replaced intelligence estimates with another value");
        Require(summary.Confidence == 0.43 && summary.LastObservationTick == 1234, "known military summary lost uncertainty/freshness inputs");
    }

    private static DuelOutcome RunDuel(GalaxyState galaxy)
    {
        var first = galaxy.Fleets[0];
        var second = galaxy.Fleets[1];
        var combat = CreateMutualHostilityCombat(galaxy);
        Require(combat.IssueOrder(galaxy, first.CivilizationId, first.Id, new MilitaryOrder(MilitaryOrderType.Attack, second.Id)).Accepted,
            "first duel attack order was rejected");
        Require(combat.IssueOrder(galaxy, second.CivilizationId, second.Id, new MilitaryOrder(MilitaryOrderType.Attack, first.Id)).Accepted,
            "second duel attack order was rejected");

        var events = new List<CombatEvent>();
        for (var i = 0; i < 12 && first.IsActive && second.IsActive; i++)
            events.AddRange(combat.Advance(galaxy, 0.75));

        var firstState = CombatProfileRegistry.EnsureState(first);
        var secondState = CombatProfileRegistry.EnsureState(second);
        return new DuelOutcome(
            first.IsActive,
            second.IsActive,
            firstState.Shields,
            firstState.Armor,
            firstState.Hull,
            secondState.Shields,
            secondState.Armor,
            secondState.Hull,
            events.Count(evt => evt.Type == CombatEventType.DamageApplied),
            events.Count(evt => evt.Type == CombatEventType.FleetDestroyed));
    }

    private static CombatSimulation CreateMutualHostilityCombat(GalaxyState galaxy)
    {
        var firstCivilizationId = galaxy.Fleets[0].CivilizationId;
        var secondCivilizationId = galaxy.Fleets[1].CivilizationId;
        return new CombatSimulation(new DelegateCombatHostilityView((first, second) =>
            (first == firstCivilizationId && second == secondCivilizationId) ||
            (first == secondCivilizationId && second == firstCivilizationId)));
    }

    private static GalaxyState CreateDuelGalaxy(bool clearFleets = false)
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x434F_4D42_4154L,
            new GalaxyGenerationSettings
            {
                SystemCount = 24,
                PreWarpCivilizationCount = 3,
                AncientCivilizationCount = 0,
                Radius = 320.0f,
            });

        galaxy.Fleets.Clear();
        if (clearFleets)
            return galaxy;

        var system = galaxy.Systems[0];
        var firstCivilization = galaxy.Civilizations[0];
        var secondCivilization = galaxy.Civilizations[1];
        galaxy.Fleets.Add(CreatePatrol(100, firstCivilization.Id, "Validation Sentinel", system.Id, system.Position));
        galaxy.Fleets.Add(CreatePatrol(101, secondCivilization.Id, "Validation Rival", system.Id, system.Position));
        return galaxy;
    }

    private static FleetState CreatePatrol(int id, int civilizationId, string name, int systemId, Vector2 position) => new()
    {
        Id = id,
        CivilizationId = civilizationId,
        Name = name,
        Role = FleetRole.Military,
        Position = position,
        CurrentSystemId = systemId,
        StrategicSpeed = 21.0,
        SensorRange = 125.0f,
        IsActive = true,
        Combat = CombatProfileRegistry.CreateInitialState(CombatProfileIds.PatrolCorvetteMk1, FleetRole.Military),
    };

    private static void WeakenSecondFleet(GalaxyState galaxy)
    {
        var state = CombatProfileRegistry.EnsureState(galaxy.Fleets[1]);
        state.Hull = 20.0;
    }

    private static void WithTemporaryDirectory(Action<string> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "stellar-continuum-combat-validation", Guid.NewGuid().ToString("N"));
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

    private sealed record DuelOutcome(
        bool FirstActive,
        bool SecondActive,
        double FirstShields,
        double FirstArmor,
        double FirstHull,
        double SecondShields,
        double SecondArmor,
        double SecondHull,
        int DamageEvents,
        int DestroyedEvents);
}

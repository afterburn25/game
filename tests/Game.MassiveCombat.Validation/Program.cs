using System.Diagnostics;
using System.Text.Json;
using Game.Simulation.Combat;
using Game.Simulation.Combat.Massive;

namespace Game.MassiveCombat.Validation;

internal static class Program
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("fixed tick determinism and save roundtrip", DeterminismAndSave),
            ("100,000 ships use bounded formation work", MassiveBenchmark),
            ("interdiction blocks spool until a real module drops", InterdictionAndEscape),
            ("persistent damage lowers power and preserves important identity", DamagePowerAndIdentity),
            ("observer snapshot hides unauthorized enemy power", ObserverSafety),
            ("observer events sanitize unidentified attackers", ObserverEventSafety),
            ("equipment enforces slot and mass budgets", EquipmentBudgets),
            ("orders validate ownership and protection targets", OrderAuthority),
            ("doctrine uses observer-safe interdiction evidence", DoctrineUsesSnapshot),
            ("events and catch-up remain bounded", BoundedRuntime),
        };
        var failures = 0;
        foreach (var test in tests)
        {
            try { test.Run(); Console.WriteLine($"PASS: {test.Name}"); }
            catch (Exception exception) { failures++; Console.Error.WriteLine($"FAIL: {test.Name}\n{exception}"); }
        }
        Console.WriteLine($"Massive combat validation: {tests.Length - failures}/{tests.Length} passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void DeterminismAndSave()
    {
        var original = Duel(1_000, 0xD371UL);
        var engine = new MassiveCombatEngine();
        Require(engine.IssueOrder(original, 0, new(1, MassiveCombatOrderType.Engage, 2)).Accepted, "engage rejected");
        Require(engine.IssueOrder(original, 1, new(2, MassiveCombatOrderType.Engage, 1)).Accepted, "return engage rejected");
        var serialized = JsonSerializer.Serialize(original, Json);
        var whole = JsonSerializer.Deserialize<MassiveCombatBattleState>(serialized, Json)!;
        var sliced = JsonSerializer.Deserialize<MassiveCombatBattleState>(serialized, Json)!;
        engine.Advance(whole, 10);
        for (var i = 0; i < 100; i++) engine.Advance(sliced, .1);
        Require(JsonSerializer.Serialize(whole, Json) == JsonSerializer.Serialize(sliced, Json), "render-frame slicing changed authoritative state");

        var pending = Duel(5, 77); engine.Advance(pending, .07);
        var loaded = JsonSerializer.Deserialize<MassiveCombatBattleState>(JsonSerializer.Serialize(pending, Json), Json)!;
        engine.Advance(loaded, .03);
        Require(loaded.Tick == 1 && loaded.PendingSeconds == 0, "save lost the fixed-tick accumulator");
        Require(loaded.Formations[0].Position.X != 0 || loaded.Formations[0].Position.Y == 0, "serialized coordinates were not restored");

        var missileBattle = Duel(20, 78);
        missileBattle.Formations[0].Loadout.Weapons[0].Kind = MassiveWeaponKind.Missile;
        Require(engine.IssueOrder(missileBattle, 0, new(1, MassiveCombatOrderType.Engage, 2)).Accepted, "missile engage rejected");
        engine.Advance(missileBattle, .1);
        Require(missileBattle.ActiveSalvos.Count > 0, "missiles did not create persistent in-flight salvos");
        var missileLoaded = JsonSerializer.Deserialize<MassiveCombatBattleState>(JsonSerializer.Serialize(missileBattle, Json), Json)!;
        Require(missileLoaded.ActiveSalvos.Count == missileBattle.ActiveSalvos.Count && missileLoaded.NextSalvoId == missileBattle.NextSalvoId,
            "save lost bounded active missile salvos");
    }

    private static void MassiveBenchmark()
    {
        var formations = new List<MassiveFormationState>();
        for (var side = 0; side < 2; side++)
            for (var i = 0; i < 50; i++)
                formations.Add(Formation(side * 100 + i + 1, side, side * 10 + i / 10, side * 1000 + i / 5,
                    $"Side {side} Formation {i}", 1_000, new(side == 0 ? -300 + i * 4 : 300 - i * 4, (i % 10) * 35), BasicLoadout(.12f)));
        var battle = MassiveCombatBattleState.Create(0x100000UL, formations);
        foreach (var formation in battle.Formations) formation.Order = MassiveCombatOrderType.Advance;
        var stopwatch = Stopwatch.StartNew();
        var metrics = new MassiveCombatEngine().Advance(battle, 10); stopwatch.Stop();
        Require(battle.Formations.Sum(x => x.InitialShipCount) == 100_000, "stress battle did not authoritatively contain 100,000 ships");
        Require(metrics.ActiveFormations <= 100 && metrics.TargetCandidatesExamined < 4_000_000, "targeting work was not bounded by the spatial formation index");
        Require(battle.Events.Count <= MassiveCombatLimits.MaxRetainedEvents, "stress events exceeded their cap");
        Console.WriteLine($"BENCHMARK massive: {stopwatch.Elapsed.TotalMilliseconds:0.0} ms, candidates={metrics.TargetCandidatesExamined:N0}, weaponGroups={metrics.WeaponGroupsResolved:N0}, machine={Environment.MachineName}/{Environment.ProcessorCount} logical CPUs");
    }

    private static void InterdictionAndEscape()
    {
        var hunter = Formation(1, 0, 1, 1, "Interdictor Guard", 20, new(0, 0), BasicLoadout(.1f));
        hunter.Loadout.Modules.Add(MassiveCombatLoadouts.WarpInterdictor(900, 72));
        hunter.ImportantVessels.Add(new() { Id = 5001, Name = "TCS Ravager", DesignId = "interdictor", IsInterdictor = true });
        hunter.Cohorts[0].InitialCount--; hunter.Cohorts[0].ActiveCount--; hunter.InitialShipCount = hunter.ActiveShipCount;
        hunter.ShieldPool = hunter.Loadout.ShieldPerShip * hunter.ActiveShipCount; hunter.ArmorPool = hunter.Loadout.ArmorPerShip * hunter.ActiveShipCount; hunter.HullPool = hunter.Loadout.HullPerShip * hunter.ActiveShipCount;
        var runnerLoadout = BasicLoadout(0); runnerLoadout.WarpSpoolSeconds = 2; runnerLoadout.WarpStabilization = 48;
        var runner = Formation(2, 1, 2, 2, "Runner", 10, new(150, 0), runnerLoadout);
        var battle = MassiveCombatBattleState.Create(5, new[] { hunter, runner });
        var engine = new MassiveCombatEngine(); engine.IssueOrder(battle, 1, new(2, MassiveCombatOrderType.Retreat));
        engine.Advance(battle, 3);
        Require(runner.WarpBlocked && !runner.Escaped && battle.Events.Any(x => x.Type == MassiveCombatEventType.WarpBlocked), "active powered interdictor did not block warp");
        hunter.Loadout.Modules.Single(x => x.Kind == MassiveModuleKind.WarpInterdictor).Condition = 0;
        engine.Advance(battle, 3);
        Require(runner.Escaped, "runner did not complete warp after the real interdiction module failed");
    }

    private static void DamagePowerAndIdentity()
    {
        var battle = Duel(200, 91);
        var named = new MassiveVesselState { Id = 9001, Name = "CSV Endurance", DesignId = "flagship", IsFlagship = true, BattlesFought = 4 };
        var own = battle.Formations[0]; own.ImportantVessels.Add(named); own.Cohorts[0].InitialCount--; own.Cohorts[0].ActiveCount--; own.InitialShipCount = own.ActiveShipCount;
        battle.Formations[1].Loadout.Weapons[0].DamagePerShot = 20;
        var before = MassiveCombatPowerCalculator.FormationPower(own);
        var engine = new MassiveCombatEngine(); engine.IssueOrder(battle, 0, new(1, MassiveCombatOrderType.Engage, 2)); engine.IssueOrder(battle, 1, new(2, MassiveCombatOrderType.Engage, 1));
        engine.Advance(battle, 8);
        var after = MassiveCombatPowerCalculator.FormationPower(own);
        Require(after < before && (own.DestroyedShips > 0 || own.HullPool < own.Loadout.HullPerShip * own.InitialShipCount), "damage did not reduce authoritative combat power");
        var copy = JsonSerializer.Deserialize<MassiveCombatBattleState>(JsonSerializer.Serialize(battle, Json), Json)!;
        Require(copy.Formations.SelectMany(x => x.ImportantVessels).Single(x => x.Id == 9001).Name == "CSV Endurance", "important vessel identity/history did not persist");
        var outcome = MassiveCombatOutcomeBuilder.Build(copy).Fleets.Single(x => x.FleetId == own.FleetId);
        Require(outcome.SurvivingShips + outcome.DestroyedShips == own.InitialShipCount, "FleetId outcome did not conserve exact ships");
    }

    private static void ObserverSafety()
    {
        var battle = Duel(100, 42); var sensors = new TestSensors(power: false);
        var first = MassiveCombatObserver.BuildSnapshot(battle, 0, sensors);
        Require(first.ExactOwnShips == 100, "snapshot exact total included hidden enemy ships");
        var enemy = first.Formations.Single(x => x.CivilizationId == 1);
        Require(enemy.StrengthLow is null && enemy.StrengthHigh is null && enemy.PerShipCombatPower is null && !enemy.IsExact, "unauthorized enemy power leaked");
        battle.Formations[1].Loadout.Weapons[0].DamagePerShot *= 1000;
        var second = MassiveCombatObserver.BuildSnapshot(battle, 0, sensors).Formations.Single(x => x.CivilizationId == 1);
        Require(enemy.StrengthLow == second.StrengthLow && enemy.StrengthHigh == second.StrengthHigh &&
            enemy.PerShipCombatPower == second.PerShipCombatPower && enemy.ShipCountLow == second.ShipCountLow && enemy.ShipCountHigh == second.ShipCountHigh,
            "hidden enemy power changed an unauthorized observer snapshot");
        var scanned = MassiveCombatObserver.BuildSnapshot(battle, 0, new TestSensors(power: true)).Formations.Single(x => x.CivilizationId == 1);
        Require(scanned.StrengthLow > 0 && scanned.StrengthHigh >= scanned.StrengthLow && scanned.PerShipCombatPower > 0, "authorized scan did not expose a bounded power estimate");
        Require(!MassiveCombatObserver.BuildSnapshot(battle, 0, new ZeroSensors()).Formations.Any(x => x.CivilizationId == 1), "zero-confidence enemy appeared in snapshot");
    }

    private static void ObserverEventSafety()
    {
        var battle = Duel(10, 99);
        battle.Events.Add(new(1, 1, MassiveCombatEventType.MissileSalvo, 1, 2, 0, 1, 9876, new(999, 888), "Secret Raiders fired 9,876 missiles."));
        var observed = MassiveCombatObserver.BuildSnapshot(battle, 0, new TestSensors(power: false)).Events.Single();
        Require(!observed.DetailsKnown && observed.ActorFormationId is null && observed.ActorCivilizationId is null &&
            observed.Magnitude is null && observed.Position is null && !observed.Message.Contains("Secret", StringComparison.Ordinal),
            "unknown attacker details leaked through the event stream");
    }

    private static void EquipmentBudgets()
    {
        var loadout = BasicLoadout(0); loadout.ModuleSlotCapacity = 1;
        loadout.Modules.Add(MassiveCombatLoadouts.WarpInterdictor());
        RequireThrows(loadout.Validate, "slot/mass overfit loadout was accepted");
    }

    private static void OrderAuthority()
    {
        var battle = Duel(10, 3); var engine = new MassiveCombatEngine();
        Require(!engine.IssueOrder(battle, 1, new(1, MassiveCombatOrderType.Hold)).Accepted, "foreign formation accepted player order");
        Require(!engine.IssueOrder(battle, 0, new(1, MassiveCombatOrderType.ProtectCriticalAsset, 2)).Accepted, "hostile formation accepted as protected asset");
        var beforeOrder = battle.Formations[0].Order; var beforeTarget = battle.Formations[0].TargetFormationId;
        Require(!engine.IssueOrder(battle, 0, new(1, MassiveCombatOrderType.Engage, 2, new(float.NaN, 0))).Accepted,
            "nonfinite objective was accepted");
        Require(battle.Formations[0].Order == beforeOrder && battle.Formations[0].TargetFormationId == beforeTarget,
            "rejected order partially mutated authoritative state");
        var escort = Formation(3, 0, 3, 3, "Escort", 5, new(-50, 0), BasicLoadout(.1f));
        battle.Formations.Add(escort); battle.Validate();
        Require(engine.IssueOrder(battle, 0, new(3, MassiveCombatOrderType.ProtectCriticalAsset, 1, Shape: MassiveFormationShape.Escort)).Accepted, "friendly protection order rejected");
        Require(engine.IssueOrder(battle, 0, new(3, MassiveCombatOrderType.Surrender)).Accepted && escort.Surrendered,
            "explicit surrender did not preserve a reconciliable formation outcome");
    }

    private static void BoundedRuntime()
    {
        var battle = Duel(500, 8); var engine = new MassiveCombatEngine();
        battle.Formations[0].Order = battle.Formations[1].Order = MassiveCombatOrderType.Advance;
        engine.Advance(battle, 1_000);
        Require(battle.Tick == MassiveCombatLimits.MaxCatchUpTicks, "catch-up work exceeded fixed cap");
        Require(battle.PendingSeconds > 0 && battle.Events.Count <= MassiveCombatLimits.MaxRetainedEvents, "catch-up remainder or event bound was lost");
        Require(battle.ActiveSalvos.Count <= MassiveCombatLimits.MaxActiveSalvos, "active salvo state exceeded its bound");
        RequireThrows(() => engine.Advance(battle, double.NaN), "invalid time was accepted");
    }

    private static void DoctrineUsesSnapshot()
    {
        var battle = Duel(10, 31);
        battle.Formations[0].WarpBlocked = true;
        battle.Formations[1].Loadout.Modules.Add(MassiveCombatLoadouts.WarpInterdictor());
        var hiddenOrders = MassiveCombatDoctrine.Decide(MassiveCombatObserver.BuildSnapshot(battle, 0, new ZeroSensors()), 0);
        Require(hiddenOrders.Single().Type == MassiveCombatOrderType.EmergencyRetreat,
            "doctrine inferred a hidden enemy interdictor outside its snapshot");
        var identifiedOrders = MassiveCombatDoctrine.Decide(MassiveCombatObserver.BuildSnapshot(battle, 0, new FullSensors()), 0);
        Require(identifiedOrders.Single().Type == MassiveCombatOrderType.Breakout && identifiedOrders.Single().TargetFormationId == 2,
            "doctrine did not break out against an identified interdictor");
    }

    private static MassiveCombatBattleState Duel(int shipsPerSide, ulong seed) => MassiveCombatBattleState.Create(seed, new[]
    {
        Formation(1, 0, 10, 100, "Human Line", shipsPerSide, new(-120, 0), BasicLoadout(.5f)),
        Formation(2, 1, 20, 200, "Enemy Line", shipsPerSide, new(120, 0), BasicLoadout(.5f)),
    });

    private static MassiveFormationState Formation(long id, int civilization, int fleet, int taskForce, string name, int ships, MassivePoint position, MassiveCombatLoadout loadout) => new()
    {
        Id = id, CivilizationId = civilization, FleetId = fleet, TaskForceId = taskForce, Name = name,
        Position = position, Objective = position, Loadout = loadout,
        Cohorts = new() { new() { Id = id * 10, DesignId = "line_ship", InitialCount = ships, ActiveCount = ships } },
        InitialShipCount = ships, ShieldPool = loadout.ShieldPerShip * ships, ArmorPool = loadout.ArmorPerShip * ships, HullPool = loadout.HullPerShip * ships,
    };

    private static MassiveCombatLoadout BasicLoadout(float damage)
    {
        var legacy = new CombatProfileDefinition("massive_test", 35, 45, 95, damage, .2, 1);
        var result = MassiveCombatLoadouts.FromLegacy(legacy);
        result.Acceleration = 20; result.MaximumSpeed = 100; result.ReactorOutputPerShip = 100; result.CoolingPerShip = 20;
        if (result.Weapons.Count > 0) { result.Weapons[0].ShotsPerSecond = 1; result.Weapons[0].Accuracy = .7f; }
        return result;
    }

    private sealed class TestSensors(bool power) : IMassiveCombatSensorView
    {
        public float Confidence(int observerCivilizationId, long formationId) => .35f;
        public bool IdentifiesImportantVessels(int observerCivilizationId, long formationId) => false;
        public bool CanEstimateCombatPower(int observerCivilizationId, long formationId) => power;
    }
    private sealed class ZeroSensors : IMassiveCombatSensorView
    {
        public float Confidence(int observerCivilizationId, long formationId) => 0;
        public bool IdentifiesImportantVessels(int observerCivilizationId, long formationId) => false;
        public bool CanEstimateCombatPower(int observerCivilizationId, long formationId) => false;
    }
    private sealed class FullSensors : IMassiveCombatSensorView
    {
        public float Confidence(int observerCivilizationId, long formationId) => 1;
        public bool IdentifiesImportantVessels(int observerCivilizationId, long formationId) => true;
        public bool CanEstimateCombatPower(int observerCivilizationId, long formationId) => true;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void RequireThrows(Action action, string message) { try { action(); } catch (Exception) { return; } throw new InvalidOperationException(message); }
}

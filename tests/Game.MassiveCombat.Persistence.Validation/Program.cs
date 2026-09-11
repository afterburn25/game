using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Campaign;
using Game.Persistence;
using Game.Simulation.Combat;
using Game.Simulation.Combat.Massive;
using Game.Simulation.Models;

namespace Game.MassiveCombat.Persistence.Validation;

internal static class Program
{
    private static int Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "stellar-massive-persistence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            OldSaveShapeRemainsOptional(root);
            ActiveEncounterRoundTripsThroughSession(root);
            InvalidBindingsAreRejectedOnSaveAndLoad(root);
            Console.WriteLine("Massive combat persistence validation: 3/3 passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void OldSaveShapeRemainsOptional(string root)
    {
        var path = Path.Combine(root, "ordinary.json");
        var galaxy = new CampaignSessionService().CreateNew(31001).Galaxy;
        new CampaignSaveService().Save(path, galaxy, 12);
        var json = File.ReadAllText(path);
        Require(!json.Contains("\"ActiveCombatEncounter\"", StringComparison.Ordinal) &&
            !json.Contains("\"CombatIntelligence\"", StringComparison.Ordinal) &&
            !json.Contains("\"TacticalLoadout\"", StringComparison.Ordinal) &&
            !json.Contains("\"TacticalVessel\"", StringComparison.Ordinal),
            "ordinary saves acquired empty combat-runtime payloads");
        var restored = new CampaignSaveService().Load(path).Galaxy;
        Require(restored.ActiveCombatEncounter is null && restored.CombatIntelligence.Count == 0 &&
            restored.Fleets.All(x => x.TacticalLoadout is null && x.TacticalVessel is null),
            "save without optional combat fields did not restore compatible defaults");
    }

    private static void ActiveEncounterRoundTripsThroughSession(string root)
    {
        var path = Path.Combine(root, "active-campaign.json");
        var session = new CampaignSessionService();
        var result = session.CreateNew(31002); var galaxy = result.Galaxy;
        AttachEncounter(galaxy);
        session.Save(path, galaxy, result.Diplomacy, result.AdaptiveResearch, 42.5);
        var restored = session.LoadOrCreate(path, -1);
        Require(restored.WasLoaded && restored.SimulationDays == 42.5, "campaign session did not load the combat save");
        var encounter = restored.Galaxy.ActiveCombatEncounter ?? throw new InvalidOperationException("active encounter was lost");
        encounter.Validate(restored.Galaxy);
        Require(encounter.Battle.Tick == 1 && encounter.Battle.ActiveSalvos.Count > 0 && encounter.Battle.PendingSeconds == 0,
            "active battle tick or missile salvos did not round-trip");
        var module = encounter.Battle.Formations[0].Loadout.Modules.Single(x => x.Kind == MassiveModuleKind.WarpInterdictor);
        Require(module.Condition == .73f && module.EffectiveRange == 900, "installed module state did not round-trip");
        Require(restored.Galaxy.Fleets.All(x => x.TacticalLoadout is not null && x.TacticalVessel?.Id == x.Id),
            "persistent fleet equipment or named-vessel identity was lost");
        var intel = restored.Galaxy.CombatIntelligence.Single();
        Require(intel.Power == 4321 && intel.ObservedDay == 41 && intel.Evidence == "Engagement", "dated combat intelligence did not round-trip");
    }

    private static void InvalidBindingsAreRejectedOnSaveAndLoad(string root)
    {
        var service = new CampaignSaveService(); var galaxy = new CampaignSessionService().CreateNew(31003).Galaxy;
        AttachEncounter(galaxy);
        var validPath = Path.Combine(root, "valid.json"); service.Save(validPath, galaxy, 9);

        galaxy.ActiveCombatEncounter!.Vessels[0] = new(galaxy.ActiveCombatEncounter.Vessels[0].FleetId, 999_999);
        RequireThrows(() => service.Save(Path.Combine(root, "invalid-save.json"), galaxy, 9),
            "save accepted a combat binding to a missing formation");

        var rootNode = JsonNode.Parse(File.ReadAllText(validPath))!.AsObject();
        rootNode["Galaxy"]!["ActiveCombatEncounter"]!["Vessels"]![0]!["FormationId"] = 999_999;
        var invalidLoad = Path.Combine(root, "invalid-load.json");
        File.WriteAllText(invalidLoad, rootNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        RequireThrows(() => service.Load(invalidLoad), "load accepted a combat binding to a missing formation");
    }

    private static void AttachEncounter(GalaxyState galaxy)
    {
        galaxy.Fleets.Clear();
        var system = galaxy.Systems[0];
        var civilizations = galaxy.Civilizations.Take(2).ToArray();
        Require(civilizations.Length == 2, "test galaxy did not contain two civilizations");
        var profile = CombatProfileRegistry.Get(CombatProfileIds.PatrolCorvetteMk1);
        for (var index = 0; index < 2; index++)
        {
            var id = index + 1; var loadout = MassiveCombatLoadouts.FromLegacy(profile);
            loadout.Weapons[0].Kind = MassiveWeaponKind.Missile;
            loadout.Weapons[0].ShotsPerSecond = 10;
            if (index == 0)
            {
                var interdictor = MassiveCombatLoadouts.WarpInterdictor(); interdictor.Condition = .73f;
                loadout.Modules.Add(interdictor);
            }
            galaxy.Fleets.Add(new FleetState
            {
                Id = id, CivilizationId = civilizations[index].Id, Name = index == 0 ? "ISS Persistence" : "HSS Durable",
                Role = FleetRole.Military, Position = system.Position, CurrentSystemId = system.Id,
                Combat = CombatProfileRegistry.CreateInitialState(profile.Id, FleetRole.Military),
                TacticalLoadout = loadout,
                TacticalVessel = new() { Id = id, Name = index == 0 ? "ISS Persistence" : "HSS Durable", DesignId = profile.Id, IsFlagship = true },
            });
        }
        var formations = galaxy.Fleets.Select((fleet, index) => new MassiveFormationState
        {
            Id = index + 1, CivilizationId = fleet.CivilizationId, FleetId = fleet.Id, TaskForceId = fleet.Id,
            Name = fleet.Name, Position = new(index == 0 ? -120 : 120, 0), Heading = new(index == 0 ? 1 : -1, 0),
            Objective = new(0, 0), Order = MassiveCombatOrderType.Engage,
            Loadout = Clone(fleet.TacticalLoadout!), ImportantVessels = [Clone(fleet.TacticalVessel!)],
        }).ToArray();
        var battle = MassiveCombatBattleState.Create(781, formations);
        var engine = new MassiveCombatEngine();
        engine.IssueOrder(battle, formations[0].CivilizationId, new(1, MassiveCombatOrderType.Engage, 2));
        engine.IssueOrder(battle, formations[1].CivilizationId, new(2, MassiveCombatOrderType.Engage, 1));
        engine.Advance(battle, .1);
        Require(battle.ActiveSalvos.Count > 0, "test encounter did not contain an in-flight salvo");
        galaxy.ActiveCombatEncounter = new()
        {
            SystemId = system.Id, StartedDay = 40, Battle = battle,
            Vessels = [new(1, 1), new(2, 2)],
        };
        galaxy.CombatIntelligence = [new(civilizations[0].Id, 2, 4321, 41, "Engagement")];
        galaxy.ActiveCombatEncounter.Validate(galaxy);
    }

    private static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void RequireThrows(Action action, string message)
    {
        try { action(); } catch (Exception) { return; }
        throw new InvalidOperationException(message);
    }
}

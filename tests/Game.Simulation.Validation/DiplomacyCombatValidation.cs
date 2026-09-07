using System.Numerics;
using System.Runtime.CompilerServices;
using Game.Simulation.Combat;
using Game.Simulation.Diplomacy;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class DiplomacyCombatValidation
{
    [ModuleInitializer]
    internal static void RunDiplomacyCombatChecks()
    {
        ValidatePoliticalHostilityControlsCombatAndCombatCreatesGrievance();
        ValidateUnattributedCombatCannotLeakIdentity();
        Console.WriteLine("PASS: diplomacy-combat political hostility and incident consequences");
    }

    private static void ValidatePoliticalHostilityControlsCombatAndCombatCreatesGrievance()
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x4449_504C_434F_4D42L,
            new GalaxyGenerationSettings
            {
                SystemCount = 24,
                PreWarpCivilizationCount = 3,
                AncientCivilizationCount = 0,
                Radius = 320.0f,
            });

        var firstCivilization = galaxy.Civilizations[0];
        var secondCivilization = galaxy.Civilizations[1];
        var system = galaxy.Systems[0];
        galaxy.Fleets.Clear();
        var attacker = CreatePatrol(8100, firstCivilization.Id, "Diplomatic Sentinel", system.Id, system.Position);
        var target = CreatePatrol(8101, secondCivilization.Id, "Diplomatic Rival", system.Id, system.Position);
        galaxy.Fleets.Add(attacker);
        galaxy.Fleets.Add(target);

        var state = new DiplomacyState();
        var diplomacy = new DiplomacySimulation(state);
        EstablishIdentifiedContact(diplomacy, firstCivilization.Id, secondCivilization.Id, "first-to-second", tick: 1, system.Id);
        EstablishIdentifiedContact(diplomacy, secondCivilization.Id, firstCivilization.Id, "second-to-first", tick: 1, system.Id);

        var hostility = new DiplomacyCombatHostilityView(state);
        var combat = new CombatSimulation(hostility);
        var incidentBridge = new CombatDiplomacyBridge(state);

        Require(!hostility.AreHostile(firstCivilization.Id, secondCivilization.Id),
            "peaceful diplomacy unexpectedly authorized combat");
        var peacefulAttack = combat.IssueOrder(
            galaxy,
            firstCivilization.Id,
            attacker.Id,
            new MilitaryOrder(MilitaryOrderType.Attack, target.Id));
        Require(!peacefulAttack.Accepted,
            "Combat accepted an attack while Diplomacy still recorded peace");

        diplomacy.SetHostile(firstCivilization.Id, secondCivilization.Id, tick: 2, "Escalating border confrontation.");
        Require(hostility.AreHostile(firstCivilization.Id, secondCivilization.Id),
            "Hostile diplomatic state did not authorize Combat");
        Require(hostility.AreHostile(secondCivilization.Id, firstCivilization.Id),
            "bilateral Hostile state was not visible symmetrically to Combat");

        var targetCombat = CombatProfileRegistry.EnsureState(target);
        targetCombat.Shields = 0.0;
        targetCombat.Armor = 0.0;
        targetCombat.Hull = 1.0;

        var hostileAttack = combat.IssueOrder(
            galaxy,
            firstCivilization.Id,
            attacker.Id,
            new MilitaryOrder(MilitaryOrderType.Attack, target.Id));
        Require(hostileAttack.Accepted,
            $"Combat rejected an attack explicitly authorized by Diplomacy: {hostileAttack.Message}");

        var combatEvents = combat.Advance(galaxy, 1.0);
        Require(combatEvents.Any(evt => evt.Type == CombatEventType.EngagementStarted),
            "authorized hostile encounter did not begin an engagement");
        Require(combatEvents.Any(evt => evt.Type == CombatEventType.FleetDestroyed && evt.TargetCivilizationId == secondCivilization.Id),
            "validation combat did not destroy the weakened target vessel");

        var processed = incidentBridge.Process(combatEvents, tick: 3);
        Require(processed >= 2,
            "Diplomacy did not consume the meaningful engagement/destruction outcomes");
        var afterLoss = state.GetRelationship(firstCivilization.Id, secondCivilization.Id)
            ?? throw new InvalidOperationException("combat consequence lost the diplomatic relationship");
        Require(afterLoss.PoliticalState == DiplomaticPoliticalState.Hostile,
            "Combat incident independently changed political war state");
        Require(afterLoss.Grievances.Any(grievance =>
                grievance.SourceCivilizationId == firstCivilization.Id &&
                Math.Abs(grievance.Severity - 1.0) < 0.000001 &&
                grievance.Reason.Contains("destroyed", StringComparison.OrdinalIgnoreCase)),
            "destroyed vessel did not become a major attributable diplomatic grievance");

        diplomacy.DeclareWar(firstCivilization.Id, secondCivilization.Id, tick: 4);
        Require(hostility.AreHostile(firstCivilization.Id, secondCivilization.Id),
            "AtWar diplomatic state did not authorize Combat");

        var communication = new DiplomaticCommunicationService(state);
        communication.EstablishMutualCommunication(firstCivilization.Id, secondCivilization.Id, tick: 5);
        var ceasefireProposal = diplomacy.SendProposal(
            firstCivilization.Id,
            secondCivilization.Id,
            DiplomaticProposalKind.CeasefireOffer,
            tick: 6,
            summary: "Cease hostile military operations.");
        diplomacy.RespondToProposal(ceasefireProposal, secondCivilization.Id, accept: true, tick: 7);

        Require(state.GetRelationship(firstCivilization.Id, secondCivilization.Id)?.PoliticalState == DiplomaticPoliticalState.Ceasefire,
            "accepted ceasefire did not change diplomatic political state");
        Require(!hostility.AreHostile(firstCivilization.Id, secondCivilization.Id),
            "Combat remained authorized after Diplomacy entered ceasefire");
    }

    private static void ValidateUnattributedCombatCannotLeakIdentity()
    {
        var state = new DiplomacyState();
        var bridge = new CombatDiplomacyBridge(state);
        var eventList = new[]
        {
            new CombatEvent(
                CombatEventType.FleetDestroyed,
                SystemId: 44,
                ActorCivilizationId: 91,
                ActorFleetId: 9001,
                TargetCivilizationId: 92,
                TargetFleetId: 9002,
                ShieldDamage: 0.0,
                ArmorDamage: 0.0,
                HullDamage: 0.0,
                Message: "Unknown attacker destroyed an unprepared vessel."),
        };

        Require(bridge.Process(eventList, tick: 10) == 0,
            "unattributed authoritative Combat identity was converted into diplomatic knowledge");
        Require(state.BuildViewFor(92).Contacts.Count == 0 && state.BuildViewFor(92).Relationships.Count == 0,
            "Combat incident leaked hidden attacker identity into victim diplomacy view");
    }

    private static void EstablishIdentifiedContact(
        DiplomacySimulation diplomacy,
        int observer,
        int target,
        string contactId,
        long tick,
        int systemId)
    {
        diplomacy.ProcessContactOpportunity(new FirstContactOpportunity(
            observer,
            contactId,
            target,
            tick,
            systemId,
            ContactAwareness.ContactEstablished,
            ContactCondition.Active,
            CommunicationAvailable: false,
            Confidence: 1.0));
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

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

using System.Numerics;
using Game.Simulation.Combat;
using Game.Simulation.Diplomacy;
using Game.Simulation.Generation;
using Game.Simulation.Models;

namespace Game.Simulation.Validation;

internal static class DiplomacyCombatValidation
{
    public static void ValidatePoliticalStateControlsCombat()
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x4449_504C_4F43_4CL,
            new GalaxyGenerationSettings
            {
                SystemCount = 24,
                PreWarpCivilizationCount = 3,
                AncientCivilizationCount = 0,
                Radius = 320.0f,
            });

        galaxy.Fleets.Clear();
        var firstCivilization = galaxy.Civilizations[0];
        var secondCivilization = galaxy.Civilizations[1];
        var system = galaxy.Systems[0];
        var attacker = CreatePatrol(8100, firstCivilization.Id, "Diplomatic Sentinel", system.Id, system.Position);
        var target = CreatePatrol(8101, secondCivilization.Id, "Diplomatic Rival", system.Id, system.Position);
        galaxy.Fleets.Add(attacker);
        galaxy.Fleets.Add(target);

        var diplomacyState = new DiplomacyState();
        var diplomacy = new DiplomacySimulation(diplomacyState);
        EstablishMutualCommunication(diplomacy, firstCivilization.Id, secondCivilization.Id, system.Id);

        var hostility = new DiplomacyCombatHostilityView(diplomacyState);
        var combat = new CombatSimulation(hostility);

        Require(!hostility.AreHostile(firstCivilization.Id, secondCivilization.Id),
            "newly established peaceful relationship was treated as combat-hostile");
        Require(!hostility.AreHostile(secondCivilization.Id, firstCivilization.Id),
            "peaceful relationship was directionally inconsistent");

        var peacefulOrder = combat.IssueOrder(
            galaxy,
            firstCivilization.Id,
            attacker.Id,
            new MilitaryOrder(MilitaryOrderType.Attack, target.Id));
        Require(!peacefulOrder.Accepted, "Combat accepted an attack during diplomatic Peace");

        diplomacy.SetHostile(firstCivilization.Id, secondCivilization.Id, tick: 2, reason: "Escalating armed border confrontation.");
        Require(hostility.AreHostile(firstCivilization.Id, secondCivilization.Id),
            "Diplomatic Hostile state did not permit a limited hostile engagement");
        Require(hostility.AreHostile(secondCivilization.Id, firstCivilization.Id),
            "bilateral Hostile state was not visible in both Combat directions");

        var hostileOrder = combat.IssueOrder(
            galaxy,
            firstCivilization.Id,
            attacker.Id,
            new MilitaryOrder(MilitaryOrderType.Attack, target.Id));
        Require(hostileOrder.Accepted, $"Combat rejected an attack during diplomatic Hostile state: {hostileOrder.Message}");

        var targetState = CombatProfileRegistry.EnsureState(target);
        var defensesBeforeCeasefire = (targetState.Shields, targetState.Armor, targetState.Hull);
        var ceasefireProposal = diplomacy.SendProposal(
            firstCivilization.Id,
            secondCivilization.Id,
            DiplomaticProposalKind.CeasefireOffer,
            tick: 3,
            summary: "Immediate tactical ceasefire.");
        diplomacy.RespondToProposal(ceasefireProposal, secondCivilization.Id, accept: true, tick: 4);

        Require(diplomacyState.GetRelationship(firstCivilization.Id, secondCivilization.Id)?.PoliticalState == DiplomaticPoliticalState.Ceasefire,
            "accepted ceasefire offer did not update Diplomacy political state");
        Require(!hostility.AreHostile(firstCivilization.Id, secondCivilization.Id),
            "Combat continued to treat a diplomatic Ceasefire as hostile");

        var ceasefireEvents = combat.Advance(galaxy, 0.25);
        var defensesAfterCeasefire = CombatProfileRegistry.EnsureState(target);
        Require(ceasefireEvents.All(evt => evt.Type != CombatEventType.DamageApplied),
            "an already-issued attack produced damage after a ceasefire became authoritative");
        Require(defensesBeforeCeasefire == (defensesAfterCeasefire.Shields, defensesAfterCeasefire.Armor, defensesAfterCeasefire.Hull),
            "target defenses changed after Diplomacy entered Ceasefire");
        Require(CombatProfileRegistry.EnsureState(attacker).Order == MilitaryOrderType.Hold,
            "Combat did not clear a stale attack order after ceasefire removed political hostility");

        diplomacy.DeclareWar(firstCivilization.Id, secondCivilization.Id, tick: 5);
        Require(hostility.AreHostile(firstCivilization.Id, secondCivilization.Id),
            "Diplomatic AtWar state did not permit Combat");
        Require(hostility.AreHostile(secondCivilization.Id, firstCivilization.Id),
            "bilateral AtWar state was not visible in both Combat directions");

        var warOrder = combat.IssueOrder(
            galaxy,
            firstCivilization.Id,
            attacker.Id,
            new MilitaryOrder(MilitaryOrderType.Attack, target.Id));
        Require(warOrder.Accepted, $"Combat rejected an attack during diplomatic AtWar state: {warOrder.Message}");
    }

    private static void EstablishMutualCommunication(
        DiplomacySimulation diplomacy,
        int firstCivilizationId,
        int secondCivilizationId,
        int systemId)
    {
        diplomacy.ProcessContactOpportunity(new FirstContactOpportunity(
            firstCivilizationId,
            $"combat-contact-{secondCivilizationId}",
            secondCivilizationId,
            ObservedAtTick: 1,
            ObservedSystemId: systemId,
            Awareness: ContactAwareness.CommunicationAvailable,
            Condition: ContactCondition.Active,
            CommunicationAvailable: true,
            Confidence: 1.0));

        diplomacy.ProcessContactOpportunity(new FirstContactOpportunity(
            secondCivilizationId,
            $"combat-contact-{firstCivilizationId}",
            firstCivilizationId,
            ObservedAtTick: 1,
            ObservedSystemId: systemId,
            Awareness: ContactAwareness.CommunicationAvailable,
            Condition: ContactCondition.Active,
            CommunicationAvailable: true,
            Confidence: 1.0));
    }

    private static FleetState CreatePatrol(
        int id,
        int civilizationId,
        string name,
        int systemId,
        Vector2 position) => new()
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

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Simulation.Diplomacy;

namespace Game.CoreRuntime.Validation;

internal static class ObserverDiplomacyActionReadModelValidation
{
    [ModuleInitializer]
    internal static void RunObserverDiplomacyActionReadModelChecks()
    {
        ValidateObserverScopedActionAvailability();
        Console.WriteLine("PASS: observer-safe Diplomacy action availability read model");
    }

    private static void ValidateObserverScopedActionAvailability()
    {
        var state = new DiplomacyState();
        EstablishCommunication(state, 1, 2, tick: 0, systemId: 10);
        EstablishCommunication(state, 3, 4, tick: 0, systemId: 20);
        var diplomacy = new DiplomacySimulation(state);
        diplomacy.ProcessContactOpportunity(new FirstContactOpportunity(
            ObserverCivilizationId: 1,
            ContactId: "known-silent-5",
            TargetCivilizationId: 5,
            ObservedAtTick: 1,
            ObservedSystemId: 30,
            Awareness: ContactAwareness.ContactEstablished,
            Condition: ContactCondition.Active,
            CommunicationAvailable: false,
            Confidence: 0.9));

        var one = new ObserverDiplomacyCommandService(state);
        var two = new ObserverDiplomacyCommandService(state);
        var three = new ObserverDiplomacyCommandService(state);
        var four = new ObserverDiplomacyCommandService(state);

        var agreementProposal = one.SendProposal(
            1,
            2,
            DiplomaticProposalKind.Agreement,
            tick: 10,
            summary: "Mutual access agreement.",
            agreementType: DiplomaticAgreementType.Access);
        Require(agreementProposal.Accepted && agreementProposal.ProposalId is > 0,
            "validation Access agreement proposal was not created");
        Require(two.RespondToProposal(2, agreementProposal.ProposalId!.Value, accept: true, tick: 11).Accepted,
            "validation Access agreement proposal was not accepted");

        var outgoing = one.SendProposal(
            1,
            2,
            DiplomaticProposalKind.Demand,
            tick: 12,
            summary: "Visible outgoing pending demand.");
        var incoming = two.SendProposal(
            2,
            1,
            DiplomaticProposalKind.AccessRequest,
            tick: 13,
            summary: "Visible incoming pending access request.");
        Require(outgoing.Accepted && outgoing.ProposalId is > 0 && incoming.Accepted && incoming.ProposalId is > 0,
            "validation visible pending proposals were not created");

        var hidden = three.SendProposal(
            3,
            4,
            DiplomaticProposalKind.Demand,
            tick: 14,
            summary: "Hidden third-party proposal.");
        Require(hidden.Accepted && hidden.ProposalId is > 0, "validation hidden proposal was not created");

        var model = new ObserverDiplomacyActionReadModel(state);
        var first = model.Build(1);

        Require(first.Counterparts.Select(counterpart => counterpart.TargetCivilizationId).SequenceEqual(new[] { 2, 5 }),
            "observer action view leaked hidden counterpart or lost visible counterpart");
        Require(!first.Counterparts.Any(counterpart => counterpart.TargetCivilizationId is 3 or 4),
            "third-party civilization leaked into observer action counterpart list");

        var communicating = first.Counterparts.Single(counterpart => counterpart.TargetCivilizationId == 2);
        Require(communicating.CommunicationAvailable && communicating.CanSendProposal && communicating.CanSetAccessPermission,
            "active communicated counterpart did not expose enabled proposal/access actions");
        Require(communicating.PendingIncomingProposalCount == 1 && communicating.PendingOutgoingProposalCount == 1,
            "visible counterpart pending proposal counts were incorrect");
        Require(communicating.ActiveAgreementCount == 1,
            "visible counterpart active agreement count was incorrect");
        Require(communicating.AccessGrantedByObserver == AccessPermission.Granted &&
                communicating.AccessGrantedToObserver == AccessPermission.Granted,
            "observer action view did not expose visible mutual Access agreement permissions");

        var silent = first.Counterparts.Single(counterpart => counterpart.TargetCivilizationId == 5);
        Require(!silent.CommunicationAvailable && !silent.CanSendProposal && !silent.CanSetAccessPermission,
            "identified-but-silent counterpart incorrectly exposed active command capability");

        Require(first.PendingProposals.Count == 2,
            "observer action view did not contain exactly its two visible pending proposals");
        Require(!first.PendingProposals.Any(proposal => proposal.ProposalId == hidden.ProposalId!.Value),
            "hidden third-party proposal leaked into observer action view");

        var outgoingActions = first.PendingProposals.Single(proposal => proposal.ProposalId == outgoing.ProposalId!.Value);
        Require(outgoingActions.IsOutgoing && !outgoingActions.IsIncoming &&
                outgoingActions.CanWithdraw && !outgoingActions.CanRespond,
            "outgoing proposal action capabilities were incorrect");
        var incomingActions = first.PendingProposals.Single(proposal => proposal.ProposalId == incoming.ProposalId!.Value);
        Require(incomingActions.IsIncoming && !incomingActions.IsOutgoing &&
                incomingActions.CanRespond && !incomingActions.CanWithdraw,
            "incoming proposal action capabilities were incorrect while channel was active");

        var agreement = first.Agreements.Single(candidate => candidate.Type == DiplomaticAgreementType.Access);
        Require(agreement.Status == DiplomaticAgreementStatus.Active && agreement.CanTerminate,
            "active visible agreement did not expose termination while channel was active");

        diplomacy.MarkContactLost(1, "1-2", tick: 20);
        var stale = model.Build(1);
        var staleCounterpart = stale.Counterparts.Single(counterpart => counterpart.TargetCivilizationId == 2);
        Require(!staleCounterpart.CommunicationAvailable &&
                !staleCounterpart.CanSendProposal &&
                !staleCounterpart.CanSetAccessPermission,
            "stale observer channel left counterpart communication actions enabled");

        var staleIncoming = stale.PendingProposals.Single(proposal => proposal.ProposalId == incoming.ProposalId.Value);
        var staleOutgoing = stale.PendingProposals.Single(proposal => proposal.ProposalId == outgoing.ProposalId.Value);
        Require(!staleIncoming.CanRespond,
            "incoming proposal remained respondable after observer communication became stale");
        Require(staleOutgoing.CanWithdraw,
            "outgoing proposal withdrawal incorrectly depended on active communication");
        Require(!stale.Agreements.Single(candidate => candidate.AgreementId == agreement.AgreementId).CanTerminate,
            "active agreement remained terminable after observer communication became stale");
    }

    private static void EstablishCommunication(
        DiplomacyState state,
        int civilizationA,
        int civilizationB,
        long tick,
        int systemId)
    {
        var diplomacy = new DiplomacySimulation(state);
        diplomacy.ProcessContactOpportunity(new FirstContactOpportunity(
            civilizationA,
            $"{civilizationA}-{civilizationB}",
            civilizationB,
            tick,
            systemId,
            ContactAwareness.CommunicationAvailable,
            ContactCondition.Active,
            CommunicationAvailable: true,
            Confidence: 1.0));
        diplomacy.ProcessContactOpportunity(new FirstContactOpportunity(
            civilizationB,
            $"{civilizationB}-{civilizationA}",
            civilizationA,
            tick,
            systemId,
            ContactAwareness.CommunicationAvailable,
            ContactCondition.Active,
            CommunicationAvailable: true,
            Confidence: 1.0));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

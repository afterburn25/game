using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Simulation.Diplomacy;

public sealed record ObserverDiplomacyCounterpartActions(
    int TargetCivilizationId,
    ContactAwareness Awareness,
    ContactCondition Condition,
    bool CommunicationAvailable,
    DiplomaticPoliticalState? PoliticalState,
    AccessPermission AccessGrantedByObserver,
    AccessPermission AccessGrantedToObserver,
    int PendingIncomingProposalCount,
    int PendingOutgoingProposalCount,
    int ActiveAgreementCount)
{
    public bool CanSendProposal => CommunicationAvailable;
    public bool CanSetAccessPermission => CommunicationAvailable;
}

public sealed record ObserverDiplomacyProposalActions(
    long ProposalId,
    int CounterpartCivilizationId,
    DiplomaticProposalKind Kind,
    DiplomaticAgreementType? AgreementType,
    bool IsIncoming,
    bool IsOutgoing,
    bool CanRespond,
    bool CanWithdraw);

public sealed record ObserverDiplomacyAgreementActions(
    long AgreementId,
    int CounterpartCivilizationId,
    DiplomaticAgreementType Type,
    DiplomaticAgreementStatus Status,
    bool CanTerminate);

public sealed record ObserverDiplomacyActionView(
    int ObserverCivilizationId,
    IReadOnlyList<ObserverDiplomacyCounterpartActions> Counterparts,
    IReadOnlyList<ObserverDiplomacyProposalActions> PendingProposals,
    IReadOnlyList<ObserverDiplomacyAgreementActions> Agreements);

/// <summary>
/// Derives UI/AI action availability strictly from one observer's already-filtered Diplomacy
/// view. It never queries hidden contacts/proposals/agreements directly and never guesses whether
/// a command would succeed from omniscient state. Consumers can therefore disable unavailable
/// controls without turning rejected commands into an information-probing oracle.
/// </summary>
public sealed class ObserverDiplomacyActionReadModel
{
    private readonly DiplomacyState _state;

    public ObserverDiplomacyActionReadModel(DiplomacyState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public ObserverDiplomacyActionView Build(int observerCivilizationId)
    {
        if (observerCivilizationId < 0)
            throw new ArgumentOutOfRangeException(nameof(observerCivilizationId));

        var view = _state.BuildViewFor(observerCivilizationId);
        var identifiedContacts = view.Contacts
            .Where(contact =>
                contact.TargetCivilizationId is not null &&
                contact.Awareness >= ContactAwareness.Identified)
            .GroupBy(contact => contact.TargetCivilizationId!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var counterpartIds = identifiedContacts.Keys.OrderBy(id => id).ToArray();
        var counterparts = counterpartIds
            .Select(target => BuildCounterpart(view, observerCivilizationId, target, identifiedContacts[target]))
            .ToArray();
        var communicationByTarget = counterparts.ToDictionary(
            counterpart => counterpart.TargetCivilizationId,
            counterpart => counterpart.CommunicationAvailable);

        var pending = view.Proposals
            .Where(proposal => proposal.Status == DiplomaticProposalStatus.Pending)
            .OrderBy(proposal => proposal.ProposalId)
            .Select(proposal =>
            {
                var incoming = proposal.RecipientCivilizationId == observerCivilizationId;
                var outgoing = proposal.ProposerCivilizationId == observerCivilizationId;
                var counterpart = incoming
                    ? proposal.ProposerCivilizationId
                    : proposal.RecipientCivilizationId;
                var hasChannel = communicationByTarget.TryGetValue(counterpart, out var available) && available;
                return new ObserverDiplomacyProposalActions(
                    proposal.ProposalId,
                    counterpart,
                    proposal.Kind,
                    proposal.AgreementType,
                    incoming,
                    outgoing,
                    CanRespond: incoming && hasChannel,
                    CanWithdraw: outgoing);
            })
            .ToArray();

        var agreements = view.Agreements
            .OrderBy(agreement => agreement.AgreementId)
            .Select(agreement =>
            {
                var counterpart = agreement.CivilizationAId == observerCivilizationId
                    ? agreement.CivilizationBId
                    : agreement.CivilizationAId;
                var hasChannel = communicationByTarget.TryGetValue(counterpart, out var available) && available;
                return new ObserverDiplomacyAgreementActions(
                    agreement.AgreementId,
                    counterpart,
                    agreement.Type,
                    agreement.Status,
                    CanTerminate: agreement.Status == DiplomaticAgreementStatus.Active && hasChannel);
            })
            .ToArray();

        return new ObserverDiplomacyActionView(
            observerCivilizationId,
            counterparts,
            pending,
            agreements);
    }

    private static ObserverDiplomacyCounterpartActions BuildCounterpart(
        DiplomaticStateView view,
        int observerCivilizationId,
        int targetCivilizationId,
        IReadOnlyList<DiplomaticContactView> contacts)
    {
        var latest = contacts
            .OrderByDescending(contact => contact.LastObservedTick)
            .ThenByDescending(contact => contact.Awareness)
            .ThenBy(contact => contact.ContactId, StringComparer.Ordinal)
            .First();
        var communicationAvailable = contacts.Any(contact =>
            contact.CommunicationAvailable &&
            contact.Condition != ContactCondition.StaleOrLost);

        var relationship = view.Relationships.FirstOrDefault(candidate =>
            candidate.OtherCivilizationId == targetCivilizationId);
        var observerGrant = view.AccessPermissions.FirstOrDefault(access =>
            access.GrantorCivilizationId == observerCivilizationId &&
            access.VisitorCivilizationId == targetCivilizationId)?.Permission ?? AccessPermission.Unspecified;
        var targetGrant = view.AccessPermissions.FirstOrDefault(access =>
            access.GrantorCivilizationId == targetCivilizationId &&
            access.VisitorCivilizationId == observerCivilizationId)?.Permission ?? AccessPermission.Unspecified;
        var pendingIncoming = view.Proposals.Count(proposal =>
            proposal.Status == DiplomaticProposalStatus.Pending &&
            proposal.ProposerCivilizationId == targetCivilizationId &&
            proposal.RecipientCivilizationId == observerCivilizationId);
        var pendingOutgoing = view.Proposals.Count(proposal =>
            proposal.Status == DiplomaticProposalStatus.Pending &&
            proposal.ProposerCivilizationId == observerCivilizationId &&
            proposal.RecipientCivilizationId == targetCivilizationId);
        var activeAgreements = view.Agreements.Count(agreement =>
            agreement.Status == DiplomaticAgreementStatus.Active &&
            (agreement.CivilizationAId == targetCivilizationId || agreement.CivilizationBId == targetCivilizationId));

        return new ObserverDiplomacyCounterpartActions(
            targetCivilizationId,
            latest.Awareness,
            communicationAvailable ? ContactCondition.Active : latest.Condition,
            communicationAvailable,
            relationship?.PoliticalState,
            observerGrant,
            targetGrant,
            pendingIncoming,
            pendingOutgoing,
            activeAgreements);
    }
}

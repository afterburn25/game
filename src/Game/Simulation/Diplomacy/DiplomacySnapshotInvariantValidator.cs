using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Simulation.Diplomacy;

public sealed record DiplomacySnapshotValidationResult(
    int ContactCount,
    int RelationshipCount,
    int AccessPermissionCount,
    int ClaimCount,
    int ClaimResponseCount,
    int AgreementCount,
    int ProposalCount,
    int HistoryEventCount);

public sealed class DiplomacySnapshotValidationException : InvalidOperationException
{
    public DiplomacySnapshotValidationException(string message) : base(message) { }
}

/// <summary>
/// Strict validation for a current, persistence-ready Diplomacy snapshot.
///
/// DiplomacyState.Restore remains intentionally tolerant for migration/recovery: it can clamp or
/// discard malformed legacy data. This validator serves the opposite boundary. Before a current
/// snapshot is written into an authoritative campaign save, it should already satisfy every
/// structural invariant and must not rely on Restore to repair impossible state.
/// </summary>
public static class DiplomacySnapshotInvariantValidator
{
    public static DiplomacySnapshotValidationResult Validate(DiplomacyStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var contacts = RequireArray(snapshot.Contacts, nameof(snapshot.Contacts));
        var relationships = RequireArray(snapshot.Relationships, nameof(snapshot.Relationships));
        var accessPermissions = RequireArray(snapshot.AccessPermissions, nameof(snapshot.AccessPermissions));
        var claims = RequireArray(snapshot.Claims, nameof(snapshot.Claims));
        var claimResponses = RequireArray(snapshot.ClaimResponses, nameof(snapshot.ClaimResponses));
        var agreements = RequireArray(snapshot.Agreements, nameof(snapshot.Agreements));
        var proposals = RequireArray(snapshot.Proposals, nameof(snapshot.Proposals));
        var history = RequireArray(snapshot.RecentHistory, nameof(snapshot.RecentHistory));

        if (contacts.Length > DiplomacyState.MaxContactRecords)
            Fail($"Contact count {contacts.Length} exceeds the bounded limit {DiplomacyState.MaxContactRecords}.");
        if (proposals.Length > DiplomacyState.MaxStoredProposals)
            Fail($"Proposal count {proposals.Length} exceeds the bounded limit {DiplomacyState.MaxStoredProposals}.");
        if (history.Length > DiplomacyState.MaxRecentHistoryEvents)
            Fail($"History count {history.Length} exceeds the bounded limit {DiplomacyState.MaxRecentHistoryEvents}.");

        var contactKeys = new HashSet<(int Observer, string ContactId)>();
        var identifiedPairs = new HashSet<(int Observer, int Target)>();
        foreach (var contact in contacts)
        {
            RequireCivilization(contact.ObserverCivilizationId, "contact observer");
            if (string.IsNullOrWhiteSpace(contact.ContactId))
                Fail("Diplomatic contact ID cannot be blank.");
            if (!contactKeys.Add((contact.ObserverCivilizationId, contact.ContactId)))
                Fail($"Duplicate contact key {contact.ObserverCivilizationId}:{contact.ContactId}.");
            if (contact.TargetCivilizationId == contact.ObserverCivilizationId)
                Fail($"Contact {contact.ContactId} targets its own observer civilization.");
            if (contact.TargetCivilizationId is int target)
                RequireCivilization(target, "contact target");
            if (contact.FirstObservedTick < 0 || contact.LastObservedTick < contact.FirstObservedTick)
                Fail($"Contact {contact.ContactId} has invalid observation chronology.");
            if (contact.LastObservedSystemId is < 0)
                Fail($"Contact {contact.ContactId} has a negative observed system ID.");
            RequireEnum(contact.Awareness, $"contact {contact.ContactId} awareness");
            RequireEnum(contact.Condition, $"contact {contact.ContactId} condition");
            if (contact.Awareness == ContactAwareness.Unknown)
                Fail($"Stored contact {contact.ContactId} cannot have Unknown awareness.");
            if (!double.IsFinite(contact.Confidence) || contact.Confidence is < 0.0 or > 1.0)
                Fail($"Contact {contact.ContactId} confidence must be finite and within [0,1].");
            if (contact.Awareness >= ContactAwareness.Identified && contact.TargetCivilizationId is null)
                Fail($"Identified contact {contact.ContactId} has no target civilization.");
            if (contact.Awareness == ContactAwareness.DetectedUnidentified && contact.TargetCivilizationId is not null)
                Fail($"Unidentified contact {contact.ContactId} exposes a target civilization.");
            if (contact.CommunicationAvailable && contact.TargetCivilizationId is null)
                Fail($"Communicating contact {contact.ContactId} has no identified target.");
            if (contact.CommunicationAvailable && contact.Awareness < ContactAwareness.ContactPossible)
                Fail($"Communicating contact {contact.ContactId} has insufficient awareness.");
            if (contact.CommunicationAvailable && contact.Condition == ContactCondition.StaleOrLost)
                Fail($"Stale/lost contact {contact.ContactId} cannot have active communication.");

            if (contact.TargetCivilizationId is int identifiedTarget && contact.Awareness >= ContactAwareness.Identified)
                identifiedPairs.Add((contact.ObserverCivilizationId, identifiedTarget));
        }

        var relationshipPairs = new HashSet<(int First, int Second)>();
        foreach (var relationship in relationships)
        {
            RequireCivilization(relationship.CivilizationAId, "relationship civilization A");
            RequireCivilization(relationship.CivilizationBId, "relationship civilization B");
            if (relationship.CivilizationAId >= relationship.CivilizationBId)
                Fail($"Relationship pair {relationship.CivilizationAId}/{relationship.CivilizationBId} is not canonical.");
            var pair = (relationship.CivilizationAId, relationship.CivilizationBId);
            if (!relationshipPairs.Add(pair))
                Fail($"Duplicate relationship pair {pair.Item1}/{pair.Item2}.");
            if (!identifiedPairs.Contains((pair.Item1, pair.Item2)) && !identifiedPairs.Contains((pair.Item2, pair.Item1)))
                Fail($"Relationship {pair.Item1}/{pair.Item2} has no legitimate identified-contact basis.");
            RequireEnum(relationship.PoliticalState, $"relationship {pair.Item1}/{pair.Item2} political state");
            RequireUnit(relationship.Trust, "trust", pair);
            RequireUnit(relationship.Hostility, "hostility", pair);
            RequireUnit(relationship.Fear, "fear", pair);
            RequireUnit(relationship.Respect, "respect", pair);
            RequireUnit(relationship.Cooperation, "cooperation", pair);

            var grievances = RequireArray(relationship.Grievances, $"relationship {pair.Item1}/{pair.Item2} grievances");
            if (grievances.Length > RelationshipState.MaxGrievances)
                Fail($"Relationship {pair.Item1}/{pair.Item2} exceeds the bounded grievance limit {RelationshipState.MaxGrievances}.");
            foreach (var grievance in grievances)
            {
                if (grievance.CreatedAtTick < 0)
                    Fail($"Relationship {pair.Item1}/{pair.Item2} has a grievance with a negative creation tick.");
                if (grievance.SourceCivilizationId != pair.Item1 && grievance.SourceCivilizationId != pair.Item2)
                    Fail($"Relationship {pair.Item1}/{pair.Item2} has a grievance sourced from an unrelated civilization.");
                if (!double.IsFinite(grievance.Severity) || grievance.Severity is < 0.0 or > 1.0)
                    Fail($"Relationship {pair.Item1}/{pair.Item2} has an invalid grievance severity.");
                if (string.IsNullOrWhiteSpace(grievance.Reason))
                    Fail($"Relationship {pair.Item1}/{pair.Item2} has a grievance without a reason.");
            }
        }

        var accessKeys = new HashSet<(int Grantor, int Visitor)>();
        foreach (var access in accessPermissions)
        {
            RequireCivilization(access.GrantorCivilizationId, "access grantor");
            RequireCivilization(access.VisitorCivilizationId, "access visitor");
            if (access.GrantorCivilizationId == access.VisitorCivilizationId)
                Fail("Access permission cannot target the same civilization as its grantor.");
            if (!accessKeys.Add((access.GrantorCivilizationId, access.VisitorCivilizationId)))
                Fail($"Duplicate access permission {access.GrantorCivilizationId}->{access.VisitorCivilizationId}.");
            RequireEnum(access.Permission, "access permission");
            if (access.UpdatedAtTick < 0)
                Fail("Access permission has a negative update tick.");
            var pair = Canonical(access.GrantorCivilizationId, access.VisitorCivilizationId);
            if (!relationshipPairs.Contains(pair))
                Fail($"Access permission {access.GrantorCivilizationId}->{access.VisitorCivilizationId} has no diplomatic relationship.");
        }

        var claimIds = new HashSet<long>();
        var claimById = new Dictionary<long, TerritorialClaimSnapshot>();
        foreach (var claim in claims)
        {
            if (claim.ClaimId <= 0 || !claimIds.Add(claim.ClaimId))
                Fail($"Territorial claim ID {claim.ClaimId} is invalid or duplicated.");
            RequireCivilization(claim.ClaimantCivilizationId, "claimant civilization");
            if (claim.SystemId < 0 || claim.AssertedAtTick < 0)
                Fail($"Territorial claim {claim.ClaimId} has invalid system/tick data.");
            var knownTo = RequireArray(claim.KnownToCivilizationIds, $"claim {claim.ClaimId} audience");
            if (knownTo.Any(id => id < 0) || knownTo.Distinct().Count() != knownTo.Length)
                Fail($"Territorial claim {claim.ClaimId} has an invalid or duplicate audience.");
            if (!knownTo.Contains(claim.ClaimantCivilizationId))
                Fail($"Territorial claim {claim.ClaimId} is not known to its claimant.");
            if (!knownTo.SequenceEqual(knownTo.OrderBy(id => id)))
                Fail($"Territorial claim {claim.ClaimId} audience is not canonical/sorted.");
            claimById.Add(claim.ClaimId, claim);
        }

        var claimResponseKeys = new HashSet<(long ClaimId, int Responder)>();
        foreach (var response in claimResponses)
        {
            if (!claimById.TryGetValue(response.ClaimId, out var claim))
                Fail($"Claim response references unknown claim {response.ClaimId}.");
            RequireCivilization(response.RespondingCivilizationId, "claim responder");
            if (response.RespondingCivilizationId == claim.ClaimantCivilizationId)
                Fail($"Claim {response.ClaimId} claimant cannot respond to its own claim.");
            if (!claim.KnownToCivilizationIds.Contains(response.RespondingCivilizationId))
                Fail($"Claim response for {response.ClaimId} comes from a civilization that does not know the claim.");
            if (!claimResponseKeys.Add((response.ClaimId, response.RespondingCivilizationId)))
                Fail($"Duplicate response to claim {response.ClaimId} from civilization {response.RespondingCivilizationId}.");
            RequireEnum(response.Response, $"claim {response.ClaimId} response");
            if (response.Response == TerritorialClaimResponse.None)
                Fail($"Stored response to claim {response.ClaimId} cannot be None.");
            if (response.RespondedAtTick < claim.AssertedAtTick)
                Fail($"Response to claim {response.ClaimId} predates the claim.");
        }

        var agreementIds = new HashSet<long>();
        var activeAgreementKeys = new HashSet<(int First, int Second, DiplomaticAgreementType Type)>();
        foreach (var agreement in agreements)
        {
            if (agreement.AgreementId <= 0 || !agreementIds.Add(agreement.AgreementId))
                Fail($"Agreement ID {agreement.AgreementId} is invalid or duplicated.");
            RequireCivilization(agreement.CivilizationAId, "agreement civilization A");
            RequireCivilization(agreement.CivilizationBId, "agreement civilization B");
            if (agreement.CivilizationAId >= agreement.CivilizationBId)
                Fail($"Agreement {agreement.AgreementId} pair is not canonical.");
            var pair = (agreement.CivilizationAId, agreement.CivilizationBId);
            if (!relationshipPairs.Contains(pair))
                Fail($"Agreement {agreement.AgreementId} has no diplomatic relationship.");
            RequireEnum(agreement.Type, $"agreement {agreement.AgreementId} type");
            RequireEnum(agreement.Status, $"agreement {agreement.AgreementId} status");
            if (agreement.StartedAtTick < 0)
                Fail($"Agreement {agreement.AgreementId} has a negative start tick.");
            if (agreement.Status == DiplomaticAgreementStatus.Active && agreement.EndedAtTick is not null)
                Fail($"Active agreement {agreement.AgreementId} has an end tick.");
            if (agreement.Status == DiplomaticAgreementStatus.Terminated &&
                (agreement.EndedAtTick is null || agreement.EndedAtTick < agreement.StartedAtTick))
                Fail($"Terminated agreement {agreement.AgreementId} lacks valid end chronology.");
            if (agreement.Status == DiplomaticAgreementStatus.Active &&
                !activeAgreementKeys.Add((pair.Item1, pair.Item2, agreement.Type)))
                Fail($"Duplicate active {agreement.Type} agreement for pair {pair.Item1}/{pair.Item2}.");
            if (agreement.Type == DiplomaticAgreementType.Trade && string.IsNullOrWhiteSpace(agreement.ExternalTermsReference))
                Fail($"Trade agreement {agreement.AgreementId} has no external economy/logistics terms reference.");
        }

        var proposalIds = new HashSet<long>();
        var pendingByPair = new Dictionary<(int First, int Second), int>();
        foreach (var proposal in proposals)
        {
            if (proposal.ProposalId <= 0 || !proposalIds.Add(proposal.ProposalId))
                Fail($"Proposal ID {proposal.ProposalId} is invalid or duplicated.");
            RequireCivilization(proposal.ProposerCivilizationId, "proposal proposer");
            RequireCivilization(proposal.RecipientCivilizationId, "proposal recipient");
            if (proposal.ProposerCivilizationId == proposal.RecipientCivilizationId)
                Fail($"Proposal {proposal.ProposalId} targets its proposer.");
            var pair = Canonical(proposal.ProposerCivilizationId, proposal.RecipientCivilizationId);
            if (!relationshipPairs.Contains(pair))
                Fail($"Proposal {proposal.ProposalId} has no diplomatic relationship.");
            RequireEnum(proposal.Kind, $"proposal {proposal.ProposalId} kind");
            RequireEnum(proposal.Status, $"proposal {proposal.ProposalId} status");
            if (proposal.AgreementType is DiplomaticAgreementType agreementType)
                RequireEnum(agreementType, $"proposal {proposal.ProposalId} agreement type");
            if ((proposal.Kind == DiplomaticProposalKind.Agreement) != proposal.AgreementType.HasValue)
                Fail($"Proposal {proposal.ProposalId} has inconsistent agreement-type data.");
            if (proposal.Kind == DiplomaticProposalKind.TradeOffer && string.IsNullOrWhiteSpace(proposal.ExternalTermsReference))
                Fail($"Trade proposal {proposal.ProposalId} has no external economy/logistics terms reference.");
            if (proposal.CreatedAtTick < 0 || string.IsNullOrWhiteSpace(proposal.Summary))
                Fail($"Proposal {proposal.ProposalId} has invalid creation data.");
            if (proposal.Status == DiplomaticProposalStatus.Pending)
            {
                if (proposal.ResolvedAtTick is not null)
                    Fail($"Pending proposal {proposal.ProposalId} already has a resolution tick.");
                pendingByPair[pair] = pendingByPair.GetValueOrDefault(pair) + 1;
                if (pendingByPair[pair] > DiplomacyState.MaxPendingProposalsPerPair)
                    Fail($"Pair {pair.First}/{pair.Second} exceeds the pending-proposal bound.");
            }
            else if (proposal.ResolvedAtTick is null || proposal.ResolvedAtTick < proposal.CreatedAtTick)
            {
                Fail($"Resolved proposal {proposal.ProposalId} lacks valid resolution chronology.");
            }
        }

        var historyIds = new HashSet<long>();
        long previousEventId = 0;
        foreach (var historyEvent in history)
        {
            if (historyEvent.EventId <= 0 || !historyIds.Add(historyEvent.EventId))
                Fail($"History event ID {historyEvent.EventId} is invalid or duplicated.");
            if (historyEvent.EventId <= previousEventId)
                Fail("Diplomatic history event IDs are not in canonical insertion order.");
            previousEventId = historyEvent.EventId;
            if (historyEvent.Tick < 0 || string.IsNullOrWhiteSpace(historyEvent.Summary))
                Fail($"History event {historyEvent.EventId} has invalid tick/summary data.");
            RequireEnum(historyEvent.Kind, $"history event {historyEvent.EventId} kind");
            RequireCivilization(historyEvent.PrimaryCivilizationId, "history primary civilization");
            if (historyEvent.SecondaryCivilizationId is int secondary)
            {
                RequireCivilization(secondary, "history secondary civilization");
                if (secondary == historyEvent.PrimaryCivilizationId)
                    Fail($"History event {historyEvent.EventId} has the same primary and secondary civilization.");
            }
            if (historyEvent.SystemId is < 0)
                Fail($"History event {historyEvent.EventId} has a negative system ID.");
            var audience = RequireArray(historyEvent.KnownToCivilizationIds, $"history event {historyEvent.EventId} audience");
            if (audience.Any(id => id < 0) || audience.Distinct().Count() != audience.Length)
                Fail($"History event {historyEvent.EventId} has an invalid or duplicate audience.");
            if (!audience.Contains(historyEvent.PrimaryCivilizationId))
                Fail($"History event {historyEvent.EventId} is not known to its primary civilization.");
            if (!audience.SequenceEqual(audience.OrderBy(id => id)))
                Fail($"History event {historyEvent.EventId} audience is not canonical/sorted.");
        }

        RequireNextId(snapshot.NextClaimId, claimIds.DefaultIfEmpty(0).Max(), "claim");
        RequireNextId(snapshot.NextAgreementId, agreementIds.DefaultIfEmpty(0).Max(), "agreement");
        RequireNextId(snapshot.NextProposalId, proposalIds.DefaultIfEmpty(0).Max(), "proposal");
        RequireNextId(snapshot.NextEventId, historyIds.DefaultIfEmpty(0).Max(), "history event");

        return new DiplomacySnapshotValidationResult(
            contacts.Length,
            relationships.Length,
            accessPermissions.Length,
            claims.Length,
            claimResponses.Length,
            agreements.Length,
            proposals.Length,
            history.Length);
    }

    private static T[] RequireArray<T>(T[]? values, string label) =>
        values ?? throw new DiplomacySnapshotValidationException($"{label} cannot be null in a current Diplomacy snapshot.");

    private static void RequireCivilization(int civilizationId, string label)
    {
        if (civilizationId < 0)
            Fail($"{label} cannot be negative.");
    }

    private static void RequireEnum<T>(T value, string label) where T : struct, Enum
    {
        if (!Enum.IsDefined(typeof(T), value))
            Fail($"{label} contains undefined enum value {value}.");
    }

    private static void RequireUnit(double value, string label, (int First, int Second) pair)
    {
        if (!double.IsFinite(value) || value is < 0.0 or > 1.0)
            Fail($"Relationship {pair.First}/{pair.Second} {label} must be finite and within [0,1].");
    }

    private static (int First, int Second) Canonical(int first, int second) =>
        first < second ? (first, second) : (second, first);

    private static void RequireNextId(long nextId, long currentMax, string label)
    {
        if (nextId <= 0 || nextId <= currentMax)
            Fail($"Next {label} ID {nextId} does not advance beyond current maximum {currentMax}.");
    }

    private static void Fail(string message) => throw new DiplomacySnapshotValidationException(message);
}

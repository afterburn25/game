using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Simulation.Diplomacy;

public enum ContactAwareness { Unknown, DetectedUnidentified, Identified, ContactPossible, ContactEstablished, CommunicationAvailable }
public enum ContactCondition { Active, Hostile, StaleOrLost }
public enum DiplomaticPoliticalState { Unknown, Peace, Hostile, AtWar, Ceasefire }
public enum AccessPermission { Unspecified, Granted, Denied }
public enum DiplomaticAgreementType { Peace, NonAggression, Access, Trade, ResearchExchange, Ceasefire, Cooperation }
public enum DiplomaticAgreementStatus { Active, Terminated }
public enum DiplomaticProposalKind { Agreement, AccessRequest, TradeOffer, Demand, PeaceOffer, CeasefireOffer }
public enum DiplomaticProposalStatus { Pending, Accepted, Rejected, Withdrawn, Expired }
public enum TerritorialClaimResponse { None, Recognized, Disputed }
public enum DiplomaticEventKind
{
    ContactObserved, ContactEstablished, CommunicationAvailable, ContactLost,
    AccessChanged, ClaimAsserted, ClaimCommunicated, ClaimResponded,
    BorderWarningIssued, TrespassRecorded, ProposalSent, ProposalAccepted,
    ProposalRejected, ProposalWithdrawn, ProposalExpired, AgreementActivated,
    AgreementTerminated, RelationshipChanged, WarDeclared,
}

/// <summary>
/// Exploration-owned handoff into diplomacy. ContactId is observer-local. TargetCivilizationId
/// stays null until the observer legitimately identifies the civilization; no hidden target state
/// belongs in this contract.
/// </summary>
public sealed record FirstContactOpportunity(
    int ObserverCivilizationId, string ContactId, int? TargetCivilizationId,
    long ObservedAtTick, int? ObservedSystemId, ContactAwareness Awareness,
    ContactCondition Condition, bool CommunicationAvailable, double Confidence)
{
    public void Validate()
    {
        if (ObserverCivilizationId < 0) throw new ArgumentOutOfRangeException(nameof(ObserverCivilizationId));
        if (string.IsNullOrWhiteSpace(ContactId)) throw new ArgumentException("ContactId is required.", nameof(ContactId));
        if (ObservedAtTick < 0) throw new ArgumentOutOfRangeException(nameof(ObservedAtTick));
        if (!double.IsFinite(Confidence) || Confidence is < 0.0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(Confidence));
        if (Awareness == ContactAwareness.Unknown) throw new ArgumentException("An opportunity requires an observation.", nameof(Awareness));
        if (TargetCivilizationId == ObserverCivilizationId) throw new ArgumentException("Foreign contact cannot target self.", nameof(TargetCivilizationId));
        if (Awareness >= ContactAwareness.Identified && TargetCivilizationId is null)
            throw new ArgumentException("Identified contact requires target civilization ID.", nameof(TargetCivilizationId));
        if (Awareness == ContactAwareness.DetectedUnidentified && TargetCivilizationId is not null)
            throw new ArgumentException("Unidentified contact cannot expose target civilization ID.", nameof(TargetCivilizationId));
        if (CommunicationAvailable && (Awareness < ContactAwareness.ContactPossible || TargetCivilizationId is null))
            throw new ArgumentException("Communication requires identified contact capability.", nameof(CommunicationAvailable));
        if (Awareness == ContactAwareness.CommunicationAvailable && !CommunicationAvailable)
            throw new ArgumentException("CommunicationAvailable awareness requires communication capability.", nameof(CommunicationAvailable));
        if (Condition == ContactCondition.StaleOrLost && CommunicationAvailable)
            throw new ArgumentException("Stale/lost contact cannot communicate.", nameof(CommunicationAvailable));
    }
}

public sealed record RelationshipImpact(
    double TrustDelta, double HostilityDelta, double FearDelta,
    double RespectDelta, double CooperationDelta, double GrievanceSeverity, string Reason)
{
    public void Validate()
    {
        foreach (var delta in new[] { TrustDelta, HostilityDelta, FearDelta, RespectDelta, CooperationDelta })
            if (!double.IsFinite(delta) || delta is < -1.0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(delta));
        if (!double.IsFinite(GrievanceSeverity) || GrievanceSeverity is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(GrievanceSeverity));
        if (string.IsNullOrWhiteSpace(Reason)) throw new ArgumentException("Relationship impact requires a gameplay reason.", nameof(Reason));
    }
}

public sealed record DiplomaticContactSnapshot(
    int ObserverCivilizationId, string ContactId, int? TargetCivilizationId,
    long FirstObservedTick, long LastObservedTick, int? LastObservedSystemId,
    ContactAwareness Awareness, ContactCondition Condition, bool CommunicationAvailable, double Confidence);
public sealed record DiplomaticGrievanceSnapshot(long CreatedAtTick, int SourceCivilizationId, double Severity, string Reason);
public sealed record DiplomaticRelationshipSnapshot(
    int CivilizationAId, int CivilizationBId, DiplomaticPoliticalState PoliticalState,
    double Trust, double Hostility, double Fear, double Respect, double Cooperation,
    DiplomaticGrievanceSnapshot[] Grievances);
public sealed record DiplomaticAccessSnapshot(int GrantorCivilizationId, int VisitorCivilizationId, AccessPermission Permission, long UpdatedAtTick);
public sealed record TerritorialClaimSnapshot(long ClaimId, int ClaimantCivilizationId, int SystemId, long AssertedAtTick, bool Active, int[] KnownToCivilizationIds);
public sealed record TerritorialClaimResponseSnapshot(long ClaimId, int RespondingCivilizationId, TerritorialClaimResponse Response, long RespondedAtTick);
public sealed record DiplomaticAgreementSnapshot(
    long AgreementId, int CivilizationAId, int CivilizationBId, DiplomaticAgreementType Type,
    DiplomaticAgreementStatus Status, long StartedAtTick, long? EndedAtTick, string? ExternalTermsReference);
public sealed record DiplomaticProposalSnapshot(
    long ProposalId, int ProposerCivilizationId, int RecipientCivilizationId,
    DiplomaticProposalKind Kind, DiplomaticAgreementType? AgreementType,
    DiplomaticProposalStatus Status, long CreatedAtTick, long? ResolvedAtTick,
    string Summary, string? ExternalTermsReference);
public sealed record DiplomaticHistoryEventSnapshot(
    long EventId, long Tick, DiplomaticEventKind Kind, int PrimaryCivilizationId,
    int? SecondaryCivilizationId, int? SystemId, string Summary, int[] KnownToCivilizationIds);
public sealed record DiplomacyStateSnapshot(
    DiplomaticContactSnapshot[] Contacts, DiplomaticRelationshipSnapshot[] Relationships,
    DiplomaticAccessSnapshot[] AccessPermissions, TerritorialClaimSnapshot[] Claims,
    TerritorialClaimResponseSnapshot[] ClaimResponses, DiplomaticAgreementSnapshot[] Agreements,
    DiplomaticProposalSnapshot[] Proposals, DiplomaticHistoryEventSnapshot[] RecentHistory,
    long NextClaimId, long NextAgreementId, long NextProposalId, long NextEventId);

public sealed record DiplomaticContactView(
    string ContactId, int? TargetCivilizationId, ContactAwareness Awareness,
    ContactCondition Condition, bool CommunicationAvailable, double Confidence,
    long LastObservedTick, int? LastObservedSystemId);
public sealed record DiplomaticRelationshipView(
    int OtherCivilizationId, DiplomaticPoliticalState PoliticalState,
    double Trust, double Hostility, double Fear, double Respect, double Cooperation,
    IReadOnlyList<DiplomaticGrievanceSnapshot> Grievances);
public sealed record DiplomaticStateView(
    int ObserverCivilizationId, IReadOnlyList<DiplomaticContactView> Contacts,
    IReadOnlyList<DiplomaticRelationshipView> Relationships,
    IReadOnlyList<DiplomaticAccessSnapshot> AccessPermissions,
    IReadOnlyList<TerritorialClaimSnapshot> Claims,
    IReadOnlyList<TerritorialClaimResponseSnapshot> ClaimResponses,
    IReadOnlyList<DiplomaticAgreementSnapshot> Agreements,
    IReadOnlyList<DiplomaticProposalSnapshot> Proposals,
    IReadOnlyList<DiplomaticHistoryEventSnapshot> RecentEvents);

internal readonly record struct CivilizationPair(int First, int Second)
{
    public static CivilizationPair Create(int a, int b)
    {
        if (a == b) throw new ArgumentException("Diplomatic pair requires different civilizations.");
        return a < b ? new(a, b) : new(b, a);
    }
    public bool Contains(int id) => id == First || id == Second;
    public int Other(int id) => id == First ? Second : First;
}

internal sealed class ContactState
{
    public required int Observer { get; init; }
    public required string ContactId { get; init; }
    public int? Target { get; set; }
    public long FirstTick { get; set; }
    public long LastTick { get; set; }
    public int? SystemId { get; set; }
    public ContactAwareness Awareness { get; set; }
    public ContactCondition Condition { get; set; }
    public bool CanCommunicate { get; set; }
    public double Confidence { get; set; }
    public DiplomaticContactSnapshot Snapshot() => new(Observer, ContactId, Target, FirstTick, LastTick, SystemId, Awareness, Condition, CanCommunicate, Confidence);
}

internal sealed class RelationshipState
{
    public const int MaxGrievances = 16;
    public required CivilizationPair Pair { get; init; }
    public DiplomaticPoliticalState PoliticalState { get; set; } = DiplomaticPoliticalState.Peace;
    public double Trust { get; set; }
    public double Hostility { get; set; }
    public double Fear { get; set; }
    public double Respect { get; set; }
    public double Cooperation { get; set; }
    public List<DiplomaticGrievanceSnapshot> Grievances { get; } = new();
    public DiplomaticRelationshipSnapshot Snapshot() => new(Pair.First, Pair.Second, PoliticalState, Trust, Hostility, Fear, Respect, Cooperation, Grievances.ToArray());
}

internal sealed class ClaimState
{
    public required long Id { get; init; }
    public required int Claimant { get; init; }
    public required int SystemId { get; init; }
    public required long Tick { get; init; }
    public bool Active { get; set; } = true;
    public HashSet<int> KnownTo { get; } = new();
    public TerritorialClaimSnapshot Snapshot() => new(Id, Claimant, SystemId, Tick, Active, KnownTo.OrderBy(x => x).ToArray());
}

internal sealed class AgreementState
{
    public required long Id { get; init; }
    public required CivilizationPair Pair { get; init; }
    public required DiplomaticAgreementType Type { get; init; }
    public DiplomaticAgreementStatus Status { get; set; } = DiplomaticAgreementStatus.Active;
    public required long Started { get; init; }
    public long? Ended { get; set; }
    public string? ExternalTerms { get; init; }
    public DiplomaticAgreementSnapshot Snapshot() => new(Id, Pair.First, Pair.Second, Type, Status, Started, Ended, ExternalTerms);
}

internal sealed class ProposalState
{
    public required long Id { get; init; }
    public required int Proposer { get; init; }
    public required int Recipient { get; init; }
    public required DiplomaticProposalKind Kind { get; init; }
    public DiplomaticAgreementType? AgreementType { get; init; }
    public DiplomaticProposalStatus Status { get; set; } = DiplomaticProposalStatus.Pending;
    public required long Created { get; init; }
    public long? Resolved { get; set; }
    public required string Summary { get; init; }
    public string? ExternalTerms { get; init; }
    public DiplomaticProposalSnapshot Snapshot() => new(Id, Proposer, Recipient, Kind, AgreementType, Status, Created, Resolved, Summary, ExternalTerms);
}

public sealed class DiplomacyState
{
    public const int MaxContactRecords = 4096;
    public const int MaxRecentHistoryEvents = 256;
    public const int MaxStoredProposals = 128;
    public const int MaxPendingProposalsPerPair = 8;

    private readonly List<ContactState> _contacts = new();
    private readonly Dictionary<CivilizationPair, RelationshipState> _relationships = new();
    private readonly Dictionary<(int Grantor, int Visitor), DiplomaticAccessSnapshot> _access = new();
    private readonly Dictionary<long, ClaimState> _claims = new();
    private readonly Dictionary<(long Claim, int Responder), TerritorialClaimResponseSnapshot> _claimResponses = new();
    private readonly Dictionary<long, AgreementState> _agreements = new();
    private readonly Dictionary<long, ProposalState> _proposals = new();
    private readonly Queue<DiplomaticHistoryEventSnapshot> _history = new();
    private long _nextClaim = 1, _nextAgreement = 1, _nextProposal = 1, _nextEvent = 1;

    public DiplomaticContactSnapshot? GetContact(int observer, string contactId) =>
        _contacts.FirstOrDefault(x => x.Observer == observer && x.ContactId == contactId)?.Snapshot();
    public DiplomaticContactSnapshot? GetContact(int observer, int target) =>
        _contacts.Where(x => x.Observer == observer && x.Target == target).OrderByDescending(x => x.LastTick).FirstOrDefault()?.Snapshot();
    public DiplomaticRelationshipSnapshot? GetRelationship(int a, int b) =>
        _relationships.TryGetValue(CivilizationPair.Create(a, b), out var r) ? r.Snapshot() : null;
    public AccessPermission GetAccessPermission(int grantor, int visitor) =>
        _access.TryGetValue((grantor, visitor), out var value) ? value.Permission : AccessPermission.Unspecified;

    /// <summary>Political/legal authorization only; movement systems must not use this as a force field.</summary>
    public bool IsTransitAuthorized(int grantor, int visitor) =>
        GetRelationship(grantor, visitor)?.PoliticalState != DiplomaticPoliticalState.AtWar &&
        GetAccessPermission(grantor, visitor) == AccessPermission.Granted;

    public DiplomaticStateView BuildViewFor(int observer)
    {
        var contacts = _contacts.Where(x => x.Observer == observer).OrderBy(x => x.ContactId, StringComparer.Ordinal)
            .Select(x => new DiplomaticContactView(x.ContactId, x.Target, x.Awareness, x.Condition, x.CanCommunicate, x.Confidence, x.LastTick, x.SystemId)).ToArray();
        var known = contacts.Where(x => x.TargetCivilizationId.HasValue && x.Awareness >= ContactAwareness.Identified)
            .Select(x => x.TargetCivilizationId!.Value).ToHashSet();
        var relationships = _relationships.Values.Where(x => x.Pair.Contains(observer) && known.Contains(x.Pair.Other(observer)))
            .OrderBy(x => x.Pair.Other(observer))
            .Select(x => new DiplomaticRelationshipView(x.Pair.Other(observer), x.PoliticalState, x.Trust, x.Hostility, x.Fear, x.Respect, x.Cooperation, x.Grievances.ToArray())).ToArray();
        var access = _access.Values.Where(x => x.GrantorCivilizationId == observer || x.VisitorCivilizationId == observer)
            .Where(x => known.Contains(x.GrantorCivilizationId == observer ? x.VisitorCivilizationId : x.GrantorCivilizationId))
            .OrderBy(x => x.GrantorCivilizationId).ThenBy(x => x.VisitorCivilizationId).ToArray();
        var claims = _claims.Values.Where(x => x.KnownTo.Contains(observer)).OrderBy(x => x.Id).Select(x => x.Snapshot()).ToArray();
        var visibleClaims = claims.Select(x => x.ClaimId).ToHashSet();
        var responses = _claimResponses.Values.Where(x => visibleClaims.Contains(x.ClaimId))
            .Where(x => x.RespondingCivilizationId == observer || _claims[x.ClaimId].Claimant == observer).ToArray();
        var agreements = _agreements.Values.Where(x => x.Pair.Contains(observer) && known.Contains(x.Pair.Other(observer))).OrderBy(x => x.Id).Select(x => x.Snapshot()).ToArray();
        var proposals = _proposals.Values.Where(x => x.Proposer == observer || x.Recipient == observer)
            .Where(x => known.Contains(x.Proposer == observer ? x.Recipient : x.Proposer))
            .OrderBy(x => x.Id).Select(x => x.Snapshot()).ToArray();
        var history = _history.Where(x => x.KnownToCivilizationIds.Contains(observer)).ToArray();
        return new(observer, contacts, relationships, access, claims, responses, agreements, proposals, history);
    }

    public DiplomacyStateSnapshot Snapshot() => new(
        _contacts.OrderBy(x => x.Observer).ThenBy(x => x.ContactId, StringComparer.Ordinal).Select(x => x.Snapshot()).ToArray(),
        _relationships.Values.OrderBy(x => x.Pair.First).ThenBy(x => x.Pair.Second).Select(x => x.Snapshot()).ToArray(),
        _access.Values.OrderBy(x => x.GrantorCivilizationId).ThenBy(x => x.VisitorCivilizationId).ToArray(),
        _claims.Values.OrderBy(x => x.Id).Select(x => x.Snapshot()).ToArray(),
        _claimResponses.Values.OrderBy(x => x.ClaimId).ThenBy(x => x.RespondingCivilizationId).ToArray(),
        _agreements.Values.OrderBy(x => x.Id).Select(x => x.Snapshot()).ToArray(),
        _proposals.Values.OrderBy(x => x.Id).Select(x => x.Snapshot()).ToArray(),
        _history.ToArray(), _nextClaim, _nextAgreement, _nextProposal, _nextEvent);

    public static DiplomacyState Restore(DiplomacyStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var state = new DiplomacyState();
        foreach (var c in (snapshot.Contacts ?? Array.Empty<DiplomaticContactSnapshot>()).Take(MaxContactRecords))
        {
            if (c.ObserverCivilizationId < 0 || string.IsNullOrWhiteSpace(c.ContactId)) continue;
            state._contacts.Add(new ContactState
            {
                Observer = c.ObserverCivilizationId, ContactId = c.ContactId, Target = c.TargetCivilizationId,
                FirstTick = Math.Max(0, c.FirstObservedTick), LastTick = Math.Max(0, c.LastObservedTick), SystemId = c.LastObservedSystemId,
                Awareness = c.Awareness, Condition = c.Condition,
                CanCommunicate = c.CommunicationAvailable && c.Condition != ContactCondition.StaleOrLost,
                Confidence = double.IsFinite(c.Confidence) ? Math.Clamp(c.Confidence, 0, 1) : 0,
            });
        }
        foreach (var r in snapshot.Relationships ?? Array.Empty<DiplomaticRelationshipSnapshot>())
        {
            if (r.CivilizationAId == r.CivilizationBId) continue;
            var restored = new RelationshipState
            {
                Pair = CivilizationPair.Create(r.CivilizationAId, r.CivilizationBId), PoliticalState = r.PoliticalState,
                Trust = Clamp(r.Trust), Hostility = Clamp(r.Hostility), Fear = Clamp(r.Fear), Respect = Clamp(r.Respect), Cooperation = Clamp(r.Cooperation),
            };
            foreach (var g in (r.Grievances ?? Array.Empty<DiplomaticGrievanceSnapshot>()).TakeLast(RelationshipState.MaxGrievances))
                if (double.IsFinite(g.Severity) && !string.IsNullOrWhiteSpace(g.Reason)) restored.Grievances.Add(g with { Severity = Math.Clamp(g.Severity, 0, 1) });
            state._relationships[restored.Pair] = restored;
        }
        foreach (var a in snapshot.AccessPermissions ?? Array.Empty<DiplomaticAccessSnapshot>())
            if (a.GrantorCivilizationId != a.VisitorCivilizationId) state._access[(a.GrantorCivilizationId, a.VisitorCivilizationId)] = a with { UpdatedAtTick = Math.Max(0, a.UpdatedAtTick) };
        foreach (var c in snapshot.Claims ?? Array.Empty<TerritorialClaimSnapshot>())
        {
            if (c.ClaimId <= 0) continue;
            var claim = new ClaimState { Id = c.ClaimId, Claimant = c.ClaimantCivilizationId, SystemId = c.SystemId, Tick = Math.Max(0, c.AssertedAtTick), Active = c.Active };
            foreach (var id in c.KnownToCivilizationIds ?? Array.Empty<int>()) claim.KnownTo.Add(id);
            claim.KnownTo.Add(c.ClaimantCivilizationId); state._claims[c.ClaimId] = claim;
        }
        foreach (var r in snapshot.ClaimResponses ?? Array.Empty<TerritorialClaimResponseSnapshot>())
            if (state._claims.ContainsKey(r.ClaimId)) state._claimResponses[(r.ClaimId, r.RespondingCivilizationId)] = r with { RespondedAtTick = Math.Max(0, r.RespondedAtTick) };
        foreach (var a in snapshot.Agreements ?? Array.Empty<DiplomaticAgreementSnapshot>())
            if (a.AgreementId > 0 && a.CivilizationAId != a.CivilizationBId) state._agreements[a.AgreementId] = new AgreementState
            { Id = a.AgreementId, Pair = CivilizationPair.Create(a.CivilizationAId, a.CivilizationBId), Type = a.Type, Status = a.Status, Started = Math.Max(0, a.StartedAtTick), Ended = a.EndedAtTick, ExternalTerms = a.ExternalTermsReference };
        foreach (var p in (snapshot.Proposals ?? Array.Empty<DiplomaticProposalSnapshot>()).TakeLast(MaxStoredProposals))
            if (p.ProposalId > 0 && p.ProposerCivilizationId != p.RecipientCivilizationId && !string.IsNullOrWhiteSpace(p.Summary)) state._proposals[p.ProposalId] = new ProposalState
            { Id = p.ProposalId, Proposer = p.ProposerCivilizationId, Recipient = p.RecipientCivilizationId, Kind = p.Kind, AgreementType = p.AgreementType, Status = p.Status, Created = Math.Max(0, p.CreatedAtTick), Resolved = p.ResolvedAtTick, Summary = p.Summary, ExternalTerms = p.ExternalTermsReference };
        foreach (var e in (snapshot.RecentHistory ?? Array.Empty<DiplomaticHistoryEventSnapshot>()).TakeLast(MaxRecentHistoryEvents))
            if (e.EventId > 0 && !string.IsNullOrWhiteSpace(e.Summary)) state._history.Enqueue(e with { Tick = Math.Max(0, e.Tick), KnownToCivilizationIds = (e.KnownToCivilizationIds ?? Array.Empty<int>()).Distinct().OrderBy(x => x).ToArray() });
        state._nextClaim = Math.Max(Math.Max(1, snapshot.NextClaimId), state._claims.Keys.DefaultIfEmpty(0).Max() + 1);
        state._nextAgreement = Math.Max(Math.Max(1, snapshot.NextAgreementId), state._agreements.Keys.DefaultIfEmpty(0).Max() + 1);
        state._nextProposal = Math.Max(Math.Max(1, snapshot.NextProposalId), state._proposals.Keys.DefaultIfEmpty(0).Max() + 1);
        state._nextEvent = Math.Max(Math.Max(1, snapshot.NextEventId), state._history.Select(x => x.EventId).DefaultIfEmpty(0).Max() + 1);
        return state;
    }

    internal ContactState UpsertContact(FirstContactOpportunity o)
    {
        var c = _contacts.FirstOrDefault(x => x.Observer == o.ObserverCivilizationId && x.ContactId == o.ContactId);
        if (c is null)
        {
            if (_contacts.Count >= MaxContactRecords)
            {
                var stale = _contacts.Where(x => x.Condition == ContactCondition.StaleOrLost && x.Target is null).OrderBy(x => x.LastTick).FirstOrDefault();
                if (stale is null) throw new InvalidOperationException("Diplomatic contact storage is full.");
                _contacts.Remove(stale);
            }
            c = new ContactState { Observer = o.ObserverCivilizationId, ContactId = o.ContactId, Target = o.TargetCivilizationId, FirstTick = o.ObservedAtTick, LastTick = o.ObservedAtTick, SystemId = o.ObservedSystemId, Awareness = o.CommunicationAvailable ? ContactAwareness.CommunicationAvailable : o.Awareness, Condition = o.Condition, CanCommunicate = o.CommunicationAvailable, Confidence = o.Confidence };
            _contacts.Add(c); return c;
        }
        if (o.ObservedAtTick < c.LastTick) throw new InvalidOperationException("Contact observations cannot move backward in time.");
        if (c.Target.HasValue && o.TargetCivilizationId.HasValue && c.Target != o.TargetCivilizationId) throw new InvalidOperationException("Stable contact ID cannot be reassigned.");
        c.Target ??= o.TargetCivilizationId; c.LastTick = o.ObservedAtTick; c.SystemId = o.ObservedSystemId ?? c.SystemId;
        c.Confidence = o.Confidence; c.Condition = o.Condition;
        c.CanCommunicate = o.Condition != ContactCondition.StaleOrLost && (o.CommunicationAvailable || c.CanCommunicate);
        if (o.Awareness > c.Awareness) c.Awareness = o.Awareness;
        if (c.CanCommunicate && c.Awareness < ContactAwareness.CommunicationAvailable) c.Awareness = ContactAwareness.CommunicationAvailable;
        return c;
    }

    internal ContactState? MutableContact(int observer, string contactId) => _contacts.FirstOrDefault(x => x.Observer == observer && x.ContactId == contactId);
    internal bool HasIdentified(int observer, int target) => _contacts.Any(x => x.Observer == observer && x.Target == target && x.Awareness >= ContactAwareness.Identified);
    internal bool HasCommunication(int observer, int target) => _contacts.Any(x => x.Observer == observer && x.Target == target && x.CanCommunicate && x.Condition != ContactCondition.StaleOrLost);
    internal bool HasMutualCommunication(int a, int b) => HasCommunication(a, b) && HasCommunication(b, a);
    internal RelationshipState Relationship(int a, int b)
    {
        var pair = CivilizationPair.Create(a, b);
        if (!_relationships.TryGetValue(pair, out var r)) _relationships[pair] = r = new RelationshipState { Pair = pair };
        return r;
    }
    internal void SetAccess(int grantor, int visitor, AccessPermission permission, long tick) => _access[(grantor, visitor)] = new(grantor, visitor, permission, tick);
    internal ClaimState CreateClaim(int claimant, int systemId, long tick)
    {
        var existing = _claims.Values.FirstOrDefault(x => x.Active && x.Claimant == claimant && x.SystemId == systemId);
        if (existing is not null) return existing;
        var c = new ClaimState { Id = _nextClaim++, Claimant = claimant, SystemId = systemId, Tick = tick }; c.KnownTo.Add(claimant); _claims[c.Id] = c; return c;
    }
    internal ClaimState Claim(long id) => _claims.TryGetValue(id, out var c) && c.Active ? c : throw new InvalidOperationException("Unknown or inactive territorial claim.");
    internal void RespondToClaim(long id, int responder, TerritorialClaimResponse response, long tick) => _claimResponses[(id, responder)] = new(id, responder, response, tick);
    internal ProposalState CreateProposal(int proposer, int recipient, DiplomaticProposalKind kind, DiplomaticAgreementType? agreementType, long tick, string summary, string? externalTerms)
    {
        var pair = CivilizationPair.Create(proposer, recipient);
        if (_proposals.Values.Count(x => x.Status == DiplomaticProposalStatus.Pending && CivilizationPair.Create(x.Proposer, x.Recipient) == pair) >= MaxPendingProposalsPerPair)
            throw new InvalidOperationException("Too many pending proposals between these civilizations.");
        while (_proposals.Count >= MaxStoredProposals)
        {
            var old = _proposals.Values.Where(x => x.Status != DiplomaticProposalStatus.Pending).OrderBy(x => x.Resolved ?? x.Created).ThenBy(x => x.Id).FirstOrDefault();
            if (old is null) throw new InvalidOperationException("Proposal storage is full of unresolved proposals."); _proposals.Remove(old.Id);
        }
        var p = new ProposalState { Id = _nextProposal++, Proposer = proposer, Recipient = recipient, Kind = kind, AgreementType = agreementType, Created = tick, Summary = summary, ExternalTerms = externalTerms }; _proposals[p.Id] = p; return p;
    }
    internal ProposalState Proposal(long id) => _proposals.TryGetValue(id, out var p) ? p : throw new InvalidOperationException("Unknown diplomatic proposal.");
    internal AgreementState ActivateAgreement(int a, int b, DiplomaticAgreementType type, long tick, string? externalTerms)
    {
        var pair = CivilizationPair.Create(a, b); var existing = _agreements.Values.FirstOrDefault(x => x.Pair == pair && x.Type == type && x.Status == DiplomaticAgreementStatus.Active);
        if (existing is not null) return existing;
        var agreement = new AgreementState { Id = _nextAgreement++, Pair = pair, Type = type, Started = tick, ExternalTerms = externalTerms }; _agreements[agreement.Id] = agreement; return agreement;
    }
    internal IEnumerable<AgreementState> ActiveAgreements(CivilizationPair pair) => _agreements.Values.Where(x => x.Pair == pair && x.Status == DiplomaticAgreementStatus.Active);
    internal void Record(long tick, DiplomaticEventKind kind, int primary, int? secondary, int? systemId, string summary, params int[] audience)
    {
        var knownTo = audience.Append(primary).Where(x => x >= 0).Distinct().OrderBy(x => x).ToArray();
        _history.Enqueue(new(_nextEvent++, tick, kind, primary, secondary, systemId, summary, knownTo));
        while (_history.Count > MaxRecentHistoryEvents) _history.Dequeue();
    }
    private static double Clamp(double value) => double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;
}

public sealed class DiplomacySimulation
{
    private readonly DiplomacyState _state;
    public DiplomacySimulation(DiplomacyState state) => _state = state ?? throw new ArgumentNullException(nameof(state));

    public DiplomaticContactSnapshot ProcessContactOpportunity(FirstContactOpportunity opportunity)
    {
        opportunity.Validate(); var contact = _state.UpsertContact(opportunity);
        if (contact.Target.HasValue && contact.Awareness >= ContactAwareness.ContactEstablished) _state.Relationship(contact.Observer, contact.Target.Value);
        var kind = contact.CanCommunicate ? DiplomaticEventKind.CommunicationAvailable : contact.Awareness >= ContactAwareness.ContactEstablished ? DiplomaticEventKind.ContactEstablished : DiplomaticEventKind.ContactObserved;
        _state.Record(opportunity.ObservedAtTick, kind, opportunity.ObserverCivilizationId, contact.Target, opportunity.ObservedSystemId,
            $"Observer {contact.Observer} recorded {(contact.Target.HasValue ? $"civilization {contact.Target.Value}" : "an unidentified contact")} as {contact.Awareness} ({contact.Condition}, confidence {contact.Confidence:0.00}).",
            opportunity.ObserverCivilizationId);
        return contact.Snapshot();
    }

    public void MarkContactLost(int observer, string contactId, long tick)
    {
        Tick(tick); var c = _state.MutableContact(observer, contactId) ?? throw new InvalidOperationException("Unknown contact.");
        if (tick < c.LastTick) throw new InvalidOperationException("Contact loss cannot precede latest observation.");
        c.LastTick = tick; c.Condition = ContactCondition.StaleOrLost; c.CanCommunicate = false;
        _state.Record(tick, DiplomaticEventKind.ContactLost, observer, c.Target, c.SystemId, $"Contact {contactId} became stale or was lost.", observer);
    }

    public void SetAccessPermission(int grantor, int visitor, AccessPermission permission, long tick)
    {
        Tick(tick); if (grantor == visitor) throw new ArgumentException("Access applies between different civilizations."); RequireMutual(grantor, visitor);
        _state.SetAccess(grantor, visitor, permission, tick);
        _state.Record(tick, DiplomaticEventKind.AccessChanged, grantor, visitor, null, $"Civilization {grantor} set access for civilization {visitor} to {permission}.", grantor, visitor);
    }

    public long AssertTerritorialClaim(int claimant, int systemId, long tick)
    {
        Tick(tick); if (claimant < 0) throw new ArgumentOutOfRangeException(nameof(claimant)); if (systemId < 0) throw new ArgumentOutOfRangeException(nameof(systemId));
        var claim = _state.CreateClaim(claimant, systemId, tick);
        _state.Record(tick, DiplomaticEventKind.ClaimAsserted, claimant, null, systemId, $"Civilization {claimant} asserted a political claim over system {systemId}.", claimant); return claim.Id;
    }

    public void CommunicateTerritorialClaim(long claimId, int recipient, long tick)
    {
        Tick(tick); var claim = _state.Claim(claimId); RequireMutual(claim.Claimant, recipient); claim.KnownTo.Add(recipient);
        _state.Record(tick, DiplomaticEventKind.ClaimCommunicated, claim.Claimant, recipient, claim.SystemId, $"Civilization {claim.Claimant} communicated claim {claim.Id} over system {claim.SystemId}.", claim.Claimant, recipient);
    }

    public void RespondToTerritorialClaim(long claimId, int responder, TerritorialClaimResponse response, long tick)
    {
        Tick(tick); if (response == TerritorialClaimResponse.None) throw new ArgumentException("Use recognition or dispute.", nameof(response));
        var claim = _state.Claim(claimId); if (!claim.KnownTo.Contains(responder)) throw new InvalidOperationException("Cannot respond to an unknown claim."); RequireMutual(claim.Claimant, responder);
        _state.RespondToClaim(claimId, responder, response, tick);
        _state.Record(tick, DiplomaticEventKind.ClaimResponded, responder, claim.Claimant, claim.SystemId, $"Civilization {responder} {response.ToString().ToLowerInvariant()} claim {claimId}.", responder, claim.Claimant);
    }

    public void IssueBorderWarning(int issuer, int recipient, int systemId, long tick)
    {
        Tick(tick); RequireMutual(issuer, recipient);
        _state.Record(tick, DiplomaticEventKind.BorderWarningIssued, issuer, recipient, systemId, $"Civilization {issuer} warned civilization {recipient} against unauthorized presence in system {systemId}.", issuer, recipient);
    }

    public void RecordTrespass(int territorialCivilizationId, int intruder, int systemId, long tick)
    {
        Tick(tick); if (!_state.HasIdentified(territorialCivilizationId, intruder)) throw new InvalidOperationException("Trespass requires attributed identity.");
        if (_state.GetAccessPermission(territorialCivilizationId, intruder) == AccessPermission.Granted) throw new InvalidOperationException("Authorized entry is not trespass.");
        var reciprocalKnowledge = _state.HasIdentified(intruder, territorialCivilizationId);
        _state.Record(tick, DiplomaticEventKind.TrespassRecorded, territorialCivilizationId, intruder, systemId,
            $"Civilization {intruder} entered system {systemId} without political access authorization from civilization {territorialCivilizationId}.",
            reciprocalKnowledge ? new[] { territorialCivilizationId, intruder } : new[] { territorialCivilizationId });
    }

    public long SendProposal(int proposer, int recipient, DiplomaticProposalKind kind, long tick, string summary, DiplomaticAgreementType? agreementType = null, string? externalTermsReference = null)
    {
        Tick(tick); if (string.IsNullOrWhiteSpace(summary)) throw new ArgumentException("Proposal summary is required.", nameof(summary)); RequireMutual(proposer, recipient);
        if (kind == DiplomaticProposalKind.Agreement && agreementType is null) throw new ArgumentException("Agreement proposal requires agreement type.", nameof(agreementType));
        if (kind != DiplomaticProposalKind.Agreement && agreementType is not null) throw new ArgumentException("Agreement type only applies to agreement proposals.", nameof(agreementType));
        if (kind == DiplomaticProposalKind.TradeOffer && string.IsNullOrWhiteSpace(externalTermsReference)) throw new ArgumentException("Trade offer requires economy/logistics terms reference.", nameof(externalTermsReference));
        var p = _state.CreateProposal(proposer, recipient, kind, agreementType, tick, summary, externalTermsReference);
        _state.Record(tick, DiplomaticEventKind.ProposalSent, proposer, recipient, null, $"Proposal {p.Id}: {summary}", proposer, recipient); return p.Id;
    }

    public void RespondToProposal(long proposalId, int responder, bool accept, long tick)
    {
        Tick(tick); var p = _state.Proposal(proposalId); if (p.Status != DiplomaticProposalStatus.Pending) throw new InvalidOperationException("Proposal is resolved.");
        if (p.Recipient != responder) throw new InvalidOperationException("Only recipient may respond."); RequireMutual(p.Proposer, p.Recipient);
        if (accept && p.Kind == DiplomaticProposalKind.Agreement && p.AgreementType == DiplomaticAgreementType.NonAggression && _state.GetRelationship(p.Proposer, p.Recipient)?.PoliticalState == DiplomaticPoliticalState.AtWar)
            throw new InvalidOperationException("Negotiate peace or ceasefire before non-aggression.");
        if (accept) ApplyAccepted(p, tick);
        p.Status = accept ? DiplomaticProposalStatus.Accepted : DiplomaticProposalStatus.Rejected; p.Resolved = tick;
        _state.Record(tick, accept ? DiplomaticEventKind.ProposalAccepted : DiplomaticEventKind.ProposalRejected, responder, p.Proposer, null, $"Proposal {p.Id} was {(accept ? "accepted" : "rejected")}.", responder, p.Proposer);
    }

    public void WithdrawProposal(long proposalId, int proposer, long tick)
    {
        Tick(tick); var p = _state.Proposal(proposalId); if (p.Status != DiplomaticProposalStatus.Pending || p.Proposer != proposer) throw new InvalidOperationException("Only proposer may withdraw pending proposal.");
        p.Status = DiplomaticProposalStatus.Withdrawn; p.Resolved = tick; _state.Record(tick, DiplomaticEventKind.ProposalWithdrawn, proposer, p.Recipient, null, $"Proposal {p.Id} was withdrawn.", proposer, p.Recipient);
    }

    public void ExpireProposal(long proposalId, long tick)
    {
        Tick(tick); var p = _state.Proposal(proposalId); if (p.Status != DiplomaticProposalStatus.Pending) return; if (tick < p.Created) throw new InvalidOperationException("Proposal cannot expire before creation.");
        p.Status = DiplomaticProposalStatus.Expired; p.Resolved = tick; _state.Record(tick, DiplomaticEventKind.ProposalExpired, p.Proposer, p.Recipient, null, $"Proposal {p.Id} expired.", p.Proposer, p.Recipient);
    }

    public void ApplyRelationshipImpact(int observer, int target, RelationshipImpact impact, long tick)
    {
        Tick(tick); impact.Validate(); if (!_state.HasIdentified(observer, target)) throw new InvalidOperationException("Relationship impact requires legitimately identified contact.");
        var r = _state.Relationship(observer, target); r.Trust = Clamp(r.Trust + impact.TrustDelta); r.Hostility = Clamp(r.Hostility + impact.HostilityDelta);
        r.Fear = Clamp(r.Fear + impact.FearDelta); r.Respect = Clamp(r.Respect + impact.RespectDelta); r.Cooperation = Clamp(r.Cooperation + impact.CooperationDelta);
        if (impact.GrievanceSeverity > 0)
        {
            r.Grievances.Add(new(tick, target, impact.GrievanceSeverity, impact.Reason)); while (r.Grievances.Count > RelationshipState.MaxGrievances) r.Grievances.RemoveAt(0);
        }
        _state.Record(tick, DiplomaticEventKind.RelationshipChanged, observer, target, null, impact.Reason, observer);
    }

    public void SetHostile(int a, int b, long tick, string reason)
    {
        Tick(tick); if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Hostility requires reason.", nameof(reason)); if (!_state.HasIdentified(a, b)) throw new InvalidOperationException("Hostility requires identified counterpart.");
        var r = _state.Relationship(a, b); if (r.PoliticalState is DiplomaticPoliticalState.Peace or DiplomaticPoliticalState.Ceasefire) r.PoliticalState = DiplomaticPoliticalState.Hostile;
        _state.Record(tick, DiplomaticEventKind.RelationshipChanged, a, b, null, reason, a);
    }

    public void DeclareWar(int declarer, int target, long tick)
    {
        Tick(tick); if (!_state.HasIdentified(declarer, target)) throw new InvalidOperationException("War declaration cannot target a hidden civilization.");
        var r = _state.Relationship(declarer, target); if (r.PoliticalState == DiplomaticPoliticalState.AtWar) return; r.PoliticalState = DiplomaticPoliticalState.AtWar;
        var pair = CivilizationPair.Create(declarer, target);
        foreach (var a in _state.ActiveAgreements(pair).ToArray())
        {
            a.Status = DiplomaticAgreementStatus.Terminated; a.Ended = tick;
            _state.Record(tick, DiplomaticEventKind.AgreementTerminated, pair.First, pair.Second, null, $"Agreement {a.Id} ({a.Type}) ended when war began.", pair.First, pair.Second);
        }
        _state.SetAccess(declarer, target, AccessPermission.Denied, tick); _state.SetAccess(target, declarer, AccessPermission.Denied, tick);
        var targetKnows = _state.HasIdentified(target, declarer);
        _state.Record(tick, DiplomaticEventKind.WarDeclared, declarer, target, null, $"Civilization {declarer} declared war on civilization {target}.", targetKnows ? new[] { declarer, target } : new[] { declarer });
    }

    private void ApplyAccepted(ProposalState p, long tick)
    {
        switch (p.Kind)
        {
            case DiplomaticProposalKind.Agreement: Activate(p.Proposer, p.Recipient, p.AgreementType!.Value, tick, p.ExternalTerms); break;
            case DiplomaticProposalKind.AccessRequest:
                _state.SetAccess(p.Recipient, p.Proposer, AccessPermission.Granted, tick);
                _state.Record(tick, DiplomaticEventKind.AccessChanged, p.Recipient, p.Proposer, null, $"Civilization {p.Recipient} granted access to civilization {p.Proposer}.", p.Recipient, p.Proposer); break;
            case DiplomaticProposalKind.TradeOffer: Activate(p.Proposer, p.Recipient, DiplomaticAgreementType.Trade, tick, p.ExternalTerms); break;
            case DiplomaticProposalKind.PeaceOffer: Activate(p.Proposer, p.Recipient, DiplomaticAgreementType.Peace, tick, p.ExternalTerms); break;
            case DiplomaticProposalKind.CeasefireOffer: Activate(p.Proposer, p.Recipient, DiplomaticAgreementType.Ceasefire, tick, p.ExternalTerms); break;
            case DiplomaticProposalKind.Demand: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private void Activate(int a, int b, DiplomaticAgreementType type, long tick, string? externalTerms)
    {
        var r = _state.Relationship(a, b); if (type == DiplomaticAgreementType.NonAggression && r.PoliticalState == DiplomaticPoliticalState.AtWar) throw new InvalidOperationException("Non-aggression cannot replace active war.");
        var agreement = _state.ActivateAgreement(a, b, type, tick, externalTerms);
        if (type == DiplomaticAgreementType.Peace) r.PoliticalState = DiplomaticPoliticalState.Peace;
        else if (type == DiplomaticAgreementType.Ceasefire) r.PoliticalState = DiplomaticPoliticalState.Ceasefire;
        else if (type == DiplomaticAgreementType.Access) { _state.SetAccess(a, b, AccessPermission.Granted, tick); _state.SetAccess(b, a, AccessPermission.Granted, tick); }
        _state.Record(tick, DiplomaticEventKind.AgreementActivated, a, b, null, $"Agreement {agreement.Id} ({type}) became active.", a, b);
    }

    private void RequireMutual(int a, int b)
    {
        if (!_state.HasMutualCommunication(a, b)) throw new InvalidOperationException("Diplomatic action requires current two-way communication.");
    }
    private static void Tick(long tick) { if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick)); }
    private static double Clamp(double value) => Math.Clamp(value, 0, 1);
}

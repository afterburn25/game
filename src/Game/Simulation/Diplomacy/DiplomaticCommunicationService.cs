using System;
using System.Linq;

namespace Game.Simulation.Diplomacy;

/// <summary>
/// Promotes two independently legitimate identified contacts into a mutual diplomatic
/// communication channel. This service does not decide whether communications technology,
/// translation, protocol negotiation or political willingness exists; the owning caller must
/// establish those prerequisites. Diplomacy only validates that neither side is hidden/stale
/// and records the resulting political communication state.
/// </summary>
public sealed class DiplomaticCommunicationService
{
    private readonly DiplomacyState _state;
    private readonly DiplomacySimulation _diplomacy;

    public DiplomaticCommunicationService(DiplomacyState state, DiplomacySimulation diplomacy)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
    }

    public void EstablishMutualCommunication(int civilizationA, int civilizationB, long tick)
    {
        if (civilizationA == civilizationB)
            throw new ArgumentException("Diplomatic communication requires two different civilizations.");
        if (civilizationA < 0)
            throw new ArgumentOutOfRangeException(nameof(civilizationA));
        if (civilizationB < 0)
            throw new ArgumentOutOfRangeException(nameof(civilizationB));
        if (tick < 0)
            throw new ArgumentOutOfRangeException(nameof(tick));

        // Resolve both observer-local contacts before mutating either side, so a failed attempt
        // cannot accidentally grant one-way communication to an otherwise unaware civilization.
        var contactA = FindEligibleContact(civilizationA, civilizationB);
        var contactB = FindEligibleContact(civilizationB, civilizationA);
        if (tick < contactA.LastObservedTick || tick < contactB.LastObservedTick)
            throw new InvalidOperationException("Communication cannot be established before either side's latest observation.");

        Promote(contactA, civilizationA, civilizationB, tick);
        Promote(contactB, civilizationB, civilizationA, tick);
    }

    private DiplomaticContactView FindEligibleContact(int observer, int target)
    {
        var contact = _state.BuildViewFor(observer).Contacts
            .Where(candidate => candidate.TargetCivilizationId == target)
            .OrderByDescending(candidate => candidate.LastObservedTick)
            .FirstOrDefault();

        if (contact is null || contact.Awareness < ContactAwareness.Identified)
            throw new InvalidOperationException("Mutual communication requires each civilization to have legitimately identified the other.");
        if (contact.Condition == ContactCondition.StaleOrLost)
            throw new InvalidOperationException("Stale or lost contact must be reacquired before communication can be established.");

        return contact;
    }

    private void Promote(DiplomaticContactView contact, int observer, int target, long tick)
    {
        _diplomacy.ProcessContactOpportunity(new FirstContactOpportunity(
            observer,
            contact.ContactId,
            target,
            tick,
            contact.LastObservedSystemId,
            ContactAwareness.CommunicationAvailable,
            contact.Condition,
            CommunicationAvailable: true,
            Confidence: contact.Confidence));
    }
}

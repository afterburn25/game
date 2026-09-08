using System;

namespace Game.Simulation.Diplomacy;

public sealed record DiplomaticContactAgingResult(int ReviewedContacts, int NewlyStaleContacts);

/// <summary>
/// Low-frequency/event-scheduled contact aging. The caller supplies the campaign-appropriate
/// stale threshold; Diplomacy does not invent one universal calendar duration. Active
/// communication keeps a diplomatic contact current, while observation-only contacts can age
/// into stale/lost state. The original last-observed tick is preserved as historical evidence.
/// </summary>
public sealed class DiplomaticContactAgingService
{
    private readonly DiplomacyState _state;

    public DiplomaticContactAgingService(DiplomacyState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public DiplomaticContactAgingResult Review(long nowTick, long staleAfterTicks)
    {
        if (nowTick < 0)
            throw new ArgumentOutOfRangeException(nameof(nowTick));
        if (staleAfterTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(staleAfterTicks));

        var snapshot = _state.Snapshot();
        var reviewed = 0;
        var newlyStale = 0;

        foreach (var contact in snapshot.Contacts)
        {
            reviewed++;
            if (nowTick < contact.LastObservedTick)
                throw new InvalidOperationException("Contact aging review cannot precede the latest observation.");
            if (contact.Condition == ContactCondition.StaleOrLost || contact.CommunicationAvailable)
                continue;

            var age = nowTick - contact.LastObservedTick;
            if (age < staleAfterTicks)
                continue;

            var mutable = _state.MutableContact(contact.ObserverCivilizationId, contact.ContactId)
                ?? throw new InvalidOperationException("Diplomatic contact changed during a scheduled aging review.");
            mutable.Condition = ContactCondition.StaleOrLost;
            mutable.CanCommunicate = false;

            _state.Record(
                nowTick,
                DiplomaticEventKind.ContactLost,
                contact.ObserverCivilizationId,
                contact.TargetCivilizationId,
                contact.LastObservedSystemId,
                $"Contact {contact.ContactId} became stale after {age} ticks without a fresh observation.",
                contact.ObserverCivilizationId);
            newlyStale++;
        }

        return new DiplomaticContactAgingResult(reviewed, newlyStale);
    }
}

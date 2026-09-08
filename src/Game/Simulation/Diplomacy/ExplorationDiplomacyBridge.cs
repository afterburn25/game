using System;
using System.Collections.Generic;
using Game.Simulation.Exploration;

namespace Game.Simulation.Diplomacy;

/// <summary>
/// Converts exploration-owned, legitimately acquired first-contact events into
/// diplomacy-owned directional contact state. The bridge never scans GalaxyState
/// or manufactures reciprocal knowledge; Exploration must explicitly supply the
/// identified target civilization on the event it emits.
/// </summary>
public sealed class ExplorationDiplomacyBridge
{
    private readonly DiplomacySimulation _diplomacy;

    public ExplorationDiplomacyBridge(DiplomacySimulation diplomacy)
    {
        _diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
    }

    public int Process(IReadOnlyList<ExplorationEvent> events, long observedAtTick)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (observedAtTick < 0)
            throw new ArgumentOutOfRangeException(nameof(observedAtTick));

        var processed = 0;
        foreach (var explorationEvent in events)
        {
            if (explorationEvent.Type != ExplorationEventType.FirstContact)
                continue;

            if (explorationEvent.TargetCivilizationId is not int targetCivilizationId)
            {
                throw new InvalidOperationException(
                    "Exploration FirstContact events must carry the legitimately identified target civilization ID.");
            }

            _diplomacy.ProcessContactOpportunity(new FirstContactOpportunity(
                explorationEvent.CivilizationId,
                $"civilization:{targetCivilizationId}",
                targetCivilizationId,
                observedAtTick,
                explorationEvent.SystemId,
                ContactAwareness.ContactEstablished,
                ContactCondition.Active,
                CommunicationAvailable: false,
                Confidence: 1.0));
            processed++;
        }

        return processed;
    }
}

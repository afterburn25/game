using System;
using System.Collections.Generic;
using Game.Simulation.Combat;

namespace Game.Simulation.Diplomacy;

/// <summary>
/// Read-only political authorization for Combat. Combat is permitted only after Diplomacy has
/// already placed the pair in Hostile or AtWar state; Combat never creates that state itself.
/// Peace, Ceasefire and unknown relationships remain non-hostile.
/// </summary>
public sealed class DiplomacyCombatHostilityView : ICombatHostilityView
{
    private readonly DiplomacyState _state;

    public DiplomacyCombatHostilityView(DiplomacyState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public bool AreHostile(int firstCivilizationId, int secondCivilizationId)
    {
        if (firstCivilizationId < 0 || secondCivilizationId < 0 || firstCivilizationId == secondCivilizationId)
            return false;

        return _state.GetRelationship(firstCivilizationId, secondCivilizationId)?.PoliticalState
            is DiplomaticPoliticalState.Hostile or DiplomaticPoliticalState.AtWar;
    }
}

/// <summary>
/// Translates only strategically meaningful Combat events into diplomatic consequences.
/// Per-salvo damage is intentionally ignored so long wars cannot flood diplomatic history.
/// An engagement may reinforce an already-hostile relationship, while destruction of a physical
/// vessel becomes a major grievance only when the victim can legitimately attribute the attacker.
/// </summary>
public sealed class CombatDiplomacyBridge
{
    private readonly DiplomacyState _state;
    private readonly DiplomacySimulation _diplomacy;

    public CombatDiplomacyBridge(DiplomacyState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _diplomacy = new DiplomacySimulation(_state);
    }

    public int Process(IReadOnlyList<CombatEvent> events, long tick)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (tick < 0)
            throw new ArgumentOutOfRangeException(nameof(tick));

        var processed = 0;
        foreach (var combatEvent in events)
        {
            if (combatEvent.TargetCivilizationId is not int targetCivilizationId ||
                targetCivilizationId == combatEvent.ActorCivilizationId)
            {
                continue;
            }

            // Raw authoritative Combat identity is not automatically diplomatic knowledge.
            // If the target cannot yet attribute the attacker, do not leak that identity through
            // relationship state/history. Exploration/intelligence can establish attribution later.
            if (!_state.HasIdentified(targetCivilizationId, combatEvent.ActorCivilizationId))
                continue;

            switch (combatEvent.Type)
            {
                case CombatEventType.EngagementStarted:
                    _diplomacy.SetHostile(
                        targetCivilizationId,
                        combatEvent.ActorCivilizationId,
                        tick,
                        $"Hostile military engagement in system {combatEvent.SystemId?.ToString() ?? "unknown"}: {combatEvent.Message}");
                    processed++;
                    break;

                case CombatEventType.FleetDestroyed:
                    // Complete loss of a physical military vessel is a major persistent grievance.
                    // We deliberately do not stack trust/fear/hostility scalar deltas here; those
                    // dimensions remain available for later culture/AI-specific interpretation.
                    _diplomacy.ApplyRelationshipImpact(
                        targetCivilizationId,
                        combatEvent.ActorCivilizationId,
                        new RelationshipImpact(
                            TrustDelta: 0.0,
                            HostilityDelta: 0.0,
                            FearDelta: 0.0,
                            RespectDelta: 0.0,
                            CooperationDelta: 0.0,
                            GrievanceSeverity: 1.0,
                            Reason: $"Military vessel destroyed in system {combatEvent.SystemId?.ToString() ?? "unknown"}: {combatEvent.Message}"),
                        tick);
                    processed++;
                    break;

                // Damage, retreat and engagement-end events remain transient Combat detail.
                // Persisting each one would violate bounded-history guidance.
                default:
                    break;
            }
        }

        return processed;
    }
}

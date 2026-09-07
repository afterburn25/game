using System;
using Game.Simulation.AI;

namespace Game.Simulation.Combat;

/// <summary>
/// Political hostility is owned by diplomacy. Combat only asks whether two
/// civilizations are currently permitted to resolve a hostile engagement.
/// </summary>
public interface ICombatHostilityView
{
    bool AreHostile(int firstCivilizationId, int secondCivilizationId);
}

public sealed class PeacefulCombatHostilityView : ICombatHostilityView
{
    public static PeacefulCombatHostilityView Instance { get; } = new();
    private PeacefulCombatHostilityView() { }
    public bool AreHostile(int firstCivilizationId, int secondCivilizationId) => false;
}

/// <summary>
/// Small adapter useful for tests and for a future diplomacy read-only view.
/// It stores no political state of its own.
/// </summary>
public sealed class DelegateCombatHostilityView(Func<int, int, bool> evaluator) : ICombatHostilityView
{
    private readonly Func<int, int, bool> _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    public bool AreHostile(int firstCivilizationId, int secondCivilizationId) =>
        _evaluator(firstCivilizationId, secondCivilizationId);
}

public sealed record MilitaryOrder(
    MilitaryOrderType Type,
    int? TargetFleetId = null,
    int? DefendSystemId = null);

public sealed record CombatOrderResult(bool Accepted, string Message);

public enum CombatEventType
{
    EngagementStarted,
    DamageApplied,
    FleetRetreatInitiated,
    FleetEscaped,
    FleetDestroyed,
    EngagementEnded,
}

/// <summary>
/// Combat events are transient integration/presentation messages. Callers may
/// retain strategically meaningful outcomes, but should not store every damage
/// event forever.
/// </summary>
public sealed record CombatEvent(
    CombatEventType Type,
    int? SystemId,
    int ActorCivilizationId,
    int ActorFleetId,
    int? TargetCivilizationId,
    int? TargetFleetId,
    double ShieldDamage,
    double ArmorDamage,
    double HullDamage,
    string Message);

public sealed record MilitaryForceSummary(
    int CivilizationId,
    int ActiveCombatVessels,
    double CurrentStrength,
    double MaximumStrength);

/// <summary>
/// Enemy military information stays as an estimate range supplied by legitimate
/// intelligence. This type deliberately cannot expose authoritative fleet state.
/// </summary>
public sealed record KnownMilitaryForceSummary(
    int CivilizationId,
    double EstimatedStrengthLow,
    double EstimatedStrengthHigh,
    double Confidence,
    long LastObservationTick)
{
    public static KnownMilitaryForceSummary FromKnowledge(KnownCivilization known) => new(
        known.CivilizationId,
        known.EstimatedMilitaryLow,
        known.EstimatedMilitaryHigh,
        known.EstimateConfidence,
        known.LastMilitaryObservationTick);
}

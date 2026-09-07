using System;
using Game.Simulation.Diplomacy;

namespace Game.Simulation.Combat;

/// <summary>
/// Read-only bridge from Diplomacy's authoritative bilateral political state into Combat.
/// Combat does not own, cache, or mutate political state through this adapter.
///
/// Hostile permits limited hostile engagements/skirmishes and AtWar permits wartime combat.
/// Peace, Ceasefire, Unknown, and missing relationships prohibit hostile Combat orders.
/// </summary>
public sealed class DiplomacyCombatHostilityView : ICombatHostilityView
{
    private readonly DiplomacyState _diplomacy;

    public DiplomacyCombatHostilityView(DiplomacyState diplomacy)
    {
        _diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
    }

    public bool AreHostile(int firstCivilizationId, int secondCivilizationId)
    {
        if (firstCivilizationId == secondCivilizationId)
            return false;

        var relationship = _diplomacy.GetRelationship(firstCivilizationId, secondCivilizationId);
        return relationship?.PoliticalState is DiplomaticPoliticalState.Hostile or DiplomaticPoliticalState.AtWar;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Simulation.Combat;

/// <summary>
/// Compact per-civilization Combat consequences derived only from CombatEvent values already
/// available to the caller. No GalaxyState lookup is performed, so this type cannot turn an
/// observer-filtered event stream into additional hidden information.
/// </summary>
public sealed record CombatCivilizationOutcomeSummary(
    int CivilizationId,
    double ShieldDamageDealt,
    double ArmorDamageDealt,
    double HullDamageDealt,
    double ShieldDamageTaken,
    double ArmorDamageTaken,
    double HullDamageTaken,
    int EnemyVesselsDestroyed,
    int OwnVesselsLost,
    int RetreatsInitiated,
    int SuccessfulEscapes,
    double EmbarkedPopulationCasualtiesInflictedMillions,
    double EmbarkedPopulationCasualtiesSufferedMillions)
{
    public double TotalDamageDealt => ShieldDamageDealt + ArmorDamageDealt + HullDamageDealt;
    public double TotalDamageTaken => ShieldDamageTaken + ArmorDamageTaken + HullDamageTaken;
}

/// <summary>
/// Compact outcome for one system represented in a transient Combat event set. A null system
/// remains null rather than being guessed from authoritative world state.
/// </summary>
public sealed record CombatSystemOutcomeSummary(
    int? SystemId,
    int EventCount,
    int EngagementsStarted,
    int EngagementsEnded,
    int DamageEvents,
    int VesselsDestroyed,
    int RetreatsInitiated,
    int SuccessfulEscapes,
    double TotalDamageApplied,
    double EmbarkedPopulationCasualtiesMillions,
    IReadOnlyList<CombatCivilizationOutcomeSummary> Civilizations);

/// <summary>
/// One transient Combat-step/batch summary. It intentionally carries aggregates rather than
/// messages or individual damage events and is not intended as permanent battle history.
/// </summary>
public sealed record CombatOutcomeSummary(
    int EventCount,
    int EngagementsStarted,
    int EngagementsEnded,
    int DamageEvents,
    int VesselsDestroyed,
    int RetreatsInitiated,
    int SuccessfulEscapes,
    double TotalDamageApplied,
    double EmbarkedPopulationCasualtiesMillions,
    IReadOnlyList<CombatCivilizationOutcomeSummary> Civilizations,
    IReadOnlyList<CombatSystemOutcomeSummary> Systems)
{
    public static CombatOutcomeSummary Empty { get; } = new(
        0, 0, 0, 0, 0, 0, 0, 0.0, 0.0,
        Array.Empty<CombatCivilizationOutcomeSummary>(),
        Array.Empty<CombatSystemOutcomeSummary>());
}

/// <summary>
/// Linear event aggregation for UI/AI/subsystem consumption. Information visibility is inherited
/// from the supplied event list; callers that require fog-of-war filtering must filter the events
/// before building this summary.
/// </summary>
public static class CombatOutcomeSummaryBuilder
{
    public static CombatOutcomeSummary Build(IEnumerable<CombatEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var overall = new OutcomeAccumulator();
        var systems = new Dictionary<SystemKey, OutcomeAccumulator>();
        var any = false;

        foreach (var combatEvent in events)
        {
            if (combatEvent is null)
                throw new ArgumentException("Combat event collections cannot contain null entries.", nameof(events));

            any = true;
            Accumulate(overall, combatEvent);

            var key = new SystemKey(combatEvent.SystemId);
            if (!systems.TryGetValue(key, out var system))
            {
                system = new OutcomeAccumulator();
                systems.Add(key, system);
            }
            Accumulate(system, combatEvent);
        }

        if (!any)
            return CombatOutcomeSummary.Empty;

        var systemSummaries = systems
            .OrderBy(pair => pair.Key.SystemId.HasValue ? 0 : 1)
            .ThenBy(pair => pair.Key.SystemId.GetValueOrDefault())
            .Select(pair => ToSystemSummary(pair.Key.SystemId, pair.Value))
            .ToArray();

        return new CombatOutcomeSummary(
            overall.EventCount,
            overall.EngagementsStarted,
            overall.EngagementsEnded,
            overall.DamageEvents,
            overall.VesselsDestroyed,
            overall.RetreatsInitiated,
            overall.SuccessfulEscapes,
            overall.TotalDamageApplied,
            overall.EmbarkedPopulationCasualtiesMillions,
            BuildCivilizationSummaries(overall),
            systemSummaries);
    }

    private static CombatSystemOutcomeSummary ToSystemSummary(int? systemId, OutcomeAccumulator accumulator) =>
        new(
            systemId,
            accumulator.EventCount,
            accumulator.EngagementsStarted,
            accumulator.EngagementsEnded,
            accumulator.DamageEvents,
            accumulator.VesselsDestroyed,
            accumulator.RetreatsInitiated,
            accumulator.SuccessfulEscapes,
            accumulator.TotalDamageApplied,
            accumulator.EmbarkedPopulationCasualtiesMillions,
            BuildCivilizationSummaries(accumulator));

    private static IReadOnlyList<CombatCivilizationOutcomeSummary> BuildCivilizationSummaries(OutcomeAccumulator accumulator) =>
        accumulator.Civilizations
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Value.ToSummary(pair.Key))
            .ToArray();

    private static void Accumulate(OutcomeAccumulator accumulator, CombatEvent combatEvent)
    {
        accumulator.EventCount++;
        var actor = accumulator.Civilization(combatEvent.ActorCivilizationId);
        MutableCivilizationOutcome? target = combatEvent.TargetCivilizationId is int targetCivilizationId
            ? accumulator.Civilization(targetCivilizationId)
            : null;

        switch (combatEvent.Type)
        {
            case CombatEventType.EngagementStarted:
                accumulator.EngagementsStarted++;
                break;

            case CombatEventType.EngagementEnded:
                accumulator.EngagementsEnded++;
                break;

            case CombatEventType.DamageApplied:
            {
                accumulator.DamageEvents++;
                var shield = SanitizeNonNegative(combatEvent.ShieldDamage);
                var armor = SanitizeNonNegative(combatEvent.ArmorDamage);
                var hull = SanitizeNonNegative(combatEvent.HullDamage);
                var total = shield + armor + hull;
                accumulator.TotalDamageApplied += total;

                actor.ShieldDamageDealt += shield;
                actor.ArmorDamageDealt += armor;
                actor.HullDamageDealt += hull;
                if (target is not null)
                {
                    target.ShieldDamageTaken += shield;
                    target.ArmorDamageTaken += armor;
                    target.HullDamageTaken += hull;
                }
                break;
            }

            case CombatEventType.FleetDestroyed:
            {
                accumulator.VesselsDestroyed++;
                actor.EnemyVesselsDestroyed++;
                if (target is not null)
                    target.OwnVesselsLost++;

                var casualties = SanitizeNonNegative(combatEvent.EmbarkedPopulationCasualtiesMillions);
                accumulator.EmbarkedPopulationCasualtiesMillions += casualties;
                actor.EmbarkedPopulationCasualtiesInflictedMillions += casualties;
                if (target is not null)
                    target.EmbarkedPopulationCasualtiesSufferedMillions += casualties;
                break;
            }

            case CombatEventType.FleetRetreatInitiated:
                accumulator.RetreatsInitiated++;
                actor.RetreatsInitiated++;
                break;

            case CombatEventType.FleetEscaped:
                accumulator.SuccessfulEscapes++;
                actor.SuccessfulEscapes++;
                break;
        }
    }

    private static double SanitizeNonNegative(double value) =>
        double.IsFinite(value) ? Math.Max(0.0, value) : 0.0;

    private readonly record struct SystemKey(int? SystemId);

    private sealed class OutcomeAccumulator
    {
        public int EventCount { get; set; }
        public int EngagementsStarted { get; set; }
        public int EngagementsEnded { get; set; }
        public int DamageEvents { get; set; }
        public int VesselsDestroyed { get; set; }
        public int RetreatsInitiated { get; set; }
        public int SuccessfulEscapes { get; set; }
        public double TotalDamageApplied { get; set; }
        public double EmbarkedPopulationCasualtiesMillions { get; set; }
        public Dictionary<int, MutableCivilizationOutcome> Civilizations { get; } = new();

        public MutableCivilizationOutcome Civilization(int civilizationId)
        {
            if (!Civilizations.TryGetValue(civilizationId, out var summary))
            {
                summary = new MutableCivilizationOutcome();
                Civilizations.Add(civilizationId, summary);
            }
            return summary;
        }
    }

    private sealed class MutableCivilizationOutcome
    {
        public double ShieldDamageDealt { get; set; }
        public double ArmorDamageDealt { get; set; }
        public double HullDamageDealt { get; set; }
        public double ShieldDamageTaken { get; set; }
        public double ArmorDamageTaken { get; set; }
        public double HullDamageTaken { get; set; }
        public int EnemyVesselsDestroyed { get; set; }
        public int OwnVesselsLost { get; set; }
        public int RetreatsInitiated { get; set; }
        public int SuccessfulEscapes { get; set; }
        public double EmbarkedPopulationCasualtiesInflictedMillions { get; set; }
        public double EmbarkedPopulationCasualtiesSufferedMillions { get; set; }

        public CombatCivilizationOutcomeSummary ToSummary(int civilizationId) =>
            new(
                civilizationId,
                ShieldDamageDealt,
                ArmorDamageDealt,
                HullDamageDealt,
                ShieldDamageTaken,
                ArmorDamageTaken,
                HullDamageTaken,
                EnemyVesselsDestroyed,
                OwnVesselsLost,
                RetreatsInitiated,
                SuccessfulEscapes,
                EmbarkedPopulationCasualtiesInflictedMillions,
                EmbarkedPopulationCasualtiesSufferedMillions);
    }
}

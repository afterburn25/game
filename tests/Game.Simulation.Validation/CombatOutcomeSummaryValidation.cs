using Game.Simulation.Combat;

namespace Game.Simulation.Validation;

internal static class CombatOutcomeSummaryValidation
{
    public static void ValidateCompactDeterministicCombatOutcomeSummary()
    {
        var events = new CombatEvent[]
        {
            Event(CombatEventType.EngagementStarted, 5, 1, 101, 2, 201),
            Event(CombatEventType.DamageApplied, 5, 1, 101, 2, 201, shield: 10, armor: 5, hull: 2),
            Event(CombatEventType.DamageApplied, 5, 2, 201, 1, 101, shield: 3, armor: 4, hull: 5),
            Event(CombatEventType.FleetRetreatInitiated, 5, 2, 201, null, null),
            Event(CombatEventType.FleetEscaped, 5, 2, 201, null, null),
            Event(CombatEventType.FleetDestroyed, 5, 1, 101, 2, 201, casualties: 250.0),
            Event(CombatEventType.EngagementEnded, 5, 1, 101, 2, 201),
            Event(CombatEventType.EngagementStarted, 9, 3, 301, 1, 101),
            Event(CombatEventType.DamageApplied, 9, 3, 301, 1, 101, shield: 1, armor: 2, hull: 3),
            Event(CombatEventType.FleetEscaped, null, 4, 401, null, null),
        };

        var summary = CombatOutcomeSummaryBuilder.Build(events);
        Require(summary.EventCount == 10, "outcome summary changed event count");
        Require(summary.EngagementsStarted == 2 && summary.EngagementsEnded == 1, "engagement counts changed");
        Require(summary.DamageEvents == 3, "damage-event count changed");
        Require(summary.VesselsDestroyed == 1, "destroyed-vessel count changed");
        Require(summary.RetreatsInitiated == 1 && summary.SuccessfulEscapes == 2, "retreat/escape counts changed");
        RequireNear(summary.TotalDamageApplied, 35.0, "total damage aggregation changed");
        RequireNear(summary.EmbarkedPopulationCasualtiesMillions, 250.0, "population casualty aggregation changed");
        Require(summary.Civilizations.Select(item => item.CivilizationId).SequenceEqual(new[] { 1, 2, 3, 4 }),
            "civilization outcomes were not sorted deterministically");
        Require(summary.Systems.Count == 3 &&
                summary.Systems[0].SystemId == 5 &&
                summary.Systems[1].SystemId == 9 &&
                summary.Systems[2].SystemId is null,
            "system outcomes were not sorted with unknown/null system preserved last");

        var civ1 = summary.Civilizations[0];
        RequireNear(civ1.ShieldDamageDealt, 10.0, "civilization 1 shield damage dealt changed");
        RequireNear(civ1.ArmorDamageDealt, 5.0, "civilization 1 armor damage dealt changed");
        RequireNear(civ1.HullDamageDealt, 2.0, "civilization 1 hull damage dealt changed");
        RequireNear(civ1.TotalDamageDealt, 17.0, "civilization 1 total damage dealt changed");
        RequireNear(civ1.ShieldDamageTaken, 4.0, "civilization 1 shield damage taken changed");
        RequireNear(civ1.ArmorDamageTaken, 6.0, "civilization 1 armor damage taken changed");
        RequireNear(civ1.HullDamageTaken, 8.0, "civilization 1 hull damage taken changed");
        RequireNear(civ1.TotalDamageTaken, 18.0, "civilization 1 total damage taken changed");
        Require(civ1.EnemyVesselsDestroyed == 1 && civ1.OwnVesselsLost == 0, "civilization 1 vessel outcome changed");
        RequireNear(civ1.EmbarkedPopulationCasualtiesInflictedMillions, 250.0, "civilization 1 inflicted casualties changed");
        RequireNear(civ1.EmbarkedPopulationCasualtiesSufferedMillions, 0.0, "civilization 1 suffered casualties changed");

        var civ2 = summary.Civilizations[1];
        RequireNear(civ2.TotalDamageDealt, 12.0, "civilization 2 damage dealt changed");
        RequireNear(civ2.TotalDamageTaken, 17.0, "civilization 2 damage taken changed");
        Require(civ2.OwnVesselsLost == 1 && civ2.EnemyVesselsDestroyed == 0, "civilization 2 vessel loss changed");
        Require(civ2.RetreatsInitiated == 1 && civ2.SuccessfulEscapes == 1, "civilization 2 retreat/escape outcome changed");
        RequireNear(civ2.EmbarkedPopulationCasualtiesSufferedMillions, 250.0, "civilization 2 casualties suffered changed");

        var system5 = summary.Systems[0];
        Require(system5.EventCount == 7 && system5.DamageEvents == 2 && system5.VesselsDestroyed == 1,
            "system 5 compact event counts changed");
        RequireNear(system5.TotalDamageApplied, 29.0, "system 5 total damage changed");
        RequireNear(system5.EmbarkedPopulationCasualtiesMillions, 250.0, "system 5 population casualties changed");
        Require(system5.Civilizations.Select(item => item.CivilizationId).SequenceEqual(new[] { 1, 2 }),
            "system 5 participant set changed");

        var reversed = CombatOutcomeSummaryBuilder.Build(Enumerable.Reverse(events));
        RequireEquivalent(summary, reversed, "reversing event input changed deterministic outcome summary");

        var empty = CombatOutcomeSummaryBuilder.Build(Array.Empty<CombatEvent>());
        Require(empty.EventCount == 0 && empty.Civilizations.Count == 0 && empty.Systems.Count == 0,
            "empty Combat event input did not produce empty summary");

        var oneSided = CombatOutcomeSummaryBuilder.Build(new[]
        {
            Event(CombatEventType.FleetEscaped, 7, 55, 5501, null, null),
        });
        Require(oneSided.Civilizations.Count == 1 && oneSided.Civilizations[0].CivilizationId == 55,
            "outcome summary invented a civilization absent from the supplied event stream");

        var repeatedDamage = Enumerable.Range(0, 1024)
            .Select(index => Event(
                CombatEventType.DamageApplied,
                11,
                70,
                7000 + index,
                71,
                7100,
                hull: 1.0))
            .ToArray();
        var compressed = CombatOutcomeSummaryBuilder.Build(repeatedDamage);
        Require(compressed.EventCount == 1024 && compressed.DamageEvents == 1024,
            "large transient damage set lost event accounting");
        Require(compressed.Systems.Count == 1 && compressed.Civilizations.Count == 2,
            "large transient damage set expanded output with event count instead of participant/system count");
        Require(compressed.Systems[0].Civilizations.Count == 2,
            "large transient system outcome was not compact by civilization");
        RequireNear(compressed.TotalDamageApplied, 1024.0, "large transient damage total changed");

        var sanitized = CombatOutcomeSummaryBuilder.Build(new[]
        {
            Event(CombatEventType.DamageApplied, 12, 80, 8001, 81, 8101, shield: double.NaN, armor: -4.0, hull: 3.0),
            Event(CombatEventType.FleetDestroyed, 12, 80, 8001, 81, 8101, casualties: double.PositiveInfinity),
        });
        RequireNear(sanitized.TotalDamageApplied, 3.0, "non-finite/negative damage contaminated compact summary");
        RequireNear(sanitized.EmbarkedPopulationCasualtiesMillions, 0.0, "non-finite casualties contaminated compact summary");

        RequireThrows<ArgumentNullException>(
            () => CombatOutcomeSummaryBuilder.Build(null!),
            "null event collection was not rejected");
        RequireThrows<ArgumentException>(
            () => CombatOutcomeSummaryBuilder.Build(new CombatEvent[] { null! }),
            "null event entry was not rejected");
    }

    private static CombatEvent Event(
        CombatEventType type,
        int? systemId,
        int actorCivilizationId,
        int actorFleetId,
        int? targetCivilizationId,
        int? targetFleetId,
        double shield = 0.0,
        double armor = 0.0,
        double hull = 0.0,
        double casualties = 0.0) =>
        new(
            type,
            systemId,
            actorCivilizationId,
            actorFleetId,
            targetCivilizationId,
            targetFleetId,
            shield,
            armor,
            hull,
            "transient validation message",
            casualties);

    private static void RequireEquivalent(CombatOutcomeSummary first, CombatOutcomeSummary second, string message)
    {
        Require(first.EventCount == second.EventCount &&
                first.EngagementsStarted == second.EngagementsStarted &&
                first.EngagementsEnded == second.EngagementsEnded &&
                first.DamageEvents == second.DamageEvents &&
                first.VesselsDestroyed == second.VesselsDestroyed &&
                first.RetreatsInitiated == second.RetreatsInitiated &&
                first.SuccessfulEscapes == second.SuccessfulEscapes,
            message);
        RequireNear(first.TotalDamageApplied, second.TotalDamageApplied, message);
        RequireNear(first.EmbarkedPopulationCasualtiesMillions, second.EmbarkedPopulationCasualtiesMillions, message);
        Require(first.Civilizations.SequenceEqual(second.Civilizations), message);
        Require(first.Systems.Count == second.Systems.Count, message);
        for (var i = 0; i < first.Systems.Count; i++)
        {
            var a = first.Systems[i];
            var b = second.Systems[i];
            Require(a.SystemId == b.SystemId &&
                    a.EventCount == b.EventCount &&
                    a.EngagementsStarted == b.EngagementsStarted &&
                    a.EngagementsEnded == b.EngagementsEnded &&
                    a.DamageEvents == b.DamageEvents &&
                    a.VesselsDestroyed == b.VesselsDestroyed &&
                    a.RetreatsInitiated == b.RetreatsInitiated &&
                    a.SuccessfulEscapes == b.SuccessfulEscapes &&
                    a.Civilizations.SequenceEqual(b.Civilizations),
                message);
            RequireNear(a.TotalDamageApplied, b.TotalDamageApplied, message);
            RequireNear(a.EmbarkedPopulationCasualtiesMillions, b.EmbarkedPopulationCasualtiesMillions, message);
        }
    }

    private static void RequireThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void RequireNear(double actual, double expected, string message, double tolerance = 0.000001)
    {
        if (Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"{message}: expected {expected:0.######}, got {actual:0.######}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

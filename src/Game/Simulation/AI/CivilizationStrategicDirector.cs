using System;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.AI;

public sealed record CivilizationStrategicReview(
    CivilizationOwnState OwnState,
    CivilizationStrategicPlan Plan,
    CivilizationStrategicIntent Intent
);

/// <summary>
/// Single fair-information entry point for early-release strategic Civilization AI.
/// It composes authoritative own-state inputs, scheduled planning and subsystem-neutral intent.
/// Foreign information is supplied only through KnowledgeSnapshot.
/// </summary>
public sealed class CivilizationStrategicDirector
{
    private readonly CivilizationStrategicInputBuilder _inputBuilder;
    private readonly CivilizationStrategicPlanner _planner;
    private readonly CivilizationStrategicIntentBuilder _intentBuilder;

    public CivilizationStrategicDirector(
        CivilizationStrategicInputBuilder? inputBuilder = null,
        CivilizationStrategicPlanner? planner = null,
        CivilizationStrategicIntentBuilder? intentBuilder = null)
    {
        _inputBuilder = inputBuilder ?? new CivilizationStrategicInputBuilder();
        _planner = planner ?? new CivilizationStrategicPlanner();
        _intentBuilder = intentBuilder ?? new CivilizationStrategicIntentBuilder();
    }

    public CivilizationStrategicReview Review(
        GalaxyState galaxy,
        int civilizationId,
        CivilizationTraits traits,
        KnowledgeSnapshot knowledge,
        long nowTick,
        bool forceReview = false)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(traits);
        ArgumentNullException.ThrowIfNull(knowledge);

        if (knowledge.ObservedAtTick > nowTick)
            throw new ArgumentOutOfRangeException(nameof(knowledge), "AI knowledge snapshot cannot originate in the future.");

        if (!galaxy.Civilizations.Any(civilization => civilization.Id == civilizationId))
            throw new ArgumentOutOfRangeException(nameof(civilizationId), $"Unknown civilization {civilizationId}.");

        var ownState = _inputBuilder.Build(galaxy, civilizationId);
        var plan = _planner.GetPlan(
            civilizationId,
            traits,
            ownState,
            knowledge,
            nowTick,
            forceReview);
        var intent = _intentBuilder.Build(plan);

        return new CivilizationStrategicReview(ownState, plan, intent);
    }

    public void Invalidate(int civilizationId) => _planner.Invalidate(civilizationId);

    public void RemoveCivilization(int civilizationId) => _planner.RemoveCivilization(civilizationId);

    public void Clear() => _planner.Clear();
}

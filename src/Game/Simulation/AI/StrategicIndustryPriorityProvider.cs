using System;
using System.Collections.Generic;
using Game.Simulation.Industry;

namespace Game.Simulation.AI;

/// <summary>
/// Bridges Civilization strategic intent into Core's shared Industry weighting contract.
/// This provider never spends Industry and never changes demand; it supplies only comparative
/// construction-vs-shipbuilding weights to the authoritative Core allocator.
/// </summary>
public sealed class StrategicIndustryPriorityProvider : IIndustryPriorityProvider
{
    private readonly Dictionary<int, CivilizationStrategicIntent> _intents = new();

    public int PublishedIntentCount => _intents.Count;

    public void Publish(CivilizationStrategicReview review)
    {
        ArgumentNullException.ThrowIfNull(review);
        _intents[review.Intent.CivilizationId] = review.Intent;
    }

    public void Publish(CivilizationStrategicIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        _intents[intent.CivilizationId] = intent;
    }

    public void Remove(int civilizationId) => _intents.Remove(civilizationId);

    public void Clear() => _intents.Clear();

    public IndustryPriorityWeights GetWeights(int civilizationId)
    {
        if (!_intents.TryGetValue(civilizationId, out var intent))
            return new IndustryPriorityWeights(1.0, 1.0);

        var construction = 1.0
            + intent.GetWeight(StrategicPriorityType.StabilizeSupply) * 0.90
            + intent.GetWeight(StrategicPriorityType.ExpandIndustry) * 0.75
            + intent.GetWeight(StrategicPriorityType.ExpandResearch) * 0.35;

        var shipbuilding = 1.0
            + intent.GetWeight(StrategicPriorityType.BuildFleet) * 0.95
            + intent.GetWeight(StrategicPriorityType.Defend) * 1.15
            + intent.GetWeight(StrategicPriorityType.Explore) * 0.25
            + intent.GetWeight(StrategicPriorityType.Colonize) * 0.35;

        // Intent weights are advisory. Keep the allocator input within a narrow positive range
        // so strategy changes emphasis without becoming an invisible production multiplier.
        return new IndustryPriorityWeights(
            Math.Clamp(construction, 0.25, 4.0),
            Math.Clamp(shipbuilding, 0.25, 4.0));
    }
}

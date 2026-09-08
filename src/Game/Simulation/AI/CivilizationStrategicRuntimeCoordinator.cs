using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Industry;
using Game.Simulation.Models;
using Game.Simulation.Shipbuilding;

namespace Game.Simulation.AI;

/// <summary>
/// Bounded runtime bridge for the early-release Civilization strategic planner.
///
/// Until Diplomacy/intelligence has an authoritative persisted runtime owner, this bridge
/// intentionally supplies an empty foreign-information snapshot. That lets non-player
/// civilizations coordinate their own supply, industry, research, exploration, colonization
/// and fleet-capacity priorities without inventing enemy knowledge or peeking at hidden state.
///
/// Strategic reviews are derived state and are not persisted. A campaign change clears the
/// planner/provider cache, and each civilization is reviewed only when its scheduled review
/// boundary is reached.
/// </summary>
public sealed class CivilizationStrategicRuntimeCoordinator : IShipbuildingStrategicPreferenceView
{
    private readonly CivilizationStrategicDirector _director;
    private readonly StrategicIndustryPriorityProvider _industryPriorities;
    private readonly Dictionary<int, long> _nextReviewTick = new();
    private long? _campaignSeed;
    private double _strategicDays;

    public CivilizationStrategicRuntimeCoordinator(
        CivilizationStrategicDirector? director = null,
        StrategicIndustryPriorityProvider? industryPriorities = null)
    {
        _director = director ?? new CivilizationStrategicDirector();
        _industryPriorities = industryPriorities ?? new StrategicIndustryPriorityProvider();
    }

    public IIndustryPriorityProvider IndustryPriorityProvider => _industryPriorities;
    public int PublishedIntentCount => _industryPriorities.PublishedIntentCount;

    /// <summary>
    /// Advances the derived strategic review clock and publishes only reviews that reached
    /// their scheduled boundary. Player and seeded-ancient civilizations remain untouched.
    /// </summary>
    public IReadOnlyList<CivilizationStrategicReview> Advance(GalaxyState galaxy, double simulationDays)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (!double.IsFinite(simulationDays) || simulationDays < 0.0)
            throw new ArgumentOutOfRangeException(nameof(simulationDays), "Strategic time must be finite and non-negative.");

        EnsureCampaign(galaxy.Seed);
        _strategicDays += simulationDays;
        var nowTick = Math.Max(0L, (long)Math.Floor(_strategicDays));

        var knowledge = new KnowledgeSnapshot
        {
            ObservedAtTick = nowTick,
            Civilizations = new Dictionary<int, KnownCivilization>(),
        };

        var reviews = new List<CivilizationStrategicReview>();
        foreach (var civilization in galaxy.Civilizations
                     .Where(civilization => !civilization.IsPlayer && !civilization.IsSeededAncient)
                     .OrderBy(civilization => civilization.Id))
        {
            if (_nextReviewTick.TryGetValue(civilization.Id, out var nextTick) && nowTick < nextTick)
                continue;

            var review = _director.Review(
                galaxy,
                civilization.Id,
                civilization.Traits,
                knowledge,
                nowTick,
                forceReview: false);

            _industryPriorities.Publish(review);
            _nextReviewTick[civilization.Id] = review.Plan.ReviewAfterTick;
            reviews.Add(review);
        }

        return reviews;
    }

    public IndustryPriorityWeights GetIndustryWeights(int civilizationId) =>
        _industryPriorities.GetWeights(civilizationId);

    public ShipbuildingStrategicPreference GetPreference(int civilizationId)
    {
        if (!_industryPriorities.TryGetIntent(civilizationId, out var intent))
            return ShipbuildingStrategicPreference.None;

        return new ShipbuildingStrategicPreference(
            intent.PreferredNewFleetRole,
            intent.DeferNewColonization);
    }

    public void Reset()
    {
        _campaignSeed = null;
        _strategicDays = 0.0;
        _nextReviewTick.Clear();
        _director.Clear();
        _industryPriorities.Clear();
    }

    private void EnsureCampaign(long seed)
    {
        if (_campaignSeed == seed)
            return;

        _campaignSeed = seed;
        _strategicDays = 0.0;
        _nextReviewTick.Clear();
        _director.Clear();
        _industryPriorities.Clear();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Simulation.Economy;

public sealed record LogisticsSupplyOffer(
    int NodeId,
    double AvailablePerDay
);

public sealed record LogisticsDemand(
    int NodeId,
    double RequiredPerDay,
    int Priority = 0
);

public sealed record LogisticsFlowAllocation(
    int SourceNodeId,
    int DestinationNodeId,
    double AllocatedPerDay,
    double TransitDays,
    IReadOnlyList<int> RouteLinkIds
);

public sealed record LogisticsFlowPlan(
    IReadOnlyList<LogisticsFlowAllocation> Allocations,
    IReadOnlyDictionary<int, double> UnmetDemandPerDay,
    IReadOnlyDictionary<int, double> UnusedSupplyPerDay
)
{
    public double TotalAllocatedPerDay => Allocations.Sum(allocation => allocation.AllocatedPerDay);
    public double TotalUnmetDemandPerDay => UnmetDemandPerDay.Values.Sum();
}

/// <summary>
/// Bounded aggregate daily supply allocator. This is intentionally not a shipment-object queue
/// or an expensive global multicommodity solver. It greedily serves higher-priority demands,
/// preferring shorter routes while tracking shared link capacity so no corridor is oversubscribed.
/// </summary>
public sealed class LogisticsFlowAllocator
{
    private readonly LogisticsRoutePlanner _routePlanner;
    private readonly IReadOnlyDictionary<int, LogisticsLink> _links;

    public LogisticsFlowAllocator(
        LogisticsRoutePlanner routePlanner,
        IReadOnlyCollection<LogisticsLink> links)
    {
        _routePlanner = routePlanner ?? throw new ArgumentNullException(nameof(routePlanner));
        ArgumentNullException.ThrowIfNull(links);
        _links = links.ToDictionary(link => link.Id);
    }

    public LogisticsFlowPlan AllocateDaily(
        IReadOnlyCollection<LogisticsSupplyOffer> supplyOffers,
        IReadOnlyCollection<LogisticsDemand> demands)
    {
        ArgumentNullException.ThrowIfNull(supplyOffers);
        ArgumentNullException.ThrowIfNull(demands);

        var remainingSupply = ValidateAndAggregateSupply(supplyOffers);
        var remainingLinkCapacity = _links.Values.ToDictionary(
            link => link.Id,
            link => link.Enabled ? Math.Max(0.0, link.CapacityPerDay) : 0.0);
        var unmetDemand = new Dictionary<int, double>();
        var allocations = new List<LogisticsFlowAllocation>();

        foreach (var demand in demands
                     .OrderByDescending(item => item.Priority)
                     .ThenBy(item => item.NodeId))
        {
            ValidateFiniteNonNegative(demand.RequiredPerDay, nameof(LogisticsDemand.RequiredPerDay), demand.NodeId);
            var remainingDemand = demand.RequiredPerDay;

            if (remainingDemand <= 0.0)
            {
                unmetDemand[demand.NodeId] = 0.0;
                continue;
            }

            var candidates = new List<RouteCandidate>();
            foreach (var offer in remainingSupply)
            {
                if (offer.Value <= 0.0 || offer.Key == demand.NodeId)
                    continue;

                var route = _routePlanner.FindRoute(offer.Key, demand.NodeId);
                if (route is null)
                    continue;

                var remainingRouteCapacity = GetRemainingRouteCapacity(route, remainingLinkCapacity);
                if (remainingRouteCapacity <= 0.0)
                    continue;

                candidates.Add(new RouteCandidate(
                    offer.Key,
                    route,
                    remainingRouteCapacity));
            }

            foreach (var candidate in candidates
                         .OrderBy(item => item.Route.TransitDays)
                         .ThenByDescending(item => item.RemainingRouteCapacity)
                         .ThenBy(item => item.SourceNodeId))
            {
                if (remainingDemand <= 0.0)
                    break;

                var sourceAvailable = remainingSupply[candidate.SourceNodeId];
                if (sourceAvailable <= 0.0)
                    continue;

                var routeCapacity = GetRemainingRouteCapacity(candidate.Route, remainingLinkCapacity);
                var amount = Math.Min(remainingDemand, Math.Min(sourceAvailable, routeCapacity));
                if (amount <= 0.0)
                    continue;

                remainingSupply[candidate.SourceNodeId] -= amount;
                remainingDemand -= amount;
                foreach (var linkId in candidate.Route.LinkIds)
                    remainingLinkCapacity[linkId] = Math.Max(0.0, remainingLinkCapacity[linkId] - amount);

                allocations.Add(new LogisticsFlowAllocation(
                    candidate.SourceNodeId,
                    demand.NodeId,
                    amount,
                    candidate.Route.TransitDays,
                    candidate.Route.LinkIds));
            }

            unmetDemand[demand.NodeId] = Math.Max(0.0, remainingDemand);
        }

        return new LogisticsFlowPlan(
            allocations,
            unmetDemand,
            remainingSupply);
    }

    private static Dictionary<int, double> ValidateAndAggregateSupply(
        IReadOnlyCollection<LogisticsSupplyOffer> supplyOffers)
    {
        var supply = new Dictionary<int, double>();
        foreach (var offer in supplyOffers)
        {
            ValidateFiniteNonNegative(offer.AvailablePerDay, nameof(LogisticsSupplyOffer.AvailablePerDay), offer.NodeId);
            supply.TryGetValue(offer.NodeId, out var existing);
            supply[offer.NodeId] = existing + offer.AvailablePerDay;
        }

        return supply;
    }

    private double GetRemainingRouteCapacity(
        LogisticsRoutePlan route,
        IReadOnlyDictionary<int, double> remainingLinkCapacity)
    {
        if (route.LinkIds.Count == 0)
            return double.PositiveInfinity;

        var capacity = double.PositiveInfinity;
        foreach (var linkId in route.LinkIds)
        {
            if (!_links.ContainsKey(linkId))
                throw new InvalidOperationException($"Route references logistics link {linkId} not known by the allocator.");
            if (!remainingLinkCapacity.TryGetValue(linkId, out var remaining))
                throw new InvalidOperationException($"No remaining-capacity state exists for logistics link {linkId}.");
            capacity = Math.Min(capacity, remaining);
        }

        return capacity;
    }

    private static void ValidateFiniteNonNegative(double value, string field, int nodeId)
    {
        if (!double.IsFinite(value) || value < 0.0)
            throw new ArgumentOutOfRangeException(field, $"Node {nodeId} has invalid logistics quantity {value}.");
    }

    private sealed record RouteCandidate(
        int SourceNodeId,
        LogisticsRoutePlan Route,
        double RemainingRouteCapacity
    );
}

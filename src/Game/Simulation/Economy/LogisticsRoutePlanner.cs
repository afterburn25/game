using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Simulation.Economy;

public enum LogisticsNodeKind
{
    Homeworld,
    OrbitalHub,
    LunarSettlement,
    PlanetarySettlement,
    ResourceSite,
    Depot,
    Shipyard,
}

/// <summary>
/// Strategic logistics node. This is an aggregate installation/settlement, never one
/// simulated warehouse, vehicle, worker or individual person.
/// </summary>
public sealed record LogisticsNode(
    int Id,
    int CivilizationId,
    int SystemId,
    string Name,
    LogisticsNodeKind Kind
);

/// <summary>
/// Aggregate cargo corridor between strategic logistics nodes. Capacity is expressed in
/// strategic cargo units/day and transit time in simulation days.
/// </summary>
public sealed record LogisticsLink(
    int Id,
    int CivilizationId,
    int FromNodeId,
    int ToNodeId,
    double CapacityPerDay,
    double TransitDays,
    bool Bidirectional = true,
    bool Enabled = true
);

public sealed record LogisticsRoutePlan(
    int SourceNodeId,
    int DestinationNodeId,
    IReadOnlyList<int> NodeIds,
    IReadOnlyList<int> LinkIds,
    double TransitDays,
    double BottleneckCapacityPerDay
);

/// <summary>
/// On-demand shortest-transit logistics routing with a strictly bounded LRU cache.
/// The planner is immutable with respect to nodes/links: rebuild it when infrastructure or
/// access changes. This makes invalidation explicit and prevents stale route state.
/// </summary>
public sealed class LogisticsRoutePlanner
{
    private readonly IReadOnlyDictionary<int, LogisticsNode> _nodes;
    private readonly IReadOnlyDictionary<int, LogisticsLink> _links;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<RouteEdge>> _adjacency;
    private readonly int _cacheCapacity;
    private readonly Dictionary<RouteKey, CacheEntry> _cache = new();
    private readonly LinkedList<RouteKey> _lru = new();

    public LogisticsRoutePlanner(
        IReadOnlyCollection<LogisticsNode> nodes,
        IReadOnlyCollection<LogisticsLink> links,
        int cacheCapacity = 256)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(links);
        if (cacheCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(cacheCapacity));

        _cacheCapacity = cacheCapacity;
        _nodes = nodes.ToDictionary(node => node.Id);
        _links = links.ToDictionary(link => link.Id);
        _adjacency = BuildAdjacency(_nodes, _links);
    }

    public int CachedRouteCount => _cache.Count;
    public int CacheCapacity => _cacheCapacity;

    public LogisticsRoutePlan? FindRoute(int sourceNodeId, int destinationNodeId)
    {
        if (!_nodes.ContainsKey(sourceNodeId))
            throw new ArgumentOutOfRangeException(nameof(sourceNodeId), $"Unknown logistics node {sourceNodeId}.");
        if (!_nodes.ContainsKey(destinationNodeId))
            throw new ArgumentOutOfRangeException(nameof(destinationNodeId), $"Unknown logistics node {destinationNodeId}.");

        var source = _nodes[sourceNodeId];
        var destination = _nodes[destinationNodeId];
        if (source.CivilizationId != destination.CivilizationId)
            return null;

        if (sourceNodeId == destinationNodeId)
        {
            return new LogisticsRoutePlan(
                sourceNodeId,
                destinationNodeId,
                new[] { sourceNodeId },
                Array.Empty<int>(),
                0.0,
                double.PositiveInfinity);
        }

        var key = new RouteKey(sourceNodeId, destinationNodeId);
        if (_cache.TryGetValue(key, out var cached))
        {
            Touch(key, cached);
            return cached.Plan;
        }

        var plan = ComputeRoute(sourceNodeId, destinationNodeId, source.CivilizationId);
        AddToCache(key, plan);
        return plan;
    }

    private LogisticsRoutePlan? ComputeRoute(int sourceNodeId, int destinationNodeId, int civilizationId)
    {
        var distances = new Dictionary<int, double> { [sourceNodeId] = 0.0 };
        var previous = new Dictionary<int, RouteEdge>();
        var queue = new PriorityQueue<int, double>();
        queue.Enqueue(sourceNodeId, 0.0);

        while (queue.TryDequeue(out var current, out var currentDistance))
        {
            if (current == destinationNodeId)
                break;

            if (distances.TryGetValue(current, out var bestKnown) && currentDistance > bestKnown)
                continue;

            if (!_adjacency.TryGetValue(current, out var edges))
                continue;

            foreach (var edge in edges)
            {
                var link = _links[edge.LinkId];
                if (!link.Enabled || link.CivilizationId != civilizationId || link.CapacityPerDay <= 0.0 || link.TransitDays < 0.0)
                    continue;

                var nextDistance = currentDistance + link.TransitDays;
                if (distances.TryGetValue(edge.ToNodeId, out var existing) && nextDistance >= existing)
                    continue;

                distances[edge.ToNodeId] = nextDistance;
                previous[edge.ToNodeId] = edge;
                queue.Enqueue(edge.ToNodeId, nextDistance);
            }
        }

        if (!distances.TryGetValue(destinationNodeId, out var transitDays))
            return null;

        var nodeIds = new List<int> { destinationNodeId };
        var linkIds = new List<int>();
        var cursor = destinationNodeId;
        var bottleneck = double.PositiveInfinity;

        while (cursor != sourceNodeId)
        {
            if (!previous.TryGetValue(cursor, out var edge))
                return null;

            var link = _links[edge.LinkId];
            linkIds.Add(link.Id);
            bottleneck = Math.Min(bottleneck, link.CapacityPerDay);
            cursor = edge.FromNodeId;
            nodeIds.Add(cursor);
        }

        nodeIds.Reverse();
        linkIds.Reverse();

        return new LogisticsRoutePlan(
            sourceNodeId,
            destinationNodeId,
            nodeIds,
            linkIds,
            transitDays,
            bottleneck);
    }

    private void AddToCache(RouteKey key, LogisticsRoutePlan? plan)
    {
        if (_cache.ContainsKey(key))
            return;

        while (_cache.Count >= _cacheCapacity && _lru.First is { } oldest)
        {
            _cache.Remove(oldest.Value);
            _lru.RemoveFirst();
        }

        var node = _lru.AddLast(key);
        _cache[key] = new CacheEntry(plan, node);
    }

    private void Touch(RouteKey key, CacheEntry entry)
    {
        _lru.Remove(entry.LruNode);
        entry.LruNode = _lru.AddLast(key);
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<RouteEdge>> BuildAdjacency(
        IReadOnlyDictionary<int, LogisticsNode> nodes,
        IReadOnlyDictionary<int, LogisticsLink> links)
    {
        var adjacency = nodes.Keys.ToDictionary(id => id, _ => new List<RouteEdge>());

        foreach (var link in links.Values)
        {
            if (!nodes.TryGetValue(link.FromNodeId, out var from))
                throw new InvalidOperationException($"Logistics link {link.Id} references unknown source node {link.FromNodeId}.");
            if (!nodes.TryGetValue(link.ToNodeId, out var to))
                throw new InvalidOperationException($"Logistics link {link.Id} references unknown destination node {link.ToNodeId}.");
            if (from.CivilizationId != link.CivilizationId || to.CivilizationId != link.CivilizationId)
                throw new InvalidOperationException($"Logistics link {link.Id} crosses civilization ownership; treaty logistics requires an explicit future contract.");
            if (!double.IsFinite(link.CapacityPerDay) || link.CapacityPerDay < 0.0)
                throw new InvalidOperationException($"Logistics link {link.Id} has invalid cargo capacity.");
            if (!double.IsFinite(link.TransitDays) || link.TransitDays < 0.0)
                throw new InvalidOperationException($"Logistics link {link.Id} has invalid transit time.");

            adjacency[link.FromNodeId].Add(new RouteEdge(link.FromNodeId, link.ToNodeId, link.Id));
            if (link.Bidirectional)
                adjacency[link.ToNodeId].Add(new RouteEdge(link.ToNodeId, link.FromNodeId, link.Id));
        }

        return adjacency.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<RouteEdge>)pair.Value.OrderBy(edge => edge.LinkId).ToArray());
    }

    private readonly record struct RouteKey(int SourceNodeId, int DestinationNodeId);
    private sealed record RouteEdge(int FromNodeId, int ToNodeId, int LinkId);

    private sealed class CacheEntry
    {
        public CacheEntry(LogisticsRoutePlan? plan, LinkedListNode<RouteKey> lruNode)
        {
            Plan = plan;
            LruNode = lruNode;
        }

        public LogisticsRoutePlan? Plan { get; }
        public LinkedListNode<RouteKey> LruNode { get; set; }
    }
}

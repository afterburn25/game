using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Game.Simulation.Models;

namespace Game.Simulation.Exploration;

public static class AstronomicalDistance
{
    public const double LightYearsPerParsec = 3.26156;
    public const double KilometresPerAu = 149_597_870.7;
    public static double LightYearsToParsecs(double lightYears) => lightYears / LightYearsPerParsec;
    public static double AuToKilometres(double au) => au * KilometresPerAu;
}

public sealed record InterstellarLane(int FirstSystemId, int SecondSystemId, double LengthLightYears)
{
    public bool Connects(int systemId) => FirstSystemId == systemId || SecondSystemId == systemId;
    public int Other(int systemId) => FirstSystemId == systemId ? SecondSystemId :
        SecondSystemId == systemId ? FirstSystemId : throw new ArgumentOutOfRangeException(nameof(systemId));
}

public sealed record RemainingFleetRoute(int RemainingLegs, double DistanceLightYears);

public static class FleetRouteMetrics
{
    public static RemainingFleetRoute Measure(GalaxyState galaxy, FleetState fleet)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(fleet);
        if (fleet.DestinationSystemId is not int destinationSystemId)
            return new RemainingFleetRoute(0, 0.0);

        IEnumerable<int> routeIds = fleet.PlannedRouteSystemIds.Count > 0
            ? fleet.PlannedRouteSystemIds
            : new[] { destinationSystemId };
        var systems = galaxy.Systems.ToDictionary(system => system.Id);
        var position = fleet.Position;
        var total = 0.0;
        var legs = 0;
        foreach (var waypointId in routeIds)
        {
            if (!systems.TryGetValue(waypointId, out var waypoint))
                continue;
            total += Vector2.Distance(position, waypoint.Position);
            position = waypoint.Position;
            legs++;
        }
        return new RemainingFleetRoute(legs, total);
    }
}

/// <summary>Deterministic sparse graph: a minimum-distance backbone plus bounded local alternatives.</summary>
public sealed class InterstellarLaneNetwork
{
    // Star records are immutable, but callers may replace entries in an IReadOnlyList backed
    // by a mutable collection. Validate record identities before reusing derived geometry.
    // Weak keys release the graph when its campaign is discarded. Fuel, ownership, survey
    // knowledge and mission acceptance are deliberately never cached here.
    private static readonly ConditionalWeakTable<IReadOnlyList<StarSystemState>, CachedGraph> Graphs = new();
    private const int MaximumCachedRouteTrees = 64;

    private sealed class CachedGraph
    {
        public StarSystemState[] Systems = Array.Empty<StarSystemState>();
        public IReadOnlyList<InterstellarLane> Lanes = Array.Empty<InterstellarLane>();
        public HashSet<int> SystemIds = new();
        public Dictionary<int, InterstellarLane[]> Adjacency = new();
        public readonly Dictionary<(int Origin, double Range), RouteTree> Routes = new();

        public void EnsureCurrent(IReadOnlyList<StarSystemState> systems)
        {
            var matches = Systems.Length == systems.Count;
            for (var i = 0; matches && i < systems.Count; i++)
                matches = ReferenceEquals(Systems[i], systems[i]);
            if (matches) return;
            Systems = systems.ToArray();
            Lanes = BuildUncached(Systems);
            SystemIds = Systems.Select(system => system.Id).ToHashSet();
            Adjacency = SystemIds.ToDictionary(id => id, id => Lanes.Where(lane => lane.Connects(id)).ToArray());
            Routes.Clear();
        }
    }

    private sealed record RouteTree(Dictionary<int, double> Distance, Dictionary<int, int> Prior);

    public IReadOnlyList<InterstellarLane> Build(IReadOnlyList<StarSystemState> systems)
    {
        ArgumentNullException.ThrowIfNull(systems);
        var graph = Graphs.GetValue(systems, _ => new CachedGraph());
        lock (graph)
        {
            graph.EnsureCurrent(systems);
            return graph.Lanes;
        }
    }

    private static IReadOnlyList<InterstellarLane> BuildUncached(IReadOnlyList<StarSystemState> systems)
    {
        if (systems.Count < 2) return Array.Empty<InterstellarLane>();
        var ordered = systems.OrderBy(system => system.Id).ToArray();
        var byId = ordered.ToDictionary(system => system.Id);
        var edges = new HashSet<(int First, int Second)>();
        var connected = new HashSet<int> { ordered[0].Id };

        while (connected.Count < ordered.Length)
        {
            var best = (First: -1, Second: -1, Distance: double.PositiveInfinity);
            foreach (var firstId in connected.OrderBy(id => id))
            foreach (var second in ordered.Where(system => !connected.Contains(system.Id)))
            {
                var distance = Vector2.Distance(byId[firstId].Position, second.Position);
                if (distance < best.Distance - 1e-9 || Math.Abs(distance - best.Distance) <= 1e-9 &&
                    (firstId < best.First || firstId == best.First && second.Id < best.Second))
                    best = (firstId, second.Id, distance);
            }
            edges.Add(Canonical(best.First, best.Second));
            connected.Add(best.Second);
        }

        foreach (var system in ordered)
        {
            foreach (var neighbour in ordered.Where(candidate => candidate.Id != system.Id)
                         .OrderBy(candidate => Vector2.DistanceSquared(system.Position, candidate.Position))
                         .ThenBy(candidate => candidate.Id).Take(3))
                edges.Add(Canonical(system.Id, neighbour.Id));
        }

        return Array.AsReadOnly(edges.OrderBy(edge => edge.First).ThenBy(edge => edge.Second)
            .Select(edge => new InterstellarLane(edge.First, edge.Second,
                Vector2.Distance(byId[edge.First].Position, byId[edge.Second].Position)))
            .ToArray());
    }

    public IReadOnlyList<int> FindShortestRoute(
        IReadOnlyList<StarSystemState> systems,
        int originSystemId,
        int destinationSystemId,
        double maximumLegRangeLightYears = double.PositiveInfinity,
        IReadOnlySet<int>? permittedSystemIds = null)
    {
        if (maximumLegRangeLightYears <= 0.0 || double.IsNaN(maximumLegRangeLightYears))
            return Array.Empty<int>();
        ArgumentNullException.ThrowIfNull(systems);
        var graph = Graphs.GetValue(systems, _ => new CachedGraph());
        lock (graph)
        {
            graph.EnsureCurrent(systems);
            if (!graph.SystemIds.Contains(originSystemId) || !graph.SystemIds.Contains(destinationSystemId))
                throw new ArgumentOutOfRangeException(nameof(destinationSystemId));
            if (permittedSystemIds is not null &&
                (!permittedSystemIds.Contains(originSystemId) || !permittedSystemIds.Contains(destinationSystemId)))
                return Array.Empty<int>();

            RouteTree tree;
            var key = (originSystemId, maximumLegRangeLightYears);
            // Permission sets may change in place; always evaluate them afresh. The common
            // unrestricted geometry query shares a shortest-path tree across destinations.
            if (permittedSystemIds is not null)
                tree = BuildRouteTree(graph, originSystemId, maximumLegRangeLightYears, permittedSystemIds);
            else if (!graph.Routes.TryGetValue(key, out tree!))
            {
                tree = BuildRouteTree(graph, originSystemId, maximumLegRangeLightYears, null);
                if (graph.Routes.Count >= MaximumCachedRouteTrees) graph.Routes.Clear();
                graph.Routes.Add(key, tree);
            }
            if (!tree.Distance.TryGetValue(destinationSystemId, out var distance) || !double.IsFinite(distance))
                return Array.Empty<int>();
            var route = new List<int> { destinationSystemId };
            while (route[^1] != originSystemId) route.Add(tree.Prior[route[^1]]);
            route.Reverse();
            return route.AsReadOnly();
        }
    }

    private static RouteTree BuildRouteTree(CachedGraph graph, int originSystemId,
        double maximumLegRangeLightYears, IReadOnlySet<int>? permittedSystemIds)
    {
        var traversableIds = permittedSystemIds is null
            ? graph.SystemIds
            : graph.SystemIds.Where(permittedSystemIds.Contains).ToHashSet();
        var distance = traversableIds.ToDictionary(id => id, _ => double.PositiveInfinity);
        var prior = new Dictionary<int, int>();
        var remaining = new HashSet<int>(traversableIds);
        distance[originSystemId] = 0;
        while (remaining.Count > 0)
        {
            var current = remaining.OrderBy(id => distance[id]).ThenBy(id => id).First();
            if (!double.IsFinite(distance[current])) break;
            remaining.Remove(current);
            foreach (var lane in graph.Adjacency[current])
            {
                if (lane.LengthLightYears > maximumLegRangeLightYears + 1e-9) continue;
                var next = lane.Other(current);
                if (!remaining.Contains(next)) continue;
                var candidate = distance[current] + lane.LengthLightYears;
                if (candidate < distance[next] - 1e-9)
                {
                    distance[next] = candidate;
                    prior[next] = current;
                }
            }
        }
        return new RouteTree(distance, prior);
    }

    private static (int First, int Second) Canonical(int first, int second) =>
        first < second ? (first, second) : (second, first);
}

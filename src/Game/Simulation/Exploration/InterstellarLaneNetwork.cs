using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

/// <summary>Deterministic sparse graph: a minimum-distance backbone plus bounded local alternatives.</summary>
public sealed class InterstellarLaneNetwork
{
    public IReadOnlyList<InterstellarLane> Build(IReadOnlyList<StarSystemState> systems)
    {
        ArgumentNullException.ThrowIfNull(systems);
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

        return edges.OrderBy(edge => edge.First).ThenBy(edge => edge.Second)
            .Select(edge => new InterstellarLane(edge.First, edge.Second,
                Vector2.Distance(byId[edge.First].Position, byId[edge.Second].Position)))
            .ToArray();
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
        var lanes = Build(systems);
        var systemIds = systems.Select(system => system.Id).ToHashSet();
        if (!systemIds.Contains(originSystemId) || !systemIds.Contains(destinationSystemId))
            throw new ArgumentOutOfRangeException(nameof(destinationSystemId));
        var traversableIds = permittedSystemIds is null
            ? systemIds
            : systemIds.Where(permittedSystemIds.Contains).ToHashSet();
        if (!traversableIds.Contains(originSystemId) || !traversableIds.Contains(destinationSystemId))
            return Array.Empty<int>();
        var distance = traversableIds.ToDictionary(id => id, _ => double.PositiveInfinity);
        var prior = new Dictionary<int, int>();
        var remaining = new HashSet<int>(traversableIds);
        distance[originSystemId] = 0;
        while (remaining.Count > 0)
        {
            var current = remaining.OrderBy(id => distance[id]).ThenBy(id => id).First();
            if (!double.IsFinite(distance[current]) || current == destinationSystemId) break;
            remaining.Remove(current);
            foreach (var lane in lanes.Where(lane =>
                         lane.Connects(current) && lane.LengthLightYears <= maximumLegRangeLightYears + 1e-9))
            {
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
        if (!double.IsFinite(distance[destinationSystemId])) return Array.Empty<int>();
        var route = new List<int> { destinationSystemId };
        while (route[^1] != originSystemId) route.Add(prior[route[^1]]);
        route.Reverse();
        return route;
    }

    private static (int First, int Second) Canonical(int first, int second) =>
        first < second ? (first, second) : (second, first);
}

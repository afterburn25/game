using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Diplomacy;
using Game.Simulation.Models;

namespace Game.Presentation;

/// <summary>Cached observer-safe political ownership cells. Claims stay separate from ownership.</summary>
public sealed class StrategicTerritoryProjection
{
    public IReadOnlyList<StrategicTerritoryRegion> Territories { get; }
    public IReadOnlyList<StrategicTerritoryClaimOutline> Claims { get; }
    public IReadOnlyList<StrategicTerritoryFillRun> FogRuns { get; }
    public IReadOnlyList<IReadOnlyList<Vector2>> FogContours { get; }
    public IReadOnlySet<int> UnexploredSystemIds { get; }
    public int UnownedCellCount { get; }
    public int GridCellCount { get; }
    private StrategicTerritoryProjection(
        IReadOnlyList<StrategicTerritoryRegion> territories,
        IReadOnlyList<StrategicTerritoryClaimOutline> claims,
        IReadOnlyList<StrategicTerritoryFillRun> fogRuns,
        IReadOnlyList<IReadOnlyList<Vector2>> fogContours,
        IReadOnlySet<int> unexplored,
        int unownedCellCount,
        int gridCellCount)
    {
        Territories = territories;
        Claims = claims;
        FogRuns = fogRuns;
        FogContours = fogContours;
        UnexploredSystemIds = unexplored;
        UnownedCellCount = unownedCellCount;
        GridCellCount = gridCellCount;
    }

    public static StrategicTerritoryProjection Build(GalaxyState galaxy, int observerId, IReadOnlyList<TerritorialClaimSnapshot>? observerClaims = null)
    {
        var systems = galaxy.Systems.ToDictionary(system => system.Id);
        var civilizations = galaxy.Civilizations.ToDictionary(civilization => civilization.Id);
        var settlementOwners = galaxy.Colonies
            .GroupBy(colony => colony.SystemId)
            .ToDictionary(group => group.Key, group => group.OrderBy(colony => colony.Id).First().CivilizationId);
        var anchors = new Dictionary<(int, int), StrategicTerritoryAnchor>();

        bool Visible(int civilizationId, int systemId) =>
            civilizationId == observerId
            || galaxy.Knowledge.IsCivilizationKnown(observerId, civilizationId)
            && galaxy.Knowledge.IsSystemFullySurveyed(observerId, systemId);

        void Add(int civilizationId, int systemId, StrategicTerritoryAnchorKind kind)
        {
            if (!systems.TryGetValue(systemId, out var star) || !Visible(civilizationId, systemId)) return;
            var key = (civilizationId, systemId);
            if (!anchors.TryGetValue(key, out var previous) || kind < previous.Kind)
                anchors[key] = new(civilizationId, systemId, star.Position, kind);
        }

        // A current settlement authority supersedes a former civilization's natal marker.
        foreach (var civilization in civilizations.Values)
            if (!settlementOwners.TryGetValue(civilization.HomeSystemId, out var owner)
                || owner == civilization.Id
                || !Visible(owner, civilization.HomeSystemId))
                Add(civilization.Id, civilization.HomeSystemId, StrategicTerritoryAnchorKind.Home);
        foreach (var colony in galaxy.Colonies)
            Add(colony.CivilizationId, colony.SystemId, StrategicTerritoryAnchorKind.Settlement);

        var all = anchors.Values.OrderBy(anchor => anchor.CivilizationId).ThenBy(anchor => anchor.SystemId).ToArray();
        var grid = TerritoryGrid.Create(galaxy.Systems);
        var radii = all.ToDictionary(
            anchor => (anchor.CivilizationId, anchor.SystemId),
            anchor => Math.Max(Radius(anchor, all), grid.CellSize * .72f));
        var cells = Assign(grid, all, radii);
        PreserveVisibleOwners(grid, cells, all);
        var regions = Regions(grid, cells, all, civilizations);
        var claims = new List<StrategicTerritoryClaimOutline>();
        foreach (var claim in observerClaims ?? Array.Empty<TerritorialClaimSnapshot>())
            if (claim.Active
                && systems.TryGetValue(claim.SystemId, out var star)
                && civilizations.ContainsKey(claim.ClaimantCivilizationId)
                && Visible(claim.ClaimantCivilizationId, claim.SystemId))
                claims.Add(new(claim.ClaimantCivilizationId, claim.SystemId, star.Position, Math.Max(28f, grid.CellSize * 1.3f)));

        var unknown = galaxy.Systems
            .Where(system => !galaxy.Knowledge.IsSystemKnown(observerId, system.Id))
            .Select(system => system.Id)
            .ToHashSet();
        var unowned = cells.Cast<int>().Count(owner => owner < 0);
        var fogCells = BuildFogCells(grid, galaxy.Systems, unknown);
        return new(
            regions,
            claims.OrderBy(x => x.CivilizationId).ThenBy(x => x.SystemId).ToArray(),
            Runs(grid, fogCells, 1),
            Contours(grid, fogCells, 1),
            unknown,
            unowned,
            grid.Width * grid.Height);
    }

    private static int[,] Assign(TerritoryGrid grid, IReadOnlyList<StrategicTerritoryAnchor> anchors, IReadOnlyDictionary<(int CivilizationId, int SystemId), float> radii)
    {
        var cells = new int[grid.Width, grid.Height];
        for (var x = 0; x < grid.Width; x++)
            for (var y = 0; y < grid.Height; y++)
            {
                var bestOwner = -1;
                var bestScore = 0f;
                var point = grid.Center(x, y);
                foreach (var anchor in anchors)
                {
                    var score = radii[(anchor.CivilizationId, anchor.SystemId)] - Vector2.Distance(point, anchor.Position);
                    if (score > bestScore || score == bestScore && score > 0 && (bestOwner < 0 || anchor.CivilizationId < bestOwner))
                    {
                        bestScore = score;
                        bestOwner = anchor.CivilizationId;
                    }
                }
                cells[x, y] = bestOwner;
            }
        return cells;
    }

    private static void PreserveVisibleOwners(TerritoryGrid grid, int[,] cells, IReadOnlyList<StrategicTerritoryAnchor> anchors)
    {
        var counts = new Dictionary<int, int>();
        for (var x = 0; x < grid.Width; x++)
            for (var y = 0; y < grid.Height; y++)
            {
                var owner = cells[x, y];
                if (owner >= 0) counts[owner] = counts.GetValueOrDefault(owner) + 1;
            }

        foreach (var group in anchors.GroupBy(anchor => anchor.CivilizationId).OrderBy(group => group.Key))
        {
            if (counts.GetValueOrDefault(group.Key) > 0) continue;

            var bestX = -1;
            var bestY = -1;
            var bestDistance = float.PositiveInfinity;
            for (var x = 0; x < grid.Width; x++)
                for (var y = 0; y < grid.Height; y++)
                {
                    var previousOwner = cells[x, y];
                    if (previousOwner >= 0 && counts.GetValueOrDefault(previousOwner) <= 1) continue;

                    var center = grid.Center(x, y);
                    var distance = group.Min(anchor => Vector2.DistanceSquared(center, anchor.Position));
                    if (distance >= bestDistance) continue;
                    bestDistance = distance;
                    bestX = x;
                    bestY = y;
                }

            if (bestX < 0) continue;
            var displacedOwner = cells[bestX, bestY];
            if (displacedOwner >= 0) counts[displacedOwner]--;
            cells[bestX, bestY] = group.Key;
            counts[group.Key] = 1;
        }
    }
    private static float Radius(StrategicTerritoryAnchor anchor, IReadOnlyList<StrategicTerritoryAnchor> anchors)
    {
        var nearestFriendly = float.PositiveInfinity;
        foreach (var other in anchors)
            if (other != anchor && other.CivilizationId == anchor.CivilizationId)
                nearestFriendly = Math.Min(nearestFriendly, Vector2.Distance(anchor.Position, other.Position));

        // Overlapping same-owner fields make connected settlements one patch; each cell has one winning owner, clipping opposing territory.
        return Math.Clamp(float.IsFinite(nearestFriendly) ? Math.Max(48f, nearestFriendly * .58f) : 58f, 42f, 118f);
    }

    private static IReadOnlyList<StrategicTerritoryRegion> Regions(TerritoryGrid grid, int[,] cells, IReadOnlyList<StrategicTerritoryAnchor> anchors, IReadOnlyDictionary<int, CivilizationState> civs)
    {
        var result = new List<StrategicTerritoryRegion>();
        foreach (var owner in anchors.Select(anchor => anchor.CivilizationId).Distinct().OrderBy(owner => owner))
        {
            var owned = anchors.Where(anchor => anchor.CivilizationId == owner).ToArray();
            var runs = Runs(grid, cells, owner);
            if (runs.Count == 0) continue;
            var largest = runs.OrderByDescending(run => run.Size.X * run.Size.Y).First();
            var label = largest.Position + largest.Size * .5f;
            result.Add(new(owner, civs[owner].Name, owned, label, runs, Contours(grid, cells, owner)));
        }
        return result;
    }

    private static int[,] BuildFogCells(TerritoryGrid grid, IReadOnlyList<StarSystemState> systems, IReadOnlySet<int> unknown)
    {
        var cells = new int[grid.Width, grid.Height];
        for (var x = 0; x < grid.Width; x++)
            for (var y = 0; y < grid.Height; y++)
            {
                var point = grid.Center(x, y);
                var nearest = systems[0];
                var bestDistance = Vector2.DistanceSquared(point, nearest.Position);
                for (var index = 1; index < systems.Count; index++)
                {
                    var distance = Vector2.DistanceSquared(point, systems[index].Position);
                    if (distance >= bestDistance) continue;
                    bestDistance = distance;
                    nearest = systems[index];
                }
                cells[x, y] = unknown.Contains(nearest.Id) ? 1 : 0;
            }
        return cells;
    }

    private static IReadOnlyList<StrategicTerritoryFillRun> Runs(TerritoryGrid grid, int[,] cells, int owner)
    {
        var result = new List<StrategicTerritoryFillRun>();
        for (var y = 0; y < grid.Height; y++)
        {
            var start = -1;
            for (var x = 0; x <= grid.Width; x++)
            {
                var match = x < grid.Width && cells[x, y] == owner;
                if (match && start < 0) start = x;
                if (!match && start >= 0)
                {
                    result.Add(grid.Run(start, y, x - start));
                    start = -1;
                }
            }
        }
        return result;
    }
    private static IReadOnlyList<IReadOnlyList<Vector2>> Contours(TerritoryGrid grid, int[,] cells, int owner)
    {
        var edges = new Dictionary<GridPoint, List<GridPoint>>();
        void Add(GridPoint from, GridPoint to)
        {
            if (!edges.TryGetValue(from, out var destinations)) edges[from] = destinations = new();
            destinations.Add(to);
        }
        bool Own(int x, int y) => x >= 0 && y >= 0 && x < grid.Width && y < grid.Height && cells[x, y] == owner;

        for (var x = 0; x < grid.Width; x++)
            for (var y = 0; y < grid.Height; y++)
            {
                if (!Own(x, y)) continue;
                if (!Own(x, y - 1)) Add(new(x, y), new(x + 1, y));
                if (!Own(x + 1, y)) Add(new(x + 1, y), new(x + 1, y + 1));
                if (!Own(x, y + 1)) Add(new(x + 1, y + 1), new(x, y + 1));
                if (!Own(x - 1, y)) Add(new(x, y + 1), new(x, y));
            }

        var result = new List<IReadOnlyList<Vector2>>();
        while (edges.Count > 0)
        {
            var first = edges.Keys.OrderBy(point => point.Y).ThenBy(point => point.X).First();
            var current = first;
            var loop = new List<Vector2>();
            do
            {
                loop.Add(grid.Node(current));
                var next = edges[current][0];
                edges[current].RemoveAt(0);
                if (edges[current].Count == 0) edges.Remove(current);
                current = next;
            } while (current != first && edges.ContainsKey(current));
            if (loop.Count >= 3) result.Add(Smooth(loop));
        }
        return result;
    }

    private static IReadOnlyList<Vector2> Smooth(IReadOnlyList<Vector2> points)
    {
        var result = new List<Vector2>(points.Count * 2);
        for (var index = 0; index < points.Count; index++)
        {
            var from = points[index];
            var to = points[(index + 1) % points.Count];
            result.Add(Vector2.Lerp(from, to, .22f));
            result.Add(Vector2.Lerp(from, to, .78f));
        }
        return result;
    }

    private readonly record struct GridPoint(int X, int Y);

    private readonly record struct TerritoryGrid(Vector2 Origin, int Width, int Height, float CellSize)
    {
        public static TerritoryGrid Create(IReadOnlyList<StarSystemState> systems)
        {
            var min = systems[0].Position;
            var max = min;
            foreach (var system in systems)
            {
                min = Vector2.Min(min, system.Position);
                max = Vector2.Max(max, system.Position);
            }

            const float minimumCell = 22f;
            const float margin = 105f;
            const float maximumCellsPerAxis = 160f;
            var span = max - min + new Vector2(margin * 2);
            var cell = Math.Max(minimumCell, Math.Max(span.X, span.Y) / maximumCellsPerAxis);
            return new(
                min - new Vector2(margin),
                Math.Max(1, (int)Math.Ceiling(span.X / cell)),
                Math.Max(1, (int)Math.Ceiling(span.Y / cell)),
                cell);
        }

        public Vector2 Center(int x, int y) => Origin + new Vector2((x + .5f) * CellSize, (y + .5f) * CellSize);
        public Vector2 Node(GridPoint point) => Origin + new Vector2(point.X * CellSize, point.Y * CellSize);
        public StrategicTerritoryFillRun Run(int x, int y, int width) => new(
            Origin + new Vector2(x * CellSize, y * CellSize),
            new Vector2(width * CellSize, CellSize));
    }
}

public enum StrategicTerritoryAnchorKind { Home, Settlement }
public sealed record StrategicTerritoryAnchor(int CivilizationId, int SystemId, Vector2 Position, StrategicTerritoryAnchorKind Kind);
public sealed record StrategicTerritoryFillRun(Vector2 Position, Vector2 Size);
public sealed record StrategicTerritoryRegion(int CivilizationId, string CivilizationName, IReadOnlyList<StrategicTerritoryAnchor> Anchors, Vector2 LabelPosition, IReadOnlyList<StrategicTerritoryFillRun> FillRuns, IReadOnlyList<IReadOnlyList<Vector2>> Contours);
public sealed record StrategicTerritoryClaimOutline(int CivilizationId, int SystemId, Vector2 Position, float Radius);

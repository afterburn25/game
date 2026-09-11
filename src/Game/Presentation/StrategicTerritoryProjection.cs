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
    public IReadOnlySet<int> UnexploredSystemIds { get; }
    private StrategicTerritoryProjection(IReadOnlyList<StrategicTerritoryRegion> territories, IReadOnlyList<StrategicTerritoryClaimOutline> claims, IReadOnlyList<StrategicTerritoryFillRun> fogRuns, IReadOnlySet<int> unexplored) => (Territories, Claims, FogRuns, UnexploredSystemIds) = (territories, claims, fogRuns, unexplored);

    public static StrategicTerritoryProjection Build(GalaxyState galaxy, int observerId, IReadOnlyList<TerritorialClaimSnapshot>? observerClaims = null)
    {
        var systems = galaxy.Systems.ToDictionary(x => x.Id); var civilizations = galaxy.Civilizations.ToDictionary(x => x.Id);
        var settlementOwners = galaxy.Colonies.GroupBy(x => x.SystemId).ToDictionary(x => x.Key, x => x.OrderBy(y => y.Id).First().CivilizationId);
        var anchors = new Dictionary<(int, int), StrategicTerritoryAnchor>();
        bool Visible(int civ, int system) => civ == observerId || galaxy.Knowledge.IsCivilizationKnown(observerId, civ) && galaxy.Knowledge.IsSystemFullySurveyed(observerId, system);
        void Add(int civ, int system, StrategicTerritoryAnchorKind kind)
        {
            if (!systems.TryGetValue(system, out var star) || !Visible(civ, system)) return;
            var key = (civ, system); if (!anchors.TryGetValue(key, out var old) || kind < old.Kind) anchors[key] = new(civ, system, star.Position, kind);
        }
        // A current settlement authority supersedes a former civilization's natal marker.
        foreach (var civ in civilizations.Values) if (!settlementOwners.TryGetValue(civ.HomeSystemId, out var owner) || owner == civ.Id) Add(civ.Id, civ.HomeSystemId, StrategicTerritoryAnchorKind.Home);
        foreach (var colony in galaxy.Colonies) Add(colony.CivilizationId, colony.SystemId, StrategicTerritoryAnchorKind.Settlement);
        var all = anchors.Values.OrderBy(x => x.CivilizationId).ThenBy(x => x.SystemId).ToArray(); var grid = TerritoryGrid.Create(galaxy.Systems);
        var cells = Assign(grid, all); var regions = Regions(grid, cells, all, civilizations);
        var claims = new List<StrategicTerritoryClaimOutline>();
        foreach (var claim in observerClaims ?? Array.Empty<TerritorialClaimSnapshot>()) if (claim.Active && systems.TryGetValue(claim.SystemId, out var star) && civilizations.ContainsKey(claim.ClaimantCivilizationId) && Visible(claim.ClaimantCivilizationId, claim.SystemId)) claims.Add(new(claim.ClaimantCivilizationId, claim.SystemId, star.Position, Math.Max(28f, grid.CellSize * 1.3f)));
        var unknown = new HashSet<int>(galaxy.Systems.Where(x => !galaxy.Knowledge.IsSystemKnown(observerId, x.Id)).Select(x => x.Id));
        return new(regions, claims.OrderBy(x => x.CivilizationId).ThenBy(x => x.SystemId).ToArray(), BuildFogRuns(grid, galaxy.Systems, unknown), unknown);
    }

    private static int[,] Assign(TerritoryGrid grid, IReadOnlyList<StrategicTerritoryAnchor> anchors)
    {
        var cells = new int[grid.Width, grid.Height];
        for (var x = 0; x < grid.Width; x++) for (var y = 0; y < grid.Height; y++)
        {
            var best = 0; var score = 0f; var point = grid.Center(x, y);
            foreach (var anchor in anchors) { var candidate = Radius(anchor, anchors) - Vector2.Distance(point, anchor.Position); if (candidate > score || candidate == score && candidate > 0 && anchor.CivilizationId < best) { score = candidate; best = anchor.CivilizationId; } }
            cells[x, y] = best;
        }
        return cells;
    }
    private static float Radius(StrategicTerritoryAnchor anchor, IReadOnlyList<StrategicTerritoryAnchor> anchors)
    {
        var same = float.PositiveInfinity; foreach (var other in anchors) if (other != anchor && other.CivilizationId == anchor.CivilizationId) same = Math.Min(same, Vector2.Distance(anchor.Position, other.Position));
        // Overlapping same-owner fields make connected settlements one patch; each cell has one winning owner, clipping opposing territory.
        return Math.Clamp(float.IsFinite(same) ? Math.Max(48f, same * .58f) : 58f, 42f, 118f);
    }
    private static IReadOnlyList<StrategicTerritoryRegion> Regions(TerritoryGrid grid, int[,] cells, IReadOnlyList<StrategicTerritoryAnchor> anchors, IReadOnlyDictionary<int, CivilizationState> civs)
    {
        var result = new List<StrategicTerritoryRegion>();
        foreach (var owner in anchors.Select(x => x.CivilizationId).Distinct().OrderBy(x => x)) { var owned = anchors.Where(x => x.CivilizationId == owner).ToArray(); var sum = Vector2.Zero; foreach (var item in owned) sum += item.Position; result.Add(new(owner, civs[owner].Name, owned, sum / owned.Length, Runs(grid, cells, owner), Contours(grid, cells, owner))); }
        return result;
    }
    private static IReadOnlyList<StrategicTerritoryFillRun> BuildFogRuns(TerritoryGrid grid, IReadOnlyList<StarSystemState> systems, IReadOnlySet<int> unknown)
    {
        var cells = new int[grid.Width, grid.Height];
        for (var x = 0; x < grid.Width; x++) for (var y = 0; y < grid.Height; y++) { var nearest = systems[0]; var best = Vector2.DistanceSquared(grid.Center(x, y), nearest.Position); for (var i = 1; i < systems.Count; i++) { var d = Vector2.DistanceSquared(grid.Center(x, y), systems[i].Position); if (d < best) { best = d; nearest = systems[i]; } } cells[x, y] = unknown.Contains(nearest.Id) ? 1 : 0; }
        return Runs(grid, cells, 1);
    }
    private static IReadOnlyList<StrategicTerritoryFillRun> Runs(TerritoryGrid grid, int[,] cells, int owner)
    {
        var result = new List<StrategicTerritoryFillRun>(); for (var y = 0; y < grid.Height; y++) { var start = -1; for (var x = 0; x <= grid.Width; x++) { var match = x < grid.Width && cells[x, y] == owner; if (match && start < 0) start = x; if (!match && start >= 0) { result.Add(grid.Run(start, y, x - start)); start = -1; } } } return result;
    }
    private static IReadOnlyList<IReadOnlyList<Vector2>> Contours(TerritoryGrid grid, int[,] cells, int owner)
    {
        var edges = new Dictionary<GridPoint, List<GridPoint>>(); void Add(GridPoint a, GridPoint b) { if (!edges.TryGetValue(a, out var list)) edges[a] = list = new(); list.Add(b); } bool Own(int x, int y) => x >= 0 && y >= 0 && x < grid.Width && y < grid.Height && cells[x, y] == owner;
        for (var x = 0; x < grid.Width; x++) for (var y = 0; y < grid.Height; y++) if (cells[x, y] == owner) { if (!Own(x, y - 1)) Add(new(x, y), new(x + 1, y)); if (!Own(x + 1, y)) Add(new(x + 1, y), new(x + 1, y + 1)); if (!Own(x, y + 1)) Add(new(x + 1, y + 1), new(x, y + 1)); if (!Own(x - 1, y)) Add(new(x, y + 1), new(x, y)); }
        var result = new List<IReadOnlyList<Vector2>>(); while (edges.Count > 0) { var first = edges.Keys.OrderBy(x => x.Y).ThenBy(x => x.X).First(); var current = first; var loop = new List<Vector2>(); do { loop.Add(grid.Node(current)); var next = edges[current][0]; edges[current].RemoveAt(0); if (edges[current].Count == 0) edges.Remove(current); current = next; } while (current != first && edges.ContainsKey(current)); if (loop.Count >= 3) result.Add(Smooth(loop)); } return result;
    }
    private static IReadOnlyList<Vector2> Smooth(IReadOnlyList<Vector2> points) { var result = new List<Vector2>(points.Count * 2); for (var i = 0; i < points.Count; i++) { var a = points[i]; var b = points[(i + 1) % points.Count]; result.Add(Vector2.Lerp(a, b, .22f)); result.Add(Vector2.Lerp(a, b, .78f)); } return result; }
    private readonly record struct GridPoint(int X, int Y);
    private readonly record struct TerritoryGrid(Vector2 Origin, int Width, int Height, float CellSize)
    {
        public static TerritoryGrid Create(IReadOnlyList<StarSystemState> systems) { var min = systems[0].Position; var max = min; foreach (var s in systems) { min = Vector2.Min(min, s.Position); max = Vector2.Max(max, s.Position); } const float cell = 22f, margin = 105f; var span = max - min + new Vector2(margin * 2); return new(min - new Vector2(margin), Math.Max(1, (int)Math.Ceiling(span.X / cell)), Math.Max(1, (int)Math.Ceiling(span.Y / cell)), cell); }
        public Vector2 Center(int x, int y) => Origin + new Vector2((x + .5f) * CellSize, (y + .5f) * CellSize); public Vector2 Node(GridPoint p) => Origin + new Vector2(p.X * CellSize, p.Y * CellSize); public StrategicTerritoryFillRun Run(int x, int y, int width) => new(Origin + new Vector2(x * CellSize, y * CellSize), new Vector2(width * CellSize, CellSize));
    }
}
public enum StrategicTerritoryAnchorKind { Home, Settlement }
public sealed record StrategicTerritoryAnchor(int CivilizationId, int SystemId, Vector2 Position, StrategicTerritoryAnchorKind Kind);
public sealed record StrategicTerritoryFillRun(Vector2 Position, Vector2 Size);
public sealed record StrategicTerritoryRegion(int CivilizationId, string CivilizationName, IReadOnlyList<StrategicTerritoryAnchor> Anchors, Vector2 LabelPosition, IReadOnlyList<StrategicTerritoryFillRun> FillRuns, IReadOnlyList<IReadOnlyList<Vector2>> Contours);
public sealed record StrategicTerritoryClaimOutline(int CivilizationId, int SystemId, Vector2 Position, float Radius);

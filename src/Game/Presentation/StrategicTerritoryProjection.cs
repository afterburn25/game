using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Simulation.Diplomacy;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Presentation;

/// <summary>
/// Observer-safe map shaping for settlement-backed territory and exploration fog. This is
/// presentation data only: colonies, homes, and already visible diplomatic claims remain
/// authoritative elsewhere.
/// </summary>
public sealed class StrategicTerritoryProjection
{
    public IReadOnlyList<StrategicTerritoryRegion> Territories { get; }
    public IReadOnlyList<StrategicFogPatch> FogPatches { get; }

    private StrategicTerritoryProjection(IReadOnlyList<StrategicTerritoryRegion> territories,
        IReadOnlyList<StrategicFogPatch> fogPatches)
    {
        Territories = territories;
        FogPatches = fogPatches;
    }

    public static StrategicTerritoryProjection Build(GalaxyState galaxy, int observerId,
        IReadOnlyList<TerritorialClaimSnapshot>? observerClaims = null)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        var systems = galaxy.Systems.ToDictionary(system => system.Id);
        var civilizations = galaxy.Civilizations.ToDictionary(civilization => civilization.Id);
        var anchors = new Dictionary<(int CivilizationId, int SystemId), StrategicTerritoryAnchor>();

        bool CanSeeOwnership(int civilizationId, int systemId) => civilizationId == observerId ||
            galaxy.Knowledge.IsCivilizationKnown(observerId, civilizationId) &&
            galaxy.Knowledge.IsSystemFullySurveyed(observerId, systemId);

        void AddAnchor(int civilizationId, int systemId, StrategicTerritoryAnchorKind kind)
        {
            if (!systems.TryGetValue(systemId, out var system) || !CanSeeOwnership(civilizationId, systemId))
                return;
            var key = (civilizationId, systemId);
            if (!anchors.TryGetValue(key, out var existing) || kind < existing.Kind)
                anchors[key] = new(civilizationId, systemId, system.Position, kind);
        }

        foreach (var civilization in civilizations.Values)
            AddAnchor(civilization.Id, civilization.HomeSystemId, StrategicTerritoryAnchorKind.Home);
        foreach (var colony in galaxy.Colonies)
            AddAnchor(colony.CivilizationId, colony.SystemId, StrategicTerritoryAnchorKind.Settlement);
        foreach (var claim in observerClaims ?? Array.Empty<TerritorialClaimSnapshot>())
            if (claim.Active)
                AddAnchor(claim.ClaimantCivilizationId, claim.SystemId, StrategicTerritoryAnchorKind.VisibleClaim);

        var allAnchors = anchors.Values.OrderBy(anchor => anchor.CivilizationId).ThenBy(anchor => anchor.SystemId).ToArray();
        var territories = allAnchors.GroupBy(anchor => anchor.CivilizationId).OrderBy(group => group.Key)
            .Select(group =>
            {
                var owner = civilizations[group.Key];
                var regionAnchors = group.OrderBy(anchor => anchor.SystemId).ToArray();
                var center = Average(regionAnchors.Select(anchor => anchor.Position));
                return new StrategicTerritoryRegion(owner.Id, owner.Name, regionAnchors, center,
                    regionAnchors.Select(anchor => RadiusFor(anchor, allAnchors)).ToArray());
            })
            .ToArray();
        var fog = galaxy.Systems.Where(system => !galaxy.Knowledge.IsSystemKnown(observerId, system.Id))
            .OrderBy(system => system.Id)
            .Select(system => new StrategicFogPatch(system.Id, system.Position, FogRadiusFor(system, galaxy.Systems)))
            .ToArray();
        return new StrategicTerritoryProjection(territories, fog);
    }

    private static Vector2 Average(IEnumerable<Vector2> positions)
    {
        var values = positions.ToArray();
        return values.Aggregate(Vector2.Zero, (sum, value) => sum + value) / Math.Max(1, values.Length);
    }

    // Local spacing shapes a compact influence halo. It avoids policy-sized circles while
    // allowing close settlements of one civilization to read as a contiguous curved frontier.
    private static float RadiusFor(StrategicTerritoryAnchor anchor, IReadOnlyList<StrategicTerritoryAnchor> all)
    {
        var nearestOpposing = all.Where(other => other.CivilizationId != anchor.CivilizationId)
            .Select(other => Vector2.Distance(anchor.Position, other.Position)).DefaultIfEmpty(float.PositiveInfinity).Min();
        var nearestAny = all.Where(other => other != anchor)
            .Select(other => Vector2.Distance(anchor.Position, other.Position)).DefaultIfEmpty(96f).Min();
        var localScale = float.IsFinite(nearestOpposing) ? Math.Min(nearestAny, nearestOpposing * .92f) : nearestAny;
        return Math.Clamp(localScale * .44f, 28f, 78f);
    }

    private static float FogRadiusFor(StarSystemState system, IReadOnlyList<StarSystemState> systems)
    {
        var nearest = systems.Where(other => other.Id != system.Id)
            .Select(other => Vector2.Distance(system.Position, other.Position)).DefaultIfEmpty(90f).Min();
        return Math.Clamp(nearest * .52f, 34f, 104f);
    }
}

public enum StrategicTerritoryAnchorKind { Home, Settlement, VisibleClaim }

public sealed record StrategicTerritoryAnchor(int CivilizationId, int SystemId, Vector2 Position, StrategicTerritoryAnchorKind Kind);

public sealed record StrategicTerritoryRegion(int CivilizationId, string CivilizationName,
    IReadOnlyList<StrategicTerritoryAnchor> Anchors, Vector2 LabelPosition, IReadOnlyList<float> Radii);

public sealed record StrategicFogPatch(int SystemId, Vector2 Position, float Radius);

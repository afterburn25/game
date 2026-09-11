using System;
using Godot;
using Game.Simulation.Diplomacy;

namespace Game.Presentation;

public partial class Main
{
    private object? _territoryCampaign;
    private int _territoryFingerprint;
    private ulong _territoryNextCheckFrame;
    private StrategicTerritoryProjection? _territoryProjection;

    private void DrawStrategicTerritoryOverlay(Vector2 center, int playerId)
    {
        if (_galaxy is null) return;
        RefreshTerritoryProjection(playerId);
        var projection = _territoryProjection; if (projection is null) return;
        // Whole-galaxy view is deliberately quieter, never absent.
        var detail = .34f + .46f * RegionalOpacity;
        foreach (var fog in projection.FogRuns)
        {
            var rect = new Rect2(ToScreen(fog.Position, center), ToGodot(fog.Size) * UiMapZoom);
            // Cached runs avoid per-cell draw calls; two expanded passes feather their shared
            // edges instead of exposing a hard rectangular survey boundary.
            DrawRect(rect.Grow(5), MapAlpha(VisualPalette.Canvas, .035f + .025f * detail));
            DrawRect(rect.Grow(2), MapAlpha(VisualPalette.Canvas, .055f + .045f * detail));
            DrawRect(rect, MapAlpha(VisualPalette.Canvas, .095f + .095f * detail));
        }
        foreach (var region in projection.Territories)
        {
            var color = TerritoryColor(region.CivilizationId, playerId);
            foreach (var run in region.FillRuns) DrawRect(new Rect2(ToScreen(run.Position, center), ToGodot(run.Size) * UiMapZoom), MapAlpha(color, .035f * detail));
            foreach (var contour in region.Contours)
            {
                if (contour.Count < 3) continue;
                for (var index = 0; index < contour.Count; index++)
                    DrawLine(ToScreen(contour[index], center), ToScreen(contour[(index + 1) % contour.Count], center), MapAlpha(color, .55f * detail), 1.35f, true);
            }
            if (region.Anchors.Count == 0 || (UiOverviewBlend > .82f && region.Anchors.Count < 2)) continue;
            var point = ToScreen(region.LabelPosition, center); var label = region.CivilizationName.ToUpperInvariant();
            DrawString(_font, point + Vector2.One, label, HorizontalAlignment.Center, 180, 13, MapAlpha(VisualPalette.Canvas, .9f));
            DrawString(_font, point, label, HorizontalAlignment.Center, 180, 13, MapAlpha(color, .82f * detail));
        }
        foreach (var claim in projection.Claims) { var point = ToScreen(claim.Position, center); var radius = claim.Radius * UiMapZoom; if (radius > 5) DrawDashedArc(point, radius, TerritoryColor(claim.CivilizationId, playerId), detail); }
    }

    // Map rendering may use this cached observer snapshot to fade public-but-unexplored stars.
    private float StrategicUnexploredStarAlpha(int systemId) => _territoryProjection?.UnexploredSystemIds.Contains(systemId) == true ? .28f : 1f;

    private void RefreshTerritoryProjection(int playerId)
    {
        var frame = Engine.GetProcessFrames(); if (ReferenceEquals(_territoryCampaign, _galaxy) && frame < _territoryNextCheckFrame) return;
        _territoryNextCheckFrame = frame + 30;
        var claims = _diplomacyRuntime?.BuildView(playerId).Claims ?? Array.Empty<TerritorialClaimSnapshot>();
        var fingerprint = TerritoryFingerprint(playerId, claims);
        if (!ReferenceEquals(_territoryCampaign, _galaxy) || fingerprint != _territoryFingerprint) { _territoryCampaign = _galaxy; _territoryFingerprint = fingerprint; _territoryProjection = StrategicTerritoryProjection.Build(_galaxy!, playerId, claims); }
    }
    private int TerritoryFingerprint(int playerId, System.Collections.Generic.IReadOnlyList<TerritorialClaimSnapshot> claims)
    {
        var hash = new HashCode(); hash.Add(playerId);
        foreach (var item in _galaxy!.Civilizations) { hash.Add(item.Id); hash.Add(item.HomeSystemId); hash.Add(_galaxy.Knowledge.IsCivilizationKnown(playerId, item.Id)); }
        foreach (var item in _galaxy.Colonies) { hash.Add(item.Id); hash.Add(item.CivilizationId); hash.Add(item.SystemId); }
        foreach (var item in _galaxy.Systems) hash.Add((int)_galaxy.Knowledge.GetSystemSurveyLevel(playerId, item.Id));
        foreach (var item in claims) { hash.Add(item.ClaimId); hash.Add(item.ClaimantCivilizationId); hash.Add(item.SystemId); hash.Add(item.Active); }
        return hash.ToHashCode();
    }
    private void DrawDashedArc(Vector2 point, float radius, Color color, float opacity) { const int segments = 24; for (var i = 0; i < segments; i += 2) { var start = Mathf.Tau * i / segments; DrawArc(point, radius, start, start + Mathf.Tau / segments, 4, MapAlpha(color, .74f * opacity), 1.1f, true); } }
    private static Vector2 ToGodot(System.Numerics.Vector2 value) => new(value.X, value.Y);
    private static Color TerritoryColor(int civ, int player) => civ == player ? VisualPalette.Selected : (civ % 6) switch { 0 => VisualPalette.Diplomacy, 1 => VisualPalette.Science, 2 => VisualPalette.Economy, 3 => VisualPalette.Military, 4 => VisualPalette.Success, _ => new Color("d484b8") };
}

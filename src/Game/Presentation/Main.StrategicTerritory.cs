using System;
using System.Linq;
using Godot;
using Game.Simulation.Diplomacy;

namespace Game.Presentation;

public partial class Main
{
    private object? _territoryCampaign;
    private int _territoryFingerprint;
    private ulong _territoryNextCheckFrame;
    private StrategicTerritoryProjection? _territoryProjection;
    private readonly System.Collections.Generic.Dictionary<int, ArrayMesh> _territoryFillMeshes = new();

    public override void _ExitTree()
    {
        ClearTerritoryProjectionCache();
        base._ExitTree();
    }

    private void DrawStrategicTerritoryOverlay(Vector2 center, int playerId)
    {
        if (_galaxy is null) return;
        RefreshTerritoryProjection(playerId);
        var projection = _territoryProjection; if (projection is null) return;
        // Whole-galaxy view is deliberately quieter, never absent.
        var detail = .34f + .46f * RegionalOpacity;
        foreach (var fog in projection.FogRuns)
        {
            var rect = new Rect2(ProjectionToScreen(fog.Position, center), ToGodot(fog.Size) * UiMapZoom);
            DrawRect(rect, MapAlpha(VisualPalette.Canvas, .095f + .095f * detail));
        }
        // Feather only the outer survey boundary. Expanding every run compounds opacity
        // where adjacent rows meet and makes the cached mask visibly striped.
        foreach (var contour in projection.FogContours)
        {
            for (var index = 0; index < contour.Count; index++)
            {
                var from = ProjectionToScreen(contour[index], center);
                var to = ProjectionToScreen(contour[(index + 1) % contour.Count], center);
                DrawLine(from, to, MapAlpha(VisualPalette.Canvas, .045f * detail), 7f, true);
                DrawLine(from, to, MapAlpha(VisualPalette.Canvas, .075f * detail), 3f, true);
            }
        }
        foreach (var region in projection.Territories)
        {
            var color = TerritoryColor(region.CivilizationId, playerId);
            var fillColor = MapAlpha(color, .042f * detail);
            foreach (var run in region.FillRuns) DrawRect(new Rect2(ProjectionToScreen(run.Position, center), ToGodot(run.Size) * UiMapZoom), fillColor);
            if (_territoryFillMeshes.TryGetValue(region.CivilizationId, out var mesh))
            {
                DrawSetTransform(center, 0f, Vector2.One * UiMapZoom);
                DrawMesh(mesh, null, null, fillColor);
                DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
            }
            foreach (var contour in region.Contours)
            {
                if (contour.Count < 3) continue;
                for (var index = 0; index < contour.Count; index++)
                    DrawLine(ProjectionToScreen(contour[index], center), ProjectionToScreen(contour[(index + 1) % contour.Count], center), MapAlpha(color, .55f * detail), 1.35f, true);
            }
            if (region.Anchors.Count == 0 || (UiOverviewBlend > .82f && region.Anchors.Count < 2)) continue;
            var point = ProjectionToScreen(region.LabelPosition, center); var label = region.CivilizationName.ToUpperInvariant();
            DrawString(_font, point + Vector2.One, label, HorizontalAlignment.Center, 180, 13, MapAlpha(VisualPalette.Canvas, .9f));
            DrawString(_font, point, label, HorizontalAlignment.Center, 180, 13, MapAlpha(color, .82f * detail));
        }
        foreach (var claim in projection.Claims) { var point = ProjectionToScreen(claim.Position, center); var radius = claim.Radius * UiMapZoom; if (radius > 5) DrawDashedArc(point, radius, TerritoryColor(claim.CivilizationId, playerId), detail); }
    }

    // Map rendering may use this cached observer snapshot to fade public-but-unexplored stars.
    private float StrategicUnexploredStarAlpha(int systemId) => _territoryProjection?.UnexploredSystemIds.Contains(systemId) == true ? .28f : 1f;

    private void RefreshTerritoryProjection(int playerId)
    {
        var frame = Engine.GetProcessFrames(); if (ReferenceEquals(_territoryCampaign, _galaxy) && frame < _territoryNextCheckFrame) return;
        _territoryNextCheckFrame = frame + 30;
        var claims = _diplomacyRuntime?.BuildView(playerId).Claims ?? Array.Empty<TerritorialClaimSnapshot>();
        var fingerprint = TerritoryFingerprint(playerId, claims);
        if (!ReferenceEquals(_territoryCampaign, _galaxy) || fingerprint != _territoryFingerprint)
        {
            _territoryCampaign = _galaxy;
            _territoryFingerprint = fingerprint;
            _territoryProjection = StrategicTerritoryProjection.Build(_galaxy!, playerId, claims, UiCatalogVisualCoordinateScale);
            DisposeTerritoryFillMeshes();
            foreach (var region in _territoryProjection.Territories)
            {
                var vertices = new System.Collections.Generic.List<Vector3>();
                foreach (var polygon in region.FillPolygons)
                    for (var index = 1; index + 1 < polygon.Points.Count; index++)
                    {
                        vertices.Add(ToGodot3(polygon.Points[0]));
                        vertices.Add(ToGodot3(polygon.Points[index]));
                        vertices.Add(ToGodot3(polygon.Points[index + 1]));
                    }
                if (vertices.Count == 0) continue;
                var arrays = new Godot.Collections.Array();
                arrays.Resize((int)Mesh.ArrayType.Max);
                arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
                var mesh = new ArrayMesh();
                mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
                _territoryFillMeshes[region.CivilizationId] = mesh;
            }
        }
    }
    private void ClearTerritoryProjectionCache()
    {
        DisposeTerritoryFillMeshes();
        _territoryProjection = null;
        _territoryCampaign = null;
        _territoryFingerprint = 0;
        _territoryNextCheckFrame = 0;
    }
    private void DisposeTerritoryFillMeshes()
    {
        foreach (var mesh in _territoryFillMeshes.Values) mesh.Dispose();
        _territoryFillMeshes.Clear();
    }
    private int TerritoryFingerprint(int playerId, System.Collections.Generic.IReadOnlyList<TerritorialClaimSnapshot> claims)
    {
        var hash = new HashCode(); hash.Add(playerId); hash.Add(UiCatalogVisualCoordinateScale);
        foreach (var item in _galaxy!.Civilizations) { hash.Add(item.Id); hash.Add(item.HomeSystemId); hash.Add(_galaxy.Knowledge.IsCivilizationKnown(playerId, item.Id)); }
        foreach (var item in _galaxy.Colonies) { hash.Add(item.Id); hash.Add(item.CivilizationId); hash.Add(item.SystemId); }
        foreach (var item in _galaxy.Systems)
        {
            hash.Add(item.Id);
            hash.Add(item.Position.X);
            hash.Add(item.Position.Y);
            hash.Add((int)_galaxy.Knowledge.GetSystemSurveyLevel(playerId, item.Id));
        }
        foreach (var item in claims) { hash.Add(item.ClaimId); hash.Add(item.ClaimantCivilizationId); hash.Add(item.SystemId); hash.Add(item.Active); }
        return hash.ToHashCode();
    }
    private void DrawDashedArc(Vector2 point, float radius, Color color, float opacity) { const int segments = 24; for (var i = 0; i < segments; i += 2) { var start = Mathf.Tau * i / segments; DrawArc(point, radius, start, start + Mathf.Tau / segments, 4, MapAlpha(color, .74f * opacity), 1.1f, true); } }
    private static Vector2 ToGodot(System.Numerics.Vector2 value) => new(value.X, value.Y);
    private Vector2 ProjectionToScreen(System.Numerics.Vector2 position, Vector2 center) => center + ToGodot(position) * UiMapZoom;
    private static Vector3 ToGodot3(System.Numerics.Vector2 value) => new(value.X, value.Y, 0f);
    private static Color TerritoryColor(int civ, int player) => civ == player ? VisualPalette.Selected : (civ % 6) switch { 0 => VisualPalette.Diplomacy, 1 => VisualPalette.Science, 2 => VisualPalette.Economy, 3 => VisualPalette.Military, 4 => VisualPalette.Success, _ => new Color("d484b8") };
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Game.Simulation.Diplomacy;

namespace Game.Presentation;

public partial class Main
{
    private object? _territoryCampaign;
    private string _territorySignature = string.Empty;
    private StrategicTerritoryProjection? _territoryProjection;

    private void DrawStrategicTerritoryOverlay(Vector2 center, int playerId)
    {
        if (_galaxy is null || RegionalOpacity <= .01f)
            return;
        var claims = _diplomacyRuntime?.BuildView(playerId).Claims ?? Array.Empty<TerritorialClaimSnapshot>();
        var signature = TerritorySignature(playerId, claims);
        if (!ReferenceEquals(_territoryCampaign, _galaxy) || !string.Equals(signature, _territorySignature, StringComparison.Ordinal))
        {
            _territoryCampaign = _galaxy;
            _territorySignature = signature;
            _territoryProjection = StrategicTerritoryProjection.Build(_galaxy, playerId, claims);
        }
        var projection = _territoryProjection;
        if (projection is null)
            return;

        foreach (var patch in projection.FogPatches)
        {
            var point = ToScreen(patch.Position, center);
            var radius = patch.Radius * UiMapZoom;
            if (radius < 10 || point.X < -radius || point.Y < -radius || point.X > GetViewportRect().Size.X + radius || point.Y > GetViewportRect().Size.Y + radius)
                continue;
            DrawCircle(point, radius, MapAlpha(VisualPalette.Canvas, .17f * RegionalOpacity));
            DrawCircle(point, radius * .68f, MapAlpha(VisualPalette.Canvas, .10f * RegionalOpacity));
        }

        foreach (var region in projection.Territories)
        {
            var color = TerritoryColor(region.CivilizationId, playerId);
            for (var index = 0; index < region.Anchors.Count; index++)
            {
                var point = ToScreen(region.Anchors[index].Position, center);
                var radius = region.Radii[index] * UiMapZoom;
                if (radius < 8) continue;
                DrawCircle(point, radius, MapAlpha(color, .055f * RegionalOpacity));
                DrawArc(point, radius, 0, Mathf.Tau, 40, MapAlpha(color, .60f * RegionalOpacity), 1.35f, true);
            }
            if (_zoom < .42f || region.Anchors.Count == 0)
                continue;
            var labelPoint = ToScreen(region.LabelPosition, center);
            var label = region.CivilizationName.ToUpperInvariant();
            var labelColor = MapAlpha(color, .82f * RegionalOpacity);
            DrawString(_font, labelPoint + Vector2.One, label, HorizontalAlignment.Center, 180, 13, MapAlpha(VisualPalette.Canvas, .9f));
            DrawString(_font, labelPoint, label, HorizontalAlignment.Center, 180, 13, labelColor);
        }
    }

    private string TerritorySignature(int playerId, IReadOnlyList<TerritorialClaimSnapshot> claims)
    {
        var key = new StringBuilder().Append(playerId).Append('|');
        foreach (var civilization in _galaxy!.Civilizations.OrderBy(item => item.Id))
            key.Append(civilization.Id).Append(':').Append(civilization.HomeSystemId).Append(':')
                .Append(_galaxy.Knowledge.IsCivilizationKnown(playerId, civilization.Id) ? 'K' : 'U').Append('|');
        foreach (var colony in _galaxy.Colonies.OrderBy(item => item.CivilizationId).ThenBy(item => item.SystemId).ThenBy(item => item.Id))
            key.Append(colony.CivilizationId).Append(':').Append(colony.SystemId).Append('|');
        foreach (var system in _galaxy.Systems.OrderBy(item => item.Id))
            key.Append((int)_galaxy.Knowledge.GetSystemSurveyLevel(playerId, system.Id));
        key.Append('|');
        foreach (var claim in claims.OrderBy(item => item.ClaimId))
            key.Append(claim.ClaimId).Append(':').Append(claim.ClaimantCivilizationId).Append(':').Append(claim.SystemId).Append(':').Append(claim.Active ? '1' : '0').Append('|');
        return key.ToString();
    }

    private static Color TerritoryColor(int civilizationId, int playerId)
    {
        if (civilizationId == playerId) return VisualPalette.Selected;
        return (civilizationId % 6) switch
        {
            0 => VisualPalette.Diplomacy,
            1 => VisualPalette.Science,
            2 => VisualPalette.Economy,
            3 => VisualPalette.Military,
            4 => VisualPalette.Success,
            _ => new Color("d484b8"),
        };
    }
}

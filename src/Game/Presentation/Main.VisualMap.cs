using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;
using Game.Simulation.Generation;
using Game.Simulation.Exploration;
using Game.Presentation.Spatial;

namespace Game.Presentation;

public partial class Main
{
    private float RegionalOpacity => Math.Clamp(1 - UiOverviewBlend * 2, 0, 1);
    private float CatalogOpacity => 0.90f + RegionalOpacity * 0.10f;
    private Color MapColor(Color color) => VisualPalette.WithAlpha(color, color.A * CatalogOpacity);
    private Color MapAlpha(Color color, float alpha) => VisualPalette.WithAlpha(color, alpha * CatalogOpacity);
    public Rect2 UiGalaxyArtworkScreenRect
    {
        get
        {
            var frame = SpatialNavigationLayout.GalaxyWorldFrame;
            if (_galaxy?.GenerationMetadata?.GalaxyShape != "Barred spiral" && _galaxy?.Systems.Count > 0)
            {
                // Preserve old disk-save coordinates and surround that catalog with matching disk dust.
                var radius = _galaxy.Systems.Max(system => system.Position.Length());
                var diameter = Math.Max(100, radius * 2.0f / .81818182f);
                return new(UiMapOriginScreen - Vector2.One * diameter * UiMapZoom * .5f,
                    Vector2.One * diameter * UiMapZoom);
            }
            return new(UiMapOriginScreen + new Vector2(frame.Left, frame.Top) * UiMapZoom,
                new Vector2(frame.Width, frame.Height) * UiMapZoom);
        }
    }
    private readonly Dictionary<(FleetRole Role, System.Numerics.Vector2 Position), (FleetState Fleet, int Count)> _visualFleetGroups = new();
    private object? _laneCampaign;
    private IReadOnlyList<InterstellarLane> _interstellarLanes = Array.Empty<InterstellarLane>();
    public bool UiHasDeepField => SpaceArtwork.DeepField is not null;

    /// <summary>
    /// Complete regional presentation. Stellar coordinates are the existing catalog transform;
    /// physical stellar hue is visible with the public coordinate catalog; names, class labels,
    /// hazards and system facts retain their established survey and civilization gates.
    /// </summary>
    protected void DrawVisualMapOverlay()
    {
        var viewport = GetViewportRect().Size;
        DrawRegionalSpace(viewport);
        if (_galaxy is null)
            return;

        var playerId = _galaxy.PlayerCivilizationId;
        var homeId = _galaxy.Civilizations.First(civilization => civilization.Id == playerId).HomeSystemId;
        var center = viewport * 0.5f + _pan;
        DrawStrategicTerritoryOverlay(center, playerId);
        DrawKnownInterstellarLanes(center, playerId);
        DrawVisualPlayerRoutes(center, playerId);

        foreach (var system in _galaxy.Systems)
        {
            var position = ToScreen(system.Position, center);
            if (position.X < -80 || position.Y < -80 || position.X > viewport.X + 80 || position.Y > viewport.Y + 80)
                continue;

            var survey = _galaxy.Knowledge.GetSystemSurveyLevel(playerId, system.Id);
            var selected = system.Id == _selectedSystemId;
            var home = system.Id == homeId;
            // A star's apparent hue is part of the public sky/catalog view. Do not use the
            // archetype as a fallback here: it encodes survey-gated strategic information.
            var hasSpectralHue = system.StellarClass.HasValue;
            var color = MapColor(hasSpectralHue
                ? GetSpectralStarColor(system.StellarClass)
                : new Color(0.63f, 0.70f, 0.79f));
            var radius = Math.Clamp(3.0f + _zoom * 1.6f, 3.1f, 5.4f);
            if (survey == SystemSurveyLevel.Unknown)
                radius *= 0.86f;

            var isBlackHole = system.StellarClass == StellarPrimaryClass.BlackHole ||
                (!hasSpectralHue && system.Archetype == StarArchetype.BlackHole);
            if (survey == SystemSurveyLevel.FullySurveyed && isBlackHole)
            {
                CinematicArt.DrawStarlight(this, position, radius * 1.25f, new Color("db9460"), .85f);
                DrawCircle(position, radius * .65f, Colors.Black, true, -1, true);
            }
            else if (hasSpectralHue)
                DrawSpectralCatalogStar(position, radius, color);
            else
                CinematicArt.DrawStarlight(this, position, radius, color, .72f + RegionalOpacity * .28f);

            if (survey == SystemSurveyLevel.FullySurveyed)
            {
                var isNeutronStar = system.StellarClass == StellarPrimaryClass.NeutronStar ||
                    (!hasSpectralHue && system.Archetype == StarArchetype.NeutronPulsar);
                if (isNeutronStar)
                    DrawLine(position + new Vector2(-radius * 2.8f, radius * .65f),
                        position + new Vector2(radius * 2.8f, -radius * .65f), MapAlpha(color, .72f), 1.1f, true);
                else if (system.Archetype == StarArchetype.Dangerous)
                    DrawArc(position, radius + 2.8f, -.65f, .5f, 14, MapAlpha(new Color("ffb35f"), .78f), 1.2f, true);
                else if (system.Archetype == StarArchetype.Legendary)
                {
                    DrawLine(position + new Vector2(-radius * 2.2f, 0), position + new Vector2(radius * 2.2f, 0), MapAlpha(color, .42f), 1, true);
                    DrawLine(position + new Vector2(0, -radius * 2.2f), position + new Vector2(0, radius * 2.2f), MapAlpha(color, .42f), 1, true);
                }
                else if (system.Archetype == StarArchetype.Nebula)
                    DrawCircle(position, radius + 4.5f, MapAlpha(new Color("9a6bd5"), .12f));
            }

            if (survey >= SystemSurveyLevel.Detected)
            {
                var extent = survey switch
                {
                    SystemSurveyLevel.Detected => MathF.PI * 0.45f,
                    SystemSurveyLevel.PartiallySurveyed => MathF.PI * 1.25f,
                    _ => MathF.PI * 2.0f,
                };
                DrawArc(position, radius + 5.0f, -MathF.PI * 0.5f, -MathF.PI * 0.5f + extent, 40,
                    MapAlpha(survey == SystemSurveyLevel.FullySurveyed ? color : VisualPalette.TextSecondary, 0.48f), 1.0f, true);
            }

            if (selected)
                DrawRegionalReticle(position, 19.0f, MapColor(VisualPalette.Selected));
            if (selected || home || (survey >= SystemSurveyLevel.PartiallySurveyed && _zoom >= 0.88f))
            {
                var label = _galaxy.Knowledge.IsSystemKnown(playerId, system.Id) ? system.Name : $"CATALOG {system.Id + 1:000}";
                var labelColor = MapColor(selected ? VisualPalette.TextPrimary : VisualPalette.TextSecondary);
                var labelPosition = position + new Vector2(18.0f, -13.0f);
                DrawString(_font, labelPosition + Vector2.One, label, HorizontalAlignment.Left, -1, selected || home ? 14 : 12, MapColor(VisualPalette.Canvas));
                DrawString(_font, labelPosition, label, HorizontalAlignment.Left, -1, selected || home ? 14 : 12, labelColor);
                if (home)
                    DrawString(_font, labelPosition + new Vector2(0.0f, 15.0f), "HOME SYSTEM", HorizontalAlignment.Left, -1, 9, MapColor(VisualPalette.Success));
            }
        }

        DrawVisualColonies(center, playerId);
        DrawVisualKnownCivilizationHomes(center, playerId);
        DrawVisualPlayerFleets(center, playerId);
    }

    private void DrawKnownInterstellarLanes(Vector2 center, int playerId)
    {
        if (!ReferenceEquals(_laneCampaign, _galaxy))
        {
            _laneCampaign = _galaxy;
            _interstellarLanes = new InterstellarLaneNetwork().Build(_galaxy.Systems);
        }
        var maximumPlayerLeg = _galaxy.Fleets
            .Where(fleet => fleet.IsActive && fleet.CivilizationId == playerId)
            .Select(fleet => fleet.MaximumLegRangeLightYears)
            .DefaultIfEmpty(0.0)
            .Max();
        foreach (var lane in _interstellarLanes)
        {
            if (!_galaxy.Knowledge.IsSystemKnown(playerId, lane.FirstSystemId) ||
                !_galaxy.Knowledge.IsSystemKnown(playerId, lane.SecondSystemId)) continue;
            var first = _galaxy.Systems.First(system => system.Id == lane.FirstSystemId);
            var second = _galaxy.Systems.First(system => system.Id == lane.SecondSystemId);
            var start = ToScreen(first.Position, center);
            var end = ToScreen(second.Position, center);
            if (lane.LengthLightYears <= maximumPlayerLeg + 0.0001)
            {
                var reachable = MapAlpha(VisualPalette.Selected, .15f + RegionalOpacity * .20f);
                DrawLine(start, end, reachable, 1.05f, true);
            }
            else
            {
                var blocked = MapAlpha(new Color("d08b62"), .10f + RegionalOpacity * .08f);
                DrawDashedLine(start, end, blocked, .75f, 9.0f);
            }
        }
    }

    private void DrawRegionalSpace(Vector2 size)
    {
        DrawRect(new Rect2(Vector2.Zero, size), new Color("02050a"));
        // The strategic galaxy owns the overview; the distant field only supplies a quiet edge.
        SpaceArtwork.DrawDeepField(this, size, .035f + UiOverviewBlend * .035f);
        SpaceArtwork.DrawNebula(this, size, _pan, .25f * (1 - UiOverviewBlend));
        if (UiOverviewBlend > 0)
        {
            SpaceArtwork.DrawGalaxyOverview(this, UiGalaxyArtworkScreenRect, _galaxy?.Seed ?? 0, UiOverviewBlend,
                _galaxy?.GenerationMetadata?.GalaxyShape == "Barred spiral");
        }
    }

    private void DrawStrategicCoordinateLayer(Vector2 size)
    {
        var opacity = RegionalOpacity * Math.Clamp((_zoom - 0.08f) * 1.8f, 0.0f, 0.16f);
        if (opacity <= 0.002f) return;
        var spacing = Math.Clamp(260.0f * _zoom, 72.0f, 210.0f);
        var origin = size * 0.5f + _pan;
        var offsetX = ((origin.X % spacing) + spacing) % spacing;
        var offsetY = ((origin.Y % spacing) + spacing) % spacing;
        var minor = VisualPalette.WithAlpha(VisualPalette.Keyline, opacity * 0.48f);
        var major = VisualPalette.WithAlpha(VisualPalette.Selected, opacity);
        var index = 0;
        for (var x = offsetX; x < size.X; x += spacing, index++)
            DrawLine(new Vector2(x, 72), new Vector2(x, size.Y), index % 4 == 0 ? major : minor, index % 4 == 0 ? 1.0f : 0.6f, true);
        index = 0;
        for (var y = offsetY; y < size.Y; y += spacing, index++)
            DrawLine(new Vector2(0, y), new Vector2(size.X, y), index % 4 == 0 ? major : minor, index % 4 == 0 ? 1.0f : 0.6f, true);
        DrawCircle(UiMapOriginScreen, 34.0f, VisualPalette.WithAlpha(VisualPalette.Selected, opacity * 1.6f), false, 1.2f, true);
    }

    private void DrawVisualColonies(Vector2 center, int playerId)
    {
        foreach (var colony in _galaxy.Colonies)
        {
            var own = colony.CivilizationId == playerId;
            if (!own &&
                (!_galaxy.Knowledge.IsSystemFullySurveyed(playerId, colony.SystemId) ||
                 !_galaxy.Knowledge.IsCivilizationKnown(playerId, colony.CivilizationId)))
                continue;
            var system = _galaxy.Systems.First(candidate => candidate.Id == colony.SystemId);
            var anchor = ToScreen(system.Position, center);
            var marker = anchor + new Vector2(-18.0f, -19.0f);
            var color = MapColor(own ? VisualPalette.Success : new Color(0.66f, 0.62f, 0.77f));
            DrawLine(anchor + new Vector2(-5.0f, -5.0f), marker, MapAlpha(color, 0.42f), 1.0f, true);
            DrawCircle(marker, 9.0f, MapColor(VisualPalette.Canvas));
            DrawVisualIcon(VisualIconLibrary.Colony, marker, own ? 17.0f : 15.0f, color);
            if (!own)
                DrawCircle(marker, 10.0f, MapAlpha(color, 0.52f), false, 0.8f, true);
        }
    }

    private void DrawVisualKnownCivilizationHomes(Vector2 center, int playerId)
    {
        foreach (var civilization in _galaxy.Civilizations)
        {
            if (civilization.Id == playerId ||
                !_galaxy.Knowledge.IsCivilizationKnown(playerId, civilization.Id) ||
                !_galaxy.Knowledge.IsSystemFullySurveyed(playerId, civilization.HomeSystemId))
                continue;
            var home = _galaxy.Systems.First(system => system.Id == civilization.HomeSystemId);
            var anchor = ToScreen(home.Position, center);
            var marker = anchor + new Vector2(17.0f, -34.0f);
            var color = MapColor(civilization.IsSeededAncient
                ? new Color(0.80f, 0.67f, 0.42f) : new Color(0.66f, 0.62f, 0.77f));
            DrawLine(anchor + new Vector2(4.0f, -8.0f), marker, MapAlpha(color, 0.34f), 1.0f, true);
            DrawCircle(marker, 9.0f, MapColor(VisualPalette.Canvas));
            DrawVisualIcon(VisualIconLibrary.DiplomacyContact, marker, 15.0f, color);
        }
    }

    private void DrawVisualPlayerRoutes(Vector2 center, int playerId)
    {
        foreach (var fleet in _galaxy.Fleets)
        {
            if (!fleet.IsActive || fleet.CivilizationId != playerId || fleet.DestinationSystemId is not int destinationId)
                continue;
            var start = ToScreen(fleet.Position, center);
            // Ownership is conveyed consistently at every map scale. Role remains in the
            // silhouette, so a player never mistakes a foreign palette for an owned vessel.
            var color = MapColor(VisualPalette.Success);
            var routeIds = fleet.PlannedRouteSystemIds.Count > 0
                ? fleet.PlannedRouteSystemIds
                : new List<int> { destinationId };
            var currentLeg = true;
            foreach (var routeSystemId in routeIds)
            {
                var destination = _galaxy.Systems.First(system => system.Id == routeSystemId);
                var end = ToScreen(destination.Position, center);
                DrawLine(start, end, MapAlpha(color, 0.07f), 5.0f, true);
                DrawDashedLine(start, end, MapAlpha(color, 0.60f), 1.15f, 8.0f);
                if (start.DistanceSquaredTo(end) > 1600.0f)
                {
                    var direction = (end - start).Normalized();
                    var normal = new Vector2(-direction.Y, direction.X);
                    var tip = start.Lerp(end, 0.62f);
                    DrawLine(tip, tip - direction * 7.0f + normal * 3.5f, color, 1.3f, true);
                    DrawLine(tip, tip - direction * 7.0f - normal * 3.5f, color, 1.3f, true);
                }
                if (currentLeg && start.DistanceSquaredTo(end) > 16.0f)
                {
                    // The state-owned fleet position is the route-progress marker. These three
                    // bounded strokes only appear while a real destination is active, so they
                    // freeze with simulation time and never invent a separate travel animation.
                    var heading = (end - start).Normalized();
                    for (var trail = 0; trail < 3; trail++)
                    {
                        var offset = 4.0f + trail * 4.0f;
                        DrawLine(start - heading * offset, start - heading * (offset + 2.4f),
                            MapAlpha(color, .58f - trail * .16f), 1.15f - trail * .18f, true);
                    }
                    DrawCircle(start, 2.2f, MapAlpha(color, .86f), true, -1, true);
                }
                start = end;
                currentLeg = false;
            }
        }
    }

    private void DrawVisualPlayerFleets(Vector2 center, int playerId)
    {
        // Reuse the dictionary rather than allocating LINQ groups each frame. Only exact-own
        // co-located ships share a count; every actual course is still drawn separately above.
        _visualFleetGroups.Clear();
        foreach (var fleet in _galaxy.Fleets)
        {
            if (!fleet.IsActive || fleet.CivilizationId != playerId)
                continue;
            var key = (fleet.Role, fleet.Position);
            _visualFleetGroups[key] = _visualFleetGroups.TryGetValue(key, out var group)
                ? (group.Fleet, group.Count + 1) : (fleet, 1);
        }
        foreach (var group in _visualFleetGroups.Values)
        {
            var fleet = group.Fleet;
            var anchor = ToScreen(fleet.Position, center);
            var position = FleetMarkerScreenPosition(fleet, center);
            if (SelectedFleet is { } selected && selected.Role == fleet.Role && selected.Position == fleet.Position)
                DrawRegionalReticle(position, 17, VisualUi.Accent);
            var color = MapColor(VisualPalette.Success);
            DrawLine(anchor, position, MapAlpha(color, 0.36f), 1.0f, true);
            DrawCircle(position, 13.0f, MapColor(new Color(0.025f, 0.055f, 0.080f, 0.96f)));
            DrawCircle(position, 13.0f, MapAlpha(color, 0.50f), false, 1.0f, true);
            DrawVisualIcon(FleetRoleTexture(fleet.Role), position, 23.0f, color);
            if (group.Count > 1)
            {
                var badge = position + new Vector2(10.0f, -10.0f);
                DrawCircle(badge, 8.0f, MapColor(VisualPalette.SurfacePrimary));
                DrawCircle(badge, 8.0f, color, false, 1.0f, true);
                DrawString(_font, badge + new Vector2(-8.0f, 3.0f), group.Count > 99 ? "99+" : group.Count.ToString(),
                    HorizontalAlignment.Center, 16.0f, 9, MapColor(VisualPalette.TextPrimary));
            }
        }
    }

    private void DrawRegionalReticle(Vector2 position, float radius, Color color)
    {
        for (var corner = 0; corner < 4; corner++)
        {
            var angle = corner * MathF.PI * 0.5f + 0.18f;
            DrawArc(position, radius, angle, angle + MathF.PI * 0.5f - 0.36f, 14, color, 1.6f, true);
        }
        DrawCircle(position, radius + 4.0f, MapAlpha(color, 0.11f), false, 1.0f, true);
    }

    /// <summary>Catalogued stellar classes receive a compact spectral corona and a fixed,
    /// high-definition core. Entries without a physical class retain the neutral glyph.</summary>
    private void DrawSpectralCatalogStar(Vector2 position, float radius, Color spectral)
    {
        var regionalDetail = Mathf.Lerp(.48f, 1.0f, RegionalOpacity);
        var outer = radius * Mathf.Lerp(4.4f, 5.8f, regionalDetail);
        DrawTextureRect(CinematicArt.Glow, new Rect2(position - Vector2.One * outer, Vector2.One * outer * 2), false,
            new Color(spectral.R, spectral.G, spectral.B, (.25f + .17f * regionalDetail) * CatalogOpacity));
        // The shared radial texture stays smooth at the four-pixel map scale, where filled
        // vector circles otherwise produce visible polygon edges. Keep its color physical.
        var inner = radius * 2.15f;
        DrawTextureRect(CinematicArt.Glow, new Rect2(position - Vector2.One * inner, Vector2.One * inner * 2), false,
            new Color(spectral.R, spectral.G, spectral.B, (.43f + .25f * regionalDetail) * CatalogOpacity));
        // Fine diffraction rays establish a stellar silhouette at overview scale. They are
        // shorter than a marker selection ring and retain the spectral halo as the identity.
        var ray = radius * Mathf.Lerp(1.25f, 2.45f, regionalDetail);
        var rayColor = new Color(spectral.R, spectral.G, spectral.B, (.18f + .28f * regionalDetail) * CatalogOpacity);
        DrawLine(position - new Vector2(ray, 0), position + new Vector2(ray, 0), rayColor, .62f, true);
        DrawLine(position - new Vector2(0, ray), position + new Vector2(0, ray), rayColor, .62f, true);
        var diagonal = radius * Mathf.Lerp(.72f, 1.45f, regionalDetail);
        DrawLine(position - new Vector2(diagonal, diagonal), position + new Vector2(diagonal, diagonal),
            new Color(spectral.R, spectral.G, spectral.B, (.08f + .17f * regionalDetail) * CatalogOpacity), .48f, true);
        DrawLine(position - new Vector2(diagonal, -diagonal), position + new Vector2(diagonal, -diagonal),
            new Color(spectral.R, spectral.G, spectral.B, (.08f + .17f * regionalDetail) * CatalogOpacity), .48f, true);
        // A sub-halo ivory core gives each catalogue star a clear bright point without
        // whitening the much larger spectral identity halo.
        DrawCircle(position, Math.Max(1.0f, radius * .42f), new Color(1f, .975f, .91f,
            .98f * CatalogOpacity), true, -1, true);
    }

    private static Texture2D FleetRoleTexture(FleetRole role) => role switch
    {
        FleetRole.Scout => VisualIconLibrary.Scout,
        FleetRole.Science => VisualIconLibrary.ScienceVessel,
        FleetRole.Military => VisualIconLibrary.PatrolCorvette,
        FleetRole.Colony => VisualIconLibrary.ColonyShip,
        _ => VisualIconLibrary.Scout,
    };

    private static Color FleetRoleColor(FleetRole role) => role switch
    {
        FleetRole.Scout => VisualPalette.Selected,
        FleetRole.Science => VisualPalette.Science,
        FleetRole.Military => VisualPalette.Military,
        FleetRole.Colony => VisualPalette.Success,
        _ => VisualPalette.TextPrimary,
    };

    private void DrawVisualIcon(Texture2D texture, Vector2 center, float size, Color color)
    {
        var half = size * 0.5f;
        DrawTextureRect(texture, new Rect2(center.X - half, center.Y - half, size, size), false, color);
    }
}

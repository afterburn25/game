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
    private const float DeepFieldOverviewOpacity = .46f;
    private const float RegionalMaximumZoom = 48f;
    private const float RegionalSystemEntryZoom = 18f;
    private const int RegionalBackdropStarCount = 260;
    private const int RegionalBackdropClusterStarCount = 96;
    private readonly List<RegionalBackdropStar> _regionalBackdropStars = new();
    private Vector2 _regionalBackdropSize;
    private long _regionalBackdropSeed = long.MinValue;
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
    /// <summary>Resolved background galaxies belong to the whole-galaxy view only.</summary>
    public float UiGalaxyDeepFieldOpacity => UiIsSystemSpatialView ? 0 : DeepFieldOverviewOpacity * UiOverviewBlend;
    public float UiRegionalMaximumZoom => RegionalMaximumZoom;
    public float UiRegionalSystemEntryZoom => RegionalSystemEntryZoom;
    public int UiRegionalBackdropStarCount => _regionalBackdropStars.Count;
    public float UiRegionalBackdropOpacity => UiIsSystemSpatialView ? 0 : RegionalOpacity;
    // Point flares are drawn with the shared gradient texture; there are no per-star nodes.
    public int UiRegionalPointSpriteCount => 0;
    public int UiRegionalPointSpriteBudget => 0;
    public int UiVisibleRegionalPointCount => 0;
    // Kept as zero-valued compatibility diagnostics until the capture harness adopts the
    // point-renderer names above. No photosphere sprite machinery remains.
    public int UiRegionalPhotosphereSpriteCount => 0;
    public int UiVisibleRegionalPhotosphereCount => 0;
    public int UiRegionalPhotosphereSpriteBudget => 0;

    /// <summary>Apparent catalogue-star radius shared by drawing and pointer hit testing.</summary>
    public float UiCatalogStarRadius(int systemId)
    {
        var system = _galaxy?.Systems.FirstOrDefault(candidate => candidate.Id == systemId);
        if (system is null) return 0;
        var radius = Math.Clamp(12.0f + 2.6f * MathF.Sqrt(Math.Max(0, _zoom - .35f)), 12.0f, 22.0f);
        return _galaxy!.Knowledge.GetSystemSurveyLevel(_galaxy.PlayerCivilizationId, systemId) == SystemSurveyLevel.Unknown
            ? Math.Max(12.0f, radius * .86f) : radius;
    }

    /// <summary>Bright point core remains tiny even at the regional zoom ceiling.</summary>
    public float UiCatalogStarCoreRadius(int systemId) => UiCatalogStarRadius(systemId) <= 0 ? 0 :
        Math.Clamp(UiCatalogStarRadius(systemId) * .20f, 2.4f, 4.4f);

    /// <summary>
    /// Complete regional presentation. Stellar coordinates are the existing catalog transform;
    /// physical stellar hue is visible with the public coordinate catalog; names, class labels,
    /// hazards and system facts retain their established survey and civilization gates.
    /// </summary>
    protected void DrawVisualMapOverlay()
    {
        if (UiIsSystemSpatialView || UiIsSurfaceOpen)
            return;
        var viewport = GetViewportRect().Size;
        DrawRegionalSpace(viewport);
        if (_galaxy is null)
            return;

        var playerId = _galaxy.PlayerCivilizationId;
        var homeId = _galaxy.Civilizations.First(civilization => civilization.Id == playerId).HomeSystemId;
        var center = viewport * 0.5f + _pan;
        DrawGalacticCore(center);
        DrawStrategicTerritoryOverlay(center, playerId);
        DrawKnownInterstellarLanes(center, playerId);
        DrawVisualPlayerRoutes(center, playerId);

        foreach (var system in _galaxy.Systems)
        {
            var position = ToScreen(system.Position, center);
            var survey = _galaxy.Knowledge.GetSystemSurveyLevel(playerId, system.Id);
            var selected = system.Id == _selectedSystemId;
            var home = system.Id == homeId;
            // A star's apparent hue is part of the public sky/catalog view. Do not use the
            // archetype as a fallback here: it encodes survey-gated strategic information.
            var hasSpectralHue = system.StellarClass.HasValue;
            var color = MapColor(hasSpectralHue
                ? GetSpectralStarColor(system.StellarClass)
                : new Color(0.63f, 0.70f, 0.79f));
            var radius = UiCatalogStarRadius(system.Id);
            // Close stars retain a broad corona, so their cull margin grows with the same
            // apparent radius used by the photosphere and hit target.
            var visualExtent = Math.Max(48.0f, radius * 2.4f + 18.0f);
            if (position.X < -visualExtent || position.Y < -visualExtent ||
                position.X > viewport.X + visualExtent || position.Y > viewport.Y + visualExtent)
                continue;
            var isBlackHole = system.StellarClass == StellarPrimaryClass.BlackHole ||
                (!hasSpectralHue && system.Archetype == StarArchetype.BlackHole);
            if (survey == SystemSurveyLevel.FullySurveyed && isBlackHole)
            {
                CinematicArt.DrawStarlight(this, position, radius * 1.25f, new Color("db9460"), .85f);
                DrawCircle(position, radius * .65f, Colors.Black, true, -1, true);
            }
            else if (hasSpectralHue)
                DrawSpectralCatalogStar(system.Id, position, radius, color, StrategicUnexploredStarAlpha(system.Id));
            else
                CinematicArt.DrawStarlight(this, position, radius, color,
                    (.72f + RegionalOpacity * .28f) * StrategicUnexploredStarAlpha(system.Id));

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
                DrawArc(position, radius * 1.14f + 5.0f, -MathF.PI * 0.5f, -MathF.PI * 0.5f + extent, 40,
                    MapAlpha(survey == SystemSurveyLevel.FullySurveyed ? color : VisualPalette.TextSecondary, 0.48f), 1.0f, true);
            }

            if (selected)
                DrawRegionalReticle(position, Math.Max(18.0f, radius + 11.0f), MapColor(VisualPalette.Selected));
            if (selected || home || (survey >= SystemSurveyLevel.PartiallySurveyed && _zoom >= 0.88f))
            {
                var label = _galaxy.Knowledge.IsSystemKnown(playerId, system.Id) ? system.Name : $"CATALOG {system.Id + 1:000}";
                var labelColor = MapColor(selected ? VisualPalette.TextPrimary : VisualPalette.TextSecondary);
                var labelPosition = position + new Vector2(Math.Max(18.0f, radius + 16.0f), -Math.Max(13.0f, radius * .55f));
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

    private void DrawGalacticCore(Vector2 mapCenter)
    {
        var core = _galaxy?.GalacticCore;
        if (core is null)
            return;

        var center = ToScreen(new System.Numerics.Vector2(core.X, core.Y), mapCenter);
        if (UiGalacticCore is null)
        {
            // The existing deep-field and galaxy-dust layers already provide irregular fog.
            // Adding any core-centred primitive here makes the secret's position perceptible.
            return;
        }
        // This mask maps the exact generated exclusion radius into the current world view.
        // The icon itself is capped separately, so zoom never makes the void larger than its
        // authoritative star-free region.
        var reservedRadius = UiGalacticCoreScreenRadius;
        // A transparent falloff softens the boundary against dust. The opaque void starts at
        // precisely the generated radius, so it never conceals a real catalogue coordinate.
        for (var fade = 4; fade >= 1; fade--)
            DrawCircle(center, reservedRadius * (1.0f + fade * .045f),
                MapAlpha(new Color("120b0b"), .016f + fade * .010f), false, 1.4f, true);
        DrawCircle(center, reservedRadius, new Color("02050a"), true, -1, true);
        var ringRadius = Math.Min(Math.Clamp(reservedRadius * .42f, 8.0f, 66.0f), reservedRadius * .68f);
        var rotation = -.36f;
        var horizontal = ringRadius * 1.78f;
        var vertical = ringRadius * .31f;

        // A tilted accretion disc: a subdued far side, particulate intermediate strokes, then
        // a hot foreground arc. It is deliberately drawn from a fixed small number of vectors.
        Vector2 Ellipse(float angle, float scale = 1.0f)
        {
            var x = MathF.Cos(angle) * horizontal * scale;
            var y = MathF.Sin(angle) * vertical * scale;
            return center + new Vector2(x * MathF.Cos(rotation) - y * MathF.Sin(rotation),
                x * MathF.Sin(rotation) + y * MathF.Cos(rotation));
        }
        for (var band = 0; band < 3; band++)
        {
            var scale = 1.0f - band * .115f;
            for (var segment = 0; segment < 28; segment++)
            {
                var first = segment * MathF.Tau / 28.0f;
                var second = (segment + 1) * MathF.Tau / 28.0f;
                var foreground = MathF.Sin((first + second) * .5f) > -.10f;
                var colour = foreground
                    ? MapAlpha(band == 0 ? new Color("ffbd62") : new Color("d96b2c"), .34f - band * .075f)
                    : MapAlpha(new Color("6a2518"), .24f - band * .045f);
                DrawLine(Ellipse(first, scale), Ellipse(second, scale), colour,
                    foreground ? 1.8f - band * .28f : 1.0f, true);
            }
        }
        // Compact lensing arcs bend around the horizon instead of reading as a UI target ring.
        DrawArc(center + new Vector2(-ringRadius * .42f, -ringRadius * .32f), ringRadius * .80f,
            -.95f, .18f, 18, MapAlpha(new Color("ffd38a"), .46f), 1.15f, true);
        DrawArc(center + new Vector2(ringRadius * .38f, ringRadius * .20f), ringRadius * .98f,
            2.22f, 3.04f, 18, MapAlpha(new Color("e9863e"), .38f), 1.0f, true);
        DrawCircle(center, ringRadius * .52f, new Color("000104"), true, -1, true);
        DrawArc(center, ringRadius * .54f, .18f, 2.86f, 24, MapAlpha(new Color("ffcb75"), .52f), 1.0f, true);
        if (UiOverviewBlend > .08f || _zoom < .72f)
        {
            var label = center + new Vector2(ringRadius * 1.55f, -ringRadius * .52f);
            DrawString(_font, label + Vector2.One, "Galactic core", HorizontalAlignment.Left, -1, 14, Colors.Black);
            DrawString(_font, label, "Galactic core", HorizontalAlignment.Left, -1, 14, MapColor(VisualPalette.TextPrimary));
            DrawString(_font, label + new Vector2(0, 15), "Supermassive black hole · Access unavailable",
                HorizontalAlignment.Left, -1, 10, MapAlpha(new Color("f0ae67"), .90f));
        }
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
        SpaceArtwork.DrawDeepField(this, size, UiGalaxyDeepFieldOpacity);
        var regionalOpacity = UiRegionalBackdropOpacity;
        if (regionalOpacity > .002f)
        {
            // At stellar-region scale the background is a local sky, not a deep-field photo:
            // visible stars, clusters, and nebula replace resolved external galaxies.
            DrawRegionalBackdrop(size, regionalOpacity);
            SpaceArtwork.DrawNebula(this, size, _pan, .56f * regionalOpacity);
        }
        if (UiOverviewBlend > 0)
        {
            SpaceArtwork.DrawGalaxyOverview(this, UiGalaxyArtworkScreenRect, _galaxy?.Seed ?? 0, UiOverviewBlend,
                _galaxy?.GenerationMetadata?.GalaxyShape == "Barred spiral");
        }
    }

    private readonly record struct RegionalBackdropStar(Vector2 Position, float Radius, float Alpha, Color Color);

    private void DrawRegionalBackdrop(Vector2 size, float opacity)
    {
        var seed = _galaxy?.Seed ?? 0;
        if (_regionalBackdropSize != size || _regionalBackdropSeed != seed)
            RebuildRegionalBackdrop(size, seed);

        // A slight pan parallax makes the field read as distant scenery. The coordinates are
        // decorative only and intentionally never enter catalogue hit testing.
        var parallaxFactor = .012f + Math.Min(.045f, _zoom * .0012f);
        var parallax = _pan * parallaxFactor;
        foreach (var star in _regionalBackdropStars)
        {
            var position = new Vector2(WrapBackdropCoordinate(star.Position.X + parallax.X, size.X),
                WrapBackdropCoordinate(star.Position.Y + parallax.Y, size.Y));
            DrawCircle(position, star.Radius, new Color(star.Color.R, star.Color.G, star.Color.B, star.Alpha * opacity), true, -1, true);
        }
    }

    private static float WrapBackdropCoordinate(float value, float extent)
    {
        if (extent <= 0) return 0;
        var wrapped = value % extent;
        return wrapped < 0 ? wrapped + extent : wrapped;
    }

    private void RebuildRegionalBackdrop(Vector2 size, long seed)
    {
        _regionalBackdropSize = size;
        _regionalBackdropSeed = seed;
        _regionalBackdropStars.Clear();
        var random = new Random(unchecked((int)(seed ^ (seed >> 32) ^ 0x4d4150)));
        void AddStar(float x, float y, bool clustered)
        {
            var cool = random.NextDouble();
            var color = cool < .16 ? new Color("96bfff") : cool > .87 ? new Color("ffd6ab") : new Color("d8e5ff");
            _regionalBackdropStars.Add(new RegionalBackdropStar(new Vector2(x, y),
                clustered ? .42f + (float)random.NextDouble() * .72f : .32f + (float)random.NextDouble() * .62f,
                clustered ? .16f + (float)random.NextDouble() * .25f : .09f + (float)random.NextDouble() * .19f, color));
        }
        for (var index = 0; index < RegionalBackdropStarCount; index++)
            AddStar((float)random.NextDouble() * size.X, (float)random.NextDouble() * size.Y, false);
        for (var cluster = 0; cluster < 3; cluster++)
        {
            var center = new Vector2((.18f + (float)random.NextDouble() * .64f) * size.X,
                (.18f + (float)random.NextDouble() * .64f) * size.Y);
            for (var index = 0; index < RegionalBackdropClusterStarCount / 3; index++)
            {
                var angle = (float)random.NextDouble() * MathF.Tau;
                var distance = MathF.Sqrt((float)random.NextDouble()) * Math.Min(size.X, size.Y) * .105f;
                AddStar(center.X + MathF.Cos(angle) * distance, center.Y + MathF.Sin(angle) * distance, true);
            }
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

    /// <summary>Regional catalogue stars are luminous points: a compact hot core, colored
    /// corona, and tapered diffraction rays. They never resolve into a solar surface.</summary>
    private void DrawSpectralCatalogStar(int systemId, Vector2 position, float haloRadius, Color spectral, float surveyOpacity)
    {
        var opacity = CatalogOpacity * surveyOpacity;
        var coreRadius = UiCatalogStarCoreRadius(systemId);
        var regional = RegionalOpacity;
        // The shared glow is a resolution-independent radial gradient and needs no per-star
        // material or node. The large soft layer carries the spectral identity.
        var halo = haloRadius * (UiOverviewBlend > .10f ? .62f : 1.0f);
        DrawTextureRect(CinematicArt.Glow, new Rect2(position - Vector2.One * halo, Vector2.One * halo * 2), false,
            new Color(spectral.R, spectral.G, spectral.B, (.30f + regional * .20f) * opacity));
        var innerHalo = haloRadius * .54f;
        DrawTextureRect(CinematicArt.Glow, new Rect2(position - Vector2.One * innerHalo, Vector2.One * innerHalo * 2), false,
            new Color(spectral.R, spectral.G, spectral.B, (.46f + regional * .22f) * opacity));

        var ray = Math.Clamp(haloRadius * .82f, 10.0f, 15.0f);
        var innerRay = ray * .52f;
        var rayColor = new Color(spectral.R, spectral.G, spectral.B, (.20f + regional * .22f) * opacity);
        var brightRay = new Color(1f, .97f, .91f, (.35f + regional * .20f) * opacity);
        // Paired outer and inner strokes make each ray fade toward its end without any
        // geometry allocation. Diagonals are shorter and quieter than the cardinal flare.
        DrawLine(position - new Vector2(ray, 0), position + new Vector2(ray, 0), rayColor, .55f, true);
        DrawLine(position - new Vector2(0, ray), position + new Vector2(0, ray), rayColor, .55f, true);
        DrawLine(position - new Vector2(innerRay, 0), position + new Vector2(innerRay, 0), brightRay, .72f, true);
        DrawLine(position - new Vector2(0, innerRay), position + new Vector2(0, innerRay), brightRay, .72f, true);
        var diagonal = ray * .63f;
        var diagonalColor = new Color(spectral.R, spectral.G, spectral.B, (.11f + regional * .12f) * opacity);
        DrawLine(position - new Vector2(diagonal, diagonal), position + new Vector2(diagonal, diagonal), diagonalColor, .46f, true);
        DrawLine(position - new Vector2(diagonal, -diagonal), position + new Vector2(diagonal, -diagonal), diagonalColor, .46f, true);
        DrawCircle(position, coreRadius, new Color(1f, .985f, .94f, .98f * opacity), true, -1, true);
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

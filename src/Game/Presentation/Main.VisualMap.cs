using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Presentation;

public partial class Main
{
    private readonly Dictionary<(FleetRole Role, System.Numerics.Vector2 Position), (FleetState Fleet, int Count)> _visualFleetGroups = new();

    /// <summary>
    /// Complete regional presentation. Stellar coordinates are the existing catalog transform;
    /// colors/classes use survey confidence. Fleets are exact-own; foreign settlements and homes
    /// retain the established full-survey and known-civilization gates.
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
        DrawVisualPlayerRoutes(center, playerId);

        foreach (var system in _galaxy.Systems)
        {
            var position = ToScreen(system.Position, center);
            if (position.X < -80 || position.Y < -80 || position.X > viewport.X + 80 || position.Y > viewport.Y + 80)
                continue;

            var survey = _galaxy.Knowledge.GetSystemSurveyLevel(playerId, system.Id);
            var selected = system.Id == _selectedSystemId;
            var home = system.Id == homeId;
            // Catalog coordinates are public. Class, color and catalog names still obey knowledge.
            var color = survey == SystemSurveyLevel.FullySurveyed
                ? GetStarColor(system.Archetype)
                : new Color(0.63f, 0.70f, 0.79f);
            var radius = Math.Clamp(2.3f + _zoom * 1.6f, 2.5f, 5.0f);
            if (survey == SystemSurveyLevel.Unknown)
                radius *= 0.73f;

            DrawCircle(position, radius * 4.2f, VisualPalette.WithAlpha(color, survey == SystemSurveyLevel.Unknown ? 0.018f : 0.045f));
            DrawCircle(position, radius * 2.4f, VisualPalette.WithAlpha(color, survey == SystemSurveyLevel.Unknown ? 0.055f : 0.13f));
            if (survey == SystemSurveyLevel.FullySurveyed && system.Archetype == StarArchetype.BlackHole)
            {
                DrawCircle(position, radius + 1.2f, VisualPalette.Canvas);
                DrawArc(position, radius + 1.5f, -0.6f, 5.0f, 32, color, 1.6f, true);
            }
            else
            {
                DrawCircle(position, radius, VisualPalette.WithAlpha(color, survey == SystemSurveyLevel.Unknown ? 0.70f : 1.0f));
                if (survey >= SystemSurveyLevel.PartiallySurveyed)
                    DrawCircle(position, Math.Max(1.0f, radius * 0.43f), new Color(0.94f, 0.98f, 1.0f));
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
                    VisualPalette.WithAlpha(survey == SystemSurveyLevel.FullySurveyed ? color : VisualPalette.TextSecondary, 0.48f), 1.0f, true);
            }

            if (selected)
                DrawRegionalReticle(position, 19.0f, VisualPalette.Selected);
            if (selected || home || (survey >= SystemSurveyLevel.PartiallySurveyed && _zoom >= 0.88f))
            {
                var label = _galaxy.Knowledge.IsSystemKnown(playerId, system.Id) ? system.Name : $"CATALOG {system.Id + 1:000}";
                var labelColor = selected ? VisualPalette.TextPrimary : VisualPalette.TextSecondary;
                var labelPosition = position + new Vector2(18.0f, -13.0f);
                DrawString(_font, labelPosition + Vector2.One, label, HorizontalAlignment.Left, -1, selected || home ? 14 : 12, VisualPalette.Canvas);
                DrawString(_font, labelPosition, label, HorizontalAlignment.Left, -1, selected || home ? 14 : 12, labelColor);
                if (home)
                    DrawString(_font, labelPosition + new Vector2(0.0f, 15.0f), "HOME SYSTEM", HorizontalAlignment.Left, -1, 9, VisualPalette.Success);
            }
        }

        DrawVisualColonies(center, playerId);
        DrawVisualKnownCivilizationHomes(center, playerId);
        DrawVisualPlayerFleets(center, playerId);
    }

    private void DrawRegionalSpace(Vector2 size)
    {
        DrawRect(new Rect2(Vector2.Zero, size), new Color(0.012f, 0.025f, 0.044f));
        // Screen-anchored, deterministic ambience: no random flicker and no simulated objects.
        for (var layer = 10; layer >= 1; layer--)
        {
            DrawCircle(new Vector2(size.X * 0.67f, size.Y * 0.40f), size.X * (0.15f + layer * 0.024f),
                new Color(0.10f, 0.24f, 0.34f, 0.006f));
            DrawCircle(new Vector2(size.X * 0.26f, size.Y * 0.73f), size.X * (0.07f + layer * 0.014f),
                new Color(0.21f, 0.14f, 0.31f, 0.005f));
        }
        for (uint index = 1; index <= 190; index++)
        {
            var hash = index * 2654435761u;
            var x = (hash & 0xFFFFu) / 65535.0f * size.X;
            hash = unchecked(hash * 2246822519u + 3266489917u);
            var y = (hash & 0xFFFFu) / 65535.0f * size.Y;
            DrawCircle(new Vector2(x, y), index % 9 == 0 ? 0.85f : 0.50f,
                new Color(0.60f, 0.73f, 0.89f, index % 9 == 0 ? 0.28f : 0.13f));
        }
        var spacing = 96.0f;
        for (var x = spacing; x < size.X; x += spacing)
            for (var y = spacing; y < size.Y; y += spacing)
                DrawLine(new Vector2(x - 1.5f, y), new Vector2(x + 1.5f, y), new Color(0.24f, 0.40f, 0.53f, 0.17f), 1.0f);
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
            var color = own ? VisualPalette.Success : new Color(0.66f, 0.62f, 0.77f);
            DrawLine(anchor + new Vector2(-5.0f, -5.0f), marker, VisualPalette.WithAlpha(color, 0.42f), 1.0f, true);
            DrawCircle(marker, 9.0f, VisualPalette.Canvas);
            DrawVisualIcon(VisualIconLibrary.Colony, marker, own ? 17.0f : 15.0f, color);
            if (!own)
                DrawCircle(marker, 10.0f, VisualPalette.WithAlpha(color, 0.52f), false, 0.8f, true);
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
            var color = civilization.IsSeededAncient
                ? new Color(0.80f, 0.67f, 0.42f) : new Color(0.66f, 0.62f, 0.77f);
            DrawLine(anchor + new Vector2(4.0f, -8.0f), marker, VisualPalette.WithAlpha(color, 0.34f), 1.0f, true);
            DrawCircle(marker, 9.0f, VisualPalette.Canvas);
            DrawVisualIcon(VisualIconLibrary.DiplomacyContact, marker, 15.0f, color);
        }
    }

    private void DrawVisualPlayerRoutes(Vector2 center, int playerId)
    {
        foreach (var fleet in _galaxy.Fleets)
        {
            if (!fleet.IsActive || fleet.CivilizationId != playerId || fleet.DestinationSystemId is not int destinationId)
                continue;
            var destination = _galaxy.Systems.First(system => system.Id == destinationId);
            var start = ToScreen(fleet.Position, center);
            var end = ToScreen(destination.Position, center);
            var color = FleetRoleColor(fleet.Role);
            DrawLine(start, end, VisualPalette.WithAlpha(color, 0.07f), 5.0f, true);
            DrawDashedLine(start, end, VisualPalette.WithAlpha(color, 0.60f), 1.15f, 8.0f);
            if (start.DistanceSquaredTo(end) > 1600.0f)
            {
                var direction = (end - start).Normalized();
                var normal = new Vector2(-direction.Y, direction.X);
                var tip = start.Lerp(end, 0.62f);
                DrawLine(tip, tip - direction * 7.0f + normal * 3.5f, color, 1.3f, true);
                DrawLine(tip, tip - direction * 7.0f - normal * 3.5f, color, 1.3f, true);
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
            // Separate stationary role markers without moving the authoritative fleet position.
            var offset = fleet.Role switch
            {
                FleetRole.Scout => new Vector2(-17.0f, 23.0f),
                FleetRole.Science => new Vector2(17.0f, 23.0f),
                FleetRole.Colony => new Vector2(-17.0f, 48.0f),
                _ => new Vector2(17.0f, 48.0f),
            };
            if (fleet.DestinationSystemId.HasValue)
                offset *= 0.55f;
            var position = anchor + offset;
            var color = FleetRoleColor(fleet.Role);
            DrawLine(anchor, position, VisualPalette.WithAlpha(color, 0.36f), 1.0f, true);
            DrawCircle(position, 13.0f, new Color(0.025f, 0.055f, 0.080f, 0.96f));
            DrawCircle(position, 13.0f, VisualPalette.WithAlpha(color, 0.50f), false, 1.0f, true);
            DrawVisualIcon(FleetRoleTexture(fleet.Role), position, 23.0f, color);
            if (group.Count > 1)
            {
                var badge = position + new Vector2(10.0f, -10.0f);
                DrawCircle(badge, 8.0f, VisualPalette.SurfacePrimary);
                DrawCircle(badge, 8.0f, color, false, 1.0f, true);
                DrawString(_font, badge + new Vector2(-8.0f, 3.0f), group.Count > 99 ? "99+" : group.Count.ToString(),
                    HorizontalAlignment.Center, 16.0f, 9, VisualPalette.TextPrimary);
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
        DrawCircle(position, radius + 4.0f, VisualPalette.WithAlpha(color, 0.11f), false, 1.0f, true);
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

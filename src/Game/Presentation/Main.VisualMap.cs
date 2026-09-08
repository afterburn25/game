using System.Linq;
using Godot;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Presentation;

public partial class Main
{
    /// <summary>
    /// Draws the production-candidate strategic-map vocabulary over the legacy prototype
    /// circles. This is presentation-only: it consumes existing fair-information state and
    /// never changes what the player knows or what commands are available.
    /// </summary>
    protected void DrawVisualMapOverlay()
    {
        if (_galaxy is null)
            return;

        var playerId = _galaxy.PlayerCivilizationId;
        var center = GetViewportRect().Size * 0.5f + _pan;

        foreach (var system in _galaxy.Systems)
        {
            var surveyLevel = _galaxy.Knowledge.GetSystemSurveyLevel(playerId, system.Id);
            var position = ToScreen(system.Position, center);
            var selected = system.Id == _selectedSystemId;

            // Common-catalog unknown stars remain intentionally quiet. Only a selected unknown
            // target receives the explicit unknown symbol so a 120-system map does not become
            // a wall of question marks.
            if (surveyLevel == SystemSurveyLevel.Unknown)
            {
                if (selected)
                    DrawVisualIcon(VisualIconLibrary.Unknown, position, 14.0f, VisualPalette.Unknown);
                continue;
            }

            var texture = surveyLevel switch
            {
                SystemSurveyLevel.Detected => VisualIconLibrary.SurveyDetected,
                SystemSurveyLevel.PartiallySurveyed => VisualIconLibrary.SurveyPartial,
                _ => VisualIconLibrary.SurveyFull,
            };
            var size = surveyLevel switch
            {
                SystemSurveyLevel.Detected => 9.0f,
                SystemSurveyLevel.PartiallySurveyed => 11.0f,
                _ => 12.5f,
            };
            var color = surveyLevel switch
            {
                SystemSurveyLevel.Detected => VisualPalette.WithAlpha(VisualPalette.TextMuted, 0.86f),
                SystemSurveyLevel.PartiallySurveyed => VisualPalette.WithAlpha(VisualPalette.TextSecondary, 0.94f),
                _ => GetStarColor(system.Archetype),
            };

            DrawVisualIcon(texture, position, size, color);

            if (selected)
                DrawCircle(position, 15.5f, VisualPalette.WithAlpha(VisualPalette.Focus, 0.72f), false, 1.5f);
        }

        DrawVisualColonies(center, playerId);
        DrawVisualPlayerFleets(center, playerId);
    }

    private void DrawVisualColonies(Vector2 center, int playerId)
    {
        foreach (var colony in _galaxy.Colonies)
        {
            var own = colony.CivilizationId == playerId;
            if (!own &&
                (!_galaxy.Knowledge.IsSystemFullySurveyed(playerId, colony.SystemId) ||
                 !_galaxy.Knowledge.IsCivilizationKnown(playerId, colony.CivilizationId)))
            {
                continue;
            }

            var system = _galaxy.Systems.First(candidate => candidate.Id == colony.SystemId);
            var color = own ? VisualPalette.Success : VisualPalette.TextSecondary;
            DrawVisualIcon(VisualIconLibrary.Colony, ToScreen(system.Position, center), 16.0f, color);
        }
    }

    private void DrawVisualPlayerFleets(Vector2 center, int playerId)
    {
        foreach (var fleet in _galaxy.Fleets.Where(candidate => candidate.IsActive && candidate.CivilizationId == playerId))
        {
            var position = ToScreen(fleet.Position, center);
            var color = FleetRoleColor(fleet.Role);

            if (fleet.DestinationSystemId is { } destinationId)
            {
                var destination = _galaxy.Systems.First(system => system.Id == destinationId);
                DrawDashedLine(
                    position,
                    ToScreen(destination.Position, center),
                    VisualPalette.WithAlpha(color, 0.46f),
                    1.15f,
                    7.0f);
            }

            DrawVisualIcon(FleetRoleTexture(fleet.Role), position, 17.0f, color);
            DrawCircle(position, 11.5f, VisualPalette.WithAlpha(color, 0.22f), false, 1.0f);
        }
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
        DrawTextureRect(
            texture,
            new Rect2(center.X - half, center.Y - half, size, size),
            false,
            color);
    }
}

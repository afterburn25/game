using System;
using System.Collections.Generic;
using Godot;

namespace Game.Presentation;

/// <summary>
/// Lazy runtime loader for production-candidate visual SVGs.
/// Centralizing paths prevents presentation code from duplicating asset strings and lets
/// import failures surface immediately in the real Godot runtime/screenshot gate.
/// </summary>
public static class VisualIconLibrary
{
    private static readonly Dictionary<string, Texture2D> Cache = new(StringComparer.Ordinal);

    public const string SurveyDetectedPath = "res://assets/visual/icons/map/icon_map_detected.svg";
    public const string SurveyPartialPath = "res://assets/visual/icons/map/icon_map_partially_surveyed.svg";
    public const string SurveyFullPath = "res://assets/visual/icons/map/icon_map_fully_surveyed.svg";
    public const string UnknownPath = "res://assets/visual/icons/core/icon_status_unknown.svg";
    public const string ColonyPath = "res://assets/visual/icons/core/icon_map_colony.svg";
    public const string ScoutPath = "res://assets/visual/icons/core/icon_map_scout.svg";
    public const string ScienceVesselPath = "res://assets/visual/icons/ships/icon_ship_science_vessel.svg";
    public const string PatrolCorvettePath = "res://assets/visual/icons/ships/icon_ship_patrol_corvette.svg";
    public const string ColonyShipPath = "res://assets/visual/icons/ships/icon_ship_colony_ship.svg";

    public static Texture2D SurveyDetected => Get(SurveyDetectedPath);
    public static Texture2D SurveyPartial => Get(SurveyPartialPath);
    public static Texture2D SurveyFull => Get(SurveyFullPath);
    public static Texture2D Unknown => Get(UnknownPath);
    public static Texture2D Colony => Get(ColonyPath);
    public static Texture2D Scout => Get(ScoutPath);
    public static Texture2D ScienceVessel => Get(ScienceVesselPath);
    public static Texture2D PatrolCorvette => Get(PatrolCorvettePath);
    public static Texture2D ColonyShip => Get(ColonyShipPath);

    public static Texture2D Get(string resourcePath)
    {
        if (Cache.TryGetValue(resourcePath, out var cached))
            return cached;

        var texture = GD.Load<Texture2D>(resourcePath)
            ?? throw new InvalidOperationException($"Visual asset could not be loaded: {resourcePath}");
        Cache.Add(resourcePath, texture);
        return texture;
    }
}

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

    public const string PausePath = "res://assets/visual/icons/core/icon_hud_pause.svg";
    public const string SpeedPath = "res://assets/visual/icons/core/icon_hud_speed.svg";
    public const string SavePath = "res://assets/visual/icons/core/icon_hud_save.svg";
    public const string SupportPath = "res://assets/visual/icons/core/icon_hud_support.svg";
    public const string ResearchPath = "res://assets/visual/icons/core/icon_action_research.svg";
    public const string ConstructionPath = "res://assets/visual/icons/core/icon_action_construction.svg";
    public const string ExplorationPath = "res://assets/visual/icons/core/icon_system_exploration.svg";
    public const string LogisticsPath = "res://assets/visual/icons/core/icon_system_logistics.svg";
    public const string RelationsPath = "res://assets/visual/icons/core/icon_system_relations.svg";
    public const string SurveyDetectedPath = "res://assets/visual/icons/map/icon_map_detected.svg";
    public const string SurveyPartialPath = "res://assets/visual/icons/map/icon_map_partially_surveyed.svg";
    public const string SurveyFullPath = "res://assets/visual/icons/map/icon_map_fully_surveyed.svg";
    public const string UnknownPath = "res://assets/visual/icons/core/icon_status_unknown.svg";
    public const string ColonyPath = "res://assets/visual/icons/core/icon_map_colony.svg";
    public const string ScoutPath = "res://assets/visual/icons/core/icon_map_scout.svg";
    public const string ScienceVesselPath = "res://assets/visual/icons/ships/icon_ship_science_vessel.svg";
    public const string PatrolCorvettePath = "res://assets/visual/icons/ships/icon_ship_patrol_corvette.svg";
    public const string ColonyShipPath = "res://assets/visual/icons/ships/icon_ship_colony_ship.svg";

    public static Texture2D Pause => Get(PausePath);
    public static Texture2D Speed => Get(SpeedPath);
    public static Texture2D Save => Get(SavePath);
    public static Texture2D Support => Get(SupportPath);
    public static Texture2D Research => Get(ResearchPath);
    public static Texture2D Construction => Get(ConstructionPath);
    public static Texture2D Exploration => Get(ExplorationPath);
    public static Texture2D Logistics => Get(LogisticsPath);
    public static Texture2D Relations => Get(RelationsPath);
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

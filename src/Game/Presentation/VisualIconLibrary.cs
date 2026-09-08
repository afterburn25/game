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
    public const string InfoPath = "res://assets/visual/icons/core/icon_status_info.svg";
    public const string SuccessPath = "res://assets/visual/icons/core/icon_status_success.svg";
    public const string SurveyDetectedPath = "res://assets/visual/icons/map/icon_map_detected.svg";
    public const string SurveyPartialPath = "res://assets/visual/icons/map/icon_map_partially_surveyed.svg";
    public const string SurveyFullPath = "res://assets/visual/icons/map/icon_map_fully_surveyed.svg";
    public const string UnknownPath = "res://assets/visual/icons/core/icon_status_unknown.svg";
    public const string ColonyPath = "res://assets/visual/icons/core/icon_map_colony.svg";
    public const string ScoutPath = "res://assets/visual/icons/core/icon_map_scout.svg";
    public const string ScienceVesselPath = "res://assets/visual/icons/ships/icon_ship_science_vessel.svg";
    public const string PatrolCorvettePath = "res://assets/visual/icons/ships/icon_ship_patrol_corvette.svg";
    public const string ColonyShipPath = "res://assets/visual/icons/ships/icon_ship_colony_ship.svg";
    public const string DiplomacyContactPath = "res://assets/visual/icons/diplomacy/icon_diplomacy_contact.svg";
    public const string DiplomacyAgreementPath = "res://assets/visual/icons/diplomacy/icon_diplomacy_agreement.svg";
    public const string DiplomacyPeacePath = "res://assets/visual/icons/diplomacy/icon_diplomacy_peace.svg";
    public const string DiplomacyCeasefirePath = "res://assets/visual/icons/diplomacy/icon_diplomacy_ceasefire.svg";
    public const string DiplomacyAccessGrantedPath = "res://assets/visual/icons/diplomacy/icon_diplomacy_access_granted.svg";
    public const string DiplomacyAccessDeniedPath = "res://assets/visual/icons/diplomacy/icon_diplomacy_access_denied.svg";

    public static Texture2D Pause => Get(PausePath);
    public static Texture2D Speed => Get(SpeedPath);
    public static Texture2D Save => Get(SavePath);
    public static Texture2D Support => Get(SupportPath);
    public static Texture2D Research => Get(ResearchPath);
    public static Texture2D Construction => Get(ConstructionPath);
    public static Texture2D Exploration => Get(ExplorationPath);
    public static Texture2D Logistics => Get(LogisticsPath);
    public static Texture2D Relations => Get(RelationsPath);
    public static Texture2D Info => Get(InfoPath);
    public static Texture2D Success => Get(SuccessPath);
    public static Texture2D SurveyDetected => Get(SurveyDetectedPath);
    public static Texture2D SurveyPartial => Get(SurveyPartialPath);
    public static Texture2D SurveyFull => Get(SurveyFullPath);
    public static Texture2D Unknown => Get(UnknownPath);
    public static Texture2D Colony => Get(ColonyPath);
    public static Texture2D Scout => Get(ScoutPath);
    public static Texture2D ScienceVessel => Get(ScienceVesselPath);
    public static Texture2D PatrolCorvette => Get(PatrolCorvettePath);
    public static Texture2D ColonyShip => Get(ColonyShipPath);
    public static Texture2D DiplomacyContact => Get(DiplomacyContactPath);
    public static Texture2D DiplomacyAgreement => Get(DiplomacyAgreementPath);
    public static Texture2D DiplomacyPeace => Get(DiplomacyPeacePath);
    public static Texture2D DiplomacyCeasefire => Get(DiplomacyCeasefirePath);
    public static Texture2D DiplomacyAccessGranted => Get(DiplomacyAccessGrantedPath);
    public static Texture2D DiplomacyAccessDenied => Get(DiplomacyAccessDeniedPath);

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

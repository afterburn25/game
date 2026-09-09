using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Construction;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;
using Game.Simulation.Research;
using Game.Simulation.Shipbuilding;
using Game.Simulation.Time;

namespace Game.Presentation;

public sealed record UiProjectCard(string Title, string Detail, double Progress,
    double Current, double Cost, bool IsActive)
{
    public static UiProjectCard Empty { get; } = new("Initializing", "", 0, 0, 0, false);
}

/// <summary>A directly selectable operation shown on a department page.</summary>
public sealed record UiOperationChoice(string Id, string Title, string Detail, string CostLabel);

public sealed record UiDashboardSnapshot(
    string CivilizationName, string Date, string SelectedSystemName, string SelectedSurveyLabel,
    double Credits, double Industry, double Science,
    double CreditsPerDay, double IndustryPerDay, double SciencePerDay,
    int ColonyCount, int FleetCount, int KnownSystemCount, int TotalSystemCount, int DemoStep,
    UiProjectCard Research, UiProjectCard Construction, UiProjectCard Shipyard);

public partial class Main
{
    public IReadOnlyList<UiOperationChoice> UiResearchChoices => _galaxy is null || PlayerTechnology.ActiveResearchId is not null
        ? Array.Empty<UiOperationChoice>()
        : TechnologyRegistry.GetAvailable(PlayerTechnology, PlayerConstruction)
            .Select(item => new UiOperationChoice(item.Id, item.Name, item.Description, $"{item.ResearchCost:N0} science"))
            .ToArray();

    public IReadOnlyList<UiOperationChoice> UiConstructionChoices => _galaxy is null || PlayerConstruction.ActiveProjectId is not null
        ? Array.Empty<UiOperationChoice>()
        : ConstructionRegistry.GetAvailable(PlayerConstruction, PlayerTechnology)
            .Select(item => new UiOperationChoice(item.Id, item.Name, item.Description, $"{item.IndustryCost:N0} industry"))
            .ToArray();

    public IReadOnlyList<UiOperationChoice> UiShipChoices => _galaxy is null
        ? Array.Empty<UiOperationChoice>()
        : _shipbuilding.GetAvailableDesigns(_galaxy, _galaxy.PlayerCivilizationId)
            .Select(item => new UiOperationChoice(item.Id, item.Name, item.Description, $"{item.IndustryCost:N0} industry"))
            .ToArray();

    /// <summary>Read-only display values; command handlers retain all eligibility checks.</summary>
    public UiDashboardSnapshot UiDashboard
    {
        get
        {
            // Child controls enter the scene before the campaign is initialized by Main.
            if (_galaxy is null)
                return new("Stellar Continuum", "", "Select a star", "", 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, UiProjectCard.Empty, UiProjectCard.Empty, UiProjectCard.Empty);

            var player = PlayerCivilization;
            var economy = PlayerEconomy;
            var technology = PlayerTechnology;
            var construction = PlayerConstruction;
            var shipyard = PlayerShipyard;
            var selected = _galaxy.Systems.FirstOrDefault(system => system.Id == _selectedSystemId);
            var survey = selected is null ? SystemSurveyLevel.Unknown :
                _galaxy.Knowledge.GetSystemSurveyLevel(player.Id, selected.Id);
            var selectedName = selected is null ? "Select a star" : survey == SystemSurveyLevel.Unknown
                ? $"Astronomical target {selected.Id + 1:000}" : selected.Name;
            var surveyLabel = selected is null ? "Choose a destination on the map" : survey switch
            {
                SystemSurveyLevel.FullySurveyed => "Fully surveyed",
                SystemSurveyLevel.PartiallySurveyed => "Partial survey",
                SystemSurveyLevel.Detected => "Detected · survey required",
                _ => "Unknown · reconnaissance required",
            };
            var colonies = _galaxy.Colonies.Count(colony => colony.CivilizationId == player.Id);
            var fleets = _galaxy.Fleets.Where(fleet => fleet.IsActive && fleet.CivilizationId == player.Id).ToArray();
            var demoStep = colonies > 1 ? 3 : !technology.CompletedTechnologyIds.Contains("prototype_warp_drive") ? 0 :
                new[] { FleetRole.Scout, FleetRole.Science, FleetRole.Colony }.All(role => fleets.Any(fleet => fleet.Role == role)) ? 2 : 1;

            var research = technology.ActiveResearchId is { } researchId
                ? TechnologyRegistry.Get(researchId) : GetResearchCandidate();
            var project = construction.ActiveProjectId is { } projectId
                ? ConstructionRegistry.Get(projectId) : GetConstructionCandidate();
            var ship = shipyard.ActiveDesignId is { } shipId
                ? ShipDesignRegistry.Get(shipId) : GetShipDesignCandidate();
            var nextShip = GetShipDesignCandidate();
            return new(player.Name, CampaignCalendar.FormatDate(_clock.SimulationDays), selectedName, surveyLabel,
                economy.Credits, economy.Industry, economy.Science,
                economy.LastCreditsPerSecond, economy.LastIndustryPerSecond, economy.LastSciencePerSecond,
                colonies, fleets.Length, _galaxy.Knowledge.GetKnownSystems(player.Id).Count, _galaxy.Systems.Count, demoStep,
                research is null ? new("No research available", "Complete required infrastructure to unlock the next discoveries. The opening guide suggests the next step.", 0, 0, 0, false)
                    : Card(research.Name, research.Description, technology.ActiveResearchProgress, research.ResearchCost, technology.ActiveResearchId is not null),
                project is null ? new("Infrastructure ready", "Research new technologies to unlock more projects.", 0, 0, 0, false)
                    : Card(project.Name, project.Description, construction.ActiveProjectProgress, project.IndustryCost, construction.ActiveProjectId is not null),
                ship is null ? new("Shipyard locked", "Complete orbital infrastructure and propulsion research to unlock designs.", 0, 0, 0, false)
                    : Card(ship.Name, $"Selected for next build: {nextShip?.Name ?? "No design available"}\n\n{ship.Description}\n{shipyard.PendingBuildCount} build(s) in queue", shipyard.ActiveBuildProgress, ship.IndustryCost, shipyard.ActiveDesignId is not null));
        }
    }

    private static UiProjectCard Card(string title, string detail, double current, double cost, bool active) =>
        new(title, detail, active && cost > 0 ? Math.Clamp(current / cost, 0, 1) : 0, active ? current : 0, cost, active);
}

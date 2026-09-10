using System;
using Game.Simulation.Construction;

namespace Game.Presentation;

public sealed record UiOrbitalConstruction(string Id, string Name, string Description, string State,
    double Progress, double DaysRemaining, string Cost, string Upkeep, double MaterialOutput,
    bool CanBuild, string? LockReason);

public partial class Main
{
    private string? _orbitalProjectId;
    public void UiCloseOrbitalInspector() => _orbitalProjectId = null;
    private void InspectOrbitalStructure(string id)
    {
        if (ConstructionRegistry.Find(id)?.Category != ConstructionCategory.Orbital) return;
        UiClearFleetSelection();
        GetNode<CampaignSidebar>("CampaignSidebar").CloseDrawer();
        _orbitalProjectId = id;
    }
    public UiOrbitalConstruction? UiSelectedOrbitalConstruction
    {
        get
        {
            if (!UiIsSystemSpatialView || _selectedSystemId != PlayerCivilization.HomeSystemId ||
                _orbitalProjectId is null || ConstructionRegistry.Find(_orbitalProjectId) is not { } project) return null;
            var state = PlayerConstruction;
            var complete = state.CompletedProjectIds.Contains(project.Id);
            var active = state.ActiveProjectId == project.Id;
            var reason = _construction.GetLockReason(_galaxy, _galaxy.PlayerCivilizationId, project);
            if (!complete && !active && state.ActiveProjectId is not null) reason ??= "Finish the current infrastructure project first.";
            if (!complete && !active && PlayerEconomy.Credits < project.CreditCost) reason ??= "Insufficient funds for construction authorization.";
            return new(project.Id, project.Name, project.Description, complete ? "Operational" : active ? "Under construction" : "Planned orbital site",
                complete ? 1 : active ? state.ActiveProjectProgress / project.IndustryCost : 0,
                complete ? 0 : Math.Max(0, project.IndustryCost - (active ? state.ActiveProjectProgress : 0)) / ConstructionSimulation.IndustryPerDay,
                $"{UiFormatMoney(project.CreditCost)} · {project.IndustryCost:N0} materials",
                UiFormatMoney(project.UpkeepCreditsPerDay) + " / day", project.IndustryPerDay,
                !complete && !active && reason is null, reason);
        }
    }
}

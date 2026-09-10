using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Construction;

public sealed class ConstructionSimulation
{
    public const double IndustryPerDay = 30;
    private readonly IConstructionCapabilityView _capabilityView;

    public ConstructionSimulation(IConstructionCapabilityView? capabilityView = null) =>
        _capabilityView = capabilityView ?? new PrototypeConstructionCapabilityView();

    public IReadOnlyList<ConstructionEvent> Advance(
        GalaxyState galaxy,
        IReadOnlyDictionary<int, double>? industryBudgets = null,
        double simulationDays = 1) =>
        AdvanceCore(galaxy, industryBudgets, null, simulationDays);

    public IReadOnlyList<ConstructionEvent> AdvanceForCivilization(GalaxyState galaxy, int civilizationId,
        double industryBudget, double simulationDays) => AdvanceCore(galaxy,
            new Dictionary<int, double> { [civilizationId] = industryBudget }, civilizationId, simulationDays);

    private IReadOnlyList<ConstructionEvent> AdvanceCore(GalaxyState galaxy,
        IReadOnlyDictionary<int, double>? industryBudgets, int? onlyCivilizationId, double simulationDays)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (!double.IsFinite(simulationDays) || simulationDays < 0) throw new ArgumentOutOfRangeException(nameof(simulationDays));
        if (simulationDays == 0) return Array.Empty<ConstructionEvent>();
        if (onlyCivilizationId is null) EnsureAutomaticOrders(galaxy);

        var events = new List<ConstructionEvent>();

        foreach (var civilization in galaxy.Civilizations)
        {
            if (onlyCivilizationId is int selected && civilization.Id != selected) continue;
            if (civilization.IsSeededAncient)
                continue;

            var state = galaxy.ConstructionStates.First(c => c.CivilizationId == civilization.Id);
            var economy = galaxy.Economies.First(e => e.CivilizationId == civilization.Id);

            var availableIndustry = ResolveBudget(industryBudgets, civilization.Id, economy.Industry);
            var surfaceDemand = SurfaceConstruction.GetIndustryDemand(galaxy, civilization.Id, simulationDays);
            var projectDemand = state.ActiveProjectId is null ? 0 :
                Math.Min(IndustryPerDay * simulationDays, Math.Max(0, ConstructionRegistry.Get(state.ActiveProjectId).IndustryCost - state.ActiveProjectProgress));
            var surfaceBudget = surfaceDemand > 0
                ? Math.Min(surfaceDemand, Math.Min(availableIndustry, surfaceDemand + projectDemand) *
                    surfaceDemand / (surfaceDemand + projectDemand))
                : 0;
            SurfaceConstruction.Advance(galaxy, civilization.Id, surfaceBudget, simulationDays);
            availableIndustry = Math.Min(economy.Industry, Math.Max(0, availableIndustry - surfaceBudget));

            if (state.ActiveProjectId is null)
                continue;

            var project = ConstructionRegistry.Get(state.ActiveProjectId);
            var remaining = Math.Max(0.0, project.IndustryCost - state.ActiveProjectProgress);
            var spend = Math.Min(remaining, Math.Min(availableIndustry, IndustryPerDay * simulationDays));
            if (spend <= 0.0 && remaining > 0.0001)
                continue;

            economy.Industry -= spend;
            state.ActiveProjectProgress += spend;

            if (state.ActiveProjectProgress + 0.0001 < project.IndustryCost)
                continue;

            state.CompletedProjectIds.Add(project.Id);
            state.ActiveProjectId = null;
            state.ActiveProjectProgress = 0.0;
            events.Add(new ConstructionEvent(civilization.Id, project.Id, $"{civilization.Name} completed {project.Name}."));
        }

        return events;
    }

    /// <summary>
    /// Selects missing AI construction orders without spending Industry. Core simulation can
    /// call this before a shared allocation pass so AI and player orders compete fairly.
    /// </summary>
    public void EnsureAutomaticOrders(GalaxyState galaxy)
    {
        ArgumentNullException.ThrowIfNull(galaxy);

        foreach (var civilization in galaxy.Civilizations)
        {
            if (civilization.IsSeededAncient || civilization.IsPlayer)
                continue;

            var state = galaxy.ConstructionStates.First(c => c.CivilizationId == civilization.Id);
            if (state.ActiveProjectId is not null)
                continue;

            state.ActiveProjectId = SelectAiProject(galaxy, civilization, state)?.Id;
        }
    }

    public double GetIndustryDemand(GalaxyState galaxy, int civilizationId, double simulationDays = double.PositiveInfinity)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        var state = galaxy.ConstructionStates.First(c => c.CivilizationId == civilizationId);
        if (state.ActiveProjectId is null)
            return SurfaceConstruction.GetIndustryDemand(galaxy, civilizationId, simulationDays);

        var project = ConstructionRegistry.Get(state.ActiveProjectId);
        return Math.Min(Math.Max(0.0, project.IndustryCost - state.ActiveProjectProgress), IndustryPerDay * Math.Max(0, simulationDays)) +
            SurfaceConstruction.GetIndustryDemand(galaxy, civilizationId, simulationDays);
    }

    public ConstructionOrderResult StartProject(GalaxyState galaxy, int civilizationId, string projectId)
    {
        var civilization = galaxy.Civilizations.FirstOrDefault(c => c.Id == civilizationId);
        if (civilization is null)
            return new ConstructionOrderResult(false, "Unknown civilization.");

        var state = galaxy.ConstructionStates.First(c => c.CivilizationId == civilizationId);
        if (state.ActiveProjectId is not null)
            return new ConstructionOrderResult(false, "A construction project is already in progress.");

        var project = ConstructionRegistry.Find(projectId);
        if (project is null)
            return new ConstructionOrderResult(false, "Unknown construction project.");
        if (state.CompletedProjectIds.Contains(project.Id))
            return new ConstructionOrderResult(false, $"{project.Name} is already complete.");
        if (GetLockReason(galaxy, civilizationId, project) is { } lockReason)
            return new ConstructionOrderResult(false, $"{project.Name} is locked: {lockReason}.");

        var economy = galaxy.Economies.First(e => e.CivilizationId == civilizationId);
        var currency = Game.Simulation.Economy.SovereignCurrencyCatalog.ForCivilization(galaxy, civilizationId);
        if (economy.Credits + 0.0001 < project.CreditCost)
            return new ConstructionOrderResult(false, $"{currency.Format(project.CreditCost)} is required to authorize {project.Name}.");

        economy.Credits -= project.CreditCost;
        state.ActiveProjectId = project.Id;
        state.ActiveProjectProgress = 0.0;
        return new ConstructionOrderResult(true, $"Construction started: {project.Name}. Authorized for {currency.Format(project.CreditCost)}.");
    }

    private static double ResolveBudget(
        IReadOnlyDictionary<int, double>? industryBudgets,
        int civilizationId,
        double availableIndustry)
    {
        if (!double.IsFinite(availableIndustry))
            throw new ArgumentOutOfRangeException(nameof(availableIndustry), "Available Industry must be finite.");
        availableIndustry = Math.Max(0, availableIndustry);
        if (industryBudgets is null)
            return availableIndustry;
        if (!industryBudgets.TryGetValue(civilizationId, out var budget))
            return 0.0;
        if (!double.IsFinite(budget))
            throw new ArgumentOutOfRangeException(nameof(industryBudgets), "Industry budgets must be finite.");

        return Math.Min(availableIndustry, Math.Max(0.0, budget));
    }

    public IReadOnlyList<ConstructionProjectDefinition> GetAvailableProjects(GalaxyState galaxy, int civilizationId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        var construction = galaxy.ConstructionStates.First(state => state.CivilizationId == civilizationId);
        return ConstructionRegistry.All
            .Where(project => !construction.CompletedProjectIds.Contains(project.Id) &&
                GetLockReason(galaxy, civilizationId, project) is null)
            .ToArray();
    }

    public string? GetLockReason(GalaxyState galaxy, int civilizationId, ConstructionProjectDefinition project)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        var construction = galaxy.ConstructionStates.First(state => state.CivilizationId == civilizationId);
        var missingCapabilities = project.RequiredTechnologies
            .Where(id => !_capabilityView.HasCivilizationCapability(galaxy, civilizationId, id))
            .Select(id => Research.TechnologyRegistry.Get(id).Name);
        var missingProjects = (project.RequiredProjects ?? Array.Empty<string>())
            .Where(id => !construction.CompletedProjectIds.Contains(id))
            .Select(id => ConstructionRegistry.Get(id).Name);
        var missing = missingCapabilities.Concat(missingProjects).ToArray();
        return missing.Length == 0 ? null : "requires " + string.Join(" and ", missing);
    }

    private ConstructionProjectDefinition? SelectAiProject(
        GalaxyState galaxy,
        CivilizationState civilization,
        ConstructionState state)
    {
        return GetAvailableProjects(galaxy, civilization.Id)
            .OrderByDescending(project => Score(project, civilization))
            .ThenBy(project => project.IndustryCost)
            .FirstOrDefault();
    }

    private static double Score(ConstructionProjectDefinition project, CivilizationState civilization)
    {
        var traits = civilization.Traits;
        return project.Category switch
        {
            ConstructionCategory.Science => 1.0 + traits.ScientificCuriosity * 0.90,
            ConstructionCategory.Industry => 1.0 + traits.Greed * 0.55 + traits.Territoriality * 0.20,
            ConstructionCategory.Orbital => 1.15 + traits.Territoriality * 0.30 + traits.ScientificCuriosity * 0.20,
            ConstructionCategory.Ftl => 1.30 + traits.ScientificCuriosity * 0.40 + traits.Aggression * 0.20,
            _ => 1.0,
        };
    }
}

public sealed record ConstructionEvent(int CivilizationId, string ProjectId, string Message);
public sealed record ConstructionOrderResult(bool Accepted, string Message);

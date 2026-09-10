using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Construction;
using Game.Simulation.Economy;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;
using Game.Simulation.Research;
using Game.Simulation.Research.Adaptive;
using Game.Simulation.Shipbuilding;
using Game.Simulation.Time;

namespace Game.Presentation;

public sealed record UiProjectCard(string Title, string Detail, double Progress,
    double Current, double Cost, bool IsActive)
{
    public static UiProjectCard Empty { get; } = new("Initializing", "", 0, 0, 0, false);
}

/// <summary>A directly selectable operation shown on a department page.</summary>
public sealed record UiOperationChoice(string Id, string Title, string Detail, string CostLabel,
    bool CanAfford = true, string? ArtworkPath = null);
public sealed record UiResearchHorizonNode(string Id, string Title, string Detail, string State,
    double Progress, bool CanStart, bool CanPause = false, bool CanResume = false);

public sealed record UiCreditFlowSnapshot(
    double ColonyRevenuePerDay, double TradeRevenuePerDay, double AdministrationPerDay,
    double PopulationServicesPerDay, double HabitatSupportPerDay, double FleetOperationsPerDay,
    double OrbitalMaintenancePerDay, double SurfaceMaintenancePerDay, double ResearchOperationsPerDay,
    double GrossIncomePerDay,
    double OperatingCostsPerDay, double NetCreditsPerDay);

public sealed record UiDashboardSnapshot(
    string CivilizationName, string Date, string SelectedSystemName, string SelectedSurveyLabel,
    double Credits, double Industry, double IndustryCapacity, double Science,
    double CreditsPerDay, double IndustryPerDay, double SciencePerDay,
    double FreeResearchLabs, double TotalResearchLabs,
    int ColonyCount, int FleetCount, int KnownSystemCount, int TotalSystemCount, int DemoStep,
    UiProjectCard Research, UiProjectCard Construction, UiProjectCard Shipyard);

public partial class Main
{
    public string UiPlayerSpeciesId => _galaxy?.Civilizations.First(civilization => civilization.Id == _galaxy.PlayerCivilizationId).SpeciesId
        ?? Game.Simulation.Species.SpeciesCatalog.TerranBaselineId;
    public string UiPlayerSpeciesName => Game.Simulation.Species.SpeciesCatalog.Get(UiPlayerSpeciesId).DisplayName;
    public SovereignCurrencyDefinition UiCurrency => SovereignCurrencyCatalog.ForSpecies(UiPlayerSpeciesId);
    public string UiFormatMoney(double budgetUnits) => UiCurrency.Format(budgetUnits);
    public string UiFormatMoneyRate(double budgetUnitsPerDay) => UiCurrency.FormatRate(budgetUnitsPerDay);
    public double UiOperatingArrears => _galaxy is null ? 0.0 : PlayerEconomy.OperatingArrears;
    public double UiBaseOperationsFundingFraction => _galaxy is null ? 1.0 : PlayerEconomy.LastBaseOperationsFundingFraction;

    public double UiActiveResearchAuthorizationCredits => _galaxy is null || _adaptiveResearch is null
        ? 0.0
        : _adaptiveResearch.GetProjectFunding(_galaxy.PlayerCivilizationId).Values
            .Sum(value => value.AuthorizationCredits);

    public double UiRemainingResearchMilestoneCredits => _galaxy is null || _adaptiveResearch is null
        ? 0.0
        : _adaptiveResearch.GetProjectFunding(_galaxy.PlayerCivilizationId).Values
            .Sum(value => Math.Max(0.0,
                value.ReservedMilestoneCredits - value.ConsumedMilestoneCredits));

    public IReadOnlyList<UiResearchHorizonNode> UiResearchHorizon
    {
        get
        {
            if (_galaxy is null) return Array.Empty<UiResearchHorizonNode>();
            var view = BuildPlayerAdaptiveResearchView();
            var candidateOrder = GetAdaptiveResearchCandidates()
                .Select((value, index) => (value.NodeId, index))
                .ToDictionary(value => value.NodeId, value => value.index, StringComparer.Ordinal);
            var projects = view.ActiveProjects.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var projectFunding = _adaptiveResearch!.GetProjectFunding(_galaxy.PlayerCivilizationId);
            // Put work the player can act on ahead of the longer record of established
            // knowledge. The full observer-safe horizon remains available by scrolling.
            return view.VisibleNodes
                .OrderBy(item => projects.ContainsKey(item.NodeId) ? 0 : candidateOrder.ContainsKey(item.NodeId) ? 1 :
                    item.State == ResearchMaturity.Mature ? 3 : 2)
                .ThenBy(item => candidateOrder.TryGetValue(item.NodeId, out var rank) ? rank : int.MaxValue)
                .ThenBy(item => item.DisplayName, StringComparer.Ordinal)
                .Select(item =>
                {
                    var active = projects.TryGetValue(item.NodeId, out var project);
                    var assignedLabs = active
                        ? project!.AssignedEffectiveLabs
                        : Math.Min(item.RecommendedLabs ?? item.MinimumLabs ?? 0,
                            view.DirectedProgramCapacity.FreeEffectiveLabs);
                    var quote = assignedLabs > 0 ? ResearchFundingQuote(item.NodeId, assignedLabs) : null;
                    var milestoneRemaining = active && projectFunding.TryGetValue(
                        item.NodeId, out var fundingState)
                            ? Math.Max(0.0, fundingState.ReservedMilestoneCredits -
                                fundingState.ConsumedMilestoneCredits)
                            : quote?.MilestoneCommitmentCredits ?? 0.0;
                    var runway = quote is null
                        ? null
                        : ResearchFundingRunwayLabel(ResearchFundingRunwayDays(
                            active ? 0.0 : quote.AuthorizationCredits + quote.MilestoneCommitmentCredits,
                            active ? 0.0 : quote.OperatingCreditsPerDay));
                    var physicalRequirement = ResearchPhysicalRequirementLabel(
                        item.NodeId,
                        active ? project!.Stage : ResearchMaturity.Experimental);
                    var canFundFirstDay = quote is not null &&
                        PlayerEconomy.Credits + 0.000001 >=
                        AdaptiveResearchCampaignCommands.CreditsNeededToStart(quote);
                    var canPause = active && !project!.Paused;
                    var canResume = active && project!.Paused &&
                        !string.Equals(project.PauseReason, "hypothesis_resolution_required", StringComparison.Ordinal) &&
                        item.Blockers.Count == 0 && quote is not null &&
                        PlayerEconomy.Credits + 0.000001 >= quote.OperatingCreditsPerDay;
                    var details = active
                        ? $"{DisplayResearchDomain(item.DomainId)} · {project!.AssignedEffectiveLabs:0.#} labs · " +
                          $"{UiFormatMoneyRate(-quote!.OperatingCreditsPerDay)} · {PlayerEconomy.LastResearchFundingFraction:P0} funded · " +
                          $"{UiFormatMoney(milestoneRemaining)} milestone reserve · {runway} · {physicalRequirement} · " +
                          $"{(project.Paused ? $"paused: {project.PauseReason}" : $"{project.ReadinessBand} readiness")}"
                        : item.State == ResearchMaturity.Mature
                            ? $"{DisplayResearchDomain(item.DomainId)} · established knowledge"
                        : item.Blockers.FirstOrDefault()?.Message ??
                          $"{DisplayResearchDomain(item.DomainId)} · {item.SolutionFamily.Replace('_', ' ')} · " +
                          (quote is null
                              ? "research requirements are not yet established"
                              : $"{UiFormatMoney(quote.AuthorizationCredits)} authorize · " +
                                $"{UiFormatMoney(quote.MilestoneCommitmentCredits)} milestones · {UiFormatMoneyRate(-quote.OperatingCreditsPerDay)} · " +
                                $"est. {UiFormatMoney(quote.EstimatedTotalCredits)} total · {runway} · {physicalRequirement}");
                    return new UiResearchHorizonNode(item.NodeId, item.DisplayName, details,
                        active ? "ACTIVE PROGRAM" : item.State.ToString().ToUpperInvariant(),
                        active ? project!.StageProgress : item.State == ResearchMaturity.Mature ? 1 : 0,
                        candidateOrder.ContainsKey(item.NodeId) && canFundFirstDay,
                        canPause,
                        canResume);
                }).ToArray();
        }
    }

    public IReadOnlyList<UiOperationChoice> UiResearchChoices
    {
        get
        {
            if (_galaxy is null) return Array.Empty<UiOperationChoice>();
            var state = _adaptiveResearch!.GetCivilization(_galaxy.PlayerCivilizationId);
            return GetAdaptiveResearchCandidates()
                .Select(item =>
                {
                    var labs = Math.Min(item.RecommendedLabs ?? item.MinimumLabs ?? 0, state.FreeEffectiveLabs);
                    var quote = ResearchFundingQuote(item.NodeId, labs);
                    var runway = ResearchFundingRunwayLabel(ResearchFundingRunwayDays(
                        quote.AuthorizationCredits + quote.MilestoneCommitmentCredits,
                        quote.OperatingCreditsPerDay));
                    return new UiOperationChoice(item.NodeId, item.DisplayName,
                        $"{DisplayResearchDomain(item.DomainId)} · {item.SolutionFamily.Replace('_', ' ')} · " +
                        ResearchPhysicalRequirementLabel(item.NodeId, ResearchMaturity.Experimental),
                        $"{labs:N0} labs · {UiFormatMoney(quote.AuthorizationCredits)} authorize · " +
                        $"{UiFormatMoney(quote.MilestoneCommitmentCredits)} milestones · " +
                        $"{UiFormatMoneyRate(-quote.OperatingCreditsPerDay)} · est. {UiFormatMoney(quote.EstimatedTotalCredits)} total · {runway}",
                        PlayerEconomy.Credits + 0.000001 >=
                        AdaptiveResearchCampaignCommands.CreditsNeededToStart(quote));
                })
                .ToArray();
        }
    }

    public IReadOnlyList<UiOperationChoice> UiConstructionChoices => _galaxy is null || PlayerConstruction.ActiveProjectId is not null
        ? Array.Empty<UiOperationChoice>()
        : _construction.GetAvailableProjects(_galaxy, _galaxy.PlayerCivilizationId)
            .Select(item => new UiOperationChoice(item.Id, item.Name, ConstructionDetail(item),
                $"{item.IndustryCost:N0} industry · {UiFormatMoney(item.CreditCost)}",
                PlayerEconomy.Credits + 0.0001 >= item.CreditCost))
            .ToArray();

    public IReadOnlyList<UiOperationChoice> UiShipChoices => _galaxy is null
        ? Array.Empty<UiOperationChoice>()
        : _shipbuilding.GetAvailableDesigns(_galaxy, _galaxy.PlayerCivilizationId)
            .Select(item =>
            {
                var propulsion = _shipbuilding.GetEffectivePropulsion(
                    _galaxy, _galaxy.PlayerCivilizationId, item);
                return new UiOperationChoice(item.Id, item.Name,
                    $"{item.Description}\n{propulsion.PropulsionGeneration}: {propulsion.StrategicSpeed:0.#} ly/day, {propulsion.MaximumLegRangeLightYears:0.#} ly per leg, {propulsion.FuelEnduranceLightYears:0.#} ly endurance.",
                    $"{item.IndustryCost:N0} industry · {UiFormatMoney(item.CreditCost)}",
                    PlayerEconomy.Credits + 0.0001 >= item.CreditCost,
                    ShipArtworkLibrary.PathForDesign(item.Id));
            })
            .ToArray();

    public UiCreditFlowSnapshot UiCreditFlow
    {
        get
        {
            if (_galaxy is null) return new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var flow = EconomySimulation.GetCreditFlow(_galaxy, _galaxy.PlayerCivilizationId);
            return new(flow.ColonyRevenuePerDay, flow.TradeRevenuePerDay,
                flow.ColonyAdministrationPerDay, flow.PopulationServicesPerDay,
                flow.HabitatSupportPerDay, flow.FleetOperationsPerDay, flow.OrbitalMaintenancePerDay,
                flow.SurfaceMaintenancePerDay, flow.ResearchOperationsPerDay, flow.GrossIncomePerDay,
                flow.OperatingCostsPerDay, flow.NetCreditsPerDay);
        }
    }

    /// <summary>Read-only display values; command handlers retain all eligibility checks.</summary>
    public UiDashboardSnapshot UiDashboard
    {
        get
        {
            // Child controls enter the scene before the campaign is initialized by Main.
            if (_galaxy is null)
                return new("Stellar Continuum", "", "Select a star", "", 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, UiProjectCard.Empty, UiProjectCard.Empty, UiProjectCard.Empty);

            var player = PlayerCivilization;
            var economy = PlayerEconomy;
            var researchCapacity = BuildPlayerAdaptiveResearchView().DirectedProgramCapacity;
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
            var hasExtrasolarColony = _galaxy.Colonies.Any(colony => colony.CivilizationId == player.Id &&
                colony.SystemId != player.HomeSystemId);
            var fleets = _galaxy.Fleets.Where(fleet => fleet.IsActive && fleet.CivilizationId == player.Id).ToArray();
            var hasExperimentalTransit = _adaptiveResearch!.GetCivilization(player.Id)
                .HasCapability(ShipbuildingCapabilityIds.ExperimentalInterstellarTransit);
            var demoStep = hasExtrasolarColony ? 3 : !hasExperimentalTransit ? 0 :
                new[] { FleetRole.Scout, FleetRole.Science, FleetRole.Colony }.All(role => fleets.Any(fleet => fleet.Role == role)) ? 2 : 1;

            var adaptiveView = BuildPlayerAdaptiveResearchView();
            var adaptiveProject = adaptiveView.ActiveProjects.FirstOrDefault();
            var adaptiveNode = adaptiveProject is not null
                ? adaptiveView.VisibleNodes.First(value => value.NodeId == adaptiveProject.NodeId)
                : GetResearchCandidate();
            var project = construction.ActiveProjectId is { } projectId
                ? ConstructionRegistry.Get(projectId) : GetConstructionCandidate();
            var ship = shipyard.ActiveDesignId is { } shipId
                ? ShipDesignRegistry.Get(shipId) : null;
            var availableShips = _shipbuilding.GetAvailableDesigns(_galaxy, player.Id);
            var firstShip = ShipDesignRegistry.All.First();
            var shipLockReason = _shipbuilding.GetLockReason(_galaxy, player.Id, firstShip);
            return new(player.Name, CampaignCalendar.FormatDate(_clock.SimulationDays), selectedName, surveyLabel,
                economy.Credits, economy.Industry,
                EconomySimulation.GetIndustryStorageCapacity(_galaxy, player.Id), economy.Science,
                economy.LastCreditsPerSecond, economy.LastIndustryPerSecond, economy.LastSciencePerSecond,
                researchCapacity.FreeEffectiveLabs, _adaptiveResearch!.GetCivilization(player.Id).TotalEffectiveResearchLabs,
                colonies, fleets.Length, _galaxy.Knowledge.GetKnownSystems(player.Id).Count, _galaxy.Systems.Count, demoStep,
                adaptiveNode is null ? new("No research available", "New possibilities emerge from established knowledge, evidence and real pressures.", 0, 0, 0, false)
                    : adaptiveProject is null
                        ? new(adaptiveNode.DisplayName,
                            $"{DisplayResearchDomain(adaptiveNode.DomainId)} · {adaptiveNode.MinimumLabs}-{adaptiveNode.RecommendedLabs} effective labs",
                            0, 0, adaptiveNode.RecommendedLabs ?? adaptiveNode.MinimumLabs ?? 0, false)
                        : new(adaptiveNode.DisplayName,
                            $"{adaptiveProject.Stage} · {adaptiveProject.AssignedEffectiveLabs:0.#} labs · " +
                            $"{UiFormatMoneyRate(-ResearchFundingQuote(adaptiveProject.NodeId, adaptiveProject.AssignedEffectiveLabs).OperatingCreditsPerDay)} · " +
                            $"{economy.LastResearchFundingFraction:P0} funded · {adaptiveProject.ReadinessBand} readiness",
                            adaptiveProject.StageProgress, adaptiveProject.StageProgress,
                            1, true),
                project is null ? new("Infrastructure ready", "Research new technologies to unlock more projects.", 0, 0, 0, false)
                    : Card(project.Name, ConstructionDetail(project), construction.ActiveProjectProgress, project.IndustryCost, construction.ActiveProjectId is not null),
                ship is not null
                    ? Card(ship.Name, $"Construction in progress.\n\n{ship.Description}\n{shipyard.PendingBuildCount} build(s) in queue", shipyard.ActiveBuildProgress, ship.IndustryCost, true)
                    : availableShips.Count == 0
                        ? new("Shipyard locked", $"{firstShip.Name} {shipLockReason ?? "has no available construction path"}.", 0, 0, 0, false)
                        : new("Choose a ship design", $"{availableShips.Count} designs are available. Choose one below to begin construction or add it to the queue.\n{shipyard.PendingBuildCount} build(s) in queue", 0, 0, 0, false));
        }
    }

    private static UiProjectCard Card(string title, string detail, double current, double cost, bool active) =>
        new(title, detail, active && cost > 0 ? Math.Clamp(current / cost, 0, 1) : 0, active ? current : 0, cost, active);

    private static string DisplayResearchDomain(string domainId) =>
        string.Join(' ', domainId.Split('_').Select(word =>
            word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..]));

    private AdaptiveResearchFundingQuote ResearchFundingQuote(string nodeId, double assignedLabs) =>
        AdaptiveResearchFundingPolicy.Quote(
            _adaptiveResearch!.Runtime.Authority.Catalog.GetNode(nodeId),
            assignedLabs,
            _adaptiveResearch.Runtime.Authority.Catalog);

    private double ResearchFundingRunwayDays(double authorizationCredits, double additionalOperatingCreditsPerDay)
    {
        if (_galaxy is null || _adaptiveResearch is null) return 0.0;
        var civilizationId = _galaxy.PlayerCivilizationId;
        var existingOperatingCreditsPerDay = _adaptiveResearch.GetCivilization(civilizationId)
            .ActiveProjects.Values
            .Where(project => !project.Paused)
            .Sum(project => ResearchFundingQuote(project.NodeId, project.AssignedEffectiveLabs)
                .OperatingCreditsPerDay);
        var nonResearchNet = EconomySimulation.GetCreditFlow(
            _galaxy, civilizationId, includeResearchOperations: false).NetCreditsPerDay;
        return AdaptiveResearchFundingPolicy.EstimateTreasuryRunwayDays(
            Math.Max(0.0, PlayerEconomy.Credits - authorizationCredits),
            nonResearchNet,
            existingOperatingCreditsPerDay + additionalOperatingCreditsPerDay);
    }

    private static string ResearchFundingRunwayLabel(double days) =>
        double.IsPositiveInfinity(days)
            ? "sustainable at current income"
            : days < 1.0
                ? "under 1 day treasury runway"
                : $"{days:N0} days treasury runway";

    private string ResearchPhysicalRequirementLabel(string nodeId, ResearchMaturity stage)
    {
        var requirement = _adaptiveResearch!.Runtime.Authority.Kernel.Facilities
            .GetStageRequirement(nodeId, stage);
        if (requirement is null || requirement.AllOf.Count + requirement.AnyOf.Count == 0)
            return "standard laboratory infrastructure";

        var parts = new List<string>();
        if (requirement.AllOf.Count > 0)
            parts.Add(string.Join(" + ", requirement.AllOf.Select(DisplayFacilityCapability)));
        if (requirement.AnyOf.Count > 0)
            parts.Add("one of " + string.Join(" / ", requirement.AnyOf.Select(DisplayFacilityCapability)));
        return $"{stage} facility: {string.Join(" + ", parts)}";
    }

    private static string DisplayFacilityCapability(string capabilityId) =>
        string.Join(' ', capabilityId.Split('_').Select(word =>
            word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..]));

    private string ConstructionDetail(ConstructionProjectDefinition project)
    {
        if (project.IndustryPerDay <= 0 && project.UpkeepCreditsPerDay <= 0) return project.Description;
        var effect = project.IndustryPerDay > 0 ? $"Produces {project.IndustryPerDay:0.00} Industry/day" : string.Empty;
        var upkeep = project.UpkeepCreditsPerDay > 0 ? $"costs {UiFormatMoneyRate(-project.UpkeepCreditsPerDay)} to operate" : string.Empty;
        return project.Description + "\n" + string.Join(" · ", new[] { effect, upkeep }.Where(text => text.Length > 0)) + ".";
    }
}

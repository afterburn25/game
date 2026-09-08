using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Simulation.Research.Adaptive;

public enum ResearchBlockerKind
{
    NotVisible,
    NotInvestigable,
    AlreadyActive,
    AlreadyResolved,
    MissingAllOfPrerequisite,
    MissingAnyOfPrerequisite,
    MissingTargetContext,
    MissingApplicabilityTrait,
    MissingApplicabilityEvidence,
    MissingRequiredEvidence,
    PressureBelowThreshold,
    PressureAnyBelowThreshold,
    MissingCapability,
    MissingAnyCapability,
    InsufficientFreeLabs,
    BelowMinimumAssignedLabs,
    DirectedProgramCapacity,
    MissingFacilityCapability,
    MissingAnyFacilityCapability,
}

public sealed record ResearchBlocker(
    ResearchBlockerKind Kind,
    string RequirementId,
    double? RequiredValue = null,
    double? CurrentValue = null,
    string? ContextId = null);

public sealed record ResearchStartability(
    string NodeId,
    bool IsVisible,
    bool CanStart,
    int MinimumLabs,
    int RecommendedLabs,
    double RequestedLabs,
    IReadOnlyList<ResearchBlocker> Blockers);

public sealed record AdaptiveResearchVisibleNodeView(
    string NodeId,
    string Name,
    string DomainId,
    string Complexity,
    ResearchMaturity Maturity,
    string? Resolution,
    bool IsActive,
    string? TargetApplicabilityContextId,
    double TotalResearchPoints,
    double BaseResearchPoints,
    int MinimumLabs,
    int RecommendedLabs,
    bool CanStart,
    IReadOnlyList<ResearchBlocker> StartBlockers);

/// <summary>
/// Visible-only authoritative query surface. It iterates sparse civilization state, never the
/// hidden future graph, when materializing consumer-facing research views.
/// </summary>
public sealed class AdaptiveResearchQueryService
{
    private readonly AdaptiveResearchCatalog _catalog;
    private readonly AdaptiveResearchApplicabilityCatalog _applicability;
    private readonly AdaptiveResearchFacilityCatalog _facilities;

    public AdaptiveResearchQueryService(
        AdaptiveResearchCatalog catalog,
        AdaptiveResearchApplicabilityCatalog applicability,
        AdaptiveResearchFacilityCatalog facilities)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _applicability = applicability ?? throw new ArgumentNullException(nameof(applicability));
        _facilities = facilities ?? throw new ArgumentNullException(nameof(facilities));
    }

    public IReadOnlyList<AdaptiveResearchVisibleNodeView> GetVisibleNodes(AdaptiveResearchCivilizationState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var result = new List<AdaptiveResearchVisibleNodeView>(state.NodeStates.Count);
        foreach (var pair in state.NodeStates.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!_catalog.Nodes.TryGetValue(pair.Key, out var definition))
                throw new InvalidOperationException($"Civilization '{state.CivilizationId}' contains unknown research node '{pair.Key}'.");

            state.ActiveProjects.TryGetValue(pair.Key, out var project);
            var targetContext = project?.TargetApplicabilityContextId;
            var startability = pair.Value.Maturity == ResearchMaturity.Investigable && project is null
                ? EvaluateStartability(state, pair.Key, targetContext, definition.ProjectRequirements.MinimumLabs)
                : new ResearchStartability(
                    pair.Key,
                    true,
                    false,
                    definition.ProjectRequirements.MinimumLabs,
                    definition.ProjectRequirements.RecommendedLabs,
                    project?.AssignedEffectiveLabs ?? definition.ProjectRequirements.MinimumLabs,
                    ProjectStateBlockers(pair.Value, project));

            result.Add(new AdaptiveResearchVisibleNodeView(
                pair.Key,
                definition.Name,
                definition.DomainId,
                definition.Complexity,
                pair.Value.Maturity,
                pair.Value.Resolution,
                project is not null,
                targetContext,
                pair.Value.TotalResearchPoints,
                definition.ProjectRequirements.BaseResearchPoints,
                definition.ProjectRequirements.MinimumLabs,
                definition.ProjectRequirements.RecommendedLabs,
                startability.CanStart,
                startability.Blockers));
        }

        return result;
    }

    public ResearchStartability EvaluateStartability(
        AdaptiveResearchCivilizationState state,
        string nodeId,
        string? targetApplicabilityContextId = null,
        double? requestedLabs = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        if (!state.TryGetNodeState(nodeId, out var nodeState))
        {
            return new ResearchStartability(
                nodeId,
                false,
                false,
                0,
                0,
                requestedLabs ?? 0.0,
                new[] { new ResearchBlocker(ResearchBlockerKind.NotVisible, "visible_research_horizon") });
        }

        if (!_catalog.Nodes.TryGetValue(nodeId, out var definition))
            throw new InvalidOperationException($"Civilization '{state.CivilizationId}' contains unknown research node '{nodeId}'.");

        var assigned = requestedLabs ?? definition.ProjectRequirements.MinimumLabs;
        var blockers = new List<ResearchBlocker>();

        if (state.ActiveProjects.ContainsKey(nodeId))
            blockers.Add(new ResearchBlocker(ResearchBlockerKind.AlreadyActive, nodeId));
        if (nodeState.Maturity != ResearchMaturity.Investigable)
        {
            blockers.Add(nodeState.Maturity is ResearchMaturity.Mature or ResearchMaturity.Archived
                ? new ResearchBlocker(ResearchBlockerKind.AlreadyResolved, nodeId)
                : new ResearchBlocker(ResearchBlockerKind.NotInvestigable, nodeState.Maturity.ToString()));
        }

        EvaluateKnowledgePrerequisites(state, definition, blockers);
        EvaluateApplicability(state, definition, targetApplicabilityContextId, blockers);
        EvaluateEvidence(state, definition, targetApplicabilityContextId, blockers);
        EvaluatePressure(state, definition, blockers);
        EvaluateCapabilities(state, definition, targetApplicabilityContextId, blockers);
        EvaluateProgramCapacity(state, blockers);
        EvaluateLabs(state, definition, assigned, blockers);
        EvaluateFacilityRequirement(state, definition.Id, ResearchMaturity.Experimental, blockers);

        return new ResearchStartability(
            nodeId,
            true,
            blockers.Count == 0,
            definition.ProjectRequirements.MinimumLabs,
            definition.ProjectRequirements.RecommendedLabs,
            assigned,
            blockers);
    }

    public IReadOnlyList<ResearchBlocker> EvaluateStageBlockers(
        AdaptiveResearchCivilizationState state,
        ResearchProjectRuntimeState project)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(project);
        var blockers = new List<ResearchBlocker>();
        EvaluateFacilityRequirement(state, project.NodeId, project.Stage, blockers);
        if (project.AssignedEffectiveLabs < _catalog.GetNode(project.NodeId).ProjectRequirements.MinimumLabs)
        {
            blockers.Add(new ResearchBlocker(
                ResearchBlockerKind.BelowMinimumAssignedLabs,
                "minimum_labs",
                _catalog.GetNode(project.NodeId).ProjectRequirements.MinimumLabs,
                project.AssignedEffectiveLabs));
        }
        return blockers;
    }

    private static IReadOnlyList<ResearchBlocker> ProjectStateBlockers(
        ResearchNodeRuntimeState nodeState,
        ResearchProjectRuntimeState? project)
    {
        if (project is not null)
            return new[] { new ResearchBlocker(ResearchBlockerKind.AlreadyActive, project.NodeId) };
        return nodeState.Maturity is ResearchMaturity.Mature or ResearchMaturity.Archived
            ? new[] { new ResearchBlocker(ResearchBlockerKind.AlreadyResolved, nodeState.NodeId) }
            : new[] { new ResearchBlocker(ResearchBlockerKind.NotInvestigable, nodeState.Maturity.ToString()) };
    }

    private static void EvaluateKnowledgePrerequisites(
        AdaptiveResearchCivilizationState state,
        AdaptiveResearchNodeDefinition definition,
        ICollection<ResearchBlocker> blockers)
    {
        foreach (var prerequisite in definition.Prerequisites.AllOf)
            if (!state.HasEstablishedKnowledge(prerequisite))
                blockers.Add(new ResearchBlocker(ResearchBlockerKind.MissingAllOfPrerequisite, prerequisite));

        if (definition.Prerequisites.AnyOf.Count > 0 &&
            !definition.Prerequisites.AnyOf.Any(state.HasEstablishedKnowledge))
        {
            blockers.Add(new ResearchBlocker(
                ResearchBlockerKind.MissingAnyOfPrerequisite,
                string.Join("|", definition.Prerequisites.AnyOf)));
        }
    }

    private void EvaluateApplicability(
        AdaptiveResearchCivilizationState state,
        AdaptiveResearchNodeDefinition definition,
        string? targetContextId,
        ICollection<ResearchBlocker> blockers)
    {
        foreach (var traitId in definition.Applicability.Traits)
        {
            var trait = _applicability.GetTrait(traitId);
            if (trait.Scope == ResearchApplicabilityTraitScope.Civilization)
            {
                if (!state.HasCivilizationTrait(traitId))
                    blockers.Add(new ResearchBlocker(ResearchBlockerKind.MissingApplicabilityTrait, traitId));
                continue;
            }

            if (string.IsNullOrWhiteSpace(targetContextId))
            {
                blockers.Add(new ResearchBlocker(ResearchBlockerKind.MissingTargetContext, traitId));
                continue;
            }

            if (!state.HasApplicabilityTrait(targetContextId, traitId))
                blockers.Add(new ResearchBlocker(ResearchBlockerKind.MissingApplicabilityTrait, traitId, ContextId: targetContextId));
        }
    }

    private static void EvaluateEvidence(
        AdaptiveResearchCivilizationState state,
        AdaptiveResearchNodeDefinition definition,
        string? targetContextId,
        ICollection<ResearchBlocker> blockers)
    {
        foreach (var evidenceId in definition.Applicability.EvidenceTypes)
            if (!state.HasEvidenceType(evidenceId, targetContextId))
                blockers.Add(new ResearchBlocker(ResearchBlockerKind.MissingApplicabilityEvidence, evidenceId, ContextId: targetContextId));

        foreach (var evidenceId in definition.ProjectRequirements.RequiredEvidence)
            if (!state.HasEvidenceType(evidenceId, targetContextId))
                blockers.Add(new ResearchBlocker(ResearchBlockerKind.MissingRequiredEvidence, evidenceId, ContextId: targetContextId));
    }

    private static void EvaluatePressure(
        AdaptiveResearchCivilizationState state,
        AdaptiveResearchNodeDefinition definition,
        ICollection<ResearchBlocker> blockers)
    {
        foreach (var requirement in definition.ProjectRequirements.RequiredPressure)
        {
            var current = state.GetPressure(requirement.Key);
            if (current < requirement.Value)
                blockers.Add(new ResearchBlocker(ResearchBlockerKind.PressureBelowThreshold, requirement.Key, requirement.Value, current));
        }

        if (definition.ProjectRequirements.RequiredPressureAny.Count > 0 &&
            !definition.ProjectRequirements.RequiredPressureAny.Any(requirement => state.GetPressure(requirement.Key) >= requirement.Value))
        {
            var summary = string.Join("|", definition.ProjectRequirements.RequiredPressureAny.Keys.OrderBy(value => value, StringComparer.Ordinal));
            blockers.Add(new ResearchBlocker(ResearchBlockerKind.PressureAnyBelowThreshold, summary));
        }
    }

    private void EvaluateCapabilities(
        AdaptiveResearchCivilizationState state,
        AdaptiveResearchNodeDefinition definition,
        string? targetContextId,
        ICollection<ResearchBlocker> blockers)
    {
        foreach (var capabilityId in definition.CapabilityRequirements.AllOf)
            if (!HasRequiredCapability(state, capabilityId, targetContextId, blockers, false))
                blockers.Add(new ResearchBlocker(ResearchBlockerKind.MissingCapability, capabilityId, ContextId: targetContextId));

        if (definition.CapabilityRequirements.AnyOf.Count == 0)
            return;

        var hasAny = definition.CapabilityRequirements.AnyOf.Any(capabilityId =>
            HasRequiredCapability(state, capabilityId, targetContextId, blockers: null, recordMissingContext: false));
        if (!hasAny)
        {
            if (definition.CapabilityRequirements.AnyOf.Any(RequiresTargetContext) && string.IsNullOrWhiteSpace(targetContextId))
                blockers.Add(new ResearchBlocker(ResearchBlockerKind.MissingTargetContext, "capability_context"));
            blockers.Add(new ResearchBlocker(
                ResearchBlockerKind.MissingAnyCapability,
                string.Join("|", definition.CapabilityRequirements.AnyOf)));
        }
    }

    private bool HasRequiredCapability(
        AdaptiveResearchCivilizationState state,
        string capabilityId,
        string? targetContextId,
        ICollection<ResearchBlocker>? blockers,
        bool recordMissingContext)
    {
        var capability = _catalog.Capabilities[capabilityId];
        if (capability.Scope == ResearchCapabilityScope.Civilization)
            return state.HasCapability(capabilityId, null);
        if (string.IsNullOrWhiteSpace(targetContextId))
        {
            if (recordMissingContext)
                blockers?.Add(new ResearchBlocker(ResearchBlockerKind.MissingTargetContext, capabilityId));
            return false;
        }
        return state.HasCapability(capabilityId, targetContextId);
    }

    private bool RequiresTargetContext(string capabilityId) =>
        _catalog.Capabilities[capabilityId].Scope != ResearchCapabilityScope.Civilization;

    private void EvaluateProgramCapacity(AdaptiveResearchCivilizationState state, ICollection<ResearchBlocker> blockers)
    {
        var stage = _catalog.GetDirectedProgramStage(state.DirectedProgramStageId);
        if (stage.DirectedProgramLimit is int limit)
        {
            var active = state.ActiveProjects.Values.Count(project => !project.Paused);
            if (active >= limit)
                blockers.Add(new ResearchBlocker(ResearchBlockerKind.DirectedProgramCapacity, stage.Id, limit, active));
        }
    }

    private static void EvaluateLabs(
        AdaptiveResearchCivilizationState state,
        AdaptiveResearchNodeDefinition definition,
        double assigned,
        ICollection<ResearchBlocker> blockers)
    {
        if (assigned < definition.ProjectRequirements.MinimumLabs)
        {
            blockers.Add(new ResearchBlocker(
                ResearchBlockerKind.BelowMinimumAssignedLabs,
                "minimum_labs",
                definition.ProjectRequirements.MinimumLabs,
                assigned));
        }
        if (assigned > state.FreeEffectiveLabs + 0.0000001)
        {
            blockers.Add(new ResearchBlocker(
                ResearchBlockerKind.InsufficientFreeLabs,
                "free_labs",
                assigned,
                state.FreeEffectiveLabs));
        }
    }

    private void EvaluateFacilityRequirement(
        AdaptiveResearchCivilizationState state,
        string nodeId,
        ResearchMaturity stage,
        ICollection<ResearchBlocker> blockers)
    {
        var requirement = _facilities.GetStageRequirement(nodeId, stage);
        if (requirement is null)
            return;

        foreach (var capabilityId in requirement.AllOf)
            if (!state.HasFacilityCapability(capabilityId))
                blockers.Add(new ResearchBlocker(ResearchBlockerKind.MissingFacilityCapability, capabilityId));

        if (requirement.AnyOf.Count > 0 && !requirement.AnyOf.Any(state.HasFacilityCapability))
        {
            blockers.Add(new ResearchBlocker(
                ResearchBlockerKind.MissingAnyFacilityCapability,
                string.Join("|", requirement.AnyOf)));
        }
    }
}

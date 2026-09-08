using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Simulation.Research.Adaptive;

public sealed record AdaptiveResearchTreeNodeProjection(
    string NodeId,
    string DisplayName,
    string DomainId,
    string GroupId,
    string AnchorId,
    ResearchMaturity Maturity,
    AdaptiveResearchProvenanceRecord PrimaryProvenance,
    IReadOnlyList<AdaptiveResearchProvenanceRecord> RecentProvenance);

public sealed record AdaptiveResearchTreeHistorySummary(
    string GroupId,
    IReadOnlyList<string> CollapsedVisibleNodeIds,
    IReadOnlyList<string> KnownCapabilityIds);

public sealed record AdaptiveResearchTreeProjection(
    long Sequence,
    string CivilizationId,
    bool RequiresFullRefresh,
    IReadOnlyList<AdaptiveResearchTreeNodeProjection> DetailedVisibleNodes,
    IReadOnlyList<AdaptiveResearchEdgeView> VisibleEdges,
    IReadOnlyList<AdaptiveResearchTreeHistorySummary> CollapsedHistory,
    IReadOnlyList<AdaptiveResearchTreeDelta> Deltas);

/// <summary>
/// Research-owned evolving-tree read model. Anchors and provenance are derived only from materialized
/// state and public metadata of those materialized nodes. Presentation owns coordinates, animation and pixels.
/// </summary>
public sealed class AdaptiveResearchTopologyRuntime
{
    public const int DefaultRecentDetailSequenceWindow = 64;

    private readonly AdaptiveResearchCatalog _catalog;

    public AdaptiveResearchTopologyRuntime(AdaptiveResearchCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public void RecordMaterialization(
        AdaptiveResearchCivilizationState state,
        string nodeId,
        AdaptiveResearchProvenanceKind kind,
        string? sourceVisibleNodeId = null,
        string? subjectId = null,
        double? year = null,
        string? explanation = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        var node = _catalog.GetNode(nodeId);
        if (!state.NodeStates.ContainsKey(nodeId))
            throw new InvalidOperationException($"Topology provenance cannot be recorded before '{nodeId}' is materialized.");

        if (sourceVisibleNodeId is not null &&
            (!state.NodeStates.ContainsKey(sourceVisibleNodeId) || string.Equals(sourceVisibleNodeId, nodeId, StringComparison.Ordinal)))
            sourceVisibleNodeId = null;

        var groupId = GroupId(node);
        var anchorId = ResolveAnchor(state, node, groupId, sourceVisibleNodeId);
        state.Topology.RecordMaterialization(
            nodeId,
            groupId,
            anchorId,
            kind,
            sourceVisibleNodeId,
            subjectId,
            year,
            explanation ?? DefaultExplanation(kind));
    }

    public void EnsureVisibleNodesHaveProvenance(
        AdaptiveResearchCivilizationState state,
        AdaptiveResearchProvenanceKind fallbackKind = AdaptiveResearchProvenanceKind.ExplicitMigration,
        string? subjectId = null,
        double? year = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        foreach (var nodeId in state.NodeStates.Keys
                     .OrderBy(id => _catalog.GetNode(id).GraphDepth)
                     .ThenBy(id => id, StringComparer.Ordinal))
        {
            if (state.Topology.TryGetNode(nodeId, out _))
                continue;
            RecordMaterialization(
                state,
                nodeId,
                fallbackKind,
                subjectId: subjectId,
                year: year,
                explanation: fallbackKind == AdaptiveResearchProvenanceKind.ExplicitMigration
                    ? "Visible research state predates topology provenance; migration preserved known state without inventing a discovery cause."
                    : null);
        }
    }

    public AdaptiveResearchTreeProjection BuildProjection(
        AdaptiveResearchCivilizationState state,
        long afterDeltaSequence = 0,
        int recentDetailSequenceWindow = DefaultRecentDetailSequenceWindow)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (afterDeltaSequence < 0)
            throw new ArgumentOutOfRangeException(nameof(afterDeltaSequence));
        if (recentDetailSequenceWindow < 0)
            throw new ArgumentOutOfRangeException(nameof(recentDetailSequenceWindow));

        EnsureVisibleNodesHaveProvenance(state);
        var visible = state.NodeStates.Keys.ToHashSet(StringComparer.Ordinal);

        foreach (var topologyNode in state.Topology.Nodes.Values)
        {
            if (!visible.Contains(topologyNode.NodeId))
                continue;
            if (topologyNode.AnchorId.StartsWith("node:", StringComparison.Ordinal))
            {
                var anchorNodeId = topologyNode.AnchorId[5..];
                if (!visible.Contains(anchorNodeId))
                    state.Topology.SetAnchor(topologyNode.NodeId, $"group:{topologyNode.GroupId}");
            }
        }

        var sequenceBeforeCollapse = state.Topology.Sequence;
        var topologyNodes = state.Topology.Nodes;
        foreach (var nodeId in visible)
        {
            var nodeState = state.NodeStates[nodeId];
            var topologyNode = topologyNodes[nodeId];
            var oldEnough = sequenceBeforeCollapse - topologyNode.LastChangedSequence > recentDetailSequenceWindow;
            var collapse = nodeState.CountsAsEstablishedKnowledge &&
                           !state.ActiveProjects.ContainsKey(nodeId) &&
                           oldEnough;
            state.Topology.SetCollapsedHistory(nodeId, collapse);
        }

        var collapsed = state.Topology.CollapsedHistoryNodeIds
            .Where(visible.Contains)
            .ToHashSet(StringComparer.Ordinal);

        var detailed = visible
            .Where(nodeId => !collapsed.Contains(nodeId))
            .Select(nodeId =>
            {
                var definition = _catalog.GetNode(nodeId);
                var runtimeState = state.NodeStates[nodeId];
                var topology = topologyNodes[nodeId];
                return new AdaptiveResearchTreeNodeProjection(
                    nodeId,
                    definition.Name,
                    definition.DomainId,
                    topology.GroupId,
                    topology.AnchorId,
                    runtimeState.Maturity,
                    topology.PrimaryProvenance,
                    topology.RecentProvenance);
            })
            .OrderBy(value => _catalog.GetNode(value.NodeId).GraphDepth)
            .ThenBy(value => value.GroupId, StringComparer.Ordinal)
            .ThenBy(value => value.NodeId, StringComparer.Ordinal)
            .ToArray();

        var detailedIds = detailed.Select(value => value.NodeId).ToHashSet(StringComparer.Ordinal);
        var edges = BuildVisibleEdges(detailedIds);
        var history = collapsed
            .GroupBy(nodeId => topologyNodes[nodeId].GroupId, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new AdaptiveResearchTreeHistorySummary(
                group.Key,
                group.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                group.SelectMany(nodeId => _catalog.GetNode(nodeId).DeclaredCapabilities)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray()))
            .ToArray();

        var deltaWindow = state.Topology.GetDeltasAfter(afterDeltaSequence);
        return new AdaptiveResearchTreeProjection(
            state.Topology.Sequence,
            state.CivilizationId,
            deltaWindow.RequiresFullRefresh,
            detailed,
            edges,
            history,
            deltaWindow.Deltas);
    }

    private IReadOnlyList<AdaptiveResearchEdgeView> BuildVisibleEdges(IReadOnlySet<string> detailedVisibleIds)
    {
        var edges = new List<AdaptiveResearchEdgeView>();
        foreach (var targetId in detailedVisibleIds)
        {
            var node = _catalog.GetNode(targetId);
            foreach (var prerequisite in node.Prerequisites.AllOf)
                if (detailedVisibleIds.Contains(prerequisite))
                    edges.Add(new AdaptiveResearchEdgeView(prerequisite, targetId, "known_prerequisite"));
            foreach (var prerequisite in node.Prerequisites.AnyOf)
                if (detailedVisibleIds.Contains(prerequisite))
                    edges.Add(new AdaptiveResearchEdgeView(prerequisite, targetId, "known_alternative"));
        }
        return edges
            .OrderBy(value => value.FromVisibleNodeId, StringComparer.Ordinal)
            .ThenBy(value => value.ToVisibleNodeId, StringComparer.Ordinal)
            .ToArray();
    }

    private string ResolveAnchor(
        AdaptiveResearchCivilizationState state,
        AdaptiveResearchNodeDefinition node,
        string groupId,
        string? sourceVisibleNodeId)
    {
        if (sourceVisibleNodeId is not null)
            return $"node:{sourceVisibleNodeId}";

        var visiblePrerequisite = node.Prerequisites.AllOf
            .Concat(node.Prerequisites.AnyOf)
            .Where(state.NodeStates.ContainsKey)
            .OrderByDescending(id => _catalog.GetNode(id).GraphDepth)
            .ThenBy(id => id, StringComparer.Ordinal)
            .FirstOrDefault();
        return visiblePrerequisite is null ? $"group:{groupId}" : $"node:{visiblePrerequisite}";
    }

    private static string GroupId(AdaptiveResearchNodeDefinition node) =>
        $"{node.DomainId}:{node.SolutionFamily}";

    private static string DefaultExplanation(AdaptiveResearchProvenanceKind kind) => kind switch
    {
        AdaptiveResearchProvenanceKind.StartingHistory => "This knowledge was already part of the civilization's legitimate starting history.",
        AdaptiveResearchProvenanceKind.BasicScience => "Ordinary basic-science review made this public possibility scientifically actionable.",
        AdaptiveResearchProvenanceKind.RecognizedNeed => "A recognized Research Pressure made this public possibility scientifically actionable.",
        AdaptiveResearchProvenanceKind.EvidenceContact => "Legitimately acquired evidence/contact created scientific awareness of this possibility.",
        AdaptiveResearchProvenanceKind.ForeignDiscovery => "Legitimate foreign-technology analysis created scientific awareness of this native research possibility.",
        AdaptiveResearchProvenanceKind.SideDiscovery => "Unexpected results from visible research exposed this related public possibility.",
        AdaptiveResearchProvenanceKind.PrerequisiteCapabilityChange => "A known prerequisite, applicability fact, or capability change made this possibility scientifically actionable.",
        AdaptiveResearchProvenanceKind.ExplicitMigration => "Migration preserved already-known research state without inventing a historical discovery cause.",
        _ => "Research state became visible through a legitimate known cause.",
    };
}

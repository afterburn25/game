using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Game.Simulation.Research.Adaptive;

public enum AdaptiveResearchProvenanceKind
{
    StartingHistory,
    BasicScience,
    RecognizedNeed,
    EvidenceContact,
    ForeignDiscovery,
    SideDiscovery,
    PrerequisiteCapabilityChange,
    ExplicitMigration,
}

public enum AdaptiveResearchTreeDeltaType
{
    NodeAdded,
    StateChanged,
    BranchAttachmentChanged,
    ProjectStateChanged,
    HistoryChanged,
}

public sealed record AdaptiveResearchProvenanceRecord(
    long Sequence,
    AdaptiveResearchProvenanceKind Kind,
    string? SourceVisibleNodeId,
    string? SubjectId,
    double? Year,
    string Explanation);

public sealed record AdaptiveResearchProvenanceCount(
    AdaptiveResearchProvenanceKind Kind,
    int Count);

public sealed record AdaptiveResearchTopologyNodeState(
    string NodeId,
    string GroupId,
    string AnchorId,
    long FirstSeenSequence,
    long LastChangedSequence,
    double? FirstSeenYear,
    AdaptiveResearchProvenanceRecord PrimaryProvenance,
    IReadOnlyList<AdaptiveResearchProvenanceCount> ProvenanceCounts,
    IReadOnlyList<AdaptiveResearchProvenanceRecord> RecentProvenance);

public sealed record AdaptiveResearchTreeDelta(
    long Sequence,
    AdaptiveResearchTreeDeltaType Type,
    string NodeId,
    string? SubjectId);

public sealed record AdaptiveResearchTreeDeltaWindow(
    long CurrentSequence,
    long EarliestRetainedSequence,
    bool RequiresFullRefresh,
    IReadOnlyList<AdaptiveResearchTreeDelta> Deltas);

/// <summary>
/// Sparse per-civilization topology/provenance sidecar. Only visible/materialized node IDs are stored.
/// Recent causal detail and UI-facing deltas are explicitly bounded; static graph definitions are never copied here.
/// </summary>
public sealed class AdaptiveResearchTopologyState
{
    public const int MaxRecentProvenancePerNode = 6;
    public const int MaxRecentDeltas = 256;

    private sealed class MutableNode
    {
        public required string NodeId { get; init; }
        public required string GroupId { get; set; }
        public required string AnchorId { get; set; }
        public required long FirstSeenSequence { get; init; }
        public required long LastChangedSequence { get; set; }
        public double? FirstSeenYear { get; init; }
        public required AdaptiveResearchProvenanceRecord PrimaryProvenance { get; init; }
        public Dictionary<AdaptiveResearchProvenanceKind, int> ProvenanceCounts { get; } = new();
        public List<AdaptiveResearchProvenanceRecord> RecentProvenance { get; } = new();

        public AdaptiveResearchTopologyNodeState Snapshot() => new(
            NodeId,
            GroupId,
            AnchorId,
            FirstSeenSequence,
            LastChangedSequence,
            FirstSeenYear,
            PrimaryProvenance,
            ProvenanceCounts
                .OrderBy(pair => pair.Key)
                .Select(pair => new AdaptiveResearchProvenanceCount(pair.Key, pair.Value))
                .ToArray(),
            RecentProvenance.OrderBy(value => value.Sequence).ToArray());
    }

    private readonly Dictionary<string, MutableNode> _nodes = new(StringComparer.Ordinal);
    private readonly List<AdaptiveResearchTreeDelta> _recentDeltas = new();
    private readonly HashSet<string> _collapsedHistoryNodeIds = new(StringComparer.Ordinal);

    public long Sequence { get; private set; }

    public IReadOnlyDictionary<string, AdaptiveResearchTopologyNodeState> Nodes =>
        new ReadOnlyDictionary<string, AdaptiveResearchTopologyNodeState>(
            _nodes.ToDictionary(pair => pair.Key, pair => pair.Value.Snapshot(), StringComparer.Ordinal));

    public IReadOnlyList<AdaptiveResearchTreeDelta> RecentDeltas =>
        _recentDeltas.OrderBy(value => value.Sequence).ToArray();

    public IReadOnlyCollection<string> CollapsedHistoryNodeIds =>
        _collapsedHistoryNodeIds.OrderBy(value => value, StringComparer.Ordinal).ToArray();

    public bool TryGetNode(string nodeId, out AdaptiveResearchTopologyNodeState node)
    {
        if (_nodes.TryGetValue(nodeId, out var stored))
        {
            node = stored.Snapshot();
            return true;
        }
        node = null!;
        return false;
    }

    internal void RecordMaterialization(
        string nodeId,
        string groupId,
        string anchorId,
        AdaptiveResearchProvenanceKind kind,
        string? sourceVisibleNodeId,
        string? subjectId,
        double? year,
        string explanation)
    {
        var sequence = NextSequence();
        var provenance = new AdaptiveResearchProvenanceRecord(
            sequence,
            kind,
            sourceVisibleNodeId,
            subjectId,
            year,
            explanation);

        if (!_nodes.TryGetValue(nodeId, out var node))
        {
            node = new MutableNode
            {
                NodeId = nodeId,
                GroupId = groupId,
                AnchorId = anchorId,
                FirstSeenSequence = sequence,
                LastChangedSequence = sequence,
                FirstSeenYear = year,
                PrimaryProvenance = provenance,
            };
            _nodes.Add(nodeId, node);
            AddProvenance(node, provenance);
            AddDelta(new AdaptiveResearchTreeDelta(sequence, AdaptiveResearchTreeDeltaType.NodeAdded, nodeId, kind.ToString()));
            return;
        }

        node.LastChangedSequence = sequence;
        AddProvenance(node, provenance);
    }

    internal void RecordNodeStateMutation(string nodeId, string? subjectId = null)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
            return;
        var sequence = NextSequence();
        node.LastChangedSequence = sequence;
        AddDelta(new AdaptiveResearchTreeDelta(sequence, AdaptiveResearchTreeDeltaType.StateChanged, nodeId, subjectId));
    }

    internal void RecordProjectMutation(string nodeId, string? subjectId = null)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
            return;
        var sequence = NextSequence();
        node.LastChangedSequence = sequence;
        AddDelta(new AdaptiveResearchTreeDelta(sequence, AdaptiveResearchTreeDeltaType.ProjectStateChanged, nodeId, subjectId));
    }

    internal void RecordNodeRemoved(string nodeId)
    {
        if (!_nodes.TryGetValue(nodeId, out var removed))
            return;

        var removalSequence = NextSequence();
        removed.LastChangedSequence = removalSequence;
        AddDelta(new AdaptiveResearchTreeDelta(removalSequence, AdaptiveResearchTreeDeltaType.StateChanged, nodeId, "removed"));
        _collapsedHistoryNodeIds.Remove(nodeId);

        var invalidAnchor = $"node:{nodeId}";
        foreach (var child in _nodes.Values.Where(value => string.Equals(value.AnchorId, invalidAnchor, StringComparison.Ordinal)).ToArray())
        {
            var sequence = NextSequence();
            child.AnchorId = $"group:{child.GroupId}";
            child.LastChangedSequence = sequence;
            AddDelta(new AdaptiveResearchTreeDelta(sequence, AdaptiveResearchTreeDeltaType.BranchAttachmentChanged, child.NodeId, child.AnchorId));
        }
    }

    internal void SetAnchor(string nodeId, string anchorId)
    {
        if (!_nodes.TryGetValue(nodeId, out var node) || string.Equals(node.AnchorId, anchorId, StringComparison.Ordinal))
            return;
        var sequence = NextSequence();
        node.AnchorId = anchorId;
        node.LastChangedSequence = sequence;
        AddDelta(new AdaptiveResearchTreeDelta(sequence, AdaptiveResearchTreeDeltaType.BranchAttachmentChanged, nodeId, anchorId));
    }

    internal void SetCollapsedHistory(string nodeId, bool collapsed)
    {
        var changed = collapsed ? _collapsedHistoryNodeIds.Add(nodeId) : _collapsedHistoryNodeIds.Remove(nodeId);
        if (!changed || !_nodes.TryGetValue(nodeId, out var node))
            return;
        var sequence = NextSequence();
        // Display-only collapse must not make old scientific state recent again.
        // Otherwise the next unchanged projection expands what this one collapsed.
        AddDelta(new AdaptiveResearchTreeDelta(sequence, AdaptiveResearchTreeDeltaType.HistoryChanged, nodeId, collapsed ? "collapsed" : "expanded"));
    }

    public AdaptiveResearchTreeDeltaWindow GetDeltasAfter(long afterSequence)
    {
        if (afterSequence < 0)
            throw new ArgumentOutOfRangeException(nameof(afterSequence));
        var earliest = _recentDeltas.Count == 0 ? Sequence + 1 : _recentDeltas[0].Sequence;
        var requiresFullRefresh = afterSequence < Sequence && afterSequence < earliest - 1;
        var deltas = requiresFullRefresh
            ? Array.Empty<AdaptiveResearchTreeDelta>()
            : _recentDeltas.Where(value => value.Sequence > afterSequence).ToArray();
        return new AdaptiveResearchTreeDeltaWindow(Sequence, earliest, requiresFullRefresh, deltas);
    }

    internal void RestoreNode(AdaptiveResearchTopologyNodeState snapshot)
    {
        if (_nodes.ContainsKey(snapshot.NodeId))
            throw new InvalidOperationException($"Duplicate topology node '{snapshot.NodeId}'.");
        var node = new MutableNode
        {
            NodeId = snapshot.NodeId,
            GroupId = snapshot.GroupId,
            AnchorId = snapshot.AnchorId,
            FirstSeenSequence = snapshot.FirstSeenSequence,
            LastChangedSequence = snapshot.LastChangedSequence,
            FirstSeenYear = snapshot.FirstSeenYear,
            PrimaryProvenance = snapshot.PrimaryProvenance,
        };
        foreach (var count in snapshot.ProvenanceCounts)
            node.ProvenanceCounts[count.Kind] = count.Count;
        node.RecentProvenance.AddRange(snapshot.RecentProvenance.OrderBy(value => value.Sequence));
        if (node.RecentProvenance.Count > MaxRecentProvenancePerNode)
            node.RecentProvenance.RemoveRange(0, node.RecentProvenance.Count - MaxRecentProvenancePerNode);
        _nodes.Add(snapshot.NodeId, node);
        Sequence = Math.Max(Sequence, Math.Max(snapshot.LastChangedSequence, snapshot.PrimaryProvenance.Sequence));
    }

    internal void RestoreSequence(long sequence)
    {
        if (sequence < 0)
            throw new ArgumentOutOfRangeException(nameof(sequence));
        Sequence = Math.Max(Sequence, sequence);
        _recentDeltas.Clear();
        _collapsedHistoryNodeIds.Clear();
    }

    private long NextSequence() => ++Sequence;

    private static void AddProvenance(MutableNode node, AdaptiveResearchProvenanceRecord provenance)
    {
        node.ProvenanceCounts[provenance.Kind] = node.ProvenanceCounts.TryGetValue(provenance.Kind, out var count) ? count + 1 : 1;
        node.RecentProvenance.Add(provenance);
        if (node.RecentProvenance.Count > MaxRecentProvenancePerNode)
            node.RecentProvenance.RemoveRange(0, node.RecentProvenance.Count - MaxRecentProvenancePerNode);
    }

    private void AddDelta(AdaptiveResearchTreeDelta delta)
    {
        _recentDeltas.Add(delta);
        if (_recentDeltas.Count > MaxRecentDeltas)
            _recentDeltas.RemoveRange(0, _recentDeltas.Count - MaxRecentDeltas);
    }
}

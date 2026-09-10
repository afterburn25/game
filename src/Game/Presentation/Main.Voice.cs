using System;
using System.Collections.Generic;
using System.Linq;
using Game.Diagnostics;
using Game.Presentation.Audio.Voice;
using Game.Simulation.Diplomacy;
using Game.Simulation.Exploration;
using Game.Simulation.Models;

namespace Game.Presentation;

public partial class Main
{
    public VoicePlaybackController? UiVoice { get; private set; }
    public bool UiHasVoiceMilestone(string key) => _voiceEvents?.HasEmitted(key) == true;
    private VoiceEventRouter? _voiceEvents;
    private readonly Dictionary<int,bool> _voiceFleetTransit = new();
    private readonly HashSet<long> _voiceProposals = new();
    private readonly HashSet<int> _voiceCriticalHulls = new();
    private readonly HashSet<string> _voiceMatureResearch = new(StringComparer.Ordinal);
    private bool _voiceBaseline, _voiceOpening = true;
    private double _voiceRefresh;

    protected void InitializeVoicePresentation()
    {
        UiVoice = new VoicePlaybackController { Name = "VoicePlaybackController" }; AddChild(UiVoice);
        try { _voiceEvents = VoiceEventRouter.FromJson(Godot.FileAccess.GetFileAsString("res://data/voice_profiles/events.json"), UiVoice.Speak); }
        catch (Exception error) { SupportLogger.Log("voice-fallback", "Dialogue catalogue unavailable: " + error.Message); }
        BaselineVoiceResearch();
    }
    private void ResetVoicePresentation()
    {
        UiVoice?.ResetCampaign(); _voiceEvents?.Reset();
        _voiceFleetTransit.Clear(); _voiceProposals.Clear(); _voiceCriticalHulls.Clear();
        _voiceBaseline = false; _voiceOpening = true; _voiceRefresh = 0;
        BaselineVoiceResearch();
    }
    protected void RefreshVoicePresentation(double delta)
    {
        if (_galaxy is null || UiVoice is null) return;
        if (_voiceOpening && !UiIsMenuOpen) { _voiceEvents?.Emit("opening"); _voiceOpening = false; }
        _voiceRefresh += delta; if (_voiceRefresh < .5) return; _voiceRefresh = 0;
        ObserveVoiceMilestones();
    }
    private void ObserveVoiceMilestones()
    {
        if (_galaxy is null || UiVoice is null) return;
        var fleets = _galaxy.Fleets.Where(f => f.IsActive && f.CivilizationId == _galaxy.PlayerCivilizationId).ToArray();
        foreach (var fleet in fleets)
        {
            bool transit = fleet.CurrentSystemId is null && fleet.DestinationSystemId.HasValue;
            if (_voiceBaseline && _voiceFleetTransit.TryGetValue(fleet.Id, out var previous))
            {
                if (!previous && transit) _voiceEvents?.Emit("departure", fleet.Name);
                if (previous && !transit && fleet.CurrentSystemId is not null) _voiceEvents?.Emit("arrival", fleet.Name);
            }
            _voiceFleetTransit[fleet.Id] = transit;
        }
        foreach (var stale in _voiceFleetTransit.Keys.Where(id => !fleets.Any(f => f.Id == id)).ToArray()) _voiceFleetTransit.Remove(stale);
        var combat = _coreSimulation.GetOwnCombatFleetStatus(_galaxy, _galaxy.PlayerCivilizationId);
        foreach (var fleet in combat.Fleets)
        {
            if (fleet.HullIntegrityRatio > .25) { _voiceCriticalHulls.Remove(fleet.FleetId); continue; }
            if (_voiceCriticalHulls.Add(fleet.FleetId) && _voiceBaseline) _voiceEvents?.Emit("critical_hull");
        }
        _voiceCriticalHulls.RemoveWhere(id => !fleets.Any(f => f.Id == id));
        // Consume the same scoped diplomatic proposal summaries the player can read.
        // The alien profile is a translator timbre, not a fabricated species identity.
        var view = new ObserverDiplomacyCommandService(_diplomacyState).BuildView(_galaxy.PlayerCivilizationId);
        foreach (var proposal in view.Proposals.Where(p => p.RecipientCivilizationId == _galaxy.PlayerCivilizationId))
            if (_voiceProposals.Add(proposal.ProposalId) && _voiceBaseline)
                _voiceEvents?.Emit("alien_transmission", proposal.Summary);
        _voiceProposals.RemoveWhere(id => !view.Proposals.Any(p => p.ProposalId == id));
        _voiceBaseline = true;
    }
    private void BaselineVoiceResearch()
    {
        _voiceMatureResearch.Clear();
        if (_adaptiveResearch is null || _galaxy is null) return;
        foreach (var state in _adaptiveResearch.GetCivilization(_galaxy.PlayerCivilizationId).NodeStates.Values)
            if (state.CountsAsEstablishedKnowledge) _voiceMatureResearch.Add(state.NodeId);
    }
    private void RouteAdaptiveResearchVoice(string nodeId)
    {
        if (_adaptiveResearch is null || !_adaptiveResearch.GetCivilization(_galaxy.PlayerCivilizationId)
                .TryGetNodeState(nodeId, out var state) || !state.CountsAsEstablishedKnowledge || !_voiceMatureResearch.Add(nodeId)) return;
        var name = _adaptiveResearch.Runtime.Authority.Catalog.Nodes.TryGetValue(nodeId, out var node) ? node.Name : "Current research program";
        _voiceEvents?.Emit("research", name + " is ready for use.");
    }
    private void RouteExplorationVoice(ExplorationEvent e)
    {
        if (e.CivilizationId != _galaxy.PlayerCivilizationId) return;
        if (e.Type == ExplorationEventType.ActivitySignatureDetected) _voiceEvents?.Emit("unknown_contact");
        else if (e.Type == ExplorationEventType.FirstContact) _voiceEvents?.Emit("first_contact", e.Message);
        else if (e.Type is ExplorationEventType.AnomalySignatureDetected or ExplorationEventType.AnomalySurveyed) _voiceEvents?.Emit("discovery", e.Message);
        else if (e.Type == ExplorationEventType.SystemSurveyed) _voiceEvents?.Emit("survey", e.Message);
    }
}

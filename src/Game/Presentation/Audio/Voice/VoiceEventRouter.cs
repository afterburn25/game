using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Game.Presentation.Audio.Voice;

public sealed record VoiceEventCue(string Event, string Profile, string[] Lines, int Priority = 40,
    bool Once = false, double CooldownSeconds = 12, bool Interruptible = true, string? PrerecordedPath = null);

/// <summary>Authored finite dialogue over already observer-filtered events. No simulation
/// writes, hidden-state access, random prose, network access, or provider calls.</summary>
public sealed class VoiceEventRouter
{
    private readonly Dictionary<string,VoiceEventCue> _cues;
    private readonly Dictionary<string,int> _counts = new();
    private readonly Dictionary<string,DateTimeOffset> _last = new();
    private readonly Action<SpeechRequest> _submit;
    public VoiceEventRouter(IEnumerable<VoiceEventCue> cues, Action<SpeechRequest> submit)
    { _cues = cues.ToDictionary(c => c.Event, StringComparer.Ordinal); _submit = submit; }
    public static VoiceEventRouter FromJson(string json, Action<SpeechRequest> submit) => new(
        JsonSerializer.Deserialize<VoiceEventCue[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? Array.Empty<VoiceEventCue>(), submit);
    public void Reset() { _counts.Clear(); _last.Clear(); }
    public bool HasEmitted(string key) => _counts.GetValueOrDefault(key) > 0;
    public bool Emit(string key, string detail = "")
    {
        if (!_cues.TryGetValue(key, out var cue) || cue.Lines.Length == 0) return false;
        _counts.TryGetValue(key, out int count);
        if (cue.Once && count > 0) return false;
        if (_last.TryGetValue(key, out var when) && DateTimeOffset.UtcNow - when < TimeSpan.FromSeconds(cue.CooldownSeconds)) return false;
        _counts[key] = count + 1; _last[key] = DateTimeOffset.UtcNow;
        var text = cue.Lines[count % cue.Lines.Length].Replace("{detail}", detail, StringComparison.Ordinal).Trim();
        _submit(new SpeechRequest(cue.Profile, text) {
            Priority = cue.Priority, Category = key, EventId = key, LocalizationKey = "voice." + key + "." + count % cue.Lines.Length,
            PrerecordedPath = cue.PrerecordedPath,
            DedupeKey = key + ":" + text, Interruptible = cue.Interruptible,
            QueueBehavior = SpeechQueueBehavior.ReplaceCategory, ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(40) });
        return true;
    }
}

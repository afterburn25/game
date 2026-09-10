# Stellar Continuum Voice Engine

Game text now becomes real offline speech through Windows SAPI, then plays through Godot with profile processing and sentence subtitles. The engine is presentation-only. Simulation does not await speech, depend on a provider, change its rules, or serialize an audio queue.

## Flow and ownership

`Main.Voice.cs` and the existing completion handlers consume already player-scoped events. `VoiceEventRouter` selects finite authored templates from `data/voice_profiles/events.json`. A `SpeechRequest` passes through profile resolution, pronunciation/normalization, prerecorded/cache lookup, a replaceable `IVoiceSpeechBackend`, WAV validation, and `VoicePlaybackController`. Only the controller accesses Godot audio and UI objects.

Construction, orbital shipyard, ship launch and colony announcements are driven by completion events, not command acceptance or message keywords. Adaptive Research announces newly established knowledge from the player's authoritative node state; starting or pausing research does not mean completion. Own fleet transitions are observed after each simulation step, including Developer time advances. Discovery/activity/contact events respect the existing observer filter. Critical hull reports read own fleet status; incoming translated transmissions read the same scoped diplomatic proposal summaries visible in the UI. Merely charting additional stars does not announce an unidentified vessel.

Opening, research, construction, shipyard, launch, departure, arrival, discovery, colony, unknown activity, alien transmission and critical hull cues are wired. Survey, first contact and operating shortfall are also covered. Voice reset clears queues and comparison state; loading a save establishes a baseline and does not repeat the opening or old completions. No save-format change is needed.

## Profiles, pronunciation and requests

| Profile ID | Role | Present implementation |
| --- | --- | --- |
| human_female_fleet_commander | Commander Elena Voss | Brisk military cadence, communications EQ |
| human_female_chief_scientist | Dr. Amara Chen | Measured technical delivery, lighter pitch |
| human_female_diplomat | Ambassador Mara Okafor | Slower, composed delivery |
| human_female_narrator | Narrator | Slow, lower-register cinematic delivery |
| human_male_fleet_commander | Commander Idris Kane | Brisk military delivery, lower register |
| human_male_governor | Governor Elias Ward | Deliberate administrative delivery |
| human_operations_officer | Operations Officer | Concise communications delivery |
| ship_computer | Ship Computer | Faster synthetic cadence and chorus |
| grey_diplomat | Grey Envoy | Slow cadence, restrained doubling, resonance, synthetic undertone |

Profiles are JSON records with identity/role/presentation, age/accent/style metadata, rate, pitch, radio/synthetic flags, resonance/chorus/reverb, preferred backend/model/voice, culture, subtitle name, portrait, fallback and enabled status. `Dsp` reserves additional alien-processing metadata. Identity, age, emotional tone and portrait fields are authoring metadata; the current backend does not turn every descriptor into a trained voice.

Pronunciation precedence is global → civilization → species → profile → character → request. Keys are matched at word boundaries in a single pass so an earlier replacement cannot destroy a later override. Scoped maps are `CivilizationPronunciations`, `SpeciesPronunciations`, `Pronunciations`, and `CharacterPronunciations`. English normalization covers ship IDs, astronomy names/units, percentages, comma-space coordinate pairs, clock times, ISO dates and common Roman designators. It preserves the original visible subtitle and ordinary “I” pronouns/thousands separators. Other languages require language-specific normalizers.

Requests carry profile/text/subtitle, priority, category, expiry/cooldown, event/localization key, interruptibility/queue behavior, cache policy, culture, emotion/urgency, pronunciation overrides, communications and spatial flags. Use the controller/router rather than calling SAPI from a screen.

## Offline provider and cache

`IVoiceSpeechBackend` exposes availability, voices, languages, offline/streaming/style support, output format, rate and hardware/latency metadata. The current factory selects Windows SAPI. A future provider can implement the interface without moving gameplay logic. No cloud provider, credentials, paid service, model download or GPU dependency is configured. Offline Only blocks synthesis through a backend reporting itself online. Unsupported platforms retain captions and valid prerecorded/cached files.

The SAPI adapter discovers installed voices, resolves preferred ID/name then gender/culture, converts SAPI language LCIDs, and confines COM calls to STA workers. Discovery is bounded; speech is asynchronous on one synthesis worker with cancellation/purge and a 30-second synthesis timeout. It writes 22,050 Hz, 16-bit mono PCM to a short temporary WAV, closes/validates it, then moves it into the user cache. This avoids SpFileStream's failure on deeply nested Windows paths. Temporary files are removed in success/failure cleanup. Text is explicitly non-XML.

Runtime storage is `user://voice-cache/v1`, normally beneath Godot's application user-data directory. The 256 MiB cache validates PCM RIFF chunks and keys normalized text, effective profile/rate/pitch/DSP, backend/model/version, actual selected voice, culture and processing version. Corrupt entries are rejected and least-recently-used entries pruned. Refresh regenerates; NoCache bypasses lookup and writes a temporary playback file subject to the same storage bound.

A request or event cue can supply `PrerecordedPath`; a valid PCM WAV is preferred to synthesis. Use an absolute external path or `user://` path. `res://` resolves to a filesystem path: exported prerecorded files must be shipped beside the executable (for example under the existing externally copied `data/` directory), rather than only inside the PCK. No prerecorded speech is bundled in this change.

When an installed voice disappears, the original voice-specific key remains intact. Only an unavailable backend may use `fallback-index.json` to recover a previously generated matching line without knowing the missing voice ID. The atomic index accepts only validated 64-hex WAV filenames inside the cache, caps itself at 1 MiB/4096 entries, and removes stale aliases. NoCache output is never indexed.

## Playback, subtitles and settings

The Godot controller has a separate bounded playback queue of eight lines, two per category, short-lived deduplication and a 40-second stale-line limit. Higher priorities sort first. Explicit interruption/replacement may interrupt lower, interruptible lines; plain enqueue does not. Cinematic opening narration is non-interruptible. No Interruptions preserves the active line. Cancel/reset clears pending generation and playback so stale callbacks cannot start audio in another campaign.

The Voice bus applies EQ, mild pitch adjustment, restrained chorus/harmonic doubling and short reverb. Profile communications processing can be bypassed for direct narration. A Communications bus is reserved for future routing. Music fades to 55% during dialogue and restores smoothly; SFX and authoritative timing continue normally.

Subtitles use speaker labels and original text, with duration based on audio length and a reading-time floor. They work when voice is muted, synthesis is unavailable or a request fails. Muting during pending synthesis cancels that work and preserves a caption. Gameplay's existing notifications remain available independently of voice.

Open **Voice & subtitles** from the campaign menu for voice enable/volume, subtitle enable/size/background/labels, communications intensity, important-only chatter, no interruptions, replay and stop. Settings persist in `user://voice-settings.json`; corrupt data safely falls back to defaults. Unsupported backend/quality controls are hidden. In Developer mode, **Developer Voice Lab** adds profile choice, editable sample text, restrained emotion/cadence preview, synthesis/play, stop/replay and diagnostics. Long token identifiers wrap at 720p. Reset closes the Lab.

Diagnostics distinguish synthesized, cached, prerecorded and subtitle-only outcomes, selected voice/profile, length and pending work. Backend errors include support-log detail; player notifications do not expose backend internals. No audio exception is allowed to escape the engine's work queue or controller's result handling.

## Licensing and privacy

| Item | Source / rights boundary | Distribution in this change |
| --- | --- | --- |
| Windows SAPI / installed voices | Existing Microsoft Windows or third-party installation; use remains subject to that component/provider's terms | Referenced through OS COM interfaces; no voice DLL, model, installer or OS component redistributed |
| New C# engine, profile JSON and authored dialogue | Project-authored source/data | Included in this branch |
| Godot and .NET | Existing project runtimes and existing export licensing notices | Existing packaging remains responsible for runtime notices |
| Third-party neural models, voice recordings, cloud SDKs | None added | None bundled |

The [Microsoft SpVoice reference](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ms723602(v=vs.85)) and [GetVoices reference](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ee125639(v=vs.85)) document the OS interface. This change does not grant redistribution rights to installed provider voices. Before bundling any future model, actor recording or generated content library, record that specific source, commercial-use/redistribution terms, attribution and voice restrictions. There are no unverified bundled voice assets in this PR.

This implementation sends no text/audio to a network service. Cache files and support logs can contain player-visible dialogue and locally entered Voice Lab text; cache filenames hash their identity but the audio itself is not encrypted. Users can remove the voice-cache folder with the game closed. A future cloud backend must be explicit opt-in and must not send hidden simulation state or log secrets.

## Quality limits and next work

The verified Windows machine has Zira and David. Four female roles currently use distinct cadence and processing over one installed female base voice; this is **not four independently voiced characters or production-quality cinematic acting**. Preferred voice IDs permit replacement, but acquiring/licensing four stronger base voices remains a quality requirement. Missing voices degrade honestly to the available installed voice or captions.

Grey is an audio/translation preset, not an invented canonical civilization. Current alien sound combines cadence, pitch, resonance and restrained chorus/reverb. Independent formant shifting, whisper layers, neural emotions and true multilingual translation are not implemented. Limited emotion choices apply small rate adjustments; they do not claim neural style control. Spatial requests reserve a future interface and currently play as non-spatial UI speech. Thalori, untranslated/partially translated dialogue and mixed original/translator layers need dedicated content/provider work.

## Validation

Run from the repository root:

```powershell
dotnet build Game.sln --configuration Release
dotnet run --project tests/VoiceCoreChecks/VoiceCoreChecks.csproj --configuration Release
dotnet run --project tests/Game.CoreRuntime.Validation/Game.CoreRuntime.Validation.csproj --configuration Release
```

Core checks cover normalization, scoped overrides, profiles/fallbacks, malformed settings, valid/corrupt WAV and cache identity/pruning, priority/expiry/duplicates/cancel, offline policy, cache modes, authored routing/variation and real installed SAPI synthesis/cache reuse. Set `STELLAR_REQUIRE_SAPI=1` to require installed voice proof; unsupported environments explicitly report a skip rather than fake successful synthesis. The voice workflow runs these checks on Ubuntu and Windows.

Native Godot evidence: set `STELLAR_CAPTURE_FOCUS=voice`, a fresh writable user profile and `STELLAR_SCREENSHOT_DIR`, then run `res://tools/ScreenshotCapture.tscn` at 1280×720 with the pinned Godot 4.7.2 .NET editor. The maintained entry point catches failures and exits nonzero. Focused captures deliberately cannot substitute for the full screenshot release gate.

On September 10, 2026, the local Windows candidate passed 19 focused runtime checks, five visually inspected 720p screenshots, real timed research/shipyard/shipbuild and mouse-directed departure/arrival. Narrator and Grey live processed-bus WAVs contained nonzero audio (narrator RMS .1257; Grey RMS .0823). Failure fallback, pending mute, captions, cache replay, no interruptions and reset/Lab cleanup passed. Debug/Release builds were clean and Core Runtime regressions passed 70/70. The other event hooks have data/structural validation; the focused live walkthrough does not claim to have naturally encountered every rare colony/contact/combat event.

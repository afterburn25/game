using System.Text;
using Game.Presentation.Audio.Voice;

return await RunSafelyAsync();

static async Task<int> RunSafelyAsync()
{
    var passed = 0;
    try
    {
        var root = Directory.GetCurrentDirectory();
        var output = Path.Combine(root, "work", "voice-core-proof");
        if (Directory.Exists(output)) Directory.Delete(output, true);
        Directory.CreateDirectory(output);
        var registry = VoiceProfileRegistry.Load(Path.Combine(root, "data", "voice_profiles", "human.json"));
        Require(registry.All.Count == 9, "Profile registry must load eight baseline roles plus the Grey envoy.");
        foreach (var id in new[] { "human_female_fleet_commander", "human_female_chief_scientist", "human_female_diplomat",
                     "human_female_narrator", "human_male_fleet_commander", "human_male_governor", "ship_computer",
                     "human_operations_officer", "grey_diplomat" }) Require(registry.Resolve(id).Id == id, $"Missing profile {id}.");
        var grey = registry.Resolve("grey_diplomat");
        Require(grey.Dsp.AlienAmount > .5f && grey.Dsp.HarmonicLayer is > 0 and < .25f && grey.Dsp.Distortion < .05f,
            "Grey processing must be recognizable, restrained and intelligible.");
        Require(new[] { "human_female_fleet_commander", "human_female_chief_scientist", "human_female_diplomat", "human_female_narrator" }
                .Select(id => registry.Resolve(id).Rate).Distinct().Count() == 4,
            "Female roles need distinct SAPI cadence even when Windows has only one installed female voice.");
        Pass("profiles-and-grey-dsp", ref passed);

        var normalized = SpeechText.Normalize("FTL-01 approaching Alpha Centauri at 0.42 c, coordinates -3.5, 8.2. Output 42% at 14:30.");
        Require(normalized.Contains("F T L zero one") && normalized.Contains("Sen-tor-eye") &&
            normalized.Contains("0.42 times the speed of light") && normalized.Contains("-3.5 by 8.2") &&
            normalized.Contains("42 percent") && normalized.Contains("14 30 hours"), "Astronomical normalization failed: " + normalized);
        Require(SpeechText.ApplyPronunciations("Sol solution SOL", new Dictionary<string, string> { ["Sol"] = "Sohl" }) ==
            "Sohl solution Sohl", "Pronunciation replacement must use word boundaries.");
        Pass("normalization-and-word-boundaries", ref passed);

        var settingsPath = Path.Combine(output, "settings.json");
        File.WriteAllText(settingsPath, "{ malformed");
        Require(VoiceSettings.Load(settingsPath) == new VoiceSettings(), "Malformed settings must fall back safely.");
        var dirty = new VoiceSettings(Volume: 4, SubtitleSize: 200, Opacity: -2, ChatterLevel: float.NaN, CommsIntensity: 8);
        dirty.Save(settingsPath); var clean = VoiceSettings.Load(settingsPath);
        Require(clean.Volume == 1 && clean.SubtitleSize == 42 && clean.Opacity == 0 && clean.ChatterLevel == 1 && clean.CommsIntensity == 1,
            "Settings were not sanitized across persistence.");
        Pass("settings-corrupt-fallback-and-sanitize", ref passed);

        var cache = new VoiceCache(Path.Combine(output, "cache"), 600);
        var commander = registry.Resolve("human_female_fleet_commander");
        var key1 = cache.PathFor(commander, "ready", "fake", "m", "1", "a", "en-US");
        var key2 = cache.PathFor(commander, "ready", "fake", "m", "1", "a", "en-US");
        var key3 = cache.PathFor(commander, "ready", "fake", "m", "2", "a", "en-US");
        Require(key1 == key2 && key1 != key3, "Cache key must be stable and include backend version.");
        File.WriteAllText(key1, "not wave"); Require(!cache.TryGetValid(key1) && !File.Exists(key1), "Invalid cached WAV was accepted.");
        for (var index = 0; index < 4; index++) WavTest.Write(Path.Combine(cache.DirectoryPath, $"trim-{index}.wav"), 200 + index * 10);
        cache.Trim(); Require(new DirectoryInfo(cache.DirectoryPath).GetFiles("*.wav").Sum(file => file.Length) <= 600, "Cache size limit was not enforced.");
        Pass("cache-identity-validation-and-pruning", ref passed);

        await VerifyQueueAsync(registry, output); Pass("priority-expiry-dedupe-clear-and-failure", ref passed);
        await VerifyOfflineFallbacksAsync(registry, output); Pass("cache-and-prerecorded-work-without-backend", ref passed);
        await VerifyVoiceIdFallbackIndexAsync(registry, output); Pass("voice-id-cache-fallback-index", ref passed);
        await VerifyRequestPoliciesAsync(registry, output); Pass("offline-policy-cache-policy-and-pronunciation-precedence", ref passed);
        VerifyDialogueRouting(registry, root); Pass("authored-routing-priority-cooldown-reset-and-variation", ref passed);
        using (var installed = new WindowsSapiSpeechBackend())
        {
            var female = registry.Resolve("human_female_chief_scientist");
            var male = registry.Resolve("human_male_governor");
            var femaleVoice = installed.ResolveVoiceId(female, female.Culture);
            var maleVoice = installed.ResolveVoiceId(male, male.Culture);
            var distinct = !string.IsNullOrWhiteSpace(femaleVoice) && !string.IsNullOrWhiteSpace(maleVoice) && femaleVoice != maleVoice;
            if (installed.Capabilities.Available && (distinct || Environment.GetEnvironmentVariable("STELLAR_REQUIRE_SAPI") == "1"))
            { await VerifySapiAsync(registry, output); Pass("real-sapi-male-female-pcm", ref passed); }
            else
            {
                Require(Environment.GetEnvironmentVariable("STELLAR_REQUIRE_SAPI") != "1", installed.Capabilities.Detail ?? "SAPI is required.");
                Console.WriteLine($"VOICE_SAPI_SKIPPED installed={installed.Capabilities.Voices.Count} female={femaleVoice} male={maleVoice} detail={installed.Capabilities.Detail}");
            }
        }

        Console.WriteLine($"VOICE_CORE_CHECKS_COMPLETE passed={passed} output={output}");
        return 0;
    }
    catch (Exception error)
    {
        Console.Error.WriteLine("VOICE_CORE_CHECKS_FAILED"); Console.Error.WriteLine(error); return 1;
    }
}

static async Task VerifyQueueAsync(VoiceProfileRegistry registry, string output)
{
    var backend = new ControlledBackend();
    await using var engine = new VoiceEngine(registry, backend, new VoiceCache(Path.Combine(output, "queue-cache")));
    var blocker = engine.EnqueueAsync(new("human_operations_officer", "blocker") { DedupeKey = "blocker", Priority = 10 });
    await backend.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
    var low = engine.EnqueueAsync(new("human_operations_officer", "low") { DedupeKey = "low", Priority = 5 });
    var high = engine.EnqueueAsync(new("human_operations_officer", "high") { DedupeKey = "high", Priority = 80 });
    var expiring = engine.EnqueueAsync(new("human_operations_officer", "stale")
        { DedupeKey = "stale", Priority = 20, ExpiresAt = DateTimeOffset.UtcNow.AddMilliseconds(40) });
    var duplicate = await engine.EnqueueAsync(new("human_operations_officer", "low") { DedupeKey = "low", Priority = 5 });
    Require(!duplicate.Succeeded && duplicate.Error == "cooldown", "Duplicate suppression failed.");
    await Task.Delay(80); backend.Release.TrySetResult(true);
    Require((await blocker).Succeeded && (await high).Succeeded && (await low).Succeeded, "Queued synthesis did not complete.");
    Require(!(await expiring).Succeeded && (await expiring).Error == "expired", "Stale queued line was spoken.");
    Require(backend.Order.Take(3).SequenceEqual(new[] { "blocker", "high", "low" }), "Priority order failed: " + string.Join(',', backend.Order));

    var cancelling = new ControlledBackend();
    await using var cancelEngine = new VoiceEngine(registry, cancelling, new VoiceCache(Path.Combine(output, "cancel-cache")));
    var active = cancelEngine.EnqueueAsync(new("human_operations_officer", "cancel me") { DedupeKey = "cancel-me" });
    await cancelling.Started.Task.WaitAsync(TimeSpan.FromSeconds(3)); cancelEngine.ClearPending();
    var cancelled = await active.WaitAsync(TimeSpan.FromSeconds(3));
    Require(!cancelled.Succeeded && cancelled.Error == "cancelled", "ClearPending did not cancel active synthesis.");
}

static async Task VerifyOfflineFallbacksAsync(VoiceProfileRegistry registry, string output)
{
    var unavailable = new UnavailableBackend();
    var cache = new VoiceCache(Path.Combine(output, "offline-cache"));
    var profile = registry.Resolve("human_female_narrator");
    var text = SpeechText.Normalize("A cached line remains available offline.", profile.Pronunciations, null);
    var cachedPath = cache.PathFor(profile, text, unavailable.BackendId, unavailable.Model, unavailable.Version,
        unavailable.ResolveVoiceId(profile, profile.Culture), profile.Culture);
    WavTest.Write(cachedPath, 320);
    await using var engine = new VoiceEngine(registry, unavailable, cache);
    var cached = await engine.EnqueueAsync(new(profile.Id, "A cached line remains available offline."));
    Require(cached.Succeeded && cached.CacheHit, "Valid cached line was blocked by unavailable backend.");
    var prerecordedPath = Path.Combine(output, "prerecorded.wav"); WavTest.Write(prerecordedPath, 480);
    var prerecorded = await engine.EnqueueAsync(new(profile.Id, "A bespoke cinematic line.")
        { DedupeKey = "prerecorded", PrerecordedPath = prerecordedPath });
    Require(prerecorded.Succeeded && prerecorded.Prerecorded, "Prerecorded line was not preferred.");
    var absent = await engine.EnqueueAsync(new(profile.Id, "No source exists.") { DedupeKey = "absent" });
    Require(!absent.Succeeded && absent.Error!.Contains("unavailable"), "Backend-unavailable path did not fail gracefully.");
    var missingProfile = await engine.EnqueueAsync(new("does_not_exist", "Subtitle survives."));
    Require(!missingProfile.Succeeded && missingProfile.SubtitleText == "Subtitle survives.", "Profile failure escaped or lost subtitle.");
}

static async Task VerifyVoiceIdFallbackIndexAsync(VoiceProfileRegistry registry, string output)
{
    var directory = Path.Combine(output, "voice-id-fallback");
    var cache = new VoiceCache(directory);
    var profile = registry.Resolve("human_female_narrator");
    const string line = "Previously synthesized audio remains available without an installed voice.";
    var available = new VoiceIdentityBackend(true, "voice-id-a");
    VoiceResult created;
    await using (var engine = new VoiceEngine(registry, available, cache))
        created = await engine.EnqueueAsync(new(profile.Id, line) { DedupeKey = "voice-id-create" });
    var normalized = SpeechText.NormalizeForProfile(line, profile, null);
    var actualVoicePath = cache.PathFor(profile, normalized, available.BackendId, available.Model,
        available.Version, "voice-id-a", profile.Culture);
    Require(created.Succeeded && !created.CacheHit && created.WavePath == actualVoicePath && available.Calls == 1,
        "Available backend did not preserve its actual voice in the primary cache identity.");

    var unavailable = new VoiceIdentityBackend(false, "");
    await using (var engine = new VoiceEngine(registry, unavailable, cache))
    {
        var fallback = await engine.EnqueueAsync(new(profile.Id, line) { DedupeKey = "voice-id-fallback" });
        Require(fallback.Succeeded && fallback.CacheHit && fallback.WavePath == actualVoicePath && unavailable.Calls == 0,
            "Unavailable backend could not resolve the validated WAV created with an actual voice ID.");
    }

    const string transientLine = "A transient line must never enter the fallback index.";
    var noCacheBackend = new VoiceIdentityBackend(true, "voice-id-a");
    await using (var engine = new VoiceEngine(registry, noCacheBackend, cache))
    {
        var transient = await engine.EnqueueAsync(new(profile.Id, transientLine)
            { DedupeKey = "voice-id-transient", CachePolicy = SpeechCachePolicy.NoCache });
        Require(transient.Succeeded && !transient.CacheHit, "NoCache synthesis did not run.");
    }
    await using (var engine = new VoiceEngine(registry, unavailable, cache))
    {
        var absent = await engine.EnqueueAsync(new(profile.Id, transientLine) { DedupeKey = "voice-id-transient-fallback" });
        Require(!absent.Succeeded && unavailable.Calls == 0, "NoCache output leaked into the fallback index.");
    }

    var outside = Path.Combine(output, "outside-cache.wav"); WavTest.Write(outside, 320);
    cache.RememberFallback(profile, "outside identity", available.BackendId, available.Model, available.Version,
        profile.Culture, outside);
    Require(!cache.TryGetFallback(profile, "outside identity", available.BackendId, available.Model,
        available.Version, profile.Culture, out _), "Fallback index accepted an arbitrary path outside its cache directory.");

    File.Delete(actualVoicePath);
    Require(!cache.TryGetFallback(profile, normalized, available.BackendId, available.Model, available.Version,
        profile.Culture, out _), "Fallback index retained a pruned WAV entry.");
    var indexText = File.ReadAllText(Path.Combine(directory, "fallback-index.json"));
    Require(!indexText.Contains(Path.GetFileName(actualVoicePath), StringComparison.Ordinal),
        "Stale fallback filename remained in the bounded index.");
}

static async Task VerifySapiAsync(VoiceProfileRegistry registry, string output)
{
    var sapi = new WindowsSapiSpeechBackend();
    Require(sapi.Capabilities.Available, sapi.Capabilities.Detail ?? "SAPI unavailable.");
    var female = registry.Resolve("human_female_chief_scientist"); var male = registry.Resolve("human_male_governor");
    var femaleVoice = sapi.ResolveVoiceId(female, female.Culture); var maleVoice = sapi.ResolveVoiceId(male, male.Culture);
    Require(!string.IsNullOrWhiteSpace(femaleVoice) && !string.IsNullOrWhiteSpace(maleVoice) && femaleVoice != maleVoice,
        $"Distinct installed female/male SAPI voices were not resolved: {femaleVoice} / {maleVoice}");
    await using var engine = new VoiceEngine(registry, sapi, new VoiceCache(Path.Combine(output, "sapi-cache")));
    var femaleResult = await engine.EnqueueAsync(new(female.Id, "Long range sensors confirm a stable exoplanet atmosphere.") { DedupeKey = "sapi-female-1" });
    var maleResult = await engine.EnqueueAsync(new(male.Id, "Colony administration reports all essential systems operational.") { DedupeKey = "sapi-male-1" });
    Require(femaleResult.Succeeded && maleResult.Succeeded && femaleResult.SelectedVoice == femaleVoice && maleResult.SelectedVoice == maleVoice,
        "VoiceEngine did not return the installed voice used for synthesis.");
    var femalePath = femaleResult.WavePath!; var malePath = maleResult.WavePath!;
    var femaleSamples = ReadPcmSampleCount(femalePath); var maleSamples = ReadPcmSampleCount(malePath);
    Require(femaleSamples > 4000 && maleSamples > 4000, $"SAPI WAV data is empty: {femaleSamples}/{maleSamples} samples.");
    var cached = await engine.EnqueueAsync(new(female.Id, "Long range sensors confirm a stable exoplanet atmosphere.") { DedupeKey = "sapi-female-cache" });
    Require(cached.Succeeded && cached.CacheHit && cached.WavePath == femalePath, "Real SAPI output did not round-trip through cache.");
    Console.WriteLine($"SAPI_PROOF femaleVoice={femaleVoice} samples={femaleSamples} path={femalePath}");
    Console.WriteLine($"SAPI_PROOF maleVoice={maleVoice} samples={maleSamples} path={malePath}");
}

static int ReadPcmSampleCount(string path)
{
    Require(VoiceCache.IsValidWave(path), "Invalid WAV: " + path);
    using var reader = new BinaryReader(File.OpenRead(path)); reader.ReadBytes(12);
    ushort channels = 1, bits = 16; int data = 0;
    while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
    {
        var id = Encoding.ASCII.GetString(reader.ReadBytes(4)); var size = reader.ReadInt32();
        if (id == "fmt ") { var format = reader.ReadInt16(); channels = reader.ReadUInt16(); reader.ReadInt32(); reader.ReadInt32(); reader.ReadInt16(); bits = reader.ReadUInt16(); Require(format == 1, "WAV is not PCM."); reader.BaseStream.Position += size - 16; }
        else if (id == "data") { data = size; break; }
        else reader.BaseStream.Position += size;
        if ((size & 1) != 0) reader.BaseStream.Position++;
    }
    return data * 8 / Math.Max(1, channels * bits);
}

static void Pass(string name, ref int count) { count++; Console.WriteLine("VOICE_CORE_CHECK_PASS " + name); }
static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }


static async Task VerifyRequestPoliciesAsync(VoiceProfileRegistry registry, string output)
{
    Require(SpeechText.Normalize("I approve 1,000 units for Sector IV.").Contains("I approve 1,000 units for Sector four."),
        "Speech normalization corrupted a pronoun, thousands separator or Roman sector number.");
    Require(SpeechText.Normalize("2050-01-02").Contains("January 2, 2050"), "ISO date normalization failed.");
    var profile = registry.Resolve("human_female_chief_scientist") with {
        CivilizationPronunciations = new Dictionary<string,string> { ["Sol"] = "civilization" },
        SpeciesPronunciations = new Dictionary<string,string> { ["Sol"] = "species" },
        Pronunciations = new Dictionary<string,string> { ["Sol"] = "profile" },
        CharacterPronunciations = new Dictionary<string,string> { ["Sol"] = "character" } };
    Require(SpeechText.NormalizeForProfile("Sol solution", profile, null) == "character solution", "Character dictionary did not override the profile/species/civilization/global dictionaries.");
    Require(SpeechText.NormalizeForProfile("Sol", profile, new Dictionary<string,string> { ["Sol"] = "request" }) == "request",
        "Request pronunciation override was lost to an earlier replacement.");
    var backend = new PolicyBackend();
    await using (var engine = new VoiceEngine(registry, backend, new VoiceCache(Path.Combine(output, "offline-policy"))))
    {
        var result = await engine.EnqueueAsync(new(profile.Id, "Private local dialogue."));
        Require(!result.Succeeded && backend.Calls == 0 && result.SubtitleText == "Private local dialogue.",
            "Offline Only permitted an online synthesis call or lost the subtitle.");
    }
    await using (var enabled = new VoiceEngine(registry, backend, new VoiceCache(Path.Combine(output, "cache-policy")), new VoiceSettings(OfflineOnly: false)))
    {
        var first = await enabled.EnqueueAsync(new(profile.Id, "Reusable line.") { DedupeKey = "cache-first" });
        var second = await enabled.EnqueueAsync(new(profile.Id, "Reusable line.") { DedupeKey = "cache-second" });
        var uncached = await enabled.EnqueueAsync(new(profile.Id, "Reusable line.") { DedupeKey = "cache-bypass", CachePolicy = SpeechCachePolicy.NoCache });
        Require(first.Succeeded && second.CacheHit && uncached.Succeeded && !uncached.CacheHit && uncached.WavePath != first.WavePath && backend.Calls == 2,
            "NoCache did not bypass the existing cache entry.");
        var tooLong = await enabled.EnqueueAsync(new(profile.Id, new string('a', 4001)));
        Require(!tooLong.Succeeded && backend.Calls == 2, "Unbounded text reached synthesis.");
    }
}

static void VerifyDialogueRouting(VoiceProfileRegistry registry, string root)
{
    var lines = new List<SpeechRequest>();
    var router = VoiceEventRouter.FromJson(File.ReadAllText(Path.Combine(root, "data", "voice_profiles", "events.json")), lines.Add);
    foreach (var key in new[] { "opening", "research", "construction", "shipyard", "ship_launch", "departure",
        "arrival", "discovery", "colony", "unknown_contact", "alien_transmission", "critical_hull" })
    {
        Require(router.Emit(key, "Observer-visible event."), "Missing required gameplay cue " + key);
        Require(!router.Emit(key, "Duplicate event."), "Cue cooldown did not suppress burst " + key);
        Require(registry.Resolve(lines[^1].ProfileId).Enabled && !string.IsNullOrWhiteSpace(lines[^1].LocalizationKey),
            "Cue uses missing speaker or localization identity.");
    }
    Require(lines.Single(l => l.Category == "critical_hull").Priority > lines.Single(l => l.Category == "research").Priority,
        "Research would outrank a critical combat warning.");
    Require(!lines.Single(l => l.Category == "opening").Interruptible, "Opening cinematic is interruptible by routine chatter.");
    router.Reset(); Require(!router.HasEmitted("opening") && router.Emit("opening"), "Campaign reset retained stale cue history.");
    var variants = new VoiceEventRouter(new[] { new VoiceEventCue("test", "ship_computer", new[] { "One", "Two" }, CooldownSeconds: 0) }, lines.Add);
    variants.Emit("test"); variants.Emit("test");
    Require(lines[^2].Text == "One" && lines[^1].Text == "Two", "Authored variation is not deterministic.");
}

sealed class PolicyBackend : IVoiceSpeechBackend
{
    public SpeechBackendCapabilities Capabilities { get; } = new(true, new[] { "test" }) { Offline = false, BackendId = "online-policy-test" };
    public int Calls { get; private set; }
    public Task SynthesizeAsync(VoiceProfile profile, string text, string path, CancellationToken token)
    { Calls++; WavTest.Write(path, 500); return Task.CompletedTask; }
}

sealed class ControlledBackend : IVoiceSpeechBackend, IDisposable
{
    public SpeechBackendCapabilities Capabilities { get; } = new(true, new[] { "controlled" }) { BackendId = "controlled" };
    public string BackendId => "controlled"; public string Model => "test"; public string Version => "1";
    public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public List<string> Order { get; } = new();
    public async Task SynthesizeAsync(VoiceProfile profile, string text, string path, CancellationToken token)
    {
        lock (Order) Order.Add(text); Started.TrySetResult(true);
        if (text is "blocker" or "cancel me") await Release.Task.WaitAsync(token);
        token.ThrowIfCancellationRequested(); WavTest.Write(path, 500);
    }
    public void Dispose() => Release.TrySetResult(true);
}

sealed class UnavailableBackend : IVoiceSpeechBackend
{
    public SpeechBackendCapabilities Capabilities { get; } = new(false, Array.Empty<string>(), "backend unavailable") { BackendId = "offline-test" };
    public string BackendId => "offline-test"; public string Model => "none"; public string Version => "7";
    public string ResolveVoiceId(VoiceProfile profile, string culture) => "none";
    public Task SynthesizeAsync(VoiceProfile profile, string text, string wavPath, CancellationToken cancellationToken) => throw new InvalidOperationException();
}

sealed class VoiceIdentityBackend(bool available, string voice) : IVoiceSpeechBackend
{
    public SpeechBackendCapabilities Capabilities { get; } = new(available, available ? new[] { voice } : Array.Empty<string>(),
        available ? null : "voice temporarily unavailable") { BackendId = "voice-id-test", Offline = true };
    public string BackendId => "voice-id-test"; public string Model => "same-model"; public string Version => "3";
    public int Calls { get; private set; }
    public string ResolveVoiceId(VoiceProfile profile, string culture) => voice;
    public Task SynthesizeAsync(VoiceProfile profile, string text, string wavPath, CancellationToken cancellationToken)
    { Calls++; WavTest.Write(wavPath, 640); return Task.CompletedTask; }
}

static class WavTest
{
    public static void Write(string path, int samples)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); var dataSize = samples * 2;
        using var writer = new BinaryWriter(File.Create(path)); writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
        writer.Write(22050); writer.Write(44100); writer.Write((short)2); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(dataSize);
        for (var index = 0; index < samples; index++) writer.Write((short)(Math.Sin(index * .08) * 4000));
    }
}

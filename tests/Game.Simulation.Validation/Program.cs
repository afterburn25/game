using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Persistence;
using Game.Simulation;
using Game.Simulation.Generation;
using Game.Simulation.Shipbuilding;
using Game.Simulation.Species;

namespace Game.Simulation.Validation;

internal static class Program
{
    private static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("deterministic galaxy generation", ValidateDeterministicGalaxyGeneration),
            ("simulation clock pause/backlog", ValidateSimulationClock),
            ("save format v8 round trip", ValidateSaveRoundTrip),
            ("v6 to current shipyard/species migration", ValidateV6Migration),
            ("v7 to v8 species migration", ValidateV7SpeciesMigration),
            ("bounded shipyard queue load", ValidateBoundedShipyardQueueLoad),
        };

        var failures = 0;
        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine($"PASS: {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.Error.WriteLine($"FAIL: {test.Name}: {ex.Message}");
            }
        }

        Console.WriteLine($"Core simulation validation: {tests.Length - failures}/{tests.Length} passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void ValidateDeterministicGalaxyGeneration()
    {
        const long seed = 0x51A7_2026_0907;
        var settings = new GalaxyGenerationSettings
        {
            SystemCount = 48,
            PreWarpCivilizationCount = 5,
            AncientCivilizationCount = 1,
            Radius = 520.0f,
        };

        var generator = new GalaxyGenerator();
        var first = generator.Generate(seed, settings);
        var second = generator.Generate(seed, settings);

        Require(first.Seed == second.Seed, "seed changed between identical generations");
        Require(first.PlayerCivilizationId == second.PlayerCivilizationId, "player civilization changed between identical generations");
        Require(first.Systems.Count == second.Systems.Count, "system count changed between identical generations");
        Require(first.Civilizations.Count == second.Civilizations.Count, "civilization count changed between identical generations");

        for (var i = 0; i < first.Systems.Count; i++)
        {
            var a = first.Systems[i];
            var b = second.Systems[i];
            Require(a.Id == b.Id && a.Name == b.Name, $"system identity diverged at index {i}");
            Require(a.Position == b.Position, $"system position diverged for {a.Name}");
            Require(a.Archetype == b.Archetype, $"system archetype diverged for {a.Name}");
            Require(a.HasHabitableWorld == b.HasHabitableWorld, $"habitability diverged for {a.Name}");
            Require(a.HasAnomaly == b.HasAnomaly, $"anomaly state diverged for {a.Name}");
            Require(a.HasRareResource == b.HasRareResource, $"resource state diverged for {a.Name}");
            Require(a.HasPreWarpCivilization == b.HasPreWarpCivilization, $"pre-warp state diverged for {a.Name}");
        }

        for (var i = 0; i < first.Civilizations.Count; i++)
        {
            var a = first.Civilizations[i];
            var b = second.Civilizations[i];
            Require(a.Id == b.Id && a.Name == b.Name, $"civilization identity diverged at index {i}");
            Require(a.HomeSystemId == b.HomeSystemId, $"home system diverged for {a.Name}");
            Require(a.Archetype == b.Archetype, $"archetype diverged for {a.Name}");
            Require(a.SpeciesId == b.SpeciesId, $"species assignment diverged for {a.Name}");
            Require(SpeciesCatalog.TryGet(a.SpeciesId, out _), $"{a.Name} references unknown species {a.SpeciesId}");
            Require(a.DevelopmentStage == b.DevelopmentStage, $"development stage diverged for {a.Name}");
            Require(a.IsSeededAncient == b.IsSeededAncient, $"ancient flag diverged for {a.Name}");
        }
    }

    private static void ValidateSimulationClock()
    {
        var clock = new SimulationClock();
        clock.Restore(42.0);
        clock.SetSpeed(SimulationClock.SpeedLevel.Paused);

        var pausedDelta = clock.Advance(10.0);
        Require(pausedDelta == 0.0, "paused clock accepted simulation time");
        Require(clock.SimulationDays == 42.0, "paused clock changed campaign date");
        Require(clock.EffectiveMultiplier == 0.0, "paused clock reported a nonzero effective multiplier");

        clock.SetSpeed(SimulationClock.SpeedLevel.Maximum);
        var requestedDays = clock.RequestedMultiplier * 1.0;
        var acceptedDays = clock.Advance(1.0);
        Require(acceptedDays > 0.0, "running clock did not advance");
        Require(acceptedDays < requestedDays, "backlog protection failed to throttle an oversized requested step");
        Require(clock.BacklogDays > 0.0, "oversized requested step did not create a bounded scalar backlog");
        Require(double.IsFinite(clock.SimulationDays) && double.IsFinite(clock.BacklogDays), "clock produced non-finite state");
    }

    private static void ValidateSaveRoundTrip()
    {
        WithTemporaryDirectory(directory =>
        {
            var galaxy = CreateValidationGalaxy();
            var service = new CampaignSaveService();
            var path = Path.Combine(directory, "roundtrip.json");
            const double simulationDays = 713.25;

            service.Save(path, galaxy, simulationDays);
            var loaded = service.Load(path);

            Require(CampaignSaveService.CurrentFormatVersion == 8, "expected species-aware save format v8");
            Require(loaded.Galaxy.Seed == galaxy.Seed, "save/load changed galaxy seed");
            Require(loaded.Galaxy.Systems.Count == galaxy.Systems.Count, "save/load changed system count");
            Require(loaded.Galaxy.Civilizations.Count == galaxy.Civilizations.Count, "save/load changed civilization count");
            Require(loaded.Galaxy.ShipyardStates.Count == galaxy.Civilizations.Count, "save/load lost shipyard state");
            Require(
                loaded.Galaxy.Civilizations.Select(civilization => civilization.SpeciesId)
                    .SequenceEqual(galaxy.Civilizations.Select(civilization => civilization.SpeciesId)),
                "save/load changed civilization species identity");
            Require(Math.Abs(loaded.SimulationDays - simulationDays) < 0.000001, "save/load changed simulation date");

            var json = File.ReadAllText(path);
            Require(json.Contains("\"FormatVersion\": 8", StringComparison.Ordinal), "save file did not declare format v8");
            Require(json.Contains("\"SpeciesId\"", StringComparison.Ordinal), "save file did not persist civilization species identity");
            Require(!File.Exists(path + ".tmp"), "atomic save left a temporary file behind");
        });
    }

    private static void ValidateV6Migration()
    {
        WithTemporaryDirectory(directory =>
        {
            var galaxy = CreateValidationGalaxy();
            var service = new CampaignSaveService();
            var currentPath = Path.Combine(directory, "source-v8.json");
            var v6Path = Path.Combine(directory, "legacy-v6.json");

            service.Save(currentPath, galaxy, 365.0);
            var root = JsonNode.Parse(File.ReadAllText(currentPath))?.AsObject()
                ?? throw new InvalidOperationException("could not parse generated v8 save");
            root["FormatVersion"] = 6;
            var galaxyNode = root["Galaxy"]?.AsObject()
                ?? throw new InvalidOperationException("generated save did not contain Galaxy");
            galaxyNode.Remove("ShipyardStates");
            foreach (var civilization in galaxyNode["Civilizations"]?.AsArray()
                         ?? throw new InvalidOperationException("generated save did not contain civilizations"))
            {
                civilization?.AsObject().Remove("SpeciesId");
            }
            File.WriteAllText(v6Path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            var migrated = service.Load(v6Path);
            Require(migrated.Galaxy.ShipyardStates.Count == migrated.Galaxy.Civilizations.Count, "v6 migration did not seed one shipyard state per civilization");
            Require(migrated.Galaxy.ShipyardStates.All(state => state.ActiveDesignId is null), "v6 migration invented active ship builds");
            Require(migrated.Galaxy.ShipyardStates.All(state => state.QueuedBuilds.Count == 0), "v6 migration invented queued ship builds");
            Require(migrated.Galaxy.ShipyardStates.All(state => state.ReservedPopulationMillions == 0.0), "v6 migration invented reserved colonists");
            foreach (var civilization in migrated.Galaxy.Civilizations)
            {
                Require(
                    civilization.SpeciesId == SpeciesAssignmentPolicy.Assign(migrated.Galaxy.Seed, civilization.Id),
                    $"v6 migration did not deterministically assign species for civilization {civilization.Id}");
            }
        });
    }

    private static void ValidateV7SpeciesMigration()
    {
        WithTemporaryDirectory(directory =>
        {
            var galaxy = CreateValidationGalaxy();
            var service = new CampaignSaveService();
            var currentPath = Path.Combine(directory, "source-v8.json");
            var v7Path = Path.Combine(directory, "legacy-v7.json");

            service.Save(currentPath, galaxy, 512.0);
            var root = JsonNode.Parse(File.ReadAllText(currentPath))?.AsObject()
                ?? throw new InvalidOperationException("could not parse generated v8 save");
            root["FormatVersion"] = 7;
            var galaxyNode = root["Galaxy"]?.AsObject()
                ?? throw new InvalidOperationException("generated save did not contain Galaxy");
            foreach (var civilization in galaxyNode["Civilizations"]?.AsArray()
                         ?? throw new InvalidOperationException("generated save did not contain civilizations"))
            {
                civilization?.AsObject().Remove("SpeciesId");
            }
            File.WriteAllText(v7Path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            var migrated = service.Load(v7Path);
            Require(migrated.Galaxy.ShipyardStates.Count == galaxy.ShipyardStates.Count, "v7 to v8 migration lost existing shipyard state");
            foreach (var civilization in migrated.Galaxy.Civilizations)
            {
                Require(
                    civilization.SpeciesId == SpeciesAssignmentPolicy.Assign(migrated.Galaxy.Seed, civilization.Id),
                    $"v7 to v8 migration did not deterministically assign species for civilization {civilization.Id}");
            }
        });
    }

    private static void ValidateBoundedShipyardQueueLoad()
    {
        WithTemporaryDirectory(directory =>
        {
            var galaxy = CreateValidationGalaxy();
            var service = new CampaignSaveService();
            var sourcePath = Path.Combine(directory, "bounded-source.json");
            var oversizedPath = Path.Combine(directory, "oversized-queue.json");

            service.Save(sourcePath, galaxy, 100.0);
            var root = JsonNode.Parse(File.ReadAllText(sourcePath))?.AsObject()
                ?? throw new InvalidOperationException("could not parse generated save");
            var shipyards = root["Galaxy"]?["ShipyardStates"]?.AsArray()
                ?? throw new InvalidOperationException("generated save did not contain shipyards");
            var first = shipyards[0]?.AsObject()
                ?? throw new InvalidOperationException("generated save had no shipyard entries");

            first["ActiveDesignId"] = "warp_scout";
            first["ReservedPopulationMillions"] = -25.0;
            var queue = new JsonArray();
            for (var i = 0; i < ShipyardState.MaxPendingBuilds * 4; i++)
            {
                queue.Add(new JsonObject
                {
                    ["DesignId"] = "warp_scout",
                    ["ReservedPopulationMillions"] = i % 2 == 0 ? -5.0 : 12.0,
                });
            }
            first["QueuedBuilds"] = queue;
            File.WriteAllText(oversizedPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            var loaded = service.Load(oversizedPath);
            var loadedState = loaded.Galaxy.ShipyardStates.First(state => state.CivilizationId == galaxy.ShipyardStates[0].CivilizationId);
            Require(loadedState.PendingBuildCount == ShipyardState.MaxPendingBuilds, "oversized queue was not clamped to the bounded maximum");
            Require(loadedState.QueuedBuilds.Count == ShipyardState.MaxPendingBuilds - 1, "active build was not counted against bounded queue capacity");
            Require(loadedState.ReservedPopulationMillions == 0.0, "negative active reserved population was not sanitized");
            Require(loadedState.QueuedBuilds.All(build => build.ReservedPopulationMillions >= 0.0), "negative queued reserved population was not sanitized");
        });
    }

    private static Game.Simulation.Models.GalaxyState CreateValidationGalaxy() =>
        new GalaxyGenerator().Generate(
            0x4D49_4752_4154_45L,
            new GalaxyGenerationSettings
            {
                SystemCount = 36,
                PreWarpCivilizationCount = 4,
                AncientCivilizationCount = 1,
                Radius = 420.0f,
            });

    private static void WithTemporaryDirectory(Action<string> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "stellar-continuum-validation", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            action(directory);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Persistence;
using Game.Simulation.Diplomacy;
using Game.Simulation.Generation;
using Game.Simulation.Shipbuilding;

namespace Game.Simulation.Validation;

internal static class ShipyardReservedPopulationSaveValidation
{
    [ModuleInitializer]
    internal static void Run()
    {
        ValidateV8RejectsPopulationLosingShipyardState();
        ValidateV9DelegatesTheSamePopulationInvariant();
        ValidateZeroPopulationOverflowCanStillBeBoundedSafely();
        Console.WriteLine("PASS: shipyard reserved population save integrity");
    }

    private static void ValidateV8RejectsPopulationLosingShipyardState()
    {
        WithTemporaryDirectory(directory =>
        {
            var service = new CampaignSaveService();

            ExpectSaveRejected(
                service,
                Path.Combine(directory, "invalid-active-v8.json"),
                galaxy =>
                {
                    var state = galaxy.ShipyardStates[0];
                    var civilization = galaxy.Civilizations.First(c => c.Id == state.CivilizationId);
                    var colonyDesign = ShipDesignRegistry.All.First(design => design.PopulationCostMillions > 0.0);
                    state.ActiveDesignId = "unknown_population_transport";
                    state.ReservedPopulationMillions = colonyDesign.PopulationCostMillions;
                    state.ReservedPopulationSpeciesId = civilization.SpeciesId;
                },
                "v8 save silently accepted reserved population behind an unknown active design");

            ExpectSaveRejected(
                service,
                Path.Combine(directory, "invalid-queue-v8.json"),
                galaxy =>
                {
                    var state = galaxy.ShipyardStates[0];
                    var civilization = galaxy.Civilizations.First(c => c.Id == state.CivilizationId);
                    var colonyDesign = ShipDesignRegistry.All.First(design => design.PopulationCostMillions > 0.0);
                    state.QueuedBuilds.Add(new ShipBuildOrderState
                    {
                        DesignId = "unknown_population_transport",
                        ReservedPopulationMillions = colonyDesign.PopulationCostMillions,
                        ReservedPopulationSpeciesId = civilization.SpeciesId,
                    });
                },
                "v8 save silently accepted reserved population behind an unknown queued design");

            ExpectSaveRejected(
                service,
                Path.Combine(directory, "overflow-v8.json"),
                galaxy => AddPopulationBearingOverflow(galaxy),
                "v8 save silently truncated a population-bearing shipyard overflow entry");
        });
    }

    private static void ValidateV9DelegatesTheSamePopulationInvariant()
    {
        WithTemporaryDirectory(directory =>
        {
            var galaxy = CreateValidationGalaxy();
            AddPopulationBearingOverflow(galaxy);

            var service = new CampaignStatePersistenceService();
            try
            {
                service.Save(
                    Path.Combine(directory, "overflow-v9.json"),
                    galaxy,
                    simulationDays: 42.0,
                    new DiplomacyState());
                throw new InvalidOperationException(
                    "v9 campaign save silently truncated a population-bearing shipyard overflow entry");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("reserved population", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("reserved colonists", StringComparison.OrdinalIgnoreCase))
            {
                // Expected: v9 normalizes/delegates its galaxy payload through the protected v8 serializer.
            }
        });
    }

    private static void ValidateZeroPopulationOverflowCanStillBeBoundedSafely()
    {
        WithTemporaryDirectory(directory =>
        {
            var galaxy = CreateValidationGalaxy();
            var state = galaxy.ShipyardStates[0];
            state.ActiveDesignId = "warp_scout";
            state.ReservedPopulationMillions = 0.0;
            state.ReservedPopulationSpeciesId = null;

            for (var i = 0; i < ShipyardState.MaxPendingBuilds; i++)
            {
                state.QueuedBuilds.Add(new ShipBuildOrderState
                {
                    DesignId = "warp_scout",
                    ReservedPopulationMillions = 0.0,
                    ReservedPopulationSpeciesId = null,
                });
            }

            var path = Path.Combine(directory, "zero-pop-overflow-v8.json");
            var service = new CampaignSaveService();
            service.Save(path, galaxy, 64.0);
            var loaded = service.Load(path);
            var loadedState = loaded.Galaxy.ShipyardStates.First(s => s.CivilizationId == state.CivilizationId);

            Require(
                loadedState.PendingBuildCount == ShipyardState.MaxPendingBuilds,
                "zero-population overflow was not safely reduced to the bounded pending-build maximum");
            Require(
                loadedState.QueuedBuilds.All(build => build.ReservedPopulationMillions == 0.0),
                "zero-population overflow unexpectedly created or retained physical population");
        });
    }

    private static void AddPopulationBearingOverflow(Game.Simulation.Models.GalaxyState galaxy)
    {
        var state = galaxy.ShipyardStates[0];
        var civilization = galaxy.Civilizations.First(c => c.Id == state.CivilizationId);
        var colonyDesign = ShipDesignRegistry.All.First(design => design.PopulationCostMillions > 0.0);

        state.ActiveDesignId = "warp_scout";
        state.ReservedPopulationMillions = 0.0;
        state.ReservedPopulationSpeciesId = null;

        for (var i = 0; i < ShipyardState.MaxPendingBuilds - 1; i++)
        {
            state.QueuedBuilds.Add(new ShipBuildOrderState
            {
                DesignId = "warp_scout",
                ReservedPopulationMillions = 0.0,
                ReservedPopulationSpeciesId = null,
            });
        }

        state.QueuedBuilds.Add(new ShipBuildOrderState
        {
            DesignId = colonyDesign.Id,
            ReservedPopulationMillions = colonyDesign.PopulationCostMillions,
            ReservedPopulationSpeciesId = civilization.SpeciesId,
        });
    }

    private static void ExpectSaveRejected(
        CampaignSaveService service,
        string path,
        Action<Game.Simulation.Models.GalaxyState> mutate,
        string failureMessage)
    {
        var galaxy = CreateValidationGalaxy();
        mutate(galaxy);

        try
        {
            service.Save(path, galaxy, 42.0);
            throw new InvalidOperationException(failureMessage);
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("reserved population", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("reserved colonists", StringComparison.OrdinalIgnoreCase))
        {
            // Expected: do not emit a save that would lose or orphan real population.
        }
    }

    private static Game.Simulation.Models.GalaxyState CreateValidationGalaxy() =>
        new GalaxyGenerator().Generate(
            0x5350_4553_4156_45L,
            new GalaxyGenerationSettings
            {
                SystemCount = 36,
                PreWarpCivilizationCount = 4,
                AncientCivilizationCount = 1,
                Radius = 420.0f,
            });

    private static void WithTemporaryDirectory(Action<string> action)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "stellar-continuum-shipyard-save-validation",
            Guid.NewGuid().ToString("N"));
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

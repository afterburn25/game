using System.Runtime.CompilerServices;
using Game.Persistence;
using Game.Simulation.Diplomacy;
using Game.Simulation.Generation;
using Game.Simulation.Shipbuilding;

namespace Game.Simulation.Validation;

internal static class ShipyardPopulationReservationSerializationValidation
{
    [ModuleInitializer]
    internal static void Run()
    {
        ValidateV8AndV9RejectPopulationLosingShipyardState();
        Console.WriteLine("PASS: v8/v9 persistence refuses population-losing shipyard serialization");
    }

    private static void ValidateV8AndV9RejectPopulationLosingShipyardState()
    {
        WithTemporaryDirectory(directory =>
        {
            ExpectRejectedByBothPersistencePaths(
                directory,
                "invalid-active",
                (state, speciesId, colonyDesign) =>
                {
                    state.ActiveDesignId = "unknown_population_transport";
                    state.ActiveBuildProgress = 10.0;
                    state.ReservedPopulationMillions = colonyDesign.PopulationCostMillions;
                    state.ReservedPopulationSpeciesId = speciesId;
                });

            ExpectRejectedByBothPersistencePaths(
                directory,
                "invalid-queued",
                (state, speciesId, colonyDesign) =>
                {
                    state.ActiveDesignId = null;
                    state.ActiveBuildProgress = 0.0;
                    state.ReservedPopulationMillions = 0.0;
                    state.ReservedPopulationSpeciesId = null;
                    state.QueuedBuilds.Add(new ShipBuildOrderState
                    {
                        DesignId = "unknown_population_transport",
                        ReservedPopulationMillions = colonyDesign.PopulationCostMillions,
                        ReservedPopulationSpeciesId = speciesId,
                    });
                });

            ExpectRejectedByBothPersistencePaths(
                directory,
                "population-overflow",
                (state, speciesId, colonyDesign) =>
                {
                    state.ActiveDesignId = "warp_scout";
                    state.ActiveBuildProgress = 0.0;
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
                        ReservedPopulationSpeciesId = speciesId,
                    });
                });
        });
    }

    private static void ExpectRejectedByBothPersistencePaths(
        string directory,
        string scenario,
        Action<ShipyardState, string, ShipDesignDefinition> mutate)
    {
        ExpectRejected(
            directory,
            scenario + "-v8",
            mutate,
            useCampaignV9: false);
        ExpectRejected(
            directory,
            scenario + "-v9",
            mutate,
            useCampaignV9: true);
    }

    private static void ExpectRejected(
        string directory,
        string scenario,
        Action<ShipyardState, string, ShipDesignDefinition> mutate,
        bool useCampaignV9)
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x5350_5253_4953_544CL,
            new GalaxyGenerationSettings
            {
                SystemCount = 36,
                PreWarpCivilizationCount = 4,
                AncientCivilizationCount = 1,
                Radius = 420.0f,
            });

        var shipyard = galaxy.ShipyardStates[0];
        var civilization = galaxy.Civilizations.First(c => c.Id == shipyard.CivilizationId);
        var colonyDesign = ShipDesignRegistry.All.First(design => design.PopulationCostMillions > 0.0);
        mutate(shipyard, civilization.SpeciesId, colonyDesign);

        var path = Path.Combine(directory, scenario + ".json");
        try
        {
            if (useCampaignV9)
            {
                new CampaignStatePersistenceService().Save(
                    path,
                    galaxy,
                    simulationDays: 42.0,
                    new DiplomacyState());
            }
            else
            {
                new CampaignSaveService().Save(path, galaxy, simulationDays: 42.0);
            }

            throw new InvalidOperationException(
                $"{scenario} persistence silently accepted population-losing shipyard state");
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("reserved population", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("reserved colonists", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("overflow", StringComparison.OrdinalIgnoreCase))
        {
            // Expected: in-memory state is corrupt in a way that persistence would otherwise
            // discard people, so refusing to write the save is the safe behavior.
        }
    }

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
}
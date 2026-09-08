using System.Numerics;
using System.Runtime.CompilerServices;
using Game.Simulation.Generation;
using Game.Simulation.Models;
using Game.Simulation.Species;

internal static class CurrentPopulationBridgeChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x5350_4543_4945_5350L,
            new GalaxyGenerationSettings
            {
                SystemCount = 36,
                PreWarpCivilizationCount = 4,
                AncientCivilizationCount = 1,
                Radius = 420.0f,
            });

        foreach (var colony in galaxy.Colonies)
        {
            var snapshot = CurrentPopulationSpeciesBridge.FromColony(colony);
            Assert(snapshot.SpeciesId == colony.PopulationSpeciesId,
                $"Colony {colony.Id} bridge changed population species identity.");
            AssertNear(snapshot.PopulationMillions, colony.PopulationMillions,
                $"Colony {colony.Id} bridge changed population amount.");
            Assert(SpeciesCatalog.TryGet(snapshot.SpeciesId, out _),
                $"Colony {colony.Id} bridge exposed an unknown species.");

            var unadapted = snapshot.AsUnadaptedCohort();
            Assert(unadapted.SpeciesId == snapshot.SpeciesId,
                "Unadapted cohort conversion changed species identity.");
            AssertNear(unadapted.PopulationMillions, snapshot.PopulationMillions,
                "Unadapted cohort conversion changed population amount.");
            AssertNear(unadapted.Adaptation.Acclimatization, 0.0,
                "Current scalar bridge must not invent acclimatization state.");
        }

        var civilization = galaxy.Civilizations[0];
        var emptyFleet = new FleetState
        {
            Id = 90001,
            CivilizationId = civilization.Id,
            Name = "Empty Population Bridge Test",
            Role = FleetRole.Science,
            Position = Vector2.Zero,
            IsActive = true,
        };
        Assert(CurrentPopulationSpeciesBridge.FromEmbarkedFleet(emptyFleet) is null,
            "A fleet with no embarked population should have no population-species snapshot.");

        var populatedFleet = new FleetState
        {
            Id = 90002,
            CivilizationId = civilization.Id,
            Name = "Populated Population Bridge Test",
            Role = FleetRole.Colony,
            Position = Vector2.Zero,
            IsActive = true,
            EmbarkedPopulationMillions = 250.0,
            EmbarkedPopulationSpeciesId = civilization.SpeciesId,
        };
        var embarked = CurrentPopulationSpeciesBridge.FromEmbarkedFleet(populatedFleet)
            ?? throw new InvalidOperationException("Populated fleet did not produce a population-species snapshot.");
        Assert(embarked.SpeciesId == civilization.SpeciesId,
            "Embarked population bridge changed species identity.");
        AssertNear(embarked.PopulationMillions, 250.0,
            "Embarked population bridge changed passenger amount.");

        var staleIdentityRejected = false;
        try
        {
            CurrentPopulationSpeciesBridge.FromEmbarkedFleet(new FleetState
            {
                Id = 90003,
                CivilizationId = civilization.Id,
                Name = "Stale Identity Test",
                Role = FleetRole.Colony,
                Position = Vector2.Zero,
                IsActive = false,
                EmbarkedPopulationMillions = 0.0,
                EmbarkedPopulationSpeciesId = civilization.SpeciesId,
            });
        }
        catch (InvalidOperationException)
        {
            staleIdentityRejected = true;
        }
        Assert(staleIdentityRejected,
            "Zero embarked population must not retain a stale population species identity.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void AssertNear(double actual, double expected, string message, double epsilon = 1e-9)
    {
        if (Math.Abs(actual - expected) > epsilon)
            throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
    }
}

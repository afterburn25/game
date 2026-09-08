using Game.Persistence;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
using Game.Simulation.Knowledge;

namespace Game.Quality.Validation;

internal static class SurveyPersistenceValidation
{
    public static void Run()
    {
        var galaxy = new GalaxyGenerator().Generate(
            0x5355_5256_4559_5254L,
            new GalaxyGenerationSettings
            {
                SystemCount = 48,
                PreWarpCivilizationCount = 5,
                AncientCivilizationCount = 1,
                Radius = 500.0f,
            });

        var observerId = galaxy.PlayerCivilizationId;
        var homeSystemId = galaxy.Civilizations.Single(civilization => civilization.Id == observerId).HomeSystemId;
        var candidateSystems = galaxy.PlanetaryBodies
            .Where(body => body.SystemId != homeSystemId)
            .Select(body => body.SystemId)
            .Distinct()
            .OrderBy(systemId => systemId)
            .Take(2)
            .ToArray();
        Require(candidateSystems.Length == 2, "validation galaxy did not contain two non-home systems with planetary bodies");

        var partialSystemId = candidateSystems[0];
        var detailedSystemId = candidateSystems[1];
        galaxy.Knowledge.RecordReconnaissance(observerId, partialSystemId, progressFloor: 0.42);
        galaxy.Knowledge.MarkSystemFullySurveyed(observerId, detailedSystemId);

        var beforeReadModel = new ExplorationReadModel().Build(galaxy, observerId);
        var beforePartial = RequireSystem(beforeReadModel, partialSystemId);
        var beforeDetailed = RequireSystem(beforeReadModel, detailedSystemId);

        Require(beforePartial.SurveyLevel == SystemSurveyLevel.PartiallySurveyed, "partial system did not enter partial-survey state before save");
        RequireNear(beforePartial.SurveyProgress, 0.42, "partial survey progress was not recorded before save");
        Require(beforePartial.PlanetaryBodies.Count > 0, "partial survey did not expose a reconnaissance body catalog");
        Require(beforePartial.PlanetaryBodies.All(body => !body.HasDetailedEnvironment),
            "partial survey exposed detailed environment data before save");
        Require(beforeDetailed.SurveyLevel == SystemSurveyLevel.FullySurveyed, "detailed system was not fully surveyed before save");
        Require(beforeDetailed.PlanetaryBodies.Count > 0, "full survey did not expose planetary bodies before save");
        Require(beforeDetailed.PlanetaryBodies.All(body => body.HasDetailedEnvironment),
            "full survey omitted detailed body environment before save");

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"stellar-continuum-survey-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var savePath = Path.Combine(tempDirectory, "campaign.json");
            var saveService = new CampaignSaveService();
            saveService.Save(savePath, galaxy, simulationDays: 37.25);
            var loaded = saveService.Load(savePath);

            RequireNear(loaded.SimulationDays, 37.25, "survey round-trip changed campaign time");
            Require(galaxy.PlanetaryBodies.SequenceEqual(loaded.Galaxy.PlanetaryBodies),
                "deterministic planetary body catalog changed after save/load reconstruction");

            var loadedPartialKnowledge = loaded.Galaxy.Knowledge.GetSystemSurveyKnowledge(observerId)
                .Single(knowledge => knowledge.SystemId == partialSystemId);
            var loadedDetailedKnowledge = loaded.Galaxy.Knowledge.GetSystemSurveyKnowledge(observerId)
                .Single(knowledge => knowledge.SystemId == detailedSystemId);

            Require(loadedPartialKnowledge.Level == SystemSurveyLevel.PartiallySurveyed,
                "partial survey level did not survive save/load");
            RequireNear(loadedPartialKnowledge.Progress, 0.42, "partial survey progress did not survive save/load");
            Require(loadedDetailedKnowledge.Level == SystemSurveyLevel.FullySurveyed,
                "full survey level did not survive save/load");
            RequireNear(loadedDetailedKnowledge.Progress, 1.0, "full survey progress did not survive save/load");

            var afterReadModel = new ExplorationReadModel().Build(loaded.Galaxy, observerId);
            var afterPartial = RequireSystem(afterReadModel, partialSystemId);
            var afterDetailed = RequireSystem(afterReadModel, detailedSystemId);

            RequireEquivalentSystemView(beforePartial, afterPartial, "partial reconnaissance view");
            RequireEquivalentSystemView(beforeDetailed, afterDetailed, "full survey view");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static KnownSystemExplorationView RequireSystem(CivilizationExplorationView view, int systemId) =>
        view.KnownSystems.SingleOrDefault(system => system.SystemId == systemId)
        ?? throw new InvalidOperationException($"observer exploration view omitted system {systemId}");

    private static void RequireEquivalentSystemView(
        KnownSystemExplorationView expected,
        KnownSystemExplorationView actual,
        string label)
    {
        Require(expected.SystemId == actual.SystemId, $"{label} changed system ID after reload");
        Require(expected.CatalogName == actual.CatalogName, $"{label} changed catalog name after reload");
        Require(expected.SurveyLevel == actual.SurveyLevel, $"{label} changed survey level after reload");
        RequireNear(actual.SurveyProgress, expected.SurveyProgress, $"{label} changed survey progress after reload");
        Require(expected.Archetype == actual.Archetype, $"{label} changed archetype visibility after reload");
        Require(expected.HasHabitableWorld == actual.HasHabitableWorld, $"{label} changed habitability visibility after reload");
        Require(expected.HasAnomaly == actual.HasAnomaly, $"{label} changed anomaly visibility after reload");
        Require(expected.HasRareResource == actual.HasRareResource, $"{label} changed rare-resource visibility after reload");
        Require(expected.HasPreWarpCivilization == actual.HasPreWarpCivilization, $"{label} changed activity visibility after reload");
        Require(expected.PlanetaryBodies.SequenceEqual(actual.PlanetaryBodies),
            $"{label} changed observer-visible planetary body facts after reload");
    }

    private static void RequireNear(double actual, double expected, string message, double tolerance = 0.000001)
    {
        if (Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"{message}: expected {expected:0.######}, got {actual:0.######}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

using Game.Presentation.Spatial;
using Game.Simulation.Exploration;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Quality.Validation;

internal static class SpatialPresentationValidation
{
    public static void Run()
    {
        DetectionCannotExposeOrbitalCatalog();
        ReconnaissanceDoesNotInferHiddenBodyClass();
        FullSurveyCanUseLegitimateEnvironmentForVisualClass();
        ProjectionIsDeterministic();
        MoonLayoutPreservesVisibleParentage();
    }

    private static void DetectionCannotExposeOrbitalCatalog()
    {
        var detected = CreateSystem(SystemSurveyLevel.Detected, Array.Empty<PlanetaryBodyExplorationView>());
        var threw = false;
        try
        {
            _ = new SystemSpatialProjection().Build(detected);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Require(threw, "detected-only system produced an orbital spatial projection");
    }

    private static void ReconnaissanceDoesNotInferHiddenBodyClass()
    {
        var body = CreateReconBody(
            bodyId: 3101,
            parentBodyId: null,
            orbitIndex: 2,
            name: "Catalog c",
            kind: PlanetaryBodyKind.Planet,
            radiusEarth: 1.15,
            resourceSignature: true);
        var snapshot = new SystemSpatialProjection().Build(
            CreateSystem(SystemSurveyLevel.PartiallySurveyed, new[] { body }));
        var marker = snapshot.Bodies.Single();

        Require(marker.VisualClass == SystemSpatialBodyVisualClass.UnknownPlanet,
            "reconnaissance-grade body was assigned a detailed environmental visual class");
        Require(!marker.HasDetailedEnvironment,
            "reconnaissance marker claimed detailed environment knowledge");
        Require(marker.PositiveResourceSignature,
            "legitimate positive reconnaissance resource signature was lost");
        Require(!marker.PositiveAnomalySignature && !marker.PositiveActivitySignature,
            "reconnaissance projection invented an unobserved positive signature");
    }

    private static void FullSurveyCanUseLegitimateEnvironmentForVisualClass()
    {
        var body = new PlanetaryBodyExplorationView(
            BodyId: 4101,
            ParentBodyId: null,
            OrbitIndex: 1,
            Name: "Confirmed ocean",
            Kind: PlanetaryBodyKind.Planet,
            RadiusEarth: 1.08,
            HasRareResourceSignature: null,
            HasAnomalySignature: null,
            HasActivitySignature: null,
            MassEarth: 1.02,
            GravityG: 0.98,
            TemperatureKelvin: 289.0,
            PressureKPa: 101.0,
            Atmosphere: PlanetaryAtmosphereRegime.OxygenNitrogen,
            AvailableSolvent: PlanetarySolventRegime.Water,
            RadiationHazard: 0.08,
            IsImmersedEnvironment: true,
            HasSolidSurface: true,
            HasRareResource: false,
            HasAnomaly: false,
            HasPreWarpCivilization: false);

        var snapshot = new SystemSpatialProjection().Build(
            CreateSystem(SystemSurveyLevel.FullySurveyed, new[] { body }, StarArchetype.Standard));
        var marker = snapshot.Bodies.Single();

        Require(marker.HasDetailedEnvironment, "full-survey body lost detailed-environment confidence");
        Require(marker.VisualClass == SystemSpatialBodyVisualClass.Oceanic,
            "full-survey immersed world did not receive its legitimate broad visual class");
        Require(!marker.PositiveResourceSignature && !marker.PositiveAnomalySignature && !marker.PositiveActivitySignature,
            "full-survey projection invented positive signatures from confirmed absence");
    }

    private static void ProjectionIsDeterministic()
    {
        var bodies = new[]
        {
            CreateReconBody(5101, null, 0, "Alpha b", PlanetaryBodyKind.Planet, 0.74),
            CreateReconBody(5102, null, 3, "Alpha e", PlanetaryBodyKind.Planet, 5.10, anomalySignature: true),
        };
        var view = CreateSystem(SystemSurveyLevel.PartiallySurveyed, bodies);
        var projection = new SystemSpatialProjection();
        var first = projection.Build(view);
        var second = projection.Build(view);

        Require(first.Bodies.Count == second.Bodies.Count, "deterministic projection changed marker count");
        for (var i = 0; i < first.Bodies.Count; i++)
        {
            Require(first.Bodies[i] == second.Bodies[i],
                $"deterministic projection moved or changed marker {first.Bodies[i].BodyId}");
        }
    }

    private static void MoonLayoutPreservesVisibleParentage()
    {
        var planet = new PlanetaryBodyExplorationView(
            BodyId: 6101,
            ParentBodyId: null,
            OrbitIndex: 0,
            Name: "Beta b",
            Kind: PlanetaryBodyKind.Planet,
            RadiusEarth: 1.00,
            HasRareResourceSignature: null,
            HasAnomalySignature: null,
            HasActivitySignature: null,
            MassEarth: 1.00,
            GravityG: 1.00,
            TemperatureKelvin: 282.0,
            PressureKPa: 90.0,
            Atmosphere: PlanetaryAtmosphereRegime.Inert,
            AvailableSolvent: PlanetarySolventRegime.None,
            RadiationHazard: 0.10,
            IsImmersedEnvironment: false,
            HasSolidSurface: true,
            HasRareResource: false,
            HasAnomaly: false,
            HasPreWarpCivilization: false);
        var moon = new PlanetaryBodyExplorationView(
            BodyId: 6102,
            ParentBodyId: 6101,
            OrbitIndex: 0,
            Name: "Beta b-1",
            Kind: PlanetaryBodyKind.Moon,
            RadiusEarth: 0.27,
            HasRareResourceSignature: null,
            HasAnomalySignature: null,
            HasActivitySignature: null,
            MassEarth: 0.02,
            GravityG: 0.20,
            TemperatureKelvin: 270.0,
            PressureKPa: 0.0,
            Atmosphere: PlanetaryAtmosphereRegime.Vacuum,
            AvailableSolvent: PlanetarySolventRegime.None,
            RadiationHazard: 0.15,
            IsImmersedEnvironment: false,
            HasSolidSurface: true,
            HasRareResource: false,
            HasAnomaly: false,
            HasPreWarpCivilization: false);

        var snapshot = new SystemSpatialProjection().Build(
            CreateSystem(SystemSurveyLevel.FullySurveyed, new[] { planet, moon }, StarArchetype.Standard));
        var planetMarker = snapshot.Bodies.Single(marker => marker.BodyId == 6101);
        var moonMarker = snapshot.Bodies.Single(marker => marker.BodyId == 6102);
        var dx = moonMarker.OffsetX - planetMarker.OffsetX;
        var dy = moonMarker.OffsetY - planetMarker.OffsetY;
        var actualDistance = Math.Sqrt(dx * dx + dy * dy);

        Require(moonMarker.ParentBodyId == planetMarker.BodyId, "moon projection lost visible parent-body identity");
        Require(Math.Abs(actualDistance - moonMarker.OrbitRadius) < 0.001,
            "moon display geometry was not centered on its visible parent planet");
        Require(moonMarker.VisualClass == SystemSpatialBodyVisualClass.Moon,
            "fully surveyed moon did not retain a moon-specific silhouette class");
    }

    private static KnownSystemExplorationView CreateSystem(
        SystemSurveyLevel level,
        IReadOnlyList<PlanetaryBodyExplorationView> bodies,
        StarArchetype? archetype = null) =>
        new(
            SystemId: 3,
            CatalogName: "Validation",
            SurveyLevel: level,
            SurveyProgress: level switch
            {
                SystemSurveyLevel.FullySurveyed => 1.0,
                SystemSurveyLevel.PartiallySurveyed => 0.45,
                _ => 0.0,
            },
            EstimatedScienceSurveyDays: level >= SystemSurveyLevel.PartiallySurveyed ? 90.0 : null,
            SurveyOperationalHazard: null,
            Archetype: level == SystemSurveyLevel.FullySurveyed ? archetype : null,
            HasHabitableWorld: null,
            HasAnomaly: null,
            HasRareResource: null,
            HasPreWarpCivilization: null,
            PlanetaryBodies: bodies);

    private static PlanetaryBodyExplorationView CreateReconBody(
        int bodyId,
        int? parentBodyId,
        int orbitIndex,
        string name,
        PlanetaryBodyKind kind,
        double radiusEarth,
        bool resourceSignature = false,
        bool anomalySignature = false,
        bool activitySignature = false) =>
        new(
            BodyId: bodyId,
            ParentBodyId: parentBodyId,
            OrbitIndex: orbitIndex,
            Name: name,
            Kind: kind,
            RadiusEarth: radiusEarth,
            HasRareResourceSignature: resourceSignature ? true : null,
            HasAnomalySignature: anomalySignature ? true : null,
            HasActivitySignature: activitySignature ? true : null,
            MassEarth: null,
            GravityG: null,
            TemperatureKelvin: null,
            PressureKPa: null,
            Atmosphere: null,
            AvailableSolvent: null,
            RadiationHazard: null,
            IsImmersedEnvironment: null,
            HasSolidSurface: null,
            HasRareResource: null,
            HasAnomaly: null,
            HasPreWarpCivilization: null);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

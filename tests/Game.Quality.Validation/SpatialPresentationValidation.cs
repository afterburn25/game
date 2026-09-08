using Game.Presentation.Spatial;
using Game.Simulation.Exploration;
using Game.Simulation.Generation;
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
        CelestialHitsDoNotRequestEmptySpaceNavigation();
        LargeCatalogFitsTheViewport();
        EnlargedBodyRimsRemainSelectable();
        ClosestVisibleBodyWinsOverAnOverlappingHitArea();
        OrbitalFieldClearsNavigationChrome();
        OpenViewCannotSurviveAnObserverOrCampaignChange();
        RefreshIsBoundedAndConfidenceChangesAreImmediate();
        ReadModelRefreshSeesVisibleChangesWithoutLeakingHiddenEnvironment();
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

    private static void CelestialHitsDoNotRequestEmptySpaceNavigation()
    {
        var snapshot = new SystemSpatialProjection().Build(CreateSystem(
            SystemSurveyLevel.PartiallySurveyed,
            new[]
            {
                CreateReconBody(6101, null, 0, "Planet", PlanetaryBodyKind.Planet, 1.0),
                CreateReconBody(6102, 6101, 0, "Moon", PlanetaryBodyKind.Moon, 0.27),
            }));
        var viewport = SystemSpatialViewport.Fit(snapshot, 1280.0f, 720.0f);
        Require(viewport.HitsCelestialObject(snapshot, viewport.CenterX, viewport.CenterY),
            "star double-click was treated as empty system space");
        foreach (var marker in snapshot.Bodies)
        {
            Require(viewport.HitsCelestialObject(snapshot,
                    viewport.CenterX + marker.OffsetX * viewport.Scale,
                    viewport.CenterY + marker.OffsetY * viewport.Scale),
                $"body {marker.BodyId} double-click was treated as empty system space");
        }
        Require(!viewport.HitsCelestialObject(snapshot, 24.0f, 360.0f),
            "empty system space was incorrectly blocked from return navigation");
    }

    private static void LargeCatalogFitsTheViewport()
    {
        var snapshot = new SystemSpatialProjection().Build(CreateSystem(
            SystemSurveyLevel.PartiallySurveyed,
            new[] { CreateReconBody(7101, null, 30, "Outer planet", PlanetaryBodyKind.Planet, 1.0) }));
        var viewport = SystemSpatialViewport.Fit(snapshot, 640.0f, 360.0f);
        var availableRadius = Math.Min(640.0f * 0.42f, 360.0f * 0.37f);
        Require(snapshot.DesignRadius * viewport.Scale <= availableRadius + 0.001f,
            "minimum zoom clipped a large system with no way to pan to its outer bodies");
    }

    private static void EnlargedBodyRimsRemainSelectable()
    {
        var snapshot = new SystemSpatialProjection().Build(CreateSystem(
            SystemSurveyLevel.PartiallySurveyed,
            new[] { CreateReconBody(8101, null, 4, "Small distant planet", PlanetaryBodyKind.Planet, 0.08) }));
        var viewport = SystemSpatialViewport.Fit(snapshot, 1024.0f, 720.0f);
        var body = snapshot.Bodies.Single();
        Require(viewport.BodyRadius(body) >= 8.0f, "planet display became an unreadable sub-pixel dot");
        var x = viewport.CenterX + body.OffsetX * viewport.Scale;
        var y = viewport.CenterY + body.OffsetY * viewport.Scale;
        Require(viewport.HitBody(snapshot, x + viewport.BodyRadius(body) - 0.25f, y) == body.BodyId,
            "enlarged visible planet rim could not be selected");
        Require(viewport.HitBody(snapshot, x + viewport.BodyRadius(body) + 4.5f, y) is null,
            "planet intercepted a pointer beyond its visible rim and targeting tolerance");
    }

    private static void ClosestVisibleBodyWinsOverAnOverlappingHitArea()
    {
        var snapshot = new SystemSpatialProjection().Build(CreateSystem(
            SystemSurveyLevel.PartiallySurveyed,
            new[]
            {
                CreateReconBody(8201, null, 0, "Planet", PlanetaryBodyKind.Planet, 1.0),
                CreateReconBody(8202, 8201, 0, "Moon", PlanetaryBodyKind.Moon, 0.27),
            }));
        var viewport = SystemSpatialViewport.Fit(snapshot, 640.0f, 360.0f);
        foreach (var body in snapshot.Bodies)
        {
            Require(viewport.HitBody(snapshot,
                    viewport.CenterX + body.OffsetX * viewport.Scale,
                    viewport.CenterY + body.OffsetY * viewport.Scale) == body.BodyId,
                $"overlapping hit areas hid visible body {body.BodyId} at its own center");
        }
        Require(viewport.HitBody(snapshot, 24.0f, 24.0f) is null,
            "empty navigation space selected an unrelated orbital body");
    }

    private static void OrbitalFieldClearsNavigationChrome()
    {
        var snapshot = new SystemSpatialProjection().Build(CreateSystem(
            SystemSurveyLevel.PartiallySurveyed,
            new[] { CreateReconBody(8301, null, 8, "Outer planet", PlanetaryBodyKind.Planet, 1.0) }));
        foreach (var dimensions in new[] { (Width: 1024.0f, Height: 720.0f), (Width: 1600.0f, Height: 900.0f) })
        {
            var viewport = SystemSpatialViewport.Fit(snapshot, dimensions.Width, dimensions.Height);
            var radius = snapshot.DesignRadius * viewport.Scale;
            Require(viewport.CenterY - radius >= 140.0f,
                "orbital field obscured the system heading");
            Require(viewport.CenterY + radius <= dimensions.Height - 130.0f,
                "orbital field fell underneath the command dock");
            Require(viewport.CenterX - radius >= 104.0f,
                "orbital field fell underneath the navigation rail");
        }
    }

    private static void OpenViewCannotSurviveAnObserverOrCampaignChange()
    {
        var campaign = new object();
        var view = new SystemSpatialViewState();
        view.Open(campaign, 7, 3);
        view.Refreshed(SystemSurveyLevel.FullySurveyed, 1.0);
        Require(view.MatchesContext(campaign, 7, 3), "opened view lost its own context");
        Require(!view.MatchesContext(campaign, 8, 3),
            "a new observer could inherit the prior observer's confirmed visual classes");
        Require(!view.MatchesContext(new object(), 7, 3),
            "a replacement campaign could reuse an unrelated catalog with matching IDs");
        Require(!view.MatchesContext(campaign, 7, 4), "a different selection retained an unrelated view");
        Require(!view.MatchesContext(null, 7, 3), "an unloaded campaign retained its view");
        view.Close();
        Require(!view.IsOpen && !view.MatchesContext(campaign, 7, 3), "closed view retained a valid cache context");
        view.Open(campaign, 8, 3);
        Require(view.NeedsRefresh(SystemSurveyLevel.PartiallySurveyed, 0.0),
            "reopening for another observer failed to request a fresh projection");
    }

    private static void RefreshIsBoundedAndConfidenceChangesAreImmediate()
    {
        var view = new SystemSpatialViewState();
        view.Open(new object(), 7, 3);
        view.Refreshed(SystemSurveyLevel.PartiallySurveyed, 0.45);
        for (var frame = 0; frame < 50; frame++)
            Require(!view.NeedsRefresh(SystemSurveyLevel.PartiallySurveyed, 0.01),
                "ordinary frames rebuilt the galaxy-wide exploration read model");
        Require(view.NeedsRefresh(SystemSurveyLevel.PartiallySurveyed, 0.51),
            "unchanged survey progress indefinitely hid later observer-visible facts");
        view.Refreshed(SystemSurveyLevel.PartiallySurveyed, 0.45);
        Require(view.NeedsRefresh(SystemSurveyLevel.FullySurveyed, 0.0),
            "completed science survey waited for the ordinary refresh timer");
        view.Refreshed(SystemSurveyLevel.FullySurveyed, 1.0);
        Require(view.NeedsRefresh(SystemSurveyLevel.PartiallySurveyed, 0.0),
            "confidence downgrade retained confirmed body classes until a later refresh");
        view.Close();
        Require(!view.NeedsRefresh(SystemSurveyLevel.FullySurveyed, 10.0),
            "a closed view continued to request exploration work");
    }

    private static void ReadModelRefreshSeesVisibleChangesWithoutLeakingHiddenEnvironment()
    {
        var generated = new GalaxyGenerator().Generate(0x5350_4154L, new GalaxyGenerationSettings
        {
            SystemCount = 72,
            PreWarpCivilizationCount = 6,
            AncientCivilizationCount = 1,
            Radius = 620.0f,
        });
        var bodies = generated.PlanetaryBodies.ToArray();
        var galaxy = new GalaxyState
        {
            Seed = generated.Seed,
            Systems = generated.Systems,
            PlanetaryBodies = bodies,
            Civilizations = generated.Civilizations,
            Fleets = generated.Fleets,
            Colonies = generated.Colonies,
            Economies = generated.Economies,
            Technologies = generated.Technologies,
            ConstructionStates = generated.ConstructionStates,
            ShipyardStates = generated.ShipyardStates,
            PlayerCivilizationId = generated.PlayerCivilizationId,
            Knowledge = generated.Knowledge,
        };
        var observerId = galaxy.PlayerCivilizationId;
        var system = galaxy.Systems.First(candidate =>
            galaxy.Knowledge.GetSystemSurveyLevel(observerId, candidate.Id) == SystemSurveyLevel.Unknown);
        galaxy.Knowledge.RecordReconnaissance(observerId, system.Id, 0.45);
        var readModel = new ExplorationReadModel();
        var projection = new SystemSpatialProjection();
        SystemSpatialSnapshot ReadSnapshot() => projection.Build(readModel.Build(galaxy, observerId)
            .KnownSystems.Single(candidate => candidate.SystemId == system.Id));

        var before = ReadSnapshot();
        Require(before.StarArchetype is null, "reconnaissance exposed hidden stellar archetype");
        var bodyIndex = Array.FindIndex(bodies, body => body.SystemId == system.Id && body.Kind == PlanetaryBodyKind.Planet);
        Require(bodyIndex >= 0, "spatial refresh fixture had no planet");
        var body = bodies[bodyIndex];
        bodies[bodyIndex] = body with
        {
            MassEarth = body.MassEarth * 2.0,
            Environment = body.Environment with { TemperatureKelvin = 500.0, IsImmersedEnvironment = true },
        };
        var hiddenChanged = ReadSnapshot();
        Require(before.Bodies.SequenceEqual(hiddenChanged.Bodies),
            "hidden physical changes altered reconnaissance display geometry or class");

        bodies[bodyIndex] = bodies[bodyIndex] with { HasAnomaly = !body.HasAnomaly };
        var visibleChanged = ReadSnapshot();
        Require(before.SurveyLevel == visibleChanged.SurveyLevel && before.SurveyProgress == visibleChanged.SurveyProgress,
            "visible-update fixture unexpectedly advanced survey progress");
        Require(visibleChanged.Bodies.Single(marker => marker.BodyId == body.Id).PositiveAnomalySignature !=
                before.Bodies.Single(marker => marker.BodyId == body.Id).PositiveAnomalySignature,
            "fresh authoritative read failed to update an observer-visible signature at unchanged survey progress");
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

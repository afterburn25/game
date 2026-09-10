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
        SmoothZoomKeepsItsPointerAnchorThroughoutTheTransition();
        PanningCancelsPendingZoomAndResizePreservesIt();
        CameraRejectsInvalidTransformsAndRespectsBounds();
        MovingAndResizedOrbitalTransformsUseTheSameHits();
        CanonicalSolAppearanceDoesNotChangePhysicsOrUnknownWorlds();
        GalaxyArtworkClearsControlsAndKeepsItsSolAnchor();
        OrbitalContextSurvivesTheBeginningOfPlanetApproach();
    }

    private static void CanonicalSolAppearanceDoesNotChangePhysicsOrUnknownWorlds()
    {
        var galaxy = new GalaxyGenerator().Generate(0x534F_4C56L, new GalaxyGenerationSettings
        {
            SystemCount = 72, PreWarpCivilizationCount = 6, AncientCivilizationCount = 1, Radius = 620,
        });
        var known = new ExplorationReadModel().Build(galaxy, galaxy.PlayerCivilizationId)
            .KnownSystems.Single(system => system.CatalogPresetId == "sol-v1");
        var projection = new SystemSpatialProjection();
        var snapshot = projection.Build(known);
        SystemSpatialBodyMarker Named(string name) => snapshot.Bodies.Single(body => body.Label == name);
        Require(Named("Jupiter").VisualClass == SystemSpatialBodyVisualClass.GasGiant &&
                Named("Saturn").VisualClass == SystemSpatialBodyVisualClass.GasGiant &&
                Named("Uranus").VisualClass == SystemSpatialBodyVisualClass.IceGiant &&
                Named("Neptune").VisualClass == SystemSpatialBodyVisualClass.IceGiant,
            "known canonical giant planets received the wrong physical-family illustration");
        Require(Named("Earth").VisualClass == SystemSpatialBodyVisualClass.Rocky && Named("Earth").HasIllustratedOcean &&
                !galaxy.PlanetaryBodies.Single(body => body.Id == SolCatalogPreset.EarthBodyId).Environment.IsImmersedEnvironment,
            "Earth's illustrated water/atmosphere either disappeared or changed terrestrial physics");

        var procedural = projection.Build(known with { CatalogPresetId = null });
        Require(procedural.Bodies.Single(body => body.Label == "Jupiter").VisualClass == SystemSpatialBodyVisualClass.IceGiant &&
                procedural.Bodies.All(body => body.SurfaceKey is null && !body.HasIllustratedOcean),
            "a procedural world's name activated canonical Sol appearance");
        var reconBodies = known.PlanetaryBodies.Select(body => CreateReconBody(body.BodyId, body.ParentBodyId,
            body.OrbitIndex, body.Name, body.Kind, body.RadiusEarth)).ToArray();
        var recon = projection.Build(known with { SurveyLevel = SystemSurveyLevel.PartiallySurveyed, PlanetaryBodies = reconBodies });
        Require(recon.Bodies.All(body => body.VisualClass is SystemSpatialBodyVisualClass.UnknownPlanet or SystemSpatialBodyVisualClass.UnknownMoon &&
                    body.SurfaceKey is null && !body.HasIllustratedOcean),
            "a retained preset/name bypassed reconnaissance-only material privacy");
    }

    private static void GalaxyArtworkClearsControlsAndKeepsItsSolAnchor()
    {
        foreach (var size in new[] { (1024f, 720f), (1280f, 720f), (1600f, 900f), (1920f, 1080f) })
        {
            var frame = SpatialNavigationLayout.FitGalaxyOverview(size.Item1, size.Item2);
            var world = SpatialNavigationLayout.GalaxyWorldFrame;
            var left = frame.CenterX + world.Left * frame.Scale;
            var top = frame.CenterY + world.Top * frame.Scale;
            var width = world.Width * frame.Scale;
            var height = world.Height * frame.Scale;
            Require(left >= 111.99f && left + width <= size.Item1 - 15.99f &&
                    top >= 111.99f && top + height <= size.Item2 - 127.99f,
                "full galaxy artwork overlapped the rail, breadcrumbs or command dock");
            Require(Math.Abs((frame.CenterX - left) / width -
                        (-world.Left / world.Width)) < 0.00001f &&
                    Math.Abs((frame.CenterY - top) / height -
                        (-world.Top / world.Height)) < 0.00001f,
                "fitting the overview moved Sol away from its generated four-arm position");
            Require(frame.Scale >= .29f,
                "100-system overview returned to the oversized tiny-star presentation");
        }
    }

    private static void OrbitalContextSurvivesTheBeginningOfPlanetApproach()
    {
        var camera = new SmoothSpatialCamera();
        camera.Snap(1, 0, 0);
        const float orbitalRadius = 8, focusRadius = 205;
        camera.SetTarget(focusRadius / orbitalRadius, -2500, 1000);
        var prior = 1f;
        var sawFade = false;
        Require(SpatialNavigationLayout.OrbitalContextOpacity(camera.Scale * orbitalRadius, orbitalRadius, focusRadius) == 1,
            "starting planet focus removed the orbital field before the camera moved");
        for (var frame = 0; frame < 120; frame++)
        {
            camera.Advance(1.0 / 60);
            var opacity = SpatialNavigationLayout.OrbitalContextOpacity(camera.Scale * orbitalRadius, orbitalRadius, focusRadius);
            Require(opacity <= prior + 0.00001f, "orbital field flashed back during planet approach");
            if (opacity > 0.01f && opacity < 0.99f) sawFade = true;
            prior = opacity;
        }
        Require(sawFade && prior == 0, "orbital field did not fade away before the planet filled the view");
        camera.SetTarget(1, 0, 0);
        for (var frame = 0; frame < 120; frame++) camera.Advance(1.0 / 60);
        Require(SpatialNavigationLayout.OrbitalContextOpacity(camera.Scale * orbitalRadius, orbitalRadius, focusRadius) == 1,
            "Back failed to restore the full orbital context");
    }

    private static void SmoothZoomKeepsItsPointerAnchorThroughoutTheTransition()
    {
        var camera = new SmoothSpatialCamera();
        camera.Snap(0.55f, 640, 360);
        const float anchorX = 805, anchorY = 294;
        var worldX = (anchorX - camera.OriginX) / camera.Scale;
        var worldY = (anchorY - camera.OriginY) / camera.Scale;
        camera.ZoomAt(1.22f, anchorX, anchorY, 0.025f, 3.2f);
        Require(camera.IsMoving && camera.Scale == 0.55f, "zoom snapped before interpolation");
        for (var frame = 0; frame < 100; frame++)
        {
            camera.Advance(1.0 / 60);
            Require(Math.Abs(camera.OriginX + worldX * camera.Scale - anchorX) < 0.001f &&
                    Math.Abs(camera.OriginY + worldY * camera.Scale - anchorY) < 0.001f,
                "a smoothed zoom moved the world point under its pointer anchor");
        }
        Require(!camera.IsMoving && Math.Abs(camera.Scale - 0.671f) < 0.00001f, "camera failed to settle");
    }

    private static void PanningCancelsPendingZoomAndResizePreservesIt()
    {
        var camera = new SmoothSpatialCamera();
        camera.Snap(1, 640, 360);
        camera.ZoomAt(2, 800, 300, 0.1f, 4);
        camera.Advance(0.016);
        var originalScale = camera.Scale;
        var originalTarget = camera.TargetScale;
        var targetX = camera.TargetOriginX;
        camera.Translate(160, 90);
        Require(camera.Scale == originalScale && camera.TargetScale == originalTarget &&
                camera.TargetOriginX == targetX + 160 && camera.IsMoving,
            "resize discarded the queued camera gesture");
        var panX = camera.OriginX;
        camera.Pan(45, -20);
        Require(camera.Scale == originalScale && camera.OriginX == panX + 45 && !camera.IsMoving,
            "middle drag retained stale zoom inertia or changed scale");
    }

    private static void CameraRejectsInvalidTransformsAndRespectsBounds()
    {
        var camera = new SmoothSpatialCamera();
        camera.ZoomAt(100, 640, 360, 0.025f, 3.2f);
        Require(camera.TargetScale == 3.2f, "maximum zoom bound failed");
        camera.ZoomAt(0.00001f, 640, 360, 0.025f, 3.2f);
        Require(camera.TargetScale == 0.025f, "minimum overview zoom bound failed");
        foreach (var invalid in new[] { 0, -1, float.NaN, float.PositiveInfinity })
        {
            var rejected = false;
            try { camera.SetTarget(invalid, 0, 0); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Require(rejected, "invalid camera scale was accepted");
        }
    }

    private static void MovingAndResizedOrbitalTransformsUseTheSameHits()
    {
        var snapshot = new SystemSpatialProjection().Build(CreateSystem(SystemSurveyLevel.PartiallySurveyed,
            new[] { CreateReconBody(21, null, 0, "Unconfirmed world", PlanetaryBodyKind.Planet, 1) }));
        var body = snapshot.Bodies[0];
        var camera = new SmoothSpatialCamera();
        camera.Snap(0.7f, 678, 366);
        camera.ZoomAt(2.1f, 860, 390, 0.1f, 5);
        for (var frame = 0; frame < 60; frame++)
        {
            if (frame == 25) camera.Translate(160, 90);
            camera.Advance(1.0 / 60);
            var viewport = new SystemSpatialViewport(camera.OriginX, camera.OriginY, camera.Scale);
            var screen = viewport.WorldToScreen(body.OffsetX, body.OffsetY);
            var world = viewport.ScreenToWorld(screen.X, screen.Y);
            Require(Math.Abs(world.X - body.OffsetX) < 0.001 && Math.Abs(world.Y - body.OffsetY) < 0.001,
                "orbital inverse transform failed while moving/resizing");
            Require(viewport.HitBody(snapshot, screen.X, screen.Y) == body.BodyId,
                "rendered world center became unselectable while moving/resizing");
        }
        Require(!body.HasDetailedEnvironment && body.SurfaceKey is null,
            "camera validation fixture exposed hidden environment imagery");
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

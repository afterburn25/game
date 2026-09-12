using System;
using System.Linq;
using System.Threading.Tasks;
using Game.Presentation;
using Game.Presentation.Spatial;
using Game.Simulation.Knowledge;
using Game.Simulation.Generation;
using Game.Simulation.Models;
using Godot;

namespace Game.Tools;

public partial class ScreenshotCapture
{
    /// <summary>Real-input receipt for the bundled nearby-star profile. It deliberately
    /// inspects public map output only, so unknown systems never reveal hidden world facts.</summary>
    private async Task VerifyNearbyCatalogAsync(MainMenuLayer menu)
    {
        await ClickNamedButtonAsync(menu, "ResumeCampaign");
        if (!_main.UiIsPaused) await PressKeyAsync(Key.Space);

        Require(_main.UiOverviewName == "Galaxy" && _main.UiSpatialCatalog.Count == 500 &&
                Math.Abs(_main.UiCatalogVisualCoordinateScale - 14.0f) < .001f,
            "Fresh Player campaign did not use the 500-system local stellar catalogue presentation.");
        Require(_main.UiSpatialCatalog.Any(system => system.SurveyLevel == SystemSurveyLevel.Unknown),
            "Nearby catalogue validation requires unknown stars to retain survey privacy.");

        var window = GetWindow();
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
        {
            window.ContentScaleSize = size;
            window.Size = size;
            await WaitFramesAsync(12);
            await ClickButtonAsync(_dock, "Home");
            await WaitForCameraAsync();
            var home = _main.UiSelectedSystemId;

            await ClickControlAsync(Descendants(_main).OfType<Button>().Single(button => button.Name == "SpatialOverview"));
            await WaitForCameraAsync();
            var artwork = _main.UiGalaxyArtworkScreenRect;
            Require(_main.UiSpatialScale == SpatialPresentationScale.GalaxyOverview && PublicCatalogFits() &&
                    _main.UiGalaxyDeepFieldOpacity >= .44f && _main.UiHasVisibleGalaxyArtwork &&
                    artwork.Size.X > 100 && Math.Abs(artwork.Size.X - artwork.Size.Y) < 1 &&
                    CatalogFitsVisibleGalaxyDisc(artwork),
                $"The complete nearby catalogue did not fit the usable {size.Y}p overview.");
            await SaveViewportAsync($"nearby-{size.Y}-01-overview.png", 0, 0);

            await ClickControlAsync(Descendants(_main).OfType<Button>().Single(button => button.Name == "SpatialRegion"));
            await WaitForCameraAsync();
            Require(_main.UiGalaxyDeepFieldOpacity == 0 && _main.UiRegionalBackdropOpacity > .99f &&
                    _main.UiVisibleRegionalPointCount > 0 && _main.UiCatalogStarCoreRadius(home) <= 4.4f,
                "Regional nearby-star view did not preserve point-star rendering and the local sky.");

            var anchor = StarPoint(home);
            var before = ObserveCamera();
            var displayWorld = (anchor - _main.UiMapOriginScreen) /
                (before.Zoom * _main.UiCatalogVisualCoordinateScale);
            await WheelAsync(true, anchor);
            var after = ObserveCamera();
            var reconstructedAnchor = _main.UiMapOriginScreen + displayWorld *
                (after.Zoom * _main.UiCatalogVisualCoordinateScale);
            Require(reconstructedAnchor.DistanceTo(anchor) < 1 && StarPoint(home).DistanceTo(anchor) < 1,
                "Cursor-anchored nearby-catalog zoom lost alignment with its visual coordinate scale.");

            var dragEnd = StarPoint(home) + new Vector2(38, -24);
            await DragAsync(StarPoint(home), dragEnd, MouseButton.Left);
            await WaitForCameraAsync();
            Require(StarPoint(home).DistanceTo(dragEnd) < 1,
                "Nearby-catalog pan did not preserve the transformed star position.");
            await ClickPositionAsync(StarPoint(home), MouseButton.Left);
            Require(_main.UiSelectedSystemId == home,
                "Nearest known nearby-catalog star was not selected by its rendered point.");

            await ClickButtonAsync(_dock, "Open System");
            await WaitForCameraAsync();
            var canvas = _main.GetNode<SystemSpatialCanvas>("SystemSpatialCanvas");
            Require(_main.UiIsSystemSpatialView && !canvas.IsDetailedFocus && _main.UiVisibleRegionalPointCount == 0,
                "Opening a known nearby-catalog star changed the ordinary system-sun presentation.");
            await ClickButtonAsync(_dock, "Back to Region");
            await WaitForCameraAsync();
            await ClickButtonAsync(_dock, "Home");
            await WaitForCameraAsync();
            var wolf = NearbyStarCatalog.Stars.Select((star, index) => (star, index))
                .Single(item => item.star.Name == "Wolf 359");
            Require(wolf.index == 4 && StarMapDiscGeometry.For(StellarPrimaryClass.MRedDwarf, 192f).CoreRadius >= 27f,
                "Wolf 359 fixture or M-dwarf close-disc geometry changed unexpectedly.");
            await ClickPositionAsync(StarPoint(wolf.index), MouseButton.Left);
            Require(_main.UiSelectedSystemId == wolf.index, "Wolf 359 close-frame fixture could not select its catalogue point.");
            for (var step = 0; _main.UiMapZoom < _main.UiRegionalMaximumZoom - .01f; step++)
            {
                Require(step < 40, "Wolf 359 close-frame fixture did not reach maximum regional zoom.");
                await WheelAsync(true, StarPoint(wolf.index));
                Require(!_main.UiIsSystemSpatialView, "Wolf 359 close-frame fixture unexpectedly entered hidden orbital detail.");
            }
            Require(_main.UiCatalogStarCoreRadius(wolf.index) >= 27f &&
                    _main.UiCatalogStarRadius(wolf.index) >= 64f,
                "Wolf 359 did not render as a materially enlarged red-dwarf disc at close regional zoom.");
            await SaveViewportAsync($"nearby-{size.Y}-02-wolf-359-close.png", 0, 0);
            GD.Print($"STELLAR_CLASS_SIZE_EVIDENCE system=Wolf 359 class=MRedDwarf core={_main.UiCatalogStarCoreRadius(wolf.index):0.0} halo={_main.UiCatalogStarRadius(wolf.index):0.0} zoom={_main.UiMapZoom:0.0}");
            await ClickButtonAsync(_dock, "Home");
            await WaitForCameraAsync();
            await VerifyUnknownEntryPrivacyAsync(home, $"-nearby-{size.Y}");
            await SaveViewportAsync($"nearby-{size.Y}-03-region.png", 0, 0);
        }

        GD.Print("NEARBY_CATALOG_EVIDENCE systems=500 profile=solar-neighborhood visualCoordinateScale=14");
    }

    private bool CatalogFitsVisibleGalaxyDisc(Rect2 artwork)
    {
        var center = artwork.GetCenter();
        var horizontalRadius = artwork.Size.X * .5f * .81818182f;
        var verticalRadius = horizontalRadius * .72f;
        return PublicCatalogIds().All(id =>
        {
            var offset = StarPoint(id) - center;
            return offset.X * offset.X / (horizontalRadius * horizontalRadius) +
                offset.Y * offset.Y / (verticalRadius * verticalRadius) <= .96f;
        });
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Game.Presentation;
using Game.Presentation.Spatial;
using Game.Simulation.Knowledge;
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

        Require(_main.UiOverviewName == "Solar neighborhood" && _main.UiSpatialCatalog.Count == 500 &&
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
            Require(_main.UiSpatialScale == SpatialPresentationScale.GalaxyOverview && PublicCatalogFits() &&
                    _main.UiGalaxyDeepFieldOpacity >= .44f,
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
            await VerifyUnknownEntryPrivacyAsync(home, $"-nearby-{size.Y}");
            await SaveViewportAsync($"nearby-{size.Y}-02-region.png", 0, 0);
        }

        GD.Print("NEARBY_CATALOG_EVIDENCE systems=500 profile=solar-neighborhood visualCoordinateScale=14");
    }
}

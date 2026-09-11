using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Game.Presentation;
using Game.Presentation.Audio.Voice;

namespace Game.Tools;

public partial class ScreenshotCapture
{
    private async Task VerifyResponsiveResolutionsAsync()
    {
        foreach (var size in new[] { new Vector2I(1920, 1080), new Vector2I(2560, 1440), new Vector2I(3840, 2160), new Vector2I(1280, 720) })
        {
            GetWindow().Size = size; await WaitFramesAsync(15); await WaitForCameraAsync();
            var logical = GetViewport().GetVisibleRect().Size;
            var expected = size.Y > 1080 ? new Vector2(1920, 1080) : (Vector2)size;
            Require(logical == expected, $"Responsive viewport is {logical}, expected {expected} at {size}.");
            if (_sidebar.IsDrawerOpen) await CloseDrawerAsync();
            await OpenSectionAsync("research"); await WaitForRefreshAsync();
            VoiceSettings? previousVoiceSettings = null;
            if (size.Y is 720 or 1080)
            {
                previousVoiceSettings = await BeginCaptionLayoutProbeAsync($"responsive-caption-layout-{size.Y}p");
                AssertCaptionDoesNotCover(ActivePanel(), $"caption-safe-area-preserves-drawer-{size.Y}p");
            }
            var grid = Descendants(ActivePanel()).OfType<ResponsiveGrid>().Single(g => g.Name == "ResearchNodes");
            Require(grid.Columns == (size.Y == 720 ? 1 : 2), "Research cards did not reflow at the compact breakpoint.");
            Require(Descendants(ActivePanel()).OfType<Label>().Where(l => l.IsVisibleInTree())
                .All(l => l.GetThemeFontSize("font_size") >= 10), "Compact layout reduced text below its readable minimum.");
            AssertInsideViewport(_drawer, "responsive operations page");
            foreach (var button in Descendants(_main.GetNode("CampaignSidebar/NavigationRail")).OfType<Button>())
                AssertInsideViewport(button, "responsive icon rail");
            var filename = size.Y switch { 1080 => "30-responsive-1080p.png", 1440 => "31-responsive-1440p.png",
                2160 => "32-responsive-4k.png", _ => "33-responsive-720p.png" };
            await SaveViewportAsync(filename, size.X, size.Y);
            if (previousVoiceSettings is not null)
            {
                _main.UiVoice?.Stop(); _main.UiVoice?.ApplySettings(previousVoiceSettings);
            }
            await CloseDrawerAsync();
            await ClickButtonAsync(_dock, "Home"); await WaitForCameraAsync();
            var home = _main.UiSelectedSystemId;
            var other = _main.UiSpatialCatalog.Where(s => s.SystemId != home)
                .First(s => new Rect2(150, 190, logical.X - 500, logical.Y - 340).HasPoint(StarPoint(s.SystemId)) &&
                    StarPoint(s.SystemId).DistanceTo(StarPoint(home)) > 35);
            await ClickPositionAsync(StarPoint(other.SystemId), MouseButton.Left);
            Require(_main.UiSelectedSystemId == other.SystemId, "Scaled mouse hit testing missed the alternate star.");
            await ClickPositionAsync(StarPoint(home), MouseButton.Left);
            Require(_main.UiSelectedSystemId == home, "Scaled mouse hit testing missed the home star.");
        }
        Check(true, "responsive-720p-1080p-1440p-4k-reflow-and-input");
    }
}

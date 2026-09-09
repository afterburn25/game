using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Game.Persistence;
using Game.Presentation;
using Game.Simulation;
using Game.Simulation.Generation;
using Godot;

namespace Game.Tools;

public partial class ScreenshotCapture
{
    private async Task CaptureGalaxySetupAsync(MainMenuLayer menu, ConfirmationDialog dialog)
    {
        var originalSeed = _main.UiGalaxySeed;
        await ClickNamedButtonAsync(menu, "NewPlayerCampaign");
        await ClickNamedButtonAsync(menu, "SandboxCampaignOption");
        var setup = Descendants(menu).OfType<Control>().Single(c => c.Name == "GalaxySetup");
        Require(setup.IsVisibleInTree() && !dialog.Visible && _main.UiIsPaused, "New Player did not open paused galaxy setup.");
        foreach (var control in Descendants(setup).OfType<Control>().Where(c => c.IsVisibleInTree() && c is Button or LineEdit or SpinBox))
            AssertInsideViewport(control, "galaxy setup " + control.Name);
        Check(true, "galaxy-setup-controls-fit-1280x720");
        var seed = Descendants(setup).OfType<LineEdit>().Single(c => c.Name == "GalaxySeed");
        var code = Descendants(setup).OfType<LineEdit>().Single(c => c.Name == "GalaxyCode");
        var generate = Descendants(setup).OfType<Button>().Single(c => c.Name == "GenerateGalaxy");
        var beforeRoll = seed.Text;
        await ClickNamedButtonAsync(setup, "RollGalaxySeed");
        Check(long.TryParse(seed.Text, out _) && seed.Text != beforeRoll, "roll-seed-updates-preview");
        await ReplaceSeedThroughKeyboardAsync(seed, "invalid");
        Check(generate.Disabled && _main.UiGalaxySeed == originalSeed, "invalid-seed-does-not-replace-campaign");
        await ReplaceSeedThroughKeyboardAsync(seed, "20260909");
        Require(!generate.Disabled, "Valid seed did not enable generation.");
        await SaveViewportAsync("galaxy-01-spiral-setup.png");
        foreach (var shape in new[] { GalaxyShape.Elliptical, GalaxyShape.Ring })
        {
            var selector = Descendants(setup).OfType<OptionButton>().Single(c => c.Name == "GalaxyShape");
            await ClickControlAsync(selector);
            var popup = selector.GetPopup();
            Require(popup.Visible, "Shape dropdown did not open.");
            // These three choices have equal-height rows in the actual popup window.
            await ClickPositionAsync((Vector2)popup.Position + new Vector2(popup.Size.X * .5f,
                popup.Size.Y * ((int)shape + .5f) / popup.ItemCount), MouseButton.Left);
            Require(selector.Selected == (int)shape, "Shape could not be changed through its visible dropdown.");
            Check(code.Text.Contains($":{(int)shape}:100:7:2", StringComparison.Ordinal), "shape-updates-recipe-" + shape);
            await SaveViewportAsync("galaxy-0" + ((int)shape + 1) + "-" + shape.ToString().ToLowerInvariant() + "-setup.png");
        }
        const string imported = "SCG1:-9223372036854775808:2:50:2:1";
        await ReplaceSeedThroughKeyboardAsync(code, imported);
        await ClickNamedButtonAsync(setup, "ImportGalaxyCode");
        Require(seed.Text == long.MinValue.ToString() && code.Text == imported, "Import did not restore seed and settings.");
        Check(true, "galaxy-code-restores-all-options");
        await ClickNamedButtonAsync(setup, "GenerateGalaxy");
        Require(dialog.Visible && dialog.DialogText.Contains("50 systems", StringComparison.Ordinal), "New campaign did not show the selected settings in confirmation.");
        await ClickControlAsync(dialog.GetCancelButton());
        Check(_main.UiGalaxySeed == originalSeed && setup.IsVisibleInTree(), "cancel-keeps-current-world");
        await ClickNamedButtonAsync(setup, "GenerateGalaxy");
        await ClickControlAsync(dialog.GetOkButton());
        await WaitForCampaignLoadingAsync();
        _main.UiResumeAtSpeed(SimulationClock.SpeedLevel.Paused);
        var expected = new GalaxySetupOptions { Shape = GalaxyShape.Ring, SystemCount = 50, RivalEmpires = 2, AncientEmpires = 1 };
        Require(_main.UiGalaxySeed == long.MinValue && _main.UiGalaxyOptions == expected && _main.UiSpatialCatalog.Count == 50 && !_main.UiIsDeveloperMode, "Confirmed Player galaxy ignored its recipe.");
        Check(true, "actual-player-campaign-uses-selected-recipe");
        var path = ProjectSettings.GlobalizePath("user://saves/autosave.json");
        var loaded = new CampaignStatePersistenceService().Load(path);
        Require(loaded.Galaxy.GenerationOptions == expected && loaded.Galaxy.Seed == long.MinValue && loaded.Galaxy.Civilizations.Count == 4, "Initial checkpoint lost the selected galaxy.");
        Check(true, "initial-checkpoint-keeps-seed-options-and-empires");
        await ClickNamedButtonAsync(_main.GetNode("SpatialNavigation"), "SpatialOverview");
        await WaitForCameraAsync();
        Require(_main.UiSpatialScaleLabel == "Generated galaxy", "Custom world is incorrectly labeled Milky Way.");
        await SaveViewportAsync("galaxy-04-generated-ring.png");
        await OpenCampaignMenuAsync();
        await ClickNamedButtonAsync(menu, "NewPlayerCampaign");
        await ClickNamedButtonAsync(menu, "SandboxCampaignOption");
        await ClickNamedButtonAsync(setup, "UseCurrentGalaxy");
        Check(code.Text == imported, "current-campaign-recipe-can-be-reused");
        await ClickNamedButtonAsync(setup, "CancelGalaxySetup");
        Require(!setup.IsVisibleInTree() && _main.UiGalaxySeed == long.MinValue, "Closing setup changed the active world.");
        File.WriteAllText(Path.Combine(_outputDirectory, "galaxy-setup-result.json"), JsonSerializer.Serialize(new
        {
            passed = true, checks = _checks, mouseActions = _mouseActions, captures = _captures,
            recipe = imported, scope = "Actual Player menu input with isolated test saves. Does not substitute for full campaign acceptance.",
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

}

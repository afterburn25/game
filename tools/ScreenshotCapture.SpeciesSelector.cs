using System;
using System.Linq;
using System.Threading.Tasks;
using Game.Presentation;
using Game.Simulation.Generation;
using Game.Simulation.Species;
using Godot;

namespace Game.Tools;

public partial class ScreenshotCapture
{
    private async Task VerifySpeciesSelectorAsync(MainMenuLayer menu, ConfirmationDialog dialog)
    {
        await ClickNamedButtonAsync(menu, "NewPlayerCampaign");
        await ClickNamedButtonAsync(menu, "SandboxCampaignOption");
        Require(menu.IsSandboxSetupVisible, "Sandbox setup did not open for species selection.");

        var window = GetWindow();
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
        {
            window.ContentScaleSize = size;
            window.Size = size;
            await WaitFramesAsync(10);
            var portraitChoices = SpeciesCatalog.All.Select(species =>
                Descendants(menu).OfType<Button>().Single(button => button.Name == $"SandboxSpecies_{species.Id}")).ToArray();
            Require(portraitChoices.All(button => button.IsVisibleInTree() && button.Icon is not null),
                $"All playable species portraits were not visible at {size.Y}p.");
            foreach (var choice in portraitChoices)
                AssertInsideViewport(choice, $"species portrait choice {choice.Name} at {size.Y}p");
            AssertInsideViewport(Descendants(menu).OfType<Button>().Single(button => button.Name == "StartConfiguredSandbox"),
                $"generate campaign action at {size.Y}p");

            foreach (var species in SpeciesCatalog.All)
            {
                await ClickNamedButtonAsync(menu, $"SandboxSpecies_{species.Id}");
                var title = Descendants(menu).OfType<Label>().Single(label => label.Name == "SandboxSpeciesTitle");
                var stats = Descendants(menu).OfType<Label>().Single(label => label.Name == "SandboxSpeciesStats");
                var bio = Descendants(menu).OfType<Label>().Single(label => label.Name == "SandboxSpeciesBio");
                var physiology = Descendants(menu).OfType<Label>().Single(label => label.Name == "SandboxSpeciesPhysiology");
                var traits = Descendants(menu).OfType<Label>().Single(label => label.Name == "SandboxSpeciesTraits");
                Require(title.Text == species.DisplayName.ToUpperInvariant() &&
                        stats.Text.Contains(Friendly(species.Environment.PreferredAtmosphere), StringComparison.Ordinal) &&
                        stats.Text.Contains(Friendly(species.Environment.BiologicalSolvent), StringComparison.Ordinal) &&
                        physiology.Text.Contains($"Adult mass {species.Physiology.TypicalAdultMassKg:0} kg", StringComparison.Ordinal) &&
                        physiology.Text.Contains($"Lifespan {species.Physiology.BaselineLifespanYears:0} years", StringComparison.Ordinal) &&
                        bio.Text.Length > 0 && traits.Text.Contains($"{species.Physiology.BaselineMetabolicDemand:0.##}×", StringComparison.Ordinal),
                    $"Selected species details did not use authoritative catalogue facts for {species.DisplayName}.");
            }
            await SaveViewportAsync($"species-selector-{size.Y}.png", 0, 0);
        }

        const string selectedId = SpeciesCatalog.CryogenicHydrocarbonId;
        var selected = SpeciesCatalog.Get(selectedId);
        await ClickNamedButtonAsync(menu, $"SandboxSpecies_{selectedId}");
        await ClickNamedButtonAsync(menu, "StartConfiguredSandbox");
        Require(dialog.Visible && dialog.DialogText.Contains(selected.DisplayName, StringComparison.Ordinal),
            "Nonhuman portrait selection did not carry into the protected campaign confirmation.");
        await ClickControlAsync(dialog.GetOkButton());
        await WaitForCampaignLoadingAsync();
        Require(_main.UiPlayerSpeciesId == selectedId && _main.UiSpatialCatalog.Count == 500,
            "Generated nearby-catalog campaign did not retain the selected nonhuman species and canonical 500-star setup.");
        Require(_main.UiSpatialCatalog.Any(system => system.SystemId == SolCatalogPreset.SystemId),
            "Nonhuman campaign setup removed canonical Sol and its visible Earth reference from the nearby catalogue.");
        Check(true, "species-portrait-selection-and-generation");
    }

    private static string Friendly<T>(T value) where T : Enum
    {
        var source = value.ToString();
        return string.Concat(source.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? " " + character : character.ToString()));
    }
}

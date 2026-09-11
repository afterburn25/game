using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Presentation;
using Godot;

namespace Game.Tools;

/// <summary>
/// Fast, explicitly Developer-labelled native regression for live ship-project cards. This
/// fixture validates presentation reconciliation only and is not ordinary Player progression.
/// Every state change still enters through a visible button and its production callback.
/// </summary>
public partial class ScreenshotCapture
{
    private async Task VerifyProjectCardStabilityAsync(MainMenuLayer menu, ConfirmationDialog dialog)
    {
        Require(_main.UiIsMenuOpen && !_main.UiIsDeveloperMode,
            "Project-card stability must start from the normal campaign menu.");
        await ClickNamedButtonAsync(menu, "OpenDevelopment");
        await ClickNamedButtonAsync(menu, "NewDeveloperCampaign");
        Require(dialog.Visible, "Developer project-card fixture skipped its confirmation.");
        await ClickControlAsync(dialog.GetOkButton());
        await WaitForCampaignLoadingAsync();
        Require(_main.UiIsDeveloperMode && !_main.UiDeveloperToolsUsed,
            "Project-card stability did not enter a fresh, explicitly Developer-labelled campaign.");
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");
        var openingCredits = _main.UiDashboard.Credits;

        await OpenCampaignMenuAsync();
        await ClickNamedButtonAsync(menu, "OpenDevelopment");
        await ClickNamedButtonAsync(menu, "DeveloperTools");
        var tools = _main.GetNode<DeveloperToolsLayer>("DeveloperToolsLayer");
        await ClickNamedButtonAsync(tools, "DeveloperCommand_unlock_technology");
        await ClickNamedButtonAsync(_main, "DeveloperToolsClose");
        await OpenSectionAsync("ships");
        await WaitForRefreshAsync();

        Require(_main.UiIsDeveloperMode && _main.UiDeveloperToolsUsed &&
                Math.Abs(_main.UiDashboard.Credits - openingCredits) < .0001 &&
                openingCredits >= 350 && openingCredits < 530,
            "Developer fixture did not retain its labelled provenance and untouched bounded opening treasury.");
        AssertShipChoiceTreeMatchesSnapshot();
        var colonyBuild = ShipChoiceButton("Choosecolony_ship");
        var scienceBuild = ShipChoiceButton("Choosescience_vessel");
        var scoutBuild = ShipChoiceButton("Choosewarp_scout");
        var choiceGridInstance = Descendants(ActivePanel()).Single(node => node.Name == "OperationChoices").GetInstanceId();
        var stableBuilds = new Dictionary<string, ulong>
        {
            [colonyBuild.Name] = colonyBuild.GetInstanceId(),
            [scienceBuild.Name] = scienceBuild.GetInstanceId(),
            [scoutBuild.Name] = scoutBuild.GetInstanceId(),
        };

        await ClickCurrentShipChoiceAsync("Choosecolony_ship");
        Require(_main.UiShipyardOrders is [{ DesignId: "colony_ship", State: "Active" }] &&
                ShipChoiceButton("Choosecolony_ship").HasFocus(),
            "The first visible colony-build callback did not create exactly one active order or retain focus.");
        AssertStableShipBuilds(stableBuilds, choiceGridInstance);
        AssertShipChoiceTreeMatchesSnapshot();
        var cancelledColonyId = _main.UiShipyardOrders.Single().OrderId;

        await ClickCurrentShipChoiceAsync("Choosescience_vessel");
        Require(_main.UiShipyardOrders.Count == 2 &&
                _main.UiShipyardOrders.Count(order => order.DesignId == "science_vessel") == 1 &&
                ShipChoiceButton("Choosescience_vessel").HasFocus(),
            "The visible science-build callback did not append exactly one queued order or retain focus.");
        AssertStableShipBuilds(stableBuilds, choiceGridInstance);
        AssertShipChoiceTreeMatchesSnapshot();
        var queuedScience = _main.UiShipyardOrders.Single(order => order.DesignId == "science_vessel");
        var scienceCancel = ShipChoiceButton("CancelShipBuild_" + queuedScience.OrderId);
        var scienceCancelInstance = scienceCancel.GetInstanceId();
        var queuedScienceTitle = ChoiceLabel(scienceCancel, "ChoiceTitle").Text;

        Require(_main.UiShipChoices.Single(choice => choice.Id == "colony_ship" && !choice.IsCancellation).CanAfford,
            "The controlled treasury crossed the colony affordability boundary too early.");
        await ClickCurrentShipChoiceAsync("Choosewarp_scout");
        Require(_main.UiShipyardOrders.Count == 3 &&
                _main.UiShipyardOrders.Count(order => order.DesignId == "warp_scout") == 1 &&
                ShipChoiceButton("Choosewarp_scout").HasFocus(),
            "The visible scout-build callback did not append exactly one queued order or retain focus.");
        AssertStableShipBuilds(stableBuilds, choiceGridInstance);
        AssertShipChoiceTreeMatchesSnapshot();
        var unaffordableColony = ShipChoiceButton("Choosecolony_ship");
        Require(unaffordableColony.Disabled &&
                ChoiceLabel(unaffordableColony, "ChoiceAction").Text == "UNAVAILABLE" &&
                unaffordableColony.TooltipText.StartsWith("UNAVAILABLE", StringComparison.Ordinal),
            "The retained colony-build card did not refresh when its live affordability changed.");

        var activeColony = _main.UiShipyardOrders.Single(order => order.OrderId == cancelledColonyId);
        var creditsBeforeCancel = _main.UiDashboard.Credits;
        await ClickCurrentShipChoiceAsync("CancelShipBuild_" + cancelledColonyId);
        Require(_main.UiShipyardOrders.Count == 2 &&
                !_main.UiShipyardOrders.Any(order => order.OrderId == cancelledColonyId) &&
                _main.UiShipyardOrders.Single(order => order.OrderId == queuedScience.OrderId).State == "Active" &&
                Math.Abs(_main.UiDashboard.Credits - creditsBeforeCancel - activeColony.RefundPreview) < .0001,
            "The current cancellation callback did not remove the exact active order, refund it, and promote its queue head.");
        AssertStableShipBuilds(stableBuilds, choiceGridInstance);
        AssertShipChoiceTreeMatchesSnapshot();
        var promotedScienceCancel = ShipChoiceButton("CancelShipBuild_" + queuedScience.OrderId);
        Require(promotedScienceCancel.GetInstanceId() == scienceCancelInstance &&
                queuedScienceTitle.StartsWith("Queued:", StringComparison.Ordinal) &&
                ChoiceLabel(promotedScienceCancel, "ChoiceTitle").Text.StartsWith("Active:", StringComparison.Ordinal),
            "The promoted order replaced its stable cancellation card or retained stale queued text.");
        var focus = GetViewport().GuiGetFocusOwner();
        Require(focus is Button focusedButton && ExpectedShipChoiceNames().Contains(focusedButton.Name),
            "Removing the focused cancellation card did not restore focus to a current ship command.");
        Require(!ShipChoiceButton("Choosecolony_ship").Disabled &&
                ChoiceLabel(ShipChoiceButton("Choosecolony_ship"), "ChoiceAction").Text == "AUTHORIZE / QUEUE  →" &&
                ShipChoiceButton("Choosecolony_ship").TooltipText.StartsWith("AVAILABLE", StringComparison.Ordinal),
            "The retained colony-build card did not refresh after the exact refund restored affordability.");

        await ClickCurrentShipChoiceAsync("Choosecolony_ship");
        Require(_main.UiShipyardOrders.Count == 3 &&
                _main.UiShipyardOrders.Count(order => order.DesignId == "colony_ship") == 1 &&
                _main.UiShipyardOrders.Single(order => order.DesignId == "colony_ship").OrderId != cancelledColonyId &&
                ShipChoiceButton("Choosecolony_ship").HasFocus(),
            "The retained build callback did not create exactly one fresh order after disable, refund, and re-enable.");
        AssertStableShipBuilds(stableBuilds, choiceGridInstance);
        AssertShipChoiceTreeMatchesSnapshot();
        Check(true, "developer-project-card-stability-add-remove-affordability-callbacks");
    }

    private async Task ClickCurrentShipChoiceAsync(string name)
    {
        // Reacquire before any pointer press. Once the press begins, this helper never retries.
        var current = ShipChoiceButton(name);
        await ClickControlAsync(current);
        await WaitForRefreshAsync();
    }

    private Button ShipChoiceButton(string name) => Descendants(ActivePanel()).OfType<Button>()
        .Single(button => button.Name == name);

    private static Label ChoiceLabel(Button button, string name) =>
        button.FindChild(name, recursive: true, owned: false) as Label
        ?? throw new InvalidOperationException($"Ship choice '{button.Name}' has no '{name}' label.");

    private HashSet<StringName> ExpectedShipChoiceNames() => _main.UiShipChoices
        .Select(choice => new StringName(choice.IsCancellation
            ? (choice.CancellationNodePrefix ?? "CancelConstruction_") + choice.Id
            : "Choose" + choice.Id))
        .ToHashSet();

    private void AssertStableShipBuilds(IReadOnlyDictionary<string, ulong> expected, ulong expectedGridInstance)
    {
        Require(Descendants(ActivePanel()).Single(node => node.Name == "OperationChoices").GetInstanceId() == expectedGridInstance,
            "Ship choice reconciliation replaced its operation grid.");
        foreach (var pair in expected)
            Require(ShipChoiceButton(pair.Key).GetInstanceId() == pair.Value,
                $"Stable ship build card '{pair.Key}' was replaced during choice reconciliation.");
    }

    private void AssertShipChoiceTreeMatchesSnapshot()
    {
        var choices = _main.UiShipChoices;
        var expectedNames = ExpectedShipChoiceNames();
        Require(expectedNames.Count == choices.Count,
            "Authoritative ship choices exposed duplicate command identities.");
        var actual = Descendants(ActivePanel()).OfType<Button>()
            .Where(button => expectedNames.Contains(button.Name)).ToArray();
        Require(actual.Length == choices.Count && actual.Select(button => button.Name).Distinct().Count() == actual.Length,
            "Rendered ship choices contain missing or duplicate command nodes.");
        foreach (var choice in choices)
        {
            var name = choice.IsCancellation
                ? (choice.CancellationNodePrefix ?? "CancelConstruction_") + choice.Id
                : "Choose" + choice.Id;
            var button = actual.Single(candidate => candidate.Name == name);
            Require(button.Disabled == !choice.CanAfford &&
                    button.TooltipText.Contains(choice.CostLabel, StringComparison.Ordinal) &&
                    ChoiceLabel(button, "ChoiceTitle").Text == choice.Title &&
                    ChoiceLabel(button, "ChoiceDetail").Text == choice.Detail &&
                    ChoiceLabel(button, "ChoiceCost").Text ==
                        (choice.IsCancellation ? "" : "COST  ") + choice.CostLabel.ToUpperInvariant(),
                $"Rendered ship choice '{name}' is stale relative to its authoritative display snapshot.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPlaybackButton");
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

        await ClickCurrentShipChoiceAsync("Choosewarp_scout", waitForRefresh: false);
        Require(_main.UiShipyardOrders is [{ DesignId: "warp_scout", State: "Active" }] &&
                ShipChoiceButton("Choosewarp_scout").HasFocus(),
            "The first visible scout-build callback did not create exactly one active order or retain focus.");
        var cancelledScoutId = _main.UiShipyardOrders.Single().OrderId;
        var renderedCancellationName = "CancelShipBuild_" + cancelledScoutId;
        Button? renderedCancellation = null;
        var renderedCancellationDeadline = Stopwatch.StartNew();
        while (renderedCancellationDeadline.Elapsed < TimeSpan.FromSeconds(2) && renderedCancellation is null)
        {
            renderedCancellation = Descendants(ActivePanel()).OfType<Button>()
                .SingleOrDefault(button => button.Name == renderedCancellationName);
            if (renderedCancellation is null) await WaitFramesAsync(1);
        }
        Require(renderedCancellation is not null,
            "The active scout cancellation card did not render after its canonical queue insertion.");
        var immediateScience = ShipChoiceButton("Choosescience_vessel");
        Node? shipAncestor = immediateScience.GetParent();
        while (shipAncestor is not null && shipAncestor is not ScrollContainer)
            shipAncestor = shipAncestor.GetParent();
        var shipScroll = shipAncestor as ScrollContainer
            ?? throw new InvalidOperationException("Ship project choices have no scroll viewport.");
        shipScroll.ScrollVertical = 0;
        await WaitFramesAsync(1);
        Require(!Encloses(ScreenRect(shipScroll), ScreenRect(immediateScience)),
            "Focused fixture could not place the next ship command partly outside the real scroll viewport.");
        await RevealControlAsync(immediateScience);
        AssertInsideViewport(immediateScience, "science ship immediately after live queue insertion");
        Require(immediateScience.GetInstanceId() == stableBuilds["Choosescience_vessel"],
            "Immediate offscreen reveal replaced the next ship command after queue insertion.");
        Check(true, "developer-project-card-immediate-post-insertion-reveal-settles-visible");
        await WaitForRefreshAsync();
        AssertStableShipBuilds(stableBuilds, choiceGridInstance);
        AssertShipChoiceTreeMatchesSnapshot();

        await ClickCurrentShipChoiceAsync("Choosecolony_ship");
        Require(_main.UiShipyardOrders.Count == 2 &&
                _main.UiShipyardOrders.Count(order => order.DesignId == "colony_ship") == 1 &&
                ShipChoiceButton("Choosecolony_ship").HasFocus(),
            "The visible colony-build callback did not append exactly one queued order or retain focus.");
        AssertStableShipBuilds(stableBuilds, choiceGridInstance);
        AssertShipChoiceTreeMatchesSnapshot();
        var queuedColony = _main.UiShipyardOrders.Single(order => order.DesignId == "colony_ship");
        var colonyCancel = ShipChoiceButton("CancelShipBuild_" + queuedColony.OrderId);
        var colonyCancelInstance = colonyCancel.GetInstanceId();
        var queuedColonyTitle = ChoiceLabel(colonyCancel, "ChoiceTitle").Text;

        Require(_main.UiShipChoices.Single(choice => choice.Id == "colony_ship" && !choice.IsCancellation).CanAfford,
            "The controlled treasury crossed the colony affordability boundary too early.");
        await ClickCurrentShipChoiceAsync("Choosescience_vessel");
        Require(_main.UiShipyardOrders.Count == 3 &&
                _main.UiShipyardOrders.Count(order => order.DesignId == "science_vessel") == 1 &&
                ShipChoiceButton("Choosescience_vessel").HasFocus(),
            "The visible science-build callback did not append exactly one queued order or retain focus.");
        AssertStableShipBuilds(stableBuilds, choiceGridInstance);
        AssertShipChoiceTreeMatchesSnapshot();
        var unaffordableColony = ShipChoiceButton("Choosecolony_ship");
        Require(unaffordableColony.Disabled &&
                ChoiceLabel(unaffordableColony, "ChoiceAction").Text == "UNAVAILABLE" &&
                unaffordableColony.TooltipText.StartsWith("UNAVAILABLE", StringComparison.Ordinal),
            "The retained colony-build card did not refresh when its live affordability changed.");

        var activeScout = _main.UiShipyardOrders.Single(order => order.OrderId == cancelledScoutId);
        var creditsBeforeCancel = _main.UiDashboard.Credits;
        await ClickCurrentShipChoiceAsync("CancelShipBuild_" + cancelledScoutId);
        Require(_main.UiShipyardOrders.Count == 2 &&
                !_main.UiShipyardOrders.Any(order => order.OrderId == cancelledScoutId) &&
                _main.UiShipyardOrders.Single(order => order.OrderId == queuedColony.OrderId).State == "Active" &&
                Math.Abs(_main.UiDashboard.Credits - creditsBeforeCancel - activeScout.RefundPreview) < .0001,
            "The current cancellation callback did not remove the exact active order, refund it, and promote its queue head.");
        AssertStableShipBuilds(stableBuilds, choiceGridInstance);
        AssertShipChoiceTreeMatchesSnapshot();
        var promotedColonyCancel = ShipChoiceButton("CancelShipBuild_" + queuedColony.OrderId);
        Require(promotedColonyCancel.GetInstanceId() == colonyCancelInstance &&
                queuedColonyTitle.StartsWith("Queued:", StringComparison.Ordinal) &&
                ChoiceLabel(promotedColonyCancel, "ChoiceTitle").Text.StartsWith("Active:", StringComparison.Ordinal),
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
                _main.UiShipyardOrders.Count(order => order.DesignId == "colony_ship") == 2 &&
                _main.UiShipyardOrders.Any(order => order.DesignId == "colony_ship" && order.OrderId != queuedColony.OrderId),
            "The retained build callback did not create exactly one fresh order after disable, refund, and re-enable.");
        AssertStableShipBuilds(stableBuilds, choiceGridInstance);
        AssertShipChoiceTreeMatchesSnapshot();
        Check(true, "developer-project-card-stability-add-remove-affordability-callbacks");
    }

    private async Task ClickCurrentShipChoiceAsync(string name, bool waitForRefresh = true)
    {
        // Reacquire before any pointer press. Once the press begins, this helper never retries.
        var current = ShipChoiceButton(name);
        await ClickControlAsync(current);
        if (waitForRefresh) await WaitForRefreshAsync();
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
            AssertProjectChoiceLayout(button, choice);
        }
    }

    private static void AssertProjectChoiceLayout(Button button, UiOperationChoice choice)
    {
        var information = Descendants(button).OfType<PanelContainer>()
            .Single(panel => panel.Name == "ChoiceInformation");
        var buttonBounds = button.GetGlobalRect();
        var informationBounds = information.GetGlobalRect();
        var costBounds = Descendants(button).OfType<PanelContainer>()
            .Single(panel => panel.Name == "Cost_" + choice.Id).GetGlobalRect();
        var actionBounds = ChoiceLabel(button, "ChoiceAction").GetGlobalRect();
        var titleBounds = ChoiceLabel(button, "ChoiceTitle").GetGlobalRect();
        var detailBounds = ChoiceLabel(button, "ChoiceDetail").GetGlobalRect();
        Require(buttonBounds.Encloses(informationBounds) && buttonBounds.Encloses(costBounds) &&
                buttonBounds.Encloses(titleBounds) && buttonBounds.Encloses(detailBounds) && buttonBounds.Encloses(actionBounds),
            $"Project choice '{button.Name}' lets information, title, detail, cost, or action escape its mouse button.");
        if (choice.ArtworkPath is not null)
        {
            var artwork = Descendants(button).OfType<TextureRect>()
                .Single(texture => texture.Name == "Artwork_" + choice.Id);
            Require(artwork.GetGlobalRect().End.Y <= informationBounds.Position.Y,
                $"Project choice '{button.Name}' overlaps its artwork preview and information panel.");
        }
    }
}

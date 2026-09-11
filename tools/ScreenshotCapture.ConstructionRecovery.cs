using System;
using System.Linq;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Game.Presentation;
using Game.Simulation.Models;
using Godot;

namespace Game.Tools;

public partial class ScreenshotCapture
{
    private async Task VerifyFreshConstructionRecoveryAsync(MainMenuLayer menu, ConfirmationDialog dialog)
    {
        await OpenCampaignMenuAsync();
        await ClickNamedButtonAsync(menu, "NewPlayerCampaign");
        await ClickNamedButtonAsync(menu, "SandboxCampaignOption");
        await ClickNamedButtonAsync(menu, "StartConfiguredSandbox");
        await ClickControlAsync(dialog.GetOkButton());
        await WaitForCampaignLoadingAsync();
        Require(!_main.UiIsDeveloperMode && !_main.UiIsMenuOpen,
            "Recovery journey must start an ordinary Player Sandbox through its confirmation.");
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");
        await OpenSectionAsync("industry");
        await ClickNamedButtonAsync(ActivePanel(), "Chooseresearch_network");
        await WaitForRefreshAsync();
        await VerifyConstructionRecoveryAsync();
        await VerifyIndustryPriorityPersistenceAsync();
    }

    private async Task VerifyIndustryPriorityPersistenceAsync()
    {
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");
        await OpenSectionAsync("economy");
        var panel = ActivePanel();
        var controls = panel.GetNode<Control>("IndustryPriorityControls");
        var status = panel.GetNode<Label>("IndustryPriorityStatus");
        foreach (var priority in new[]
                 {
                     IndustryPriority.Balanced,
                     IndustryPriority.InfrastructureFirst,
                     IndustryPriority.ShipbuildingFirst,
                 })
        {
            var button = controls.GetNode<Button>("IndustryPriority_" + priority);
            await ClickControlAsync(button);
            await WaitForRefreshAsync();
            Require(_main.UiIndustryPriority.Priority == priority,
                $"Industry priority command did not select {priority}.");
            Require(button.ButtonPressed && status.Text.Contains(priority.ToString(), StringComparison.Ordinal),
                $"Industry priority presentation did not reflect {priority}.");
            Check(true, "industry-priority-pointer-" + priority);
        }

        var selected = _main.UiIndustryPriority.Priority;
        await OpenSectionAsync("menu");
        await ClickButtonAsync(ActivePanel(), "Save");
        var path = ProjectSettings.GlobalizePath("user://saves/autosave.json");
        Require(File.Exists(path), "Industry priority save was not written through the Player menu.");
        var saved = FindConstructionGalaxy(JsonNode.Parse(File.ReadAllText(path)))
            ?? throw new InvalidOperationException("Saved campaign has no galaxy payload.");
        var player = saved["PlayerCivilizationId"]!.GetValue<int>();
        var economy = saved["Economies"]!.AsArray()
            .Single(item => item!["CivilizationId"]!.GetValue<int>() == player)!;
        Check(economy["IndustryPriority"]?.GetValue<string>() == selected.ToString(),
            "industry-priority-save-persisted");

        await OpenCampaignMenuAsync();
        await ClickNamedButtonAsync(_main.GetNode("MainMenuLayer"), "ModePlayer");
        await WaitForCampaignLoadingAsync();
        Require(!_main.UiIsMenuOpen && _main.UiIndustryPriority.Priority == selected,
            "Player reload did not restore the selected industry priority.");
        await OpenSectionAsync("economy");
        var restoredStatus = ActivePanel().GetNode<Label>("IndustryPriorityStatus");
        Check(restoredStatus.Text.Contains(selected.ToString(), StringComparison.Ordinal),
            "industry-priority-load-reflected-in-economy-panel");
    }

    // Orders and recovery use the same visible controls as a player. No resources,
    // research, progress or private state are injected to make the journey pass.
    private async Task VerifyConstructionRecoveryAsync(bool captureEvidence = false)
    {
        var resume = !_main.UiIsPaused;
        if (resume) await ClickNamedButtonAsync(_main, "SimulationPause");
        if (!_sidebar.IsDrawerOpen || _sidebar.ActiveSection != "industry") await OpenSectionAsync("industry");
        Require(_main.UiConstructionOrders is [{ Id: "research_network", State: "Active" }],
            "Queue acceptance requires the ordinary opening research-network project.");
        var original = _main.UiConstructionOrders.Single();
        var credits = _main.UiDashboard.Credits;
        var materials = _main.UiDashboard.Industry;
        await ClickNamedButtonAsync(ActivePanel(), "Chooseindustrial_automation");
        await WaitForRefreshAsync();
        var queued = _main.UiConstructionOrders.Single(order => order.Id == "industrial_automation");
        Check(queued.State == "Queued" && queued.Progress == 0 && queued.AuthorizationCredits > 0 &&
            Math.Abs(_main.UiDashboard.Credits - (credits - queued.AuthorizationCredits)) < .0001 &&
            _main.UiDashboard.Industry == materials &&
            _main.UiConstructionOrders.First() == original,
            "construction-queue-debits-once-without-free-work");
        var duplicate = Descendants(ActivePanel()).OfType<Button>().Single(button => button.Name == "Chooseindustrial_automation");
        Check(duplicate.Disabled, "construction-queue-duplicate-disabled");
        Check(_main.UiConstructionOrders.Select(order => order.Id).SequenceEqual(
                new[] { "research_network", "industrial_automation" }),
            "construction-queue-order-visible");
        foreach (var order in _main.UiConstructionOrders)
        {
            var cancel = Descendants(ActivePanel()).OfType<Button>().Single(button => button.Name == "CancelConstruction_" + order.Id);
            await RevealControlAsync(cancel);
            AssertInsideViewport(cancel, "construction cancellation " + order.Id);
            Require(cancel.TooltipText.Contains(_main.UiFormatMoney(order.RefundPreview), StringComparison.Ordinal),
                "Cancellation control must disclose the exact refund before the click.");
        }
        Check(true, "construction-refund-controls-fit-720p");
        if (captureEvidence) await SaveViewportAsync("construction-queue-720p.png");

        await OpenSectionAsync("menu");
        await ClickButtonAsync(ActivePanel(), "Save");
        var path = ProjectSettings.GlobalizePath(_main.UiIsDeveloperMode
            ? "user://saves/developer-autosave.json" : "user://saves/autosave.json");
        var saved = FindConstructionGalaxy(JsonNode.Parse(File.ReadAllText(path)))
            ?? throw new InvalidOperationException("Saved campaign has no construction state.");
        var player = saved["PlayerCivilizationId"]!.GetValue<int>();
        var state = saved["ConstructionStates"]!.AsArray().Single(item => item!["CivilizationId"]!.GetValue<int>() == player)!;
        Check(state["QueuedProjects"]!.AsArray().Select(item => item!["ProjectId"]!.GetValue<string>())
                .SequenceEqual(new[] { "industrial_automation" }) &&
            Math.Abs(state["ActiveProjectAuthorizationCredits"]!.GetValue<double>() - original.AuthorizationCredits) < .0001,
            "construction-queue-saved-through-player-control");

        await OpenSectionAsync("industry");
        var beforeCancel = _main.UiDashboard.Credits;
        await ClickNamedButtonAsync(ActivePanel(), "CancelConstruction_industrial_automation");
        await WaitForRefreshAsync();
        Check(Math.Abs(_main.UiDashboard.Credits - beforeCancel - queued.RefundPreview) < .0001 &&
            _main.UiDashboard.Industry == materials &&
            _main.UiConstructionOrders.Select(order => order.Id).SequenceEqual(new[] { "research_network" }),
            "construction-queued-cancellation-refunds-authorization-only");
        await ClickNamedButtonAsync(ActivePanel(), "Chooseindustrial_automation");
        await WaitForRefreshAsync();
        beforeCancel = _main.UiDashboard.Credits;
        await ClickNamedButtonAsync(ActivePanel(), "CancelConstruction_research_network");
        await WaitForRefreshAsync();
        Check(Math.Abs(_main.UiDashboard.Credits - beforeCancel - original.RefundPreview) < .0001 &&
            _main.UiDashboard.Industry == materials &&
            _main.UiConstructionOrders is [{ Id: "industrial_automation", State: "Active", Progress: 0 }],
            "construction-active-cancellation-promotes-without-material-refund");
        await ClickNamedButtonAsync(ActivePanel(), "CancelConstruction_industrial_automation");
        await WaitForRefreshAsync();
        await ClickNamedButtonAsync(ActivePanel(), "Chooseresearch_network");
        await WaitForRefreshAsync();
        Require(_main.UiConstructionOrders is [{ Id: "research_network", State: "Active", Progress: 0 }],
            "Recovery did not allow the player to authorize the cancelled project again.");
        if (resume) await ClickNamedButtonAsync(_main, "SimulationPause");
    }

    private static JsonObject? FindConstructionGalaxy(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            if (obj.ContainsKey("ConstructionStates") && obj.ContainsKey("PlayerCivilizationId")) return obj;
            foreach (var pair in obj)
                if (FindConstructionGalaxy(pair.Value) is { } found) return found;
        }
        else if (node is JsonArray array)
            foreach (var child in array)
                if (FindConstructionGalaxy(child) is { } found) return found;
        return null;
    }
}

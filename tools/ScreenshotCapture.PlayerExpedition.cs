using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Game.Campaign;
using Game.Presentation;
using Game.Simulation.Models;
using Godot;

namespace Game.Tools;

/// <summary>
/// Focused native evidence for the complete ordinary-player opening. This deliberately uses no
/// Developer actions: every order below is a visible control click or a map pointer command.
/// </summary>
public partial class ScreenshotCapture
{
    private static readonly string[] OpeningConstruction =
    {
        "research_network", "industrial_automation", "orbital_launch_complex", "orbital_shipyard", "warp_test_facility",
    };

    private async Task VerifyPlayerExpeditionAsync(MainMenuLayer menu, ConfirmationDialog dialog)
    {
        await StartFreshOrdinarySandboxAsync(menu, dialog);
        await SelectMaximumPlayerSpeedAsync();
        Require(_main.UiCurrentSpeed == Game.Simulation.SimulationClock.SpeedLevel.Maximum && !_main.UiIsDeveloperMode,
            "Ordinary Player expedition did not begin at the visible 8× speed setting.");

        await SaveViewportAsync("player-expedition-01-opening-research.png");
        await ProgressToWarpAndShipOrdersAsync();
        await SaveViewportAsync("player-expedition-02-first-warp-shipyard.png");
        await CompleteSurveyAndSettlementAsync();
        await SaveViewportAsync("player-expedition-03-settlement-authorized.png");
        await VerifyPlayerExpeditionSaveReloadAsync(menu);
        await SaveViewportAsync("player-expedition-04-reloaded-colony.png");
        WritePlayerExpeditionEvidenceManifest();
    }

    private async Task StartFreshOrdinarySandboxAsync(MainMenuLayer menu, ConfirmationDialog dialog)
    {
        Require(_main.UiIsMenuOpen && !_main.UiIsDeveloperMode, "Focused Player journey requires the normal startup menu.");
        await ClickNamedButtonAsync(menu, "NewPlayerCampaign");
        await ClickNamedButtonAsync(menu, "SandboxCampaignOption");
        var seed = Descendants(menu).OfType<LineEdit>().Single(input => input.Name == "SandboxSeed");
        await ReplaceSeedThroughKeyboardAsync(seed, "20260908");
        await ClickNamedButtonAsync(menu, "StartConfiguredSandbox");
        Require(dialog.Visible, "Fresh Player Sandbox skipped its confirmation.");
        await ClickControlAsync(dialog.GetOkButton());
        await WaitForCampaignLoadingAsync();
        Require(!_main.UiIsMenuOpen && !_main.UiIsDeveloperMode && !_main.UiDeveloperToolsUsed,
            "Fresh Sandbox did not enter ordinary Player mode.");
        Check(true, "player-expedition-fresh-ordinary-sandbox");
    }

    private async Task SelectMaximumPlayerSpeedAsync()
    {
        var selector = Descendants(_main.GetNode("PlayerControls")).OfType<OptionButton>()
            .Single(control => control.Name == "SimulationSpeed");
        await ClickPositionAsync(ScreenRect(selector).GetCenter(), MouseButton.Left);
        await WaitFramesAsync(2);
        var popup = selector.GetPopup();
        Require(popup.Visible, "The visible simulation speed selector did not open.");
        for (var step = 0; step < selector.ItemCount; step++) await PressKeyAsync(Key.Up);
        for (var step = 0; step < 3; step++) await PressKeyAsync(Key.Down);
        await PressKeyAsync(Key.Enter);
        Require(selector.Selected == 3, "Visible speed selector did not select the ordinary Player 8× item.");
        await WaitForRefreshAsync();
    }

    private async Task ProgressToWarpAndShipOrdersAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var startedConstruction = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var startedShips = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var researchIndex = 0;
        while (stopwatch.Elapsed < TimeSpan.FromMinutes(15))
        {
            if (!_sidebar.IsDrawerOpen || _sidebar.ActiveSection != "research") await OpenSectionAsync("research");
            while (researchIndex < EarlyCampaignResearchPlan.WarpCapabilityPath.Count &&
                   _main.UiResearchHorizon.Any(node => node.Id == EarlyCampaignResearchPlan.WarpCapabilityPath[researchIndex] && node.State == "MATURE"))
                researchIndex++;
            if (researchIndex < EarlyCampaignResearchPlan.WarpCapabilityPath.Count)
            {
                var id = EarlyCampaignResearchPlan.WarpCapabilityPath[researchIndex];
                var node = _main.UiResearchHorizon.SingleOrDefault(value => value.Id == id);
                if (node is { CanStart: true })
                {
                    await ClickNamedButtonAsync(ActivePanel(), "ResearchNode_" + id);
                    Check(true, "player-expedition-research-" + id);
                    if (researchIndex == 0) await VerifyOpeningResearchControlsAsync(id);
                }
            }

            if (!_sidebar.IsDrawerOpen || _sidebar.ActiveSection != "industry") await OpenSectionAsync("industry");
            if (_main.UiConstructionOrders.Count == 0)
            {
                var next = OpeningConstruction.FirstOrDefault(id => !startedConstruction.Contains(id) &&
                    Descendants(ActivePanel()).OfType<Button>().Any(button => button.Name == "Choose" + id && !button.Disabled));
                if (next is not null)
                {
                    await ClickNamedButtonAsync(ActivePanel(), "Choose" + next);
                    startedConstruction.Add(next);
                    Check(true, "player-expedition-construction-" + next);
                }
            }

            if (!_sidebar.IsDrawerOpen || _sidebar.ActiveSection != "ships") await OpenSectionAsync("ships");
            foreach (var id in new[] { "warp_scout", "science_vessel", "colony_ship" })
            {
                if (startedShips.Contains(id) || _main.UiOwnedFleets.Any(fleet => fleet.DesignId == id) ||
                    _main.UiShipyardOrders.Any(order => order.DesignId == id))
                    continue;
                var choice = _main.UiShipChoices.SingleOrDefault(value => value.Id == id && !value.IsCancellation);
                if (choice is { CanAfford: true })
                {
                    await ClickNamedButtonAsync(ActivePanel(), "Choose" + id);
                    startedShips.Add(id);
                    Check(true, "player-expedition-build-" + id);
                }
            }

            if (new[] { FleetRole.Scout, FleetRole.Science, FleetRole.Colony }
                .All(role => _main.UiOwnedFleets.Any(fleet => fleet.Role == role)))
            {
                Check(_main.UiResearchHorizon.Any(node => node.Id == "prototype_warp_drive" && node.State == "MATURE"),
                    "player-expedition-first-warp-completed");
                return;
            }
            await WaitForRefreshAsync();
        }
        throw new InvalidOperationException("Ordinary Player opening did not reach physical scout, science, and colony ships within 15 active minutes.");
    }

    private async Task CompleteSurveyAndSettlementAsync()
    {
        if (_sidebar.IsDrawerOpen) await CloseDrawerAsync();
        await ClickButtonAsync(_dock, "Home");
        var scout = _main.UiOwnedFleets.Single(fleet => fleet.Role == FleetRole.Scout);
        var scoutTarget = FindReachablePublicStar(scout, requireUnknown: true);
        await SelectFleetByMarkerAsync(scout.FleetId);
        await ClickPositionAsync(scoutTarget.Point, MouseButton.Right);
        Check(_main.UiSelectedFleetId == scout.FleetId && _main.UiPointerCommandRevision > 0,
            "player-expedition-scout-right-click-order");
        await WaitForSurveyLevelAsync(scoutTarget.SystemId, "PartiallySurveyed", "scout reconnaissance");

        await ClickButtonAsync(_dock, "Home");
        var science = _main.UiOwnedFleets.Single(fleet => fleet.Role == FleetRole.Science);
        await SelectFleetByMarkerAsync(science.FleetId);
        await ClickPositionAsync(scoutTarget.Point, MouseButton.Right);
        Check(_main.UiSelectedFleetId == science.FleetId, "player-expedition-science-right-click-order");
        await WaitForSurveyLevelAsync(scoutTarget.SystemId, "FullySurveyed", "science detailed survey");

        await OpenSectionAsync("colonies");
        var opportunity = _main.GetUiColonyOpportunityState(0, 0);
        Require(opportunity is { FleetId: not null, SystemId: not null, PlanetaryBodyId: not null, CanOrder: true },
            "The ordinary colonies panel did not expose a funded, fully surveyed settlement opportunity.");
        var colonyFleetId = opportunity.FleetId!.Value;
        var settlementSystemId = opportunity.SystemId!.Value;
        var settlementBodyId = opportunity.PlanetaryBodyId!.Value;
        var colonyCountBefore = _main.UiOwnedColonies.Length;
        await ClickControlAsync(RequireButton(ActivePanel(), "Select ship on map"));
        Require(_main.UiSelectedFleetId == colonyFleetId, "Colony Sites did not select its real populated colony ship.");
        var settlementPoint = _main.UiGetCatalogScreenPosition(settlementSystemId)
            ?? throw new InvalidOperationException("Selected colony opportunity does not have a visible public map position.");
        await ClickPositionAsync(settlementPoint, MouseButton.Right);
        Check(_main.UiSelectedFleetId == colonyFleetId, "player-expedition-colony-transit-right-click-order");
        await WaitForFleetAtSelectedSystemAsync(colonyFleetId, settlementSystemId);
        await ClickPositionAsync(settlementPoint, MouseButton.Left);
        await ClickButtonAsync(_dock, "Open System");
        await WaitForCameraAsync();
        var localColonyShip = Descendants(_main).OfType<Button>()
            .Single(button => button.Name == "SystemFleet" + colonyFleetId);
        await ClickControlAsync(localColonyShip);
        Require(_main.UiSelectedFleetId == colonyFleetId,
            "The visible local system fleet control did not select the arrived colony ship.");
        var bodyPoint = _main.UiGetBodyScreenPosition(settlementBodyId)
            ?? throw new InvalidOperationException("Surveyed settlement world has no visible system-view body marker.");
        await ClickPositionAsync(bodyPoint, MouseButton.Right);
        Check(_main.UiPointerCommandRevision > 0 && _main.UiStatusMessage.Contains("settlement", StringComparison.OrdinalIgnoreCase),
            "player-expedition-colony-body-right-click-authorizes-settlement");
        await WaitForColonyAsync(colonyCountBefore, settlementBodyId);
        Check(true, "player-expedition-settlement-timed-and-complete");
    }

    private async Task VerifyOpeningResearchControlsAsync(string researchId)
    {
        var control = Descendants(ActivePanel()).OfType<Button>().Single(button => button.Name == "ResearchNode_" + researchId);
        var instance = control.GetInstanceId();
        var progressBefore = _main.UiResearchHorizon.Single(node => node.Id == researchId).Progress;
        await WaitForRefreshAsync();
        var refreshed = Descendants(ActivePanel()).OfType<Button>().Single(button => button.Name == "ResearchNode_" + researchId);
        var progressAfter = _main.UiResearchHorizon.Single(node => node.Id == researchId).Progress;
        Check(refreshed.GetInstanceId() == instance && refreshed.HasFocus() && progressAfter > progressBefore &&
              refreshed.TooltipText.Contains(_main.UiResearchHorizon.Single(node => node.Id == researchId).Detail, StringComparison.Ordinal),
            "player-expedition-active-research-control-retains-focus-and-refreshes-detail");

        await ClickControlAsync(refreshed);
        await WaitForRefreshAsync();
        var paused = _main.UiResearchHorizon.Single(node => node.Id == researchId);
        var pausedProgress = paused.Progress;
        Check(paused.CanResume && !paused.CanPause && Math.Abs(_main.UiCreditFlow.ResearchOperationsPerDay) < .000001,
            "player-expedition-research-pointer-pause-halts-canonical-spend");
        await WaitForRefreshAsync();
        Check(Math.Abs(_main.UiResearchHorizon.Single(node => node.Id == researchId).Progress - pausedProgress) < .000001,
            "player-expedition-paused-research-progress-remains-stable");

        var resume = Descendants(ActivePanel()).OfType<Button>().Single(button => button.Name == "ResearchNode_" + researchId);
        await ClickControlAsync(resume);
        await WaitForRefreshAsync();
        var resumed = _main.UiResearchHorizon.Single(node => node.Id == researchId);
        Check(resumed.CanPause && !resumed.CanResume && Math.Abs(_main.UiCreditFlow.ResearchOperationsPerDay) > .000001 &&
              resumed.Progress > pausedProgress,
            "player-expedition-research-pointer-resume-restores-canonical-progress-and-spend");
    }

    private async Task SelectFleetByMarkerAsync(int fleetId)
    {
        var point = _main.UiGetFleetScreenPosition(fleetId)
            ?? throw new InvalidOperationException("Player ship marker is not visible in the regional map.");
        await ClickPositionAsync(point, MouseButton.Left);
        await WaitForRefreshAsync();
        Require(_main.UiSelectedFleetId == fleetId, "Visible fleet-marker click did not select the requested owned ship.");
    }

    private (int SystemId, Vector2 Point) FindReachablePublicStar(UiOwnedFleetSnapshot ship, bool requireUnknown)
    {
        var homePoint = _main.UiGetFleetScreenPosition(ship.FleetId)
            ?? throw new InvalidOperationException("Cannot choose an exploration target without the ship marker.");
        var mapBounds = new Rect2(100, 150, 780, 470);
        var candidate = _main.UiSpatialCatalog
            .Where(entry => !requireUnknown || entry.SurveyLevel.ToString() == "Unknown")
            .Select(entry => new { entry.SystemId, Point = _main.UiGetCatalogScreenPosition(entry.SystemId) })
            .Where(entry => entry.Point.HasValue && mapBounds.HasPoint(entry.Point.Value) &&
                entry.Point.Value.DistanceTo(homePoint) > 70 && entry.Point.Value.DistanceTo(homePoint) < ship.MaximumLegRangeLightYears * .85)
            .OrderBy(entry => entry.Point!.Value.DistanceTo(homePoint))
            .FirstOrDefault();
        if (candidate is null) throw new InvalidOperationException("No visible public stellar target is within the selected ship's legal range.");
        return (candidate.SystemId, candidate.Point!.Value);
    }

    private async Task WaitForSurveyLevelAsync(int systemId, string level, string phase)
    {
        await WaitForPlayerConditionAsync(() => _main.UiSpatialCatalog.Any(entry => entry.SystemId == systemId && entry.SurveyLevel.ToString() == level),
            phase + " did not complete");
    }

    private async Task WaitForFleetAtSelectedSystemAsync(int fleetId, int systemId)
    {
        await WaitForPlayerConditionAsync(() =>
        {
            var target = _main.UiGetCatalogScreenPosition(systemId);
            var ship = _main.UiGetFleetScreenPosition(fleetId);
            return target.HasValue && ship.HasValue && target.Value.DistanceTo(ship.Value) < 28;
        }, "Colony ship did not arrive at its selected surveyed system");
    }

    private async Task WaitForColonyAsync(int colonyCountBefore, int bodyId) =>
        await WaitForPlayerConditionAsync(() => _main.UiOwnedColonies.Length > colonyCountBefore &&
            _main.UiOwnedColonies.Any(colony => colony.BodyId == bodyId && colony.PopulationMillions > 0),
            "Timed settlement did not found the authorized ordinary Player colony");

    private async Task WaitForPlayerConditionAsync(Func<bool> predicate, string failure)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromMinutes(15))
        {
            if (predicate()) return;
            await WaitForRefreshAsync();
        }
        throw new InvalidOperationException(failure + " within 15 active minutes.");
    }

    private async Task VerifyPlayerExpeditionSaveReloadAsync(MainMenuLayer menu)
    {
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");
        var coloniesBefore = _main.UiOwnedColonies.Select(colony => (colony.ColonyId, colony.BodyId, colony.PopulationMillions)).OrderBy(value => value.ColonyId).ToArray();
        var shipsBefore = _main.UiOwnedFleets.Select(fleet => (fleet.FleetId, fleet.Role, fleet.DesignId)).OrderBy(value => value.FleetId).ToArray();
        await OpenSectionAsync("menu");
        await ClickButtonAsync(ActivePanel(), "Save");
        await OpenCampaignMenuAsync();
        await ClickNamedButtonAsync(menu, "ModePlayer");
        await WaitForCampaignLoadingAsync();
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");
        var coloniesAfter = _main.UiOwnedColonies.Select(colony => (colony.ColonyId, colony.BodyId, colony.PopulationMillions)).OrderBy(value => value.ColonyId).ToArray();
        var shipsAfter = _main.UiOwnedFleets.Select(fleet => (fleet.FleetId, fleet.Role, fleet.DesignId)).OrderBy(value => value.FleetId).ToArray();
        Check(coloniesBefore.SequenceEqual(coloniesAfter) && shipsBefore.SequenceEqual(shipsAfter) && coloniesAfter.Length > 1,
            "player-expedition-save-reload-preserves-colony-people-and-ships");
    }

    private void WritePlayerExpeditionEvidenceManifest()
    {
        var evidence = new
        {
            schema_version = 1,
            scope = "focused ordinary Player Sandbox opening; no Developer mode, grants, direct state mutation, finish-orders, or hidden-knowledge commands",
            input_mode = "Input.ParseInputEvent",
            captures = _captureRecords,
            checks = _checks,
        };
        File.WriteAllText(Path.Combine(_outputDirectory, "player-expedition-manifest.json"),
            JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
    }
}

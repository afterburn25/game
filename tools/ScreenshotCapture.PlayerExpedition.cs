using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    private Stopwatch? _playerExpeditionStopwatch;
    private object? _playerAuthorizationSaveEvidence;
    private object? _playerSettlementEvidence;
    private static readonly string[] OpeningConstruction =
    {
        "research_network", "industrial_automation", "orbital_launch_complex", "orbital_shipyard", "warp_test_facility",
    };

    private async Task VerifyPlayerExpeditionAsync(MainMenuLayer menu, ConfirmationDialog dialog)
    {
        _playerExpeditionStopwatch = Stopwatch.StartNew();
        try
        {
            await StartFreshOrdinarySandboxAsync(menu, dialog);
            await SelectMaximumPlayerSpeedAsync();
            Require(_main.UiCurrentSpeed == Game.Simulation.SimulationClock.SpeedLevel.Maximum && !_main.UiIsDeveloperMode,
                "Ordinary Player expedition did not begin at the visible 8× speed setting.");
            Require(_main.UiDashboard.TotalSystemCount == 100, "Configured Sandbox did not generate the ordinary 100-system campaign.");

            await SaveViewportAsync("player-expedition-01-opening-research.png");
            await ProgressToWarpAndShipOrdersAsync();
            await SaveViewportAsync("player-expedition-02-first-warp-shipyard.png");
            await CompleteSurveyAndSettlementAsync();
            await VerifyPlayerExpeditionSaveReloadAsync(menu);
            await SaveViewportAsync("player-expedition-04-reloaded-colony.png");
            WritePlayerExpeditionEvidenceManifest();
        }
        finally { _playerExpeditionStopwatch = null; }
    }

    private async Task VerifyPlayerExpeditionControlsAsync(MainMenuLayer menu, ConfirmationDialog dialog)
    {
        _playerExpeditionStopwatch = Stopwatch.StartNew();
        try
        {
            await StartFreshOrdinarySandboxAsync(menu, dialog);
            await SelectMaximumPlayerSpeedAsync();
            await OpenSectionAsync("research");
            var deadline = Stopwatch.StartNew();
            while (deadline.Elapsed < TimeSpan.FromMinutes(2))
            {
                var startable = _main.UiResearchHorizon.FirstOrDefault(node => node.CanStart);
                if (startable is not null)
                {
                    await ClickNamedButtonAsync(ActivePanel(), "ResearchNode_" + startable.Id);
                    await VerifyOpeningResearchControlsAsync(startable.Id);
                    await SaveViewportAsync("player-expedition-controls.png");
                    WritePlayerExpeditionEvidenceManifest();
                    return;
                }
                await WaitForRefreshAsync();
            }
            throw new InvalidOperationException("Ordinary Player research controls did not expose a legal startable program within two active minutes.");
        }
        finally { _playerExpeditionStopwatch = null; }
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
        for (var step = 0; popup.GetFocusedItem() != 3 && step <= selector.ItemCount; step++)
            await PressKeyAsync(Key.Down);
        Require(popup.GetFocusedItem() == 3,
            $"Visible speed popup did not focus its ordinary 8× item (focused {popup.GetFocusedItem()}).");
        await PressKeyAsync(Key.Enter);
        Require(selector.Selected == 3 && selector.GetItemId(selector.Selected) == 4,
            $"Visible speed selector did not select the ordinary Player 8× item (selected {selector.Selected}).");
        await WaitForRefreshAsync();
    }

    private async Task ProgressToWarpAndShipOrdersAsync()
    {
        var startedConstruction = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var startedShips = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var researchIndex = 0;
        while (WithinPlayerExpeditionBudget())
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

            if (_main.UiShipChoices.Any(choice => !choice.IsCancellation && choice.Id == "warp_scout") &&
                _main.UiResearchHorizon.FirstOrDefault(node => node.CanPause) is { } activeResearch)
            {
                if (!_sidebar.IsDrawerOpen || _sidebar.ActiveSection != "research") await OpenSectionAsync("research");
                await ClickNamedButtonAsync(ActivePanel(), "ResearchNode_" + activeResearch.Id);
                Check(true, "player-expedition-pauses-active-research-for-shipbuilding-capital");
            }

            if (_main.UiDashboard.DemoStep >= 1 && new[] { FleetRole.Scout, FleetRole.Science, FleetRole.Colony }
                .All(role => _main.UiOwnedFleets.Any(fleet => fleet.Role == role)))
            {
                Check(true, "player-expedition-first-warp-completed");
                return;
            }
            await WaitForRefreshAsync();
        }
        throw new InvalidOperationException("Ordinary Player opening did not reach physical scout, science, and colony ships within the shared 22-minute journey budget.");
    }

    private async Task CompleteSurveyAndSettlementAsync()
    {
        if (_sidebar.IsDrawerOpen) await CloseDrawerAsync();
        await ClickButtonAsync(_dock, "Home");
        var scout = _main.UiOwnedFleets.Single(fleet => fleet.Role == FleetRole.Scout);
        ColonyOpportunityUiState opportunity = FindFundedSettlementOpportunity();
        for (var survey = 0; survey < 8 && !opportunity.CanOrder; survey++)
        {
            await ClickButtonAsync(_dock, "Home");
            scout = _main.UiOwnedFleets.Single(fleet => fleet.FleetId == scout.FleetId);
            var scoutTarget = FindReachablePublicStar(scout, requireUnknown: true);
            await SelectFleetByMarkerAsync(scout.FleetId);
            await ClickPositionAsync(scoutTarget.Point, MouseButton.Right);
            await RequireRouteStartedAsync(scout.FleetId, scoutTarget.SystemId, "scout right-click");
            Check(true, "player-expedition-scout-right-click-order-" + (survey + 1));
            await WaitForSurveyLevelAsync(scoutTarget.SystemId, "PartiallySurveyed", "scout reconnaissance");

            await ClickButtonAsync(_dock, "Home");
            var science = _main.UiOwnedFleets.Single(fleet => fleet.Role == FleetRole.Science);
            await SelectFleetByMarkerAsync(science.FleetId);
            var sciencePoint = _main.UiGetCatalogScreenPosition(scoutTarget.SystemId)
                ?? throw new InvalidOperationException("Reconnoitered public target lost its map marker.");
            await ClickPositionAsync(sciencePoint, MouseButton.Right);
            await RequireRouteStartedAsync(science.FleetId, scoutTarget.SystemId, "science right-click");
            Check(true, "player-expedition-science-right-click-order-" + (survey + 1));
            await WaitForSurveyLevelAsync(scoutTarget.SystemId, "FullySurveyed", "science detailed survey");
            opportunity = FindFundedSettlementOpportunity();
        }

        Require(opportunity is { FleetId: not null, SystemId: not null, PlanetaryBodyId: not null, CanOrder: true },
            "The ordinary colonies panel did not expose a funded, fully surveyed settlement opportunity after eight public survey attempts.");
        var colonyFleetId = opportunity.FleetId!.Value;
        var settlementSystemId = opportunity.SystemId!.Value;
        var settlementBodyId = opportunity.PlanetaryBodyId!.Value;
        var colonyCountBefore = _main.UiOwnedColonies.Length;
        await OpenSectionAsync("colonies");
        for (var site = 0; site < opportunity.SiteIndex; site++)
            await ClickButtonAsync(ActivePanel(), "Site →");
        var visibleOpportunity = _main.GetUiColonyOpportunityState(opportunity.FleetIndex, opportunity.SiteIndex);
        Require(visibleOpportunity.CanOrder && visibleOpportunity.FleetId == colonyFleetId &&
                visibleOpportunity.SystemId == settlementSystemId && visibleOpportunity.PlanetaryBodyId == settlementBodyId,
            "Visible Colony Sites paging did not reach the selected canonical opportunity.");
        await ClickControlAsync(RequireButton(ActivePanel(), "Select ship on map"));
        Require(_main.UiSelectedFleetId == colonyFleetId, "Colony Sites did not select its real populated colony ship.");
        var settlementPoint = _main.UiGetCatalogScreenPosition(settlementSystemId)
            ?? throw new InvalidOperationException("Selected colony opportunity does not have a visible public map position.");
        await ClickPositionAsync(settlementPoint, MouseButton.Right);
        await RequireRouteStartedAsync(colonyFleetId, settlementSystemId, "colony transit right-click");
        Check(true, "player-expedition-colony-transit-right-click-order");
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
        await WaitForRefreshAsync();
        var authorized = _main.UiOwnedFleets.Single(fleet => fleet.FleetId == colonyFleetId);
        Check(authorized.CurrentSystemId == settlementSystemId &&
              authorized.DestinationPlanetaryBodyId == settlementBodyId && authorized.SettlementBodyId == settlementBodyId &&
              authorized.EmbarkedPopulationMillions > 0 && !_main.UiOwnedColonies.Any(colony => colony.BodyId == settlementBodyId),
            "player-expedition-colony-body-right-click-authorizes-settlement");
        await SavePlayerSettlementAuthorizationAsync(colonyFleetId, settlementBodyId, authorized.EmbarkedPopulationMillions);
        await WaitForColonyAsync(colonyCountBefore, settlementBodyId);
        var founded = _main.UiOwnedColonies.Single(colony => colony.BodyId == settlementBodyId);
        var colonyShipConsumed = !_main.UiOwnedFleets.Any(fleet => fleet.FleetId == colonyFleetId);
        Check(_main.UiOwnedColonies.Length == colonyCountBefore + 1 &&
              Math.Abs(founded.PopulationMillions - authorized.EmbarkedPopulationMillions) < .000001 && colonyShipConsumed,
            "player-expedition-exact-body-founded-and-colony-ship-consumed");
        _playerSettlementEvidence = new { fleet_id = colonyFleetId, body_id = settlementBodyId,
            colonies_before = colonyCountBefore, colonies_after = _main.UiOwnedColonies.Length,
            population_millions = founded.PopulationMillions, colony_ship_consumed = colonyShipConsumed };
        Check(true, "player-expedition-settlement-timed-and-complete");
    }

    private ColonyOpportunityUiState FindFundedSettlementOpportunity()
    {
        var first = _main.GetUiColonyOpportunityState(0, 0);
        for (var site = 0; site < first.SiteCount; site++)
        {
            var candidate = _main.GetUiColonyOpportunityState(first.FleetIndex, site);
            if (candidate.CanOrder) return candidate;
        }
        return first;
    }

    private async Task VerifyOpeningResearchControlsAsync(string researchId)
    {
        var control = Descendants(ActivePanel()).OfType<Button>().Single(button => button.Name == "ResearchNode_" + researchId);
        var instance = control.GetInstanceId();
        var opening = _main.UiResearchHorizon.Single(node => node.Id == researchId);
        var progressBefore = opening.Progress;
        var detailBefore = opening.Detail;
        var changedDetail = false;
        var refreshDeadline = Stopwatch.StartNew();
        while (refreshDeadline.Elapsed < TimeSpan.FromSeconds(7))
        {
            await WaitForRefreshAsync();
            var live = _main.UiResearchHorizon.Single(node => node.Id == researchId);
            if (live.Detail != detailBefore) { changedDetail = true; break; }
        }
        var refreshed = Descendants(ActivePanel()).OfType<Button>().Single(button => button.Name == "ResearchNode_" + researchId);
        var progressAfter = _main.UiResearchHorizon.Single(node => node.Id == researchId).Progress;
        GD.Print($"STELLAR_RESEARCH_CONTROL id={researchId} sameInstance={refreshed.GetInstanceId() == instance} focus={refreshed.HasFocus()} progress={progressBefore:R}->{progressAfter:R} changedDetail={changedDetail} tooltipCurrent={refreshed.TooltipText.Contains(_main.UiResearchHorizon.Single(node => node.Id == researchId).Detail, StringComparison.Ordinal)} before='{detailBefore}' after='{_main.UiResearchHorizon.Single(node => node.Id == researchId).Detail}'");
        Check(refreshed.GetInstanceId() == instance && refreshed.HasFocus() && progressAfter > progressBefore && changedDetail &&
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
        var mapBounds = new Rect2(100, 150, 780, 470);
        var candidate = _main.UiSpatialCatalog
            .Where(entry => !requireUnknown || entry.SurveyLevel.ToString() == "Unknown")
            .Select(entry => new { entry.SystemId, Point = _main.UiGetCatalogScreenPosition(entry.SystemId) })
            .Where(entry => entry.Point.HasValue && mapBounds.HasPoint(entry.Point.Value))
            .Select(entry => new { entry.SystemId, Point = entry.Point!.Value,
                Reach = _main.UiGetFleetRouteAssessment(ship.FleetId, entry.SystemId) })
            .Where(entry => entry.Reach.ReachSupported && entry.Reach.DistanceLy > .001)
            .OrderBy(entry => entry.Reach.DistanceLy)
            .FirstOrDefault();
        if (candidate is null) throw new InvalidOperationException("No visible public stellar target passes the selected ship's canonical route and fuel assessment.");
        return (candidate.SystemId, candidate.Point);
    }

    private async Task WaitForSurveyLevelAsync(int systemId, string level, string phase)
    {
        await WaitForPlayerConditionAsync(() => _main.UiSpatialCatalog.Any(entry => entry.SystemId == systemId && entry.SurveyLevel.ToString() == level),
            phase + " did not complete");
    }

    private async Task WaitForFleetAtSelectedSystemAsync(int fleetId, int systemId)
    {
        await WaitForPlayerConditionAsync(() => _main.UiOwnedFleets.Any(fleet => fleet.FleetId == fleetId &&
            fleet.CurrentSystemId == systemId && fleet.DestinationSystemId is null && fleet.RemainingRouteLegs == 0 &&
            fleet.RemainingRouteDistanceLightYears < .0001), "Colony ship did not finish its canonical route to the selected surveyed system");
    }

    private async Task WaitForColonyAsync(int colonyCountBefore, int bodyId) =>
        await WaitForPlayerConditionAsync(() => _main.UiOwnedColonies.Length > colonyCountBefore &&
            _main.UiOwnedColonies.Any(colony => colony.BodyId == bodyId && colony.PopulationMillions > 0),
            "Timed settlement did not found the authorized ordinary Player colony");

    private async Task SavePlayerSettlementAuthorizationAsync(int fleetId, int bodyId, double embarkedPopulation)
    {
        if (!_main.UiIsPaused) await ClickNamedButtonAsync(_main, "SimulationPause");
        await OpenSectionAsync("menu");
        await ClickButtonAsync(ActivePanel(), "Save");
        var path = ProjectSettings.GlobalizePath("user://saves/autosave.json");
        var saved = FindConstructionGalaxy(JsonNode.Parse(File.ReadAllText(path)))
            ?? throw new InvalidOperationException("Ordinary Player authorization save has no galaxy payload.");
        var fleet = saved["Fleets"]!.AsArray().Single(item => item!["Id"]!.GetValue<int>() == fleetId)!.AsObject();
        Check(fleet["DestinationPlanetaryBodyId"]!.GetValue<int>() == bodyId &&
              fleet["SettlementBodyId"]!.GetValue<int>() == bodyId &&
              Math.Abs(fleet["EmbarkedPopulationMillions"]!.GetValue<double>() - embarkedPopulation) < .000001,
            "player-expedition-authorization-save-preserves-ship-id-target-and-embarked-people");
        await CloseDrawerAsync();
        Require(_main.UiIsPaused, "Settlement authorization evidence resumed the clock before capture.");
        _playerAuthorizationSaveEvidence = new { fleet_id = fleetId, body_id = bodyId,
            embarked_population_millions = embarkedPopulation, simulation_days = _main.UiSimulationDays,
            save_bytes = new FileInfo(path).Length, save_sha256 = HashFile(path), captured_while_paused = true };
        await SaveViewportAsync("player-expedition-03-settlement-authorized.png");
        await SelectMaximumPlayerSpeedAsync();
    }

    private async Task WaitForPlayerConditionAsync(Func<bool> predicate, string failure)
    {
        while (WithinPlayerExpeditionBudget())
        {
            if (predicate()) return;
            await WaitForRefreshAsync();
        }
        throw new InvalidOperationException(failure + $" before the shared journey deadline. status='{_main.UiStatusMessage}', date='{_main.UiDashboard.Date}', speed='{_main.UiSpeedLabel}'.");
    }

    private bool WithinPlayerExpeditionBudget() => _playerExpeditionStopwatch?.Elapsed < TimeSpan.FromMinutes(22);

    private async Task RequireRouteStartedAsync(int fleetId, int destinationSystemId, string action)
    {
        await WaitForRefreshAsync();
        var fleet = _main.UiOwnedFleets.SingleOrDefault(item => item.FleetId == fleetId);
        Require(fleet is not null && fleet.DestinationSystemId == destinationSystemId &&
            (fleet.RemainingRouteLegs > 0 || fleet.RemainingRouteDistanceLightYears > .0001),
            $"{action} was not accepted: fleet={fleetId}, expectedDestination={destinationSystemId}, actualDestination={fleet?.DestinationSystemId}, route={fleet?.RemainingRouteLegs}/{fleet?.RemainingRouteDistanceLightYears:0.###}, status='{_main.UiStatusMessage}'.");
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
            git_sha = System.Environment.GetEnvironmentVariable("STELLAR_CAPTURE_SHA") ?? "unknown",
            seed = "20260908",
            system_count = _main.UiDashboard.TotalSystemCount,
            player_mode = !_main.UiIsDeveloperMode && !_main.UiDeveloperToolsUsed,
            simulation_days = _main.UiSimulationDays,
            campaign_date = _main.UiDashboard.Date,
            elapsed_wall_seconds = _playerExpeditionStopwatch?.Elapsed.TotalSeconds ?? 0,
            scope = "focused ordinary Player Sandbox opening; no Developer mode, grants, direct state mutation, finish-orders, or hidden-knowledge commands",
            input_mode = "Input.ParseInputEvent",
            mouse_actions = _mouseActions,
            authorization_save = _playerAuthorizationSaveEvidence,
            settlement = _playerSettlementEvidence,
            captures = _captureRecords,
            checks = _checks,
        };
        File.WriteAllText(Path.Combine(_outputDirectory, "player-expedition-manifest.json"),
            JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
    }
}

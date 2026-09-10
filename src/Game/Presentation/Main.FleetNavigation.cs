using System;
using System.Linq;
using Godot;
using Game.Simulation.Models;
using Game.Simulation.Exploration;
using Game.Simulation.Economy;
using Game.Simulation.Colonization;

namespace Game.Presentation;

/// <summary>Selection and mouse orders use exact owned vessels and authoritative route commands.</summary>
public partial class Main
{
    private int? _selectedFleetId;
    private GalaxyState? _fleetSelectionContext;
    private int? _hoverDestinationId;
    private FleetState? SelectedFleet => ReferenceEquals(_fleetSelectionContext, _galaxy)
        ? _galaxy.Fleets.FirstOrDefault(f => f.Id == _selectedFleetId && f.IsActive && f.CivilizationId == _galaxy.PlayerCivilizationId)
        : null;
    public int? UiSelectedFleetId => SelectedFleet?.Id;
    public void UiClearFleetSelection() { _selectedFleetId = null; QueueRedraw(); }

    public void UiSelectOwnedFleet(int fleetId, bool center = false)
    {
        var fleet = _galaxy.Fleets.FirstOrDefault(f => f.Id == fleetId && f.IsActive && f.CivilizationId == _galaxy.PlayerCivilizationId);
        if (fleet is null) return;
        UiCloseOrbitalInspector();
        _fleetSelectionContext = _galaxy;
        _selectedFleetId = fleetId;
        GetNode<CampaignSidebar>("CampaignSidebar").CloseDrawer();
        if (center)
        {
            ReturnToStellarView(announce: false);
            _zoom = Spatial.SpatialNavigationLayout.StellarRegionScale;
            _pan = -new Vector2(fleet.Position.X, fleet.Position.Y) * _zoom;
            SynchronizeRegionalCamera();
        }
        SetStatus($"{fleet.Name} selected. Right-click a destination to set its course.", 4);
        AudioDirector.PlayConfirm();
        QueueRedraw();
    }

    private Vector2 FleetMarkerScreenPosition(FleetState fleet, Vector2 center)
    {
        var offset = fleet.Role switch
        {
            FleetRole.Scout => new Vector2(-17, 23), FleetRole.Science => new Vector2(17, 23),
            FleetRole.Colony => new Vector2(-17, 52), FleetRole.Military => new Vector2(17, 52),
            _ => new Vector2(51, 52),
        };
        return ToScreen(fleet.Position, center) + offset;
    }

    public Vector2? UiGetFleetScreenPosition(int fleetId)
    {
        var fleet = _galaxy.Fleets.FirstOrDefault(f => f.Id == fleetId && f.IsActive && f.CivilizationId == _galaxy.PlayerCivilizationId);
        return fleet is null || UiIsSystemSpatialView ? null : FleetMarkerScreenPosition(fleet, GetViewportRect().Size * .5f + _pan);
    }

    private bool TrySelectFleetAt(Vector2 pointer)
    {
        var center = GetViewportRect().Size * .5f + _pan;
        var hits = _galaxy.Fleets.Where(f => f.IsActive && f.CivilizationId == _galaxy.PlayerCivilizationId &&
            FleetMarkerScreenPosition(f, center).DistanceTo(pointer) <= 15).OrderBy(f => f.Id).ToArray();
        if (hits.Length == 0) return false;
        // Repeated clicks cycle the ships in a co-located role group; the outliner selects exact IDs.
        var index = Array.FindIndex(hits, f => f.Id == UiSelectedFleetId);
        UiSelectOwnedFleet(hits[(index + 1) % hits.Length].Id);
        return true;
    }

    private void IssueSelectedFleetOrderAt(Vector2 pointer)
    {
        var target = FindNearestCatalogSystem(pointer, 18);
        if (target is null) return;
        var fleet = SelectedFleet;
        if (fleet is null) { SetStatus("Select a ship icon first, then right-click its destination.", 5); return; }
        UiPointerCommandRevision++;
        if (fleet.Role is FleetRole.Scout or FleetRole.Science)
            IssueExplorationOrder(fleet, target.Id, fleet.Role == FleetRole.Scout ? "scout" : "science vessel");
        else if (fleet.Role == FleetRole.Military)
        {
            var result = _coreSimulation.IssueMilitaryDeploymentOrder(_galaxy, _galaxy.PlayerCivilizationId, fleet.Id, target.Id);
            SetStatus(result.Message, 6);
        }
        else if (fleet.Role == FleetRole.Colony)
        {
            var result = _colonization.IssueTransitOrder(_galaxy, _galaxy.PlayerCivilizationId, fleet.Id, target.Id);
            SetStatus(result.Message, 7);
        }
        else
        {
            var result = _coreSimulation.IssueFreightTransitOrder(_galaxy, _galaxy.PlayerCivilizationId, fleet.Id, target.Id);
            SetStatus(result.Message, 6);
        }
        QueueRedraw();
    }

    private void IssueSelectedFleetBodyOrder(int bodyId)
    {
        if (SelectedFleet is not { } fleet) { SetStatus("Select a ship before choosing its destination.", 5); return; }
        UiPointerCommandRevision++;
        if (fleet.Role == FleetRole.Colony)
            SetStatus(IssueUiColonyOrder(fleet.Id, _selectedSystemId, bodyId), 7);
        else if (fleet.Role is FleetRole.Scout or FleetRole.Science)
            IssueExplorationOrder(fleet, _selectedSystemId, fleet.Role == FleetRole.Scout ? "scout" : "science vessel");
        else SetStatus("Select a star on the galaxy map to order interstellar travel.", 5);
    }

    public string UiSelectedFleetEta
    {
        get
        {
            if (SelectedFleet is not { } fleet) return "";
            var status = _missionStatusEvaluator.Build(_galaxy, fleet);
            if (status.Phase == ExplorationMissionPhase.Traveling && status.EstimatedTransitDaysRemaining is double transit)
                return $"Arrival in {transit:0.0} game days";
            return status.EstimatedMissionDaysRemaining is > 0 ? $"Work: {status.EstimatedMissionDaysRemaining:0.0} game days remaining" : status.Summary;
        }
    }

    public string UiFleetDestinationPreview
    {
        get
        {
            if (SelectedFleet is not { } fleet) return "Select a ship to preview its orders.";
            if (UiIsSystemSpatialView)
            {
                if (fleet.Role == FleetRole.Colony && (_systemSpatialCanvas?.HoveredBodyId ?? UiSelectedBodyId) is int bodyId)
                {
                    var outpost = ResourceOutpostOpportunityPlanner.IsOutpostFleet(fleet);
                    var assessment = outpost
                        ? new ResourceOutpostOpportunityPlanner().AssessOrder(_galaxy, fleet.Id, _selectedSystemId, bodyId)
                        : null;
                    var colony = outpost ? null : _colonization.AssessColonyOrder(_galaxy, fleet.Id, _selectedSystemId, bodyId);
                    if (!(assessment?.Accepted ?? colony!.Accepted)) return assessment?.Message ?? colony!.Message;
                    var authorized = !fleet.PreventAutomaticSettlement && (fleet.DestinationPlanetaryBodyId is not null || fleet.SettlementBodyId is not null);
                    var cost = authorized ? 0 : outpost ? ColonizationSimulation.ResourceOutpostExpeditionCreditCost : ColonizationSimulation.ColonyExpeditionCreditCost;
                    return $"{UiFormatMoney(cost)} • At least {ColonizationSimulation.EstablishmentDays(fleet):0} game days\n" +
                        (PlayerEconomy.Credits < cost ? "Additional funding required." : "Right-click this world to begin settlement.");
                }
                return fleet.Role == FleetRole.Colony ? "Point at a surveyed world to review settlement costs and requirements." :
                    "Scouts and science vessels work after arrival. Select a star in the galaxy view to set a new course.";
            }
            if (_hoverDestinationId is not int targetId) return "Hover a star to preview its route. Right-click to travel.";
            var reach = fleet.Role == FleetRole.Colony ? _colonization.AssessOperationalReach(_galaxy, fleet.Id, targetId)
                : _exploration.AssessOperationalReach(_galaxy, fleet.Id, targetId);
            if (!reach.IsSupported) return reach.Reason;
            var funding = CivilizationOperatingCapacity.GetFundingFraction(_galaxy, fleet.CivilizationId);
            var speed = fleet.StrategicSpeed * funding;
            var time = speed > 0 ? $"{reach.RouteDistanceLightYears / speed:0.0} game days" : "Awaiting operations funding";
            return $"{reach.RouteDistanceLightYears:0.0} ly • {reach.RouteDistanceLightYears / 3.26156:0.0} pc\n{time} • Fuel {reach.RouteDistanceLightYears:0.0} ly\nRight-click to set course.";
        }
    }
}

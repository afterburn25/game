using System;
using Godot;

namespace Game.Presentation;

/// <summary>
/// Compact observer-safe exploration/colonization panel. It renders Main's already-filtered
/// presentation state. Colony selection is presentation-only; authoritative order validation and
/// mutation occur through Core when Settle Here is pressed.
/// </summary>
public partial class ExplorationMissionPanel : CanvasLayer
{
    private Main _main = null!;
    private Label _content = null!;
    private HFlowContainer _colonyControls = null!;
    private Label _actionStatus = null!;
    private Button _previousFleetButton = null!;
    private Button _nextFleetButton = null!;
    private Button _previousSiteButton = null!;
    private Button _nextSiteButton = null!;
    private Button _settleButton = null!;
    private double _refreshTimer;
    private bool _showColonySites;
    private int _selectedFleetIndex;
    private int _selectedSiteIndex;

    public override void _Ready()
    {
        _main = GetParent() as Main
            ?? throw new InvalidOperationException("ExplorationMissionPanel must be a child of Main.");

        var panel = new PanelContainer { Name = "ExplorationPanel" };

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        panel.AddChild(root);

        var header = new HFlowContainer();
        header.AddThemeConstantOverride("h_separation", 8);
        header.AddThemeConstantOverride("v_separation", 6);
        root.AddChild(header);

        header.AddChild(new Label
        {
            Text = "EXPLORATION / COLONIZATION",
            TooltipText = "Mission phases and colony opportunities come from observer-safe simulation read models. The panel does not calculate survey, biological suitability, or operational reach itself.",
        });

        var missionsButton = new Button
        {
            Text = "Missions",
            Icon = VisualIconLibrary.Exploration,
            TooltipText = "Show active scout, science, and colony mission phases and ETAs.",
            CustomMinimumSize = new Vector2(104, 28),
        };
        missionsButton.Pressed += () =>
        {
            _showColonySites = false;
            _actionStatus.Text = string.Empty;
            RefreshContent();
        };
        header.AddChild(missionsButton);

        var colonyButton = new Button
        {
            Text = "Colony Sites",
            Icon = VisualIconLibrary.Colony,
            TooltipText = "Browse fully surveyed settlement opportunities for populated player colony ships. Suitability and reach come from shared simulation contracts.",
            CustomMinimumSize = new Vector2(128, 28),
        };
        colonyButton.Pressed += () =>
        {
            _showColonySites = true;
            _actionStatus.Text = string.Empty;
            RefreshContent();
        };
        header.AddChild(colonyButton);

        _content = new Label
        {
            Text = "Exploration missions are initializing…",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(0, 156),
            VerticalAlignment = VerticalAlignment.Top,
        };
        root.AddChild(_content);

        _colonyControls = new HFlowContainer
        {
            Visible = false,
        };
        _colonyControls.AddThemeConstantOverride("h_separation", 6);
        _colonyControls.AddThemeConstantOverride("v_separation", 6);
        root.AddChild(_colonyControls);

        _previousFleetButton = AddControlButton(_colonyControls, "← Ship", "Previous populated colony ship.", () =>
        {
            _selectedFleetIndex--;
            _selectedSiteIndex = 0;
            ClearActionAndRefresh();
        });
        _nextFleetButton = AddControlButton(_colonyControls, "Ship →", "Next populated colony ship.", () =>
        {
            _selectedFleetIndex++;
            _selectedSiteIndex = 0;
            ClearActionAndRefresh();
        });
        _previousSiteButton = AddControlButton(_colonyControls, "← Site", "Previous bounded colony-site candidate.", () =>
        {
            _selectedSiteIndex--;
            ClearActionAndRefresh();
        });
        _nextSiteButton = AddControlButton(_colonyControls, "Site →", "Next bounded colony-site candidate.", () =>
        {
            _selectedSiteIndex++;
            ClearActionAndRefresh();
        });
        _settleButton = AddControlButton(_colonyControls, "Settle Here", "Issue an exact-body colony order. Core revalidates the opportunity at click time.", IssueSelectedColonyOrder, 132.0f, VisualIconLibrary.Colony);

        _actionStatus = new Label
        {
            Visible = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(0, 38),
        };
        root.AddChild(_actionStatus);

        _main.GetNode<CampaignSidebar>("CampaignSidebar").AddPanel(panel);
        RefreshContent();
    }

    public override void _Process(double delta)
    {
        _refreshTimer += delta;
        if (_main is null || _content is null || _refreshTimer < 0.5)
            return;

        _refreshTimer = 0.0;
        RefreshContent();
    }

    private static Button AddControlButton(
        Container parent,
        string text,
        string tooltip,
        Action action,
        float width = 82.0f,
        Texture2D? icon = null)
    {
        var button = new Button
        {
            Text = text,
            Icon = icon,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(width, 28),
        };
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }

    private void ClearActionAndRefresh()
    {
        _actionStatus.Text = string.Empty;
        RefreshContent();
    }

    private void IssueSelectedColonyOrder()
    {
        var selection = _main.GetUiColonyOpportunityState(_selectedFleetIndex, _selectedSiteIndex);
        _selectedFleetIndex = selection.FleetIndex;
        _selectedSiteIndex = selection.SiteIndex;

        if (!selection.CanOrder ||
            selection.FleetId is not int fleetId ||
            selection.SystemId is not int systemId ||
            selection.PlanetaryBodyId is not int bodyId)
        {
            _actionStatus.Text = selection.ActionReason;
            RefreshContent();
            return;
        }

        _actionStatus.Text = _main.IssueUiColonyOrder(fleetId, systemId, bodyId);
        RefreshContent();
    }

    private void RefreshContent()
    {
        if (_main is null || _content is null)
            return;

        if (!_showColonySites)
        {
            _content.Text = _main.UiExplorationMissionDetails;
            _colonyControls.Visible = false;
            _actionStatus.Visible = false;
            return;
        }

        var selection = _main.GetUiColonyOpportunityState(_selectedFleetIndex, _selectedSiteIndex);
        _selectedFleetIndex = selection.FleetIndex;
        _selectedSiteIndex = selection.SiteIndex;
        _content.Text = selection.Details;
        _colonyControls.Visible = true;

        _previousFleetButton.Disabled = selection.FleetCount <= 1 || selection.FleetIndex <= 0;
        _nextFleetButton.Disabled = selection.FleetCount <= 1 || selection.FleetIndex >= selection.FleetCount - 1;
        _previousSiteButton.Disabled = selection.SiteCount <= 1 || selection.SiteIndex <= 0;
        _nextSiteButton.Disabled = selection.SiteCount <= 1 || selection.SiteIndex >= selection.SiteCount - 1;
        _settleButton.Disabled = !selection.CanOrder;
        _settleButton.TooltipText = selection.CanOrder
            ? "Issue this exact-body colony order. Core revalidates current survey, species, occupancy and reach before mutation."
            : selection.ActionReason;

        _actionStatus.Visible = !string.IsNullOrWhiteSpace(_actionStatus.Text);
    }
}

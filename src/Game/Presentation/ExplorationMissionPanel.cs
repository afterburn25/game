using System;
using System.Collections.Generic;
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
    private CampaignSidebar _sidebar = null!;
    private Label _content = null!;
    private VBoxContainer _ownedColonies = null!;
    private readonly Dictionary<int, Label> _ownedColonyLabels = new();
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

        _sidebar = _main.GetNode<CampaignSidebar>("CampaignSidebar");
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
            Text = "MISSIONS & SETTLEMENT",
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

        _ownedColonies = new VBoxContainer { Visible = false };
        _ownedColonies.AddThemeConstantOverride("separation", 7);
        root.AddChild(_ownedColonies);

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
        _settleButton = AddControlButton(_colonyControls, "Fund & Settle", "Fund and issue an exact-body colony order. Core revalidates the opportunity at click time.", IssueSelectedColonyOrder, 150.0f, VisualIconLibrary.Colony);

        _actionStatus = new Label
        {
            Visible = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(0, 38),
        };
        root.AddChild(_actionStatus);

        _sidebar.AddPanel(panel);
        _sidebar.SectionChanged += OnSectionChanged;
        RefreshContent();
    }

    public override void _ExitTree() => _sidebar.SectionChanged -= OnSectionChanged;

    private void OnSectionChanged(string? section)
    {
        if (section is not ("explore" or "colonies")) return;
        _showColonySites = section == "colonies";
        _actionStatus.Text = string.Empty;
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
            _ownedColonies.Visible = false;
            _colonyControls.Visible = false;
            _actionStatus.Visible = false;
            return;
        }

        var selection = _main.GetUiColonyOpportunityState(_selectedFleetIndex, _selectedSiteIndex);
        RefreshOwnedColonies();
        _ownedColonies.Visible = true;
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
            ? "Fund this exact-body colony expedition for 120 credits ($1.2B Earth reference). Core revalidates current survey, species, occupancy and reach before mutation."
            : selection.ActionReason;

        _actionStatus.Visible = !string.IsNullOrWhiteSpace(_actionStatus.Text);
    }

    private void RefreshOwnedColonies()
    {
        var colonies = _main.UiOwnedColonies;
        foreach (var staleId in new List<int>(_ownedColonyLabels.Keys))
        {
            if (Array.Exists(colonies, colony => colony.ColonyId == staleId)) continue;
            _ownedColonyLabels[staleId].GetParent().QueueFree();
            _ownedColonyLabels.Remove(staleId);
        }
        if (_ownedColonyLabels.Count == 0)
            _ownedColonies.AddChild(VisualUi.Text("OWNED WORLDS", 12, VisualUi.Accent));
        foreach (var colony in colonies)
        {
            if (!_ownedColonyLabels.TryGetValue(colony.ColonyId, out var label))
            {
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 8);
                label = VisualUi.Text("", 14, Colors.White, wrap: true);
                label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                row.AddChild(label);
                row.AddChild(VisualUi.Button("View", "Open this colony's orbital system and focus its world.",
                    () => _main.UiOpenOwnedColony(colony.ColonyId, false), VisualIconLibrary.NavSystem));
                var land = VisualUi.Button("Land", "Open the freely navigable colony surface and construction palette.",
                    () => _main.UiOpenOwnedColony(colony.ColonyId, true), VisualIconLibrary.Colony);
                land.Disabled = !colony.CanLand;
                row.AddChild(land);
                _ownedColonies.AddChild(row);
                _ownedColonyLabels.Add(colony.ColonyId, label);
            }
            var population = colony.PopulationMillions >= 1
                ? $"{colony.PopulationMillions:N0}M"
                : $"{colony.PopulationMillions * 1000:N0}K";
            var habitatCost = colony.HabitatSupportReduction > 0
                ? $"{colony.HabitatSupportCreditsPerDay:0.00} C/day life support after {colony.HabitatSupportReduction:P0} local reduction (gross {colony.GrossHabitatSupportCreditsPerDay:0.00})"
                : $"{colony.HabitatSupportCreditsPerDay:0.00} C/day life support";
            var powerState = colony.SurfacePowerDemand > colony.SurfacePowerSupply ? "POWER SHORTAGE" : "power available";
            label.Text = $"{colony.ColonyName}  ·  {colony.PlanetName}, {colony.SystemName}\n" +
                $"{colony.SettlementScale} · {population} population · {colony.AdministrationCreditsPerDay:0.00} C/day administration\n" +
                $"{colony.HabitatNeeds} · {habitatCost}\n" +
                $"Surface {colony.BuildingCount} buildings · power {colony.SurfacePowerDemand:0.#} / {colony.SurfacePowerSupply:0.#} ({powerState})\n" +
                $"{colony.SpecializationName} · {colony.SpecializationDescription}";
        }
    }
}

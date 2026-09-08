using System;
using Godot;

namespace Game.Presentation;

/// <summary>
/// Early-release discoverable control surface. It intentionally contains presentation and
/// command wiring only; authoritative game rules remain in Main's simulation services.
/// </summary>
public partial class PlayerControls : CanvasLayer
{
    private Main _main = null!;
    private Label _buildLabel = null!;
    private Label _speedLabel = null!;
    private Label _logisticsLabel = null!;
    private Label _shipbuildingLabel = null!;
    private Button _pauseButton = null!;
    private Button _panelsButton = null!;
    private HFlowContainer _mapToolbar = null!;
    private bool _panelsVisible = true;
    private double _logisticsRefreshTimer;

    public override void _Ready()
    {
        _main = GetParent() as Main
            ?? throw new InvalidOperationException("PlayerControls must be a child of Main.");

        var panel = new PanelContainer { Name = "CommandPanel" };

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        panel.AddChild(root);

        var header = new HFlowContainer();
        header.AddThemeConstantOverride("h_separation", 14);
        header.AddThemeConstantOverride("v_separation", 6);
        root.AddChild(header);

        _buildLabel = new Label { Text = _main.UiBuildLabel };
        _speedLabel = new Label { Text = _main.UiSpeedLabel };
        header.AddChild(_buildLabel);
        header.AddChild(_speedLabel);

        _logisticsLabel = new Label
        {
            Text = _main.UiLogisticsSummary,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            TooltipText = "Strategic supply is calculated by the authoritative economy/logistics subsystem. Effective coverage includes local support plus current cargo-handling capacity.",
        };
        root.AddChild(_logisticsLabel);

        _shipbuildingLabel = new Label
        {
            Name = "ShipbuildingStatus",
            Text = _main.UiShipbuildingSummary,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        root.AddChild(_shipbuildingLabel);

        var timeRow = new HFlowContainer();
        timeRow.AddThemeConstantOverride("h_separation", 4);
        timeRow.AddThemeConstantOverride("v_separation", 4);
        root.AddChild(timeRow);

        _pauseButton = AddButton(timeRow, "Pause", "Pause or resume the strategic simulation.", _main.UiTogglePause, 82, VisualIconLibrary.Pause);
        AddButton(timeRow, "1x", "Normal simulation speed.", () => _main.UiSetSpeed(1));
        AddButton(timeRow, "2x", "Fast simulation speed.", () => _main.UiSetSpeed(2));
        AddButton(timeRow, "3x", "Very fast simulation speed.", () => _main.UiSetSpeed(3));
        AddButton(timeRow, "4x", "Maximum requested simulation speed. Effective speed may be lower if the machine cannot sustain it.", () => _main.UiSetSpeed(4));

        var actionRow = new HFlowContainer();
        actionRow.AddThemeConstantOverride("h_separation", 4);
        actionRow.AddThemeConstantOverride("v_separation", 4);
        root.AddChild(actionRow);

        AddButton(actionRow, "Next Research", "Cycle through currently available research choices.", _main.UiCycleResearch, 128, VisualIconLibrary.Research);
        AddButton(actionRow, "Start Research", "Begin the currently selected research project.", _main.UiStartResearch, 128, VisualIconLibrary.Research);
        AddButton(actionRow, "Next Build", "Cycle through currently available construction projects.", _main.UiCycleConstruction, 108, VisualIconLibrary.Construction);
        AddButton(actionRow, "Start Build", "Begin the currently selected construction project.", _main.UiStartConstruction, 108, VisualIconLibrary.Construction);

        var shipRow = new HFlowContainer();
        shipRow.AddThemeConstantOverride("h_separation", 4);
        shipRow.AddThemeConstantOverride("v_separation", 4);
        root.AddChild(shipRow);
        AddButton(shipRow, "Next Ship", "Choose an available ship design. Requires warp capability and an Orbital Shipyard.", _main.UiCycleShipDesign, 108);
        AddButton(shipRow, "Build / Queue Ship", "Build the ship named in Shipyard status using your available Industry.", _main.UiBuildShip, 158);

        // Keep map commands outside the panels: players need to select stars that the
        // panels otherwise cover, then issue an order without reopening the sidebar.
        _mapToolbar = new HFlowContainer
        {
            Name = "MapToolbar",
            Position = new Vector2(16, 158),
        };
        var explorationRow = _mapToolbar;
        explorationRow.AddThemeConstantOverride("h_separation", 4);
        explorationRow.AddThemeConstantOverride("v_separation", 4);
        AddChild(explorationRow);
        _panelsButton = AddButton(explorationRow, "Hide Panels", "Clear the map for star selection. The toolbar stays visible; Show Panels restores controls and inspection.", TogglePanels, 126);
        AddButton(explorationRow, "Home", "Select and center your home star. Open System shows its known orbits.", _main.UiSelectHomeSystem, 72);
        AddButton(explorationRow, "Send Scout", "Send your first active scout to the selected star for reconnaissance.", _main.UiSendScout, 112);
        AddButton(explorationRow, "Send Science", "Send your first active science vessel to fully survey the selected star.", _main.UiSendScience, 120);
        AddButton(explorationRow, "Open System", "Inspect known orbits after scout reconnaissance.", _main.UiOpenSelectedSystem, 120);
        AddButton(explorationRow, "Back to Region", "Return from the orbital view to the regional star map.", _main.UiReturnToRegion, 130);

        var utilityRow = new HFlowContainer();
        utilityRow.AddThemeConstantOverride("h_separation", 4);
        utilityRow.AddThemeConstantOverride("v_separation", 4);
        root.AddChild(utilityRow);

        AddButton(utilityRow, "New Game", "Generate a new campaign beginning January 1, 2050.", _main.UiNewCampaign, 92);
        AddButton(utilityRow, "Menu", "Open campaign and demo options. Pauses while the menu is open.", _main.UiOpenMenu, 82);
        AddButton(utilityRow, "Save", "Save the current campaign in its own autosave slot.", _main.UiSave, 82, VisualIconLibrary.Save);
        AddButton(utilityRow, "Support Bundle", "Export diagnostics and include the autosave when available.", _main.UiExportDiagnostics, 138, VisualIconLibrary.Support);
        AddButton(utilityRow, "Relations", "Open or close the observer-safe diplomatic relations overlay.", _main.UiToggleRelationsPanel, 104, VisualIconLibrary.Relations);

        _main.GetNode<CampaignSidebar>("CampaignSidebar").AddPanel(panel);
        RefreshState(forceLogistics: true);
    }

    public override void _Process(double delta)
    {
        _mapToolbar.Size = new Vector2(Mathf.Max(1, GetViewport().GetVisibleRect().Size.X - 32), _mapToolbar.Size.Y);
        _logisticsRefreshTimer += delta;
        RefreshState(forceLogistics: _logisticsRefreshTimer >= 0.5);
        if (_logisticsRefreshTimer >= 0.5)
            _logisticsRefreshTimer = 0.0;
    }

    private void TogglePanels()
    {
        _panelsVisible = !_panelsVisible;
        _main.GetNode<CampaignSidebar>("CampaignSidebar").Visible = _panelsVisible;
        _main.GetNode<SystemInspectionPanel>("SystemInspectionPanel").Visible = _panelsVisible;
        _main.GetNode<LogisticsNetworkPanel>("LogisticsNetworkPanel").Visible = _panelsVisible;
        if (!_panelsVisible)
            _main.GetNode<RelationsPanel>("RelationsPanel").Visible = false;
        _panelsButton.Text = _panelsVisible ? "Hide Panels" : "Show Panels";
    }

    private void RefreshState(bool forceLogistics)
    {
        if (_main is null || _buildLabel is null || _speedLabel is null || _logisticsLabel is null || _shipbuildingLabel is null || _pauseButton is null)
            return;

        _buildLabel.Text = _main.UiBuildLabel;
        _speedLabel.Text = $"Speed: {_main.UiSpeedLabel}";
        _pauseButton.Text = _main.UiIsPaused ? "Resume" : "Pause";
        _shipbuildingLabel.Text = _main.UiShipbuildingSummary;

        if (forceLogistics)
            _logisticsLabel.Text = _main.UiLogisticsSummary;
    }

    private static Button AddButton(
        Container parent,
        string text,
        string tooltip,
        Action action,
        float minimumWidth = 64,
        Texture2D? icon = null)
    {
        var button = new Button
        {
            Text = text,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(minimumWidth, 30),
            FocusMode = Control.FocusModeEnum.All,
            Icon = icon,
        };
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }
}

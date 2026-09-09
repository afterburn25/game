using System;
using Godot;
using Game.Simulation.Economy;

namespace Game.Presentation;

/// <summary>Compact graphical command shell; all commands delegate to Main.</summary>
public partial class PlayerControls : CanvasLayer
{
    private Main _main = null!;
    private CampaignSidebar _sidebar = null!;
    private PanelContainer _topBar = null!;
    private PanelContainer _dock = null!;
    private PanelContainer _statusPanel = null!;
    private Label _identity = null!;
    private Label _date = null!;
    private Label _credits = null!;
    private Label _industry = null!;
    private Label _science = null!;
    private Label _selection = null!;
    private Label _statusLabel = null!;
    private Label _speed = null!;
    private Button _pauseButton = null!;
    private OptionButton _speedSelector = null!;
    private Button _developerTools = null!;
    private ProjectCard _research = null!;
    private ProjectCard _construction = null!;
    private ProjectCard _shipyard = null!;
    private double _refreshTimer;

    public override void _Ready()
    {
        _main = GetParent() as Main ?? throw new InvalidOperationException("PlayerControls must be a child of Main.");
        _sidebar = _main.GetNode<CampaignSidebar>("CampaignSidebar");
        Layer = 6;
        BuildTopBar();
        BuildActionDock();
        _research = BuildProject("research", "RESEARCH", VisualIconLibrary.Research,
            "Next Research", _main.UiCycleResearch, "Start Research", _main.UiStartResearch);
        _construction = BuildProject("industry", "CONSTRUCTION", VisualIconLibrary.Construction,
            "Next Build", _main.UiCycleConstruction, "Start Build", _main.UiStartConstruction);
        _shipyard = BuildProject("ships", "SHIPYARD", VisualIconLibrary.NavShips,
            "Next Ship", _main.UiCycleShipDesign, "Build / Queue Ship", _main.UiBuildShip);
        BuildCampaignMenu();
        GetViewport().SizeChanged += UpdateBounds;
        UpdateBounds();
        RefreshState();
    }

    public override void _ExitTree() => GetViewport().SizeChanged -= UpdateBounds;

    public override void _Process(double delta)
    {
        _refreshTimer += delta;
        if (_refreshTimer < 0.2) return;
        _refreshTimer = 0;
        RefreshState();
    }

    private void BuildTopBar()
    {
        _topBar = new PanelContainer { Name = "ResourceBar", MouseFilter = Control.MouseFilterEnum.Stop };
        VisualUi.ContainPointerInput(_topBar);
        _topBar.AddThemeStyleboxOverride("panel", VisualUi.Surface(margin: 10));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 20);
        _topBar.AddChild(row);
        var identity = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        identity.AddThemeConstantOverride("separation", 0);
        _identity = VisualUi.Text("PLAYER MODE", 15);
        _identity.Name = "CampaignModeBadge";
        _identity.MouseFilter = Control.MouseFilterEnum.Pass;
        _date = VisualUi.Text("", 11, VisualUi.Muted);
        _date.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        identity.AddChild(_identity);
        identity.AddChild(_date);
        row.AddChild(identity);
        _credits = AddResource(row, "CREDITS", VisualIconLibrary.Credits, VisualUi.Gold);
        _credits.CustomMinimumSize = new Vector2(142, 0);
        _credits.AddThemeFontSizeOverride("font_size", 14);
        _industry = AddResource(row, "INDUSTRY", VisualIconLibrary.Industry, VisualUi.Accent);
        _science = AddResource(row, "SCIENCE", VisualIconLibrary.Science, new Color("b4a0e4"));
        var time = new HBoxContainer();
        time.AddThemeConstantOverride("separation", 3);
        _pauseButton = VisualUi.Button("", "Pause or resume the simulation. Keyboard: Space.", _main.UiTogglePause, VisualIconLibrary.Pause);
        _pauseButton.CustomMinimumSize = new Vector2(36, 36);
        time.AddChild(_pauseButton);
        var speedSelector = new OptionButton { TooltipText = "Simulation speed. Player: 1–4×. Developer also allows 24×.", CustomMinimumSize = new Vector2(70, 36) };
        _speedSelector = speedSelector;
        speedSelector.Name = "SimulationSpeed";
        speedSelector.AddItem("1×", 1);
        speedSelector.AddItem("2×", 2);
        speedSelector.AddItem("3×", 3);
        speedSelector.AddItem("4×", 4);
        speedSelector.AddItem("24× Developer", 24);
        speedSelector.ItemSelected += index =>
        {
            var id = speedSelector.GetItemId((int)index);
            if (id == 24) _main.UiResumeDemoSpeed(); else _main.UiSetSpeed(id);
        };
        time.AddChild(speedSelector);
        _speed = VisualUi.Text("", 11, VisualUi.Muted);
        _speed.CustomMinimumSize = new Vector2(72, 0);
        time.AddChild(_speed);
        row.AddChild(time);
        AddChild(_topBar);
    }

    private static Label AddResource(Container row, string name, Texture2D icon, Color color)
    {
        var group = new HBoxContainer();
        group.AddThemeConstantOverride("separation", 7);
        group.AddChild(VisualUi.Icon(icon, 25));
        var values = new VBoxContainer();
        values.AddThemeConstantOverride("separation", 0);
        values.AddChild(VisualUi.Text(name, 9, VisualUi.Muted));
        var amount = VisualUi.Text("0", 17, color);
        amount.CustomMinimumSize = new Vector2(98, 0);
        amount.MouseFilter = Control.MouseFilterEnum.Pass;
        values.AddChild(amount);
        group.AddChild(values);
        row.AddChild(group);
        return amount;
    }

    private void BuildActionDock()
    {
        _dock = new PanelContainer { Name = "MapToolbar", MouseFilter = Control.MouseFilterEnum.Stop };
        VisualUi.ContainPointerInput(_dock);
        _dock.AddThemeStyleboxOverride("panel", VisualUi.Surface(margin: 9));
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 5);
        _dock.AddChild(body);
        _selection = VisualUi.Text("SELECT A STAR", 12, VisualUi.Accent);
        _selection.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        body.AddChild(_selection);
        var actions = VisualUi.Actions(body);
        actions.AddChild(VisualUi.Button("Home", "Select and center your home star.", _main.UiSelectHomeSystem, VisualIconLibrary.NavHome));
        actions.AddChild(VisualUi.Button("Send Scout", "Send your scout to the selected star for reconnaissance.", _main.UiSendScout, VisualIconLibrary.Scout));
        actions.AddChild(VisualUi.Button("Send Science", "Send your science vessel to survey the selected star.", _main.UiSendScience, VisualIconLibrary.ScienceVessel));
        actions.AddChild(VisualUi.Button("Open System", "Inspect known orbits after reconnaissance.", _main.UiOpenSelectedSystem, VisualIconLibrary.NavSystem));
        actions.AddChild(VisualUi.Button("Back to Region", "Return from orbital view to the star map.", _main.UiReturnToRegion, VisualIconLibrary.NavBack));
        actions.AddChild(VisualUi.Button("Inspect", "Show what your civilization knows about the selected star.", () => _sidebar.ShowSection("inspection"), VisualIconLibrary.Info));
        var zoomIn = VisualUi.Button("", "Zoom toward the selected star or world. Wheel: zoom at the pointer.", _main.UiZoomIn, VisualIconLibrary.NavZoomIn);
        zoomIn.Name = "MapZoomIn";
        actions.AddChild(zoomIn);
        var zoomOut = VisualUi.Button("", "Zoom outward through planet, system, region and galaxy views.", _main.UiZoomOut, VisualIconLibrary.NavZoomOut);
        zoomOut.Name = "MapZoomOut";
        actions.AddChild(zoomOut);
        AddChild(_dock);
        _statusPanel = new PanelContainer { Name = "CommandFeedback" };
        VisualUi.ContainPointerInput(_statusPanel);
        _statusPanel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        _statusLabel = VisualUi.Text("", 12, VisualUi.Muted);
        _statusLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _statusLabel.MouseFilter = Control.MouseFilterEnum.Pass;
        _statusPanel.AddChild(_statusLabel);
        AddChild(_statusPanel);
    }

    private ProjectCard BuildProject(string section, string category, Texture2D icon, string nextLabel, Action next, string startLabel, Action start)
    {
        var panel = new PanelContainer { Name = category + "Card" };
        var card = new ProjectCard();
        panel.AddChild(card);
        card.Build(icon, category);
        var actions = card.Actions;
        actions.AddChild(VisualUi.Button(nextLabel, "Choose the next available option.", next));
        var begin = VisualUi.Button(startLabel, "Start the selected project. Its current requirements are checked when you click.", start, icon);
        begin.Modulate = VisualUi.Accent;
        actions.AddChild(begin);
        var details = VisualUi.Text(section == "ships" ? "Ships require warp capability and an Orbital Shipyard. A colony ship also carries colonists." : "Research and construction can run together. Choose an available project, then start it.", 12, VisualUi.Muted, wrap: true);
        card.AddChild(details);
        _sidebar.RegisterSection(section, panel);
        return card;
    }

    private void BuildCampaignMenu()
    {
        var panel = new PanelContainer { Name = "CampaignMenu" };
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 12);
        panel.AddChild(body);
        body.AddChild(VisualUi.Icon(VisualIconLibrary.NavGalaxy, 72));
        body.AddChild(VisualUi.Text("STELLAR CONTINUUM", 21));
        body.AddChild(VisualUi.Text(_main.UiBuildLabel, 12, VisualUi.Muted, wrap: true));
        body.AddChild(VisualUi.Button("Save", "Save this campaign in its own slot.", _main.UiSave, VisualIconLibrary.Save));
        var campaignMenu = VisualUi.Button("Campaign & modes", "Pause, resume, switch Player/Developer mode, or create a campaign.", _main.UiOpenMenu, VisualIconLibrary.NavMenu);
        campaignMenu.Name = "CampaignMenu";
        body.AddChild(campaignMenu);
        _developerTools = VisualUi.Button("Developer tools", "Explicit testing actions, available only in Developer mode.", _main.UiOpenDeveloperTools, VisualIconLibrary.Construction);
        _developerTools.Name = "DeveloperToolsShortcut";
        body.AddChild(_developerTools);
        body.AddChild(VisualUi.Button("New Player campaign", "Review confirmation before creating a fresh Player campaign.", _main.UiNewCampaign));
        body.AddChild(VisualUi.Button("Support Bundle", "Export game diagnostics and the available campaign save.", _main.UiExportDiagnostics, VisualIconLibrary.Support));
        body.AddChild(VisualUi.Text("Wheel: zoom · middle-drag: pan\nDouble-click: open star or focus world\nBackspace: previous view · Space: pause · F6: save", 12, VisualUi.Muted, wrap: true));
        _sidebar.RegisterSection("menu", panel);
    }

    private void RefreshState()
    {
        var state = _main.UiDashboard;
        _identity.Text = _main.UiModeLabel.ToUpperInvariant() + (_main.UiIsDeveloperMode && _main.UiDeveloperToolsUsed ? " · TOOLS USED" : "");
        _identity.Modulate = _main.UiIsDeveloperMode ? VisualUi.Gold : VisualUi.Accent;
        _identity.TooltipText = _main.UiIsDeveloperMode
            ? "Developer mode uses its own saves. " + (_main.UiDeveloperToolsUsed ? "Development actions have been used in this campaign." : "No development actions have been used in this campaign.")
            : "Player mode follows ordinary rules and uses a separate save from Developer campaigns.";
        _date.Text = state.Date + "  ·  " + state.CivilizationName;
        _credits.Text = $"{state.Credits:N0} C · {EarthDollarReference.Format(state.Credits)}";
        _industry.Text = state.Industry.ToString("N0");
        _science.Text = state.Science.ToString("N0");
        _credits.TooltipText = $"Stored credits: {state.Credits:N1} ({EarthDollarReference.Format(state.Credits)} 2050 Earth reference). Net cash flow after colony administration and active-fleet operations: {state.CreditsPerDay:+0.00;-0.00;0.00}/day. Construction, ships, surface buildings, and colony expeditions require authorization credits.";
        _industry.TooltipText = $"Stored industry: {state.Industry:N1}. Production: {state.IndustryPerDay:N2}/day before construction and shipbuilding spending.";
        _science.TooltipText = $"Stored science: {state.Science:N1}. Production: {state.SciencePerDay:N2}/day before research spending.";
        _selection.Text = $"{state.SelectedSystemName.ToUpperInvariant()}  /  {state.SelectedSurveyLabel}  ·  {_main.UiSpatialScaleLabel.ToUpperInvariant()}";
        _statusLabel.Text = _main.UiStatusMessage;
        _statusLabel.TooltipText = _main.UiStatusMessage;
        _speedSelector.SetItemDisabled(4, !_main.UiIsDeveloperMode);
        _developerTools.Disabled = !_main.UiIsDeveloperMode;
        _speedSelector.Select(_main.UiCurrentSpeed == Game.Simulation.SimulationClock.SpeedLevel.Demo ? 4 : Mathf.Clamp((int)_main.UiCurrentSpeed - 1, 0, 3));
        _pauseButton.Modulate = _main.UiIsPaused ? VisualUi.Gold : Colors.White;
        _pauseButton.TooltipText = _main.UiIsPaused ? "Resume simulation. Keyboard: Space." : "Pause simulation. Keyboard: Space.";
        _speed.Text = _main.UiIsPaused ? "PAUSED" : _main.UiIsDeveloperMode && _main.UiCurrentSpeed == Game.Simulation.SimulationClock.SpeedLevel.Demo ? "24× DEV" : _main.UiCurrentSpeed.ToString().ToUpperInvariant();
        _research.UpdateDisplay(state.Research);
        _construction.UpdateDisplay(state.Construction);
        _shipyard.UpdateDisplay(state.Shipyard);
        _research.UpdateChoices(_main.UiResearchChoices, _main.UiStartResearch);
        _construction.UpdateChoices(_main.UiConstructionChoices, _main.UiStartConstruction);
        _shipyard.UpdateChoices(_main.UiShipChoices, _main.UiBuildShip);
    }

    private void UpdateBounds()
    {
        var viewport = GetViewport().GetVisibleRect().Size;
        _topBar.Position = new Vector2(12, 12);
        _topBar.Size = new Vector2(viewport.X - 24, 56);
        _dock.Position = new Vector2(120, viewport.Y - 116);
        _dock.Size = new Vector2(Mathf.Max(1, viewport.X - 136), 76);
        _statusPanel.Position = new Vector2(126, viewport.Y - 31);
        _statusPanel.Size = new Vector2(Mathf.Max(1, viewport.X - 150), 24);
    }
}

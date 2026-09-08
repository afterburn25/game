using System;
using Godot;

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
        _topBar.AddThemeStyleboxOverride("panel", VisualUi.Surface(margin: 10));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 20);
        _topBar.AddChild(row);
        var identity = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        identity.AddThemeConstantOverride("separation", 0);
        _identity = VisualUi.Text("STELLAR CONTINUUM", 15);
        _date = VisualUi.Text("", 11, VisualUi.Muted);
        _date.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        identity.AddChild(_identity);
        identity.AddChild(_date);
        row.AddChild(identity);
        _credits = AddResource(row, "CREDITS", VisualIconLibrary.Credits, VisualUi.Gold);
        _industry = AddResource(row, "INDUSTRY", VisualIconLibrary.Industry, VisualUi.Accent);
        _science = AddResource(row, "SCIENCE", VisualIconLibrary.Science, new Color("b4a0e4"));
        var time = new HBoxContainer();
        time.AddThemeConstantOverride("separation", 3);
        _pauseButton = VisualUi.Button("", "Pause or resume the simulation. Keyboard: Space.", _main.UiTogglePause, VisualIconLibrary.Pause);
        _pauseButton.CustomMinimumSize = new Vector2(36, 36);
        time.AddChild(_pauseButton);
        var speedSelector = new OptionButton { TooltipText = "Simulation speed. Demo acceleration is available while playing the demo.", CustomMinimumSize = new Vector2(70, 36) };
        _speedSelector = speedSelector;
        speedSelector.AddItem("1×", 1);
        speedSelector.AddItem("2×", 2);
        speedSelector.AddItem("3×", 3);
        speedSelector.AddItem("4×", 4);
        speedSelector.AddItem("24× demo", 24);
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
        actions.AddChild(VisualUi.Button("", "Zoom in on the regional map.", _main.UiZoomIn, VisualIconLibrary.NavZoomIn));
        actions.AddChild(VisualUi.Button("", "Zoom out of the regional map.", _main.UiZoomOut, VisualIconLibrary.NavZoomOut));
        AddChild(_dock);
        _statusPanel = new PanelContainer { Name = "CommandFeedback" };
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
        var actions = VisualUi.Actions(card);
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
        body.AddChild(VisualUi.Button("Campaign & demo menu", "Pause and open Continue, Play Demo and campaign options.", _main.UiOpenMenu, VisualIconLibrary.NavMenu));
        body.AddChild(VisualUi.Button("New Game", "Review confirmation before starting a new campaign.", _main.UiNewCampaign));
        body.AddChild(VisualUi.Button("Support Bundle", "Export game diagnostics and the available campaign save.", _main.UiExportDiagnostics, VisualIconLibrary.Support));
        body.AddChild(VisualUi.Text("Map: wheel to zoom · middle-drag to pan\nSpace: pause · F6: save", 12, VisualUi.Muted, wrap: true));
        _sidebar.RegisterSection("menu", panel);
    }

    private void RefreshState()
    {
        var state = _main.UiDashboard;
        _identity.Text = "STELLAR CONTINUUM";
        _date.Text = state.Date + "  ·  " + state.CivilizationName;
        _credits.Text = state.Credits.ToString("N0");
        _industry.Text = state.Industry.ToString("N0");
        _science.Text = state.Science.ToString("N0");
        _credits.TooltipText = $"Credits: {state.Credits:N1} · {state.CreditsPerDay:+0.00;-0.00;0}/day";
        _industry.TooltipText = $"Industry: {state.Industry:N1} · {state.IndustryPerDay:+0.00;-0.00;0}/day";
        _science.TooltipText = $"Science: {state.Science:N1} · {state.SciencePerDay:+0.00;-0.00;0}/day";
        _selection.Text = $"{state.SelectedSystemName.ToUpperInvariant()}  /  {state.SelectedSurveyLabel}  ·  {(_main.UiIsSystemSpatialView ? "ORBITAL VIEW" : "REGIONAL MAP")}";
        _statusLabel.Text = _main.UiStatusMessage;
        _statusLabel.TooltipText = _main.UiStatusMessage;
        _speedSelector.SetItemDisabled(4, !_main.UiIsPlayableDemo);
        _speedSelector.Select(_main.UiCurrentSpeed == Game.Simulation.SimulationClock.SpeedLevel.Demo ? 4 : Mathf.Clamp((int)_main.UiCurrentSpeed - 1, 0, 3));
        _pauseButton.Modulate = _main.UiIsPaused ? VisualUi.Gold : Colors.White;
        _pauseButton.TooltipText = _main.UiIsPaused ? "Resume simulation. Keyboard: Space." : "Pause simulation. Keyboard: Space.";
        _speed.Text = _main.UiIsPaused ? "PAUSED" : _main.UiIsPlayableDemo && _main.UiCurrentSpeed == Game.Simulation.SimulationClock.SpeedLevel.Demo ? "24× DEMO" : _main.UiCurrentSpeed.ToString().ToUpperInvariant();
        _research.UpdateDisplay(state.Research.Title, state.Research.Detail, state.Research.Progress, state.Research.IsActive);
        _construction.UpdateDisplay(state.Construction.Title, state.Construction.Detail, state.Construction.Progress, state.Construction.IsActive);
        _shipyard.UpdateDisplay(state.Shipyard.Title, state.Shipyard.Detail, state.Shipyard.Progress, state.Shipyard.IsActive);
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

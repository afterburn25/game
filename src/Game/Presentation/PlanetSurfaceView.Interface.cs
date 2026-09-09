using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Construction;
using Godot;

namespace Game.Presentation;

public partial class PlanetSurfaceView
{
    private enum SurfaceDrawer { Closed, Build, Projects, Overview, Details }
    private SurfaceDrawer _drawerMode;
    private PanelContainer _drawer = null!;
    private Label _drawerTitle = null!;
    private Label _detailTitle = null!;
    private Label _detailBody = null!;
    private Label _detailImpact = null!;
    private Label _projectSummary = null!;
    private VBoxContainer _details = null!;
    private VBoxContainer _detailActions = null!;
    private VBoxContainer _projects = null!;
    private ProgressBar _progress = null!;
    private Button _buildToggle = null!;
    private Button _confirmUpgrade = null!;
    private bool _upgradePreview;
    private string _surfaceLayer = "Colony";
    private readonly Dictionary<int, Button> _projectButtons = new();
    private readonly Dictionary<string, Button> _layerButtons = new();
    public bool BuildMenuOpen => _drawerMode == SurfaceDrawer.Build && _drawer.Visible;
    public string SurfaceLayer => _surfaceLayer;
    public bool UpgradePreviewOpen => _upgradePreview;
    private double _centerElapsed = 1;
    private Vector3 _centerStartTarget;
    private float _centerStartDistance;
    private float _centerStartPitch;
    private bool _reducedMotion;

    private void BeginCenterHub()
    {
        if (InputBlocked) return;
        _centerStartTarget = _target; _centerStartDistance = _distance; _centerStartPitch = _pitch;
        _centerElapsed = 0;
        if (_reducedMotion) AdvanceCenterHub(1);
    }

    private void AdvanceCenterHub(double delta)
    {
        if (_centerElapsed >= .6) return;
        _centerElapsed = Math.Min(.6, _centerElapsed + Math.Min(delta, .1));
        if (_reducedMotion) _centerElapsed = .6;
        var t = (float)(_centerElapsed / .6);
        var eased = t * t * t * (t * (t * 6 - 15) + 10);
        _target = _centerStartTarget.Lerp(Vector3.Zero, eased);
        _distance = Mathf.Lerp(_centerStartDistance, 260, eased);
        _pitch = Mathf.Lerp(_centerStartPitch, .85f, eased);
    }

    private void BuildOverlay()
    {
        var header = new PanelContainer { Name = "SurfaceHeader" };
        VisualUi.ContainPointerInput(header);
        AddChild(header); _overlayPanels.Add(header);
        header.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        header.OffsetLeft = 16; header.OffsetRight = -16; header.OffsetTop = 12;
        header.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 12));
        var column = new VBoxContainer(); header.AddChild(column);
        var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 12); column.AddChild(row);
        var back = VisualUi.Button("← Orbit", "Return to orbit (Esc)", () => { if (!InputBlocked) ReturnToOrbit?.Invoke(); });
        back.Name = "SurfaceBack"; row.AddChild(back);
        _title = VisualUi.Text("COLONY SURFACE", 20, new Color("e9eeea"));
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis; row.AddChild(_title);
        var save = VisualUi.Button("Save", "Save campaign", () => { if (!InputBlocked) SaveRequested?.Invoke(); });
        save.Name = "SurfaceSave"; row.AddChild(save);
        var pause = VisualUi.Button("Pause / resume", "Pause or resume construction", () => { if (!InputBlocked) PauseRequested?.Invoke(); });
        pause.Name = "SurfacePause"; row.AddChild(pause);
        foreach (var option in new[] { (Level: 1, Multiplier: 1), (Level: 2, Multiplier: 2), (Level: 3, Multiplier: 3), (Level: 4, Multiplier: 8) })
        {
            var button = VisualUi.Button($"{option.Multiplier}×", "Simulation speed", () => { if (!InputBlocked) SpeedRequested?.Invoke(option.Level); });
            button.Name = "SurfaceSpeed" + option.Level; button.CustomMinimumSize = new(38, 38);
            row.AddChild(button); _speedButtons.Add(option.Level, button);
        }
        _resources = VisualUi.Text("", 14); _resources.Name = "SurfaceResources"; column.AddChild(_resources);
        var summary = new HBoxContainer(); column.AddChild(summary);
        _production = VisualUi.Text("", 12, VisualUi.Accent); _production.Name = "SurfaceProduction";
        _production.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _production.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis; summary.AddChild(_production);
        _time = VisualUi.Text("", 12, VisualUi.Gold); _time.Name = "SurfaceTime"; summary.AddChild(_time);

        var footer = new PanelContainer { Name = "SurfaceCommandBar" };
        VisualUi.ContainPointerInput(footer); AddChild(footer); _overlayPanels.Add(footer);
        footer.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide); footer.GrowVertical = GrowDirection.Begin;
        footer.OffsetLeft = 16; footer.OffsetRight = -16; footer.OffsetBottom = -12;
        footer.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 10));
        var footerColumn = new VBoxContainer(); footer.AddChild(footerColumn);
        var actions = new HBoxContainer(); actions.AddThemeConstantOverride("separation", 8); footerColumn.AddChild(actions);
        _buildToggle = VisualUi.Button("Build", "Open construction catalogue", () => ToggleDrawer(SurfaceDrawer.Build));
        _buildToggle.Name = "SurfaceBuildMenu"; actions.AddChild(_buildToggle);
        var projects = VisualUi.Button("Projects", "Inspect active sites and their remaining work", () => ToggleDrawer(SurfaceDrawer.Projects));
        projects.Name = "SurfaceProjects"; actions.AddChild(projects);
        var overview = VisualUi.Button("Overview", "Colony output, population and habitat support", () => ToggleDrawer(SurfaceDrawer.Overview));
        overview.Name = "SurfaceOverview"; actions.AddChild(overview);
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill }; actions.AddChild(spacer);
        foreach (var layer in new[] { "Colony", "Power", "Construction" })
        {
            var button = VisualUi.Button(layer, "Show " + layer.ToLowerInvariant() + " status on the surface", () =>
            { if (!InputBlocked) { _surfaceLayer = layer; RefreshSnapshot(); } });
            button.Name = "SurfaceLayer" + layer; actions.AddChild(button); _layerButtons.Add(layer, button);
        }
        var home = VisualUi.Button("Center hub", "Smoothly return to the colony hub", BeginCenterHub);
        home.Name = "SurfaceCenterHub"; actions.AddChild(home);
        var motion = new CheckButton { Name = "SurfaceReducedMotion", Text = "Less motion", TooltipText = "Stop decorative motion and make Center hub immediate" };
        motion.Toggled += enabled =>
        {
            if (InputBlocked) { motion.SetPressedNoSignal(_reducedMotion); return; }
            _reducedMotion = enabled;
            if (_settlementVisual is SurfaceSettlementVisual settlement) settlement.ReducedMotion = enabled;
            foreach (var visual in _buildings.Values) visual.ReducedMotion = enabled;
        };
        actions.AddChild(motion);
        _status = VisualUi.Text("", 13, VisualUi.Accent); _status.Name = "SurfaceStatus";
        _status.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis; footerColumn.AddChild(_status);
        _instructions = VisualUi.Text("", 12, VisualUi.Muted); footerColumn.AddChild(_instructions);

        _drawer = new PanelContainer { Name = "SurfaceBuildPalette", Visible = false };
        VisualUi.ContainPointerInput(_drawer); AddChild(_drawer); _overlayPanels.Add(_drawer);
        _drawer.SetAnchorsAndOffsetsPreset(LayoutPreset.RightWide);
        _drawer.OffsetLeft = -348; _drawer.OffsetRight = -16; _drawer.OffsetTop = 130; _drawer.OffsetBottom = -132;
        _drawer.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 12));
        var drawerColumn = new VBoxContainer(); drawerColumn.AddThemeConstantOverride("separation", 10); _drawer.AddChild(drawerColumn);
        var drawerHeading = new HBoxContainer(); drawerColumn.AddChild(drawerHeading);
        _drawerTitle = VisualUi.Text("Construction", 18, VisualUi.Accent); _drawerTitle.SizeFlagsHorizontal = SizeFlags.ExpandFill; drawerHeading.AddChild(_drawerTitle);
        var close = VisualUi.Button("Close", "Close details and cancel a placement preview", () =>
        { if (!InputBlocked) { CancelPlacement(); SelectExistingBuilding(null); SetDrawer(SurfaceDrawer.Closed); } });
        close.Name = "SurfaceDrawerClose"; drawerHeading.AddChild(close);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill };
        drawerColumn.AddChild(scroll);
        var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; scroll.AddChild(content);
        _palette = new GridContainer { Columns = 1, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _palette.AddThemeConstantOverride("v_separation", 8); content.AddChild(_palette);
        _details = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _details.AddThemeConstantOverride("separation", 12); content.AddChild(_details);
        _detailTitle = VisualUi.Text("", 18, new Color("edf0e7"), true); _detailTitle.Name = "SurfaceDetailTitle"; _details.AddChild(_detailTitle);
        _detailBody = VisualUi.Text("", 14, new Color("c5d4df"), true); _detailBody.Name = "SurfaceDetailBody"; _details.AddChild(_detailBody);
        _progress = new ProgressBar { CustomMinimumSize = new(0, 20), MaxValue = 100 }; _progress.Name = "SurfaceSiteProgress"; _details.AddChild(_progress);
        _detailImpact = VisualUi.Text("", 14, VisualUi.Accent, true); _detailImpact.Name = "SurfaceDetailImpact"; _details.AddChild(_detailImpact);
        _detailActions = new VBoxContainer(); _detailActions.AddThemeConstantOverride("separation", 6); drawerColumn.AddChild(_detailActions);
        _rotate = VisualUi.Button("Rotate 15°", "Rotate placement (R)", RotatePreview); _rotate.Name = "SurfaceRotate"; _detailActions.AddChild(_rotate);
        _cancel = VisualUi.Button("Cancel", "Cancel preview (Esc)", () => { if (!InputBlocked) { CancelPlacement(); SetDrawer(SurfaceDrawer.Build); } });
        _cancel.Name = "SurfaceCancel"; _detailActions.AddChild(_cancel);
        _upgrade = VisualUi.Button("Preview upgrade", "Review upgrade costs and power impact", () =>
        { if (!InputBlocked) { _upgradePreview = true; RefreshDrawer(); } });
        _upgrade.Name = "SurfaceUpgrade"; _detailActions.AddChild(_upgrade);
        _confirmUpgrade = VisualUi.Button("Confirm upgrade", "Apply the displayed upgrade through construction authority", UpgradeSelectedBuilding);
        _confirmUpgrade.Name = "SurfaceConfirmUpgrade"; _detailActions.AddChild(_confirmUpgrade);
        _remove = VisualUi.Button("Demolish", "Remove selected building", RemoveSelectedBuilding); _remove.Name = "SurfaceRemove"; _detailActions.AddChild(_remove);
        _projects = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _projects.AddThemeConstantOverride("separation", 8); content.AddChild(_projects);
        _projectSummary = VisualUi.Text("", 14, VisualUi.Muted, true); _projects.AddChild(_projectSummary);
        CancelPlacement(); SetDrawer(SurfaceDrawer.Closed);
    }

    private void ToggleDrawer(SurfaceDrawer mode)
    {
        if (InputBlocked) return;
        var next = _drawerMode == mode ? SurfaceDrawer.Closed : mode;
        CancelPlacement(); SelectExistingBuilding(null); SetDrawer(next);
    }

    private void SetDrawer(SurfaceDrawer mode)
    {
        _drawerMode = mode; _drawer.Visible = mode != SurfaceDrawer.Closed;
        RefreshDrawer();
    }

    private void RefreshDrawer()
    {
        if (_drawer is null) return;
        _palette.Visible = _drawerMode == SurfaceDrawer.Build;
        _details.Visible = _drawerMode is SurfaceDrawer.Details or SurfaceDrawer.Overview;
        _detailActions.Visible = _drawerMode == SurfaceDrawer.Details;
        _projects.Visible = _drawerMode == SurfaceDrawer.Projects;
        _buildToggle.Modulate = _drawerMode == SurfaceDrawer.Build ? VisualUi.Accent : Colors.White;
        foreach (var pair in _layerButtons) pair.Value.Modulate = pair.Key == _surfaceLayer ? VisualUi.Accent : Colors.White;
        _drawerTitle.Text = _drawerMode switch { SurfaceDrawer.Build => "Build a facility", SurfaceDrawer.Projects => "Active construction", SurfaceDrawer.Overview => "Colony overview", _ => "Facility details" };
        // Do not hide and re-show a button on each snapshot: Godot cancels a held press
        // when visibility changes between mouse-down and mouse-up.
        var selected = _snapshot?.Buildings.FirstOrDefault(item => item.Id == _selectedBuildingId);
        _progress.Visible = _drawerMode == SurfaceDrawer.Details && _selectedType is null && selected?.Complete == false;
        _confirmUpgrade.Visible = _drawerMode == SurfaceDrawer.Details && _selectedType is null &&
            _upgradePreview && selected?.CanUpgrade == true;
        if (_snapshot is not { } snapshot) return;
        if (_drawerMode == SurfaceDrawer.Overview)
        {
            _detailTitle.Text = snapshot.SpecializationName;
            _detailBody.Text = $"Population  {snapshot.PopulationMillions * 1_000_000:N0}\nEnvironment  {snapshot.SurfaceVisualClass}\nHabitat systems required  {snapshot.RequiredHabitatSystems}\nLocal support-cost reduction  {snapshot.HabitatSupportReduction:P0}\n\nSurface output\n{snapshot.IndustryPerDay:0.##} industry/day\n{snapshot.SciencePerDay:0.###} Effective Research Labs\n{snapshot.CreditsPerDay:0.00} credits/day\nUpkeep  {snapshot.UpkeepCreditsPerDay:0.00} credits/day";
            _detailImpact.Text = snapshot.SpecializationDescription;
            _rotate.Visible = _cancel.Visible = _remove.Visible = _upgrade.Visible = false;
        }
        else if (_drawerMode == SurfaceDrawer.Details)
        {
            var building = snapshot.Buildings.FirstOrDefault(item => item.Id == _selectedBuildingId);
            var typeId = _selectedType ?? building?.TypeId;
            var definition = typeId is null ? null : SurfaceBuildingCatalog.Find(typeId);
            if (definition is null) { SetDrawer(SurfaceDrawer.Closed); return; }
            _detailTitle.Text = definition.Name;
            _detailImpact.Modulate = Colors.White;
            if (_selectedType is not null || (_upgradePreview && building?.CanUpgrade == true))
            {
                var preview = SurfaceOrderPreview.Create(snapshot, definition.Id, _selectedType is null ? building?.Id : null);
                if (preview is null) return;
                _detailTitle.Text = _upgradePreview ? preview.ResultName : definition.Name;
                var duration = _upgradePreview ? "Immediate upgrade" : $"At least {Math.Ceiling(preview.MinimumDays):0} simulation days\nShared Industry availability can extend this.";
                var resultDefinition = _upgradePreview ? SurfaceBuildingCatalog.Find(definition.UpgradeTypeId!)! : definition;
                _detailBody.Text = $"{resultDefinition.Description}\n\n{preview.CreditCost:N0} credits {(_upgradePreview ? "now" : "to authorize")}\n{preview.IndustryCost:N0} industry {(_upgradePreview ? "now" : "during construction")}\n\n{duration}";
                _detailImpact.Text = $"After completion · current colony\nPower  {preview.Supply:0.##} supply / {preview.Demand:0.##} demand\nBalance  {preview.PowerBalance:+0.##;-0.##;0}\n{(preview.Powered ? "This facility can operate." : "This facility will need more power.")}\nOther unfinished sites are excluded.";
                _detailImpact.Modulate = Colors.White;
                _detailImpact.AddThemeColorOverride("font_color", preview.Powered ? VisualUi.Accent : VisualUi.Gold);
                _confirmUpgrade.Disabled = building?.CanAffordUpgrade != true;
                if (_upgradePreview) { _upgrade.Visible = _remove.Visible = false; }
            }
            else if (building is not null)
            {
                _detailBody.Text = definition.Description + "\n\n" + (building.Complete ? building.Powered ? "Operating · Powered" : "Offline · Insufficient power" : $"Under construction · {building.Progress:P0}\n{(1 - building.Progress) * building.Cost:0.#} industry remaining\nWork shares the available construction budget.");
                _progress.Visible = !building.Complete; _progress.Value = building.Progress * 100;
                _detailImpact.Text = !building.Complete ? "Output begins only after completion and sufficient power." : building.Powered ? "This facility contributes to the colony." : "Build or upgrade power generation to restore operation.";
                _detailImpact.Modulate = Colors.White;
                _detailImpact.AddThemeColorOverride("font_color", building.Complete && !building.Powered ? VisualUi.Gold : VisualUi.Accent);
            }
        }
        else if (_drawerMode == SurfaceDrawer.Projects)
        {
            var pending = snapshot.Buildings.Where(item => !item.Complete).OrderBy(item => item.Id).ToArray();
            _projectSummary.Text = pending.Length == 0 ? "No active construction. Choose Build to develop the colony." : $"{pending.Length} active site(s)\n{pending.Sum(item => (1 - item.Progress) * item.Cost):0.#} industry remaining\nSites share available Industry; this is not a serial queue.";
            foreach (var id in _projectButtons.Keys.Where(id => pending.All(item => item.Id != id)).ToArray())
            { var button = _projectButtons[id]; _projects.RemoveChild(button); button.QueueFree(); _projectButtons.Remove(id); }
            foreach (var item in pending)
            {
                if (!_projectButtons.TryGetValue(item.Id, out var button))
                {
                    var id = item.Id;
                    button = VisualUi.Button("", "Inspect this construction site", () =>
                    { if (!InputBlocked) SelectExistingBuilding(_snapshot?.Buildings.FirstOrDefault(building => building.Id == id)); });
                    button.Name = "SurfaceProject_" + id; _projects.AddChild(button); _projectButtons.Add(id, button);
                }
                button.Text = $"{item.Name} · {item.Progress:P0}";
            }
        }
    }
}

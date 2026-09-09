using System;
using System.Collections.Generic;
using Godot;

namespace Game.Presentation;

/// <summary>Map-first navigation with a dedicated operational page for each game department.</summary>
public partial class CampaignSidebar : CanvasLayer
{
    public const float RailWidth = 102;
    // The old narrow drawer made research, industry, fleets, and colonies feel like menus.
    // These are now proper operational pages that retain the map behind them.
    public const float DrawerWidth = 760;
    private PanelContainer _rail = null!;
    private PanelContainer _drawer = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _panels = null!;
    private Label _title = null!;
    private readonly Dictionary<string, PanelContainer> _sections = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Button> _navigation = new(StringComparer.Ordinal);
    public string? ActiveSection { get; private set; }
    public bool IsDrawerOpen => ActiveSection is not null;
    public event Action<string?>? SectionChanged;

    public override void _Ready()
    {
        Layer = 5;
        _rail = new PanelContainer { Name = "NavigationRail", MouseFilter = Control.MouseFilterEnum.Stop };
        VisualUi.ContainPointerInput(_rail);
        _rail.AddThemeStyleboxOverride("panel", VisualUi.Surface(margin: 6));
        var railScroll = new ScrollContainer
        {
            Name = "NavigationScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto, FollowFocus = true,
        };
        _rail.AddChild(railScroll);
        var railItems = new VBoxContainer { Name = "Items", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        railItems.AddThemeConstantOverride("separation", 2);
        railScroll.AddChild(railItems);
        AddChild(_rail);
        AddNavigation(railItems, "map", "Map", VisualIconLibrary.NavGalaxy, "Show the map and close the detail drawer.", CloseDrawer);
        AddNavigation(railItems, "economy", "Economy", VisualIconLibrary.Credits, "Review revenue, operating costs, and purchasing power.");
        AddNavigation(railItems, "research", "Research", VisualIconLibrary.Research, "Choose research and follow progress.");
        AddNavigation(railItems, "industry", "Industry", VisualIconLibrary.Construction, "Construct planetary and orbital infrastructure.");
        AddNavigation(railItems, "ships", "Ships", VisualIconLibrary.NavShips, "Choose a ship design and build your fleet.");
        AddNavigation(railItems, "explore", "Explore", VisualIconLibrary.Exploration, "Follow scout and science missions.");
        AddNavigation(railItems, "colonies", "Colonies", VisualIconLibrary.Colony, "Choose a surveyed world and settle with a colony ship.");
        AddNavigation(railItems, "logistics", "Logistics", VisualIconLibrary.Logistics, "Inspect supply and infrastructure connections.");
        AddNavigation(railItems, "relations", "Relations", VisualIconLibrary.Relations, "Review known diplomatic contacts.");
        AddNavigation(railItems, "menu", "Menu", VisualIconLibrary.NavMenu, "Save, switch Player or Developer mode, or manage your campaign.");

        _drawer = new PanelContainer { Name = "DetailDrawer", Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        VisualUi.ContainPointerInput(_drawer);
        _drawer.AddThemeStyleboxOverride("panel", VisualUi.Surface());
        var body = new VBoxContainer { Name = "Body" };
        body.AddThemeConstantOverride("separation", 14);
        _drawer.AddChild(body);
        var heading = new HBoxContainer { Name = "Header" };
        _title = VisualUi.Text("OPERATIONS", 22);
        _title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        heading.AddChild(_title);
        var close = VisualUi.Button("", "Close this operations page and return to the map.", CloseDrawer, VisualIconLibrary.NavClose);
        close.Name = "DrawerClose";
        heading.AddChild(close);
        body.AddChild(heading);
        _scroll = new ScrollContainer
        {
            Name = "DetailScroll", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto, FollowFocus = true,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        body.AddChild(_scroll);
        _panels = new VBoxContainer { Name = "Panels", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _scroll.AddChild(_panels);
        AddChild(_drawer);
        GetViewport().SizeChanged += UpdateBounds;
        UpdateBounds();
        UpdateNavigation();
    }

    public override void _ExitTree() => GetViewport().SizeChanged -= UpdateBounds;

    public void AddPanel(PanelContainer panel)
    {
        var section = panel.Name.ToString() switch
        {
            "DemoProgress" => "demo", "ExplorationPanel" => "explore",
            "SystemInspection" => "inspection", "LogisticsNetwork" => "logistics",
            "RelationsOverlay" => "relations", _ => panel.Name.ToString().ToLowerInvariant(),
        };
        RegisterSection(section, panel);
    }

    public void RegisterSection(string section, PanelContainer panel)
    {
        panel.Name = section switch { "economy" => "Economy", "research" => "Research", "industry" => "Industry", "ships" => "Ships", "explore" => "Exploration", "inspection" => "Inspection", "logistics" => "Logistics", "relations" => "Relations", "menu" => "Menu", "demo" => "Demo", _ => section };
        panel.MouseFilter = Control.MouseFilterEnum.Stop;
        // Section content belongs to DetailScroll: it must forward wheel input up to that
        // scroller. DetailDrawer, including its header and margins, is the final boundary.
        panel.MouseForcePassScrollEvents = true;
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        _sections.Add(section, panel);
        _panels.AddChild(panel);
        panel.Visible = ActiveSection == section || section == "explore" && ActiveSection == "colonies";
    }

    public void ShowSection(string section)
    {
        if (ActiveSection == section) { CloseDrawer(); return; }
        ActiveSection = section;
        _title.Text = section switch
        {
            "economy" => "ECONOMY", "industry" => "INDUSTRY", "ships" => "SHIPYARD", "inspection" => "SYSTEM INTELLIGENCE",
            "explore" => "EXPEDITION CONTROL", "colonies" => "COLONY SITES", "demo" => "YOUR FIRST COLONY",
            "menu" => "CAMPAIGN", _ => section.ToUpperInvariant(),
        };
        foreach (var entry in _sections)
            entry.Value.Visible = entry.Key == section || entry.Key == "explore" && section == "colonies";
        _drawer.Visible = true;
        _scroll.ScrollVertical = 0;
        UpdateNavigation();
        SectionChanged?.Invoke(section);
    }

    public void CloseDrawer()
    {
        ActiveSection = null;
        _drawer.Visible = false;
        foreach (var panel in _sections.Values) panel.Visible = false;
        UpdateNavigation();
        SectionChanged?.Invoke(null);
    }

    private void AddNavigation(Container parent, string key, string title, Texture2D icon, string tooltip, Action? action = null)
    {
        var button = VisualUi.Button(title, tooltip, action ?? (() => ShowSection(key)), icon);
        button.Name = "Nav" + title;
        button.ToggleMode = true;
        button.CustomMinimumSize = new Vector2(0, 40);
        // Theme padding adds to this minimum. Keep all ten destinations fully visible
        // without scrolling after a larger-window round trip at the supported 720px height.
        foreach (var state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled", "focus" })
        {
            var style = (StyleBoxFlat)button.GetThemeStylebox(state).Duplicate();
            style.ContentMarginTop = 3;
            style.ContentMarginBottom = 3;
            button.AddThemeStyleboxOverride(state, style);
        }
        button.IconAlignment = HorizontalAlignment.Center;
        button.VerticalIconAlignment = VerticalAlignment.Top;
        button.AddThemeFontSizeOverride("font_size", 11);
        parent.AddChild(button);
        _navigation.Add(key, button);
    }

    private void UpdateNavigation()
    {
        foreach (var pair in _navigation)
        {
            var selected = ActiveSection == pair.Key || ActiveSection is null && pair.Key == "map";
            pair.Value.SetPressedNoSignal(selected);
            pair.Value.Modulate = selected ? VisualUi.Accent : Colors.White;
        }
    }

    private void UpdateBounds()
    {
        var viewport = GetViewport().GetVisibleRect().Size;
        // The ten graphical destinations need the full height between the top bar and
        // viewport edge. Four pixels at the bottom retain a visible outer boundary.
        _rail.Position = new Vector2(12, 74);
        _rail.Size = new Vector2(RailWidth - 12, Mathf.Max(120, viewport.Y - 78));
        var availableWidth = Mathf.Max(240, viewport.X - RailWidth - 48);
        var pageWidth = Mathf.Min(DrawerWidth, availableWidth);
        _drawer.Position = new Vector2(RailWidth + 24 + Mathf.Max(0, (availableWidth - pageWidth) * 0.5f), 80);
        _drawer.Size = new Vector2(pageWidth, Mathf.Max(120, viewport.Y - 112));
    }
}

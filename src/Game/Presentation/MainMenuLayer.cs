using System;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Game.Simulation;

namespace Game.Presentation;

/// <summary>Campaign mode selection. Main owns switching, checkpoints and commands.</summary>
public partial class MainMenuLayer : CanvasLayer
{
    private Main _main = null!;
    private Control _overlay = null!;
    private Control _loading = null!;
    private Label _loadingStatus = null!;
    private ProgressBar _loadingProgress = null!;
    private ConfirmationDialog _confirmation = null!;
    private Label _saveError = null!;
    private Label _mode = null!;
    private Button _player = null!, _developer = null!, _tools = null!;
    private Button _resume = null!;
    private LineEdit _seed = null!;
    private Action? _confirmedStart;
    private SimulationClock.SpeedLevel _resumeSpeed = SimulationClock.SpeedLevel.Normal;
    private double _refresh;
    public bool IsBlockingGameplay => (_overlay?.IsVisibleInTree() ?? false) ||
        (_loading?.IsVisibleInTree() ?? false) || (_confirmation?.Visible ?? false);
    public bool HasLoadingPresentation => _loading is not null &&
        _loading.GetNodeOrNull<TextureRect>("SplashArtwork")?.Texture is { } texture &&
        texture.GetWidth() >= 1280 && texture.GetHeight() >= 720;
    public int LoadingPresentationShownCount { get; private set; }

    public override void _Ready()
    {
        _main = GetParent() as Main ?? throw new InvalidOperationException("MainMenuLayer must be a child of Main.");
        _overlay = new ColorRect { Color = VisualPalette.Canvas };
        VisualUi.ContainPointerInput(_overlay);
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var backdrop = new MainMenuBackdrop();
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _overlay.AddChild(backdrop);
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); _overlay.AddChild(center);
        var panel = new PanelContainer { Name = "CampaignModes", CustomMinimumSize = new(680, 0) };
        panel.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 20)); center.AddChild(panel);
        var content = new VBoxContainer(); content.AddThemeConstantOverride("separation", 10); panel.AddChild(content);
        var title = VisualUi.Text("STELLAR CONTINUUM", 28);
        title.HorizontalAlignment = HorizontalAlignment.Center; content.AddChild(title);
        var build = VisualUi.Text(_main.UiBuildLabel, 12, VisualUi.Muted);
        build.HorizontalAlignment = HorizontalAlignment.Center; content.AddChild(build);
        _mode = VisualUi.Text("PLAYER MODE", 13, VisualUi.Accent);
        _mode.Name = "CampaignModeLabel"; _mode.HorizontalAlignment = HorizontalAlignment.Center; content.AddChild(_mode);
        content.AddChild(new HSeparator());
        _resume = AddButton(content, "ResumeCampaign", "Resume campaign", "Return to the active campaign at its previous speed.", ContinueCampaign, VisualIconLibrary.NavGalaxy);
        var modes = new HBoxContainer(); modes.AddThemeConstantOverride("separation", 12); content.AddChild(modes);
        var player = ModeCard(modes, "PLAYER", "Play with ordinary resource, research and construction rules.", VisualIconLibrary.Colony);
        _player = AddButton(player, "ModePlayer", "Open Player", "Open your separate Player campaign; the current campaign is saved first.", SwitchToPlayer, VisualIconLibrary.NavGalaxy);
        AddButton(player, "NewPlayerCampaign", "New Player campaign", "Create a fresh Player campaign after confirmation.", RequestNewCampaign, VisualIconLibrary.NavHome);
        player.AddChild(VisualUi.Text("Player saves are separate from Developer saves.", 12, VisualUi.Muted, true));
        var developer = ModeCard(modes, "DEVELOPER", "Explore and test the game with explicit development tools.", VisualIconLibrary.Construction);
        _developer = AddButton(developer, "ModeDeveloper", "Open Developer", "Open your separate Developer campaign; the current campaign is saved first.", SwitchToDeveloper, VisualIconLibrary.Construction);
        var seedRow = new HBoxContainer(); developer.AddChild(seedRow);
        seedRow.AddChild(VisualUi.Text("New world seed", 12, VisualUi.Muted));
        _seed = new LineEdit { Name = "DeveloperSeed", Text = "20260908", PlaceholderText = "20260908",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MaxLength = 20, CustomMinimumSize = new(140, 34),
            TooltipText = "Whole-number seed for a fresh Developer world. Existing saves keep their own seed." };
        seedRow.AddChild(_seed);
        AddButton(developer, "NewDeveloperCampaign", "New Developer campaign", "Create a fresh Developer campaign with the entered seed after confirmation.", RequestDeveloperCampaign, VisualIconLibrary.NavHome);
        developer.AddChild(VisualUi.Text("Developer changes stay marked in Developer saves.", 12, VisualUi.Gold, true));
        var footer = new HBoxContainer(); footer.AddThemeConstantOverride("separation", 10); content.AddChild(footer);
        _tools = AddButton(footer, "DeveloperTools", "Developer tools", "Open explicit actions for the active Developer campaign.", OpenTools, VisualIconLibrary.Construction);
        AddButton(footer, "QuitCampaign", "Save and quit", "Save the active campaign in its own mode and exit.", _main.UiQuit, VisualIconLibrary.Save);
        _saveError = VisualUi.Text("", 13, new Color("efac92"), true);
        _saveError.Name = "CampaignMenuError"; _saveError.Visible = false; content.AddChild(_saveError);
        AddChild(_overlay);
        BuildLoadingPresentation();
        _confirmation = new ConfirmationDialog { Title = "Start a new campaign?", DialogAutowrap = true };
        _confirmation.Confirmed += ConfirmStart;
        _confirmation.Canceled += () => _confirmedStart = null;
        AddChild(_confirmation);
        GetViewport().GuiFocusChanged += KeepMenuFocus;
        _resume.GrabFocus();
        _main.UiResumeAtSpeed(SimulationClock.SpeedLevel.Paused);
    }

    public override void _ExitTree() => GetViewport().GuiFocusChanged -= KeepMenuFocus;

    private void KeepMenuFocus(Control focus)
    {
        if (focus is not null && IsBlockingGameplay && !_confirmation.Visible && !_overlay.IsAncestorOf(focus))
            _resume.GrabFocus();
    }

    public override void _Process(double delta)
    {
        _refresh += delta;
        if (_refresh < .2 || !IsBlockingGameplay) return;
        _refresh = 0;
        _mode.Text = _main.UiModeLabel.ToUpperInvariant() + (_main.UiIsDeveloperMode ? (_main.UiDeveloperToolsUsed ? " · TOOLS USED" : " · TOOLS UNUSED") : "");
        _mode.Modulate = _main.UiIsDeveloperMode ? VisualUi.Gold : VisualUi.Accent;
        _player.Text = _main.UiIsDeveloperMode ? (_main.UiHasPlayerSave ? "Resume Player" : "Start Player") : "Player active";
        _developer.Text = _main.UiIsDeveloperMode ? "Developer active" : (_main.UiHasDeveloperSave ? "Resume Developer" : "Start Developer");
        _tools.Disabled = !_main.UiIsDeveloperMode;
    }

    private void ContinueCampaign() { _overlay.Hide(); _main.UiResumeAtSpeed(_resumeSpeed); }
    public void ShowMenu()
    {
        if (_overlay.IsVisibleInTree()) return;
        _main.GetNodeOrNull<DeveloperToolsLayer>("DeveloperToolsLayer")?.Close();
        _resumeSpeed = _main.UiCurrentSpeed;
        _main.UiResumeAtSpeed(SimulationClock.SpeedLevel.Paused); _overlay.Show(); _resume.GrabFocus();
    }
    public void ShowSaveFailure(string message) { _saveError.Text = message; _saveError.Show(); }
    public void ClearSaveFailure() => _saveError.Hide();
    public void RequestNewCampaign()
    {
        ShowMenu(); _confirmedStart = _main.UiCreateNewCampaignConfirmed;
        _confirmation.DialogText = "Start a fresh Player campaign? The current campaign will be saved first. The previous Player save is kept as its backup; Developer saves stay separate.";
        _confirmation.PopupCentered(new(510, 185));
    }
    private void RequestDeveloperCampaign()
    {
        if (!long.TryParse(_seed.Text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var seed))
        { ShowSaveFailure("Enter a whole-number seed from −9223372036854775808 to 9223372036854775807."); _seed.GrabFocus(); return; }
        ShowMenu(); _confirmedStart = () => _main.UiCreateDeveloperCampaignConfirmed(seed);
        _confirmation.DialogText = $"Start a fresh Developer campaign with seed {seed}? The current campaign will be saved first. The previous Developer save is kept as its backup; Player saves stay separate. Tools run only when you choose them.";
        _confirmation.PopupCentered(new(510, 210));
    }
    private async void ConfirmStart()
    {
        var start = _confirmedStart; _confirmedStart = null;
        if (start is null || !_main.UiCheckpointBeforeCampaignSwitch()) return;
        await RunLoadingAsync("Generating a new 100-star campaign", () => { start(); return true; });
    }
    private async void SwitchToPlayer()
    {
        if (!_main.UiIsDeveloperMode) { ContinueCampaign(); return; }
        await RunLoadingAsync("Loading the Player campaign", _main.UiSwitchToPlayerMode);
    }
    private async void SwitchToDeveloper()
    {
        if (_main.UiIsDeveloperMode) { ContinueCampaign(); return; }
        await RunLoadingAsync("Loading the Developer campaign", _main.UiSwitchToDeveloperMode);
    }
    private void OpenTools()
    {
        if (!_main.UiIsDeveloperMode) return;
        ContinueCampaign(); _main.UiOpenDeveloperTools();
    }
    public override void _Input(InputEvent input)
    {
        if (!IsBlockingGameplay || !input.IsActionPressed("ui_cancel")) return;
        if (_confirmation.Visible) { _confirmation.Hide(); _confirmedStart = null; } else ContinueCampaign();
        GetViewport().SetInputAsHandled();
    }
    private static VBoxContainer ModeCard(Container parent, string title, string description, Texture2D icon)
    {
        var panel = new PanelContainer { CustomMinimumSize = new(300, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 12)); parent.AddChild(panel);
        var content = new VBoxContainer(); content.AddThemeConstantOverride("separation", 8); panel.AddChild(content);
        var heading = new HBoxContainer(); content.AddChild(heading);
        heading.AddChild(VisualUi.Icon(icon, 27)); heading.AddChild(VisualUi.Text(title, 18));
        content.AddChild(VisualUi.Text(description, 13, VisualUi.Muted, true)); return content;
    }
    private static Button AddButton(Container parent, string name, string text, string tooltip, Action action, Texture2D icon)
    {
        var button = VisualUi.Button(text, tooltip, action, icon);
        button.Name = name; button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.CustomMinimumSize = new(0, 40); parent.AddChild(button); return button;
    }

    private void BuildLoadingPresentation()
    {
        _loading = new Control { Name = "CampaignLoading", Visible = false };
        _loading.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        VisualUi.ContainPointerInput(_loading);
        var artwork = new TextureRect
        {
            Name = "SplashArtwork",
            Texture = GD.Load<Texture2D>("res://assets/visual/loading/stellar-continuum-splash.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        artwork.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _loading.AddChild(artwork);
        var veil = new ColorRect { Color = new Color(0.003f, .008f, .018f, .48f), MouseFilter = Control.MouseFilterEnum.Ignore };
        veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _loading.AddChild(veil);
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        margin.OffsetLeft = 72; margin.OffsetRight = -72; margin.OffsetTop = -210; margin.OffsetBottom = -54;
        _loading.AddChild(margin);
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 18));
        margin.AddChild(panel);
        var column = new VBoxContainer(); column.AddThemeConstantOverride("separation", 10); panel.AddChild(column);
        column.AddChild(VisualUi.Text("STELLAR CONTINUUM", 30, Colors.White));
        _loadingStatus = VisualUi.Text("Preparing campaign", 15, VisualUi.Accent);
        _loadingStatus.Name = "LoadingStatus"; column.AddChild(_loadingStatus);
        _loadingProgress = new ProgressBar
        {
            Name = "LoadingProgress", MinValue = 0, MaxValue = 100, Value = 0,
            ShowPercentage = false, CustomMinimumSize = new Vector2(0, 8),
        };
        column.AddChild(_loadingProgress);
        column.AddChild(VisualUi.Text("Building the galaxy, civilization state and navigable views", 11, VisualUi.Muted));
        AddChild(_loading);
    }

    private async Task RunLoadingAsync(string status, Func<bool> action)
    {
        _loadingStatus.Text = status;
        _loadingProgress.Value = 18;
        _overlay.Hide();
        _loading.Show();
        LoadingPresentationShownCount++;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _loadingProgress.Value = 52;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!action())
        {
            _loading.Hide();
            _overlay.Show();
            _resume.GrabFocus();
            return;
        }
        _loadingStatus.Text = "Campaign ready";
        _loadingProgress.Value = 100;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _loading.Hide();
    }
}

using System;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Game.Simulation;
using Game.Simulation.Generation;
using Game.Simulation.Species;

namespace Game.Presentation;

/// <summary>Campaign mode selection. Main owns switching, checkpoints and commands.</summary>
public partial class MainMenuLayer : CanvasLayer
{
    private Main _main = null!;
    private Control _overlay = null!;
    private PanelContainer _campaignModes = null!;
    private Control _newGameSelection = null!;
    private Control _sandboxSetup = null!;
    private LineEdit _sandboxSeed = null!;
    private Label _sandboxSeedResolved = null!;
    private Label _sandboxSummary = null!;
    private SandboxGalaxyPreview _sandboxPreview = null!;
    private OptionButton _sandboxSpecies = null!;
    private TextureRect _sandboxSpeciesPortrait = null!;
    private Control _loading = null!;
    private Control _audioSettings = null!;
    private HSlider _masterVolume = null!, _musicVolume = null!, _sfxVolume = null!;
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
    public bool IsLoadingCampaign => _loading?.IsVisibleInTree() ?? false;
    public bool IsNewGameSelectionVisible => _newGameSelection?.IsVisibleInTree() ?? false;
    public bool IsSandboxSetupVisible => _sandboxSetup?.IsVisibleInTree() ?? false;
    public bool IsAudioSettingsVisible => _audioSettings?.IsVisibleInTree() ?? false;

    public override void _Ready()
    {
        _main = GetParent() as Main ?? throw new InvalidOperationException("MainMenuLayer must be a child of Main.");
        AudioDirector.Instance?.SetMenuContext(true);
        _overlay = new ColorRect { Color = VisualPalette.Canvas };
        VisualUi.ContainPointerInput(_overlay);
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var backdrop = new MainMenuBackdrop();
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _overlay.AddChild(backdrop);
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        center.OffsetRight = -260;
        _overlay.AddChild(center);
        _campaignModes = new PanelContainer { Name = "CampaignModes", CustomMinimumSize = new(680, 0) };
        _campaignModes.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 20)); center.AddChild(_campaignModes);
        var content = new VBoxContainer(); content.AddThemeConstantOverride("separation", 10); _campaignModes.AddChild(content);
        var title = VisualUi.Text("STELLAR CONTINUUM", 34);
        title.HorizontalAlignment = HorizontalAlignment.Center; content.AddChild(title);
        var subtitle = VisualUi.Text("THE FIRST LIGHT OF AN INTERSTELLAR AGE", 11, VisualUi.Gold);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center; content.AddChild(subtitle);
        var build = VisualUi.Text(_main.UiBuildLabel, 12, VisualUi.Muted);
        build.HorizontalAlignment = HorizontalAlignment.Center; content.AddChild(build);
        _mode = VisualUi.Text("PLAYER MODE", 13, VisualUi.Accent);
        _mode.Name = "CampaignModeLabel"; _mode.HorizontalAlignment = HorizontalAlignment.Center; content.AddChild(_mode);
        content.AddChild(new HSeparator());
        _resume = AddButton(content, "ResumeCampaign", "Resume campaign", "Return to the active campaign at its previous speed.", ContinueCampaign, VisualIconLibrary.NavGalaxy);
        var modes = new HBoxContainer(); modes.AddThemeConstantOverride("separation", 12); content.AddChild(modes);
        var player = ModeCard(modes, "PLAYER", "Play with ordinary resource, research and construction rules.", VisualIconLibrary.Colony);
        _player = AddButton(player, "ModePlayer", "Open Player", "Open your separate Player campaign; the current campaign is saved first.", SwitchToPlayer, VisualIconLibrary.NavGalaxy);
        AddButton(player, "NewPlayerCampaign", "New Game", "Choose the type of Player campaign to begin.", RequestNewCampaign, VisualIconLibrary.NavHome);
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
        AddButton(footer, "AudioSettings", "Settings / Audio", "Adjust master, music and sound-effect volume.", ShowAudioSettings, VisualIconLibrary.Info);
        AddButton(footer, "QuitCampaign", "Save and quit", "Save the active campaign in its own mode and exit.", _main.UiQuit, VisualIconLibrary.Save);
        _saveError = VisualUi.Text("", 13, new Color("efac92"), true);
        _saveError.Name = "CampaignMenuError"; _saveError.Visible = false; content.AddChild(_saveError);
        BuildNewGameSelection();
        BuildSandboxSetup();
        BuildAudioSettings();
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

    private void ContinueCampaign()
    {
        _newGameSelection.Hide();
        _sandboxSetup.Hide();
        _campaignModes.Show();
        _audioSettings.Hide();
        _overlay.Hide();
        AudioDirector.Instance?.SetMenuContext(false);
        _main.UiResumeAtSpeed(_resumeSpeed);
    }
    public void ShowMenu()
    {
        if (_overlay.IsVisibleInTree()) return;
        _main.GetNodeOrNull<DeveloperToolsLayer>("DeveloperToolsLayer")?.Close();
        _resumeSpeed = _main.UiCurrentSpeed;
        _main.UiResumeAtSpeed(SimulationClock.SpeedLevel.Paused);
        _newGameSelection.Hide();
        _sandboxSetup.Hide();
        _audioSettings.Hide();
        _campaignModes.Show();
        _overlay.Show();
        AudioDirector.Instance?.SetMenuContext(true);
        _resume.GrabFocus();
    }
    public void ShowSaveFailure(string message) { _saveError.Text = message; _saveError.Show(); }
    public void ClearSaveFailure() => _saveError.Hide();
    public void RequestNewCampaign()
    {
        ShowMenu();
        _campaignModes.Hide();
        _sandboxSetup.Hide();
        _newGameSelection.Show();
        _newGameSelection.GetNode<Button>("NewGamePanel/Body/Choices/SandboxCampaignOption").GrabFocus();
    }
    private void RequestSandboxCampaign()
    {
        _newGameSelection.Hide();
        _sandboxSetup.Show();
        if (string.IsNullOrWhiteSpace(_sandboxSeed.Text)) RandomizeSandboxSeed();
        _sandboxSeed.GrabFocus();
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
        if (_confirmation.Visible) { _confirmation.Hide(); _confirmedStart = null; }
        else if (_newGameSelection.Visible)
        {
            _newGameSelection.Hide();
            _campaignModes.Show();
            _resume.GrabFocus();
        }
        else if (_sandboxSetup.Visible)
        {
            _sandboxSetup.Hide();
            _newGameSelection.Show();
            _newGameSelection.GetNode<Button>("NewGamePanel/Body/Choices/SandboxCampaignOption").GrabFocus();
        }
        else if (_audioSettings.Visible)
        {
            _audioSettings.Hide();
            _campaignModes.Show();
            _resume.GrabFocus();
        }
        else ContinueCampaign();
        GetViewport().SetInputAsHandled();
    }

    private void BuildSandboxSetup()
    {
        _sandboxSetup = new CenterContainer { Name = "SandboxSetup", Visible = false };
        _sandboxSetup.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var panel = new PanelContainer { Name = "SandboxSetupPanel", CustomMinimumSize = new Vector2(720, 0) };
        panel.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 20));
        _sandboxSetup.AddChild(panel);
        var body = new VBoxContainer { Name = "Body", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 7); panel.AddChild(body);
        var heading = new HBoxContainer(); body.AddChild(heading);
        var title = VisualUi.Text("CONFIGURE SANDBOX", 26); title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; heading.AddChild(title);
        var back = VisualUi.Button("Back", "Return to game type selection.", () =>
        {
            _sandboxSetup.Hide(); _newGameSelection.Show();
            _newGameSelection.GetNode<Button>("NewGamePanel/Body/Choices/SandboxCampaignOption").GrabFocus();
        }, VisualIconLibrary.NavBack);
        back.Name = "SandboxSetupBack"; heading.AddChild(back);
        body.AddChild(VisualUi.Text("Create a reproducible Milky Way-inspired 100-system campaign.", 13, VisualUi.Muted));
        _sandboxPreview = new SandboxGalaxyPreview { Name = "SandboxGalaxyPreview", CustomMinimumSize = new Vector2(0, 125) };
        body.AddChild(_sandboxPreview);

        var speciesPanel = new PanelContainer(); speciesPanel.AddThemeStyleboxOverride("panel", VisualUi.Surface(true, 12)); body.AddChild(speciesPanel);
        var speciesRow = new HBoxContainer(); speciesRow.AddThemeConstantOverride("separation", 12); speciesPanel.AddChild(speciesRow);
        _sandboxSpeciesPortrait = new TextureRect
        {
            Name = "SandboxSpeciesPortrait", CustomMinimumSize = new Vector2(62, 62),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        speciesRow.AddChild(_sandboxSpeciesPortrait);
        var speciesBody = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        speciesBody.AddChild(VisualUi.Text("PLAYABLE SPECIES", 13, VisualUi.Gold));
        _sandboxSpecies = new OptionButton { Name = "SandboxSpecies", CustomMinimumSize = new Vector2(0, 34) };
        foreach (var species in SpeciesCatalog.All) _sandboxSpecies.AddItem(species.DisplayName);
        _sandboxSpecies.ItemSelected += _ => RefreshSandboxSetup();
        speciesBody.AddChild(_sandboxSpecies);
        speciesBody.AddChild(VisualUi.Text("Humans begin on Earth in Sol. Every other species begins on its own naturally viable homeworld; Humanity still occupies Earth.", 11, VisualUi.Muted, true));
        speciesRow.AddChild(speciesBody);

        var seedPanel = new PanelContainer(); seedPanel.AddThemeStyleboxOverride("panel", VisualUi.Surface(true, 12)); body.AddChild(seedPanel);
        var seedBody = new VBoxContainer(); seedBody.AddThemeConstantOverride("separation", 7); seedPanel.AddChild(seedBody);
        seedBody.AddChild(VisualUi.Text("GALAXY SEED", 13, VisualUi.Gold));
        _sandboxSeed = new LineEdit { Name = "SandboxSeed", PlaceholderText = "Number or memorable text", MaxLength = 80, CustomMinimumSize = new Vector2(0, 34) };
        _sandboxSeed.TextChanged += _ => RefreshSandboxSetup(); seedBody.AddChild(_sandboxSeed);
        _sandboxSeedResolved = VisualUi.Text("", 11, VisualUi.Muted); _sandboxSeedResolved.Name = "ResolvedSeed"; seedBody.AddChild(_sandboxSeedResolved);
        var seedActions = new HBoxContainer(); seedActions.AddThemeConstantOverride("separation", 8); seedBody.AddChild(seedActions);
        AddButton(seedActions, "RandomizeSandboxSeed", "Randomize", "Generate a fresh seed.", RandomizeSandboxSeed, VisualIconLibrary.NavGalaxy);
        AddButton(seedActions, "CopySandboxSetup", "Copy setup", "Copy the reproducible setup to the clipboard.", CopySandboxSetup, VisualIconLibrary.Save);
        AddButton(seedActions, "RestoreSandboxDefaults", "Restore defaults", "Restore the recommended setup and generate a fresh seed.", RandomizeSandboxSeed, VisualIconLibrary.NavHome);

        var settingsPanel = new PanelContainer();
        settingsPanel.AddThemeStyleboxOverride("panel", VisualUi.Surface(true, 9));
        body.AddChild(settingsPanel);
        var settings = new VBoxContainer(); settings.AddThemeConstantOverride("separation", 3); settingsPanel.AddChild(settings);
        settings.AddChild(VisualUi.Text("100 SYSTEMS  ·  BARRED SPIRAL  ·  BALANCED STARS  ·  COMMON PLANETARY SYSTEMS", 11, VisualUi.Gold));
        settings.AddChild(VisualUi.Text("UNCOMMON HABITABLE WORLDS  ·  2 NEARBY CANDIDATES  ·  5 RIVALS  ·  STANDARD", 11, VisualUi.Muted));
        _sandboxSummary = VisualUi.Text("", 12, VisualUi.Accent, true);
        _sandboxSummary.Name = "SandboxSummary";
        _sandboxSummary.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _sandboxSummary.CustomMinimumSize = new Vector2(0, 28);
        body.AddChild(_sandboxSummary);
        body.AddChild(VisualUi.Text("Advanced generation controls will unlock after the balanced 100-system profile is validated.", 11, VisualUi.Muted, true));
        AddButton(body, "StartConfiguredSandbox", "Generate campaign", "Create this reproducible Player campaign.", StartConfiguredSandbox, VisualIconLibrary.NavGalaxy);
        _overlay.AddChild(_sandboxSetup);
        RandomizeSandboxSeed();
    }

    private void RandomizeSandboxSeed()
    {
        _sandboxSeed.Text = CampaignSeed.CreateRandomNumericText();
        RefreshSandboxSetup();
    }

    private void RefreshSandboxSetup()
    {
        try
        {
            var entered = _sandboxSeed.Text.Trim();
            var internalSeed = CampaignSeed.Parse(entered);
            var metadata = GalaxyGenerationMetadata.Standard100(entered, internalSeed);
            metadata = metadata with { PlayerSpeciesId = SelectedSandboxSpeciesId() };
            _sandboxSeedResolved.Text = $"Internal seed: {internalSeed}";
            _sandboxSummary.Text = metadata.SpoilerFreeSummary;
            _sandboxPreview.SetSeed(internalSeed);
            _saveError.Hide();
        }
        catch (ArgumentException ex)
        {
            _sandboxSeedResolved.Text = ex.Message;
            _sandboxSummary.Text = "Enter a seed to preview this campaign setup.";
        }
    }

    private void CopySandboxSetup()
    {
        try
        {
            var entered = _sandboxSeed.Text.Trim();
            var metadata = GalaxyGenerationMetadata.Standard100(entered, CampaignSeed.Parse(entered), SelectedSandboxSpeciesId());
            DisplayServer.ClipboardSet($"Stellar Continuum Sandbox | Seed: {entered} | {metadata.SpoilerFreeSummary}");
            _sandboxSeedResolved.Text = $"Copied setup · Internal seed: {metadata.InternalSeed}";
        }
        catch (ArgumentException) { RefreshSandboxSetup(); _sandboxSeed.GrabFocus(); }
    }

    private void StartConfiguredSandbox()
    {
        var entered = _sandboxSeed.Text.Trim();
        try { _ = CampaignSeed.Parse(entered); }
        catch (ArgumentException ex) { _sandboxSeedResolved.Text = ex.Message; _sandboxSeed.GrabFocus(); return; }
        var species = SpeciesCatalog.Get(SelectedSandboxSpeciesId());
        _confirmedStart = () => _main.UiCreateNewCampaignConfirmed(entered, species.Id);
        _confirmation.DialogText = $"Generate a fresh 100-system {species.DisplayName} Player campaign with seed ‘{entered}’? The current Player campaign will be checkpointed first.";
        _confirmation.PopupCentered(new(560, 190));
    }

    private string SelectedSandboxSpeciesId()
    {
        var species = SpeciesCatalog.All;
        var index = Math.Clamp(_sandboxSpecies?.Selected ?? 0, 0, species.Count - 1);
        var selected = species[index];
        if (_sandboxSpeciesPortrait is not null)
            _sandboxSpeciesPortrait.Texture = VisualIconLibrary.Get(CivilizationArtworkLibrary.PathForSpecies(selected.Id));
        return selected.Id;
    }

    private void BuildNewGameSelection()
    {
        _newGameSelection = new CenterContainer { Name = "NewGameSelection", Visible = false };
        _newGameSelection.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var panel = new PanelContainer { Name = "NewGamePanel", CustomMinimumSize = new Vector2(760, 0) };
        panel.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 20));
        _newGameSelection.AddChild(panel);
        var body = new VBoxContainer { Name = "Body" };
        body.AddThemeConstantOverride("separation", 12); panel.AddChild(body);
        var heading = new HBoxContainer(); body.AddChild(heading);
        var title = VisualUi.Text("CHOOSE YOUR GAME", 26); title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        heading.AddChild(title);
        var back = VisualUi.Button("Back", "Return to campaign options.", () =>
        {
            _newGameSelection.Hide(); _campaignModes.Show(); _resume.GrabFocus();
        }, VisualIconLibrary.NavBack);
        back.Name = "NewGameBack"; heading.AddChild(back);
        body.AddChild(VisualUi.Text("Select how your civilization's journey will begin.", 13, VisualUi.Muted));
        var choices = new HBoxContainer { Name = "Choices" };
        choices.AddThemeConstantOverride("separation", 14); body.AddChild(choices);
        choices.AddChild(GameTypeCard("StoryCampaignOption", "STORY CAMPAIGN",
            "A guided narrative with authored characters, conflicts and discoveries.",
            "res://assets/visual/loading/stellar-continuum-splash.png", enabled: false, action: null));
        choices.AddChild(GameTypeCard("SandboxCampaignOption", "SANDBOX",
            "Build humanity's future freely in a generated 100-system sector.",
            "res://assets/visual/space/campaign-galaxy-four-arm-v1.png", enabled: true, RequestSandboxCampaign));
        _overlay.AddChild(_newGameSelection);
    }

    private static Button GameTypeCard(string name, string title, string description,
        string artworkPath, bool enabled, Action? action)
    {
        var button = new Button
        {
            Name = name, Disabled = !enabled, ClipContents = true,
            CustomMinimumSize = new Vector2(350, 310),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FocusMode = enabled ? Control.FocusModeEnum.All : Control.FocusModeEnum.None,
            TooltipText = enabled ? $"Begin {title}." : "Story Campaign is coming soon.",
        };
        if (action is not null) button.Pressed += action;
        AudioDirector.Bind(button);
        foreach (var state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
        {
            var style = VisualUi.Surface(highlighted: enabled && state is "hover" or "focus", margin: 10);
            style.BgColor = enabled ? new Color(.015f, .035f, .055f, .98f) : new Color(.025f, .028f, .032f, .98f);
            button.AddThemeStyleboxOverride(state, style);
        }
        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        column.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        column.OffsetLeft = 10; column.OffsetRight = -10; column.OffsetTop = 10; column.OffsetBottom = -10;
        column.AddThemeConstantOverride("separation", 9); button.AddChild(column);
        var artFrame = new Control { CustomMinimumSize = new Vector2(0, 190), MouseFilter = Control.MouseFilterEnum.Ignore };
        artFrame.ClipContents = true; column.AddChild(artFrame);
        var art = new TextureRect
        {
            Name = name + "Artwork", Texture = GD.Load<Texture2D>(artworkPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        art.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); artFrame.AddChild(art);
        if (!enabled)
        {
            var veil = new ColorRect { Color = new Color(.05f, .055f, .06f, .72f), MouseFilter = Control.MouseFilterEnum.Ignore };
            veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); artFrame.AddChild(veil);
            var comingSoon = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            comingSoon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); artFrame.AddChild(comingSoon);
            var banner = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            banner.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 10)); comingSoon.AddChild(banner);
            var label = VisualUi.Text("COMING SOON", 18, VisualUi.Gold); label.MouseFilter = Control.MouseFilterEnum.Ignore;
            banner.AddChild(label);
        }
        var modeTitle = VisualUi.Text(title, 20, enabled ? Colors.White : VisualUi.Muted);
        modeTitle.MouseFilter = Control.MouseFilterEnum.Ignore; column.AddChild(modeTitle);
        var detail = VisualUi.Text(description, 12, VisualUi.Muted, true);
        detail.MouseFilter = Control.MouseFilterEnum.Ignore; column.AddChild(detail);
        if (enabled)
        {
            var call = VisualUi.Text("START SANDBOX  →", 12, VisualUi.Gold);
            call.MouseFilter = Control.MouseFilterEnum.Ignore; column.AddChild(call);
        }
        return button;
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
        column.AddChild(VisualUi.Text("2050 · Humanity stands at the edge of its first interstellar age", 12, VisualUi.Gold));
        column.AddChild(VisualUi.Text("Preparing a coherent galaxy, living systems and your first expedition", 11, VisualUi.Muted));
        AddChild(_loading);
    }

    private void BuildAudioSettings()
    {
        _audioSettings = new CenterContainer { Name = "AudioSettingsPanel", Visible = false };
        _audioSettings.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(520, 0) };
        panel.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 22));
        _audioSettings.AddChild(panel);
        var content = new VBoxContainer(); content.AddThemeConstantOverride("separation", 14); panel.AddChild(content);
        content.AddChild(VisualUi.Text("AUDIO", 28, Colors.White));
        content.AddChild(VisualUi.Text("Balance the score and interface feedback for your play space.", 13, VisualUi.Muted, true));
        var settings = AudioDirector.Instance?.Settings ?? new AudioSettings();
        _masterVolume = AddVolumeRow(content, "MASTER", settings.Master);
        _musicVolume = AddVolumeRow(content, "MUSIC", settings.Music);
        _sfxVolume = AddVolumeRow(content, "SOUND EFFECTS", settings.Sfx);
        foreach (var slider in new[] { _masterVolume, _musicVolume, _sfxVolume })
            slider.ValueChanged += _ => ApplyAudioSettings();
        var actions = VisualUi.Actions(content);
        var done = VisualUi.Button("Done", "Save audio settings and return.", CloseAudioSettings, VisualIconLibrary.NavBack);
        done.Name = "AudioSettingsDone"; actions.AddChild(done);
        var defaults = VisualUi.Button("Restore defaults", "Restore the recommended audio mix.", () =>
        {
            _masterVolume.Value = 78; _musicVolume.Value = 64; _sfxVolume.Value = 82;
            ApplyAudioSettings();
        }, VisualIconLibrary.NavHome);
        defaults.Name = "AudioSettingsDefaults"; actions.AddChild(defaults);
        _overlay.AddChild(_audioSettings);
    }

    private static HSlider AddVolumeRow(Container parent, string label, float value)
    {
        var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 14); parent.AddChild(row);
        var name = VisualUi.Text(label, 12, VisualUi.Gold); name.CustomMinimumSize = new Vector2(130, 0); row.AddChild(name);
        var slider = new HSlider { MinValue = 0, MaxValue = 100, Step = 1, Value = value * 100,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(300, 34) };
        row.AddChild(slider);
        return slider;
    }

    private void ShowAudioSettings()
    {
        _campaignModes.Hide(); _newGameSelection.Hide(); _sandboxSetup.Hide(); _audioSettings.Show();
        _masterVolume.GrabFocus();
    }

    private void CloseAudioSettings()
    {
        ApplyAudioSettings(); _audioSettings.Hide(); _campaignModes.Show(); _resume.GrabFocus();
    }

    private void ApplyAudioSettings() => AudioDirector.Instance?.SetVolumes(
        (float)_masterVolume.Value / 100f, (float)_musicVolume.Value / 100f, (float)_sfxVolume.Value / 100f);

    private async Task RunLoadingAsync(string status, Func<bool> action)
    {
        _loadingStatus.Text = status;
        _loadingProgress.Value = 18;
        _overlay.Hide();
        _loading.Show();
        LoadingPresentationShownCount++;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _loadingProgress.Value = 52;
        await ToSignal(GetTree().CreateTimer(0.35), SceneTreeTimer.SignalName.Timeout);
        if (!action())
        {
            _loading.Hide();
            _overlay.Show();
            _resume.GrabFocus();
            return;
        }
        // Loading a campaign can take long enough for the next rendered frame to carry a
        // large delta. Pause the newly loaded clock while the ready state is presented so
        // that opening a save never advances its world before the player regains control.
        var readySpeed = _main.UiCurrentSpeed;
        _main.UiResumeAtSpeed(SimulationClock.SpeedLevel.Paused);
        _loadingStatus.Text = "Campaign ready";
        _loadingProgress.Value = 100;
        await ToSignal(GetTree().CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
        _loading.Hide();
        AudioDirector.Instance?.SetMenuContext(false);
        _main.UiResumeAtSpeed(readySpeed);
    }
}

/// <summary>Compact vector preview generated from the same barred-spiral coordinate profile as play.</summary>
public sealed partial class SandboxGalaxyPreview : Control
{
    private long _seed;

    public SandboxGalaxyPreview()
    {
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void SetSeed(long seed)
    {
        _seed = seed;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var size = Size;
        DrawRect(new Rect2(Vector2.Zero, size), new Color(.006f, .012f, .026f));
        var random = new Random(unchecked((int)(_seed ^ (_seed >> 32) ^ 0x50525657)));
        for (var index = 0; index < 90; index++)
        {
            var position = new Vector2((float)random.NextDouble() * size.X, (float)random.NextDouble() * size.Y);
            var alpha = .10f + (float)random.NextDouble() * .28f;
            DrawCircle(position, random.NextDouble() < .10 ? 1.1f : .55f,
                VisualPalette.WithAlpha(new Color(.68f, .79f, 1f), alpha));
        }
        for (var index = 0; index < 360; index++)
        {
            var world = GalaxySpatialLayout.NextPosition(GalaxyShape.BarredSpiral, 900, random) -
                GalaxySpatialLayout.SolOffset(900);
            var point = Project(world, size);
            var color = index % 9 == 0 ? new Color(1f, .52f, .36f) : new Color(.36f, .62f, 1f);
            DrawCircle(point, .55f + (float)random.NextDouble() * .75f,
                VisualPalette.WithAlpha(color, .14f + (float)random.NextDouble() * .28f));
        }
        for (var index = 0; index < 100; index++)
        {
            var world = GalaxySpatialLayout.NextPosition(GalaxyShape.BarredSpiral, 900, random) -
                GalaxySpatialLayout.SolOffset(900);
            var point = Project(world, size);
            DrawCircle(point, 2.0f, VisualPalette.WithAlpha(VisualPalette.Selected, .18f));
            DrawCircle(point, .9f, new Color(.86f, .93f, 1f));
        }
        var sol = Project(System.Numerics.Vector2.Zero, size);
        DrawCircle(sol, 4.2f, VisualPalette.WithAlpha(VisualUi.Gold, .18f));
        DrawCircle(sol, 1.4f, VisualUi.Gold);
        DrawRect(new Rect2(Vector2.Zero, size), VisualPalette.WithAlpha(VisualPalette.Keyline, .62f), false, 1);
    }

    private static Vector2 Project(System.Numerics.Vector2 world, Vector2 size)
    {
        var normalizedX = world.X / 900f / 2f + .68f;
        var normalizedY = world.Y / 900f / 1.44f + .60f;
        return new Vector2(size.X * (.04f + normalizedX * .92f), size.Y * (.07f + normalizedY * .86f));
    }
}

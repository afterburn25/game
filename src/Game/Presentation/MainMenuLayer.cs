using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    private Control _videoSettings = null!;
    private OptionButton _videoResolution = null!, _videoMode = null!, _videoVsync = null!, _videoMsaa = null!, _videoRenderScale = null!;
    private readonly VideoSettingsService _videoService = new();
    private Control _videoRollback = null!;
    private Label _videoRollbackText = null!, _videoError = null!;
    private Button _videoKeep = null!;
    private VideoSettingsService.Settings _videoPrevious;
    private double _videoRollbackSeconds;
    private bool _videoHasUncommittedChange;
    private Control _development = null!;
    private HSlider _masterVolume = null!, _musicVolume = null!, _sfxVolume = null!;
    private Label _loadingStatus = null!;
    private ProgressBar _loadingProgress = null!;
    private ConfirmationDialog _confirmation = null!;
    private Label _confirmationTitle = null!, _confirmationBody = null!;
    private Button _confirmationAccept = null!, _confirmationCancel = null!;
    private Label _saveError = null!;
    private Label _mode = null!;
    private Button _player = null!, _developer = null!, _tools = null!;
    private Button _resume = null!, _load = null!;
    private LineEdit _seed = null!;
    private Action? _confirmedStart;
    private Func<bool>? _confirmedLoad;
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
    public bool UiCampaignConfirmationVisible => _confirmation?.Visible ?? false;
    public string UiCampaignConfirmationTitle => _confirmationTitle?.Text ?? string.Empty;
    public string UiCampaignConfirmationAcceptText => _confirmationAccept?.Text ?? string.Empty;
    public string UiCampaignConfirmationCancelText => _confirmationCancel?.Text ?? string.Empty;

    public override void _Ready()
    {
        _main = GetParent() as Main ?? throw new InvalidOperationException("MainMenuLayer must be a child of Main.");
        _videoService.LoadAndApply();
        AudioDirector.Instance?.SetMenuContext(true);
        _overlay = new ColorRect { Color = VisualPalette.Canvas };
        VisualUi.ContainPointerInput(_overlay);
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var backdrop = new MainMenuBackdrop();
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _overlay.AddChild(backdrop);
        _campaignModes = new PanelContainer { Name = "CampaignModes" };
        _campaignModes.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.LeftWide);
        _campaignModes.OffsetLeft = 56; _campaignModes.OffsetRight = 510;
        _campaignModes.OffsetTop = 68; _campaignModes.OffsetBottom = -44;
        _campaignModes.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        _overlay.AddChild(_campaignModes);
        var content = new VBoxContainer(); content.AddThemeConstantOverride("separation", 8); _campaignModes.AddChild(content);
        content.AddChild(VisualUi.Text("STELLAR", 48));
        content.AddChild(VisualUi.Text("C O N T I N U U M", 23));
        content.AddChild(VisualUi.Text("THE FIRST LIGHT OF AN INTERSTELLAR AGE", 11, VisualUi.Gold));
        content.AddChild(new Control { CustomMinimumSize = new(0, 30) });
        _resume = AddMenuButton(content, "ResumeCampaign", "Continue", "Return to your campaign.", ContinueCampaign);
        _load = AddMenuButton(content, "LoadCampaign", "Load saved campaign", "Discard unsaved changes and reload this Player campaign from its save slot.", RequestLoadCampaign);
        AddMenuButton(content, "NewPlayerCampaign", "New Game", "Begin a new Player campaign.", RequestNewCampaign);
        _player = AddMenuButton(content, "ModePlayer", "Player campaign", "Open your separate Player campaign.", SwitchToPlayer);
        _load.Visible = !_main.UiIsDeveloperMode;
        _player.Visible = _main.UiIsDeveloperMode;
        AddMenuButton(content, "AudioSettings", "Audio", "Adjust sound and music.", ShowAudioSettings);
        AddMenuButton(content, "VideoSettings", "Video", "Configure display and rendering settings.", ShowVideoSettings);
        AddMenuButton(content, "VoiceSettings", "Voice & subtitles", "Configure offline dialogue and accessibility.", () => _main.UiVoice?.ShowVoiceSettings());
        AddMenuButton(content, "OpenDevelopment", "Development", "Switch to your separate Developer world and tools.", () =>
        {
            _campaignModes.Hide(); _development.Show(); _developer.GrabFocus();
        });
        AddMenuButton(content, "QuitCampaign", "Save and exit", "Save this campaign and exit.", _main.UiQuit);
        content.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        _mode = VisualUi.Text("PLAYER MODE", 11, VisualUi.Accent);
        _mode.Name = "CampaignModeLabel"; content.AddChild(_mode);
        content.AddChild(VisualUi.Text(_main.UiBuildLabel, 10, VisualUi.Muted));
        _saveError = VisualUi.Text("", 13, new Color("efac92"), true);
        _saveError.Name = "CampaignMenuError"; _saveError.Visible = false; content.AddChild(_saveError);
        BuildDevelopmentMenu();
        BuildNewGameSelection();
        BuildSandboxSetup();
        BuildAudioSettings();
        BuildVideoSettings();
        AddChild(_overlay);
        BuildLoadingPresentation();
        _confirmation = new ConfirmationDialog
        {
            // The stock ConfirmationDialog is a native-looking grey window. Keep the
            // real ConfirmationDialog contract for input/tests, but make it an
            // in-game surface with no OS title bar.
            Title = "Start a new campaign?", DialogAutowrap = true,
            Borderless = true, Unresizable = true,
            // AcceptDialog enables wrap_controls in its native constructor. That
            // mode re-fits this custom content window from child minimums and was
            // expanding the modal to the full 720px viewport.
            WrapControls = false,
        };
        StyleCampaignConfirmation();
        ConfigureCampaignConfirmation(loading: false);
        _confirmation.Confirmed += ConfirmCampaignAction;
        _confirmation.Canceled += ClearConfirmedCampaignAction;
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
        if (_videoRollback?.Visible == true)
        {
            _videoRollbackSeconds -= delta;
            _videoRollbackText.Text = $"Keep these display settings? Reverting in {Math.Max(0, (int)Math.Ceiling(_videoRollbackSeconds))} seconds.";
            if (_videoRollbackSeconds <= 0) RevertVideoSettings();
        }
        _refresh += delta;
        if (_refresh < .2 || !IsBlockingGameplay) return;
        _refresh = 0;
        _mode.Text = _main.UiModeLabel.ToUpperInvariant() + (_main.UiIsDeveloperMode ? (_main.UiDeveloperToolsUsed ? " · TOOLS USED" : " · TOOLS UNUSED") : "");
        _mode.Modulate = _main.UiIsDeveloperMode ? VisualUi.Gold : VisualUi.Accent;
        _load.Visible = !_main.UiIsDeveloperMode;
        _load.Disabled = !_main.UiHasPlayerSave;
        _player.Visible = _main.UiIsDeveloperMode;
        _player.Text = _main.UiIsDeveloperMode ? (_main.UiHasPlayerSave ? "Resume Player" : "Start Player") : "Player active";
        _developer.Text = _main.UiIsDeveloperMode ? "Load Developer save" : (_main.UiHasDeveloperSave ? "Resume Developer" : "Start Developer");
        _developer.Disabled = _main.UiIsDeveloperMode && !_main.UiHasDeveloperSave;
        _tools.Disabled = !_main.UiIsDeveloperMode;
    }

    private void ContinueCampaign()
    {
        _newGameSelection.Hide();
        _sandboxSetup.Hide();
        _development.Hide();
        _campaignModes.Show();
        _audioSettings.Hide();
        _videoSettings.Hide();
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
        _videoSettings.Hide();
        _development.Hide();
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
        _confirmedLoad = null;
        ConfigureCampaignConfirmation(loading: false);
        SetCampaignConfirmationText($"Start a fresh Developer campaign with seed {seed}? The current campaign will be saved first. The previous Developer save is kept as its backup; Player saves stay separate. Tools run only when you choose them.");
        ShowCampaignConfirmation(new(620, 260));
    }
    private async void ConfirmCampaignAction()
    {
        var load = _confirmedLoad; _confirmedLoad = null;
        if (load is not null)
        {
            _confirmedStart = null;
            await RunLoadingAsync("Loading the saved campaign", load);
            return;
        }
        var start = _confirmedStart; _confirmedStart = null;
        if (start is null || !_main.UiCheckpointBeforeCampaignSwitch()) return;
        await RunLoadingAsync("Generating a new 100-star campaign", () => { start(); return true; });
    }
    private void ClearConfirmedCampaignAction() { _confirmedStart = null; _confirmedLoad = null; }
    private void RequestLoadCampaign()
    {
        ShowMenu();
        _development.Hide();
        _newGameSelection.Hide();
        _sandboxSetup.Hide();
        _campaignModes.Show();
        _confirmedStart = null;
        _confirmedLoad = _main.UiLoadCurrentCampaign;
        ConfigureCampaignConfirmation(loading: true);
        SetCampaignConfirmationText($"Load the saved {_main.UiModeLabel} campaign? Unsaved changes in the current campaign will be discarded. Loading does not overwrite the save or the other mode's campaign.");
        ShowCampaignConfirmation(new(640, 250));
    }
    private async void SwitchToPlayer()
    {
        if (!_main.UiIsDeveloperMode) { ContinueCampaign(); return; }
        await RunLoadingAsync("Loading the Player campaign", _main.UiSwitchToPlayerMode);
    }
    private async void SwitchToDeveloper()
    {
        if (_main.UiIsDeveloperMode) { RequestLoadCampaign(); return; }
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
        if (_videoRollback.Visible) RevertVideoSettings();
        else if (_confirmation.Visible) { _confirmation.Hide(); ClearConfirmedCampaignAction(); }
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
        else if (_videoSettings.Visible) CancelVideoSettings();
        else if (_development.Visible) CloseDevelopmentMenu();
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
        _confirmedLoad = null;
        ConfigureCampaignConfirmation(loading: false);
        SetCampaignConfirmationText($"Generate a fresh 100-system {species.DisplayName} Player campaign with seed '{entered}'? The current Player campaign will be checkpointed first.");
        ShowCampaignConfirmation(new(650, 250));
    }

    private void StyleCampaignConfirmation()
    {
        _confirmation.Name = "CampaignConfirmation";
        _confirmation.AddThemeStyleboxOverride("panel", CinematicArt.Frame("panel", 22));
        _confirmation.AddThemeColorOverride("font_color", new Color("e8eee9"));
        _confirmation.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, .55f));
        _confirmation.AddThemeConstantOverride("outline_size", 2);
        _confirmation.AddThemeFontSizeOverride("font_size", 15);

        // AcceptDialog positions each direct content child into the same content
        // rectangle; its built-in message label is an internal direct child of the
        // Window, not a VBox. Hide that renderer and mirror DialogText into our own
        // VBox so heading, rule, and body receive real vertical layout.
        var bodyLabel = _confirmation.GetLabel();
        bodyLabel.Visible = false;
        // AcceptDialog measures every direct Control child, even an invisible
        // label. Exclude this required internal renderer so its autowrapped
        // DialogText cannot inflate the modal to a full-height window.
        bodyLabel.SetAsTopLevel(true);
        bodyLabel.CustomMinimumSize = Vector2.Zero;
        var body = new VBoxContainer
        {
            Name = "CampaignConfirmationContent",
            CustomMinimumSize = new Vector2(520, 120),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.AddThemeConstantOverride("separation", 10);
        _confirmation.AddChild(body);
        var heading = VisualUi.Text("START A NEW CAMPAIGN?", 23, VisualUi.Gold);
        heading.Name = "CampaignConfirmationTitle";
        heading.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.AddChild(heading);

        var rule = new ColorRect
        {
            Name = "CampaignConfirmationRule", Color = new Color("587d73"),
            CustomMinimumSize = new Vector2(0, 1), MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.AddChild(rule);

        _confirmationBody = VisualUi.Text("", 15, new Color("c2cfca"), wrap: false);
        _confirmationBody.Name = "CampaignConfirmationBody";
        _confirmationBody.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _confirmationBody.CustomMinimumSize = new Vector2(0, 72);
        body.AddChild(_confirmationBody);

        _confirmationTitle = heading;
        _confirmationAccept = _confirmation.GetOkButton();
        _confirmationCancel = _confirmation.GetCancelButton();
        StyleConfirmationButton(_confirmationAccept, highlighted: true);
        StyleConfirmationButton(_confirmationCancel, highlighted: false);
    }

    private void ConfigureCampaignConfirmation(bool loading)
    {
        _confirmation.Title = loading ? "Load saved campaign?" : "Start a new campaign?";
        _confirmationTitle.Text = loading ? "LOAD SAVED CAMPAIGN?" : "START A NEW CAMPAIGN?";
        _confirmationAccept.Text = loading ? "LOAD CAMPAIGN" : "START CAMPAIGN";
        _confirmationAccept.TooltipText = loading
            ? "Load the saved campaign and discard unsaved changes."
            : "Save the current campaign, then begin a fresh campaign.";
        _confirmationCancel.Text = loading ? "KEEP CURRENT CAMPAIGN" : "CANCEL";
        _confirmationCancel.TooltipText = loading
            ? "Keep the current campaign and return to the menu."
            : "Keep the current campaign and return to setup.";
    }

    private static void StyleConfirmationButton(Button button, bool highlighted)
    {
        button.CustomMinimumSize = new Vector2(180, 42);
        button.FocusMode = Control.FocusModeEnum.All;
        button.AddThemeFontSizeOverride("font_size", 13);
        button.AddThemeColorOverride("font_color", highlighted ? VisualUi.Gold : new Color("d0d9d5"));
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeStyleboxOverride("normal", CinematicArt.Frame(highlighted ? "button-hover" : "button", 9));
        button.AddThemeStyleboxOverride("hover", CinematicArt.Frame("button-hover", 9));
        button.AddThemeStyleboxOverride("pressed", CinematicArt.Frame("button-pressed", 9));
        button.AddThemeStyleboxOverride("focus", CinematicArt.Frame("button-hover", 9));
    }

    private void ShowCampaignConfirmation(Vector2I size)
    {
        _confirmation.Size = size;
        _confirmation.PopupCentered(size);
        // Popup layout can apply the Window minimum one more time; restore the
        // deliberate cinematic footprint after it has become visible.
        _confirmation.Size = size;
        // Starting a campaign is destructive. Make the reversible action the
        // default keyboard focus, while Enter still explicitly confirms.
        CallDeferred(nameof(FocusSafeCampaignCancel));
    }

    private void FocusSafeCampaignCancel()
    {
        if (_confirmation.Visible) _confirmation.GetCancelButton().GrabFocus();
    }

    private void SetCampaignConfirmationText(string text)
    {
        // Preserve the public/native DialogText contract for callers and probes;
        // the dedicated body label is the correctly laid-out visual renderer.
        _confirmation.DialogText = text;
        if (_confirmationBody is null) return;
        // Wrap using measured glyph widths before popup layout. Godot's first-layout
        // autowrap minimum can otherwise treat this label as zero pixels wide.
        var font = ThemeDB.FallbackFont;
        var lines = new System.Collections.Generic.List<string>();
        var line = string.Empty;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && font.GetStringSize(candidate, fontSize: 15).X > 560)
            { lines.Add(line); line = word; }
            else line = candidate;
        }
        if (line.Length > 0) lines.Add(line);
        _confirmationBody.Text = string.Join("\n", lines);
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
    private void BuildDevelopmentMenu()
    {
        _development = new CenterContainer { Name = "DevelopmentPanel", Visible = false };
        _development.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var panel = new PanelContainer { CustomMinimumSize = new(480, 0) };
        panel.AddThemeStyleboxOverride("panel", CinematicArt.Frame(margin: 22)); _development.AddChild(panel);
        var body = new VBoxContainer(); body.AddThemeConstantOverride("separation", 12); panel.AddChild(body);
        body.AddChild(VisualUi.Text("DEVELOPMENT", 26));
        body.AddChild(VisualUi.Text("Test in a separate world. Player saves stay separate.", 13, VisualUi.Muted));
        _developer = AddButton(body, "ModeDeveloper", "Open Developer", "Open your separate Developer campaign.", SwitchToDeveloper, VisualIconLibrary.Construction);
        body.AddChild(VisualUi.Text("WORLD SEED", 11, VisualUi.Muted));
        _seed = new LineEdit { Name = "DeveloperSeed", Text = "20260908", MaxLength = 20, CustomMinimumSize = new(0, 36) };
        body.AddChild(_seed);
        AddButton(body, "NewDeveloperCampaign", "New Developer campaign", "Create a separate Developer world after confirmation.", RequestDeveloperCampaign, VisualIconLibrary.NavHome);
        _tools = AddButton(body, "DeveloperTools", "Developer tools", "Open tools for the active Developer campaign.", OpenTools, VisualIconLibrary.Construction);
        AddButton(body, "CloseDevelopment", "Back", "Return to the main menu.", CloseDevelopmentMenu, VisualIconLibrary.NavBack);
        _overlay.AddChild(_development);
    }

    private void CloseDevelopmentMenu()
    {
        _development.Hide(); _campaignModes.Show(); _resume.GrabFocus();
    }

    private static Button AddMenuButton(Container parent, string name, string text, string tooltip, Action action)
    {
        var button = VisualUi.Button(text, tooltip, action);
        button.Name = name; button.Alignment = HorizontalAlignment.Left;
        button.CustomMinimumSize = new(0, 43); button.AddThemeFontSizeOverride("font_size", 20);
        button.AddThemeStyleboxOverride("normal", new StyleBoxEmpty { ContentMarginLeft = 8 });
        var focus = VisualUi.Surface(true, 8);
        focus.BgColor = new Color(0,0,0,0); focus.BorderColor = new Color("8fac9c");
        focus.BorderWidthTop = focus.BorderWidthRight = focus.BorderWidthBottom = 0;
        focus.BorderWidthLeft = 2; focus.ShadowSize = 0;
        button.AddThemeStyleboxOverride("focus", focus);
        button.AddThemeColorOverride("font_color", new Color("cbd6d3"));
        button.AddThemeColorOverride("font_hover_color", new Color("f4ddab"));
        parent.AddChild(button); return button;
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
        var veil = new ColorRect { Color = new Color(0.003f, .008f, .018f, .12f), MouseFilter = Control.MouseFilterEnum.Ignore };
        veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _loading.AddChild(veil);
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        margin.AnchorLeft = .28f; margin.AnchorRight = .72f;
        margin.OffsetLeft = 0; margin.OffsetRight = 0; margin.OffsetTop = -90; margin.OffsetBottom = -35;
        _loading.AddChild(margin);
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 8));
        margin.AddChild(panel);
        var column = new VBoxContainer(); column.AddThemeConstantOverride("separation", 10); panel.AddChild(column);
        _loadingStatus = VisualUi.Text("Preparing campaign", 15, VisualUi.Accent);
        _loadingStatus.Name = "LoadingStatus"; column.AddChild(_loadingStatus);
        _loadingProgress = new ProgressBar
        {
            Name = "LoadingProgress", MinValue = 0, MaxValue = 100, Value = 0,
            ShowPercentage = false, CustomMinimumSize = new Vector2(0, 8),
        };
        column.AddChild(_loadingProgress);
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

    private void BuildVideoSettings()
    {
        _videoSettings = new CenterContainer { Name = "VideoSettingsPanel", Visible = false };
        _videoSettings.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(600, 0) };
        panel.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 22)); _videoSettings.AddChild(panel);
        var content = new VBoxContainer(); content.AddThemeConstantOverride("separation", 10); panel.AddChild(content);
        content.AddChild(VisualUi.Text("VIDEO", 28));
        content.AddChild(VisualUi.Text("Detected display modes and renderer controls for this computer.", 13, VisualUi.Muted, true));
        content.AddChild(VisualUi.Text($"{_videoService.AdapterName}  ·  {_videoService.RendererName}", 11, VisualUi.Accent, true));
        _videoResolution = AddVideoOption(content, "RESOLUTION", _videoService.Modes.Select(mode => mode.ToString()));
        _videoMode = AddVideoOption(content, "DISPLAY", new[] { "Windowed", "Borderless", "Exclusive fullscreen" });
        _videoMode.ItemSelected += _ => UpdateVideoResolutionAvailability();
        content.AddChild(VisualUi.Text("Fullscreen uses the desktop resolution; resolution selection sets the window size.", 12, VisualUi.Muted, true));
        _videoVsync = AddVideoOption(content, "V-SYNC", new[] { "Off", "On", "Adaptive" });
        _videoMsaa = AddVideoOption(content, "MSAA", new[] { "Off", "2×", "4×", "8×" });
        _videoRenderScale = AddVideoOption(content, "3D RESOLUTION", new[] { "75% · Performance", "100% · Native", "125% · Quality" });
        var nvidiaPanel = _videoService.FindNvidiaControlPanel();
        if (nvidiaPanel is not null)
        {
            var nvidia = VisualUi.Button("Open NVIDIA Control Panel", "Open NVIDIA's installed control panel. Stellar Continuum does not change global driver settings.", () =>
            {
                try { VideoSettingsService.OpenNvidiaControlPanel(nvidiaPanel); }
                catch (Exception error)
                {
                    _videoError.Text = $"NVIDIA Control Panel could not open: {error.Message}";
                    _videoError.Show();
                }
            });
            nvidia.Name = "OpenNvidiaControlPanel"; content.AddChild(nvidia);
        }
        _videoError = VisualUi.Text("", 12, new Color("efac92"), true); _videoError.Visible = false; content.AddChild(_videoError);
        var actions = VisualUi.Actions(content);
        var apply = VisualUi.Button("Apply", "Preview these settings for 15 seconds.", ApplyVideoSettings, VisualIconLibrary.Save);
        apply.Name = "VideoSettingsDone"; actions.AddChild(apply);
        var cancel = VisualUi.Button("Cancel", "Return without saving changes.", CancelVideoSettings, VisualIconLibrary.NavBack);
        cancel.Name = "VideoSettingsCancel"; actions.AddChild(cancel);
        _overlay.AddChild(_videoSettings);

        _videoRollback = new Control { Name = "VideoSettingsConfirmation", Visible = false };
        _videoRollback.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        VisualUi.ContainPointerInput(_videoRollback);
        var shade = new ColorRect { Color = new Color(0, 0, 0, .72f), MouseFilter = Control.MouseFilterEnum.Stop };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); _videoRollback.AddChild(shade);
        var center = new CenterContainer(); center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); _videoRollback.AddChild(center);
        var confirmPanel = new PanelContainer { CustomMinimumSize = new Vector2(510, 0) };
        confirmPanel.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 22)); center.AddChild(confirmPanel);
        var confirmBody = new VBoxContainer(); confirmBody.AddThemeConstantOverride("separation", 14); confirmPanel.AddChild(confirmBody);
        confirmBody.AddChild(VisualUi.Text("CONFIRM DISPLAY", 22, Colors.White));
        _videoRollbackText = VisualUi.Text("", 13, VisualUi.Muted, true); confirmBody.AddChild(_videoRollbackText);
        var confirmActions = VisualUi.Actions(confirmBody);
        _videoKeep = VisualUi.Button("Keep", "Keep and save these display settings.", KeepVideoSettings, VisualIconLibrary.Save);
        _videoKeep.Name = "KeepVideoSettings"; confirmActions.AddChild(_videoKeep);
        var revert = VisualUi.Button("Revert", "Restore the previous display settings.", RevertVideoSettings, VisualIconLibrary.NavBack);
        revert.Name = "RevertVideoSettings"; confirmActions.AddChild(revert);
        _overlay.AddChild(_videoRollback);
    }
    private static OptionButton AddVideoOption(Container parent, string label, IEnumerable<string> values)
    { var row=new HBoxContainer(); parent.AddChild(row); var name=VisualUi.Text(label,12,VisualUi.Gold); name.CustomMinimumSize=new Vector2(130,0); row.AddChild(name); var option=new OptionButton { SizeFlagsHorizontal=Control.SizeFlags.ExpandFill }; foreach(var value in values) option.AddItem(value); row.AddChild(option); return option; }
    private void ShowVideoSettings()
    {
        SyncVideoControls(VideoSettingsService.Current);
        _videoError.Hide(); _campaignModes.Hide(); _videoSettings.Show(); _videoResolution.GrabFocus();
    }

    private void UpdateVideoResolutionAvailability()
    {
        _videoResolution.Disabled = _videoMode.Selected != 0;
        _videoResolution.TooltipText = _videoResolution.Disabled
            ? "Fullscreen uses your desktop resolution. Use 3D resolution to adjust rendering quality."
            : "Choose the game window's resolution.";
    }

    private void ApplyVideoSettings()
    {
        var resolution = _videoService.Modes[Math.Clamp(_videoResolution.Selected, 0, _videoService.Modes.Count - 1)];
        var displayMode = _videoMode.Selected switch
        {
            1 => VideoSettingsService.DisplayMode.Borderless,
            2 => VideoSettingsService.DisplayMode.Fullscreen,
            _ => VideoSettingsService.DisplayMode.Windowed,
        };
        var vsync = _videoVsync.Selected switch
        {
            1 => DisplayServer.VSyncMode.Enabled,
            2 => DisplayServer.VSyncMode.Adaptive,
            _ => DisplayServer.VSyncMode.Disabled,
        };
        var msaa = _videoMsaa.Selected switch
        {
            1 => Viewport.Msaa.Msaa2X,
            2 => Viewport.Msaa.Msaa4X,
            3 => Viewport.Msaa.Msaa8X,
            _ => Viewport.Msaa.Disabled,
        };
        var renderScale = _videoRenderScale.Selected switch { 0 => .75f, 2 => 1.25f, _ => 1f };
        var previous = _videoService.ApplyPreview(new(resolution, displayMode, vsync, msaa, renderScale));
        if (!_videoHasUncommittedChange) _videoPrevious = previous;
        _videoHasUncommittedChange = true;
        _videoRollbackSeconds = 15;
        _videoRollback.Show();
        _videoKeep.GrabFocus();
    }

    private void KeepVideoSettings()
    {
        var error = _videoService.SaveCurrent();
        _videoRollback.Hide();
        if (!string.IsNullOrEmpty(error)) { _videoError.Text = error; _videoError.Show(); return; }
        _videoHasUncommittedChange = false;
        _videoSettings.Hide(); _campaignModes.Show(); _resume.GrabFocus();
    }

    private void RevertVideoSettings()
    {
        _videoService.Revert(_videoPrevious);
        _videoHasUncommittedChange = false;
        SyncVideoControls(_videoPrevious);
        _videoRollback.Hide(); _videoResolution.GrabFocus();
    }

    private void CancelVideoSettings()
    {
        if (_videoHasUncommittedChange) RevertVideoSettings();
        _videoSettings.Hide(); _campaignModes.Show(); _resume.GrabFocus();
    }

    private void SyncVideoControls(VideoSettingsService.Settings settings)
    {
        var resolutionIndex = 0;
        for (var index = 0; index < _videoService.Modes.Count; index++)
            if (_videoService.Modes[index] == settings.Resolution) { resolutionIndex = index; break; }
        _videoResolution.Select(resolutionIndex);
        _videoMode.Select(settings.DisplayMode switch { VideoSettingsService.DisplayMode.Borderless => 1, VideoSettingsService.DisplayMode.Fullscreen => 2, _ => 0 });
        _videoVsync.Select(settings.VSync switch { DisplayServer.VSyncMode.Enabled => 1, DisplayServer.VSyncMode.Adaptive => 2, _ => 0 });
        _videoMsaa.Select(settings.Msaa switch { Viewport.Msaa.Msaa2X => 1, Viewport.Msaa.Msaa4X => 2, Viewport.Msaa.Msaa8X => 3, _ => 0 });
        _videoRenderScale.Select(settings.RenderScale switch { .75f => 0, 1.25f => 2, _ => 1 });
        UpdateVideoResolutionAvailability();
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

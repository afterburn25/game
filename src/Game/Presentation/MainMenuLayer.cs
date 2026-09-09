using System;
using System.Globalization;
using Godot;
using Game.Simulation;

namespace Game.Presentation;

/// <summary>Campaign mode selection. Main owns switching, checkpoints and commands.</summary>
public partial class MainMenuLayer : CanvasLayer
{
    private Main _main = null!;
    private Control _overlay = null!;
    private ConfirmationDialog _confirmation = null!;
    private Label _saveError = null!;
    private Label _mode = null!;
    private Button _player = null!, _developer = null!, _tools = null!;
    private LineEdit _seed = null!;
    private Action? _confirmedStart;
    private SimulationClock.SpeedLevel _resumeSpeed = SimulationClock.SpeedLevel.Normal;
    private double _refresh;
    public bool IsBlockingGameplay => (_overlay?.IsVisibleInTree() ?? false) || (_confirmation?.Visible ?? false);

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
        AddButton(content, "ResumeCampaign", "Resume campaign", "Return to the active campaign at its previous speed.", ContinueCampaign, VisualIconLibrary.NavGalaxy);
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
        _confirmation = new ConfirmationDialog { Title = "Start a new campaign?", DialogAutowrap = true };
        _confirmation.Confirmed += ConfirmStart;
        _confirmation.Canceled += () => _confirmedStart = null;
        AddChild(_confirmation);
        _main.UiResumeAtSpeed(SimulationClock.SpeedLevel.Paused);
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
        _main.UiResumeAtSpeed(SimulationClock.SpeedLevel.Paused); _overlay.Show();
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
    private void ConfirmStart()
    {
        var start = _confirmedStart; _confirmedStart = null;
        if (start is null || !_main.UiCheckpointBeforeCampaignSwitch()) return;
        start(); _overlay.Hide(); // Main chooses the new mode's clock speed.
    }
    private void SwitchToPlayer()
    {
        if (!_main.UiIsDeveloperMode) { ContinueCampaign(); return; }
        if (_main.UiSwitchToPlayerMode()) _overlay.Hide();
    }
    private void SwitchToDeveloper()
    {
        if (_main.UiIsDeveloperMode) { ContinueCampaign(); return; }
        if (_main.UiSwitchToDeveloperMode()) _overlay.Hide();
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
}

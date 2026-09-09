using System;
using System.Globalization;
using Godot;
using Game.Simulation.Generation;

namespace Game.Presentation;

public partial class MainMenuLayer
{
    private Control _setupRoot = null!;
    private LineEdit _setupSeed = null!, _setupCode = null!;
    private OptionButton _setupShape = null!, _setupSize = null!;
    private SpinBox _setupRivals = null!, _setupAncients = null!;
    private Label _setupError = null!, _setupSummary = null!, _currentRecipe = null!;
    private GalaxyLayoutPreview _setupPreview = null!;
    private Button _generateCampaign = null!;
    private Button _useCurrentRecipe = null!;
    private static readonly int[] SetupSizes = { 50, 100, 200 };

    private void BuildGalaxySetup()
    {
        _setupRoot = new ColorRect { Name = "GalaxySetup", Color = VisualPalette.Canvas, Visible = false };
        _setupRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        VisualUi.ContainPointerInput(_setupRoot); _overlay.AddChild(_setupRoot);
        var center = new CenterContainer(); center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); _setupRoot.AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = new(980, 0) };
        panel.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 20)); center.AddChild(panel);
        var column = new VBoxContainer(); column.AddThemeConstantOverride("separation", 12); panel.AddChild(column);
        column.AddChild(VisualUi.Text("CREATE YOUR GALAXY", 26));
        column.AddChild(VisualUi.Text("Choose the starting universe for a new Player campaign.", 14, VisualUi.Muted));
        var body = new HBoxContainer(); body.AddThemeConstantOverride("separation", 24); column.AddChild(body);
        var fields = new VBoxContainer { CustomMinimumSize = new(440, 0) }; fields.AddThemeConstantOverride("separation", 12); body.AddChild(fields);
        fields.AddChild(VisualUi.Text("Galaxy seed", 14, VisualUi.Accent));
        var seedRow = new HBoxContainer(); fields.AddChild(seedRow);
        _setupSeed = new LineEdit { Name = "GalaxySeed", Text = Random.Shared.NextInt64().ToString(CultureInfo.InvariantCulture),
            MaxLength = 20, CustomMinimumSize = new(280, 38), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        seedRow.AddChild(_setupSeed);
        AddButton(seedRow, "RollGalaxySeed", "Roll seed", "Choose a new seed and update the preview.",
            () => _setupSeed.Text = Random.Shared.NextInt64().ToString(CultureInfo.InvariantCulture), VisualIconLibrary.NavGalaxy);
        _setupSeed.TextChanged += _ => RefreshGalaxyPreview();
        _setupShape = SetupChoice(fields, "GalaxyShape", "Shape", new[] { "Spiral · four sweeping arms", "Elliptical · a broad oval", "Ring · an open center" });
        _setupSize = SetupChoice(fields, "GalaxySize", "Size", new[] { "Small · 50 systems", "Medium · 100 systems", "Large · 200 systems" });
        _setupSize.Select(1);
        var empires = new HBoxContainer(); empires.AddThemeConstantOverride("separation", 20); fields.AddChild(empires);
        _setupRivals = SetupCount(empires, "GalaxyRivals", "Rival empires", 7, 7);
        _setupAncients = SetupCount(empires, "GalaxyAncients", "Ancient empires", 3, 2);
        fields.AddChild(VisualUi.Text("Rivals start before warp travel, like you. Ancient empires begin with interstellar technology. Your empire is additional.", 12, VisualUi.Muted, true));
        var previewColumn = new VBoxContainer { CustomMinimumSize = new(448, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddChild(previewColumn);
        _setupPreview = new GalaxyLayoutPreview { Name = "GalaxyPreview", CustomMinimumSize = new(440, 270), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        previewColumn.AddChild(_setupPreview);
        _setupSummary = VisualUi.Text("", 13, VisualUi.Accent, true); _setupSummary.Name = "GalaxyPreviewSummary"; previewColumn.AddChild(_setupSummary);
        previewColumn.AddChild(VisualUi.Text("Preview shows the actual star positions. The gold marker is Sol. Planet details and other empires remain discoveries.", 12, VisualUi.Muted, true));
        column.AddChild(new HSeparator());
        column.AddChild(VisualUi.Text("Share or recreate a galaxy", 14));
        var codeRow = new HBoxContainer(); column.AddChild(codeRow);
        _setupCode = new LineEdit { Name = "GalaxyCode", PlaceholderText = "Paste a galaxy code", CustomMinimumSize = new(520, 36), MaxLength = 100, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        codeRow.AddChild(_setupCode);
        AddButton(codeRow, "ImportGalaxyCode", "Apply code", "Restore the seed and all generation settings from a galaxy code.", ApplyGalaxyCode, VisualIconLibrary.NavGalaxy);
        AddButton(codeRow, "CopyGalaxyCode", "Copy code", "Copy the seed and settings together.", CopyGalaxyCode, VisualIconLibrary.Save);
        _setupError = VisualUi.Text("", 12, VisualUi.Gold, true); _setupError.Name = "GalaxySetupMessage"; column.AddChild(_setupError);
        var actions = new HBoxContainer(); actions.AddThemeConstantOverride("separation", 12); column.AddChild(actions);
        AddButton(actions, "CancelGalaxySetup", "Back", "Return to the campaign menu.", CloseGalaxySetup, VisualIconLibrary.NavGalaxy);
        _useCurrentRecipe = AddButton(actions, "UseCurrentGalaxy", "Use current settings", "Load the current campaign's seed and settings into this preview.",
            () => { _setupCode.Text = _main.UiGalaxyOptions!.ShareCode(_main.UiGalaxySeed); ApplyGalaxyCode(); }, VisualIconLibrary.Save);
        _generateCampaign = AddButton(actions, "GenerateGalaxy", "Start this galaxy", "Review the new campaign before replacing the Player save.", ConfirmGalaxySetup, VisualIconLibrary.NavHome);
        RefreshGalaxyPreview();
    }

    private OptionButton SetupChoice(Container parent, string name, string label, string[] choices)
    {
        var row = new HBoxContainer(); parent.AddChild(row);
        row.AddChild(new Label { Text = label, CustomMinimumSize = new(64, 0) });
        var input = new OptionButton { Name = name, CustomMinimumSize = new(330, 38), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (var choice in choices) input.AddItem(choice);
        row.AddChild(input); input.ItemSelected += _ => RefreshGalaxyPreview(); return input;
    }

    private SpinBox SetupCount(Container parent, string name, string title, int maximum, int initial)
    {
        var column = new VBoxContainer(); parent.AddChild(column); column.AddChild(VisualUi.Text(title, 13));
        var input = new SpinBox { Name = name, MinValue = 0, MaxValue = maximum, Step = 1, Value = initial, CustomMinimumSize = new(180, 36) };
        column.AddChild(input); input.ValueChanged += _ => RefreshGalaxyPreview(); return input;
    }

    private bool ReadGalaxyRecipe(out long seed, out GalaxySetupOptions options)
    {
        options = new() { Shape = (GalaxyShape)_setupShape.Selected, SystemCount = SetupSizes[_setupSize.Selected],
            RivalEmpires = (int)_setupRivals.Value, AncientEmpires = (int)_setupAncients.Value };
        return long.TryParse(_setupSeed.Text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out seed);
    }

    private void RefreshGalaxyPreview()
    {
        if (_generateCampaign is null) return;
        try
        {
            if (!ReadGalaxyRecipe(out var seed, out var options)) throw new ArgumentException("Enter a whole-number seed between −9223372036854775808 and 9223372036854775807.");
            _setupPreview.SetRecipe(seed, options);
            _setupSummary.Text = options.Summary;
            _setupCode.Text = options.ShareCode(seed);
            _setupError.Text = "Same seed + settings + generator version = same starting galaxy.";
            _generateCampaign.Disabled = false;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        {
            _setupPreview.Clear(); _setupSummary.Text = "Preview unavailable";
            _setupCode.Text = ""; _setupError.Text = error.Message; _generateCampaign.Disabled = true;
        }
    }

    private void ApplyGalaxyCode()
    {
        if (!GalaxySetupOptions.TryParseCode(_setupCode.Text, out var seed, out var options))
        { _setupError.Text = "That galaxy code is invalid or uses an unsupported generator version."; return; }
        _setupShape.Select((int)options.Shape); _setupSize.Select(Array.IndexOf(SetupSizes, options.SystemCount));
        _setupRivals.SetValueNoSignal(options.RivalEmpires); _setupAncients.SetValueNoSignal(options.AncientEmpires);
        _setupSeed.Text = seed.ToString(CultureInfo.InvariantCulture); RefreshGalaxyPreview();
    }

    private void CopyGalaxyCode()
    {
        if (!ReadGalaxyRecipe(out var seed, out var options) || _generateCampaign.Disabled) return;
        DisplayServer.ClipboardSet(options.ShareCode(seed)); _setupError.Text = "Galaxy code copied, including its seed and settings.";
    }

    private void OpenGalaxySetup()
    {
        ShowMenu(); ClearSaveFailure(); _campaignModes.Hide(); _newGameSelection.Hide(); _setupRoot.Show();
        _useCurrentRecipe.Disabled = _main.UiGalaxyOptions is null;
        RefreshGalaxyPreview(); _setupSeed.GrabFocus();
    }

    private void CloseGalaxySetup() { _setupRoot.Hide(); RequestNewCampaign(); }

    private void ConfirmGalaxySetup()
    {
        if (!ReadGalaxyRecipe(out var seed, out var options) || _generateCampaign.Disabled) return;
        _confirmedStart = () => _main.UiCreateConfiguredCampaignConfirmed(seed, options);
        _confirmation.DialogText = $"Start a fresh Player campaign?\n{options.Summary}\nSeed: {seed}\n\nYour current campaign will be saved first. The previous Player save becomes its backup. Developer saves stay separate.";
        _confirmation.PopupCentered(new(570, 240));
    }
}

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
    private Button _pauseButton = null!;
    private double _logisticsRefreshTimer;

    public override void _Ready()
    {
        _main = GetParent() as Main
            ?? throw new InvalidOperationException("PlayerControls must be a child of Main.");

        var panel = new PanelContainer
        {
            OffsetLeft = 16,
            OffsetTop = 194,
            OffsetRight = 700,
            OffsetBottom = 362,
        };

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        panel.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 14);
        root.AddChild(header);

        _buildLabel = new Label { Text = _main.UiBuildLabel };
        _speedLabel = new Label { Text = _main.UiSpeedLabel };
        header.AddChild(_buildLabel);
        header.AddChild(_speedLabel);

        _logisticsLabel = new Label
        {
            Text = _main.UiLogisticsSummary,
            TooltipText = "Strategic supply is calculated by the authoritative economy/logistics subsystem. Effective coverage includes local support plus current cargo-handling capacity.",
        };
        root.AddChild(_logisticsLabel);

        var timeRow = new HBoxContainer();
        timeRow.AddThemeConstantOverride("separation", 4);
        root.AddChild(timeRow);

        _pauseButton = AddButton(timeRow, "Pause", "Pause or resume the strategic simulation.", _main.UiTogglePause, 82, VisualIconLibrary.Pause);
        AddButton(timeRow, "1x", "Normal simulation speed.", () => _main.UiSetSpeed(1));
        AddButton(timeRow, "2x", "Fast simulation speed.", () => _main.UiSetSpeed(2));
        AddButton(timeRow, "3x", "Very fast simulation speed.", () => _main.UiSetSpeed(3));
        AddButton(timeRow, "4x", "Maximum requested simulation speed. Effective speed may be lower if the machine cannot sustain it.", () => _main.UiSetSpeed(4));

        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 4);
        root.AddChild(actionRow);

        AddButton(actionRow, "Next Research", "Cycle through currently available research choices.", _main.UiCycleResearch, 128, VisualIconLibrary.Research);
        AddButton(actionRow, "Start Research", "Begin the currently selected research project.", _main.UiStartResearch, 128, VisualIconLibrary.Research);
        AddButton(actionRow, "Next Build", "Cycle through currently available construction projects.", _main.UiCycleConstruction, 108, VisualIconLibrary.Construction);
        AddButton(actionRow, "Start Build", "Begin the currently selected construction project.", _main.UiStartConstruction, 108, VisualIconLibrary.Construction);

        var utilityRow = new HBoxContainer();
        utilityRow.AddThemeConstantOverride("separation", 4);
        root.AddChild(utilityRow);

        AddButton(utilityRow, "New Game", "Generate a new campaign beginning January 1, 2050.", _main.UiNewCampaign, 92);
        AddButton(utilityRow, "Save", "Write the current campaign to the autosave slot.", _main.UiSave, 82, VisualIconLibrary.Save);
        AddButton(utilityRow, "Support Bundle", "Export diagnostics and include the autosave when available.", _main.UiExportDiagnostics, 138, VisualIconLibrary.Support);
        AddButton(utilityRow, "Relations", "Open or close the observer-safe diplomatic relations overlay.", _main.UiToggleRelationsPanel, 104, VisualIconLibrary.Relations);

        AddChild(panel);
        RefreshState(forceLogistics: true);
    }

    public override void _Process(double delta)
    {
        _logisticsRefreshTimer += delta;
        RefreshState(forceLogistics: _logisticsRefreshTimer >= 0.5);
        if (_logisticsRefreshTimer >= 0.5)
            _logisticsRefreshTimer = 0.0;
    }

    private void RefreshState(bool forceLogistics)
    {
        if (_main is null || _buildLabel is null || _speedLabel is null || _logisticsLabel is null || _pauseButton is null)
            return;

        _buildLabel.Text = _main.UiBuildLabel;
        _speedLabel.Text = $"Speed: {_main.UiSpeedLabel}";
        _pauseButton.Text = _main.UiIsPaused ? "Resume" : "Pause";

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

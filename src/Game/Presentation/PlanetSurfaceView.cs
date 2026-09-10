using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Construction;
using Game.Simulation.Economy;
using Godot;

namespace Game.Presentation;

/// <summary>A real 3D colony area. The UI proposes world-space orders; the simulation owns
/// permission, placement, power and construction. No camera or preview state is persisted.</summary>
public partial class PlanetSurfaceView : Control
{
    private const float StreetViewDistance = 10f;
    private const float ColonyOverviewDistance = 1200f;
    private Func<UiSurfaceSnapshot?>? _readSnapshot;
    private Func<string, float, float, float, UiSurfaceOrderResult>? _placeBuilding;
    private Func<int, UiSurfaceOrderResult>? _removeBuilding;
    private Func<int, UiSurfaceOrderResult>? _upgradeBuilding;
    private Func<int, bool, UiSurfaceOrderResult>? _setBuildingEnabled;
    private Func<UiSurfaceOrderResult>? _upgradeHub;
    private UiSurfaceSnapshot? _snapshot;
    private readonly Dictionary<int, SurfaceBuildingVisual> _buildings = new();
    private readonly List<SurfaceBuildingState> _placementStates = new();
    private readonly Dictionary<string, Button> _buildButtons = new();
    private readonly Dictionary<int, Button> _speedButtons = new();
    private readonly List<Control> _overlayPanels = new();
    private SubViewport _viewport = null!;
    private Node3D _world = null!;
    private Camera3D _camera = null!;
    private ProceduralSkyMaterial _skyMaterial = null!;
    private Godot.Environment _environment = null!;
    private DirectionalLight3D _sun = null!;
    private ShaderMaterial _terrainMaterial = null!;
    private string? _surfacePaletteKey;
    private Node3D? _settlementVisual;
    private string _settlementVisualKey = string.Empty;
    private Node3D? _hubVisual;
    private string _hubVisualKey = string.Empty;
    private Label _title = null!;
    private Label _resources = null!;
    private Label _production = null!;
    private Label _time = null!;
    private Label _status = null!;
    private Label _instructions = null!;
    private GridContainer _palette = null!;
    private Button _rotate = null!;
    private Button _cancel = null!;
    private Button _remove = null!;
    private Button _upgrade = null!;
    private Button _toggleOperation = null!;
    private Button _upgradeHubButton = null!;
    private SurfaceBuildingVisual? _ghost;
    private string? _selectedType;
    private int? _selectedBuildingId;
    private string? _placementError;
    private Vector3 _ground;
    private Vector2 _pointerViewport;
    private bool _hasPointer;
    private bool _hasGround;
    private bool _orbitDragging;
    private bool _leftPanCandidate;
    private bool _leftPanMoved;
    private Vector2 _leftPanStart;
    private Vector3 _target = Vector3.Zero;
    private float _distance = 205;
    private float _yaw = .65f;
    private float _pitch = .69f;
    private float _rotation;
    private double _refresh;
    private double _messageRemaining;
    private bool _built;
    public event Action? ReturnToOrbit;
    public event Action? SaveRequested;
    public event Action? PauseRequested;
    public event Action<int>? SpeedRequested;
    public Func<bool>? IsInputBlocked { get; set; }
    public Func<string>? ReadTimeLabel { get; set; }
    public Func<int>? ReadSpeedLevel { get; set; }
    public bool IsOpen { get; private set; }
    public Vector3 CameraPosition => _built ? _camera.Position : Vector3.Zero;
    public string? SelectedBuildingType => _selectedType;
    public string? PlacementErrorText => _hasGround ? _placementError : null;
    public bool HasGroundPreview => _hasGround && _selectedType is not null;
    public string SurfaceVisualClass { get; private set; } = string.Empty;
    public int SettlementVisualParts => _settlementVisual?.GetChildCount() ?? 0;
    public int AmbientShuttleCount => _settlementVisual?.GetChildren()
        .Count(child => child.Name.ToString().StartsWith("CivilianShuttle", StringComparison.Ordinal)) ?? 0;
    public int HighRiseCount => _settlementVisual?.GetChildren()
        .Count(child => child.Name.ToString().StartsWith("HighRise", StringComparison.Ordinal)) ?? 0;
    public int DistrictRingRoadCount => _settlementVisual?.GetChildren()
        .Count(child => child.Name.ToString().StartsWith("DistrictRingRoad", StringComparison.Ordinal)) ?? 0;
    public int DistrictPlazaCount => _settlementVisual?.GetChildren()
        .Count(child => child.Name.ToString().StartsWith("DistrictPlaza", StringComparison.Ordinal)) ?? 0;
    private bool InputBlocked => IsInputBlocked?.Invoke() == true;

    /// <summary>Read-only projection into the main viewport, for real pointer interaction and
    /// accessibility. Returns null when the ground point is behind or outside the camera view.</summary>
    public Vector2? GetSurfaceScreenPosition(float x, float z)
    {
        if (!_built || !IsOpen || !float.IsFinite(x) || !float.IsFinite(z)
            || _viewport.Size.X <= 0 || _viewport.Size.Y <= 0) return null;
        var world = new Vector3(x, SurfaceConstruction.TerrainHeight(x, z), z);
        if (_camera.IsPositionBehind(world)) return null;
        var projected = _camera.UnprojectPosition(world);
        var point = projected * new Vector2(Size.X / _viewport.Size.X, Size.Y / _viewport.Size.Y);
        if (!new Rect2(Vector2.Zero, Size).HasPoint(point)) return null;
        return GetGlobalTransformWithCanvas() * point;
    }

    public void Configure(Func<UiSurfaceSnapshot?> readSnapshot,
        Func<string, float, float, float, UiSurfaceOrderResult> placeBuilding,
        Func<int, UiSurfaceOrderResult> removeBuilding,
        Func<int, UiSurfaceOrderResult> upgradeBuilding,
        Func<int, bool, UiSurfaceOrderResult> setBuildingEnabled,
        Func<UiSurfaceOrderResult> upgradeHub)
    {
        _readSnapshot = readSnapshot;
        _placeBuilding = placeBuilding;
        _removeBuilding = removeBuilding;
        _upgradeBuilding = upgradeBuilding;
        _setBuildingEnabled = setBuildingEnabled;
        _upgradeHub = upgradeHub;
    }

    public override void _Ready()
    {
        Name = "PlanetSurfaceView";
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        ClipContents = true;
        BuildScene();
        BuildOverlay();
        _built = true;
        Visible = IsOpen;
        _viewport.RenderTargetUpdateMode = IsOpen ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Disabled;
        _viewport.ProcessMode = IsOpen ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        if (IsOpen) RefreshSnapshot();
    }

    public void Open()
    {
        IsOpen = true;
        _hasPointer = false;
        _leftPanCandidate = _leftPanMoved = false;
        Visible = true;
        if (!_built) return;
        _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        _viewport.ProcessMode = ProcessModeEnum.Inherit;
        RefreshSnapshot();
        GrabFocus();
        UpdateCamera();
    }

    public void Close()
    {
        IsOpen = false;
        Visible = false;
        _orbitDragging = _leftPanCandidate = _leftPanMoved = false;
        _hasPointer = false;
        if (!_built) return;
        CancelPlacement();
        _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        _viewport.ProcessMode = ProcessModeEnum.Disabled;
        ReleaseFocus();
    }

    public override void _Process(double delta)
    {
        if (!IsOpen || !_built) return;
        _refresh -= delta;
        _messageRemaining -= delta;
        if (_refresh <= 0) { _refresh = .15; RefreshSnapshot(); }
        if (InputBlocked)
        {
            _orbitDragging = _leftPanCandidate = _leftPanMoved = false;
            _hasGround = false;
            if (_ghost is not null) _ghost.Visible = false;
            return;
        }
        var motion = Vector2.Zero;
        if (Input.IsPhysicalKeyPressed(Key.W)) motion.Y -= 1;
        if (Input.IsPhysicalKeyPressed(Key.S)) motion.Y += 1;
        if (Input.IsPhysicalKeyPressed(Key.A)) motion.X -= 1;
        if (Input.IsPhysicalKeyPressed(Key.D)) motion.X += 1;
        if (motion != Vector2.Zero)
        {
            motion = motion.Normalized() * (float)delta * Math.Max(22, _distance * .43f)
                * (Input.IsPhysicalKeyPressed(Key.Shift) ? 2.4f : 1);
            Pan(motion);
        }
        UpdateCamera();
        UpdateGhost();
    }

    public override void _Input(InputEvent input)
    {
        if (!IsOpen || InputBlocked) return;
        if (input is InputEventMouse pointer)
        {
            // Keep the coordinates from the actual input stream. Root-window mouse polling
            // reads the OS pointer, which can differ for remote/emulated input and replay.
            // Recording does not consume input: HUD controls still decide GUI routing below.
            _pointerViewport = pointer.Position;
            _hasPointer = true;
            return;
        }
        if (input is not InputEventKey key || !key.Pressed || key.Echo) return;
        var code = key.PhysicalKeycode == Key.None ? key.Keycode : key.PhysicalKeycode;
        if (code == Key.Escape)
        {
            if (_selectedType is not null) CancelPlacement(); else ReturnToOrbit?.Invoke();
            GetViewport().SetInputAsHandled();
        }
        else if (code == Key.R && _selectedType is not null)
        {
            RotatePreview();
            GetViewport().SetInputAsHandled();
        }
        else if (code is Key.W or Key.A or Key.S or Key.D)
            GetViewport().SetInputAsHandled();
    }

    public override void _GuiInput(InputEvent input)
    {
        if (!IsOpen || InputBlocked) return;
        if (input is InputEventMouse pointer)
        {
            // GUI mouse coordinates are already local to this Control. Store the matching
            // viewport point before a click places, even when no motion preceded that click.
            _pointerViewport = GetGlobalTransformWithCanvas() * pointer.Position;
            _hasPointer = true;
        }
        if (input is InputEventMouseButton button)
        {
            if (button.ButtonIndex == MouseButton.Middle) _orbitDragging = button.Pressed;
            if (button.Pressed)
            {
                GrabFocus();
                if (button.ButtonIndex == MouseButton.WheelUp) _distance = Math.Clamp(_distance * .88f, StreetViewDistance, ColonyOverviewDistance);
                if (button.ButtonIndex == MouseButton.WheelDown) _distance = Math.Clamp(_distance / .88f, StreetViewDistance, ColonyOverviewDistance);
                if (button.ButtonIndex == MouseButton.Left)
                {
                    if (_selectedType is not null) PlacePreview();
                    else
                    {
                        _leftPanCandidate = true;
                        _leftPanMoved = false;
                        _leftPanStart = button.Position;
                    }
                }
            }
            else if (button.ButtonIndex == MouseButton.Left && _leftPanCandidate)
            {
                if (!_leftPanMoved) SelectBuildingAt(_pointerViewport);
                _leftPanCandidate = false;
                _leftPanMoved = false;
            }
        }
        if (input is InputEventMouseMotion movement)
        {
            // Buttons can capture a release over the HUD, so also check the actual held state.
            _orbitDragging &= Input.IsMouseButtonPressed(MouseButton.Middle);
            if (_orbitDragging)
            {
                _yaw -= movement.Relative.X * .005f;
                _pitch = Math.Clamp(_pitch + movement.Relative.Y * .004f, .12f, 1.48f);
            }
            if (_leftPanCandidate)
            {
                if (!_leftPanMoved && movement.Position.DistanceTo(_leftPanStart) >= 5)
                    _leftPanMoved = true;
                if (_leftPanMoved) Pan(-movement.Relative * (_distance * .0018f));
            }
        }
        AcceptEvent();
    }

    private void Pan(Vector2 motion)
    {
        var right = new Vector3(MathF.Cos(_yaw), 0, -MathF.Sin(_yaw));
        var back = new Vector3(MathF.Sin(_yaw), 0, MathF.Cos(_yaw));
        _target += right * motion.X + back * motion.Y;
        _target.X = Math.Clamp(_target.X, -SurfaceConstruction.AreaHalfSize, SurfaceConstruction.AreaHalfSize);
        _target.Z = Math.Clamp(_target.Z, -SurfaceConstruction.AreaHalfSize, SurfaceConstruction.AreaHalfSize);
        _target.Y = SurfaceConstruction.TerrainHeight(_target.X, _target.Z);
    }

    private void UpdateCamera()
    {
        _camera.Position = _target + new Vector3(MathF.Sin(_yaw) * MathF.Cos(_pitch),
            MathF.Sin(_pitch), MathF.Cos(_yaw) * MathF.Cos(_pitch)) * _distance;
        _camera.Position = new(_camera.Position.X,
            Math.Max(_camera.Position.Y, SurfaceConstruction.TerrainHeight(_camera.Position.X, _camera.Position.Z) + 2.2f),
            _camera.Position.Z);
        _camera.LookAt(_target, Vector3.Up);
    }

    private void UpdateGhost()
    {
        if (_ghost is null || _selectedType is null) return;
        var local = GetGlobalTransformWithCanvas().AffineInverse() * _pointerViewport;
        var overHud = _overlayPanels.Any(panel => panel.IsVisibleInTree() &&
            new Rect2(Vector2.Zero, panel.Size).HasPoint(panel.GetGlobalTransformWithCanvas().AffineInverse() * _pointerViewport));
        _hasGround = _hasPointer && !overHud && Size.X > 0 && Size.Y > 0
            && new Rect2(Vector2.Zero, Size).HasPoint(local) && TryGround(local, out _ground);
        _ghost.Visible = _hasGround;
        if (!_hasGround) return;
        _placementError = SurfaceConstruction.PlacementError(_placementStates, _selectedType, _ground.X, _ground.Z, _rotation);
        _ghost.PlaceOnTerrain(_ground.X, _ground.Z, _rotation);
        _ghost.ShowPreview(_placementError is null);
        if (_messageRemaining <= 0)
        {
            _status.Text = _placementError ?? $"Click to place · {_ground.X:0}, {_ground.Z:0} m · Rotation {_rotation:0}°";
            _status.Modulate = _placementError is null ? new Color("a5ecce") : new Color("f2ac8e");
        }
    }

    // Intersect the camera ray against the exact shared height function. Terrain is a continuous
    // heightfield, so bracketed bisection avoids a separate collision mesh or grid-snapped orders.
    private bool TryGround(Vector2 pointer, out Vector3 position)
    {
        var screen = pointer * new Vector2(_viewport.Size.X / Size.X, _viewport.Size.Y / Size.Y);
        var origin = _camera.ProjectRayOrigin(screen);
        var direction = _camera.ProjectRayNormal(screen);
        float previous = 0;
        for (float distance = 4; distance <= 3000; distance += 8)
        {
            var sample = origin + direction * distance;
            if (sample.Y <= SurfaceConstruction.TerrainHeight(sample.X, sample.Z))
            {
                var low = previous; var high = distance;
                for (var refine = 0; refine < 14; refine++)
                {
                    var middle = (low + high) * .5f;
                    var test = origin + direction * middle;
                    if (test.Y > SurfaceConstruction.TerrainHeight(test.X, test.Z)) low = middle; else high = middle;
                }
                position = origin + direction * ((low + high) * .5f);
                position.Y = SurfaceConstruction.TerrainHeight(position.X, position.Z);
                return true;
            }
            previous = distance;
        }
        position = Vector3.Zero;
        return false;
    }

    private void SelectBuilding(string id)
    {
        if (InputBlocked) return;
        SelectExistingBuilding(null);
        _selectedType = id;
        _rotation = 0;
        _ghost?.QueueFree();
        _ghost = SurfaceBuildingVisuals.Create(id);
        _ghost.Name = "PlacementPreview";
        _world.AddChild(_ghost);
        _ghost.ShowPreview(true);
        foreach (var pair in _buildButtons) pair.Value.ButtonPressed = pair.Key == id;
        _rotate.Visible = _cancel.Visible = true;
        _instructions.Text = "Click terrain to place   ·   R rotate   ·   Esc cancel   ·   WASD move   ·   Middle-drag look   ·   Wheel zoom";
        _messageRemaining = 0;
        GrabFocus();
        UpdateGhost();
    }

    private void CancelPlacement()
    {
        _selectedType = null;
        _hasGround = false;
        _placementError = null;
        _ghost?.QueueFree();
        _ghost = null;
        foreach (var button in _buildButtons.Values) button.ButtonPressed = false;
        _rotate.Visible = _cancel.Visible = false;
        _instructions.Text = "Left-drag move   ·   WASD move   ·   Middle-drag look   ·   Wheel zoom   ·   Esc return";
        _status.Text = "Choose a building, then place it anywhere suitable inside the colony boundary.";
        _status.Modulate = Colors.White;
    }

    private void SelectBuildingAt(Vector2 screenPosition)
    {
        if (_snapshot is null) return;
        UiSurfaceBuilding? closest = null;
        var closestDistance = 52f;
        foreach (var building in _snapshot.Buildings)
        {
            var projected = GetSurfaceScreenPosition(building.X, building.Z);
            if (projected is null) continue;
            var distance = projected.Value.DistanceTo(screenPosition);
            if (distance >= closestDistance) continue;
            closestDistance = distance;
            closest = building;
        }
        SelectExistingBuilding(closest);
    }

    private void SelectExistingBuilding(UiSurfaceBuilding? building)
    {
        _selectedBuildingId = building?.Id;
        _remove.Visible = building is not null;
        _upgrade.Visible = building?.CanUpgrade == true;
        _toggleOperation.Visible = building?.Complete == true;
        if (building is null)
        {
            foreach (var visual in _buildings.Values) visual.SetSelected(false);
            if (_messageRemaining <= 0)
            {
                _status.Text = "Choose a building to place, or click an existing structure to manage it.";
                _status.Modulate = Colors.White;
            }
            return;
        }
        _remove.Text = building.Complete ? "Demolish" : "Cancel site";
        _remove.TooltipText = building.Complete
            ? $"Demolish {building.Name}. Production and power effects stop immediately."
            : $"Cancel {building.Name}. Recover half its authorization funding; consumed materials are not recovered.";
        if (building.CanUpgrade)
        {
            _upgrade.Disabled = !building.CanAffordUpgrade || building.UpgradeLockReason is not null;
            _upgrade.Text = "Upgrade";
            _upgrade.TooltipText = building.UpgradeLockReason ?? (building.CanAffordUpgrade
                ? $"Upgrade to {building.UpgradeName} for {_snapshot!.Currency.Format(building.UpgradeCreditCost)} and {building.UpgradeIndustryCost:N0} materials."
                : $"{building.UpgradeName} requires {_snapshot!.Currency.Format(building.UpgradeCreditCost)} and {building.UpgradeIndustryCost:N0} available materials.");
        }
        _toggleOperation.Text = building.Enabled ? "Shut down" : "Restart";
        _toggleOperation.TooltipText = building.Enabled
            ? "Suspend this building's staffing, power demand, output and upkeep."
            : "Return this building to operation when staffing and power are available.";
        _status.Text = building.Complete
            ? $"{building.Name} selected · {(!building.Enabled ? "shut down" : !building.Staffed ? "offline: insufficient workforce" : building.Powered ? "powered and operating" : "offline: insufficient power")}"
            : $"{building.Name} selected · {building.Progress:P0} constructed";
        _status.Modulate = building.Powered || !building.Complete ? new Color("a5ecce") : new Color("f2c078");
        foreach (var pair in _buildings) pair.Value.SetSelected(pair.Key == building.Id);
    }

    private void RemoveSelectedBuilding()
    {
        if (InputBlocked || _selectedBuildingId is not int buildingId || _removeBuilding is null) return;
        var result = _removeBuilding(buildingId);
        if (result.Accepted) _selectedBuildingId = null;
        ShowMessage(result.Message, result.Accepted);
        RefreshSnapshot();
    }

    private void UpgradeSelectedBuilding()
    {
        if (InputBlocked || _selectedBuildingId is not int buildingId || _upgradeBuilding is null) return;
        var result = _upgradeBuilding(buildingId);
        ShowMessage(result.Message, result.Accepted);
        RefreshSnapshot();
    }

    private void ToggleSelectedBuildingOperation()
    {
        if (InputBlocked || _selectedBuildingId is not int buildingId || _setBuildingEnabled is null || _snapshot is null) return;
        var building = _snapshot.Buildings.FirstOrDefault(item => item.Id == buildingId);
        if (building is null || !building.Complete) return;
        var result = _setBuildingEnabled(buildingId, !building.Enabled);
        ShowMessage(result.Message, result.Accepted);
        RefreshSnapshot();
    }

    private void RotatePreview()
    {
        if (InputBlocked) return;
        _rotation = (_rotation + 15) % 360;
        UpdateGhost();
    }

    private void PlacePreview()
    {
        if (InputBlocked || _selectedType is null || _placeBuilding is null) return;
        UpdateGhost();
        if (!_hasGround) return;
        if (_placementError is not null) { ShowMessage(_placementError, false); return; }
        var result = _placeBuilding(_selectedType, _ground.X, _ground.Z, _rotation);
        ShowMessage(result.Message, result.Accepted);
        RefreshSnapshot();
    }

    private void ShowMessage(string text, bool success)
    {
        _status.Text = text;
        _status.Modulate = success ? new Color("a5ecce") : new Color("f2ac8e");
        _messageRemaining = 4;
    }

    private void RefreshSnapshot()
    {
        _time.Text = ReadTimeLabel?.Invoke() ?? string.Empty;
        var speed = ReadSpeedLevel?.Invoke() ?? 0;
        foreach (var pair in _speedButtons)
            pair.Value.Modulate = pair.Key == speed ? VisualUi.Accent : Colors.White;
        var next = _readSnapshot?.Invoke();
        if (next is null)
        {
            _title.Text = "Colony surface unavailable";
            _resources.Text = "Return to orbit and select a colony you own.";
            foreach (var button in _buildButtons.Values) button.Disabled = true;
            foreach (var visual in _buildings.Values) visual.Visible = false;
            if (_selectedType is not null) CancelPlacement();
            _snapshot = null;
            return;
        }
        if (_snapshot?.ColonyId != next.ColonyId)
        {
            foreach (var visual in _buildings.Values) visual.QueueFree();
            _buildings.Clear();
            _target = Vector3.Zero; _distance = 205; _yaw = .65f; _pitch = .69f;
            CancelPlacement();
        }
        if (_selectedBuildingId is int selectedId && !next.Buildings.Any(item => item.Id == selectedId))
            _selectedBuildingId = null;
        _snapshot = next;
        ApplyWorldPalette(next.SurfaceVisualClass, next.BodyId);
        ApplySettlementVisual(next);
        ApplyHubVisual(next);
        _title.Text = $"{next.PlanetName.ToUpperInvariant()}  /  {next.ColonyName}";
        _resources.Text = $"{next.Currency.Code}  {next.Currency.Format(next.Credits, includeCode: false)}     Materials  {next.Industry:N0}     Power  {next.PowerDemand:0.#} / {next.PowerSupply:0.#}     {next.HubName} L{next.HubLevel}  {next.Buildings.Count} / {next.BuildingCapacity} modules";
        _resources.Text += $"\nPopulation {next.PopulationMillions:N0}M / {next.SupportedPopulationMillions:N0}M sustainable   ·   Food {next.FoodCapacityMillions:N0}M   ·   Water {next.WaterCapacityMillions:N0}M   ·   Housing {next.HousingCapacityMillions:N0}M";
        _resources.Text += $"   ·   Reserves {next.FoodReserveDays:0.0}d food / {next.WaterReserveDays:0.0}d water";
        _resources.Text += $"   ·   Surface workforce {Math.Min(next.WorkforceAvailableMillions, next.WorkforceDemandMillions):N3}M / {next.WorkforceDemandMillions:N3}M";
        _resources.Text += $"   ·   Total employed {next.EmployedPopulationMillions:N0}M / {next.WorkingAgePopulationMillions:N0}M ({next.EmploymentRate:P0})";
        _resources.Modulate = next.PowerDemand > next.PowerSupply || next.WorkforceDemandMillions > next.WorkforceAvailableMillions + .0000001 ? new Color("e8b463") : Colors.White;
        var districtState = next.SpecializationActive ? "ACTIVE" : next.SpecializationComplexes > 0 ? $"{next.SpecializationComplexes}/3" : string.Empty;
        _production.Text = next.IsResourceOutpost
            ? $"SEALED RESOURCE OUTPOST  ·  EXTRACTION {next.ExtractionPerDay:0.##}/day  ·  LOCAL STORAGE {next.StoredExtractedMaterials:0.#}/{next.ExtractedMaterialCapacity:0.#}  ·  +{next.SciencePerDay:0.###} labs  ·  Upkeep {next.Currency.FormatRate(-next.UpkeepCreditsPerDay)}"
            : $"{next.SpecializationName.ToUpperInvariant()} {districtState}  ·  OUTPUT  {next.Currency.FormatRate(next.CreditsPerDay)}  {next.IndustryPerDay:+0.0;0.0;0.0} materials/day  +{next.SciencePerDay:0.###} labs  Habitat −{next.HabitatSupportReduction:P0}  Upkeep {next.Currency.FormatRate(-next.UpkeepCreditsPerDay)}";
        if (next.BaseOperationsFundingFraction < 0.999999)
            _production.Text += $"  ·  OPERATIONS {next.BaseOperationsFundingFraction:P0} FUNDED";
        _production.TooltipText = next.IsResourceOutpost
            ? next.OutpostOperationsStatus
            : $"{next.SpecializationName}: {next.SpecializationDescription}";
        if (next.EnvironmentConstructionCostMultiplier > 1.0001)
            _production.TooltipText += $" Local gravity, atmosphere, pressure, temperature and radiation make surface authorizations {next.EnvironmentConstructionCostMultiplier:0.00}× baseline cost.";
        _upgradeHubButton.Visible = next.CanUpgradeHub;
        _upgradeHubButton.Disabled = !next.CanAffordHubUpgrade;
        _upgradeHubButton.Text = $"Upgrade to L{next.HubLevel + 1}";
        _upgradeHubButton.TooltipText = next.HubUpgradeLockReason ?? (next.CanUpgradeHub
            ? $"Expand {next.HubName.ToLowerInvariant()} capacity for {next.Currency.Format(next.HubUpgradeCreditCost)} and {next.HubUpgradeIndustryCost:N0} materials."
            : $"{next.HubName} is at maximum capacity.");
        _placementStates.Clear();
        foreach (var building in next.Buildings)
        {
            _placementStates.Add(new SurfaceBuildingState
            {
                Id = building.Id, TypeId = building.TypeId, X = building.X, Z = building.Z,
                RotationDegrees = building.RotationDegrees, IndustryProgress = building.Progress * building.Cost,
                IsComplete = building.Complete,
            });
            if (_buildings.TryGetValue(building.Id, out var existing) && existing.TypeId != building.TypeId)
            {
                existing.QueueFree();
                _buildings.Remove(building.Id);
            }
            if (!_buildings.TryGetValue(building.Id, out var visual))
            {
                visual = SurfaceBuildingVisuals.Create(building.TypeId);
                visual.Name = "SurfaceBuilding_" + building.Id;
                _world.AddChild(visual);
                _buildings.Add(building.Id, visual);
            }
            visual.PlaceOnTerrain(building.X, building.Z, building.RotationDegrees);
            visual.Visible = true;
            visual.UpdateState(building);
            visual.SetSelected(building.Id == _selectedBuildingId);
        }
        foreach (var id in _buildings.Keys.Where(id => !next.Buildings.Any(building => building.Id == id)).ToArray())
        { _buildings[id].QueueFree(); _buildings.Remove(id); }
        SelectExistingBuilding(_selectedBuildingId is int activeId
            ? next.Buildings.FirstOrDefault(item => item.Id == activeId) : null);
        foreach (var option in next.BuildOptions)
        {
            if (!_buildButtons.ContainsKey(option.Id)) AddBuildButton(option);
            _buildButtons[option.Id].Disabled = !option.CanAfford;
            _buildButtons[option.Id].TooltipText = option.CanAfford
                ? $"{option.Name}: {option.Description}. Authorization costs {next.Currency.Format(option.CreditCost)}; construction consumes {option.IndustryCost:N0} materials over time."
                : $"{option.Name} requires {next.Currency.Format(option.CreditCost)}; only {next.Currency.Format(next.Credits)} is available.";
        }
        foreach (var pair in _buildButtons)
            pair.Value.Disabled = !next.BuildOptions.Any(option => option.Id == pair.Key && option.CanAfford);
    }

    private void BuildScene()
    {
        var container = new SubViewportContainer { Name = "SurfaceViewportContainer", Stretch = true, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(container);
        container.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _viewport = new SubViewport
        {
            Name = "SurfaceViewport", OwnWorld3D = true, TransparentBg = false,
            Size = new(1280, 720), Msaa3D = Viewport.Msaa.Msaa4X,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
            PhysicsObjectPicking = false, GuiDisableInput = true,
        };
        container.AddChild(_viewport);
        _world = new Node3D { Name = "ColonyLandscape" };
        _viewport.AddChild(_world);
        _skyMaterial = new ProceduralSkyMaterial
        {
            SkyTopColor = new("315067"), SkyHorizonColor = new("a6b3a6"),
            GroundBottomColor = new("1c2423"), GroundHorizonColor = new("a6b3a6"),
            SkyCurve = .25f,
        };
        _environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = _skyMaterial },
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new("8caba7"), AmbientLightEnergy = .35f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Sky,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
            FogEnabled = true, FogLightColor = new("a0afa2"), FogDensity = .00065f,
        };
        _world.AddChild(new WorldEnvironment { Environment = _environment });
        _sun = new DirectionalLight3D
        {
            Name = "ColonySun", RotationDegrees = new(-42, -36, 0), LightColor = new("fff0ce"),
            LightEnergy = 1.05f, ShadowEnabled = true, DirectionalShadowMaxDistance = 700,
        };
        _world.AddChild(_sun);
        _camera = new Camera3D { Name = "SurfaceCamera", Current = true, Fov = 48, Near = .5f, Far = 3200 };
        _world.AddChild(_camera);
        _world.AddChild(CreateTerrain());
        _hubVisual = SurfaceBuildingVisuals.CreateHub();
        _world.AddChild(_hubVisual);
        AddBoundaryMarkers();
        AddLandscapeRocks();
        UpdateCamera();
    }

    private MeshInstance3D CreateTerrain()
    {
        // Spend mesh detail on the actual build area: 4 m cells through the boundary and its
        // apron, then progressively larger scenic cells. One static mesh, 81,225 vertices;
        // only 23% more vertices than the old uniform 16 m mesh, with four times finer ground.
        var axis = new List<float> { -2048, -1536, -1152, -896, -704, -608 };
        for (var coordinate = -544; coordinate <= 544; coordinate += 4) axis.Add(coordinate);
        axis.AddRange(new float[] { 608, 704, 896, 1152, 1536, 2048 });
        var segments = axis.Count - 1;
        var vertices = new Vector3[(segments + 1) * (segments + 1)];
        var normals = new Vector3[vertices.Length];
        var indices = new int[segments * segments * 6];
        for (var z = 0; z <= segments; z++)
        for (var x = 0; x <= segments; x++)
        {
            var px = axis[x];
            var pz = axis[z];
            var index = z * (segments + 1) + x;
            vertices[index] = new(px, SurfaceConstruction.TerrainHeight(px, pz), pz);
            normals[index] = new Vector3(SurfaceConstruction.TerrainHeight(px - 1, pz) - SurfaceConstruction.TerrainHeight(px + 1, pz),
                2, SurfaceConstruction.TerrainHeight(px, pz - 1) - SurfaceConstruction.TerrainHeight(px, pz + 1)).Normalized();
        }
        var write = 0;
        for (var z = 0; z < segments; z++)
        for (var x = 0; x < segments; x++)
        {
            var a = z * (segments + 1) + x; var b = a + segments + 1;
            // Godot's front-face convention is clockwise when viewed from above.
            indices[write++] = a; indices[write++] = a + 1; indices[write++] = b;
            indices[write++] = a + 1; indices[write++] = b + 1; indices[write++] = b;
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Godot.Mesh.ArrayType.Max);
        arrays[(int)Godot.Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Godot.Mesh.ArrayType.Normal] = normals;
        arrays[(int)Godot.Mesh.ArrayType.Index] = indices;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays);
        _terrainMaterial = new ShaderMaterial
            { Shader = GD.Load<Shader>("res://assets/visual/shaders/colony_terrain.gdshader") };
        return new MeshInstance3D { Name = "Terrain", Mesh = mesh, MaterialOverride = _terrainMaterial };
    }

    private void ApplySettlementVisual(UiSurfaceSnapshot snapshot)
    {
        var populationBand = Math.Clamp(3 + (int)Math.Floor(Math.Log10(Math.Max(0.001, snapshot.PopulationMillions) * 1000 + 1)), 3, 9);
        var key = $"{snapshot.ColonyId}:{populationBand}:{snapshot.RequiredHabitatSystems}:{snapshot.SurfaceVisualClass}";
        if (_settlementVisualKey == key) return;
        _settlementVisualKey = key;
        if (_settlementVisual is not null)
        {
            _world.RemoveChild(_settlementVisual);
            _settlementVisual.QueueFree();
        }
        _settlementVisual = SurfaceBuildingVisuals.CreateHabitatCluster(
            snapshot.PopulationMillions, snapshot.RequiredHabitatSystems, snapshot.SurfaceVisualClass);
        _world.AddChild(_settlementVisual);
    }

    private void ApplyHubVisual(UiSurfaceSnapshot snapshot)
    {
        var key = $"{snapshot.ColonyId}:{snapshot.HubLevel}:{snapshot.IsCapitalHub}:{snapshot.IsResourceOutpost}";
        if (_hubVisualKey == key) return;
        _hubVisualKey = key;
        if (_hubVisual is not null)
        {
            _world.RemoveChild(_hubVisual);
            _hubVisual.QueueFree();
        }
        _hubVisual = SurfaceBuildingVisuals.CreateHub(snapshot.HubLevel, snapshot.IsCapitalHub, snapshot.IsResourceOutpost);
        _world.AddChild(_hubVisual);
    }

    private sealed record WorldPalette(string Low, string High, string ExposedLow, string ExposedHigh,
        string SkyTop, string Horizon, string Fog, string Sun);

    private void ApplyWorldPalette(string visualClass, int bodyId)
    {
        var paletteKey = $"{visualClass}:{bodyId}";
        if (_surfacePaletteKey == paletteKey) return;
        _surfacePaletteKey = paletteKey;
        SurfaceVisualClass = visualClass;
        var palette = visualClass switch
        {
            "frozen" => new WorldPalette("485967", "9bb6c3", "697986", "d2e3e7", "13263c", "a9c6d3", "a9c6d3", "d9edff"),
            "hot" => new WorldPalette("4f2417", "a34a22", "2d1714", "75402a", "48130d", "d16b38", "b64d2b", "ffd0a3"),
            "airless" => new WorldPalette("34363b", "777b82", "202126", "51545c", "03050b", "171c27", "171c27", "fff3dd"),
            "oceanic" => new WorldPalette("123f53", "2f8793", "1a5867", "58aab0", "153c58", "76b4c2", "63a0b0", "d6f3ff"),
            "reducing" => new WorldPalette("293f30", "65733b", "453822", "8a7540", "152c25", "8c9a63", "71845a", "e8d89d"),
            "rocky" => new WorldPalette("3b322b", "777064", "2d2723", "62564a", "252b36", "9b9488", "80796f", "ffe7c4"),
            _ => new WorldPalette("152719", "34452b", "33291d", "57452e", "203e59", "819a8d", "788f82", "ffe5bd"),
        };
        static Vector3 Rgb(string value) { var color = new Color(value); return new(color.R, color.G, color.B); }
        _terrainMaterial.SetShaderParameter("terrain_low", Rgb(palette.Low));
        _terrainMaterial.SetShaderParameter("terrain_high", Rgb(palette.High));
        _terrainMaterial.SetShaderParameter("terrain_exposed_low", Rgb(palette.ExposedLow));
        _terrainMaterial.SetShaderParameter("terrain_exposed_high", Rgb(palette.ExposedHigh));
        var seed = (unchecked((uint)bodyId * 2654435761u) & 1023u) / 1023f;
        _terrainMaterial.SetShaderParameter("terrain_seed", seed);
        _terrainMaterial.SetShaderParameter("terrain_roughness", visualClass switch
        {
            "frozen" => .74f,
            "oceanic" => .82f,
            "hot" => .92f,
            _ => .97f,
        });
        _skyMaterial.SkyTopColor = new Color(palette.SkyTop);
        _skyMaterial.SkyHorizonColor = new Color(palette.Horizon);
        _skyMaterial.GroundHorizonColor = new Color(palette.Horizon);
        _skyMaterial.GroundBottomColor = new Color(palette.ExposedLow);
        _environment.FogLightColor = new Color(palette.Fog);
        _sun.LightColor = new Color(palette.Sun);
    }

    private void AddBoundaryMarkers()
    {
        var edge = SurfaceConstruction.AreaHalfSize;
        var markerMaterial = SurfaceBuildingVisuals.Material("86ada0", .7f, .1f, true);
        for (var side = 0; side < 4; side++)
        for (var step = 0; step < 16; step++)
        {
            var along = -edge + step * (edge * 2 / 16);
            var x = side is 0 or 2 ? along : (side == 1 ? edge : -edge);
            var z = side is 1 or 3 ? along : (side == 0 ? -edge : edge);
            var h = SurfaceConstruction.TerrainHeight(x, z);
            SurfaceBuildingVisuals.Cylinder(_world, .45f, .8f, 3, new(x, h + 1.5f, z), SurfaceBuildingVisuals.Metal, 8);
            SurfaceBuildingVisuals.Sphere(_world, .65f, new(x, h + 3.2f, z), markerMaterial);
        }
    }

    private void AddLandscapeRocks()
    {
        // Decoration stays outside the buildable area; it never introduces invisible obstacles.
        var stone = SurfaceBuildingVisuals.Material("686959", .95f);
        var random = new Random(7021);
        for (var i = 0; i < 85; i++)
        {
            var angle = (float)random.NextDouble() * MathF.Tau;
            var radius = 745 + (float)random.NextDouble() * 140;
            var x = MathF.Cos(angle) * radius; var z = MathF.Sin(angle) * radius;
            var height = 3 + (float)random.NextDouble() * 16;
            var rock = SurfaceBuildingVisuals.Mesh(_world, new SphereMesh
            { Radius = 1, Height = 2, RadialSegments = 7, Rings = 4 },
                new(x, SurfaceConstruction.TerrainHeight(x, z), z), stone);
            rock.Scale = new(height * 1.8f, height, height * 1.3f);
            rock.RotationDegrees = new(0, (float)random.NextDouble() * 360, 13);
        }
    }

    private void BuildOverlay()
    {
        var header = new PanelContainer { Name = "SurfaceHeader", MouseFilter = MouseFilterEnum.Stop };
        VisualUi.ContainPointerInput(header);
        AddChild(header); _overlayPanels.Add(header);
        header.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        header.OffsetLeft = 18; header.OffsetRight = -18; header.OffsetTop = 16;
        header.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 12));
        var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 18); header.AddChild(row);
        var back = VisualUi.Button("← Orbit", "Return to the planet in orbit (Esc)", () =>
        { if (!InputBlocked) ReturnToOrbit?.Invoke(); });
        back.Name = "SurfaceBack"; row.AddChild(back);
        var titleBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; row.AddChild(titleBox);
        _title = VisualUi.Text("COLONY SURFACE", 22, new Color("e9eeea")); titleBox.AddChild(_title);
        _title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _resources = VisualUi.Text("", 14, VisualUi.Muted); titleBox.AddChild(_resources);
        _production = VisualUi.Text("", 12, VisualUi.Accent); _production.Name = "SurfaceProduction";
        _production.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        titleBox.AddChild(_production);
        var home = VisualUi.Button("Center hub", "Return the camera to your colony hub", () =>
        { if (!InputBlocked) { _target = Vector3.Zero; _distance = 205; _pitch = .69f; } });
        home.Name = "SurfaceCenterHub"; row.AddChild(home);
        _upgradeHubButton = VisualUi.Button("Upgrade hub", "Expand surface module capacity", UpgradeSurfaceHub);
        _upgradeHubButton.Name = "SurfaceUpgradeHub";
        _upgradeHubButton.Visible = false;
        row.AddChild(_upgradeHubButton);
        var timeBox = new VBoxContainer(); row.AddChild(timeBox);
        var sessionActions = new HBoxContainer(); timeBox.AddChild(sessionActions);
        var save = VisualUi.Button("Save", "Save this campaign, including colony construction", () =>
        { if (!InputBlocked) SaveRequested?.Invoke(); }, VisualIconLibrary.Save);
        save.Name = "SurfaceSave"; sessionActions.AddChild(save);
        var pause = VisualUi.Button("Pause / resume", "Pause or resume colony construction and the simulation", () =>
        { if (!InputBlocked) PauseRequested?.Invoke(); }, VisualIconLibrary.Pause);
        pause.Name = "SurfacePause"; sessionActions.AddChild(pause);
        foreach (var option in new[] { (Level: 1, Multiplier: 1), (Level: 2, Multiplier: 2), (Level: 3, Multiplier: 3), (Level: 4, Multiplier: 8) })
        {
            var speed = VisualUi.Button($"{option.Multiplier}×", $"Run the ordinary simulation at {option.Multiplier}× speed", () =>
            { if (!InputBlocked) SpeedRequested?.Invoke(option.Level); });
            speed.Name = "SurfaceSpeed" + option.Level;
            speed.CustomMinimumSize = new Vector2(38, 38);
            sessionActions.AddChild(speed);
            _speedButtons.Add(option.Level, speed);
        }
        _time = VisualUi.Text("", 12, VisualUi.Gold);
        _time.Name = "SurfaceTime"; _time.HorizontalAlignment = HorizontalAlignment.Right;
        timeBox.AddChild(_time);

        var bottom = new PanelContainer { Name = "SurfaceBuildPalette", MouseFilter = MouseFilterEnum.Stop };
        VisualUi.ContainPointerInput(bottom);
        AddChild(bottom); _overlayPanels.Add(bottom);
        bottom.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        bottom.GrowVertical = GrowDirection.Begin;
        bottom.OffsetLeft = 18; bottom.OffsetRight = -18; bottom.OffsetBottom = -18;
        bottom.AddThemeStyleboxOverride("panel", VisualUi.Surface(false, 12));
        var column = new VBoxContainer(); column.AddThemeConstantOverride("separation", 7); bottom.AddChild(column);
        var statusRow = new HBoxContainer(); column.AddChild(statusRow);
        _status = VisualUi.Text("", 14, VisualUi.Accent, true);
        _status.Name = "SurfaceStatus"; _status.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        statusRow.AddChild(_status);
        _rotate = VisualUi.Button("Rotate 15°", "Rotate the placement preview (R)", RotatePreview);
        _rotate.Name = "SurfaceRotate"; statusRow.AddChild(_rotate);
        _cancel = VisualUi.Button("Cancel", "Cancel building placement (Esc)", () =>
        { if (!InputBlocked) CancelPlacement(); });
        _cancel.Name = "SurfaceCancel"; statusRow.AddChild(_cancel);
        _remove = VisualUi.Button("Demolish", "Remove the selected surface building", RemoveSelectedBuilding);
        _remove.Name = "SurfaceRemove"; _remove.Visible = false; statusRow.AddChild(_remove);
        _upgrade = VisualUi.Button("Upgrade", "Upgrade the selected completed building", UpgradeSelectedBuilding);
        _upgrade.Name = "SurfaceUpgrade"; _upgrade.Visible = false; statusRow.AddChild(_upgrade);
        _toggleOperation = VisualUi.Button("Shut down", "Suspend or restart the selected building", ToggleSelectedBuildingOperation);
        _toggleOperation.Name = "SurfaceToggleOperation"; _toggleOperation.Visible = false; statusRow.AddChild(_toggleOperation);
        _palette = new GridContainer { Columns = 3 };
        _palette.AddThemeConstantOverride("h_separation", 10);
        _palette.AddThemeConstantOverride("v_separation", 10);
        column.AddChild(_palette);
        _instructions = VisualUi.Text("", 12, VisualUi.Muted); column.AddChild(_instructions);
        CancelPlacement();
    }

    private void UpgradeSurfaceHub()
    {
        if (InputBlocked || _upgradeHub is null) return;
        var result = _upgradeHub();
        ShowMessage(result.Message, result.Accepted);
        RefreshSnapshot();
    }

    private void AddBuildButton(UiSurfaceBuildOption option)
    {
        var button = new Button
        {
            Name = "SurfaceBuild_" + option.Id, ToggleMode = true, FocusMode = FocusModeEnum.All,
            CustomMinimumSize = new(280, 92), SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = $"{option.Name}: {option.Description}. Authorization costs {_snapshot?.Currency.Format(option.CreditCost) ?? option.CreditCost.ToString("N0")}; construction consumes {option.IndustryCost:N0} materials over time.",
        };
        button.Pressed += () => SelectBuilding(option.Id);
        _palette.AddChild(button); _buildButtons.Add(option.Id, button);
        var content = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        button.AddChild(content);
        content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        content.OffsetLeft = 7; content.OffsetRight = -7; content.OffsetTop = 7; content.OffsetBottom = -7;
        content.AddThemeConstantOverride("separation", 8);
        content.AddChild(CreateBuildingThumbnail(option.Id));
        var labels = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
        content.AddChild(labels);
        labels.AddChild(VisualUi.Text(option.Name, 16, new Color("edf0e7")));
        labels.AddChild(VisualUi.Text($"{option.IndustryCost:N0} materials · {_snapshot?.Currency.Format(option.CreditCost) ?? option.CreditCost.ToString("N0")}", 14, VisualUi.Gold));
        labels.AddChild(VisualUi.Text(option.Description, 12, VisualUi.Muted, true));
    }

    private static Control CreateBuildingThumbnail(string id)
    {
        var container = new SubViewportContainer
        {
            CustomMinimumSize = new(86, 74), Stretch = true, MouseFilter = MouseFilterEnum.Ignore,
        };
        var viewport = new SubViewport
        {
            Size = new(172, 148), OwnWorld3D = true, TransparentBg = true, GuiDisableInput = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once, Msaa3D = Viewport.Msaa.Msaa2X,
        };
        container.AddChild(viewport);
        var root = new Node3D(); viewport.AddChild(root);
        root.AddChild(new WorldEnvironment { Environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color(0, 0, 0, 0),
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new("afc4cb"), AmbientLightEnergy = .8f,
        } });
        root.AddChild(new DirectionalLight3D { RotationDegrees = new(-50, -30, 0), LightEnergy = 1.5f });
        var visual = SurfaceBuildingVisuals.Create(id); root.AddChild(visual);
        visual.UpdateState(new(0, id, "", 0, 0, 0, 1, 0, true, true));
        var camera = new Camera3D { Position = new(28, 26, 35), Projection = Camera3D.ProjectionType.Orthogonal, Size = 40, Current = true };
        // The thumbnail is assembled before its container enters the scene tree. Set the local
        // basis directly; Node3D.LookAt would require a live global transform at this point.
        camera.Basis = Basis.LookingAt(new Vector3(0, 5, 0) - camera.Position, Vector3.Up);
        root.AddChild(camera);
        return container;
    }
}

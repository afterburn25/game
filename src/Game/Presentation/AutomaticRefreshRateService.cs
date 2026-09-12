using System;
using System.Collections.Generic;

namespace Game.Presentation;

public readonly record struct RefreshDisplayMode(
    string DeviceName, int Width, int Height, int RefreshHz, bool Progressive, object? PlatformState = null);

public interface IRefreshRatePlatform
{
    string? FindDisplayForWindow(nint windowHandle);
    RefreshDisplayMode? GetCurrentMode(string deviceName);
    IReadOnlyList<RefreshDisplayMode> GetSupportedModes(string deviceName);
    string? TryApplyTemporary(RefreshDisplayMode mode);
    string? TryRestore(RefreshDisplayMode originalMode);
}

public readonly record struct RefreshRateActivation(int EffectiveHz, bool ChangedMode, string? Error);

public sealed class AutomaticRefreshRateService : IDisposable
{
    private readonly IRefreshRatePlatform _platform;
    private RefreshDisplayMode? _originalMode;
    private string? _observedDevice;
    private int _effectiveHz = RefreshRatePolicy.FallbackHz;

    public AutomaticRefreshRateService(IRefreshRatePlatform platform) => _platform = platform;

    public bool HasPendingRestore => _originalMode is not null;
    public string? LastError { get; private set; }

    public RefreshRateActivation Activate(nint windowHandle, int fallbackHz)
    {
        var device = _platform.FindDisplayForWindow(windowHandle);
        if (string.IsNullOrWhiteSpace(device))
            return Result(RefreshRatePolicy.Normalize(fallbackHz), false,
                "Windows could not identify the monitor containing the game window.");

        if (_originalMode is not null && !string.Equals(_originalMode.Value.DeviceName, device, StringComparison.Ordinal))
        {
            var restoreError = Restore();
            if (restoreError is not null)
                return Result(_effectiveHz, true, restoreError);
        }

        if (string.Equals(_observedDevice, device, StringComparison.Ordinal))
            return Result(_effectiveHz, _originalMode is not null, LastError);

        var current = _platform.GetCurrentMode(device);
        if (current is null)
            return Observe(device, RefreshRatePolicy.Normalize(fallbackHz),
                "Windows could not read the monitor's current display mode.");

        var target = RefreshRatePolicy.HighestProgressiveAtCurrentResolution(
            _platform.GetSupportedModes(device), current.Value);
        if (target is null || target.Value.RefreshHz <= current.Value.RefreshHz)
            return Observe(device, RefreshRatePolicy.Normalize(current.Value.RefreshHz), null);

        var requested = target.Value with { PlatformState = current.Value.PlatformState };
        var applyError = _platform.TryApplyTemporary(requested);
        if (applyError is not null)
            return Observe(device, RefreshRatePolicy.Normalize(current.Value.RefreshHz), applyError);

        _originalMode = current.Value;
        _observedDevice = device;
        _effectiveHz = requested.RefreshHz;
        LastError = null;
        return new RefreshRateActivation(_effectiveHz, true, null);
    }

    public string? Deactivate()
    {
        var error = Restore();
        if (error is null)
        {
            _observedDevice = null;
            _effectiveHz = RefreshRatePolicy.FallbackHz;
        }
        return error;
    }

    public void Dispose() => Deactivate();

    private string? Restore()
    {
        if (_originalMode is null)
        {
            LastError = null;
            return null;
        }
        var error = _platform.TryRestore(_originalMode.Value);
        if (error is not null)
        {
            LastError = error;
            return error;
        }
        _originalMode = null;
        LastError = null;
        return null;
    }

    private RefreshRateActivation Observe(string device, int effectiveHz, string? error)
    {
        _observedDevice = device;
        _effectiveHz = effectiveHz;
        return Result(effectiveHz, false, error);
    }

    private RefreshRateActivation Result(int effectiveHz, bool changedMode, string? error)
    {
        LastError = error;
        return new RefreshRateActivation(effectiveHz, changedMode, error);
    }
}

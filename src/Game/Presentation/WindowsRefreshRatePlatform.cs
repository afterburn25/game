using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;

namespace Game.Presentation;

/// <summary>Windows-only temporary display-mode helper. It never writes the registry; callers
/// retain the raw original DEVMODE and restore it on every preview exit.</summary>
internal sealed class WindowsRefreshRatePlatform
{
    private const int DevModeSize = 220, EnumCurrentSettings = -1, CdsTest = 2, CdsFullscreen = 4, DispChangeSuccessful = 0;
    private byte[]? _original;
    private string? _device;
    private int _raisedHz;

    public int TryRaiseFor(Window window, int fallbackHz)
    {
        if (_original is not null) return _raisedHz > 0 ? _raisedHz : fallbackHz;
        if (!OperatingSystem.IsWindows()) return fallbackHz;
        var hwnd = (IntPtr)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, window.GetWindowId());
        if (hwnd == IntPtr.Zero) return fallbackHz;
        var monitor = MonitorFromWindow(hwnd, 2); if (monitor == IntPtr.Zero) return fallbackHz;
        var info = new MonitorInfoEx { CbSize = Marshal.SizeOf<MonitorInfoEx>() };
        if (!GetMonitorInfoW(monitor, ref info)) return fallbackHz;
        var current = Read(info.DeviceName, EnumCurrentSettings); if (current is null) return fallbackHz;
        _device = info.DeviceName; _original = current;
        var width = BitConverter.ToInt32(current, 172); var height = BitConverter.ToInt32(current, 176);
        var modes = new List<(int Width,int Height,int RefreshHz,bool Progressive)>();
        for (var i = 0; ; i++) { var mode = Read(_device, i); if (mode is null) break; modes.Add((BitConverter.ToInt32(mode,172), BitConverter.ToInt32(mode,176), BitConverter.ToInt32(mode,184), true)); }
        var targetHz = RefreshRatePolicy.HighestSupportedAtCurrentResolution(modes, width, height, fallbackHz);
        if (targetHz <= fallbackHz) return fallbackHz;
        var target = (byte[])current.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(targetHz), 0, target, 184, 4);
        _raisedHz = Change(_device, target, CdsTest) == DispChangeSuccessful && Change(_device, target, CdsFullscreen) == DispChangeSuccessful ? targetHz : fallbackHz;
        return _raisedHz;
    }

    public void Restore() { if (_original is not null && _device is not null) Change(_device, _original, CdsFullscreen); _original = null; _device = null; _raisedHz = 0; }
    private static byte[]? Read(string device, int index) { var p=Marshal.AllocHGlobal(DevModeSize); try { Marshal.Copy(new byte[DevModeSize],0,p,DevModeSize); Marshal.WriteInt16(p,68,DevModeSize); if(!EnumDisplaySettingsW(device,index,p)) return null; var b=new byte[DevModeSize]; Marshal.Copy(p,b,0,DevModeSize); return b; } finally { Marshal.FreeHGlobal(p); } }
    private static int Change(string device, byte[] mode, int flags) { var p=Marshal.AllocHGlobal(DevModeSize); try { Marshal.Copy(mode,0,p,DevModeSize); return ChangeDisplaySettingsExW(device,p,IntPtr.Zero,flags,IntPtr.Zero); } finally { Marshal.FreeHGlobal(p); } }
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] private struct MonitorInfoEx { public int CbSize; public int L,T,R,B; public int WL,WT,WR,WB; public uint Flags; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string DeviceName; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern bool GetMonitorInfoW(IntPtr monitor,ref MonitorInfoEx info);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern bool EnumDisplaySettingsW(string device,int mode,IntPtr devmode);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern int ChangeDisplaySettingsExW(string device,IntPtr devmode,IntPtr hwnd,int flags,IntPtr param);
}

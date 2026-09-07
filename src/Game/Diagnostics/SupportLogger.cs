using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Godot;

namespace Game.Diagnostics;

public static class SupportLogger
{
    private static readonly object Gate = new();
    private static string _sessionId = string.Empty;
    private static string _logDirectory = string.Empty;
    private static string _logPath = string.Empty;
    private static string _systemInfoPath = string.Empty;
    private static bool _initialized;

    public static string SessionId => _sessionId;
    public static string LogDirectory => _logDirectory;

    public static void Initialize()
    {
        lock (Gate)
        {
            if (_initialized) return;

            _sessionId = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            _logDirectory = ProjectSettings.GlobalizePath("user://logs");
            Directory.CreateDirectory(_logDirectory);
            _logPath = Path.Combine(_logDirectory, $"game-{_sessionId}.log");
            _systemInfoPath = Path.Combine(_logDirectory, $"system-{_sessionId}.txt");

            File.WriteAllText(_systemInfoPath, BuildSystemInfo(), Encoding.UTF8);
            _initialized = true;
            Log("session", $"Session {_sessionId} started.");
        }
    }

    public static void Log(string category, string message)
    {
        if (!_initialized) Initialize();
        var line = $"{DateTimeOffset.UtcNow:O} [{_sessionId}] [{category}] {message}{System.Environment.NewLine}";
        lock (Gate)
            File.AppendAllText(_logPath, line, Encoding.UTF8);
    }

    public static string ExportSupportBundle(string? savePath = null)
    {
        if (!_initialized) Initialize();

        var supportDirectory = ProjectSettings.GlobalizePath("user://support");
        Directory.CreateDirectory(supportDirectory);
        var output = Path.Combine(supportDirectory, $"support-{_sessionId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");

        using var archive = ZipFile.Open(output, ZipArchiveMode.Create);
        AddIfExists(archive, _logPath, Path.GetFileName(_logPath));
        AddIfExists(archive, _systemInfoPath, Path.GetFileName(_systemInfoPath));
        if (!string.IsNullOrWhiteSpace(savePath))
            AddIfExists(archive, savePath, Path.GetFileName(savePath));

        return output;
    }

    private static void AddIfExists(ZipArchive archive, string path, string entryName)
    {
        if (File.Exists(path))
            archive.CreateEntryFromFile(path, entryName, CompressionLevel.Fastest);
    }

    private static string BuildSystemInfo()
    {
        var lines = new List<string>
        {
            $"Session={_sessionId}",
            $"GameVersion={GameVersion.Current}",
            $"Godot={Engine.GetVersionInfo()["string"]}",
            $"OS={OS.GetName()}",
            $"OSVersion={OS.GetVersion()}",
            $"CPU={OS.GetProcessorName()}",
            $"CPUThreads={OS.GetProcessorCount()}",
            $"GPU={RenderingServer.GetVideoAdapterName()}",
            $"GPUVendor={RenderingServer.GetVideoAdapterVendor()}",
            $"GraphicsAPI={RenderingServer.GetVideoAdapterApiVersion()}",
            $"Locale={OS.GetLocale()}",
            $"DisplayCount={DisplayServer.GetScreenCount()}",
        };

        return string.Join(System.Environment.NewLine, lines) + System.Environment.NewLine;
    }
}

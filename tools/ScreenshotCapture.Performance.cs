using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Game.Presentation;
using Game.Presentation.Spatial;
using Game.Simulation;
using Godot;

namespace Game.Tools;

public partial class ScreenshotCapture
{
    // This focus deliberately keeps the supplied campaign. Run with isolated APPDATA and a
    // copied save to exercise an aged game; the normal fresh-campaign suite cannot cover it.
    private async Task VerifyCampaignPerformanceAsync(MainMenuLayer menu)
    {
        // Performance evidence is intentionally captured at the release aged-save target
        // even though CaptureSuiteAsync starts every non-production job at 1280x720.
        var performanceSize = new Vector2I(2560, 1440);
        if (int.TryParse(System.Environment.GetEnvironmentVariable("STELLAR_PERFORMANCE_WIDTH"), out var configuredWidth) &&
            int.TryParse(System.Environment.GetEnvironmentVariable("STELLAR_PERFORMANCE_HEIGHT"), out var configuredHeight))
            performanceSize = new Vector2I(configuredWidth, configuredHeight);
        var sampleSeconds = double.TryParse(System.Environment.GetEnvironmentVariable("STELLAR_PERFORMANCE_SECONDS"), out var configuredSeconds)
            ? Math.Max(1, configuredSeconds) : 5;
        var diagnosticMode = string.Equals(System.Environment.GetEnvironmentVariable("STELLAR_PERFORMANCE_DIAGNOSTIC"), "1", StringComparison.Ordinal);
        var originalMaxFps = Engine.MaxFps;
        var originalVsync = DisplayServer.WindowGetVsyncMode();
        var failures = new List<string>();
        if (diagnosticMode)
        {
            Engine.MaxFps = 0;
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        }
        await ResizeResponsiveWindowAsync(performanceSize);
        await WaitFramesAsync(3);
        using var nativeImage = GetViewport().GetTexture().GetImage();
        Require(GetWindow().Size == performanceSize && nativeImage.GetSize() == performanceSize,
            $"Performance capture resolution did not settle at {performanceSize}: {ResponsiveDiagnostics(performanceSize)}");
        var savePath = ProjectSettings.GlobalizePath("user://saves/autosave.json");
        Require(File.Exists(savePath), $"Aged-save performance fixture is missing: {savePath}");
        var fixtureSha = HashFile(savePath);
        var samples = new List<object>();
        var clock = (SimulationClock)typeof(Main).GetField("_clock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_main)!;
        async Task Sample(string name, bool running)
        {
            _main.UiSetPaused(!running, announce: false);
            await WaitFramesAsync(90); // Scene/shader warm-up is separate from steady-state pacing.
            var startDay = clock.SimulationDays;
            var watch = Stopwatch.StartNew();
            var previous = watch.Elapsed.TotalMilliseconds;
            var frames = new List<double>();
            do
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var now = watch.Elapsed.TotalMilliseconds;
                frames.Add(now - previous);
                previous = now;
            } while (watch.Elapsed.TotalSeconds < sampleSeconds);
            var sorted = frames.OrderBy(value => value).ToArray();
            var p95 = sorted[Math.Min(sorted.Length - 1, (int)(sorted.Length * .95))];
            var fps = frames.Count / watch.Elapsed.TotalSeconds;
            var advancedDays = clock.SimulationDays - startDay;
            var monitorHz = DisplayServer.ScreenGetRefreshRate();
            var capped = !diagnosticMode && monitorHz > 0 && Engine.MaxFps > 0;
            var minimumFps = double.TryParse(System.Environment.GetEnvironmentVariable("STELLAR_MIN_FPS"), out var configured) ? configured : 60;
            var fpsFloor = capped ? monitorHz * .99 : minimumFps;
            var processMs = Performance.GetMonitor(Performance.Monitor.TimeProcess);
            var physicsMs = Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess);
            var drawCalls = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
            var objects = Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame);
            var vram = Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed);
            var over33 = frames.Count(value => value > 33.333);
            var over50 = frames.Count(value => value > 50);
            samples.Add(new { name, frames = frames.Count, fps, p95FrameMs = p95,
                maxFrameMs = sorted[^1], frameP99Ms = sorted[Math.Min(sorted.Length - 1, (int)(sorted.Length * .99))],
                framesOver33Ms = over33, framesOver50Ms = over50, advancedDays, running,
                width = GetWindow().Size.X, height = GetWindow().Size.Y, monitorHz,
                vsync = DisplayServer.WindowGetVsyncMode().ToString(), maxFps = Engine.MaxFps,
                processMs, physicsMs, drawCalls, objects, vram });
            File.WriteAllText(Path.Combine(_outputDirectory, "performance.json"), JsonSerializer.Serialize(new {
                source = System.Environment.GetEnvironmentVariable("STELLAR_CAPTURE_SHA"),
                resolution = new { width = performanceSize.X, height = performanceSize.Y },
                fixture = new { path = savePath, sha256 = fixtureSha },
                gpu = RenderingServer.GetVideoAdapterName(), samples }, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print($"STELLAR_PERFORMANCE {name} fps={fps:F1} p95_ms={p95:F2} max_ms={sorted[^1]:F2} advanced_days={advancedDays:F3}");
            Require(!running || advancedDays > .1, $"{name}: simulation did not advance during measurement.");
            Require(running || advancedDays == 0, $"{name}: paused simulation advanced.");
            if (fps < fpsFloor || (!capped && p95 > 20))
                failures.Add($"{name}: frame budget failed ({fps:F1} FPS, p95 {p95:F1} ms, floor {fpsFloor:F1}).");
            await SaveViewportAsync("performance-" + name + ".png", performanceSize.X, performanceSize.Y);
        }
        try
        {
        await ClickNamedButtonAsync(menu, "ResumeCampaign");
        _main.UiSelectHomeSystem();
        await WaitForCameraAsync();
        await Sample("region-paused", false);
        await Sample("region-running", true);
        _main.UiShowGalaxyOverview();
        await WaitForCameraAsync();
        await Sample("galaxy-running", true);
        _main.UiSelectHomeSystem();
        _main.UiOpenSelectedSystem();
        await WaitForCameraAsync();
        await Sample("orbits-running", true);
        var canvas = _main.GetNode<SystemSpatialCanvas>("SystemSpatialCanvas");
        var earth = _main.UiSystemBodies.First(body => body.SurfaceKey == "earth");
        Require(canvas.FocusBody(earth.BodyId), "Earth could not be focused for performance measurement.");
        await WaitForCameraAsync();
        await Sample("planet-running", true);
        _main.UiOpenPlanetSurface(earth.BodyId);
        Require(_main.UiIsSurfaceOpen, "Earth surface did not open for performance measurement.");
        await Sample("surface-running", true);
        _main.UiReturnToOrbit();
        _main.UiNavigateBack();
        await WaitForCameraAsync();
        await Sample("orbits-return-running", true);
        }
        finally
        {
            Engine.MaxFps = originalMaxFps;
            DisplayServer.WindowSetVsyncMode(originalVsync);
        }
        foreach (var failure in failures)
            GD.PushError(failure);
        Require(failures.Count == 0, string.Join(" ", failures));
    }
}

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
            } while (watch.Elapsed.TotalSeconds < 5);
            var sorted = frames.OrderBy(value => value).ToArray();
            var p95 = sorted[Math.Min(sorted.Length - 1, (int)(sorted.Length * .95))];
            var fps = frames.Count / watch.Elapsed.TotalSeconds;
            var advancedDays = clock.SimulationDays - startDay;
            samples.Add(new { name, frames = frames.Count, fps, p95FrameMs = p95,
                maxFrameMs = sorted[^1], advancedDays, running, width = GetWindow().Size.X, height = GetWindow().Size.Y });
            File.WriteAllText(Path.Combine(_outputDirectory, "performance.json"), JsonSerializer.Serialize(new {
                source = System.Environment.GetEnvironmentVariable("STELLAR_CAPTURE_SHA"),
                gpu = RenderingServer.GetVideoAdapterName(), samples }, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print($"STELLAR_PERFORMANCE {name} fps={fps:F1} p95_ms={p95:F2} max_ms={sorted[^1]:F2} advanced_days={advancedDays:F3}");
            Require(!running || advancedDays > .1, $"{name}: simulation did not advance during measurement.");
            Require(running || advancedDays == 0, $"{name}: paused simulation advanced.");
            // Configurable hardware budget, default 30 FPS with no recurrent >50ms stalls.
            var minimumFps = double.TryParse(System.Environment.GetEnvironmentVariable("STELLAR_MIN_FPS"), out var configured) ? configured : 30;
            Require(fps >= minimumFps && p95 < 50, $"{name}: frame budget failed ({fps:F1} FPS, p95 {p95:F1} ms).");
            await SaveViewportAsync("performance-" + name + ".png", 0, 0);
        }
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
}

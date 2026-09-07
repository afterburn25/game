using System;

namespace Game.Simulation;

public sealed class SimulationClock
{
    public enum SpeedLevel
    {
        Paused = 0,
        Normal = 1,
        Fast = 2,
        VeryFast = 3,
        Maximum = 4,
    }

    private readonly double[] _multipliers = { 0.0, 1.0, 2.0, 3.0, 4.0 };

    public SpeedLevel Speed { get; private set; } = SpeedLevel.Normal;
    public double SimulationSeconds { get; private set; }
    public double EffectiveMultiplier { get; private set; } = 1.0;
    public double RequestedMultiplier => _multipliers[(int)Speed];
    public double BacklogSeconds { get; private set; }

    public void SetSpeed(SpeedLevel speed) => Speed = speed;

    public void Restore(double simulationSeconds)
    {
        SimulationSeconds = Math.Max(0.0, simulationSeconds);
        BacklogSeconds = 0.0;
    }

    public void Advance(double realDeltaSeconds, double maxSimulationStepSeconds = 0.25)
    {
        var requested = RequestedMultiplier;
        if (requested <= 0.0)
        {
            EffectiveMultiplier = 0.0;
            return;
        }

        var requestedSimulationDelta = realDeltaSeconds * requested;
        var accepted = Math.Min(requestedSimulationDelta, maxSimulationStepSeconds);
        BacklogSeconds = Math.Max(0.0, BacklogSeconds + requestedSimulationDelta - accepted);

        // Drain only a bounded amount. The UI stays responsive even when the machine cannot sustain requested speed.
        var drain = Math.Min(BacklogSeconds, maxSimulationStepSeconds * 0.20);
        accepted += drain;
        BacklogSeconds -= drain;

        SimulationSeconds += accepted;
        EffectiveMultiplier = realDeltaSeconds > 0.0 ? accepted / realDeltaSeconds : requested;
    }
}

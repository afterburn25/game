using System;

namespace Game.Simulation.Economy;

public enum TreasuryHealthState
{
    Surplus,
    Deficit,
    Depleted,
}

public sealed record TreasuryHealthSnapshot(
    TreasuryHealthState State,
    double RunwayDays,
    double Balance,
    double NetPerDay);

public static class TreasuryHealth
{
    public static TreasuryHealthSnapshot Assess(double balance, double netPerDay)
    {
        if (!double.IsFinite(balance) || balance < 0.0) throw new ArgumentOutOfRangeException(nameof(balance));
        if (!double.IsFinite(netPerDay)) throw new ArgumentOutOfRangeException(nameof(netPerDay));
        if (netPerDay >= 0.0)
            return new(TreasuryHealthState.Surplus, double.PositiveInfinity, balance, netPerDay);
        if (balance <= 0.000001)
            return new(TreasuryHealthState.Depleted, 0.0, balance, netPerDay);
        return new(TreasuryHealthState.Deficit, balance / -netPerDay, balance, netPerDay);
    }
}

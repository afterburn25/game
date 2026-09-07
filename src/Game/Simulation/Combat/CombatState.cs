namespace Game.Simulation.Combat;

public enum MilitaryOrderType
{
    Hold,
    Defend,
    Attack,
    Retreat,
}

/// <summary>
/// Compact combat-relevant state attached to the existing strategic FleetState.
/// Active engagement bookkeeping remains transient; damage, orders and disengagement
/// state are persistent so battle consequences survive save/load.
/// </summary>
public sealed class FleetCombatState
{
    public string ProfileId { get; set; } = string.Empty;
    public double Shields { get; set; }
    public double Armor { get; set; }
    public double Hull { get; set; }
    public double WeaponCooldownRemainingDays { get; set; }
    public MilitaryOrderType Order { get; set; } = MilitaryOrderType.Hold;
    public int? TargetFleetId { get; set; }
    public int? DefendSystemId { get; set; }
    public double RetreatProgressDays { get; set; }
    public bool RetreatStarted { get; set; }
    public bool IsDisengaged { get; set; }
    public int? DisengagedSystemId { get; set; }
}

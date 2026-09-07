using Game.Simulation.AI;

namespace Game.Simulation.Models;

public sealed record CivilizationState(
    int Id,
    string Name,
    int HomeSystemId,
    CivilizationArchetype Archetype,
    CivilizationTraits Traits,
    bool IsPlayer,
    CivilizationDevelopmentStage DevelopmentStage,
    bool IsSeededAncient = false,
    bool ExpansionAllowed = true,
    bool NeutralUnlessProvoked = false
);

public enum CivilizationDevelopmentStage
{
    PreWarp,
    WarpCapable,
    AncientSpacefaring,
}

public enum CivilizationArchetype
{
    Adaptive,
    Mercantile,
    Scientific,
    Militarist,
    Isolationist,
    Territorial,
    Diplomatic,
    HonorBound,
    AncientCustodian,
    AncientArchivist,
}

using System.Runtime.CompilerServices;

namespace Game.Simulation.Validation;

internal static class CombatRepairDemandSyncValidation
{
    [ModuleInitializer]
    internal static void RunCombatRepairDemandSyncCheck()
    {
        CombatRepairDemandValidation.ValidateNonMutatingCombatRepairDemand();
        Console.WriteLine("PASS: synchronized Combat repair-demand regression");
    }
}

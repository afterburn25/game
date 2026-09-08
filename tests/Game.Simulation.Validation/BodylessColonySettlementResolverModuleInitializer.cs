using System.Runtime.CompilerServices;

namespace Game.Simulation.Validation;

internal static class BodylessColonySettlementResolverModuleInitializer
{
    [ModuleInitializer]
    internal static void Run()
    {
        BodylessColonySettlementResolverValidation.ValidateReadStatusAndFoundingUseSameSpeciesRelativeBody();
        Console.WriteLine("PASS: shared bodyless colony settlement resolver");
    }
}

using System.Runtime.CompilerServices;

namespace Game.Simulation.Validation;

internal static class ScienceReconnaissanceSignatureModuleInitializer
{
    [ModuleInitializer]
    internal static void Run()
    {
        ScienceReconnaissanceSignatureValidation.ValidateScienceTransitionEmitsPositiveSignaturesOnce();
        ScienceReconnaissanceSignatureValidation.ValidateDirectCompletionSkipsTransientSignatures();
        Console.WriteLine("PASS: science reconnaissance signature event consistency");
    }
}

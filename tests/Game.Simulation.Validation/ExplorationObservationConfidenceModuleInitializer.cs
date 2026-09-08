using System.Runtime.CompilerServices;

namespace Game.Simulation.Validation;

internal static class ExplorationObservationConfidenceModuleInitializer
{
    [ModuleInitializer]
    internal static void Run()
    {
        ExplorationObservationConfidenceValidation.ValidateConfidenceTracksObserverKnowledgeWithoutLeaks();
        Console.WriteLine("PASS: observer-local exploration observation confidence");
    }
}

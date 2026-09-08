using System.Runtime.CompilerServices;

namespace Game.Simulation.Validation;

internal static class LocalSurveyFirstContactModuleInitializer
{
    [ModuleInitializer]
    internal static void Run()
    {
        LocalSurveyFirstContactValidation.ValidateScoutSurveyCreatesDirectionalContact();
        LocalSurveyFirstContactValidation.ValidateScienceSurveyCreatesDirectionalContact();
        LocalSurveyFirstContactValidation.ValidatePassiveColocationDoesNotCreateContact();
        Console.WriteLine("PASS: directional first contact during active local survey");
    }
}

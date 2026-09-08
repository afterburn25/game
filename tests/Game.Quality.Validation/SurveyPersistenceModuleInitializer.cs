using System;
using System.Runtime.CompilerServices;

namespace Game.Quality.Validation;

internal static class SurveyPersistenceModuleInitializer
{
    [ModuleInitializer]
    internal static void RunSurveyPersistenceChecks()
    {
        SurveyPersistenceValidation.Run();
        Console.WriteLine("PASS: survey/discovery persistence round trip");
    }
}

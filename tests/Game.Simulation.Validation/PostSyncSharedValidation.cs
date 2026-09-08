using System.Runtime.CompilerServices;

namespace Game.Simulation.Validation;

internal static class PostSyncSharedValidation
{
    [ModuleInitializer]
    internal static void Run()
    {
        CombatCasualtyValidation.ValidateEmbarkedPopulationCasualties();
        Console.WriteLine("PASS: embarked population casualties on fleet destruction");

        CombatBatchOrderValidation.ValidateMixedSelectionBatchOrders();
        Console.WriteLine("PASS: deterministic batch military command behavior");

        CombatSystemPresenceValidation.ValidateAuthoritativeSystemMilitaryPresence();
        Console.WriteLine("PASS: authoritative system military presence and interdiction");

        CombatRepairDemandValidation.ValidateNonMutatingCombatRepairDemand();
        Console.WriteLine("PASS: non-mutating Combat repair demand");

        CombatRepairApplicationValidation.ValidateExternallyBudgetedCombatRepairApplication();
        Console.WriteLine("PASS: externally-budgeted Combat repair application");

        SurveyOperationsValidation.ValidateDeterministicBoundedSurveyEffortAndTickInvariance();
        Console.WriteLine("PASS: deterministic bounded survey effort and tick invariance");

        SurveyOperationsValidation.ValidateReconnaissanceSignalsRemainPositiveOnly();
        Console.WriteLine("PASS: reconnaissance signals remain positive only");

        SurveyOperationsValidation.ValidatePositiveSignaturesAndConfirmedBodyDiscoveries();
        Console.WriteLine("PASS: positive signatures and confirmed body discoveries");

        ExplorationMissionPlanningValidation.ValidateBoundedObserverSafeMissionPlan();
        Console.WriteLine("PASS: bounded observer-safe exploration mission plan");

        ExplorationMissionPlanningValidation.ValidateSharedReachRejectionAndLocalOrders();
        Console.WriteLine("PASS: shared reach rejection and local survey orders");

        ExplorationMissionPlanningValidation.ValidateAiUsesSharedMissionPlan();
        Console.WriteLine("PASS: AI uses shared exploration mission plan");

        ExplorationMissionStatusValidation.ValidateTransitEtaAndSurveyInformationBoundary();
        Console.WriteLine("PASS: transit ETA and survey information boundary");

        ExplorationMissionStatusValidation.ValidateLocalScoutAndSciencePhases();
        Console.WriteLine("PASS: local scout and science mission phases");

        ExplorationMissionStatusValidation.ValidateColonySettlementReadiness();
        Console.WriteLine("PASS: colony settlement readiness status");
    }
}

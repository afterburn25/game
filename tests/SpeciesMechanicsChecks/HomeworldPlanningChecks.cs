using System.Runtime.CompilerServices;
using Game.Simulation.Generation;
using Game.Simulation.Species;

internal static class HomeworldPlanningChecks
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Run();
        Console.WriteLine("PASS: natural Species homeworld planning and founding-colony anchoring");
    }

    public static void Run()
    {
        var settings = new GalaxyGenerationSettings();
        var planner = new SpeciesHomeworldPlanner();
        var evaluator = new PlanetarySpeciesHabitabilityEvaluator();
        var seeds = new long[]
        {
            0x1001,
            0x2002,
            0x3003,
            0x4004,
            0x5005,
            0x6006,
            0x7007,
            0x8008,
            0x9009,
            0xA00A,
            0xB00B,
            0xC00C,
        };

        foreach (var seed in seeds)
        {
            var generated = new GalaxyGenerator().Generate(seed, settings);
            var civilizationCount = settings.PreWarpCivilizationCount + settings.AncientCivilizationCount;
            var speciesIds = Enumerable.Range(0, civilizationCount)
                .Select(id => SpeciesAssignmentPolicy.AssignNewCampaign(seed, id))
                .ToArray();
            var assignments = planner.Plan(generated.Systems, generated.PlanetaryBodies, speciesIds);
            var assignmentByCivilization = assignments.ToDictionary(assignment => assignment.CivilizationId);

            Require(assignments.Count == civilizationCount,
                $"seed {seed}: homeworld planner did not assign every civilization");
            Require(assignments.Select(assignment => assignment.SystemId).Distinct().Count() == civilizationCount,
                $"seed {seed}: homeworld planner reused a founding star system");
            Require(assignments.Select(assignment => assignment.PlanetaryBodyId).Distinct().Count() == civilizationCount,
                $"seed {seed}: homeworld planner reused a founding planetary body");
            Require(generated.Civilizations.Count == civilizationCount,
                $"seed {seed}: generated civilization count diverged from requested count");
            Require(generated.Colonies.Count == civilizationCount,
                $"seed {seed}: founding colony count diverged from civilization count");

            foreach (var assignment in assignments)
            {
                Require(assignment.CivilizationId >= 0 && assignment.CivilizationId < civilizationCount,
                    $"seed {seed}: planner produced invalid civilization ID {assignment.CivilizationId}");
                Require(assignment.SpeciesId == speciesIds[assignment.CivilizationId],
                    $"seed {seed}: planner changed deterministic Species identity for civilization {assignment.CivilizationId}");

                var civilization = generated.Civilizations.First(c => c.Id == assignment.CivilizationId);
                Require(civilization.SpeciesId == assignment.SpeciesId,
                    $"seed {seed}: civilization {civilization.Id} Species diverged from deterministic assignment");
                Require(civilization.HomeSystemId == assignment.SystemId,
                    $"seed {seed}: civilization {civilization.Id} did not use its planned natural home system");

                var colony = generated.Colonies.Single(c => c.CivilizationId == civilization.Id);
                Require(colony.SystemId == assignment.SystemId,
                    $"seed {seed}: founding colony {colony.Id} was outside its civilization home system");
                Require(colony.PlanetaryBodyId == assignment.PlanetaryBodyId,
                    $"seed {seed}: founding colony {colony.Id} was not anchored to its planned physical homeworld");
                Require(colony.PopulationSpeciesId == assignment.SpeciesId,
                    $"seed {seed}: founding colony {colony.Id} population Species diverged from its civilization");

                var body = generated.PlanetaryBodies.First(candidate => candidate.Id == assignment.PlanetaryBodyId);
                Require(body.SystemId == assignment.SystemId,
                    $"seed {seed}: planned homeworld body was outside its assigned system");
                Require(!body.HasPreWarpCivilization,
                    $"seed {seed}: planner placed a seeded civilization on an existing native pre-warp world");

                var assessment = evaluator.Evaluate(SpeciesCatalog.Get(assignment.SpeciesId), body);
                Require(assessment.NaturallyColonizable,
                    $"seed {seed}: planner assigned non-natural homeworld {body.Id} to {assignment.SpeciesId}");
                RequireClose(assessment.NaturalHabitability, assignment.NaturalHabitability,
                    $"seed {seed}: planner's stored homeworld habitability did not match authoritative evaluation");
            }

            foreach (var civilization in generated.Civilizations)
            {
                var assignment = assignmentByCivilization[civilization.Id];
                var colony = generated.Colonies.Single(c => c.CivilizationId == civilization.Id);
                Require(colony.PlanetaryBodyId is not null,
                    $"seed {seed}: civilization {civilization.Id} still has a system-level-only founding colony");
                Require(civilization.HomeSystemId == colony.SystemId && colony.SystemId == assignment.SystemId,
                    $"seed {seed}: civilization/home-colony/planner system identity diverged");
            }
        }
    }

    private static void RequireClose(double actual, double expected, string message)
    {
        if (Math.Abs(actual - expected) > 0.000000001)
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

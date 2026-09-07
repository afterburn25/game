using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Research;

namespace Game.Simulation.Construction;

public static class ConstructionRegistry
{
    public static readonly IReadOnlyList<ConstructionProjectDefinition> All = new[]
    {
        new ConstructionProjectDefinition(
            "research_network",
            "Planetary Research Network",
            "Expand universities, laboratories, compute infrastructure, and scientific coordination.",
            700.0,
            Array.Empty<string>(),
            ConstructionCategory.Science),
        new ConstructionProjectDefinition(
            "industrial_automation",
            "Industrial Automation Program",
            "Modernize planetary production with autonomous fabrication and logistics.",
            900.0,
            Array.Empty<string>(),
            ConstructionCategory.Industry),
        new ConstructionProjectDefinition(
            "orbital_launch_complex",
            "Orbital Launch Complex",
            "Build permanent heavy-lift infrastructure needed for sustained orbital construction.",
            1100.0,
            Array.Empty<string>(),
            ConstructionCategory.Orbital),
        new ConstructionProjectDefinition(
            "orbital_shipyard",
            "Orbital Shipyard",
            "Construct a permanent orbital yard capable of assembling large interplanetary and future interstellar vessels.",
            1600.0,
            new[] { "orbital_industry" },
            ConstructionCategory.Orbital),
        new ConstructionProjectDefinition(
            "warp_test_facility",
            "Warp Test Facility",
            "A remote hardened research and engineering complex for full-scale spacetime-field experiments.",
            2200.0,
            new[] { "warp_field_control" },
            ConstructionCategory.Ftl),
    };

    public static ConstructionProjectDefinition Get(string id) =>
        All.First(project => string.Equals(project.Id, id, StringComparison.Ordinal));

    public static IReadOnlyList<ConstructionProjectDefinition> GetAvailable(ConstructionState construction, TechnologyState technology) =>
        All.Where(project =>
                !construction.CompletedProjectIds.Contains(project.Id) &&
                project.RequiredTechnologies.All(technology.CompletedTechnologyIds.Contains))
            .ToArray();
}

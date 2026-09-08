using System;

namespace Game.Simulation.Research.Adaptive;

/// <summary>
/// Composes the validated competence-aware research authority with causal Research Pressure and
/// visible-only agenda/AI planning. This remains side-by-side with the legacy gameplay research loop.
/// </summary>
public sealed class AdaptiveResearchStrategicRuntime
{
    private AdaptiveResearchStrategicRuntime(
        AdaptiveResearchAuthority authority,
        AdaptiveResearchPressureCatalog pressureCatalog,
        AdaptiveResearchPressureRuntime pressure,
        AdaptiveResearchAgendaCatalog agendaCatalog,
        AdaptiveResearchAgendaRuntime agenda)
    {
        Authority = authority;
        PressureCatalog = pressureCatalog;
        Pressure = pressure;
        AgendaCatalog = agendaCatalog;
        Agenda = agenda;
    }

    public AdaptiveResearchAuthority Authority { get; }
    public AdaptiveResearchPressureCatalog PressureCatalog { get; }
    public AdaptiveResearchPressureRuntime Pressure { get; }
    public AdaptiveResearchAgendaCatalog AgendaCatalog { get; }
    public AdaptiveResearchAgendaRuntime Agenda { get; }

    public static AdaptiveResearchStrategicRuntime LoadFromDirectory(string rootPath)
    {
        var authority = AdaptiveResearchAuthority.LoadFromDirectory(rootPath);
        var pressureCatalog = AdaptiveResearchPressureCatalog.LoadFromDirectory(rootPath, authority.Catalog);
        var pressure = new AdaptiveResearchPressureRuntime(authority.Kernel, pressureCatalog);
        var agendaCatalog = AdaptiveResearchAgendaCatalog.LoadFromDirectory(
            rootPath,
            authority.Catalog,
            authority.ExpertiseCatalog);
        var agenda = new AdaptiveResearchAgendaRuntime(authority, agendaCatalog);
        return new AdaptiveResearchStrategicRuntime(authority, pressureCatalog, pressure, agendaCatalog, agenda);
    }
}

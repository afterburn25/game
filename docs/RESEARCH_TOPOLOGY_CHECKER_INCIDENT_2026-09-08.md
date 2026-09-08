# Research topology checker crash repair — 2026-09-08

The Core session's Research agent created a local source-linked console helper under `tests/AdaptiveResearchTopologyChecks`. The initial project generated `bin/Release/net8.0/AdaptiveResearchTopologyChecks.exe` and reused that same output for several research programs through `ResearchCheckProgram`. It was not a game executable and was not a previously accepted repository checker.

The original invocation, from the research worktree root, was:

```text
dotnet run --project tests/AdaptiveResearchTopologyChecks/AdaptiveResearchTopologyChecks.csproj --configuration Release --no-restore
```

Three uncaught `System.InvalidOperationException` assertions produced the reported Windows CLR application-error dialogs. The first, `Repeated reads must not expand unchanged collapsed history.`, reproduced a real defect: display collapse updated scientific recency, so the next unchanged projection expanded the just-collapsed record. The other two failures were an incorrect newly authored capability assertion; the corrected check compares legitimate capability knowledge before and after collapse. The executable name subsequently also represented other passing suites because of the source override.

There was no scheduled application retry loop. The agent performed finite validation attempts; one build retried copying the locked apphost ten times while a failed process/dialog held it. Research work was interrupted before diagnosis. Recorded launcher sessions completed; later process samples found no named checker or WerFault process. A matching Application/.NET Runtime event 1026 was not present in the accessible event query; live process-command-line CIM inspection was permission denied, so the task's command records, generated-file manifest and terminal reproduction supplied the evidence.

## Repair and evidence

- Removed the misleading program override; the project has a fixed topology entry point.
- Disabled apphost generation; use `dotnet run`/managed DLL invocation.
- Added caught top-level reporting for full exception/inner stack and runtime/path context, returning exit 1 rather than an unhandled CLR exception.
- Added a repository-anchored Python launcher with no retries and exact exit-code propagation.
- Fixed display-only recency mutation. Added a check that a real scientific mutation still restores detailed presentation and emits deltas.
- Preserved all substantive topology assertions. An isolated original-source reproducer fails the same repeated-read assertion with a captured full stack and exit 1; the repaired source passes.
- Five repeated repaired runs, three repeated runs within maintained tests, unrelated-directory launch, and six maintained console contract tests passed. Missing root/catalog/index, malformed JSON and invalid arguments fail cleanly with useful diagnostics.
- Re-ran seven established C# research suites plus the stranded M19 outcome-policy check using actual linked research source under caught managed hosts; all eight passed. All 15 Python research validators/benchmarks passed. Temporary broader-suite hosts remain local investigation tooling, not game/source deliverables.
- The user confirmed no new dialogs after repair. No WER/global exception-suppression settings were changed.

The named checker `.exe` was removed by the normal no-apphost rebuild and was absent in subsequent searches. One obsolete intermediate `obj/Release/net8.0/apphost.exe` remained: automatic approval rejected its direct deletion even after an exact-file write grant, because sandbox approval is disabled. It is not referenced by the repaired build manifest or launcher, and is not committed. This cleanup limitation does not reactivate it.

The topology project, runner, tests and Windows/Linux CI are maintained source on the canonical `research/adaptive-research` workstream. The helper binary remains generated and uncommitted. This repair does not accept unfinished M20 provenance/event wiring or persistence, activate unregistered catalog expansion, or cut over legacy gameplay research. Full absolute paths and terminal logs are retained in the local user-facing incident report.

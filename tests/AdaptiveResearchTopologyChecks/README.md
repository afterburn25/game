# Adaptive Research topology checks

This is a maintained, Godot-independent .NET 8 console validation project. It links the actual research implementation and checks stable collapsed history, preservation of established knowledge, restoration of detail on real scientific changes, observer-visible edges, stable anchors, and bounded 1,000-year history.

Run from any directory with the repository-anchored launcher:

```text
python <repository>/scripts/validate_research_topology.py
```

Or from the repository root:

```text
dotnet run --project tests/AdaptiveResearchTopologyChecks/AdaptiveResearchTopologyChecks.csproj --configuration Release -- --repository-root .
python -m unittest discover -s tests/AdaptiveResearchTopologyChecks -p test_console.py
```

The launcher executes once, sets the repository working directory, and preserves the result. There is no retry loop. `UseAppHost=false` prevents generation/launch of a standalone checker executable. Source files use `.cs.txt` and explicit compile includes so the game's default source glob does not compile this console entry point.

Success exits 0. Exceptions exit 1 and report type, message, inner exception, full stack trace, working directory, repository/catalog paths, assembly location, and runtime. Missing catalog/index and malformed JSON are failures, not skipped validations. `--catalog-directory <path>` selects an explicit fixture catalog for diagnostic tests.

Do not reuse this project's output for other research suites through an MSBuild source override. Other checks have their own established projects. Generated `bin/` and `obj/` files are not source assets and must never be committed.

The `research-topology` workflow runs the process-level success/failure contract on Windows and Linux. A topology projection milestone is still separate from the future gameplay research cutover and from full M20 event/persistence acceptance.

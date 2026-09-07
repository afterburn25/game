# Adaptive Research CI — Known Shared Issues

This file records shared validation limitations discovered while running Adaptive Research CI. It does not transfer ownership of non-research gameplay/Testing code to this workstream.

## Godot runtime smoke can false-positive on script instantiation error

Discovered from GitHub Actions run `34163593221`, job `101870178023`.

The .NET build completed successfully, but the Godot runtime smoke command logged:

```text
ERROR: Cannot instantiate C# script because the associated class could not be found. Script: 'res://src/Game/Presentation/Main.cs'. Make sure the script exists and contains a class definition with a name that matches the filename of the script exactly (it's case-sensitive).
```

The Godot process nevertheless exited with a success status, so GitHub marked the runtime smoke step successful.

### Consequence

Until the Testing/Release workstream repairs the shared runtime smoke gate, Adaptive Research PRs may report that the **runtime smoke process step completed**, but should not claim the Godot runtime is semantically clean solely from that step.

### Ownership boundary

Adaptive Research does not own `Main.cs` or the shared gameplay/runtime smoke implementation. A GitHub issue should track the required Testing/Release fix.

Recommended gate behavior:

- capture/runtime-test Godot output;
- fail when the project cannot instantiate its main C# script/main scene;
- ideally assert that the expected main scene/class actually instantiated rather than relying only on process exit code;
- distinguish benign shutdown/editor warnings from runtime engine errors.

#!/usr/bin/env python3
"""Run the maintained, caught topology console check from its repository root; no retries."""
from pathlib import Path
import subprocess
import sys


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    project = root / "tests/AdaptiveResearchTopologyChecks/AdaptiveResearchTopologyChecks.csproj"
    command = ["dotnet", "run", "--project", str(project), "--configuration", "Release",
               "--", "--repository-root", str(root)]
    try:
        return subprocess.run(command, cwd=root, check=False).returncode
    except OSError as error:
        print(f"Could not launch topology validation: {error}\nRepository: {root}\nProject: {project}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

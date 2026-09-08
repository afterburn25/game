STELLAR CONTINUUM - WINDOWS DEMO

Extract the entire ZIP into a folder, then open StellarContinuum.exe.
Keep the executable, StellarContinuum.pck, data_Game_windows_x86_64, and
data folders together. The required .NET runtime is included; Godot and
the .NET SDK do not need to be installed.

This is a development demo for Windows PCs with a 64-bit Intel/AMD processor.
It is unsigned. BUILD.json identifies its exact version, source commit,
official Godot tools, file checksums, and CI run.

START PLAYING

Choose Continue to enter the campaign. Use the visible campaign controls
to explore systems, inspect colony sites, manage research/construction,
and view relations. Space pauses/resumes; 1-4 change simulation speed.
Use the mouse wheel to zoom and middle-drag to pan the galaxy map.
F6 saves the campaign. F8 exports a support bundle.

The main menu and on-screen control hints describe the current candidate's
actions. This is an early demo, so some long-term systems remain incomplete.

SAVES AND SUPPORT

Campaign saves and logs are stored in the Godot app_userdata/Game folder
under your Windows application-data directory. They are separate from the
download folder. Keep a copy of existing saves before testing a new build.
If something fails, include BUILD.json and an F8 support bundle in the report.

VALIDATION

The final Windows artifact is published after the actual exported executable
starts successfully in a hosted Windows CI job. The package also passes file
and runtime completeness checks and a Linux exported-game startup check.
BUILD.json records which checks have completed. Automated headless startup
does not certify interactive input or graphics on every Windows PC.

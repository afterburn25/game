STELLAR CONTINUUM - WINDOWS DEMO

Extract the entire ZIP into a folder, then open StellarContinuum.exe.
Keep the executable, StellarContinuum.pck, data_Game_windows_x86_64, and
data folders together. The required .NET runtime is included; Godot and
the .NET SDK do not need to be installed.

This is a development demo for Windows PCs with a 64-bit Intel/AMD processor.
It is unsigned. BUILD.json identifies its exact version, source commit,
official Godot tools, file checksums, and CI run.

START PLAYING

Choose Play Demo for a reproducible opening with a faster clock and next-step
guidance. Follow research and construction prompts, then use Next Ship and
Build / Queue Ship to build a scout, science vessel and colony ship.
Select a star, Send Scout to reconnoitre, then Send Science to survey.
Use the icon navigation rail for Research, Industry, Ships and Explore.
One detail drawer opens at a time; Map or its close icon clears the view.
Use Colonies to select a supported settlement and Settle Here.
Open System shows known orbits; click a planet for its known classification.
Back to Region returns to the star map. Inspect opens selected-star details.

The demo uses the normal resource, research and ship rules. Its 24x clock
shortens waiting. Normal campaigns retain their original 1-4x speeds.
Space pauses/resumes; 1-4 choose ordinary speed, and the speed selector offers
24x demo acceleration. Esc opens campaign options; Menu opens save and support.
Use the mouse wheel to zoom and middle-drag to pan the galaxy map.
F6 saves the campaign. F8 exports a support bundle.

The main menu and on-screen control hints describe the current candidate's
actions. This is an early demo, so some long-term systems remain incomplete.

SAVES AND SUPPORT

Campaign saves and logs are stored in the Godot app_userdata/Game folder
under your Windows application-data directory. They are separate from the
download folder. Demo and normal campaigns use separate save slots.
Choose Continue Demo to resume the demo; Continue resumes the loaded campaign.
Starting a replacement campaign requires confirmation.
If something fails, include BUILD.json and an F8 support bundle in the report.

VALIDATION

The final Windows artifact is published after the actual exported executable
starts successfully in a hosted Windows CI job. The package also passes file
and runtime completeness checks and a Linux exported-game startup check.
BUILD.json records which checks have completed. Automated headless startup
does not certify interactive input or graphics on every Windows PC.

# Earth-orbit loading splash

The user supplied `897309b4-1156-407c-9155-d6f96cb02a39.png` on 2026-09-11 and requested that it replace the loading artwork while retaining the existing main-menu screen. No creator or license claim is inferred from the supplied file.

The supplied raster included a fixed 61% progress bar. OpenAI's built-in image editing tool removed that baked loading display, retaining the scene, title, and corner inscriptions. The game supplies the actual progress bar, percentage, and loading status.

- Runtime asset: `assets/visual/loading/stellar-loading-splash.png`
- SHA-256: `38344B1243DF70006FC317F0E222A8E9B5F640FB145663CC2F01CD2B74BB9ECC`
- Method: built-in image editing; no fallback API or CLI.
- Existing main-menu image: `assets/visual/loading/stellar-continuum-splash.png`.

Exact edit prompt:

> Edit target: the supplied Stellar Continuum loading-screen artwork. Precise local inpainting only. Remove ONLY the lower center loading UI: words LOADING GAME..., the cyan progress bar and frame, the 61% number, and the small line Preparing menus, audio, and interface. Fill just those marks with seamlessly matching dark blue space/nebula background. Preserve the entire rest of the image as exactly as possible: same scene, crop, aspect ratio, station, Earth, ships, asteroids, all corner slogans and insignia and especially the STELLAR CONTINUUM title and logo. Do not redesign, add objects, move anything, or add any new text/UI. Output a clean game background with room for a real runtime loading bar in its original lower-center position.

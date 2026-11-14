# HOUSE OF 77

A cursed casino arena shooter. Fight to rig your fate.

## Play in the existing Unity project

Open `Assets/HouseOf77/Scenes/HouseOf77.unity` and press Play. The scene is also the first enabled build scene. The runtime builds its arena, object pools, and interface from the serialized GameConfig asset.

| Control | Action |
| --- | --- |
| WASD / arrows | Move |
| Mouse | Aim |
| Hold left click | Cast blades |
| Space | Dash |
| Esc | Pause / resume |

The game letterboxes to 16:9. A close camera smoothly follows the player and opens up for the boss. Enemies telegraph their attacks, and each seven kills unleashes a blade burst. Pause offers sound and screen-shake toggles. Losing focus pauses combat. Settings and best score persist; run upgrades reset on retry.

## Tune or rebuild

`Assets/HouseOf77/Configuration/HouseOf77.asset` holds difficulty, camera zoom and follow rate, imported sprites, materials, fonts, and licensed audio clips. Runtime rules are in `RunRules.cs`; the UI and gameplay components live under `Assets/HouseOf77/Runtime`.

The **House of 77** editor menu provides:

- **Prepare Game**: recreate the game scene and art references, preserving an existing configuration's tuning and audio. Save your current scene first. This command intentionally rebuilds only the generated HouseOf77 scene.
- **Assign Licensed Fonts and Audio**: wire the bundled typography and music, and fill empty sound effects slots.
- **Build WebGL**: export to `Builds/WebGL`.
- **Build Windows**: export to `Builds/Windows/HouseOf77.exe` when Windows build support is available.

The existing project Input System actions are reused. No engine version change or third-party gameplay package is required.

## Checks

Run the Unity Test Runner's EditMode assembly `HouseOf77.Tests`. It covers the probability distribution, chip cap, spending, upgrade limits, unique offers, protected spin, repeat-click lock, changing targets, and clean new-run state. Playtest the release build as well as the editor.

See `JAM.md`, `CREDITS.md`, `SUBMISSION.md`, and `VALIDATION.md` for submission details and remaining external requirements.


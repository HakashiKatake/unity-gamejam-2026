# Validation — House of 77

## Current delivery scope

The user subsequently requested **no builds and no uploads**. Delivery is the playable Unity Editor project. No further exports or publishing actions are being performed. The earlier WebGL attempt did not produce a playable build; it is not counted as validation or delivered as a release.

## Passed

- Unity C# compilation: no current compiler errors.
- Nine EditMode tests in `HouseOf77.Tests`: 9 passed, 0 failed, 0 skipped.
- Rule probability check: 20,000 samples for each of 15%, 30%, 50%, 75%; observed match rates within 1.5 percentage points of the displayed rate.
- Other rule tests: chip cap/carry, single purchase, upgrade caps, insufficient funds, odds cap, two-miss protection, duplicate-spin rejection, valid/non-repeating targets, unique offers, heal fallback, fresh run state.
- Real Play-mode smoke verification: 13 checks passed. Raw report: `Documentation/editor-smoke.json`.
- Smoke checks cover WASD, mouse aim/casting, dash cooldown/protection, damage invulnerability, blade collision/enemy death, shop UI double-click guard, combat-input isolation, rig purchase, reel/continue lock, next-wave transition, pause/resume, all four waves, boss fan/beam/summons, victory, defeat, and clean retry.
- Serialized scene/configuration and all required animation/sprite references validated through the editor.
- All three handwritten shaders compile without errors. Custom sprite rendering now uses Unity 6's sprite color/flip data, including correct projectile tints, fading, shadows, and facing.
- Project GUID/reference checker passed with zero errors and zero warnings.
- CPU-only stress simulation at the configured caps of 60 enemies and 250 projectiles: approximately 4.23 ms per simulated gameplay tick across 30 steps on the development machine. This excludes rendering and does **not** establish a measured 60 FPS frame rate.
- Actual rendered title, combat, shop, and boss views inspected through Unity rendering APIs. Previews are in `Documentation/Previews`.

## External or intentionally unperformed checks

- Human audio clips are not supplied. Optional audio slots are wired and safely silent.
- A complete 6–8 minute human playtest and difficulty pass remain recommended. The smoke check accelerates encounter transitions; it is not a full natural-paced playthrough.
- Browser/export checks and itch-hosted checks were stopped at the user's request. No browser compatibility or hosted-play claim is made.
- Human Warriors FREE permits non-commercial use only. Credits include the primary author/source links; review before monetization.

## Re-run inside Unity

1. Open `Assets/HouseOf77/Scenes/HouseOf77.unity`.
2. Run **House of 77 → Verify Scene References**.
3. Run EditMode tests in assembly `HouseOf77.Tests`.
4. Enter Play mode and play normally. The editor-only `HouseGameChecks.Smoke()` can also be invoked through MCP; it creates/removes temporary virtual input devices and restores the saved best score/settings.


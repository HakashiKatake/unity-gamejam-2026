# HOUSE OF 77

Official Unity Game Jam 2026 — Lucky Number

- Deadline: November 14, 2026, 12:00 UTC / 17:30 IST.
- Unity: 6000.4.10f1, URP 2D, Input System 1.20.1.
- Scene: `Assets/HouseOf77/Scenes/HouseOf77.unity`.
- Configuration: `Assets/HouseOf77/Configuration/HouseOf77.asset`.
- Browser export path if the team later chooses to build: `Builds/WebGL/` (no playable build delivered).
- Submission: https://itch.io/jam/official-unity-game-jam-2026

You fight enchanted warriors to collect chips and rig a lucky-number spin, while surviving four waves and a possessed slot-machine boss.

## Locked scope

One arena, one character, aimed blade combat, dash, three enemy behaviors, four 75-second waves, one boss, six permanent upgrades, four spins. No multiplayer, separate minigames, procedural maps, meta progression, or generated media.

Player starts with seven health and seven chips. Each wave generates up to 21 chips. One upgrade may be purchased per break for seven chips. Each seven-chip rig purchase raises odds from 15% to 30%, 50%, then 75%. Two missed spins guarantee the next match. A match heals two and grants 20 seconds of double damage plus periodic seven-blade bursts in the next encounter; a miss heals one.

The changing target remains in 7–77, hidden until the final 30 seconds. Rolls select a match at exactly the displayed probability, then choose a uniform non-target value on a miss. Visual reel animation cannot affect the already-decided result.

## Build and release gates

The user later requested no builds and no uploads. The current handoff is the Unity Editor game only; build/submission instructions below are for the team's later use.

- Run EditMode tests in assembly `HouseOf77.Tests`.
- Check start, movement, aim/fire, dash, pause, shop, spin, boss, death, victory, retry.
- Test a clean extracted WebGL build and the itch-hosted version.
- Licensed music and sound effects are assigned in GameConfig. Verify the mix through a full human playtest.
- Publish for free. The imported Human Warriors FREE edition permits non-commercial use only. Review the author's terms before paid distribution or enabling donations.
- Include `CREDITS.md` and AI-assistance disclosure in the game page.
- WebGL should be marked playable in browser. A game-page upload must also be entered separately into the jam.
- No game-file changes after the deadline or during judging. Page descriptions and screenshots may be updated under the jam's rules.

## Responsibilities and cuts

Implementation: gameplay, scene, UI, shaders, effects, automated checks, browser export.
Saurabh and Prince: final playtest, account/page details, final submission verification.

Cut order: music, decorative effects, elite enemy, additional cards. Retain combat, odds choices, jackpot, boss, and a complete ending.

## Validation log

- Installed the requested gamedev router, Unity bundle, jam, roguelike, game-feel, shader, UI, performance, and itch publishing skills.
- Confirmed AnkleBreaker MCP and the existing open editor connection.
- Nine rule tests passed, including 80,000 probability samples across all odds tiers.
- Unity project checker: 687 files checked, zero errors, zero warnings at the first completed source pass.
- Remaining build/runtime verification is recorded in `VALIDATION.md` as it completes.


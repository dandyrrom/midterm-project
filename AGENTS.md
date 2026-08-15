# midterm-project (InfoTech E3)

Unity **6000.4.11f1**, URP, Windows. Keyboard only (no mouse look required for gameplay). Product: third-person stealth action-adventure at a Filipino **kasal** that turns; aswang hunt by **sound**.

## Git and how to work with Danni

- Default branch for gameplay: **`danni`**. Do not create extra branches unless Danni asks.
- Inspector-first: give Unity Inspector field names and values when that is enough. Implement scripts on `danni` and commit/push when she asks.
- Danni Play-tests in the Editor. This Cloud environment has no Unity Editor.
- Do not merge to `main` unless she asks. Sherall owns splash, menu, and backstory (do not overwrite that work).

## Player (`mc-Peasant Girl`)

`Assets/Scripts/ThirdPersonController.cs` + `PeasantGirl_Controller`.

| Input | Behavior |
|---|---|
| WASD | Move (camera-relative; S turns her toward the camera — leave unless asked) |
| Shift | Run |
| Space | Jump: crouch pause, then hop, WASD in air, land pause **only after Space** (not walking off ledges) |
| H | Hit |
| K | Death |

Tune on the Third Person Controller component: Jump Height, Jump Takeoff Delay, Pause During Crouch, Land Pause, Pause On Landing, Hit Pause, Jump Buffer Time, Air Control.

**Pinned:** death clip sinking into stairs/cubes — leave it. Die on flat ground for demos.

## Scene

`Assets/Scenes/SampleScene.unity`: Terrain from Cemetery Kit `Display_Ter.asset` (shared with `Full_Display`). URP material convert done. Do not put the old grey **Plane** back under the church floor (z-fighting).

Level 1 layout (kasal stealth):

| Folder | Contents | Notes |
|---|---|---|
| **Town Barrio** | Town Kit houses, cellar, lanterns, storage | Around spawn `(7.63, 0, 34.32)`. Path toward the church stays open. |
| **Kasal Church** | Church 1 Open `(56.48, 1, 50.91)`, pews, stage, podium, cross, cross arch | Same church pose Danni play-tested. Nave along Z. |
| **Cemetery** | Graves, tomb, coffins, gates/fences, stone walls | East of the church (aswang grounds). |

Pieces start as SampleScene roots. If pews sit outside the nave or props float, nudge in the Inspector, or run **Tools > Midterm > Rebuild Kasal Environment** (groups them under Town Barrio / Kasal Church / Cemetery).

Kits: `Assets/Cemetery Kit V1.25/`, `Assets/Town Creator Kit LITE/`. Prefabs may lack colliders; add them for bump-to-noise.

## Rubric (must ship)

One-level Unity 3D game, splash + backstory (Sherall), Filipino culture (kasal, asin/bawang/holy water vs aswang, rain). Genre: action-adventure stealth.

| | Feature |
|---|---|
| A | Score on screen during play |
| B | Limited lives (not infinite) |
| C | Player weakness (stamina sprint and/or limited lunas) |
| D | Level challenge: **sound stealth** (Level 1) |
| E | 2–3 respawn points |
| F | Character feature: throw **lunas** (asin, bawang, holy water) |
| G | Environment: morning **rain** that masks noise and randomly lightens |
| H | Pause, Exit, Restart |

Veil/decoy: flagged, not built. Flag church bells for later levels.

## Next (gameplay)

Bumpable props → noise → stealth meter → aswang hear/chase/hit → lives + lunas HUD → pause hooks for Sherall.

## Do not

- Install Unity or treat Play Mode as available here.
- Rewrite movement to strafe/backpedal unless asked.
- Force-merge teammate branches (`sherall`) without Danni saying so.

# midterm-project (InfoTech E3)

Unity **6000.4.11f1**, URP, Windows. Product: third-person stealth action-adventure at a Filipino **kasal** that turns; aswang hunt by **sound**.

Keyboard WASD / Shift / Space / H / K drive the player. Cinemachine **FreeLook Camera** also reads the Input System **Look** action (mouse orbit). Do not require mouse look for core movement.

## Git and how to work with Danni

- Current gameplay/environment branch: **`hibalag`**. Do not create extra branches unless Danni asks.
- Older gameplay history lives on **`danni`**. **`sherall`** owns splash, menu, and backstory (do not overwrite that work). Do not merge to **`main`** unless she asks.
- Inspector-first: give Unity Inspector field names and values when that is enough. Implement scripts on `hibalag` and commit/push when she asks.
- Danni Play-tests in the Editor. This Cloud environment has no Unity Editor.

## Player (`mc-Peasant Girl`)

`Assets/Scripts/ThirdPersonController.cs` + `PeasantGirl_Controller` (`Assets/assets-people/anims/PeasantGirl_Controller.controller`). Scene YAML may still label the component `NewMonoBehaviourScript`; the script GUID is `ThirdPersonController`.

| Input | Behavior |
|---|---|
| WASD | Move (camera-relative; S turns her toward the camera — leave unless asked) |
| Shift | Run |
| Space | Jump: crouch pause, then hop, WASD in air, land pause **only after Space** (not walking off ledges) |
| H | Hit |
| K | Death |

Tune on the Third Person Controller component. Current play-scene values (Map / Preview 1):

| Field | Value |
|---|---|
| Use Root Motion | Off (player). On only if wiring Mixamo root motion on an aswang/Warzombie |
| Walk Speed | 2.5 |
| Run Speed | 5 |
| Jump Height | 0.4 |
| Jump Takeoff Delay | 0.8 |
| Pause During Crouch | On |
| Land Pause | 1.2 |
| Pause On Landing | On |
| Hit Pause | 0.7 |
| Jump Buffer Time | 0.2 |
| Air Control | 1 |

**Pinned:** death clip sinking into stairs/cubes — leave it. Die on flat ground for demos.

## Scenes

`Assets/Scenes/SampleScene.unity` is gone. Play these:

| Scene | Role |
|---|---|
| **Preview 1** | Village/kasal playtest: `hauses` / `hauses-2`, Church 2 Open, `objects`, `bodies` / `hanging bodies`, NavMesh, tin cans, aswang placeholders. Player spawn about `(-14.68, 0, 266.07)`. |
| **Map** | Later “final map”: industrial roads/fences (`RPG_FPS_game_assets_industrial`), `playground` (`PlaygroundApocalypse`), Church 2 Open. Player spawn about `(73.9, 2.09, 4.46)`. |
| **with env1** | Earlier village + one aswang placeholder. Player spawn about `(17.7, 0, 60.1)`. |
| **Backstory** | Sherall. Currently the **only** scene in File > Build Settings. |

Camera on play scenes: **FreeLook Camera** (Cinemachine 3 Orbital Follow) + Cinemachine Brain on Main Camera. NavMesh Surface object: **NavMesh**.

Pink imported trees/foliage: **Tools > Rendering > Fix Pink Materials (URP)** (`Assets/Editor/FixPinkImportedMaterials.cs`). There is no **Tools > Midterm > Rebuild Kasal Environment** menu anymore.

## Aswang and noise (missing scripts)

Scenes still reference gameplay scripts that are **not in the repo** (last `hibalag` commit removed zombie-related assets). Unity will show Missing Script until they are restored:

- **AswangMotor** — on `Zombieguy-topless` instances (Preview 1 has several; `with env1` has one). YAML values: Walk Speed `1.4`, Run Speed `3.5`, Enable Go To Player Hotkey **On**. Each also has NavMeshAgent (Speed `1.4`, Stopping Distance `1.6`) and a capsule collider.
- **KnockableTinCans** — Preview 1 tin can: Player Push Force `4`, Wake Radius `0.9`, Shove Force `8`, Fall Height For Noise `0.2`, Min Impact Speed `1.4`, Hear Radius `18`.

Do not assume `Warzombie` assets are present; they were deleted after being placed.

## Asset kits in use

URP project (HDRP package is in `Packages/manifest.json` but Graphics Settings use URP). Kits under `Assets/`:

- Cemetery Kit V1.25, Town Creator Kit LITE
- MyDreamGameStudio Medieval Village Pack Vol.1 (`hauses`)
- PlaygroundApocalypse, RPG_FPS_game_assets_industrial, DeadBody LITE
- MarpaStudio, Realistic Tree, TerrainSampleAssets, SwampHouse, FREE_CartoonPack_Buildings, Gwangju_3D asset, MASH Virtual

Prefabs may lack colliders; add them for bump-to-noise.

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

Restore or rewrite **AswangMotor** / bump-to-noise (`KnockableTinCans`) → stealth meter → aswang hear/chase/hit → lives + lunas HUD → pause hooks for Sherall. Add play scenes to Build Settings when shipping (today only Backstory is listed).

## Do not

- Install Unity or treat Play Mode as available here.
- Rewrite movement to strafe/backpedal unless asked.
- Force-merge teammate branches (`sherall`) without Danni saying so.
- Convert the project to HDRP.

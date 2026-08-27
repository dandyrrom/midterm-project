# Bawang (garlic) collect — dedicated chat handoff

Paste this into a **new Agent chat** in the midterm-project workspace.

## Working rules
- Branch: `hibalag`. Do not merge to `main` unless asked.
- Do **not** edit the project unless Danni says **you do this** or **go**.
- When giving instructions: be detailed (panel, component, fields, file/line).
- After each finished step, say **what’s next**.
- Keep systems simple (midterm / Filipino kasal stealth game).

## Product context
Unity 6000 URP. Level1 scene. MC: `mc-Peasant Girl` + `ThirdPersonController`. Zombies: blind, sound-sensitive (`NoiseEvents` + `ZombieRoam`). Health bar already on Canvas.

## This chat’s scope ONLY: bawang collect (not throw yet)
Asin / holy water / throw / score / kill-all = later.

### Locked decisions
1. Collect = face garlic + key **E**
2. World mesh = `Assets/ITEMS/Mesh_FBX/garlic_01.fbx`
3. HUD icon = **bottom-right** of screen
4. Icon art = `Assets/ITEMS/garlic-icon.png`
5. Pick-up anim = `Assets/assets-people/c-anims/mc-Pick Up.fbx` → wire into `PeasantGirl_Controller`
6. Count: 0 = faded icon; >0 = full color + number (bottom-right of icon)

### Build order
1. Configure `mc-Pick Up` (Humanoid if needed) + Animator state/trigger
2. Canvas UI: garlic icon bottom-right (faded / color + count)
3. Place `garlic_01` in Hierarchy near MC (trigger collider)
4. Face + E → play pick-up anim → +1 bawang → HUD updates

### Start with
Step 1: configure pick-up animation. Wait for Danni to say **you do this** / **go** before editing.

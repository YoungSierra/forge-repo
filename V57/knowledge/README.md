# V57 knowledge (lessons)

Short, concrete lessons from real runs. One topic per file. Read all of them once per session before stage I1 (they are small). Written and condensed by `/context-postmortem`.

| File | Topic |
|---|---|
| `asset-intake.md` | Provider art problems and how intake/import handle them |
| `camera.md` | Camera is decided once, from TDD §11.5 + ADD camera refs |
| `verification.md` | Self-grading, faked physics, screenshots, evidence rules |
| `runtime-bootstrap.md` | Why runtime scene builders are banned; what a bootstrap may do |
| `unity-cli.md` | CLI/Editor races, input simulation, device restore |
| `assets-collision-builds.md` | No placeholders (MISSING_ASSETS.md), collision lives with its mesh, 3D physics for 3D scenes, no player builds by default |
| `process.md` | Autonomy, decide + log, timeboxes, state on disk |
| `rigged-characters.md` | ANIM_ hierarchy, loop sidecar, Blender rig −90° X, provider FBX export settings, replacing static stand-ins |

Lesson format: **Symptom → Cause → Rule → Enforced by → Source**.
Sources: `HH` = Happy Habitat pinball run (2026-09, first V57 vertical slice); `PS` = ProfesorSprat delivery (2026-10); `WoO` = World of Oldcraft process learnings (AI-built game reference process).

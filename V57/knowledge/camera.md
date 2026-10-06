# Camera lessons

## Lock the camera once
- **Symptom:** camera re-framed in three stages (asset pipeline: landscape top-down, ortho size 5.78, euler 90°; camera stage: portrait 9:16, size 8.21, euler 83.5°; later crafts adjusted again). Screens, UI and tuning were validated against different framings.
- **Cause:** camera decided per stage from screenshots instead of from the design data.
- **Rule:** camera values are decided **once**, at intake, from TDD §11.5 (`In-play camera` row) + ADD `Docs/ArtDirection/Camera/<ViewId>.png` + blockout `Marker_Camera_<ViewId>` → `Docs/Generated/json/camera.json`. I2 builds scenes with those values; M1 locks them. Later stages may only polish motion (damp, shake) — changing type/fov-or-size/angle/distance needs a `D-###` and a gold path re-run.
- **Enforced by:** Edit vs Play diff (camera must match `camera.json` at frame 1), `/independent-review` numbers vs `camera.json`. **Source:** HH camera + asset-pipeline reports.

## Orientation and resolution come first
- **Rule:** `package.platform.orientation` + `reference_resolution` set the Game View at I1. Framing is computed for that aspect; screenshots for review use it.
- **Source:** HH (portrait 9:16 discovered late).

## Tilt vs gravity
- **Rule:** when the TDD says the tilt is visual ("simulates incline without artificial gravity"), the tilt is on the camera only; gameplay gravity/plane follows the TDD physics table. Do not bake camera tilt into level geometry.
- **Source:** HH TDD §11.5.

## 2D vs 3D physics is a camera-independent decision
- **Rule:** an orthographic 3D camera does not imply 2D physics. `package.physics` comes from the TDD (§11.5 "Space"), but when the delivered scene is 3D (Y-up, play surface in XZ) V57 uses 3D physics constrained to the play plane and logs the TDD mismatch as a `conflict` (`assets-collision-builds.md`) — never rotate the level to force Physics2D.
- **Source:** HH (TDD said Rigidbody2D plane; implementation used 3D).

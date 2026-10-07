# I1 — Project baseline · ProfessorWortSprat

2026-10-07T23:48:11Z · Unity 6000.6.2f1 (warm Editor) · gate = script check + console.

| # | Item | Expected | Actual | Result |
|---|---|---|---|---|
| 1 | ProjectVersion | 6000.6.2f1 | 6000.6.2f1 | pass |
| 3 | Packages | com.v57.assembly, URP, Input System, Test Framework, testables | com.v57.assembly (file:../V57/tools/unity-assembly), URP 17.6.0, Input System 1.20.0, Test Framework 1.8.0, Cinemachine 3.1.4 (TDD §11.5 camera), testables [com.v57.assembly] | pass |
| 4 | Render pipeline | URP assigned | defaultRenderPipeline set | pass |
| 5 | Color space | Linear | Linear | pass |
| 6 | Serialization | Force Text | Force Text | pass |
| 7 | Version control | Visible Meta Files | Visible Meta Files | pass |
| 8 | Input handling | Input System (New) | activeInputHandler: 1 | pass |
| 9 | Tags / layers | V57 base set + per game | tags +Environment, Interactable, Hazard, Trigger, Projectile, UIWorld, Collectible (Enemy existed); layers Player=8, Environment=9, Interactable=10, Hazard=11, Trigger=12, Projectile=13, UIWorld=14, Enemy=15, Collectible=16 | pass |
| 10 | Platform | landscape 1920×1080, 60 fps | from package.json | pass (Game View set before screenshots) |
| 11 | Physics | 3D | package.physics = 3d | pass |
| — | Compile / console | 0 errors | compileFailed=False, console errors=1 | pass |

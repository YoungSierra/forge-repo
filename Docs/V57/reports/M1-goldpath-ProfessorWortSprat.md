# Gold path — ProfessorWortSprat

- **Run:** Docs/V57/evidence/goldpath/20261008T002433Z/checks.json
- **Result:** all pass (22/22), 0 console errors, ~257 fps average
- **Steps:** 22 (Docs/V57/gold_path.json, seeded from: acceptance.json draft Move/Jump/Stomp + TDD §3 core loop)
- **Loop covered:** zone starts (HUD 0 / 6) → run collects 2 flies → running jump collects the elevated fly → stomp defeats the static crab and collects the fly above it → run collects the last 2 flies (zone complete, key ceremony + key shot) → key carried (HUD key glyph) → door opens → zone exit → level-end panel (HUD 6 / 6)
- **Screenshots:** 00_S01 (start, Crash-style follow camera), 10_S11 (crab defeated, 4 / 6), 14_S15 (zone complete), 15_S16 (key carried, KEY glyph), 19_S20 (level end), 21_end_state, 22_final
- **Probe review:** every checkpoint image matches its check. Run 20261008T002239Z passed its checks but showed a camera occluded by the start bulkhead and a key shot inside a glass wall; fixed by mirroring the test route (camera on the open side of the tunnel, D-016) and a key shot that keeps the follow direction — evidence of that run is superseded.
- **Status word:** agent-verified

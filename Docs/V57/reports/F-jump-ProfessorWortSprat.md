# F — jump · ProfessorWortSprat

- **Spec:** V57/specs/ProfessorWortSprat/features/jump.yaml (adopted from TDD §C)
- **EditMode:** ConfigTests.ACJMP01, ModelTests.ACJMP02 — pass (ProfessorSprat.Tests.EditMode 13/13)
- **PlayMode:** JumpPlayTests (AC-JMP-03…05) — pass (ProfessorSprat.Tests.PlayMode 28/28), scene SCN_Jump_Test
- **Gold path after the spec:** Docs/V57/evidence/goldpath/20261008T003358Z/checks.json — pass
- **Test style:** component PlayMode tests through the mechanic's public API with real FixedUpdate physics (D-019); the input-driven acceptance is the gold path.
- **Status word:** agent-verified

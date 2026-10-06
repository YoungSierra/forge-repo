# Sync TDD (Unity-clean)

Update the canonical TDD so Unity can build from it. The operator pressed **Sync** because they are satisfied with the current playable. Gameplay code under the mode roots is **evidence** of validated feel; the TDD is a **product** document.

## Required workflow

1. **Read every gameplay evidence file** listed below (C# under `Assets/Prototypes`, `Assets/VerticalSlice`, or `Assets/Scripts`, plus scenes/UXML when relevant).
2. Compare against the current TDD — numbers, bindings, HUD features, win/lose, camera.
3. Write product rules into the TDD so another Unity implementer can match the same behavior.

## Allowed write

- Only the **canonical TDD markdown** in `Docs/tdds/<slug>/` (filename is not required to be `TDD.md` — edit the resolved path shown in the prompt)

## What to update

- **§B mechanic blocks** — quantified rules, states, ACs when feel/numbers/behavior changed
- **§C companion specs** — stay 1:1 with §B; add new YAML blocks when you add mechanics
- **§9.1 UI registry** — when the playable gained HUD/overlays: register `UI_*` ids and wire them to the owning mechanic
- **§11.3 input map** — bindings, axis inversion, dead zones, hold vs tap
- **§11.5 camera / control** — follow offset, FOV, invert-Y, chase rig when changed
- **§4 / §3** — only when core loop or win/lose changed materially

## Operator-approved checklist

If a checklist is attached, **only** apply those items. Do not add extra mechanics, HUD, or input rows the operator left unchecked.

## New features from chat

If the validated digest or gameplay code shows a feature **not yet in the TDD**:

- Add a **§B mechanic** (or extend an existing HUD/movement mechanic) with quantified rules and ≥1 PlayMode AC
- Add matching **§C** spec with the same id
- Register UI in **§9.1** when it is on-screen chrome
- Update **§11.3 / §11.5** when input or camera changed
- Use Unity vocabulary: UI Toolkit, MonoBehaviour, NavMesh, ScriptableObject, URP, `Assets/...`

Do **not** skip a feature because it was “added in chat” — Sync means the operator approved it for the product spec.

## Forbidden in the TDD

- Web/lab stack jargon (Three.js, WebGL, canvas DOM sandboxes, `public/gameplay`, hot-reload-as-product)
- Mentions that something was “validated in browser” or similar non-Unity delivery
- Replacing Unity terms with web jargon
- Inventing non-product pipeline sections

## Versioning

Bump the TDD version note when mechanics or numbers change.

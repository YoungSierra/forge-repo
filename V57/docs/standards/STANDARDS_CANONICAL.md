# Canonical Unity C# Standards (Strict)

This file is the **single source of truth** for production C# in this repository. Other docs (`NAMING_CONVENTIONS.md`, `STYLE_QUICKREF.md`, skills) must not contradict it.

**Policy mode:** strict — rules apply unless this document explicitly marks them as optional.

---

## 1. Precedence

1. `V57/docs/standards/STANDARDS_CANONICAL.md` (this file)
2. `V57/docs/standards/NAMING_CONVENTIONS.md` — naming detail and examples (derived)
3. `V57/docs/standards/CODING_STANDARDS.md` — patterns and extended examples (must align with this file)
4. `V57/docs/standards/STYLE_QUICKREF.md` — cheat sheet (derived)
5. Agent skills — operational workflows; if a skill conflicts with this file, **this file wins**

---

## 2. Global rules (strict)

These rules apply to **production code** under `Assets/_Game/Scripts/` (and the same conventions for any runtime/editor code you treat as product). Test assemblies, sample sandboxes, and ad-hoc scratch files are **not** covered by this phase; you may still apply the same rules there for consistency.

- **No `var`** in production C#. Always use an explicit type on the left-hand side.
- **One top-level type per file** (one of: class, struct, enum, interface). Nested types are allowed only when tightly scoped and still readable within the 200-line cap.
- **File name must match the primary type name** exactly (e.g. `PlayerController.cs` ↔ `class PlayerController`).
- **Hard cap: 200 lines per file.** Exceeding requires splitting types or extracting helpers into new files; do not waive without team agreement.
- **No `GetComponent` / `GetComponentInChildren` / similar lookups in `Update`, `FixedUpdate`, or `LateUpdate`.** Cache auto-references in `Awake`; cross-object references in `Start` (after peers initialized); subscriptions in `OnEnable`/`OnDisable` (or inject via serialized references).

---

## 3. Naming (summary)

| Element | Convention | Example |
|---------|------------|---------|
| Class, struct, enum | PascalCase | `PlayerController`, `WeaponType` |
| Enum value | PascalCase | `WeaponType.Sword` |
| Interface | IPascalCase | `IDamageable` |
| Method, property | PascalCase | `TakeDamage()`, `Health` |
| Event | OnPascalCase | `OnHealthChanged` |
| Coroutine | CoPascalCase | `CoFadeIn()` |
| Private field | _camelCase | `_health`, `_inputReader` |
| Public mutable field | — | *disallowed — use property or `[SerializeField] private`* |
| Constant / static readonly | PascalCase | `MaxHealth`, `DefaultSpeed` |

**Public fields:** disallowed for new code unless there is an exceptional, documented reason. Prefer properties with controlled mutability and `[SerializeField] private` for inspector data.

**C# version:** Resolved from **`engine`** in `CONTEXT.md` (or the consumer project's Unity Editor). **Omit `language`** in CONTEXT unless the TDD documents an explicit override (unusual, e.g. custom `csp.rsp` `-langversion`).

| Unity Editor (examples) | Official C# language version |
|-------------------------|------------------------------|
| 2020.3 LTS | C# 8 |
| 2021.3 LTS | C# 9 (partial) |
| 2022.3 LTS | C# 9 |
| Unity 6.0–6.7 (6000.x) | C# 9 |
| Unity 6.8+ (planned) | C# 14 (.NET 10 / CoreCLR) |

When `language` is omitted, agents and reviewers resolve the effective version from `engine` using this table. Do not use language features beyond the resolved version. Verify against the [Unity C# compiler reference](https://docs.unity3d.com/Manual/csharp-compiler.html) for the exact Editor in use.

Full tables and examples: `V57/docs/standards/NAMING_CONVENTIONS.md`.

---

## 4. Namespaces

- Format: `Company.Project.<Area>[.<SubArea>]` matching folder layout under `Assets/_Game/Scripts/`.
- Every production script in `Assets/_Game/Scripts/` **must** declare a namespace (unless the project explicitly adopts global usings for a single assembly — not the default here).

Folder mapping reference: `V57/docs/standards/CODING_STANDARDS.md` (Namespaces section).

---

## 5. Regions and structure

- For files **longer than 50 lines**, use `#region` blocks:
  - `Unity Lifecycle` — `Awake`, `OnEnable`, `Start`, `Update`, etc.
  - `Public Methods` — public API
  - `Private Methods` — implementation details
- Optional additional regions: `Event Handlers`, `Serialized Fields`, when they improve scanability.
- For files **50 lines or fewer**, regions are optional; keep a clear field → lifecycle → methods order.

---

## 6. Unity / API encapsulation

- Inspector-visible private state: `[SerializeField] private` with optional `[Header]`, `[Tooltip]`, `[Range]`.
- Serialized fields are **private**; expose read-only or carefully designed accessors via properties when other systems need data.
- Singletons: `public static TypeName Instance { get; private set; }` with duplicate-instance guard in `Awake` (see `V57/docs/standards/NAMING_CONVENTIONS.md`).

---

## 7. Async and coroutines

- Prefer `async Task` for asynchronous work. **Do not** use `async void` except where the runtime requires a `void` signature (e.g. some event callbacks); document those cases.
- Coroutines: `IEnumerator`, name with `Co` prefix, `yield` used correctly.
- Do not conflate `IEnumerator` coroutines with `Task` without a clear bridge (`StartCoroutine` vs `await`).

---

## 8. Error handling and diagnostics

- Recoverable issues: `Debug.LogWarning` with context (type name, key values).
- Broken invariants or missing required references: `Debug.LogError` and/or `Assert.IsNotNull` for developer builds.
- Null-check or pattern-match before dereferencing components and interface references from `GetComponent` / external callers.

---

## 9. ScriptableObjects

- Use `[CreateAssetMenu(menuName = "...")]` when designers create assets from the menu.
- Menu paths: hierarchical, stable (e.g. `Game/Data/Character Stats`).
- Prefer `[SerializeField] private` fields with public getters (or methods) instead of public mutable fields on the type.

---

## 10. Compliance levels (for review automation)

**Critical (blocks approval)**

- Wrong type/file naming; interface without `I`
- Public mutable fields without documented exception
- `var` usage
- `GetComponent` (or equivalent) in per-frame methods
- `async void` without justified Unity callback pattern
- Singleton without `Instance` pattern and duplicate guard
- File over 200 lines without split
- Missing namespace where required

**Warning (should fix before merge when practical)**

- Missing `[Header]` / `[Tooltip]` on grouped serialized fields
- Missing regions in files over 50 lines
- Overly broad catch or silent failure (if introduced)

---

## 11. Change control

Any change to this document should state:

- What changed and why
- Which derived docs or skills were updated to match
- Migration note for existing code (if any)

---

*Aligned with strict policy. Testing layout and test project paths are intentionally out of scope for this document until adopted separately.*

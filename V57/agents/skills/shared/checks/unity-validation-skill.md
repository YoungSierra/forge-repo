# Unity Validation Skill

Use this skill when reviewing generated Unity C# code against project standards.

| Item | Detail |
|------|--------|
| **Authoritative** | `V57/docs/standards/STANDARDS_CANONICAL.md` — read first; canonical wins over this skill |
| **Companion** | `V57/agents/skills/shared/references/unity-naming-skill.md`, `V57/docs/standards/CODING_STANDARDS.md` |

---

## Scope

Apply strict checks to **production code** under `Assets/_Game/Scripts/` (and runtime/editor code treated as product). Paths outside that tree: follow canonical §2 where applicable.

---

## Validation checklist (maps to canonical)

Use as a **quick pass**; details and examples live in **`STANDARDS_CANONICAL.md`** and **`V57/docs/standards/CODING_STANDARDS.md`**.

### Naming and files (§2–§3)

- [ ] Types, methods, properties, events, coroutines follow §3  
- [ ] Private fields: `_camelCase`  
- [ ] Primary type name matches filename; one top-level type per file (§2)  
- [ ] No undocumented **public mutable fields** — prefer properties + `[SerializeField] private` (§3, §10)  

### Structure (§4–§5)

- [ ] Namespace matches folder under `Assets/_Game/Scripts/`  
- [ ] Files **> 50 lines**: `#region` for lifecycle, public API, private implementation (§5)  
- [ ] Files **≤ 50 lines**: regions optional; clear field → lifecycle → methods order  
- [ ] File **≤ 200 lines** (§2)  
- [ ] **No `var`** (§2)  

### Unity patterns (§2, §6–§7)

- [ ] `[SerializeField] private` for inspector data; optional `[Header]`, `[Tooltip]`, `[Range]`  
- [ ] **No** `GetComponent` (or equivalent) in `Update` / `FixedUpdate` / `LateUpdate` (§2)  
- [ ] Singleton: `Instance { get; private set; }` + duplicate guard in `Awake` when applicable (§6)  
- [ ] Coroutines: `IEnumerator`, `Co` prefix; async: prefer `async Task`, not `async void` except justified Unity callbacks (§7)  

### ScriptableObjects (§9)

- [ ] `[CreateAssetMenu]` when designers create assets; stable menu paths  
- [ ] Prefer `[SerializeField] private` + accessors, not public mutable fields on the type  

### Error handling and performance (§8, §2)

- [ ] `Debug.LogWarning` / `Debug.LogError` / `Assert.IsNotNull` where appropriate  
- [ ] Auto-references cached in `Awake`; cross-object refs in `Start`; subscriptions in `OnEnable`/`OnDisable` — never per frame in `Update`/`FixedUpdate`/`LateUpdate`  

---

## Common issues to flag

```csharp
// ISSUE: Missing namespace (production under Assets/_Game/Scripts/)
public class PlayerController : MonoBehaviour { }

// ISSUE: Wrong private field naming
private int playerHealth;

// ISSUE: Public mutable field (strict: avoid unless documented)
public int health;

// ISSUE: Missing SerializeField for inspector-editable private field
private int health;

// ISSUE: Interface without I prefix
interface Damageable { }

// ISSUE: GetComponent in Update
private void Update() { var rb = GetComponent<Rigidbody>(); }

// ISSUE: var (strict: never in production code)
var speed = 10;

// ISSUE: async void (unless required Unity callback — document)
async void LoadData() { }
```

---

## Validation steps

| Step | Action |
|------|--------|
| 1 | Read `V57/docs/standards/STANDARDS_CANONICAL.md` |
| 2 | Read the target file(s) |
| 3 | Run checklist; use `V57/docs/standards/NAMING_CONVENTIONS.md` for naming examples |
| 4 | Flag issues with line numbers; mark **critical** vs **warning** per canonical §10 |
| 5 | If **critical** issues exist, block further generation until fixed |

---

## Output format for issues

```text
VALIDATION: Assets/_Game/Scripts/Features/Player/PlayerController.cs

ISSUES:
1. Line 15: ...

WARNING:
- Line 89: ...

RECOMMENDATION:
Fix critical issues before proceeding. See V57/docs/standards/STANDARDS_CANONICAL.md §10.
```

---

## Rules

1. **Critical vs warning** — only definitions from canonical §10 count as authoritative.  
2. Do not invent stricter rules than the canonical document.  
3. Re-validate after every non-trivial fix pass.  

---

## Apply this skill

1. After codegen or before merge, run this checklist on touched `Assets/_Game/Scripts/**` files.  
2. Fix **critical** issues before playtesting or handoff.  
3. Triage **warnings** by risk and schedule.  
4. Re-run validation after fixes until clean or explicitly waived with reason.

**Not in scope:** gameplay correctness or spec `acceptanceCriteria`. Those are verified by Unity Test Framework + implement report per **`V57/agents/skills/commands/spec/unity-spec-skill.md` § Definition of Done**.

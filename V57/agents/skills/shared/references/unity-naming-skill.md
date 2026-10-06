# Unity Naming Skill

Use this skill when generating C# code for Unity projects.

| Item | Detail |
|------|--------|
| **Authoritative** | `V57/docs/standards/STANDARDS_CANONICAL.md` — this file summarizes; canonical wins on conflict |
| **Elaboration** | `V57/docs/standards/NAMING_CONVENTIONS.md`, `V57/docs/standards/CODING_STANDARDS.md` |

---

## Types (classes, structs, enums, interfaces)

| Kind | Convention | Example |
|------|------------|---------|
| Class | PascalCase | `PlayerController`, `AudioManager` |
| Struct | PascalCase | `CharacterStats`, `Vector3Data` |
| Enum | PascalCase | `WeaponType`, `GameState` |
| Enum value | PascalCase | `WeaponType.Sword` |
| Interface | `IPascalCase` | `IDamageable`, `IInteractable` |

---

## Variables and fields

| Scope | Convention | Example |
|-------|------------|---------|
| Private field | `_camelCase` | `_health`, `_instance` |
| Public mutable field | — | *disallowed — use property or `[SerializeField] private`* |
| `[SerializeField]` private | `_camelCase` | `[SerializeField] private int _health` |
| Constant | PascalCase | `MaxHealth`, `DefaultSpeed` |
| Static readonly | PascalCase | `Instance` |

---

## Methods and properties

| Kind | Convention | Example |
|------|------------|---------|
| Method | PascalCase | `TakeDamage()`, `Initialize()` |
| Property | PascalCase | `Health`, `CurrentSpeed` |
| Event | `OnPascalCase` | `OnHealthChanged`, `OnDeath` |
| Coroutine | `CoPascalCase` | `CoFadeIn()`, `CoMoveTo()` |

---

## Files and folders

| Item | Convention | Example |
|------|------------|---------|
| Script file | `PascalCase.cs` | `PlayerController.cs` |
| Folder | PascalCase | `Controllers`, `ScriptableObjects` |
| Scene | PascalCase | `MainMenu`, `LevelOne` |
| Prefab | PascalCase | `Player_Prefab`, `Enemy_Base` |

---

## Code generation template

```csharp
using UnityEngine;

namespace Company.Project.Module
{
    public class ClassName : MonoBehaviour
    {
        #region Fields
        [SerializeField] private float _value;
        #endregion

        #region Unity Lifecycle
        private void Awake() { }
        #endregion

        #region Public Methods
        public void PublicMethod() { }
        #endregion

        #region Private Methods
        private void PrivateMethod() { }
        #endregion
    }
}
```

---

## Anti-patterns (never)

```csharp
// WRONG — Hungarian notation
private int m_iHealth;

// GOOD — private fields use _camelCase
private int _health;

// WRONG — cryptic abbreviations
private int hp;

// WRONG — generic names
private int temp;
private object data;

// WRONG — interface without I prefix
interface Damageable { }  // Use: interface IDamageable

// WRONG — public field with underscore
public int _Health;
```

---

## Common suffixes

| Suffix | Use | Example |
|--------|-----|---------|
| `Controller` | Main behavior | `PlayerController` |
| `Manager` | Singleton-style coordinator | `GameManager` |
| `System` | Non-Mono service | `DamageSystem` |
| `Handler` | Event handling | `InputHandler` |
| `Factory` | Creation | `EnemyFactory` |
| `Pool` | Pooling | `BulletPool` |
| `Data` | ScriptableObject data | `CharacterData` |
| `Config` | Configuration SO | `GameConfig` |
| `Event` | Event channel | `HealthEvent` |
| `State` | State enum | `PlayerState` |
| `Base` | Base class | `EnemyBase` |

---

## Rules

1. Filenames must match the primary type name (one public type per file when required by project rules).  
2. Interfaces always **`I` + PascalCase**.  
3. Events always **`On` + PascalCase**.  
4. On any ambiguity, **`V57/docs/standards/STANDARDS_CANONICAL.md`** decides.  

---

## Apply this skill

1. Apply these tables to **every** generated C# identifier and path.  
2. Cross-check against **`V57/agents/skills/shared/checks/unity-validation-skill.md`** before shipping edits.  
3. Ensure suffixes match the role of the type.  
4. Prefer explicit readable names over abbreviations.

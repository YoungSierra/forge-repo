# Unity Coding Style Quick Reference

**Policy:** Strict. Full rules: [`STANDARDS_CANONICAL.md`](STANDARDS_CANONICAL.md). Prefer properties over public fields for new code.

## Naming At a Glance

```
┌────────────────────────────────────────────────────────────────┐
│ TYPE                      │ CONVENTION    │ EXAMPLE            │
├────────────────────────────────────────────────────────────────┤
│ Class                     │ PascalCase    │ PlayerController   │
│ Struct                    │ PascalCase    │ CharacterStats     │
│ Enum                      │ PascalCase    │ WeaponType         │
│ Enum Value                │ PascalCase    │ WeaponType.Sword   │
│ Interface                 │ IPascalCase   │ IDamageable        │
│ Method                    │ PascalCase    │ TakeDamage()       │
│ Property                  │ PascalCase    │ Health             │
│ Public Field              │ —             │ *disallowed*       │
│ Private Field             │ _camelCase    │ _health            │
│ Constant                  │ PascalCase    │ MaxHealth          │
│ Event                     │ OnPascalCase  │ OnHealthChanged    │
│ Coroutine                 │ CoPascalCase  │ CoFadeIn()         │
│ File                      │ PascalCase.cs │ PlayerController.cs│
└────────────────────────────────────────────────────────────────┘
```

## Common Patterns

### Field Declaration
```csharp
// Public API (property — not a mutable field)
public int Health { get; private set; }

// Private serialized
[SerializeField] private int _health;

// Private not serialized
private int _maxHealth;

// Constant
public const int MaxHealth = 100;

// Static readonly
public static readonly Vector3 SpawnPoint = new Vector3(0, 1, 0);
```

### Property
```csharp
// Auto
public int Health { get; private set; }

// Readonly expression
public Vector3 Position => transform.position;

// With backing field
private int _health;
public int Health
{
    get => _health;
    set => _health = Mathf.Clamp(value, 0, MaxHealth);
}
```

### Event
```csharp
public event Action<int> OnHealthChanged;

private void RaiseHealthChanged(int newHealth)
{
    OnHealthChanged?.Invoke(newHealth);
}
```

### Singleton
```csharp
public class ManagerName : MonoBehaviour
{
    public static ManagerName Instance { get; private set; }

    [SerializeField] private AudioSource _musicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
```

### async/await
```csharp
public async Task LoadDataAsync()
{
    SomeDataType data = await _service.FetchDataAsync();
    ApplyData(data);
}

public async Task WaitForSecondsAsync(float seconds)
{
    await Task.Delay((int)(seconds * 1000));
}
```

## Anti-Patterns

```
BAD                     │ GOOD
────────────────────────┼──────────────────────
m_iHealth               │ _health
hp                      │ _health
speed                   │ _speed (field) or Speed (property)
temp / temp2            │ descriptive name
playerObject            │ _player
audioSource1            │ _JumpSound / _BgMusic
Damageable (interface)  │ IDamageable
```

## Suffix Cheat Sheet

| Suffix       | Use For           | Example            |
|--------------|-------------------|--------------------|
| `Controller` | Main behavior     | `PlayerController` |
| `Manager`    | Singleton manager | `GameManager`      |
| `System`     | Non-Mono service  | `DamageSystem`     |
| `Handler`    | Event handler     | `InputHandler`     |
| `Factory`    | Creation          | `EnemyFactory`     |
| `Pool`       | Object pool       | `BulletPool`       |
| `Data`       | Data container    | `CharacterData`    |
| `Config`     | Configuration SO  | `GameConfig`       |
| `Event`      | Event channel SO  | `HealthEvent`      |
| `State`      | State enum        | `PlayerState`      |
| `Base`       | Base class        | `EnemyBase`        |
| `SO`         | ScriptableObject  | `PlayerStatsSO`    |

## Namespace Format

```
Company.Project.Module.SubModule
```

Example:
```csharp
namespace Studio.Game.Features.Combat
{
    public class DamageCalculator { }
}
```

## Unity Lifecycle Order

1. `Awake()` - Always, before anything else
2. `OnEnable()` - When object becomes active
3. `Start()` - Before first frame update
4. `FixedUpdate()` - Physics updates (50/sec default)
5. `Update()` - Every frame
6. `LateUpdate()` - After all Updates
7. `OnDisable()` - When object becomes inactive
8. `OnDestroy()` - When object is destroyed

## File Locations

```
Scripts/
├── Core/Managers/      → Core.Managers
├── Systems/Audio/      → Systems.Audio
├── Features/Player/    → Features.Player
└── ScriptableObjects/  → ScriptableObjects

Art/2D/Sprites/
Art/3D/Models/
Audio/SFX/
Audio/Music/
Prefabs/Characters/
Prefabs/Environment/
```

# Unity Naming Conventions

**Policy:** Strict. Authoritative rules live in [`STANDARDS_CANONICAL.md`](STANDARDS_CANONICAL.md). This file expands naming with examples; if anything disagrees, the canonical document wins.

This document defines the naming conventions for all C# code in Unity projects.

## Overview

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `PlayerController` |
| Structs | PascalCase | `CharacterStats` |
| Enums | PascalCase | `WeaponType` |
| Enum Values | PascalCase | `WeaponType.Sword` |
| Interfaces | IPascalCase | `IDamageable` |
| Methods | PascalCase | `TakeDamage()` |
| Properties | PascalCase | `Health` |
| Public mutable fields | — | *disallowed — use property or `[SerializeField] private`* |
| Private Fields | _camelCase | `_health` |
| Constants | PascalCase | `MaxHealth` |
| Events | OnPascalCase | `OnHealthChanged` |
| Coroutines | CoPascalCase | `CoFadeIn()` |

---

## Classes and Structs

### Regular Classes
```csharp
public class PlayerController : MonoBehaviour { }
public class AudioManager : Singleton<AudioManager> { }
public class GameStateMachine : StateMachine<GameState> { }
```

### Structs
```csharp
public struct Vector3Data { }
public struct CharacterStats
{
    public int Health;
    public float Speed;
}
```

### Enums
```csharp
public enum WeaponType
{
    Sword,
    Bow,
    Staff
}

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver
}
```

### Interfaces
```csharp
public interface IDamageable
{
    void TakeDamage(int amount);
}

public interface IInteractable
{
    void Interact();
    bool CanInteract();
}
```

---

## Fields and Variables

### Private Fields (with SerializeField)
```csharp
[SerializeField] private int _health;
[SerializeField] private float _speed;
[SerializeField] private Transform _target;
```

### Private Fields (no SerializeField)
```csharp
private int _maxHealth;
private float _movementSpeed;
private bool _isInitialized;
```

### Public Fields (disallowed in new code)

Public mutable fields are **not allowed** in production code. Use `[SerializeField] private` for Inspector data or properties for API:

```csharp
[SerializeField] private int _health;
public int Health { get; private set; }
```

Exception: `public const` / `public static readonly` (PascalCase) per `STANDARDS_CANONICAL.md`.

### Constants
```csharp
public const int MaxHealth = 100;
public const float DefaultSpeed = 5f;
public const string PlayerTag = "Player";
```

### Static Readonly
```csharp
public static readonly Vector3 SpawnPoint = new Vector3(0, 1, 0);
public static readonly string[] ValidLayers = { "Ground", "Platform" };
```

---

## Properties

### Auto-Implemented Properties
```csharp
public int Health { get; private set; }
public float Speed { get; private set; }
public bool IsAlive { get; private set; }
```

### Readonly Properties
```csharp
public Vector3 Position => transform.position;
public Quaternion Rotation => transform.rotation;
```

### Properties with Backing Field
```csharp
private int _health;
public int Health
{
    get => _health;
    set => _health = Mathf.Clamp(value, 0, MaxHealth);
}
```

---

## Methods

### Regular Methods
```csharp
public void Initialize() { }
public void TakeDamage(int amount) { }
public Vector3 GetSpawnPosition() { return Vector3.zero; }
```

### Void Methods (event callbacks)
```csharp
private void OnTriggerEnter(Collider other) { }
private void OnHealthChanged(int newHealth) { }
```

### Coroutines
```csharp
private IEnumerator CoFadeIn() { yield return null; }
private IEnumerator CoMoveTo(Vector3 destination) { yield return null; }
private IEnumerator CoWaitForSeconds(float duration) { yield return new WaitForSeconds(duration); }
```

### Events
```csharp
public event Action<int> OnHealthChanged;
public event Action OnDeath;
public event Action<Transform> OnTargetFound;
```

### Event Handlers
```csharp
private void HandleHealthChanged(int newValue) { }
private void HandleDeath() { }
```

---

## Unity Specific Patterns

### Singleton
```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

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

### State Machine
```csharp
public class PlayerStateMachine : StateMachine<PlayerState>
{
    [SerializeField] private PlayerController _controller;

    private void Update()
    {
        Tick();
    }
}

public enum PlayerState
{
    Idle,
    Running,
    Jumping,
    Falling
}
```

### ScriptableObject Event Channel
```csharp
[CreateAssetMenu(menuName = "Events/Health Event Channel")]
public class HealthEventChannel : ScriptableObject
{
    public event Action<int> OnHealthChanged;

    public void RaiseEvent(int newHealth)
    {
        OnHealthChanged?.Invoke(newHealth);
    }
}
```

---

## File Naming

| Content | Filename |
|---------|----------|
| Class `PlayerController` | `PlayerController.cs` |
| Class `AudioManager` | `AudioManager.cs` |
| Enum `WeaponType` | `WeaponType.cs` |
| Interface `IDamageable` | `IDamageable.cs` |
| ScriptableObject `GameConfig` | `GameConfig.cs` |

**Rule:** One class per file. Filename must match the class name exactly.

---

## Folder Structure for Scripts

```
Scripts/
├── Core/
│   ├── Managers/
│   ├── Singletons/
│   └── StateMachines/
├── Systems/
│   ├── Audio/
│   ├── Inventory/
│   └── UI/
├── Features/
│   ├── Player/
│   ├── Combat/
│   └── Dialogue/
├── ScriptableObjects/
│   ├── Events/
│   ├── Data/
│   └── Configs/
└── Editor/
```

---

## Naming Suffixes and Prefixes

| Suffix/Prefix | Usage | Example |
|---------------|-------|---------|
| `Controller` | Main behavior class | `PlayerController` |
| `Manager` | Singleton manager | `GameManager` |
| `System` | Non-MonoBehaviour service | `DamageSystem` |
| `Handler` | Event/delegate handler | `InputHandler` |
| `Factory` | Creation pattern | `EnemyFactory` |
| `Pool` | Object pooling | `BulletPool` |
| `Data` | Data containers | `CharacterData` |
| `Config` | Configuration SO | `GameConfig` |
| `Event` | Event channels | `HealthEvent` |
| `State` | State enums | `PlayerState` |
| `Base` | Base classes | `EnemyBase` |

---

## Anti-Patterns (Avoid)

```csharp
// BAD - Hungarian notation
private int m_iHealth;
private float m_fSpeed;

// BAD - Underscore without SerializeField
private int _health; // Should be [SerializeField] if you need inspector

// BAD - Naming fields after types
private GameObject playerObject;
private AudioSource audioSource1;

// BAD - Generic names
private int temp;
private int temp2;
private object data;

// BAD - Abbreviations
private int hp;        // Use: _health
private float spd;     // Use: _speed
private AudioSource aus; // Use: _audioSource

// BAD - Prefix with underscore for public
public int _Health;    // No underscore for public, PascalCase instead

// BAD - Interfaces without I prefix
public interface Damageable { }  // Use: IDamageable
```

---

## Quick Reference

```
Classes:        PascalCase        PlayerController
Structs:        PascalCase        CharacterStats
Enums:          PascalCase        WeaponType.Sword
Interfaces:     IPascalCase       IDamageable

Methods:        PascalCase        TakeDamage()
Properties:     PascalCase       Health
Public Fields:  PascalCase        Health
Private Fields: _camelCase       _health

Constants:      PascalCase        MaxHealth
Events:         OnPascalCase     OnHealthChanged
Coroutines:     CoPascalCase     CoFadeIn()
```

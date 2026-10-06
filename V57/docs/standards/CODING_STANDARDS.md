# Unity Coding Standards

**Policy:** Strict. Authoritative rules live in [`STANDARDS_CANONICAL.md`](STANDARDS_CANONICAL.md). This file adds patterns and examples; it must not contradict the canonical document.

Additional coding standards beyond naming conventions.

## Namespaces

Format: `{Company}.{Project}.{Module}`

```csharp
namespace Company.Project.Core
{
    public class GameManager { }
}

namespace Company.Project.Features.Combat
{
    public class DamageSystem { }
}

namespace Company.Project.Systems.Audio
{
    public class AudioManager { }
}
```

### Namespace Organization by Folder

Each folder under `Scripts/` should correspond to a namespace:

```
Scripts/
├── Core/           → Company.Project.Core
│   ├── Managers/   → Company.Project.Core.Managers
│   └── Utils/      → Company.Project.Core.Utils
├── Systems/        → Company.Project.Systems
│   ├── Audio/      → Company.Project.Systems.Audio
│   └── UI/         → Company.Project.Systems.UI
└── Features/       → Company.Project.Features
    └── Player/     → Company.Project.Features.Player
```

---

## Regions

**Strict policy:** Follow `STANDARDS_CANONICAL.md`. In practice:

- Files **over 50 lines**: use `#region` for Unity lifecycle, public API, and private implementation (and event handlers when it helps).
- Files **50 lines or fewer**: regions optional; keep a clear order (fields → lifecycle → methods).

Use regions for:
1. Unity lifecycle methods
2. Public/Private sections
3. Event handling (optional dedicated region when many handlers)

```csharp
public class PlayerController : MonoBehaviour
{
    #region Unity Lifecycle
    private void Awake() { }
    private void Start() { }
    private void Update() { }
    private void OnDestroy() { }
    #endregion

    #region Public Methods
    public void TakeDamage(int amount) { }
    public void Heal(int amount) { }
    #endregion

    #region Private Methods
    private void HandleDeath() { }
    private void UpdateAnimation() { }
    #endregion
}
```

---

## File Headers

Optional header for documentation purposes:

```csharp
// =============================================================================
// Company.Project.Features.Player
// PlayerController.cs
//
// Description:
//     Controls player movement and handles input.
// =============================================================================
```

Or a minimal header:

```csharp
// <summary>Controls player movement and handles input.</summary>
```

---

## File Length

- **Maximum: 200 lines** per file (hard cap — splitting required)
- **Recommended: 120 lines** for better readability
- If a file exceeds 200 lines, split types or extract helpers into new files

---

## Explicit Types (No var)

**Always use explicit type declarations instead of `var`.**

```csharp
// BAD - Using var
var speed = 10;
var damage = CalculateDamage();
var player = GetPlayer();

// GOOD - Explicit type
int speed = 10;
int damage = CalculateDamage();
PlayerController player = GetPlayer();
```

**No exception in strict mode:** do not use `var` in production code. See `STANDARDS_CANONICAL.md`.

---

## Dependency Injection

### Constructor Injection (preferred for non-MonoBehaviour)
```csharp
public class DamageCalculator
{
    private readonly IHealthSystem _healthSystem;
    private readonly IStatsSystem _statsSystem;

    public DamageCalculator(IHealthSystem healthSystem, IStatsSystem statsSystem)
    {
        _healthSystem = healthSystem;
        _statsSystem = statsSystem;
    }
}
```

### Service Locator Pattern (for MonoBehaviour)
```csharp
public class ServiceLocator
{
    public static ServiceLocator Instance { get; private set; }

    public IAudioService Audio { get; set; }
    public IGameStateService GameState { get; set; }
}
```

### Find with Tag (use sparingly)
```csharp
// Only for truly global singletons
[SerializeField] private string _playerTag = "Player";
private PlayerController _player;

private void Awake()
{
    GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);
    if (playerObject != null)
    {
        _player = playerObject.GetComponent<PlayerController>();
    }
}
```

---

## Coroutines vs async/await

### async/await (preferred for new code)
```csharp
public async Task LoadSceneAsync(string sceneName)
{
    AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
    while (!operation.isDone)
    {
        _progressBar.Value = operation.progress;
        await Task.Yield();
    }
}

public async Task<float> GetDamagePerSecond()
{
    await Task.Delay(100); // Simulate calculation
    return CalculateDPS();
}
```

### Coroutines (for Unity lifecycle integration)
```csharp
private IEnumerator CoShowWelcomeMessage()
{
    _welcomePanel.SetActive(true);
    yield return new WaitForSeconds(3f);
    _welcomePanel.SetActive(false);
}
```

### Mixed approach (start coroutine from async)
```csharp
public async Task StartGameAsync()
{
    await _sceneLoader.LoadSceneAsync("GameScene");
    StartCoroutine(CoFadeIn());
}
```

---

## Null Checks

### Null coalescing for defaults
```csharp
private int _maxHealth = 100;
[SerializeField] private int _overrideMaxHealth = 0;

public int MaxHealth => _overrideMaxHealth > 0 ? _overrideMaxHealth : _maxHealth;
```

### Null check with early return
```csharp
private void OnTriggerEnter(Collider other)
{
    IDamageable damageable = other.GetComponent<IDamageable>();
    if (damageable == null) return;

    damageable.TakeDamage(_damage);
}
```

### Null check before assignment
```csharp
if (_debugText != null)
{
    _debugText.text = $"Health: {_health}";
}
```

---

## Collections

### Arrays vs Lists

- **Arrays:** Fixed size, performance critical, serialized by Unity
- **Lists:** Dynamic size, more flexible, use when count varies

```csharp
// For serialized data (Unity can serialize List)
[SerializeField] private List<EnemyBase> _enemies = new List<EnemyBase>();

// For runtime-only dynamic collections
private readonly List<Bullet> _activeBullets = new List<Bullet>();
private readonly Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();
```

---

## ScriptableObjects

### Definition
```csharp
[CreateAssetMenu(menuName = "Game/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [SerializeField] private int _maxHealth;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private int _attackPower;

    public int MaxHealth => _maxHealth;
    public float MoveSpeed => _moveSpeed;
    public int AttackPower => _attackPower;
}
```

### Usage
```csharp
[SerializeField] private CharacterStats _stats;
private int _currentHealth;

private void Start()
{
    _currentHealth = _stats.MaxHealth;
}

private void TakeDamage(int damage)
{
    _currentHealth = Mathf.Max(0, _currentHealth - damage);
}
```

---

## Attributes

### Common Unity Attributes
```csharp
[Header("Movement Settings")]
[SerializeField] private float _speed;

[Tooltip("Damage per second")]
[SerializeField] private float _damagePerSecond;

[Range(0, 100)]
[SerializeField] private float _healthPercent;

[ContextMenu("Reset Health")]
public void ResetHealth() { }

[ContextMenuItem("Reset to Max", "ResetHealth")]
[SerializeField] private float _currentHealth;
```

---

## Error Handling

### Logging
```csharp
private void Awake()
{
    if (_player == null)
    {
        Debug.LogWarning($"[{GetType().Name}] Player not found. Using default.");
        _player = FindObjectOfType<PlayerController>();
    }
}
```

### Assert
```csharp
using UnityEngine.Assertions;

private void Start()
{
    Assert.IsNotNull(_player, $"[{GetType().Name}] Player reference is required.");
    Assert.IsNotNull(_stats, $"[{GetType().Name}] Stats reference is required.");
}
```

---

## Performance Considerations

### Caching
```csharp
// BAD - Every frame
private void Update()
{
    GetComponent<Rigidbody>().velocity = Vector3.forward * _speed;
}

// GOOD - Cached reference
private Rigidbody _rigidbody;

private void Awake()
{
    _rigidbody = GetComponent<Rigidbody>();
}

private void Update()
{
    _rigidbody.velocity = Vector3.forward * _speed;
}
```

### Destroy vs DestroyImmediate
```csharp
// Use Destroy for runtime objects (99% of cases)
Destroy(gameObject);

// Use DestroyImmediate only in Editor scripts, and only when necessary
#if UNITY_EDITOR
DestroyImmediate(_editorOnlyObject);
#endif
```

---

## Script Templates

### MonoBehaviour Template
```csharp
using UnityEngine;

namespace Company.Project.Module
{
    public class ClassName : MonoBehaviour
    {
        [SerializeField] private float _value;

        #region Unity Lifecycle
        private void Awake() { }
        private void Start() { }
        private void Update() { }
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

### ScriptableObject Template
```csharp
using UnityEngine;

namespace Company.Project.Data
{
    [CreateAssetMenu(menuName = "Game/Data/TypeName")]
    public class TypeName : ScriptableObject
    {
        [SerializeField] private float _value;

        public float Value => _value;
    }
}
```

---

## Testing Conventions

### Unit Tests
```csharp
public class DamageCalculatorTests
{
    [Test]
    public void CalculateDamage_WithNoArmor_ReturnsBaseDamage()
    {
        var calculator = new DamageCalculator();
        int result = calculator.CalculateDamage(10, 0);
        Assert.AreEqual(10, result);
    }
}
```

### PlayMode Tests
```csharp
public class PlayerControllerTests
{
    private PlayerController _player;

    [SetUp]
    public void Setup()
    {
        PlayerController prefab = Resources.Load<PlayerController>("Prefabs/Player");
        _player = Object.Instantiate(prefab);
    }

    [TearDown]
    public void Teardown()
    {
        Object.Destroy(_player.gameObject);
    }

    [UnityTest]
    public IEnumerator TakeDamage_ReducesHealth()
    {
        int initialHealth = _player.Health;
        _player.TakeDamage(10);
        yield return null;
        Assert.Less(_player.Health, initialHealth);
    }
}
```

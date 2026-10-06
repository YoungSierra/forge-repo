# Unity Audio Skill (`/audio-setup`)

Configure **AudioMixer**, SFX pooling, spatial vs 2D sources, and audio data assets via Unity MCP with OVR verification. Slice gate: primary action should be **hearable** when TDD lists SFX.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **OVR helper** | `unity-editor-verification-skill.md` |
| **Import naming** | `unity-import-skill.md` — `sfx_`, `mus_` prefixes |
| **Task subagent** | When G-AUDIO applies |

---

## Invocation

```bash
/audio-setup --mixer Assets/Audio/Mixers/MainMixer.mixer
/audio-setup --spec @V57/specs/<slug>/features/coin_collectible.yaml
/audio-setup verify --mixer Assets/Audio/Mixers/MainMixer.mixer
/audio-setup --dry-run
```

---

## Standard mixer layout

| Group | Purpose | Exposed param |
|-------|---------|---------------|
| Master | Root | `MasterVolume` |
| Music | BGM streaming | `MusicVolume` |
| SFX | Gameplay one-shots | `SfxVolume` |
| UI | Clicks, HUD | `UiVolume` |
| Voice | Dialogue / VO (optional) | `VoiceVolume` |

Operate via MCP + `AssetDatabase.CreateAsset` for mixer; assign groups and expose floats. Optional ducking: Music lowers while Voice plays (snapshot or script).

---

## SFX pattern

- **Pool:** `AudioSource` pool on `AudioManager` or dedicated `SfxPool` MonoBehaviour
- **Data:** `AudioCue` / `AudioEvent` ScriptableObject — clip ref, volume, pitch variance, mixer group, **spatialBlend** (0 = 2D UI/SFX, 1 = world)
- **Trigger:** gameplay calls `IAudioService.Play(AudioCueId)` — no scattered `PlayOneShot` in every script
- **2D games:** prefer spatialBlend 0 unless positional audio is in TDD
- **3D games:** world SFX use 3D curve; UI stays 2D

---

## Import settings (via MCP or `/asset-pipeline`)

| Type | Load type | Compression |
|------|-----------|-------------|
| Music | Streaming (+ Load In Background) | Vorbis/OGG |
| SFX short | Decompress On Load | ADPCM/PCM per platform |
| SFX long | Compressed In Memory | Vorbis |

Folders: `Assets/Audio/Music/`, `Assets/Audio/SFX/`, `Assets/Audio/Mixers/`, `Assets/Audio/Voice/`

Full matrix (sample rates, mono-for-3D, mixer CPU): `shared/references/unity-audio-optimize-reference.md`.

### Optimize pass (when G-AUDIO or `/perf-audit`)

1. Confirm exactly one `AudioListener` in the play scene.
2. Flag stereo clips with `spatialBlend > 0` → Force To Mono on importer (CLI/`eval` reimport — do not hand-edit `.meta`).
3. Flag Decompress On Load on clips &gt; 1 MB; Streaming without Load In Background.
4. Keep mixer depth shallow; no expensive FX on Master.

---

## Verify (read-back)

- Mixer asset exists; group names match spec
- Exposed parameters readable via `mixer.GetFloat("MasterVolume", out _)`
- `AudioCue` assets reference valid clips and group
- Run-scope gate: primary action cue wired (or documented missing clip with WARN → `/playability-cert` G-FUNC-01 may FAIL if TDD requires hearable feedback)
- Test play one SFX via MCP (optional)
- Prefer CLI `eval` for importer/listener read-back after optimize

Report: `Docs/V57/reports/audio-setup-report-{slug}.md`

---

## Spec block (`audio:`)

Events per mechanic: cue id, trigger, mixer group, clip path (when art exists), spatialBlend.

---

## Integration

`/game-setup` **G-AUDIO** when TDD/spec has audio or primary action needs SFX. If not required: **silent skip** — no report, no progress line.

---

*Last updated: 2026-09-10*

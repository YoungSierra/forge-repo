# Audio optimize reference (V57)

Condensed from Unity Agent Plugin `optimize-audio`. Use from `/audio-setup` audit / `/perf-audit`.

## Load type

| Load type | Use for |
|-----------|---------|
| Decompress On Load | Short SFX (&lt; ~200 KB PCM) |
| Compressed In Memory | Medium / occasional clips |
| Streaming (+ Load In Background) | Music, long ambience, VO |

**Flags:** Decompress On Load on &gt;1 MB = memory bloat; Streaming on many simultaneous voices = disk pressure.

## Compression / sample rate

| Platform | Format notes |
|----------|----------------|
| PC | Vorbis 0.5–0.7 (dialogue 0.7–0.85) |
| iOS | AAC preferred |
| Android / Web | Vorbis |
| Mobile SFX / UI | Consider 22050 Hz |

## Spatial

- `spatialBlend > 0` + stereo clip → only left channel: enable **Force To Mono** on importer.
- Exactly **one** `AudioListener` in play scenes.

## Mixer CPU

- Prefer ≤3 group levels (Master → Music/SFX/UI → optional leaf).
- Do not park reverb/chorus on Master.
- Prefer snapshots over toggling heavy effects at runtime.
- DSP buffer &lt; 256 → consider Good Latency (512) unless rhythm game needs Best Latency.

## Verify (CLI `eval`)

Re-read importer `loadType`, `channels`, `forceToMono` after reimport; confirm `AudioSource.outputAudioMixerGroup`.

*Upstream: Unity-Technologies/unity-agent-plugin — Companion License.*

# Lighting lessons

## Light cookies leak into shadow
- **Symptom:** the underwater caustics (a cookie on the sun) still showed inside a roofed corridor.
- **Cause:** sun shadow strength 0.85. A shadowed pixel still gets 15 % of the light, and the cookie pattern is high contrast, so it stays readable.
- **Rule:** a directional light that carries a cookie (caustics, cloud shadows, foliage) uses shadow strength 1.0. Shadowed areas get their light from ambient or probes, not from a weaker sun.
- **How to check:** before touching geometry, render with the light's cookie removed and with the shadow strength at 1. The URP Rendering Debugger "Shadow Cascades" view shows whether receivers sample the shadow map.
- **Source:** PS V10 (D-272).

## Flipped modular pieces cast no shadow
- **Symptom:** floor plates reused upside down as roofs (or on their side as walls) let the sun through.
- **Cause:** single-sided meshes. The shadow pass culls back faces, and a flipped piece faces away from the light.
- **Rule:** structure pieces the layout places flipped or sideways cast two-sided shadows (`shadows: two_sided` in `Docs/V57/asset_rules.json`).
- **Source:** PS V10.

# SFX — Professor Wort & Sprat · lista y prompts para ElevenLabs

Fuente: TDD §10 (Audio Direction), §13.1 (≈ 30 SFX, 3 ambiences, ≈ 22 VO no verbal) y el "Feedback by outcome" de cada mecánica (§B), más las puertas B-17 (D-260).
Cada SFX tiene su **ID** (nombre del archivo y del cue en `AudioService`), cuándo suena, ajustes sugeridos y el **prompt** listo para copiar.

## Cómo generarlos

- **Herramienta:** ElevenLabs → Sound Effects (modelo v2). Escribe los prompts **en inglés**, tal como están.
- **Duración:** usa la indicada (no "auto"); los SFX cortos responden mejor con una duración fija.
- **Prompt influence:** 0.6 por defecto. Súbelo a 0.8 si el resultado se aleja del prompt; bájalo a 0.4 si suena rígido.
- **Loop:** actívalo solo donde dice **Loop: sí** (ambientes, patter del cangrejo, raspado).
- **Variantes:** genera 3–4 tomas por SFX y quédate con la mejor. Donde dice **Variantes: N**, entrega N tomas distintas (por ejemplo, pasos) y el motor alterna entre ellas.
- **Formato de descarga:** WAV (PCM) 48 kHz si tu plan lo permite; si no, MP3 de la mayor calidad. Lo convertimos al importar.

## Estilo común (ya incluido en cada prompt)

Plataformas 3D estilizado, tono caricaturesco y juguetón (referencia: Rayman Legends/Origins), en un laboratorio submarino de acero mojado. Sonidos **limpios, cercanos, secos, con ataque inmediato**: sin música, sin voces, sin reverb larga (la acústica la pone el motor).

## Entrega

- **Nombre:** `<ID>.wav`; con variantes, `<ID>_01.wav`, `<ID>_02.wav`, …
- **Edición mínima:** sin silencio al inicio (el sonido debe arrancar en el primer ms); pico máximo −1 dBFS.
- **Dónde:** anéxalos aquí en el chat o en una carpeta; yo los ubico en `Assets/_Game/Audio/SFX/<Categoría>/` y los conecto.
- **Columna "Motor":** **conectado** = el evento ya existe en el código y suena apenas llegue el archivo; **por conectar** = hay que añadir el disparo (lo hago yo al integrarlo).

## Resumen

| # | ID | Categoría | Duración | Loop | Var. | Motor | Estado |
|---|---|---|---|---|---|---|---|
| 1 | `SFX_Footstep_Steel` | Player | 0.35 s | no | 1 | conectado | ☑ entregado (D-264) |
| 2 | `SFX_Jump` | Player | 0.5 s | no | 2 | conectado | ☑ entregado (D-266) |
| 3 | `SFX_Land` | Player | 0.6 s | no | 2 | conectado | ☑ entregado, +13.6 dB (D-266) |
| 4 | `SFX_SlopeScrape` | Player | 2.0 s | sí | 1 | por conectar | ☐ |
| 5 | `SFX_StompWhoosh` | Player | 0.6 s | no | 1 | por conectar | ☐ |
| 6 | `SFX_StompImpact` | Player | 0.8 s | no | 2 | conectado | ☐ |
| 7 | `SFX_FallOut` | Player | 1.2 s | no | 1 | por conectar | ☐ |
| 8 | `SFX_Respawn` | Player | 1.0 s | no | 1 | por conectar | ☐ |
| 9 | `SFX_FlyPop` | Flies | 0.4 s | no | 3 | conectado | ☐ |
| 10 | `SFX_FlyPopLast` | Flies | 0.6 s | no | 1 | conectado | ☐ |
| 11 | `SFX_FlyBuzz` | Flies | 2.0 s | sí | 1 | por conectar | ☐ |
| 12 | `SFX_ZoneComplete` | Flies | 1.5 s | no | 1 | por conectar | ☐ |
| 13 | `SFX_KeyMaterialise` | Key | 1.5 s | no | 1 | por conectar | ☐ |
| 14 | `SFX_KeyCollect` | Key | 0.8 s | no | 1 | por conectar | ☐ |
| 15 | `SFX_ZoneDoorSlideUp` | Door | 1.0 s | no | 1 | por conectar | ☐ |
| 16 | `SFX_ZoneDoorLocked` | Door | 0.6 s | no | 1 | por conectar | ☐ |
| 17 | `SFX_LabDoorOpen` | Door | 0.8 s | no | 1 | por conectar | ☐ |
| 18 | `SFX_LabDoorClose` | Door | 0.8 s | no | 1 | por conectar | ☐ |
| 19 | `SFX_CrabPatter` | Crab | 2.0 s | sí | 1 | por conectar | ☐ |
| 20 | `SFX_CrabCharge` | Crab | 0.6 s | no | 1 | por conectar | ☐ |
| 21 | `SFX_CrabContact` | Crab | 0.6 s | no | 2 | conectado | ☐ |
| 22 | `SFX_CrabDefeat` | Crab | 1.2 s | no | 1 | conectado | ☐ |
| 23 | `SFX_SharkPass` | Ambient creatures | 3.0 s | no | 1 | por conectar | ☐ |
| 24 | `SFX_JellyfishPulse` | Ambient creatures | 2.0 s | no | 2 | por conectar | ☐ |
| 25 | `SFX_FishSchool` | Ambient creatures | 2.5 s | no | 1 | por conectar | ☐ |
| 26 | `SFX_Bubbles` | Environment | 1.5 s | no | 3 | por conectar | ☐ |
| 27 | `SFX_UI_Navigate` | UI | 0.15 s | no | 1 | por conectar | ☐ |
| 28 | `SFX_UI_Confirm` | UI | 0.4 s | no | 1 | por conectar | ☐ |
| 29 | `SFX_UI_Back` | UI | 0.3 s | no | 1 | por conectar | ☐ |
| 30 | `SFX_UI_Pause` | UI | 0.5 s | no | 1 | por conectar | ☐ |
| 31 | `SFX_UI_Resume` | UI | 0.5 s | no | 1 | por conectar | ☐ |
| 32 | `AMB_Sector7HydroLabs` | Ambience | 30 s | sí | 1 | por conectar | ☐ |
| 33 | `AMB_CrystalPassage` | Ambience | 30 s | sí | 1 | por conectar | ☐ |
| 34 | `AMB_ResearchWingB17` | Ambience | 30 s | sí | 1 | por conectar | ☐ |

Los VO no verbales (Profesor y Sprat) están al final, en una sección aparte.

---

## Player — el Profesor

### 1. `SFX_Footstep_Steel`
- **Cuándo:** cada paso al caminar o correr sobre la rejilla de acero mojado (TDD §B Locomotion: "footstep SFX").
- **Duración:** 0.35 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 1 (una sola pisada metálica; misma superficie en todo el nivel)
```
Single footstep of a small, chubby cartoon character in rubber boots on a wet perforated steel grate, a soft metallic tap with a tiny water splash, underwater lab, light and bouncy, close-mic, dry, no reverb, no music, no voice
```

### 2. `SFX_Jump`
- **Cuándo:** al despegar en un salto (§B Jump: "jump SFX").
- **Duración:** 0.5 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 2
```
Cartoon platformer jump take-off underwater: a quick springy boot push off a wet metal floor followed by a short bubbly upward whoosh, playful and light, close-mic, dry, no music, no voice
```

### 3. `SFX_Land`
- **Cuándo:** al aterrizar (§B Jump: "land SFX"; splash VFX).
- **Duración:** 0.6 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 2
```
Small cartoon character landing on a wet steel grate floor: a soft cushioned thud with a short water splash and a faint metallic rattle of the grate, playful, close-mic, dry, no music, no voice
```

### 4. `SFX_SlopeScrape`
- **Cuándo:** mientras el Profesor resbala por una pendiente demasiado empinada (§B Locomotion: "scrape SFX").
- **Duración:** 2.0 s · **Influence:** 0.6 · **Loop:** sí · **Variantes:** 1
```
Continuous rubber boots sliding and scraping down a wet sloped metal surface, steady squeaky friction with light water hiss, seamless loop, cartoon tone, close-mic, dry, no music, no voice
```

### 5. `SFX_StompWhoosh`
- **Cuándo:** al iniciar el ataque en picada desde el aire (§B Stomp: "slam start → whoosh SFX").
- **Duración:** 0.6 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 1
```
Fast downward cartoon dive whoosh underwater, a rising-to-falling swoosh with a trail of bubbles, energetic and punchy, ends abruptly, close-mic, dry, no music, no voice
```

### 6. `SFX_StompImpact`
- **Cuándo:** cuando el ataque en picada toca el suelo (§B Stomp: "impact SFX", camera bump).
- **Duración:** 0.8 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 2
```
Heavy cartoon ground-pound impact on a steel floor underwater: a deep punchy metallic boom with a short water splash burst and a quick grate rattle, satisfying and chunky, close-mic, short tail, no music, no voice
```

### 7. `SFX_FallOut`
- **Cuándo:** al caer fuera del nivel, antes del respawn (`RespawnService`).
- **Duración:** 1.2 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 1
```
Cartoon falling-away sound underwater: a descending slide whistle mixed with a soft bubbly plunge, comedic but gentle, fades out, no music, no voice
```

### 8. `SFX_Respawn`
- **Cuándo:** cuando el Profesor reaparece en el último punto seguro (VFX "respawn fade").
- **Duración:** 1.0 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 1
```
Magical cartoon reappear sound underwater: a soft rising shimmer with a gentle bubble swirl and a light pop at the end, friendly and short, no music, no voice
```

---

## Flies — moscas

### 9. `SFX_FlyPop`
- **Cuándo:** al recoger una mosca (§B FlyCollection: "pop SFX").
- **Duración:** 0.4 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 3
```
Cute cartoon collectible pickup: a bright juicy bubble pop with a tiny sparkling chime on top, glowing neon insect being collected, cheerful and satisfying, very short, close-mic, dry, no music, no voice
```

### 10. `SFX_FlyPopLast`
- **Cuándo:** al recoger la última mosca de la zona (§B: "higher-pitched pop").
- **Duración:** 0.6 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 1
```
Special cartoon collectible pickup, higher pitched than a normal one: a bright bubble pop followed by a quick rising sparkly two-note chime, celebratory and exciting, close-mic, dry, no music, no voice
```

### 11. `SFX_FlyBuzz`
- **Cuándo:** zumbido 3D de cada mosca mientras no ha sido recogida (ayuda a encontrarlas).
- **Duración:** 2.0 s · **Influence:** 0.5 · **Loop:** sí · **Variantes:** 1
```
Soft glowing neon fly buzzing gently in place, small and cute, slightly electric hum with faint bubbly wing flutter, calm, seamless loop, close-mic, dry, no music, no voice
```

### 12. `SFX_ZoneComplete`
- **Cuándo:** al completar todas las moscas de la zona (§B: "zone complete → ring VFX, sting SFX").
- **Duración:** 1.5 s · **Influence:** 0.7 · **Loop:** no · **Variantes:** 1
```
Short triumphant cartoon success sting made of sound effects (not music): a rising magical shimmer, a bright bubbly whoosh ring expanding outward and a sparkling chime burst at the end, joyful, underwater, no voice
```

---

## Key — llave de acceso

### 13. `SFX_KeyMaterialise`
- **Cuándo:** cuando aparece la llave de la zona (§B KeyMaterialisation: "dissolve-in 0.4 s, amber light"; el motivo musical `MUS_KeyMotif` va aparte).
- **Duración:** 1.5 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 1
```
Magical high-tech key materializing underwater: a warm rising energy hum that builds into a crystalline shimmer and resolves with a soft electric glint, amber glow feeling, wondrous, no music, no voice
```

### 14. `SFX_KeyCollect`
- **Cuándo:** al recoger la llave (§B: "collect VFX, SFX").
- **Duración:** 0.8 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 1
```
Cartoon key pickup: a satisfying metallic key jingle with a bright electronic confirmation blip and a little magical sparkle, rewarding and clear, close-mic, dry, no music, no voice
```

---

## Doors — puertas

### 15. `SFX_ZoneDoorSlideUp`
- **Cuándo:** la puerta de zona se abre subiendo en 0.8 s (§B: "door slides up 0.8 s"; la cadencia musical `MUS_DoorUnlockCadence` va aparte).
- **Duración:** 1.0 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 1
```
Heavy sci-fi underwater lab door unlocking and sliding upward: a magnetic lock clunk, a hydraulic hiss and a smooth mechanical slide ending in a solid stop, chunky and cartoonish, no music, no voice
```

### 16. `SFX_ZoneDoorLocked`
- **Cuándo:** el Profesor llega a la puerta sin la llave (§B: "door without key → locked clunk").
- **Duración:** 0.6 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 1
```
Locked heavy metal door refusing to open: a dull metallic clunk with a short negative electronic buzz, cartoon tone, clear and readable, close-mic, dry, no music, no voice
```

### 17. `SFX_LabDoorOpen`
- **Cuándo:** las puertas dobles B-17 se abren deslizándose a los lados al acercarse (D-260, 0.6 s).
- **Duración:** 0.8 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 1
```
Sci-fi double sliding doors opening sideways in an underwater research lab: a short pneumatic hiss and a smooth fast metallic glide splitting apart, ending with a soft thunk, cartoonish, no music, no voice
```

### 18. `SFX_LabDoorClose`
- **Cuándo:** las puertas dobles B-17 se cierran al alejarse.
- **Duración:** 0.8 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 1
```
Sci-fi double sliding doors closing in an underwater research lab: a smooth metallic glide coming together, ending with a firm soft clunk and a tiny air hiss, cartoonish, no music, no voice
```

---

## Crab — cangrejo mecánico

### 19. `SFX_CrabPatter`
- **Cuándo:** mientras patrulla (3D, se oye al acercarse) (§B CrabEncounter: "patter SFX (3D)").
- **Duración:** 2.0 s · **Influence:** 0.6 · **Loop:** sí · **Variantes:** 1
```
Small mechanical robot crab scuttling sideways on a wet steel floor: fast rhythmic clicking metal leg taps with tiny servo whirs, cute but slightly menacing, seamless loop, close-mic, dry, no music, no voice
```

### 20. `SFX_CrabCharge`
- **Cuándo:** el cangrejo embiste al tocar al Profesor (§B: "crab Charge 0.5 s").
- **Duración:** 0.6 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 1
```
Small robot crab revving up and lunging: a quick rising servo whine with snapping metal pincers, aggressive but cartoonish, close-mic, dry, no music, no voice
```

### 21. `SFX_CrabContact`
- **Cuándo:** el cangrejo golpea al Profesor (knockback + aturdido) (§B: "contact SFX").
- **Duración:** 0.6 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 2
```
Cartoon hit when a small robot crab bumps the hero underwater: a springy bonk with a metallic pinch clack and a quick water splash, comedic and readable, not violent, close-mic, dry, no music, no voice
```

### 22. `SFX_CrabDefeat`
- **Cuándo:** el Profesor aplasta al cangrejo con el ataque en picada (§B: "sparks, eye light off, defeat SFX").
- **Duración:** 1.2 s · **Influence:** 0.6 · **Loop:** no · **Variantes:** 1
```
Small robot crab being squashed and shutting down: a crunchy metal squish, a burst of electric sparks and a descending power-down whine ending in a little boing, satisfying and comedic, no music, no voice
```

---

## Ambient creatures — criaturas de fondo (decorativas, 3D)

### 23. `SFX_SharkPass`
- **Cuándo:** el tiburón radioactivo pasa en la distancia.
- **Duración:** 3.0 s · **Influence:** 0.5 · **Loop:** no · **Variantes:** 1
```
Large creature swimming past in the distance underwater: a deep slow water displacement whoosh with a low muffled rumble, mysterious, soft, distant, no music, no voice
```

### 24. `SFX_JellyfishPulse`
- **Cuándo:** pulsos ocasionales de las medusas.
- **Duración:** 2.0 s · **Influence:** 0.5 · **Loop:** no · **Variantes:** 2
```
Glowing jellyfish pulsing underwater: a soft gelatinous whoomp with a gentle airy shimmer and faint bubbles, dreamy and calm, distant, no music, no voice
```

### 25. `SFX_FishSchool`
- **Cuándo:** un banco de peces pasa cerca.
- **Duración:** 2.5 s · **Influence:** 0.5 · **Loop:** no · **Variantes:** 1
```
School of small fish darting past underwater: a quick fluttery swish of many tiny fins with light bubbles, lively and soft, no music, no voice
```

---

## Environment

### 26. `SFX_Bubbles`
- **Cuándo:** burbujas puntuales de tuberías y del fondo (decoración 3D).
- **Duración:** 1.5 s · **Influence:** 0.5 · **Loop:** no · **Variantes:** 3
```
Short burst of bubbles rising underwater from a pipe vent, gentle gurgling blub sounds, natural and soft, close-mic, dry, no music, no voice
```

---

## UI — menús y pausa

### 27. `SFX_UI_Navigate`
- **Cuándo:** mover el foco entre botones.
- **Duración:** 0.15 s · **Influence:** 0.7 · **Loop:** no · **Variantes:** 1
```
Tiny soft UI tick for moving between menu buttons, a short bubbly click, clean and subtle, underwater-themed, no music, no voice
```

### 28. `SFX_UI_Confirm`
- **Cuándo:** confirmar una opción.
- **Duración:** 0.4 s · **Influence:** 0.7 · **Loop:** no · **Variantes:** 1
```
Positive UI confirm sound: a bright bubbly pop with a friendly two-note rising blip, clean, playful, underwater-themed, no music, no voice
```

### 29. `SFX_UI_Back`
- **Cuándo:** volver o cancelar.
- **Duración:** 0.3 s · **Influence:** 0.7 · **Loop:** no · **Variantes:** 1
```
UI back or cancel sound: a soft descending bubbly blip, gentle, clean, playful, underwater-themed, no music, no voice
```

### 30. `SFX_UI_Pause`
- **Cuándo:** abrir la pausa (el mezclador aplica el filtro de pausa).
- **Duración:** 0.5 s · **Influence:** 0.7 · **Loop:** no · **Variantes:** 1
```
Game pause sound: a soft muffled whoosh down with a gentle bubbly stop, like sound going underwater, calm, clean, no music, no voice
```

### 31. `SFX_UI_Resume`
- **Cuándo:** salir de la pausa.
- **Duración:** 0.5 s · **Influence:** 0.7 · **Loop:** no · **Variantes:** 1
```
Game resume sound: a soft rising whoosh with a light bubbly pop, like surfacing back to clarity, clean, playful, no music, no voice
```

---

## Ambience — ambientes (uno por tema de zona, TDD §10)

### 32. `AMB_Sector7HydroLabs`
- **Cuándo:** fondo continuo en las zonas "Sector 7 Hydro Labs".
- **Duración:** 30 s · **Influence:** 0.4 · **Loop:** sí · **Variantes:** 1
```
Underwater research lab ambience: a low steady machinery hum, distant water pumps, occasional soft pipe gurgles and bubbles, faint metal creaks, calm and slightly mysterious, seamless loop, no music, no voice
```

### 33. `AMB_CrystalPassage`
- **Cuándo:** fondo continuo en las zonas "Crystal Passage".
- **Duración:** 30 s · **Influence:** 0.4 · **Loop:** sí · **Variantes:** 1
```
Underwater crystal tunnel ambience: soft resonant glassy tones from currents moving over crystal panels, gentle water flow, sparse tiny bubbles, airy and magical, seamless loop, no music, no voice
```

### 34. `AMB_ResearchWingB17`
- **Cuándo:** fondo continuo en las zonas "Research Wing B-17".
- **Duración:** 30 s · **Influence:** 0.4 · **Loop:** sí · **Variantes:** 1
```
Deep sea laboratory wing ambience: electrical buzz of lab equipment, slow ventilation, distant clanks behind thick walls, muffled ocean pressure rumble, faint bubbling tanks, slightly tense, seamless loop, no music, no voice
```

---

## VO no verbal (opcional con ElevenLabs Sound Effects)

El TDD pide ≈ 22 eventos de VO **sin palabras**: esfuerzos del Profesor y "tonales" de Sprat. ElevenLabs Sound Effects los genera bien si el prompt pide explícitamente **no words**. Si alguno no convence, se puede pasar a una sesión de voz real más adelante. Misma entrega: `VO_<ID>.wav`.

### Profesor (anciano científico caricaturesco, voz cálida y algo gruñona)

| ID | Cuándo | Dur. | Var. | Prompt |
|---|---|---|---|---|
| `VO_Prof_Jump` | despegue del salto | 0.5 s | 3 | `Short cartoon effort grunt of a chubby elderly scientist jumping, "hup!" sound without words, playful, close-mic, dry, no music` |
| `VO_Prof_Land` | aterrizaje desde altura | 0.5 s | 2 | `Short cartoon landing exhale of a chubby elderly scientist, a soft "oof" without words, close-mic, dry, no music` |
| `VO_Prof_Stomp` | inicio del ataque en picada | 0.7 s | 2 | `Determined cartoon battle cry of a chubby elderly scientist diving down, short "hyaa!" without words, comedic, close-mic, dry, no music` |
| `VO_Prof_Hit` | golpeado por el cangrejo | 0.6 s | 3 | `Cartoon pained yelp of a chubby elderly scientist bumped by a small robot, short "ow!" without words, comedic not violent, close-mic, dry, no music` |
| `VO_Prof_Fall` | cae fuera del nivel | 1.0 s | 1 | `Cartoon falling yell of a chubby elderly scientist fading away, "whoooa" without words, comedic, no music` |
| `VO_Prof_Collect` | recoge una mosca | 0.5 s | 3 | `Pleased cartoon chuckle of a chubby elderly scientist, short "heh!" without words, warm, close-mic, dry, no music` |
| `VO_Prof_Key` | recoge la llave | 0.8 s | 1 | `Delighted cartoon "aha!" exclamation sound of a chubby elderly scientist without real words, triumphant, close-mic, dry, no music` |
| `VO_Prof_Locked` | puerta cerrada sin llave | 0.7 s | 1 | `Grumpy cartoon "hmph" of a chubby elderly scientist, displeased, without words, close-mic, dry, no music` |
| `VO_Prof_Victory` | completa la zona o el nivel | 1.2 s | 1 | `Happy cartoon victory cheer of a chubby elderly scientist, short "woo-hoo!" without words, joyful, close-mic, dry, no music` |

### Sprat (pececito compañero, "tonales" tipo criatura, sin palabras)

| ID | Cuándo | Dur. | Var. | Prompt |
|---|---|---|---|---|
| `VO_Sprat_Idle` | ocasional mientras sigue al Profesor | 0.6 s | 3 | `Tiny cute fish creature chirp underwater, soft curious bubbly squeak, no words, close-mic, dry, no music` |
| `VO_Sprat_Excited` | moscas recogidas / queda una | 0.8 s | 3 | `Tiny cute fish creature excited happy trill underwater, rapid bubbly chirps, no words, close-mic, dry, no music` |
| `VO_Sprat_Hazard` | cangrejo a menos de 3 m | 0.7 s | 2 | `Tiny cute fish creature alarmed warning squeal underwater, short urgent bubbly chirps, no words, close-mic, dry, no music` |
| `VO_Sprat_Freeze` | Profesor aturdido | 0.6 s | 2 | `Tiny cute fish creature worried gasp underwater, a small startled bubbly squeak, no words, close-mic, dry, no music` |
| `VO_Sprat_Spin` | aparece la llave | 1.0 s | 1 | `Tiny cute fish creature amazed delighted spinning whistle underwater, rising bubbly "wheee" without words, close-mic, dry, no music` |

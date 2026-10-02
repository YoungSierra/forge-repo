# Plantilla de entrega V57

Esta es la estructura desde la que se arranca una entrega para V57 Studio. Se clona, se renombra al
slug del juego, se borran las carpetas que no apliquen y **no se añaden otras**.

La guía completa —campos, nombres de archivo, reglas de exportación y checklist— es
`ProviderStyle.md`. Este README es el mapa: dónde va cada cosa y en qué orden se entrega.

---

## Lo que se entrega y lo que no

| Se entrega | Lo hace V57 en Unity |
| --- | --- |
| TDD completo | Paquete Unity, escenas, prefabs, scripts |
| Art Direction Document y los ADI 11.1–11.11 **completos**, no su resumen | Shaders, materiales, presets de VFX Graph |
| Imágenes de referencia y concept art como archivos | Escenas de prueba PlayMode y tests |
| Modelos, animaciones, texturas, sprites de UI, VFX, audio y fuentes finales | `.meta`, `ProjectSettings/`, `Packages/` |
| Mockups de cada pantalla y vídeo de cada efecto | Import, atlas, compresión, versión de Unity |
| Licencias de todo recurso externo o generado con IA | |

Donde un documento pida «Unity package with sample scenes», presets de VFX Graph o tareas de
shader, se entrega la **especificación** escrita y la **referencia visual**. La implementación es
de V57.

---

## Dónde va cada cosa

```text
Docs/
  Design/
    TDD.md                          el TDD completo, con las tablas del contrato V57
    LevelMaps/<LevelId>.png         un plano por nivel
    Encounters/<LevelId>.csv        encuentros por nivel (ADI 11.10)
  ArtDirection/
    ArtDirectionDocument.md         el ADD maestro, TODAS las secciones
    VisualProductionBlueprint.md
    ADI/ADI_11.1_ConceptArt.md …    los once paquetes, con su contenido
    Reference/ref<N>_<Slug>.png     un archivo por reference_image
    Concept/<AssetName>_<Vista>.png solo lo aprobado
    UIMockups/<ScreenId>.png        una imagen por pantalla completa
    Camera/<ViewId>.png             cómo se ve el juego en cámara
    VFXReference/<AssetName>.mp4    el vídeo de cada efecto terminado
    AnimationPreviews/<AssetName>_<Clip>.mp4
  Audio/AudioParameters.md          parámetros y mapa de eventos (ADI 11.9)
  Marketing/                        ADI 11.11 — fuera del vertical slice
  LICENSES.md

Source/                             PSD, .blend, .spp: el trabajo, no se importa

Assets/_Game/
  Art/
    Environment/Blockout/<LevelName>/BLK_<LevelName>.fbx
    Environment/Kits/<AssetName>/{Meshes,Textures}
    Environment/Decoration/<AssetName>/{Meshes,Textures}
    Environment/Sky/
    Props/<Category>/<AssetName>/{Meshes,Textures}
    Characters/<AssetName>/{Meshes,Animations,Textures}
    Shared/Textures/
    UI/{Sprites/<UI_ID>, Icons, Fonts}
    VFX/<AssetName>/
  Audio/{Music, SFX/<Category>, Ambience, Voice/<lang>}
```

Cada carpeta lleva un `.gitkeep` que recuerda qué va dentro.

## De la ficha del asset al archivo

La `category` del brief decide la carpeta; el `asset_name` decide los nombres.

| `category` | Carpeta | Archivos |
| --- | --- | --- |
| Character, Companion, Boss | `Art/Characters/<AssetName>/` | `Meshes/SK_<AssetName>.fbx`, `Animations/ANIM_<AssetName>_<Clip>.fbx`, `Textures/T_<AssetName>_<Mapa>.png` |
| Prop, Interactable, Platform | `Art/Props/<Category>/<AssetName>/` | `Meshes/SM_<AssetName>.fbx`, `Textures/` |
| Environment tileset | `Art/Environment/Kits/<AssetName>/` | `Meshes/SM_<AssetName>_<Pieza>.fbx`, `Textures/` |
| Decoration, Background | `Art/Environment/Decoration/<AssetName>/` o `Sky/` | igual que props |
| VFX | `Art/VFX/<AssetName>/` | `VFX_<AssetName>_Sheet_<cols>x<rows>.png` |
| UI / Frame, UI element | `Art/UI/Sprites/<UI_ID>/` | `SPR_<Id>_<State>.png`; `_9s-<px>` si se estira |
| Icon, Font | `Art/UI/Icons/`, `Art/UI/Fonts/` | `ICO_<Name>.png`, `FNT_<Name>.ttf` |
| Music, SFX, Ambience, Voice | `Audio/...` | `MUS_`, `SFX_`, `AMB_`, `VO_` |
| Nivel del manifest | `Art/Environment/Blockout/<LevelName>/` | `BLK_<LevelName>.fbx` |

Sufijos de textura: `_BC` color · `_N` normal (OpenGL, verde arriba) · `_ORM` · `_E` emisión ·
`_Mask`.

## Reglas de exportación, en corto

- **Modelos:** 1 unidad = 1 metro, transformaciones aplicadas, Y arriba y Z adelante. Un asset por
  archivo, en el origen, sin cámaras ni luces. LODs como `SM_<Name>_LOD0/1/2` en el mismo FBX.
- **Colisión:** malla convexa `UCX_<AssetName>_NN` dentro del FBX. **Anclajes:** vacíos
  `Socket_<Nombre>`.
- **Material:** uno por conjunto de texturas, con el mismo nombre que el `asset_name` — así V57
  encuentra las texturas.
- **Rig:** T-pose o A-pose, un clip por FBX con el mismo esqueleto, 30 fps, máximo 4 influencias
  por vértice.
- **Texturas:** PNG real, lado potencia de 2.
- **UI:** un PNG por elemento y estado, con transparencia. Sin hojas de sprites: V57 arma los
  atlas.
- **Audio:** WAV 48 kHz / 24 bits, sin silencio inicial, loops sin corte.
- **Marcadores** dentro de cada `BLK_`: `Marker_Spawn_Player`, `Marker_Spawn_<Entity>_NN`,
  `Marker_Zone_<Tipo>`, `Marker_Bounds`, `Marker_Camera_<ViewId>`, `Marker_Checkpoint_NN`.

## Consistencia: por qué un acento rompe un asset

V57 enlaza los documentos por IDs y nombres **exactos, sin interpretar**. Un error de tipeo corta
el enlace y ese asset termina como placeholder. Un ID se escribe igual en el TDD, el ADD, los ADI y
los nombres de archivo; los números viven en un solo sitio y los demás lo citan; y ningún
`[DRAFT — artist approval required]` llega a la entrega sin resolver.

## Orden de entrega

Un commit por paso, con este mensaje exacto. **El P3 es el más importante**: con los blockouts V57
ya puede construir el juego jugable en gris mientras llega el resto.

| | |
| --- | --- |
| `P0 repo` | repositorio creado desde esta plantilla |
| `P1 design` | `TDD.md`, planos de nivel y CSV de encuentros |
| `P2 art-direction` | ADD, Blueprint, ADI 11.1–11.11, referencias, concept, mockups, cámara |
| `P3 blockout` | un `BLK_` por cada escena `slice: true`, con sus marcadores |
| `P4 characters` | personajes, companions y bosses con sus animaciones |
| `P5 props` | objetos jugables y plataformas |
| `P6 environment` | tilesets, decoración, cielo y texturas compartidas |
| `P7 ui` | sprites por `UI_*` y estado, íconos y fuentes |
| `P8 vfx` | texturas de efectos y vídeos de referencia |
| `P9 audio` | música, SFX, ambientes, voces y `AudioParameters.md` |
| `P10 delivery` | `LICENSES.md`, checklist verificado y tag `vX.Y.Z` |

## Checklist antes del tag

- [ ] La estructura es la de arriba; no hay `.meta`, `Library/`, `ProjectSettings/` ni `Packages/`
- [ ] Cada asset en `Assets/_Game/` tiene su brief, y cada brief apunta a archivos que existen
- [ ] Cada `UI_*` tiene sus sprites, y cada pantalla su mockup
- [ ] Cada escena `slice: true` tiene su `BLK_` con marcadores
- [ ] Cada `reference_images` tiene su archivo en `Reference/`
- [ ] Los paquetes ADI están completos, no resumidos
- [ ] Ningún `[DRAFT]` pendiente (o marcado `status: provisional`)
- [ ] IDs y nombres idénticos en todos los documentos
- [ ] Nombres con los prefijos de arriba, sin espacios, acentos ni `temp`/`copy`/`New Folder`
- [ ] Modelos a escala real (verificados junto a un cubo de 1 m) y con el pivote declarado
- [ ] `LICENSES.md` cubre todo recurso externo o generado con IA

---

## Antes de clonar: Git LFS

Los binarios van por **Git LFS** (ver `.gitattributes`). Hay que hacer `git lfs install` una vez en
la máquina **antes de clonar**; si no, se descargan punteros de texto en vez de archivos y parece
que el repositorio está roto.

```bash
git lfs install
git clone <url>
```

# <NombreDelJuego>

| Campo | Valor |
| --- | --- |
| Slug del juego | `<GameSlug>` (PascalCase, sin espacios; nombre de la carpeta raíz) |
| Versión de la entrega | `v1.0.0` |
| Proveedor | `<Estudio / contacto>` |
| Fecha | `AAAA-MM-DD` |

Este repositorio sigue el **Contrato de entrada V57**. El proveedor llena las carpetas con documentos y assets; V57 abre después este mismo repositorio con Unity y hace el resto (configuración, materiales, prefabs, escenas, código).

## Qué va en cada carpeta

| Carpeta | Contenido |
| --- | --- |
| `Docs/Design/` | `TDD.md`, `LevelMaps/<LevelId>.png`, `Encounters/<LevelId>.csv` |
| `Docs/ArtDirection/` | `ArtDirectionDocument.md` (ADD completo con campos V57), `VisualProductionBlueprint.md`, `ADI/ADI_11.x_*.md`, `Reference/ref<N>_<Slug>.png`, `Concept/`, `UIMockups/<ScreenId>.png`, `Camera/<ViewId>.png`, `VFXReference/<AssetName>.mp4`, `AnimationPreviews/` |
| `Docs/Audio/` | `AudioParameters.md` (mapa de eventos y parámetros) |
| `Docs/Marketing/` | Arte de marketing (fuera del vertical slice) |
| `Docs/LICENSES.md` | Origen y licencia de cada recurso externo o generado |
| `Source/` | Opcional: archivos de trabajo (`.blend`, `.psd`, `.spp`). Unity no lo lee |
| `Assets/_Game/Art/Environment/Blockout/<LevelId>/` | `BLK_<LevelId>.fbx`: geometría del nivel con marcadores `Marker_*` |
| `Assets/_Game/Art/Environment/Kits/<KitId>/` | Piezas modulares: `Meshes/`, `Textures/` |
| `Assets/_Game/Art/Environment/Decoration/<AssetId>/` | Adornos sin interacción: `Meshes/`, `Textures/` |
| `Assets/_Game/Art/Environment/Sky/` | Cielo o fondo (`.hdr`, `.exr`, `.png`) |
| `Assets/_Game/Art/Props/<Category>/<AssetId>/` | Objetos jugables, uno por carpeta: `Meshes/`, `Textures/` |
| `Assets/_Game/Art/Characters/<CharacterId>/` | `Meshes/SK_*.fbx`, `Animations/ANIM_*.fbx`, `Textures/` |
| `Assets/_Game/Art/Shared/Textures/` | Texturas repetibles (suelo, pasto, piedra) |
| `Assets/_Game/Art/UI/Sprites/<ScreenId>/` | Cada elemento de la pantalla en un PNG individual |
| `Assets/_Game/Art/UI/Icons/`, `Fonts/` | Íconos generales; fuentes `.ttf` / `.otf` con licencia |
| `Assets/_Game/Art/VFX/<EffectId>/` | Texturas o secuencias `VFX_<EffectId>_Sheet_<cols>x<rows>.png` |
| `Assets/_Game/Audio/` | `Music/`, `SFX/<Category>/`, `Ambience/`, `Voice/<lang>/` |

Las carpetas que no apliquen al juego se pueden borrar. No se crean carpetas fuera de esta lista.

## Qué NO se sube

- Nada de Unity: `.meta`, `Library/`, `ProjectSettings/`, `Packages/`, materiales, prefabs, escenas, scripts, `.yaml`.
- Candidatos, pruebas y temporales (`temp`, `copy`, `New Folder`, nombres automáticos de generadores).

## Nombres

Inglés, sin espacios ni acentos, mayúscula inicial por palabra: `<Prefijo>_<AssetId>[_<Variante>][_<Sufijo>]`.

`SM_` objeto 3D · `SK_` personaje · `ANIM_` animación · `BLK_` nivel · `T_` textura (`_BC` `_N` `_ORM` `_E`) · `SPR_` elemento de UI (`_9s-<px>` si se estira) · `ICO_` ícono · `FNT_` fuente · `VFX_` efecto · `MUS_` `SFX_` `AMB_` `VO_` audio · dentro del FBX: `UCX_` colisión, `Socket_` punto de anclaje, `Marker_` marcador de nivel.

## Orden de commits

1. `P0 repo` — este repositorio tal como viene en la plantilla
2. `P1 design` — `Docs/Design/`
3. `P2 art-direction` — `Docs/ArtDirection/` (ADD, Blueprint, ADI completos, referencias, mockups)
4. `P3 blockout` — un `BLK_` por cada escena `slice: true`
5. `P4 characters`
6. `P5 props`
7. `P6 environment` — tilesets, decoración, cielo, texturas compartidas
8. `P7 ui`
9. `P8 vfx`
10. `P9 audio`
11. `P10 delivery` — `LICENSES.md`, checklist verificado y tag `vX.Y.Z`

La guía completa está en la **Guía de entrega V57 para el proveedor**.

## Checklist antes del tag

- [ ] Estructura exacta de este README; nada fuera de ella salvo `Source/`.
- [ ] Sin `.meta`, `Library/`, `ProjectSettings/` ni `Packages/`.
- [ ] Cada asset tiene su `asset_brief` con los campos V57, y cada brief apunta a archivos que existen.
- [ ] Ningún `[DRAFT]` ni `[PENDING]` pendiente; IDs idénticos en todos los documentos.
- [ ] Cada entidad, pantalla, evento de sonido y nivel tiene su asset, o figura como `provisorio`.
- [ ] Modelos en metros, transformaciones aplicadas, pivote funcional, sin cámaras ni luces.
- [ ] Un mockup por pantalla y al menos una imagen de referencia por área del juego.
- [ ] `Docs/LICENSES.md` cubre todo recurso externo o generado.

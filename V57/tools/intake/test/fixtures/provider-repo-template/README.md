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
| `Docs/Design/` | `TDD.md`, `LevelMaps/<LevelId>.png`, `LevelMaps/<LevelId>/unity_scene.json` + `manifest.json` (layout exportado desde Blender, opcional), `Encounters/<LevelId>.csv` |
| `Docs/ArtDirection/` | `ArtDirectionDocument.md` (ADD completo con campos V57), `VisualProductionBlueprint.md`, `ADI/ADI_11.x_*.md`, `Reference/ref<N>_<Slug>.png`, `Concept/`, `UIMockups/<ScreenId>.png`, `Camera/<ViewId>.png`, `VFXReference/<AssetName>.mp4`, `AnimationPreviews/` |
| `Docs/Audio/` | `AudioParameters.md` (mapa de eventos y parámetros) |
| `Docs/Marketing/` | Arte de marketing (fuera del vertical slice) |
| `Docs/LICENSES.md` | Origen y licencia de cada recurso externo o generado |
| `Source/` | Opcional: archivos de trabajo (`.blend`, `.psd`, `.spp`). Unity no lo lee |
| `Assets/_Game/Art/Environment/Blockout/<LevelId>/` | `BLK_<LevelId>.fbx`: geometría del nivel con marcadores `Marker_*` (alternativa: el layout JSON de `Docs/Design/LevelMaps/<LevelId>/`) |
| `Assets/_Game/Art/Environment/Kits/<KitId>/` | Piezas modulares: `Meshes/`, `Textures/` |
| `Assets/_Game/Art/Environment/Decoration/<AssetId>/` | Adornos sin interacción: `Meshes/`, `Textures/` |
| `Assets/_Game/Art/Environment/Sky/` | Cielo o fondo 360° lat-long (`.hdr`, `.exr`, `.png`; 4096×2048 o más) |
| `Assets/_Game/Art/Props/<Category>/<AssetId>/` | Objetos jugables, uno por carpeta: `Meshes/`, `Textures/` |
| `Assets/_Game/Art/Characters/<CharacterId>/` | `Meshes/SK_*.fbx`, `Animations/ANIM_*.fbx`, `Textures/`, opcional `<CharacterId>_export.json` (clips y loops del script de export) |
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

`SM_` objeto 3D · `SK_` personaje · `ANIM_` animación · `BLK_` nivel · `T_` textura (`_BC` `_N` `_ORM` `_MS` `_E`) · `SPR_` elemento de UI (`_9s-<px>` si se estira) · `ICO_` ícono · `FNT_` fuente · `VFX_` efecto · `MUS_` `SFX_` `AMB_` `VO_` audio · dentro del FBX: `UCX_` colisión, `Socket_` punto de anclaje, `Marker_` marcador de nivel.

### Layout de nivel exportado (opcional, en lugar de `BLK_` + `Marker_*`)

`Docs/Design/LevelMaps/<LevelId>/unity_scene.json` (contrato `unity_scene/1.x`, ejes Unity Y-up en metros) con un objeto por instancia (`nombre`, `asset_id`, `capa`, `position`, `rotation` x,y,z,w, `scale`) y, opcional, `manifest.json` con los materiales de cada asset (`base_color`, `metallic`, `smoothness`, `double_sided`, rutas de texturas). Reglas:

- `asset_id` = nombre del FBX entregado (sin extensión); si un asset se reemplaza (por ejemplo un estático por su versión `SK_` animada), el JSON se vuelve a exportar con el nuevo `asset_id`.
- `capa` en inglés y PascalCase (`Structure`, `Dressing`, `Ocean`, `Gameplay`, `Events`). V57 traduce los nombres comunes en español, pero lo reporta.
- Los nombres de instancia pueden venir como `Nombre.001`; V57 los normaliza a `Nombre_01` en la escena.

### Texturas exportadas desde Blender

Se aceptan también los sufijos `_albedo`, `_normal`, `_MetallicSmoothness`, `_metallic`, `_roughness`, `_ao` y `_emission` dentro de `<Asset>/Textures/`. V57 arma el material igual y el intake sugiere el nombre `T_` canónico.

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
- [ ] FBX exportados con: Apply Scalings **FBX All**, Apply Unit, Forward **-Z**, Up **Y**, Apply Transform desactivado. `SK_`: Armature + Mesh, Only Deform Bones, sin Add Leaf Bones, sin animación. `ANIM_`: solo Armature (mismo esqueleto que el `SK_`), Bake Animation, sin NLA Strips ni All Actions (una acción por archivo). El −90° en X que Blender deja dentro del rig es esperado.
- [ ] Un mockup por pantalla y al menos una imagen de referencia por área del juego.
- [ ] `Docs/LICENSES.md` cubre todo recurso externo o generado.

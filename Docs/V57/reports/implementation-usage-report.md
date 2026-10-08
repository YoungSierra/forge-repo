# Reporte de consumo — implementación del TDD 1.0.0 en ProfesorSprat

Periodo medido: desde el inicio de la implementación (18:45:09) hasta el cierre de M4 (19:52:07), 2026-10-07, hora local.

## Tiempo (reloj de pared)

| Fase | Inicio → fin | Duración |
|---|---|---|
| Ajustes del core (prefab de juego en el layout, RebuildLevelContent) + commit | 18:45:09 → 18:45:54 | 0 min 45 s (el código del core ya estaba escrito antes de la marca) |
| I0 intake | → 18:47:35 | 1 min 41 s |
| I1 baseline del proyecto | → 18:48:12 | 0 min 37 s |
| I2 assembly | → 18:54:12 | 6 min 00 s |
| M1 código de juego: 31 scripts, compila | → 19:10:42 | 16 min 30 s |
| M1 armado de la escena + primera corrida del gold path | → 19:21:26 | 10 min 44 s |
| M1 corrección de cámara + gold path verde + specs | → 19:27:09 | 5 min 43 s |
| F tests: 13 EditMode + 28 PlayMode, todos verdes | → 19:34:37 | 7 min 28 s |
| M2 menú, lint, diff de jerarquía, colisión | → 19:41:12 | 6 min 35 s |
| M3 2 rondas de revisión independiente + correcciones | → 19:51:24 | 10 min 12 s |
| M4 reporte final | → 19:52:07 | 0 min 43 s |
| **Total** | | **≈ 67 min** |

## Tokens

| Medida | Valor | Nota |
|---|---|---|
| Contexto de la sesión principal | 549 665 → 940 258 (**+390 593**) | Tokens que entraron a la conversación: código escrito, salidas de herramientas, capturas leídas. Lo facturado es mayor porque cada turno relee el contexto (mayormente desde caché). |
| Subagentes (revisores independientes de M3) | 95 504 + 94 962 = **190 466** | Dos sesiones nuevas, una por ronda |
| **Total aproximado de tokens generados o leídos** | **≈ 581 000** | Sin contar la relectura del contexto entre turnos |
| Límite semanal (todos los modelos, plan Max) | 6 % → 8 % (**+2 puntos**) | Lectura del uso del plan al inicio y al final |
| Límite de 5 h | 28 % al inicio; la ventana se reinició durante el trabajo (3 % de la nueva al final) | No da un delta comparable |

## Qué se produjo

- **Código:** 34 scripts de juego (Core, Gameplay, GoldPath) y 10 archivos de tests.
- **Datos y UI:** 9 configs, un asset de input, 5 UXML + USS, el PanelSettings.
- **Prefabs de juego:** 11, todos variantes de los Visual prefabs.
- **Escenas:** 9 en total (menú, nivel y 7 de test).
- **Specs y gold path:** 7 specs adoptadas y un gold path de 22 pasos.
- **Estado y decisiones:** reportes por etapa y las decisiones D-013 a D-020.
- **Core V57:** 3 commits por huecos encontrados durante la implementación:
  - los prefabs de juego se colocan automáticamente desde el layout;
  - `RebuildLevelContent`;
  - un solo Visual prefab por modelo.

Limitación: el harness no expone el conteo facturado por request. Las cifras de tokens salen del tamaño del contexto, del uso reportado por cada subagente y del porcentaje del plan.

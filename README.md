# Roba y corre

Juego 3D de sigilo y humor hecho en **Unity 6000.6.6f1 (URP)**: un mapache entra de noche en casas para robar
comida y objetos, guardarlos en la salida y escapar sin que lo atrapen.

## Organización

| Carpeta | Qué contiene |
|---|---|
| [`main/`](main/) | El proyecto de Unity **en desarrollo** (la última versión). Ábrelo con Unity Hub. |
| [`versiones/`](versiones/) | Una subcarpeta por versión publicada (`v1`, `v2`, `v3`, …) con su código fuente y un `CAMBIOS.md`. |

Cada versión también tiene una etiqueta de git (`v1`, `v2`, `v3`, …).
En la PC, cada versión guarda además su ejecutable en `versiones/vN/Build/RobaYCorre.exe`.
Los ejecutables no se suben a GitHub porque pesan demasiado.

## Jugar

- **Última versión, sin Unity:** abre `versiones/v3/Build/RobaYCorre.exe` (solo en la PC).
- **Desde Unity:** en Unity Hub, agrega la carpeta `main`, ábrela con la versión 6000.6.6f1, abre `Assets/Scenes/MainMenu.unity` y pulsa Play.

Más detalles (controles, reglas, cómo modificar el juego) en [`main/README.md`](main/README.md).

# Roba y corre

Juego 3D de sigilo y humor hecho en **Unity 6000.6.6f1 (URP)**. Eres un mapache que entra de noche en casas
para robar comida y objetos, guardarlos en la salida y escapar sin que te atrapen.

## Cómo jugar desde Unity

1. Abre Unity Hub → **Add / Agregar proyecto** → elige esta carpeta (`elmapacho/main`).
2. Ábrelo con la versión **6000.6.6f1**.
3. Abre la escena `Assets/Scenes/MainMenu.unity` y pulsa **Play**.

El ejecutable de cada versión está en `../versiones/vN/Build/RobaYCorre.exe` (se genera con el menú
**Roba y corre → Compilar ejecutable para Windows**, que lo deja en `main/Build`).

## Controles

| Tecla | Acción |
|---|---|
| WASD / flechas | Moverse |
| Shift | Correr (hace mucho ruido) |
| Ctrl o C | Agacharse (silencioso, te ven desde menos lejos) |
| E | Robar / escapar por la salida |
| Q | Esconderse / salir del escondite |
| R | Soltar el último objeto |
| Esc o P | Pausa |

## Reglas

- La mochila tiene **10** de espacio; con más de 6 vas un 15 % más lento.
- Al llegar a la salida, lo que llevas se guarda automáticamente. Puedes hacer varios viajes.
- Cuando tengas el botín del nivel (y el joyero en el nivel 5), pulsa **E** en la salida para escapar.
- Barra de ruido (izquierda, más sensible desde la v2): 30 → alguien viene a mirar, 60 → te buscan activamente, si te ven te persiguen.
- Estrellas: ★ escapar con el objetivo · ★ botín ≥ 140 % del objetivo · ★ que nadie te descubra.

| Nivel | Escena | Objetivo | Vigilancia |
|---|---|---|---|
| 1 | La casa tranquila | 25 | 1 habitante dormido (tutorial) |
| 2 | La casa desordenada | 55 | habitante de sueño ligero, sillas/lámparas que se caen, jarrones y platos frágiles |
| 3 | La casa con perro | 90 | habitante + perro que patrulla, huele y ladra |
| 4 | La casa inteligente | 130 | 2 habitantes (uno patrulla), 3 cámaras, 2 sensores |
| 5 | La mansión del coleccionista | 180 + joyero | 3 habitantes, perro, 4 cámaras, 2 sensores; el joyero tiene alarma |

## Estructura del proyecto

```
Assets/
  Scenes/MainMenu.unity, Scenes/Niveles/Nivel1..5_*.unity
  Scripts/        Código del juego (ensamblado RobaYCorre)
    Core/         LevelManager, GameProgress (guardado y escenas), GameInput, AudioManager, ProceduralAudio
    Player/       RaccoonController (movimiento, ruido, robar, esconderse), RaccoonVisual (animaciones)
    AI/           Resident (habitantes), Dog, SecurityCamera, MotionSensor, HumanVisual
    Gameplay/     NoiseSystem, Lootable, HidingSpot, Bumpable, Fragile, ExitZone, Perception
    Visual/       CameraRig (cámara inclinada + paredes transparentes), WallFade, VisionCone, Flicker
    UI/           HUD, MainMenu, UIFactory
  Editor/         Generador del contenido (modelos, materiales, niveles)
    ProjectBuilder.cs  menú "Roba y corre"
    ModelLibrary.cs    todos los modelos low poly (mapache, habitantes, perro, muebles, botín...)
    LevelLayouts.cs    mapas y contenido de los 5 niveles
    LevelBuilder.cs    convierte un mapa en escena
  Prefabs/        Personajes, Muebles, Botin, Fragiles, Seguridad, Casa, Decoracion, Exterior
  Materials/      Paleta de colores, versiones transparentes, suelos con textura
  Models/Meshes/  Mallas low poly (cubo, esfera, cilindro, cono...)
  Resources/      Material de conos de visión; aquí se pueden poner sonidos (ver abajo)
  Tests/PlayMode/ Pruebas automáticas
```

## Modificar el juego

- **Valores ajustables**: selecciona un objeto en la escena y cambia sus valores en el Inspector:
  `Lootable` (valor, espacio, ruido, alarma), `RaccoonController` (velocidades, capacidad, ruido),
  `Resident` / `Dog` (vista, oído, velocidades), `SecurityCamera`, `MotionSensor`, `LevelManager` (objetivo).
- **Regenerar todo** (modelos, materiales y niveles): menú **Roba y corre → Reconstruir todo el juego**.
  Ojo: esto sobrescribe las escenas; si las editas a mano, cambia el diseño en `LevelLayouts.cs` o no lo vuelvas a ejecutar.
- **Sonido**: el juego genera sus efectos y música por código. Para usar archivos propios, crea
  `Assets/Resources/Audio/` y pon clips con el nombre del efecto (`Pickup`, `Coin`, `Bump`, `Crash`, `Spotted`,
  `Hide`, `Unhide`, `Escape`, `Caught`, `Bark`, `Alarm`, `Drop`, `Click`, `Crunch`, `Deposit`, `Full`,
  `Question`, `Wake`) o `Music` para la música.

## Pruebas automáticas

`Window → General → Test Runner → PlayMode → Run All`. Comprueban que los 5 niveles cargan, que todo el
botín, la salida, los escondites y las rutas de patrulla son alcanzables, que cada nivel se puede completar,
y que funcionan el ruido, las capturas, los escondites, el perro y las cámaras.

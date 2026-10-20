# Versión 2 — más ruido, nueva interfaz y casas reordenadas

Fecha: 2026-10-19 · etiqueta git `v2`

## Jugabilidad
- **Ruido más sensible (≈ +30 %)**: caminar 2 → 2,6/s, agachado 0,5 → 0,7/s, correr 12 → 15/s, chocar 15 → 19,
  recoger = ruido del objeto × 3,8 (antes × 3), el ruido baja más despacio (quieto −8,5/s, caminando −3,5/s)
  y los habitantes oyen un poco más lejos (15 m). Todo sigue siendo editable en el Inspector.
- **Nuevas teclas**: **Q** = esconderse / salir del escondite, **R** = soltar el último objeto, **E** = robar y escapar.
- **Nueva distribución del botín** en las 5 casas (objetos en otros muebles y habitaciones). Todo sigue siendo
  alcanzable y los objetivos se pueden cumplir; lo comprueban las pruebas automáticas.

## Interfaz (estilo "fiesta", animada)
- Letra redonda y gruesa con contorno, botones gorditos con borde, degradado y sombra.
- Animaciones con rebote: los paneles y botones "saltan" al aparecer, los botones crecen y se balancean con el ratón,
  los títulos se mueven.
- Menú nuevo: título grande animado, rayos girando de fondo y selección de niveles con tarjetas de colores.
- Carteles grandes animados: "¡HICISTE RUIDO!", "¡TE VIO!", "¡ALARMA!", "¡OBJETIVO CUMPLIDO!", "¡ATRAPADO!", "¡ESCAPASTE!".
- HUD nuevo: moneda animada, mochila con 10 casillas (se ven las lentas), barra de ruido que tiembla con mucho ruido,
  teclas de colores en las indicaciones, cartel "SALIDA" sobre la puerta.
- Pantalla de victoria con rayos de sol y estrellas que aparecen una a una con sonido.

## Gráficos
- Texturas nuevas a mayor resolución (256 px, filtrado trilineal): baldosas con bisel, madera con vetas y nudos,
  mármol con vetas, moqueta con trama, cemento, césped.
- Paredes con papel pintado que ya no se estira (UV en metros) y zócalo de madera.
- Modelos mejorados con contorno de dibujo animado: mapache (antifaz en dos manchas, ojos grandes con brillo,
  mejillas peludas, cejas, orejas con punta blanca, cola de 8 anillos), habitantes (cabeza más grande, ojos,
  mejillas, cejas, pelo más voluminoso, pantuflas con pompón, mangas) y perro (hocico, mancha en el ojo,
  orejas largas, cola con punta clara). El botín también tiene contorno.

Ejecutable (solo en la PC, no en GitHub): `versiones/v2/Build/RobaYCorre.exe`.

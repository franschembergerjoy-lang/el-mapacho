# Versión 3 — interfaz pulida, fallos gráficos arreglados y personajes nuevos

Fecha: 2026-10-22 · etiqueta git `v3`

## Interfaz
- **Sin sombra en las letras**: se quitó la sombra desplazada y el doble contorno; ahora el texto lleva un
  contorno fino y nítido.
- Paneles con sombra más suave y borde más fino; botones con animación más suave (menos rebote y balanceo).
- Ventanas (inicio, pausa, victoria, derrota) aparecen con un rebote corto en lugar de uno elástico largo.
- Menú: el fondo morado ahora es un degradado suave (antes tenía un corte recto) y los rayos de fondo son
  más grandes y sin escalones.
- HUD ordenado: el panel de consejos pasó arriba a la derecha (antes tapaba el texto de la barra de ruido) y
  los avisos son más estrechos para no taparlo.

## Fallos gráficos encontrados y arreglados
1. **Esquinas de paredes que parpadeaban** (z-fighting): en cada esquina y cruce, el muro horizontal y el
   vertical se solapaban con caras en el mismo plano, y el papel pintado, el remate y el zócalo parpadeaban.
   Ahora los muros verticales terminan justo en la cara del horizontal y sus remates/zócalos están
   ligeramente desplazados.
2. **Papel pintado con efecto muaré**: rayas finas y lunares que vibraban a distancia. Ahora son rayas anchas y suaves.
3. **Césped y moqueta "granulados"**: el ruido píxel a píxel hacía brillar la textura al moverse. Ahora usan
   ruido suave.
4. **Alfombras que parpadeaban**: sus capas estaban separadas 2 mm. Ahora se separan 1 cm.
5. **Cuadros**: el lienzo y el dibujo estaban casi pegados; se separaron para que no parpadeen.
6. **Césped demasiado cerca del suelo de la casa**: se bajó para evitar parpadeos en los bordes.
7. **Contornos sucios en los personajes**: los ojos, botones y demás piezas pequeñas tenían un borde negro
   grueso. Ahora solo las piezas grandes llevan contorno, y el grosor es igual en todas.

## Personajes
- Mallas nuevas más redondas (esfera suave y cápsula) para todos los personajes.
- **Mapache**: cuerpo y cabeza redondeados, antifaz en una pieza, ojos negros brillantes de dibujo animado,
  frente blanca, nariz con brillo, orejas con interior rosa, patas redondeadas y cola de 8 anillos.
- **Habitantes** con proporciones de dibujo animado (cabeza grande, piernas cortas, manos tipo manopla con
  pulgar), ojos con brillo, cejas, mejillas, boca o bigote, y ropa propia de cada uno:
  - Habitante: pijama azul con cuello, botones, bolsillo y puños.
  - Vecina: bata rosa con cinturón y lazo, moño.
  - Insomne: pijama verde, gafas redondas, ojeras y pelo revuelto.
  - Coleccionista: bata morada con ribetes dorados, monóculo con cadenita y bigote.
  - Mayordomo: frac con faldones, pechera blanca, pajarita roja, pelo engominado y bigote.
  - Ama de llaves: vestido verde con delantal blanco, cuello y cinta en el pelo.
- **Perro**: hocico redondo, mancha en el ojo, orejas largas, patas redondeadas, collar con medalla.

Ejecutable (solo en la PC, no en GitHub): `versiones/v3/Build/RobaYCorre.exe`.

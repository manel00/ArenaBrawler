# 💀 Biodeath Arena

**Biodeath Arena** es un prototipo local de un juego de tipo *Arena Brawler* multijugador (de 1 a 4 jugadores) desarrollado en **Unity**, con un estilo visual Low Poly y un sistema de combate *Tab-Target* inspirado en mecánicas clásicas de MMORPGs (estilo World of Warcraft).

Este repositorio contiene la Fase 1 del proyecto (el MVP local), que unifica todo el manejo del juego, las físicas y las armas dentro de Unity como base antes de añadir cualquier infraestructura de red.

---

## 🚀 Cómo ejecutar en local (Entorno de Desarrollo)

El proyecto está diseñado para probarse rápidamente como un modo juego (Local Brawler) dentro del editor.

1. Abre **Unity Hub** y asegúrate de tener una versión 2022.3+ instalada.
2. Añade este proyecto apuntando a la carpeta: `...\wow\Setup Guide In-Editor Tutorial`
3. Abre el proyecto en Unity.
4. En la ventana **Project**, navega a: `Assets/Scenes/GetStarted_Scene.unity` y ábrela.
5. Dale al botón **Play** (▶️) en la parte superior del editor para empezar.

## 🎮 Controles de Juego

El control actual del prototipo utiliza el teclado y ratón:

- **Movimiento**: `W` y `S` avanzan y retroceden. `A` y `D` rotan al personaje.
- **Cámara**: Clic derecho del ratón pulsado para orbitar la cámara.
- **Combate (Tab-Target base)**:
  - `1`: Atacar con el arma equipada.
  - `2`: Invocar unidades aliadas (Perros).
  - `3`: Activar escudo temporal.
  - `4`: Dash/Esquivar.
  - `5`: Ataque cuerpo a cuerpo rápido.
  - `Clic Izquierdo`: Cambiar de arma si hay múltiples cerca.
- **Utilidades**:
  - `Espacio`: Saltar.
  - `R`: Reiniciar la partida.

## 📌 Documentación Completa

Para conocer todos los detalles de diseño, mecánicas y objetivos del juego, consulta el archivo [GDD.md](GDD.md) incluido en este mismo directorio. Esta es la **única fuente de verdad** del diseño actual.
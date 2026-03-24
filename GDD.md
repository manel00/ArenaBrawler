# 📜 Game Design Document (GDD) - Biodeath Arena

## 1. Visión del Juego
* **Título:** Biodeath Arena
* **Género:** Arena Brawler Multijugador (1-4 Jugadores).
* **Cámara:** Tercera persona (Estilo World of Warcraft).
* **Arte:** Low Poly 3D (colores planos, sin texturas complejas, priorizando la legibilidad en combate).
* **Audiencia/Core Loop:** Los jugadores entran a la arena desde cero, recolectan armas/mejoras esparcidas por el mapa, y luchan hasta que solo quede un jugador en pie.

## 2. Sistema de Combate
El combate es **Tab-target** clásico (Estilo WoW), alejándose de los action-RPGs basados en físicas complejas.
* **Selección de Objetivos:** El jugador selecciona a su enemigo (con clic o tabulador).
* **Habilidades:** Se activan presionando números (1, 2, 3...) y se dirigen automáticamente (o afectan) al objetivo seleccionado si está a rango.
* **Global Cooldown (GCD):** Las habilidades comparten un tiempo de recarga base para evitar el "spam" y fomentar decisiones tácticas.
* **Sin colisión entre jugadores:** Evita que los personajes se atasquen o bloqueen en melé.

## 3. Progresión y Sesiones
* **Basado en Sesiones:** Cada partida es independiente. No hay inventario ni experiencia guardada en una base de datos persistente. Todos empiezan en igualdad de condiciones.
* **Economía de Arena:** Durante la partida, el mapa genera recolecciones (*pickups*) temporales como armas, salud o mejoras de daño que definen la ventaja táctica.

## 4. Estructura de la Arena
* **Zonas:** Los mapas tienen zonas de bosque profundo (para ocultar línea de visión y emboscadas) y zonas centrales descubiertas para el enfrentamiento directo.
* **Death/Respawn:** En partidas por rondas, la muerte elimina al jugador hasta la siguiente ronda. En modos de práctica/FFA, existe un sistema de respawn automático.

## 5. Fases de Desarrollo (Actualizado)
* **Fase 1 (MVP Actual):** Brawler local 100% en el cliente Unity. Físicas gestionadas localmente, combate instanciado contra IA (Bots/Perros) o multijugador en la misma máquina si se implementan inputs separados.
* **Fase 2 (Multijugador de Red):** Transición a servidor autoritativo o P2P usando la infraestructura de red decidida, enfocándose exclusivamente en replicar inputs de tab-target, posición y barras de vida a los otros 3 jugadores.
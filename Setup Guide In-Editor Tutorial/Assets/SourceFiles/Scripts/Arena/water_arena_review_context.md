# Contexto: Revisión de Diseño - Mapa Acuático ZCB-HYDRO

## Información del Proyecto
- **Juego:** Biodeath Arena: The Horde Survival
- **Género:** Action Horde Survival
- **Estilo:** Low Poly
- **Mapa a revisar:** ZCB-HYDRO (Water Arena / waterarena)

## Core Pillars del Juego
1. **Action-Survival Combat:** Free-aim combat, área control y movimiento son esenciales
2. **Brutal Bio-Mutations:** Enemigos: Monkeys, Sabrewulfs, Piranhas, Killer Bees
3. **Colossal Boss Encounters:** T-Rexes 5x tamaño normal que destruyen el entorno

## Sistema de Combate
- 3-Wave Attrition: 20 → 40 → 60 monstruos base por wave
- 3 T-Rexes colosales por wave
- Armas spawn en el suelo (10 por aliado)
- Flamethrower, rifles, shotguns
- Fireball con AoE

## Implementación Actual del Mapa Acuático (BuildHydroEnvironmentPremium)

### Layout Espacial
- **Plataforma Central:** Cilindro de 40x0.5x40 (radio ~20 unidades)
- **Islas Satélite:** 10 islas distribuidas en círculo entre radius 35-55
- **Tamaño total del arena:** Aproximadamente 150x150 unidades (con agua extendida)
- **Alturas:** Plataforma central a y=1, islas satélite a y=0

### Elementos de Layout
1. **BuildCentralMarinePlatform:**
   - Plataforma base: Cilindro 45x3x45
   - Superficie walkable: Cilindro 40x0.5x40
   - 16 rocas alrededor del borde (radius 18)
   - Torre central elevada: Cilindro 8x4x8 en y=3
   - Platforma superior de torre: 10x0.3x10 en y=5.5

2. **BuildSatelliteIslands:**
   - 10 islas en distribución circular
   - Tamaños aleatorios: 8-15 unidades de radio
   - 2-5 rocas por isla
   - Posición: radius 35-55 del centro

3. **BuildMarineTowers:**
   - 4 torres de observación en radius 28
   - Altura escalada 3x
   - Luces amarillas en la cima

4. **BuildConnectingBridges:**
   - 4 puentes conectando centro con zona media
   - Longitud: ~12 unidades cada uno
   - Soporte de pilares

5. **BuildDeepOceanFloor:**
   - Plano en y=-15
   - 20 dunas de arena distribuidas
   - Profundidad visual del agua: ~15 unidades

6. **BuildRealisticWaterSurface:**
   - Capa superficial: y=-0.5, azul brillante
   - Capa profunda: y=-3, azul oscuro
   - Escala: 15x1x15 (150x150 unidades totales)

7. **Underwater Elements:**
   - 40 algas/kelp distribuidas
   - 15 arrecifes de coral con colores variados
   - Posicionados lejos del centro (>25 unidades)

8. **Surface Details:**
   - 25 nenúfares flotantes
   - 8 boyas rojas en círculo (radius 45)

9. **Atmospheric Effects:**
   - 30 partículas flotantes
   - 6 "rayos de luz" cilíndricos desde arriba

### Materiales y Colores
- Agua superficial: (0.08, 0.25, 0.45) con smoothness 0.95
- Agua profunda: (0.04, 0.15, 0.3)
- Plataforma base: (0.25, 0.3, 0.35)
- Superficie walkable: (0.5, 0.55, 0.5)
- Arena fondo: (0.02, 0.05, 0.12)
- Corales: Rosa, cyan, morado, amarillo, verde neón

### Iluminación y Atmósfera
- Ambient Light: (0.08, 0.15, 0.25) - azul oscuro profundo
- Fog: Exponential, density 0.025, color (0.05, 0.12, 0.2)
- Emisión en corales: 0.4
- Emisión en torres: 1.0 (luces amarillas)

## Assets Utilizados
### Synty Polygon Generic:
- SM_Gen_Env_Cliff_Pillar_01, _02
- SM_Gen_Env_Fern_01, _02, _03 (algas submarinas)

### TerrainDemoScene:
- Rock_Overgrown_A, B, C

## Código Fuente
Ubicación: `Assets/SourceFiles/Scripts/Arena/ArenaEnvironmentBuilder.cs`
Método: `BuildHydroEnvironmentPremium()` (líneas ~1763-1808)
Métodos auxiliares: BuildDeepOceanFloor, BuildRealisticWaterSurface, BuildCentralMarinePlatform, BuildSatelliteIslands, BuildMarineTowers, BuildConnectingBridges, BuildUnderwaterVegetation, BuildCoralReefs, BuildSurfaceDetails, BuildMarineAtmosphere

## Preguntas para el Level Designer
1. ¿El layout actual soporta adecuadamente el combate de hordas con 20-60 enemigos + 3 T-Rexes?
2. ¿Hay suficiente espacio de movimiento y kiting para el jugador?
3. ¿Las islas satélite proporcionan opciones tácticas interesantes o son solo decorativas?
4. ¿Los puentes crean cuellos de botella problemáticos con los T-Rexes?
5. ¿La plataforma central es demasiado grande o pequeña para los encuentros?
6. ¿Se necesitan más rutas de escape o posiciones elevadas?
7. ¿La visibilidad del agua afecta la legibilidad del combate?

## Preguntas para el Art Director
1. ¿La paleta de colores azul profundo es apropiada para un juego de acción rápida?
2. ¿Los corales neón compiten visualmente con los enemigos/marcadores importantes?
3. ¿La niebla a densidad 0.025 limita demasiado la visibilidad para el gameplay?
4. ¿Hay suficiente contraste entre plataformas jugables y agua?
5. ¿Los efectos de partículas y rayos de luz añaden o restan claridad?
6. ¿El estilo low-poly se mantiene consistente con los demás mapas?
7. ¿Qué assets adicionales harían falta para mejorar la identidad visual?

# REGLAS DEL PROYECTO - Arena Brawler
# =====================================
# ESTAS REGLAS SON INQUEBRANTABLES - Consultar antes de cualquier cambio


## 1. WELCOME SCREEN / PÁGINA DE BIENVENIDA
**NUNCA** se debe eliminar, hay una flag para saltar la Welcome Screen, bajo ningún concepto eliminarla.
- Es el punto de entrada principal del juego
- Configura los parámetros esenciales (nombre del jugador, cantidad de bots, selección de mapas)
- Sin ella, el juego no puede inicializarse correctamente

## 2. HABILIDADES DEL JUGADOR
**NUNCA** se deben cambiar las habilidades base del jugador:

| Tecla | Habilidad | Comportamiento |
|-------|-----------|----------------|
| 1 | Fireball | Lanza una bola de fuego (daño directo 15-25 + AoE 5-10 en 5m) |
| 2 | Summon Dog | Invoca **1 solo perro** (límite máximo 5 perros) |
| 3 | Katana Strike | Ataque con katana - 3 golpes rápidos (tap) o 1 cargado (hold) |
| 4 | Flamethrower | Arma de fuego continuo (20m alcance, 5-25 DPS) |
| 6 | Grenade | Lanza granada con trayectoria parabólica (mantener para cargar fuerza) |

- La habilidad 2 (Summon Dog) debe crear exactamente 1 perro, máximo 5 en total
- La habilidad 6 (Grenade) usa mecánica de carga: mantener para aumentar fuerza, soltar para lanzar
- No modificar el comportamiento de las habilidades sin consultar
- No cambiar las teclas asignadas

## 3. ANTES DE CUALQUIER CAMBIO
1. **VERIFICAR CONEXIÓN MCP UNITY** - Siempre ejecutar `telemetry_ping` antes de cualquier operación MCP
2. Verificar que no se esté rompiendo la Welcome Screen
3. Confirmar que las habilidades siguen funcionando como se especifica aquí
4. Probar que la habilidad 2 invoca exactamente 1 perro
5. Probar que la habilidad 6 (Grenade) lanza correctamente con trayectoria visual

## 4. CONSECUENCIAS DE VIOLAR ESTAS REGLAS
- Ruptura del flujo del juego
- Bugs de duplicación (ej: doble perro)
- Pérdida de funcionalidad core (habilidades que no responden)
- Sistema de granada sin integración correcta
- Dificultad para debugging

## 5. CONVENCIONES DE CÓDIGO
**NUNCA** usar APIs obsoletas de Unity:
- Usar `FindAnyObjectByType<T>()` en lugar de `FindObjectOfType<T>()` o `FindFirstObjectByType<T>()`
- `FindFirstObjectByType` está obsoleto porque depende del orden de IDs de instancia
- `FindAnyObjectByType` no depende del orden y es el método recomendado

**Por qué es importante**: Los warnings de API obsoleta se acumulan, ensucian la consola y pueden romper el proyecto en futuras versiones de Unity.

## 6. EFECTOS VISUALES (VFX)
**NUNCA** crear efectos visuales usando primitivas de Unity con materiales:
- **NO** usar `GameObject.CreatePrimitive(PrimitiveType.Sphere)` para bolas de fuego
- **NO** asignar materiales a esferas u otros objetos primitivos
- **SIEMPRE** usar **prefabs de efectos visuales** (Particle Systems)

**Ejemplos de prefabs disponibles:**
- `"Prefabs/Fireball_KCISA"` - Efecto de bola de fuego KCISA
- `"KoreanTraditionalPattern_Effect/Prefabs/Fly/Fly08-04"` - Sistema de partículas de fuego
- `"KoreanTraditionalPattern_Effect/Prefabs/Fly/Fly01-01"` - Efecto alternativo

**Por qué es importante:**
- Los prefabs de Particle Systems tienen mejor rendimiento
- Mejor calidad visual con texturas y emisores de partículas
- Consistencia en el estilo visual del juego
- Fácil de modificar sin cambiar código

---
**Última actualización:** Abril 2026
**Responsable:** Mantener estas reglas visibles y consultadas

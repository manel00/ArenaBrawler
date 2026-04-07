# Fireball KCISA - Design Document

## Overview
Implementar una habilidad de bola de fuego usando el asset KCISA `Fly03-02.prefab` que sea visualmente impactante y funcione correctamente como proyectil de combate.

## Player Fantasy
El jugador lanza una bola de fuego tradicional coreana que viaja en línea recta, impacta con enemigos y explota al contacto.

## Core Mechanics

### 1. Spawn Position
- **Location**: Frente al personaje, a 1.5m de distancia
- **Height**: 1.0m desde el suelo (altura de pecho del personaje)
- **Visual**: El fireball debe aparecer claramente saliendo del personaje

### 2. Movement
- **Direction**: Línea recta hacia donde mira el personaje
- **Speed**: 15 m/s
- **Lifetime**: 4 segundos máximo
- **Gravity**: No, vuela en línea recta

### 3. Collision & Damage
- **Collider**: SphereCollider (trigger) de radio 0.5m
- **Damage**: 35 puntos al impactar enemigo
- **Impact**: El fireball se destruye al tocar cualquier cosa
- **Friendly Fire**: No, no daña aliados

### 4. Visual Requirements
- **Prefab**: `Assets/KoreanTraditionalPattern_Effect/Prefabs/Fly/Fly03-02.prefab`
- **Scale**: 0.5x (más pequeño que el original)
- **Color**: Naranja/rojo intenso (ya incluido en el prefab)
- **Trail**: Debe dejar estela de partículas

## Technical Specifications

### Input
- **Key**: Tecla "1" o Numpad 1
- **Cooldown**: 2 segundos
- **Stamina Cost**: 0 (usamos mana en futuro)

### Code Structure
```csharp
// PlayerController.cs
CastFireball() -> SpawnKcisaFireball()

// MagicProjectile.cs (ya existe)
- Initialize(direction, caster, damage)
- OnTriggerEnter() para daño
- DestroyOnImpact()
```

### Integration Points
- PlayerController.TryCastAbility(1) → CastFireball()
- ArenaCombatant.TakeDamage() para aplicar daño
- ArenaHUD para mostrar cooldown

## Edge Cases
1. **No enemies**: Fireball viaja hasta max range o hit wall
2. **Ally in path**: Colisiona pero no daña (friendly fire off)
3. **Spawn inside wall**: Se destruye inmediatamente
4. **Caster dies**: Fireball continúa (no depende de caster)

## Tuning Knobs
- `fireballSpeed`: 10-20 m/s
- `fireballDamage`: 20-50 HP
- `fireballScale`: 0.3x - 1.0x
- `spawnHeight`: 0.5m - 1.5m

## Acceptance Criteria
- [ ] Fireball aparece frente al personaje (no flotando arriba)
- [ ] Viaja en línea recta hacia donde mira el jugador
- [ ] Detecta colisión con enemigos
- [ ] Aplica 35 daño al impactar
- [ ] Se destruye al impactar (no atraviesa enemigos)
- [ ] Efecto visual KCISA es visible y reconocible

## Dependencies
- MagicProjectile.cs (ya implementado)
- PlayerController.cs (modificar CastFireball)
- KCISA prefab Fly03-02

## Risks & Mitigation
- **Risk**: Prefab KCISA tiene Y position muy alto → Mitigación: Ajustar spawn offset
- **Risk**: ParticleSystem no tiene collider → Mitigación: Agregar SphereCollider en runtime
- **Risk**: No hace daño → Mitigación: Verificar OnTriggerEnter en MagicProjectile

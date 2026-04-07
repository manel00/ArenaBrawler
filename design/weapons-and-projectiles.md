# Mecánicas de Armas y Proyectiles

## Resumen

Documentación de las mecánicas de armas y proyectiles del juego, incluyendo comportamientos especiales y limitaciones técnicas.

---

## Fireballs

### Comportamiento
- **Ilimitados:** Los fireballs no tienen límite de uso
- **Pool de objetos:** Usan sistema de pooling para optimizar rendimiento
- **Retorno automático:** Los fireballs retornan al pool tras impactar o expirar

### Pool Configuration
```
Pool: "Fireball"
- Tamaño inicial: 10
- Máximo: 20 (expandible)
- Comportamiento: Crecimiento automático si se agota
```

### Notas Técnicas
- El pool puede alcanzar el límite máximo si los fireballs no retornan correctamente
- Se implementó `ReturnToPoolOrDestroy()` para garantizar retorno al pool
- Si el pool está agotado, se instancian fireballs nuevos (fallback)

---

## Armas de Fuego (Firearms)

### Sistema de Munición
- Las armas pueden requerir munición (`UsesAmmo`)
- Pickups de armas en suelo otorgan munición inicial
- Sin munición = no se puede disparar

### Tipos de Armas

#### Pistola
- Daño: Moderado
- Cadencia: Media
- Munición: Sí

#### Rifle de Asalto
- Daño: Alto
- Cadencia: Alta
- Munición: Sí

#### Escopeta
- Daño: Muy alto (por perdigón)
- Cadencia: Baja
- Efecto especial: Knockback
- Munición: Sí

#### Lanzallamas
- Daño: Continuo
- Tipo de proyectil: Llama
- Munición: Sí (consumo rápido)

---

## Katana (Arma Cuerpo a Cuerpo)

### Características
- Sin munición
- Daño instantáneo en área frontal
- Cooldown entre ataques
- Efecto visual de slash

---

## Proyectiles de Armas

### WeaponProjectile
- Sistema de pooling similar a fireballs
- Lifetime: 4 segundos máximo
- Colisión: Trigger-based
- Retorno automático al pool tras impacto o timeout

### Daño de Área (Splash)
Algunas armas (escopeta, explosivos) aplican daño en área:
- Radio configurable por arma
- Daño directo + daño de splash
- Friendly fire: No (excepto con entorno)

---

## Notas de Balance

### Fireballs Ilimitados
- **Razón de diseño:** Habilidad básica del jugador, debe estar siempre disponible
- **Balance:** Daño menor que armas de fuego pero acceso constante
- **Riesgo:** Puede hacer que el jugador ignore otras armas si el daño es demasiado alto

### Recomendaciones
1. Mantener daño de fireball inferior a armas de fuego de nivel equivalente
2. Cooldown entre lanzamientos para evitar spam
3. Velocidad de proyectil más lenta que balas (skill shot)

---

## Cross-References
- Implementación: `Scripts/Arena/FireballProjectile.cs`
- Pool system: `Scripts/Arena/Managers/GenericObjectPool.cs`
- Spawning: `Scripts/Arena/CombatLogic.cs` (RuntimeSpawner)

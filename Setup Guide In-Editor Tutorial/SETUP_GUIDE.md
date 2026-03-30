# Arena Brawler - Complete Setup Guide

## Overview
This guide covers all systems implemented in the Arena Brawler project.

## Table of Contents
1. [Quick Start](#quick-start)
2. [Core Systems](#core-systems)
3. [Input System](#input-system)
4. [Combat System](#combat-system)
5. [Weapon System](#weapon-system)
6. [Ability System](#ability-system)
7. [UI/HUD System](#uihud-system)
8. [Visual Effects](#visual-effects)
9. [Audio System](#audio-system)
10. [Performance Optimization](#performance-optimization)

## Quick Start

### Required Scene Setup
1. Create empty GameObject named "ArenaBootstrap"
   - Add `ArenaBootstrap.cs` component
   
2. Create empty GameObject named "InputManager"
   - Add `InputManager.cs` component
   
3. Create empty GameObject named "GameManagers"
   - Add `GenericObjectPool.cs` component
   - Configure pools in inspector

4. (Optional) Create empty GameObject named "VisualEffectsManager"
   - Add `VisualEffectsManager.cs` component

## Core Systems

### ArenaBootstrap
**File**: `ArenaBootstrap.cs`

Purpose: Initializes the entire arena match including:
- Player spawn
- Allied bot spawn
- Environment generation
- Weapon placement
- HUD setup
- Horde wave manager setup

### ArenaGameManager
**File**: `ArenaGameManager.cs`

Purpose: Manages game state, wave progression, and game over conditions.

## Input System

### InputManager
**File**: `Managers/InputManager.cs`

Centralized input handling with event-based architecture.

**Setup**:
1. Add InputManager GameObject to scene
2. All input is automatically handled

**Available Events**:
```csharp
InputManager.OnJumpPressed += YourJumpMethod;
InputManager.OnDashPressed += YourDashMethod;
InputManager.OnAbilityPressed += YourAbilityMethod;
InputManager.OnKatanaAttackPressed += YourKatanaMethod;
```

**Key Mappings**:
| Key | Event | Purpose |
|-----|-------|---------|
| 1 | OnAbilityPressed(1) | Fireball |
| 2 | OnAbilityPressed(2) | Summon Dog |
| 4 | OnWeaponAttackPressed | Weapon Attack |
| 5 | OnKatanaAttackPressed | Katana/Melee |
| K | OnKatanaEquipToggle | Toggle Katana |
| Q | OnDropWeaponPressed | Drop Weapon |
| E | OnPickUpWeaponPressed | Pick Up Weapon |
| F | OnDashPressed | Dash |
| Space | OnJumpPressed | Jump |

## Combat System

### ArenaCombatant
**File**: `ArenaCombatant.cs`

Base class for all combat entities. Features:
- Health management
- Team system (no friendly fire)
- Damage events
- Knockback support
- Death callbacks

### Key Properties:
```csharp
public float hp;              // Current health
public float maxHp;           // Maximum health
public int teamId;            // Team identifier (0=none, 1=player, 2=enemies)
public bool isPlayer;         // Is this the player character
public float damageMultiplier; // Damage output modifier
```

## Weapon System

### PlayerWeaponSystem
**File**: `Weapons/PlayerWeaponSystem.cs`

Manages weapon pickup, holding, and firing.

### WeaponData
**File**: `WeaponData.cs`

ScriptableObject for weapon configuration:
```csharp
public string weaponName;
public WeaponType weaponType;
public float minDamage;
public float maxDamage;
public float range;
public float cooldown;
public int maxAmmo;
public bool infiniteAmmo;
```

### WeaponFactory
**File**: `WeaponFactory.cs`

Creates weapons programmatically. Usage:
```csharp
WeaponData[] weapons = WeaponFactory.GetWeapons();
WeaponData rifle = WeaponFactory.FindWeapon("Assault Rifle");
```

## Ability System

### AbilitySystem
**File**: `Abilities/AbilitySystem.cs`

Generic ability management with cooldowns.

### AbilityData
**File**: `Abilities/AbilityData.cs`

Base class for creating abilities:
1. Right-click in Project > Create > Arena > Ability
2. Configure in inspector:
   - Ability Name
   - Key Binding (1-9)
   - Cooldown
   - Damage
   - VFX Prefab

### Built-in Abilities
- **Fireball** (Key 1): Ranged projectile
- **Summon Dog** (Key 2): Ally spawn
- **Katana** (Key 5): Melee weapon system

## UI/HUD System

### ArenaHUD
**File**: `UI/ArenaHUD.cs`

Main HUD with health bar, stamina bar, and wave info.

### StylishDamageNumbers
**File**: `UI/StylishDamageNumbers.cs`

Floating damage numbers with:
- Color coding (normal, critical, healing)
- Animation curves
- Pooling for performance

### EnhancedDamageNumbers
**File**: `UI/EnhancedDamageNumbers.cs`

Premium version with:
- Multiple damage types
- Critical hit effects
- Shake animations
- World-space canvas

## Visual Effects

### VisualEffectsManager
**File**: `VisualEffectsManager.cs`

Premium VFX system with:
- Camera shake
- Screen flash
- Slow motion
- Hit stop

Usage:
```csharp
VisualEffectsManager.Instance.CameraShake(0.5f, 0.3f);
VisualEffectsManager.Instance.ScreenFlash(Color.red);
VisualEffectsManager.Instance.TriggerSlowMotion();
```

### VFXManagerPooled
**File**: `Managers/VFXManagerPooled.cs`

Object-pooled VFX for performance.

### ParticleEffectController
**File**: `ParticleEffectController.cs`

Component for pooled particle systems.

## Audio System

### ArenaAudioManager
**File**: `Managers/ArenaAudioManager.cs`

**NOTE**: Audio is currently disabled.

To enable, set:
```csharp
ArenaAudioManager.AudioEnabled = true;
```

Usage:
```csharp
ArenaAudioManager.PlaySound(clip, volume);
ArenaAudioManager.PlaySoundAtPosition(clip, position, volume);
```

## Performance Optimization

### Implemented Optimizations

1. **Object Pooling**
   - Projectiles reuse objects
   - VFX uses pooling
   - Damage numbers pooled

2. **Input Centralization**
   - Single input check per frame
   - Event-based architecture

3. **Raycast Buffers**
   - Pre-allocated arrays
   - NonAlloc methods

4. **Debug Stripping**
   - All Debug.Log wrapped in `#if DEBUG`

5. **Culling**
   - Environmental interactions culled by distance
   - Max updates per frame limited

### Pool Configuration

Required pools for GenericObjectPool:
- `FireballProjectile`
- `WeaponProjectile`
- `ImpactEffect`
- `DeathEffect`
- `ShieldEffect`
- `DashEffect`
- `MeleeEffect`
- `DamageNumbers` (optional)

## ScriptableObject Creation

### Creating Abilities
1. Right-click Project window
2. Create > Arena > Ability
3. Configure properties
4. Assign to AbilitySystem component

### Creating Game Balance Config
1. Right-click Project window
2. Create > Arena > Game Balance Config
3. Adjust all balance values
4. Reference in game manager

## Debugging

### Enable Debug Logs
Add `DEBUG` to Scripting Define Symbols:
1. Edit > Project Settings > Player
2. Other Settings > Scripting Define Symbols
3. Add: `DEBUG`

### Key Debug Commands
- Press `K` to toggle katana
- Press `5` for katana attack
- Press `1` for fireball
- Press `2` for summon dog

## Common Issues

### Issue: Input not working
**Solution**: Ensure InputManager GameObject exists in scene

### Issue: No damage numbers showing
**Solution**: Check that StylishDamageNumbers GameObject exists with Canvas

### Issue: Weapons not spawning
**Solution**: Verify WeaponFactory and ArenaBootstrap configuration

### Issue: Audio not playing
**Solution**: Enable audio with `ArenaAudioManager.AudioEnabled = true`

## File Structure
```
Assets/SourceFiles/Scripts/
├── Arena/
│   ├── Abilities/
│   │   ├── AbilityData.cs
│   │   └── AbilitySystem.cs
│   ├── Environment/
│   │   ├── InteractiveGrass.cs
│   │   ├── InteractiveWater.cs
│   │   └── EnvironmentalInteractionManager.cs
│   ├── Managers/
│   │   ├── ArenaAudioManager.cs
│   │   ├── ArenaGameManager.cs
│   │   ├── GenericObjectPool.cs
│   │   ├── InputManager.cs
│   │   ├── VFXManager.cs
│   │   └── VFXManagerPooled.cs
│   └── UI/
│       ├── ArenaHUD.cs
│       ├── StylishDamageNumbers.cs
│       └── EnhancedDamageNumbers.cs
└── Weapons/
    ├── PlayerWeaponSystem.cs
    ├── WeaponData.cs
    └── WeaponFactory.cs
```

## Support
For issues or questions, refer to the code comments or create a ticket.

## Changelog
- v1.0: Initial implementation
- v1.1: Added InputManager
- v1.2: Added AbilitySystem
- v1.3: Added WeaponFactory
- v1.4: Added VisualEffectsManager
- v1.5: Audio disabled by default

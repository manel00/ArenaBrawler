# CHANGELOG — Biodeath Arena

All notable changes to this project are documented here.

---

## [Unreleased] — 2026-03-30

### Added
- **Flamethrower Weapon System** — Massive 30m range fire weapon:
  - `FlamethrowerVFXController.cs` — Premium particle system with 800 particles/sec, cone shape 30° angle
  - `FlamethrowerDamageZone.cs` — Continuous damage zone with burn DOT effects
  - Color over lifetime: Yellow → White → Orange → Red → Smoke
  - 25-50 DPS with distance-based damage falloff
- **CameraCache.cs** — Global camera caching system to eliminate Camera.main search overhead
- **WeaponFactoryConfig.cs** — ScriptableObject for centralized weapon balance configuration
- **CameraCache.cs** — Performance utility to cache Camera.main and eliminate per-frame lookups
- **EnhancedDamageNumbers.cs** — Premium floating damage numbers with critical hit effects
- **VisualEffectsManager.cs** — Camera shake, screen flash, slow motion, and hit stop effects
- **ParticleEffectController.cs** — Component for pooled particle lifecycle management
- **GameBalanceConfig.cs** — Centralized balance configuration ScriptableObject
- **ActualDPS and AverageDamage properties** to `WeaponData.cs` for auto-calculated stats

### Changed
- **WeaponData.cs**:
  - Changed namespace from `WoW.Armas` to `ArenaEnhanced`
  - Added `Rifle` and `Shotgun` to `WeaponType` enum
  - Added `groundScale` and `handScale` properties
  - Renamed `range` → `attackRange`, `cooldown` → `attackCooldown`
- **WeaponFactory.cs**: 
  - Updated to use `WeaponFactoryConfig` for all weapon stats
  - Fixed property names (`weaponType` → `type`, `Single` → `Projectile`)
  - Flamethrower now has 30m range, 25-50 DPS
- **PlayerWeaponSystem.cs**: Added throttle to `CheckForNearbyWeapons()` (60fps → ~6-7fps checks)
- **ThirdPersonController.cs**: Removed redundant `TryGetComponent` call from Update (now only in Start)
- **WorldHPBar.cs** & **DamagePopup.cs**: Now use `CameraCache.Main` instead of `Camera.main`
- **WeaponPickup.cs**: Added shader caching to avoid repeated `Shader.Find()` calls
- **InputManager.cs** & **PlayerController.cs**: Added `UnityEngine.InputSystem` using directive
- **EnvironmentalInteractionManager.cs**: Updated to use non-deprecated `FindObjectsByType` overloads
- **StylishDamageNumbers.cs** & **MinimapSystem.cs**: Replaced deprecated `FindFirstObjectByType` with `FindAnyObjectByType`
- **ArenaAudioManager.cs**: Added stub methods `PlayFireball()`, `PlayPickup()`, `PlayMelee()` for compatibility
- **FlamethrowerVFXController.cs**: Removed non-existent `emitFrom` property
- **FlamethrowerDamageZone.cs**: Fixed `VFXManagerPooled` call (removed incorrect `.Instance` access)
- **KatanaWeapon.cs**: Removed duplicate `IsEquipped` property
- **EnhancedDamageNumbers.cs**: Fixed to use `TextMeshProUGUI` directly instead of non-existent `DamageNumberVisual`
- **StylishDamageNumbers.cs**: Fixed pool type (`List<DamageNumberInstance>` → `Queue<GameObject>`)

### Fixed
- **Compilation Errors**:
  - `CS0246`: WoW namespace not found → Changed to ArenaEnhanced
  - `CS0117`: ArenaAudioManager missing methods → Added stub methods
  - `CS1061`: WeaponData property names → Fixed all references
  - `CS0103`: Keyboard not found → Added InputSystem using directive
  - `CS0101`: Duplicate ArenaAudioManager class → Removed duplicate file
  - `CS0101`: Duplicate DamageType enum → Removed from StylishDamageNumbers
  - `CS0102`: Duplicate IsEquipped property → Removed from KatanaWeapon
  - `CS0308`: Queue<T> not found → Added Collections.Generic using
  - `CS0618`: Obsolete FindFirstObjectByType → Updated to FindAnyObjectByType
- **FlamethrowerDamageZone.cs**: Added null checks for target transform

### Fixed (Warnings)
- **CS0618**: Removed obsolete `FindObjectsSortMode` parameter from `EnvironmentalInteractionManager.cs`
- **CS0414**: Removed unused fields to eliminate warnings:
  - `StylishDamageNumbers._useGenericPool`
  - `FlamethrowerVFXController.heatIntensity`
  - `ParticleEffectController.scaleWithParent`
  - `EnhancedDamageNumbers.criticalScale`
  - `FlamethrowerDamageZone.damageRadius`

### Removed
- Duplicate `ArenaAudioManager.cs` file in `Arena/` folder (kept version in `Managers/`)
- Duplicate `DamageType` enum from `StylishDamageNumbers.cs`
- Duplicate `IsEquipped` property from `KatanaWeapon.cs`

---

## [Unreleased] — 2026-03-27

### Added
- **Katana (Ice Sword) weapon** — `KatanaWeapon.cs`:
  - **K** → equip / unequip the `ice_sword_by_get3dmodels` katana
  - **5 (tap)** → 5-hit rapid samurai combo (18 dmg per hit, 35° arc)
  - **5 (hold ≥ 0.45s)** → charged wide slash (90 dmg, 60° arc, ice knockback)
  - Visual simulation: `TrailRenderer` blade trail + `LineRenderer` slash arc + camera shake
  - **Ajustado tamaño**: Reducido al 15% del tamaño original para proporciones realistas
  - Automatically attached to the player on arena start
- **`CHANGELOG.md`** — created at project root to track all future changes

### Fixed
- **HUD not visible**: Completely rewrote `ArenaHUD.cs`. Root cause was that panels used plain `Transform` instead of `RectTransform`, so Unity's Canvas couldn't render any children. Now all elements (health bar, stamina bar, wave counter, points) are built procedurally with correct `RectTransform` anchors inside a `ScreenSpaceOverlay` Canvas.
- **`ArenaHUD` warning**: Fixed `warning CS0414` by removing the unused `maxPointsForFullBar` field.
- **Player stuck after 'R' key respawn**: Fixed `ArenaCombatant.Respawn()` — it now disables the `CharacterController` before teleporting and re-enables it afterward. Also re-enables `PlayerController` script if it was disabled.
- **Game-over / Victory screen not shown**: Added a full-screen dark overlay panel (`GameOverHUD_Panel`) to `ArenaHUD` that polls `ArenaGameManager.Instance.ended` each frame and displays the appropriate message (DERROTADO / BIODEATH CONQUISTADO).
- **Mesh Collider warnings on HP bars**: `WorldHPBar.cs` now manually creates `MeshFilter + MeshRenderer` without `MeshCollider`, eliminating physics warnings.
- **Component dependency errors on fighter spawn**: `ArenaBootstrap.SpawnFighter` now removes `MonoBehaviour` scripts in a first pass and engine components in a second pass to respect Unity's `[RequireComponent]` dependency order.

### Added
- `Assets/Resources/Prefabs/UI/ArenaHUD.prefab` — persistent HUD prefab loaded via `Resources.Load` at runtime.
- `project_manifest.md` — catalog of all project scenes, scripts, and systems to prevent accidental future data loss.
- Respawn now teleports the player to a random position within the central white square (15m radius).

---

## How to contribute entries

When making changes, add a new entry under `[Unreleased]` using these categories:
- **Added** — new features or files
- **Changed** — changes in existing functionality
- **Fixed** — bug fixes
- **Removed** — removed features or files
- **Deprecated** — features scheduled for removal

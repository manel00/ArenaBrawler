# CHANGELOG — Biodeath Arena

All notable changes to this project are documented here.

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

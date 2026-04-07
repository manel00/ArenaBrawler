using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ArenaEnhanced
{
    /// <summary>
    /// Módulo dedicado al spawneo de armas en el suelo
    /// </summary>
    public static class ArenaWeaponSpawner
    {
        private const int WeaponsPerAlliedCombatant = 10;
        private static WeaponData[] _runtimeWeapons;

        public static void SpawnWeaponsOnFloor(IReadOnlyList<ArenaCombatant> alliedCombatants)
        {
#if UNITY_EDITOR
            if (alliedCombatants == null || alliedCombatants.Count == 0) return;

            var existing = GameObject.Find("GroundWeapons");
            if (existing != null) Object.Destroy(existing);

            var weaponsParent = new GameObject("GroundWeapons").transform;
            WeaponData[] availableWeapons = GetRuntimeWeapons();
            int totalWeapons = alliedCombatants.Count * WeaponsPerAlliedCombatant;

            for (int i = 0; i < totalWeapons; i++)
            {
                WeaponData selected = availableWeapons[i % availableWeapons.Length];
                Vector3 spawnPos = GetWeaponSpawnPosition(i, totalWeapons);
                var pickup = WeaponPickup.CreatePickup(selected, spawnPos, selected.DefaultAmmo);
                if (pickup == null)
                {
                    Debug.LogWarning($"[ArenaWeaponSpawner] Failed to create weapon pickup for {selected?.weaponName ?? "unknown"}");
                    continue;
                }
                pickup.transform.SetParent(weaponsParent);
            }

            Debug.Log($"[ArenaWeaponSpawner] Spawned {totalWeapons} weapons for {alliedCombatants.Count} allied combatants.");
#else
            // RUNTIME FALLBACK: En builds standalone no spawneamos armas complejas
            // El sistema de armas melee/katana sigue funcionando
            Debug.Log("[ArenaWeaponSpawner] Weapon spawning disabled in runtime builds - using default weapons only.");
#endif
        }

        private static Vector3 GetWeaponSpawnPosition(int index, int totalWeapons)
        {
            float angle = (Mathf.PI * 2f / Mathf.Max(1, totalWeapons)) * index;
            float radius = Mathf.Lerp(10f, 32f, Random.value);
            Vector3 pos = new Vector3(Mathf.Cos(angle), 8f, Mathf.Sin(angle)) * radius;
            pos += new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-2.5f, 2.5f));

            if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Ignore))
            {
                pos = hit.point + Vector3.up * 0.65f;
            }
            else
            {
                pos.y = 0.65f;
            }

            return pos;
        }

#if UNITY_EDITOR
        public static WeaponData[] GetRuntimeWeapons()
        {
            if (_runtimeWeapons != null && _runtimeWeapons.Length > 0) return _runtimeWeapons;

            GameObject assaultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Weapons/AssaultRifle_01.obj");
            GameObject shotgunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Weapons/Double Barrel Shotgun/ShortDoubleBarrel.fbx");
            GameObject flamethrowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Weapons/Pistola agua/model.obj");

            Material rifleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Simple_BlueDark.mat");
            Material shotgunMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_GoldGlow.mat");
            Material flamethrowerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Simple_Orange.mat");
            GameObject flameVFX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/FX/FX_Fire_01.prefab");

            _runtimeWeapons = new[]
            {
                CreateRuntimeWeapon(
                    "Assault Rifle",
                    WeaponType.Ranged,
                    WeaponFireMode.Projectile,
                    assaultPrefab,
                    rifleMat,
                    null,
                    new Color(0.25f, 0.55f, 1f),
                    new Vector3(2f, 2f, 2f),
                    180f,
                    10f, 25f, 26f, 0.01f, 20, true, 1, 1.5f, 40f, 0f, 0f, 0f, 0f, 0f),
                CreateRuntimeWeapon(
                    "Shotgun",
                    WeaponType.Ranged,
                    WeaponFireMode.Projectile,
                    shotgunPrefab,
                    shotgunMat,
                    null,
                    new Color(1f, 0.82f, 0.35f),
                    new Vector3(0.5f, 0.5f, 0.5f),
                    180f,
                    10f, 25f, 18f, 0.9f, 20, true, 8, 14f, 35f, 0f, 0f, 0f, 0f, 0f),
                CreateRuntimeWeapon(
                    "Flamethrower",
                    WeaponType.Flamethrower,
                    WeaponFireMode.Continuous,
                    flamethrowerPrefab,
                    flamethrowerMat,
                    null,
                    new Color(1f, 0.35f, 0.1f),
                    new Vector3(12f, 12f, 12f),
                    180f,
                    0f, 0f, 20f, 0.1f, 0, true, 1, 30f, 0f, 0f, 0f, 0f, 5f, 25f, flameVFX)
            };

            return _runtimeWeapons;
        }

        private static WeaponData CreateRuntimeWeapon(
            string weaponName, WeaponType type, WeaponFireMode fireMode,
            GameObject prefab, Material material, Texture2D texture, Color color, Vector3 scale, float rotationY,
            float minDamage, float maxDamage, float range, float cooldown, int maxAmmo, bool infiniteAmmo,
            int projectilesPerShot, float spreadAngle, float projectileSpeed,
            float splashRadius, float splashMin, float splashMax, float minDps, float maxDps, GameObject vfx = null)
        {
            var data = ScriptableObject.CreateInstance<WeaponData>();
            data.weaponName = weaponName;
            data.type = type;
            data.fireMode = fireMode;
            data.prefab = prefab;
            data.weaponMaterial = material;
            data.weaponTexture = texture;
            data.weaponColor = color;
            data.weaponScale = scale;
            data.rotationOffset = new Vector3(0, rotationY, 0);
            data.minDamage = minDamage;
            data.maxDamage = maxDamage;
            data.attackRange = range;
            data.attackCooldown = cooldown;
            data.maxAmmo = maxAmmo;
            data.infiniteAmmo = infiniteAmmo;
            data.projectilesPerShot = projectilesPerShot;
            data.spreadAngle = spreadAngle;
            data.projectileSpeed = projectileSpeed;
            data.splashRadius = splashRadius;
            data.splashMinDamage = splashMin;
            data.splashMaxDamage = splashMax;
            data.minDamagePerSecond = minDps;
            data.maxDamagePerSecond = maxDps;
            data.attackVFX = vfx;
            return data;
        }
#endif
    }
}

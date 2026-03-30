using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// ScriptableObject que define las propiedades de cada tipo de arma
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "ArenaEnhanced/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Información")]
        public string weaponName = "Arma";
        public WeaponType type = WeaponType.Ranged;
        public WeaponFireMode fireMode = WeaponFireMode.Projectile;
        public GameObject prefab;
        public Material weaponMaterial;
        public Texture2D weaponTexture;
        
        [Header("Estadísticas Base")]
        public float minDamage = 1f;
        public float maxDamage = 1f;
        public float attackRange = 20f;
        public float attackCooldown = 0.5f;
        public float projectileSpeed = 40f;
        public int maxAmmo = 20;
        public bool infiniteAmmo = false;

        [Header("Modificadores de Disparo")]
        public int projectilesPerShot = 1;
        public float spreadAngle = 0f;

        [Header("Daño Continuo / Área")]
        public float minDamagePerSecond = 1f;
        public float maxDamagePerSecond = 1f;
        public float splashRadius = 0f;
        public float splashMinDamage = 0f;
        public float splashMaxDamage = 0f;
        
        [Header("Apariencia")]
        public Color weaponColor = Color.gray;
        public Vector3 weaponScale = Vector3.one;
        public Vector3 groundScale = Vector3.one * 3f;
        public Vector3 handScale = Vector3.one;
        public Vector3 rotationOffset = Vector3.zero;
        
        [Header("VFX")]
        public GameObject attackVFX;
        public AudioClip attackSound;

        public bool UsesAmmo => !infiniteAmmo;
        public int DefaultAmmo => infiniteAmmo ? -1 : Mathf.Max(0, maxAmmo);

        /// <summary>
        /// Daño por segundo actual (auto-calculado)
        /// </summary>
        public float ActualDPS => RollDamagePerSecond();

        /// <summary>
        /// Daño promedio por disparo
        /// </summary>
        public float AverageDamage => (minDamage + maxDamage) / 2f;

        public float RollDamage()
        {
            return 0.5f; // Daño fijo de 0.5 por disparo
        }

        public float RollSplashDamage()
        {
            return 0.5f; // Daño fijo de 0.5 por splash
        }

        public float RollDamagePerSecond()
        {
            return 0.5f; // Daño fijo de 0.5 por segundo
        }
    }
    
    public enum WeaponType
    {
        Melee,
        Ranged,
        Thrown,
        Flamethrower,
        Rifle,
        Shotgun
    }

    public enum WeaponFireMode
    {
        Projectile,
        Hitscan,
        Continuous
    }
}
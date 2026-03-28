using UnityEngine;

namespace ArenaEnhanced.Configs
{
    /// <summary>
    /// Configuración para armas - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "Arena/Weapon Config")]
    public class WeaponConfig : ScriptableObject
    {
        [Header("General")]
        public string weaponName = "Weapon";
        public string description = "";
        public GameObject prefab;
        public Sprite icon;
        
        [Header("Stats")]
        public float damage = 25f;
        public float attackSpeed = 1f;
        public float range = 10f;
        public float projectileSpeed = 35f;
        
        [Header("Ammo")]
        public int maxAmmo = 20;
        public bool infiniteAmmo = false;
        public float reloadTime = 1.5f;
        
        [Header("Effects")]
        public GameObject muzzleFlashEffect;
        public GameObject impactEffect;
        public GameObject trailEffect;
        
        [Header("Audio")]
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public AudioClip emptySound;
        
        [Header("Visual")]
        public Color weaponColor = Color.white;
        public float scale = 1f;
        
        [Header("Special")]
        public bool isExplosive = false;
        public float explosionRadius = 3f;
        public float explosionDamage = 50f;
        
        [Header("Drop")]
        public int dropPriority = 0;
        [Range(0f, 1f)]
        public float dropChance = 0.5f;
        
        /// <summary>
        /// Valida que la configuración sea correcta
        /// </summary>
        public bool Validate()
        {
            if (damage <= 0)
            {
                Debug.LogError($"[WeaponConfig] {weaponName}: damage must be > 0");
                return false;
            }
            
            if (attackSpeed <= 0)
            {
                Debug.LogError($"[WeaponConfig] {weaponName}: attackSpeed must be > 0");
                return false;
            }
            
            if (range <= 0)
            {
                Debug.LogError($"[WeaponConfig] {weaponName}: range must be > 0");
                return false;
            }
            
            if (maxAmmo <= 0 && !infiniteAmmo)
            {
                Debug.LogError($"[WeaponConfig] {weaponName}: maxAmmo must be > 0 or infiniteAmmo must be true");
                return false;
            }
            
            return true;
        }
    }
}
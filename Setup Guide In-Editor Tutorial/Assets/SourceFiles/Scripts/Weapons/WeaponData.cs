using UnityEngine;

namespace WoW.Armas
{
    /// <summary>
    /// ScriptableObject que define las propiedades de cada tipo de arma
    /// </summary>
    [CreateAssetMenu(fileName = "NuevaArma", menuName = "WoW/Datos de Arma")]
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
        public float minDamage = 10f;
        public float maxDamage = 25f;
        public float attackRange = 20f;
        public float attackCooldown = 0.5f;
        public float projectileSpeed = 40f;
        public int maxAmmo = 20;
        public bool infiniteAmmo = false;

        [Header("Modificadores de Disparo")]
        public int projectilesPerShot = 1;
        public float spreadAngle = 0f;

        [Header("Daño Continuo / Área")]
        public float minDamagePerSecond = 5f;
        public float maxDamagePerSecond = 25f;
        public float splashRadius = 0f;
        public float splashMinDamage = 0f;
        public float splashMaxDamage = 0f;
        
        [Header("Apariencia")]
        public Color weaponColor = Color.gray;
        public Vector3 weaponScale = Vector3.one;
        public Vector3 rotationOffset = Vector3.zero;
        
        [Header("VFX")]
        public GameObject attackVFX;
        public AudioClip attackSound;

        public bool UsesAmmo => !infiniteAmmo;
        public int DefaultAmmo => infiniteAmmo ? -1 : Mathf.Max(0, maxAmmo);

        public float RollDamage()
        {
            return Random.Range(minDamage, maxDamage);
        }

        public float RollSplashDamage()
        {
            return Random.Range(splashMinDamage, splashMaxDamage);
        }

        public float RollDamagePerSecond()
        {
            return Random.Range(minDamagePerSecond, maxDamagePerSecond);
        }
    }
    
    public enum WeaponType
    {
        Melee,
        Ranged,
        Thrown,
        Flamethrower
    }

    public enum WeaponFireMode
    {
        Projectile,
        Hitscan,
        Continuous
    }
}
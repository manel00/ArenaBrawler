using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Configuración centralizada para el WeaponFactory.
    /// Permite ajustar stats de armas sin modificar código.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponFactoryConfig", menuName = "ArenaEnhanced/Weapon Factory Config")]
    public class WeaponFactoryConfig : ScriptableObject
    {
        [Header("Assault Rifle Settings")]
        public float arMinDamage = 12f;
        public float arMaxDamage = 18f;
        public float arAttackRange = 9999f;
        public float arAttackCooldown = 0.15f;
        public int arMaxAmmo = 30;
        public float arSpreadAngle = 2f;
        public float arProjectileSpeed = 65f;
        public Color arWeaponColor = new Color(0.3f, 0.7f, 1f);
        public Vector3 arGroundScale = new Vector3(1.3f, 1.3f, 1.3f);
        public Vector3 arHandScale = new Vector3(0.4f, 0.4f, 0.4f);
        
        [Header("Shotgun Settings")]
        public float sgMinDamage = 10f;
        public float sgMaxDamage = 25f;
        public float sgAttackRange = 18f;
        public float sgAttackCooldown = 0.9f;
        public int sgMaxAmmo = 20;
        public int sgProjectilesPerShot = 8;
        public float sgSpreadAngle = 14f;
        public float sgProjectileSpeed = 35f;
        public Color sgWeaponColor = new Color(1f, 0.82f, 0.35f);
        public Vector3 sgGroundScale = new Vector3(0.5f, 0.5f, 0.5f);
        public Vector3 sgHandScale = new Vector3(0.1f, 0.1f, 0.1f);
        
        [Header("Flamethrower Settings")]
        public float ftMinDamagePerSecond = 25f;
        public float ftMaxDamagePerSecond = 50f;
        public float ftAttackRange = 30f;
        public float ftAttackCooldown = 0.05f;
        public float ftSpreadAngle = 30f;
        public Color ftWeaponColor = new Color(1f, 0.35f, 0.1f);
        public Vector3 ftGroundScale = new Vector3(12f, 12f, 12f);
        public Vector3 ftHandScale = new Vector3(2.5f, 2.5f, 2.5f);
        
        private static WeaponFactoryConfig _instance;
        
        /// <summary>
        /// Obtiene la instancia de configuración (carga desde Resources si existe)
        /// </summary>
        public static WeaponFactoryConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<WeaponFactoryConfig>("WeaponFactoryConfig");
                    if (_instance == null)
                    {
                        // Crear configuración por defecto si no existe
                        _instance = CreateInstance<WeaponFactoryConfig>();
                    }
                }
                return _instance;
            }
        }
    }
}

using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Factory para crear armas de forma centralizada.
    /// Reemplaza la creación runtime de armas en ArenaBootstrap con prefabs configurables.
    /// </summary>
    public static class WeaponFactory
    {
        private static WeaponData[] _cachedWeapons;
        
        /// <summary>
        /// Obtiene o crea las armas del juego
        /// </summary>
        public static WeaponData[] GetWeapons()
        {
            if (_cachedWeapons != null) return _cachedWeapons;
            
            _cachedWeapons = CreateDefaultWeapons();
            return _cachedWeapons;
        }
        
        /// <summary>
        /// Crea las armas por defecto del juego
        /// </summary>
        private static WeaponData[] CreateDefaultWeapons()
        {
            return new WeaponData[]
            {
                CreateAssaultRifle(),
                CreateShotgun(),
                CreateFlamethrower()
            };
        }
        
        private static WeaponData CreateAssaultRifle()
        {
            var config = WeaponFactoryConfig.Instance;
            var weapon = ScriptableObject.CreateInstance<WeaponData>();
            weapon.weaponName = "Assault Rifle";
            weapon.type = WeaponType.Rifle;
            weapon.fireMode = WeaponFireMode.Projectile;
            weapon.weaponColor = config.arWeaponColor;
            weapon.groundScale = config.arGroundScale;
            weapon.handScale = config.arHandScale;
            weapon.rotationOffset = new Vector3(0, 180f, 0);
            weapon.minDamage = config.arMinDamage;
            weapon.maxDamage = config.arMaxDamage;
            weapon.attackRange = config.arAttackRange;
            weapon.attackCooldown = config.arAttackCooldown;
            weapon.maxAmmo = config.arMaxAmmo;
            weapon.infiniteAmmo = false;
            weapon.projectilesPerShot = 1;
            weapon.spreadAngle = config.arSpreadAngle;
            weapon.projectileSpeed = config.arProjectileSpeed;
            weapon.splashRadius = 0f;
            return weapon;
        }
        
        private static WeaponData CreateShotgun()
        {
            var config = WeaponFactoryConfig.Instance;
            var weapon = ScriptableObject.CreateInstance<WeaponData>();
            weapon.weaponName = "Shotgun";
            weapon.type = WeaponType.Shotgun;
            weapon.fireMode = WeaponFireMode.Projectile;
            weapon.weaponColor = config.sgWeaponColor;
            weapon.groundScale = config.sgGroundScale;
            weapon.handScale = config.sgHandScale;
            weapon.rotationOffset = new Vector3(0, 180f, 0);
            weapon.minDamage = config.sgMinDamage;
            weapon.maxDamage = config.sgMaxDamage;
            weapon.attackRange = config.sgAttackRange;
            weapon.attackCooldown = config.sgAttackCooldown;
            weapon.maxAmmo = config.sgMaxAmmo;
            weapon.infiniteAmmo = false;
            weapon.projectilesPerShot = config.sgProjectilesPerShot;
            weapon.spreadAngle = config.sgSpreadAngle;
            weapon.projectileSpeed = config.sgProjectileSpeed;
            weapon.splashRadius = 0f;
            return weapon;
        }
        
        private static WeaponData CreateFlamethrower()
        {
            var config = WeaponFactoryConfig.Instance;
            var weapon = ScriptableObject.CreateInstance<WeaponData>();
            weapon.weaponName = "Flamethrower";
            weapon.type = WeaponType.Flamethrower;
            weapon.fireMode = WeaponFireMode.Continuous;
            weapon.weaponColor = config.ftWeaponColor;
            weapon.groundScale = config.ftGroundScale;
            weapon.handScale = config.ftHandScale;
            weapon.rotationOffset = new Vector3(0, 180f, 0);
            weapon.minDamage = 0f;
            weapon.maxDamage = 0f;
            weapon.attackRange = config.ftAttackRange;
            weapon.attackCooldown = config.ftAttackCooldown;
            weapon.maxAmmo = 0;
            weapon.infiniteAmmo = true;
            weapon.projectilesPerShot = 1;
            weapon.spreadAngle = config.ftSpreadAngle;
            weapon.projectileSpeed = 0f;
            weapon.splashRadius = 0f;
            weapon.minDamagePerSecond = config.ftMinDamagePerSecond;
            weapon.maxDamagePerSecond = config.ftMaxDamagePerSecond;
            return weapon;
        }
        
        /// <summary>
        /// Busca un arma por nombre
        /// </summary>
        public static WeaponData FindWeapon(string name)
        {
            var weapons = GetWeapons();
            foreach (var w in weapons)
            {
                if (w != null && w.weaponName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return w;
            }
            return null;
        }
        
        /// <summary>
        /// Crea un pickup de arma en el mundo
        /// </summary>
        public static WeaponPickup CreateWeaponPickup(WeaponData weaponData, Vector3 position, int ammo)
        {
            if (weaponData == null) return null;
            return WeaponPickup.CreatePickup(weaponData, position, ammo);
        }
    }
}

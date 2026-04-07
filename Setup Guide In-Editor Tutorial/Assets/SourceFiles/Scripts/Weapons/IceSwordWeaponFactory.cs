using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// ScriptableObject factory para crear la espada de hielo
    /// Ejecutar en Editor para crear el asset
    /// </summary>
    public class IceSwordWeaponFactory
    {
        public static WeaponData CreateIceSword()
        {
            var weapon = ScriptableObject.CreateInstance<WeaponData>();
            weapon.weaponName = "Ice Sword";
            weapon.type = WeaponType.MeleeSword;
            weapon.fireMode = WeaponFireMode.Melee;
            weapon.minDamage = 25f;
            weapon.maxDamage = 35f;
            weapon.attackRange = 3f;
            weapon.attackCooldown = 0.4f;
            weapon.maxAmmo = 0;
            weapon.infiniteAmmo = true;
            weapon.weaponColor = new Color(0.5f, 0.8f, 1f, 1f);
            weapon.weaponScale = Vector3.one;
            weapon.handScale = Vector3.one * 0.5f;
            weapon.rotationOffset = new Vector3(0, 0, 90);
            return weapon;
        }
    }
}
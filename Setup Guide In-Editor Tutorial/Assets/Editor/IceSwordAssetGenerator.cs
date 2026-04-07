#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ArenaEnhanced.Editor
{
    public static class IceSwordAssetGenerator
    {
        [MenuItem("Tools/Generate Ice Sword Weapon")]
        public static void Generate()
        {
            WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();
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
            weapon.handScale = new Vector3(0.5f, 0.5f, 0.5f);
            weapon.rotationOffset = new Vector3(0, 0, 90);
            
            // Cargar prefab de espada si existe
            GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Weapons/ice_sword_by_get3dmodels.glb");
            if (swordPrefab != null)
            {
                weapon.prefab = swordPrefab;
            }
            
            string path = "Assets/Resources/Weapons/IceSword_WeaponData.asset";
            AssetDatabase.CreateAsset(weapon, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Ice Sword Created", "WeaponData created at:\n" + path, "OK");
            Selection.activeObject = weapon;
        }
    }
}
#endif

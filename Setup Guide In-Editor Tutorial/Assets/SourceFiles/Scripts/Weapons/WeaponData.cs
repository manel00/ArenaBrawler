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
        public WeaponType type = WeaponType.Melee;
        public GameObject prefab;
        
        [Header("Estadísticas")]
        public int maxDurability = 5;
        public float damage = 10f;
        public float attackRange = 2f;
        public float attackCooldown = 0.5f;
        
        [Header("Apariencia")]
        public Color weaponColor = Color.gray;
        public Vector3 weaponScale = Vector3.one;
        
        [Header("VFX")]
        public GameObject attackVFX;
        public AudioClip attackSound;
    }
    
    public enum WeaponType
    {
        Melee,
        Ranged,
        Thrown
    }
}
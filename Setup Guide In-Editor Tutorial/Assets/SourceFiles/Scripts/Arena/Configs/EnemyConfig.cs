using UnityEngine;

namespace ArenaEnhanced.Configs
{
    /// <summary>
    /// Configuración para enemigos - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Arena/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("General")]
        public string enemyName = "Enemy";
        public string description = "";
        public GameObject prefab;
        
        [Header("Stats")]
        public float maxHealth = 100f;
        public float moveSpeed = 6.5f;
        public float damage = 10f;
        public float attackRange = 2f;
        public float attackCooldown = 1.2f;
        
        [Header("Detection")]
        public float detectDistance = 25f;
        public float preferredDistance = 8f;
        public float bossEvadeDistance = 15f;
        
        [Header("Combat")]
        public float fireballSpeed = 20f;
        public float strafeStrength = 0.35f;
        
        [Header("Visual")]
        public Color enemyColor = Color.red;
        public float scale = 1f;
        
        [Header("Drops")]
        public int pointsValue = 10;
        public GameObject[] dropPrefabs;
        [Range(0f, 1f)]
        public float dropChance = 0.1f;
        
        [Header("Audio")]
        public AudioClip attackSound;
        public AudioClip deathSound;
        public AudioClip hurtSound;
        
        /// <summary>
        /// Valida que la configuración sea correcta
        /// </summary>
        public bool Validate()
        {
            if (maxHealth <= 0)
            {
                Debug.LogError($"[EnemyConfig] {enemyName}: maxHealth must be > 0");
                return false;
            }
            
            if (moveSpeed <= 0)
            {
                Debug.LogError($"[EnemyConfig] {enemyName}: moveSpeed must be > 0");
                return false;
            }
            
            if (detectDistance <= 0)
            {
                Debug.LogError($"[EnemyConfig] {enemyName}: detectDistance must be > 0");
                return false;
            }
            
            return true;
        }
    }
}
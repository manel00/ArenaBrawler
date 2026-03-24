using UnityEngine;
using WoW.Armas;

namespace WoW.Armas
{
    /// <summary>
    /// Script de utilidad para spawnear armas en la escena
    /// Útil para pruebas en Editor
    /// </summary>
    public class WeaponSpawner : MonoBehaviour
    {
        [Header("Configuración de Spawn")]
        [SerializeField] private WeaponData[] weaponsToSpawn;
        [SerializeField] private int weaponsPerType = 3;
        [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero;
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(20f, 0f, 20f);
        
        [Header("Spawn Automático")]
        [SerializeField] private bool spawnOnStart = false;
        [SerializeField] private bool spawnInGrid = false;
        [SerializeField] private float gridSpacing = 3f;
        
        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnAllWeapons();
            }
        }
        
        /// <summary>
        /// Inicializa el spawner con la cantidad de jugadores
        /// </summary>
        public void Initialize(int playerCount)
        {
            Debug.Log($"[WeaponSpawner] Inicializado para {playerCount} jugadores");
            
            // Ajustar cantidad de armas según jugadores si es necesario
            if (playerCount > 1)
            {
                weaponsPerType = Mathf.Max(weaponsPerType, playerCount);
            }
        }
        
        /// <summary>
        /// Inicializa el spawner con armas y configuración completa
        /// </summary>
        public void Initialize(WeaponData[] weapons, int perType, float radius, float sizeMultiplier = 1f)
        {
            weaponsToSpawn = weapons;
            weaponsPerType = perType;
            spawnAreaSize = new Vector3(radius * 2f * sizeMultiplier, 0f, radius * 2f * sizeMultiplier);
            Debug.Log($"[WeaponSpawner] Inicializado con {weapons?.Length ?? 0} tipos de armas, {perType} por tipo");
        }
        
        /// <summary>
        /// Spawnea armas alrededor de un punto central
        /// </summary>
        public void SpawnWeapons(Transform parent, WeaponData[] weapons, int perType, float radius, float sizeMultiplier = 1f)
        {
            if (weapons == null || weapons.Length == 0)
            {
                Debug.LogWarning("[WeaponSpawner] No hay armas para spawnear");
                return;
            }
            
            int totalWeapons = weapons.Length * perType;
            float angleStep = 360f / totalWeapons;
            
            for (int i = 0; i < totalWeapons; i++)
            {
                WeaponData weapon = weapons[i % weapons.Length];
                
                float angle = angleStep * i + Random.Range(-15f, 15f);
                float rad = angle * Mathf.Deg2Rad;
                float distance = Random.Range(8f * sizeMultiplier, radius * sizeMultiplier);
                Vector3 pos = new Vector3(Mathf.Cos(rad) * distance, 0.5f, Mathf.Sin(rad) * distance);
                
                var pickup = WeaponPickup.CreatePickup(weapon, pos);
                if (pickup != null && parent != null)
                {
                    pickup.transform.SetParent(parent);
                }
            }
            
            Debug.Log($"[WeaponSpawner] Spawneadas {totalWeapons} armas");
        }
        
        /// <summary>
        /// Spawnea todas las armas configuradas
        /// </summary>
        [ContextMenu("Spawn Todas las Armas")]
        public void SpawnAllWeapons()
        {
            if (weaponsToSpawn == null || weaponsToSpawn.Length == 0)
            {
                Debug.LogWarning("WeaponSpawner: No hay armas configuradas para spawnear");
                return;
            }
            
            // Limpiar armas existentes en el área
            ClearSpawnedWeapons();
            
            int index = 0;
            foreach (var weaponData in weaponsToSpawn)
            {
                for (int i = 0; i < weaponsPerType; i++)
                {
                    Vector3 spawnPos;
                    
                    if (spawnInGrid)
                    {
                        // Spawn en patrón de grid
                        float x = (index % 5) * gridSpacing - 2 * gridSpacing;
                        float z = (i % 3) * gridSpacing - gridSpacing;
                        spawnPos = spawnAreaCenter + new Vector3(x, 1f, z);
                    }
                    else
                    {
                        // Spawn aleatorio
                        spawnPos = GetRandomSpawnPosition();
                    }
                    
                    WeaponPickup.CreatePickup(weaponData, spawnPos);
                    index++;
                }
            }
            
            Debug.Log($"WeaponSpawner: Se han spawnado {weaponsToSpawn.Length * weaponsPerType} armas");
        }
        
        /// <summary>
        /// Spawnear un arma específica en una posición
        /// </summary>
        public static WeaponPickup SpawnWeapon(WeaponData data, Vector3 position)
        {
            return WeaponPickup.CreatePickup(data, position);
        }
        
        /// <summary>
        /// Spawnear un arma aleatoria en posición aleatoria
        /// </summary>
        public static WeaponPickup SpawnRandomWeapon(WeaponData[] weapons, Vector3 center, float radius)
        {
            if (weapons == null || weapons.Length == 0)
            {
                Debug.LogWarning("SpawnRandomWeapon: No hay armas disponibles");
                return null;
            }
            
            int randomIndex = Random.Range(0, weapons.Length);
            Vector3 randomPos = center + Random.insideUnitSphere * radius;
            randomPos.y = 1f; // Altura del suelo
            
            return WeaponPickup.CreatePickup(weapons[randomIndex], randomPos);
        }
        
        /// <summary>
        /// Limpiar todas las armas spawnedas
        /// </summary>
        [ContextMenu("Limpiar Armas Spawnedas")]
        public void ClearSpawnedWeapons()
        {
            WeaponPickup[] pickups = FindObjectsByType<WeaponPickup>();
            foreach (var pickup in pickups)
            {
                Destroy(pickup.gameObject);
            }
            Debug.Log($"WeaponSpawner: Se han eliminado {pickups.Length} armas");
        }
        
        /// <summary>
        /// Obtener posición aleatoria dentro del área de spawn
        /// </summary>
        private Vector3 GetRandomSpawnPosition()
        {
            float x = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
            float z = Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f);
            return spawnAreaCenter + new Vector3(x, 1f, z);
        }
        
        /// <summary>
        /// Visualizar el área de spawn en el Editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawCube(spawnAreaCenter, spawnAreaSize);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
        }
    }
}
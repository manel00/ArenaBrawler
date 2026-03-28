using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Spawner dinámico de elementos de entorno interactivo
    /// </summary>
    public class EnvironmentSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Intervalo mínimo entre spawns en segundos")]
        [Range(10f, 120f)]
        [SerializeField] private float minSpawnInterval = 30f;
        
        [Tooltip("Intervalo máximo entre spawns en segundos")]
        [Range(30f, 180f)]
        [SerializeField] private float maxSpawnInterval = 90f;
        
        [Tooltip("Distancia mínima del jugador para spawn")]
        [Range(5f, 30f)]
        [SerializeField] private float minDistanceFromPlayer = 10f;
        
        [Tooltip("Distancia máxima del jugador para spawn")]
        [Range(20f, 100f)]
        [SerializeField] private float maxDistanceFromPlayer = 50f;
        
        [Tooltip("Altura de spawn sobre el suelo")]
        [Range(0f, 2f)]
        [SerializeField] private float spawnHeight = 0.5f;
        
        [Header("Element Limits")]
        [Tooltip("Número máximo de barriles explosivos")]
        [Range(1, 20)]
        [SerializeField] private int maxExplosiveBarrels = 5;
        
        [Tooltip("Número máximo de trampas de pinchos")]
        [Range(1, 30)]
        [SerializeField] private int maxSpikeTraps = 8;
        
        [Tooltip("Número máximo de zonas de fuego")]
        [Range(1, 10)]
        [SerializeField] private int maxFireZones = 3;
        
        [Header("Element Prefabs")]
        [SerializeField] private GameObject explosiveBarrelPrefab;
        [SerializeField] private GameObject spikeTrapPrefab;
        [SerializeField] private GameObject fireZonePrefab;
        
        [Header("Map Bounds")]
        [SerializeField] private Vector3 mapCenter = Vector3.zero;
        [SerializeField] private float mapRadius = 100f;
        
        [Header("Dependencies")]
        [SerializeField] private Transform playerTransform;
        
        private List<GameObject> _activeBarrels = new List<GameObject>();
        private List<GameObject> _activeTraps = new List<GameObject>();
        private List<GameObject> _activeFireZones = new List<GameObject>();
        private List<Transform> _allyTransforms = new List<Transform>();
        private List<Transform> _enemyTransforms = new List<Transform>();
        
        // Object pools
        private Queue<GameObject> _barrelPool = new Queue<GameObject>();
        private Queue<GameObject> _trapPool = new Queue<GameObject>();
        private Queue<GameObject> _fireZonePool = new Queue<GameObject>();
        
        // OverlapSphere buffer to avoid GC allocations
        private readonly Collider[] _overlapBuffer = new Collider[10];
        
        private Coroutine _barrelSpawnCoroutine;
        private Coroutine _trapSpawnCoroutine;
        private Coroutine _fireZoneSpawnCoroutine;
        
        public static EnvironmentSpawner Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void Start()
        {
            // Try to find player if not assigned
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }
            
            // Initialize object pools
            InitializePools();
            
            // Start spawning cycles
            _barrelSpawnCoroutine = StartCoroutine(SpawnExplosiveBarrels());
            _trapSpawnCoroutine = StartCoroutine(SpawnSpikeTraps());
            _fireZoneSpawnCoroutine = StartCoroutine(SpawnFireZones());
        }
        
        private void InitializePools()
        {
            // Pre-instantiate objects for pooling
            for (int i = 0; i < maxExplosiveBarrels; i++)
            {
                if (explosiveBarrelPrefab != null)
                {
                    GameObject barrel = Instantiate(explosiveBarrelPrefab, transform.position, Quaternion.identity);
                    barrel.SetActive(false);
                    _barrelPool.Enqueue(barrel);
                }
            }
            
            for (int i = 0; i < maxSpikeTraps; i++)
            {
                if (spikeTrapPrefab != null)
                {
                    GameObject trap = Instantiate(spikeTrapPrefab, transform.position, Quaternion.identity);
                    trap.SetActive(false);
                    _trapPool.Enqueue(trap);
                }
            }
            
            for (int i = 0; i < maxFireZones; i++)
            {
                if (fireZonePrefab != null)
                {
                    GameObject fireZone = Instantiate(fireZonePrefab, transform.position, Quaternion.identity);
                    fireZone.SetActive(false);
                    _fireZonePool.Enqueue(fireZone);
                }
            }
        }
        
        private IEnumerator SpawnExplosiveBarrels()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));
                
                if (_activeBarrels.Count < maxExplosiveBarrels)
                {
                    SpawnElementFromPool(_barrelPool, _activeBarrels);
                }
            }
        }
        
        private IEnumerator SpawnSpikeTraps()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(30f, 40f));
                
                if (_activeTraps.Count < maxSpikeTraps)
                {
                    SpawnElementFromPool(_trapPool, _activeTraps);
                }
            }
        }
        
        private IEnumerator SpawnFireZones()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(60f, 90f));
                
                if (_activeFireZones.Count < maxFireZones)
                {
                    SpawnElementFromPool(_fireZonePool, _activeFireZones);
                }
            }
        }
        
        private void SpawnElementFromPool(Queue<GameObject> pool, List<GameObject> activeList)
        {
            if (pool.Count == 0) return;
            
            Vector3 spawnPosition = GetRandomSpawnPosition();
            if (spawnPosition == Vector3.zero) return;
            
            GameObject element = pool.Dequeue();
            element.transform.position = spawnPosition;
            element.SetActive(true);
            activeList.Add(element);
            
            Debug.Log($"[EnvironmentSpawner] Spawned {element.name} at {spawnPosition}");
        }
        
        public void ReturnToPool(GameObject element)
        {
            if (element == null) return;
            
            element.SetActive(false);
            element.transform.position = transform.position;
            
            // Return to appropriate pool based on component
            if (element.GetComponent<ExplosiveBarrel>() != null)
            {
                _activeBarrels.Remove(element);
                _barrelPool.Enqueue(element);
            }
            else if (element.GetComponent<SpikeTrap>() != null)
            {
                _activeTraps.Remove(element);
                _trapPool.Enqueue(element);
            }
            else if (element.GetComponent<FireZone>() != null)
            {
                _activeFireZones.Remove(element);
                _fireZonePool.Enqueue(element);
            }
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            List<Vector3> validPositions = new List<Vector3>();
            
            // Get positions near player
            if (playerTransform != null)
            {
                Vector3 playerPos = playerTransform.position;
                for (int i = 0; i < 10; i++)
                {
                    Vector3 randomPos = playerPos + Random.insideUnitSphere * maxDistanceFromPlayer;
                    randomPos.y = spawnHeight;
                    
                    if (IsValidSpawnPosition(randomPos))
                    {
                        validPositions.Add(randomPos);
                    }
                }
            }
            
            // Get positions near allies
            foreach (Transform ally in _allyTransforms)
            {
                if (ally != null)
                {
                    Vector3 allyPos = ally.position;
                    Vector3 randomPos = allyPos + Random.insideUnitSphere * (maxDistanceFromPlayer * 0.5f);
                    randomPos.y = spawnHeight;
                    
                    if (IsValidSpawnPosition(randomPos))
                    {
                        validPositions.Add(randomPos);
                    }
                }
            }
            
            // Get positions near enemies
            foreach (Transform enemy in _enemyTransforms)
            {
                if (enemy != null)
                {
                    Vector3 enemyPos = enemy.position;
                    Vector3 randomPos = enemyPos + Random.insideUnitSphere * (maxDistanceFromPlayer * 0.3f);
                    randomPos.y = spawnHeight;
                    
                    if (IsValidSpawnPosition(randomPos))
                    {
                        validPositions.Add(randomPos);
                    }
                }
            }
            
            // If no valid positions, try random map position
            if (validPositions.Count == 0)
            {
                Vector3 randomMapPos = mapCenter + Random.insideUnitSphere * mapRadius;
                randomMapPos.y = spawnHeight;
                
                if (IsValidSpawnPosition(randomMapPos))
                {
                    return randomMapPos;
                }
            }
            
            return validPositions.Count > 0 ? validPositions[Random.Range(0, validPositions.Count)] : Vector3.zero;
        }
        
        private bool IsValidSpawnPosition(Vector3 position)
        {
            // Check if position is within map bounds
            if (Vector3.Distance(position, mapCenter) > mapRadius)
                return false;
            
            // Check if position is not too close to player
            if (playerTransform != null && Vector3.Distance(position, playerTransform.position) < minDistanceFromPlayer)
                return false;
            
            // Check for obstacles using NonAlloc
            int hitCount = Physics.OverlapSphereNonAlloc(position, 1f, _overlapBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col.gameObject != gameObject && !col.isTrigger)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        public void RegisterAlly(Transform ally)
        {
            if (!_allyTransforms.Contains(ally))
            {
                _allyTransforms.Add(ally);
            }
        }
        
        public void RegisterEnemy(Transform enemy)
        {
            if (!_enemyTransforms.Contains(enemy))
            {
                _enemyTransforms.Add(enemy);
            }
        }
        
        public void UnregisterAlly(Transform ally)
        {
            _allyTransforms.Remove(ally);
        }
        
        public void UnregisterEnemy(Transform enemy)
        {
            _enemyTransforms.Remove(enemy);
        }
        
        public void ClearAllElements()
        {
            // Return all active elements to pool
            foreach (GameObject barrel in _activeBarrels)
            {
                if (barrel != null) ReturnToPool(barrel);
            }
            
            foreach (GameObject trap in _activeTraps)
            {
                if (trap != null) ReturnToPool(trap);
            }
            
            foreach (GameObject fire in _activeFireZones)
            {
                if (fire != null) ReturnToPool(fire);
            }
            
            _activeBarrels.Clear();
            _activeTraps.Clear();
            _activeFireZones.Clear();
        }
        
        public void SetPlayerTransform(Transform player)
        {
            playerTransform = player;
        }
        
        private void OnDestroy()
        {
            // Stop coroutines
            if (_barrelSpawnCoroutine != null)
            {
                StopCoroutine(_barrelSpawnCoroutine);
            }
            if (_trapSpawnCoroutine != null)
            {
                StopCoroutine(_trapSpawnCoroutine);
            }
            if (_fireZoneSpawnCoroutine != null)
            {
                StopCoroutine(_fireZoneSpawnCoroutine);
            }
            
            // Destroy all pooled objects
            while (_barrelPool.Count > 0)
            {
                Destroy(_barrelPool.Dequeue());
            }
            while (_trapPool.Count > 0)
            {
                Destroy(_trapPool.Dequeue());
            }
            while (_fireZonePool.Count > 0)
            {
                Destroy(_fireZonePool.Dequeue());
            }
            
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
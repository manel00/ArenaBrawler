using UnityEngine;
using System.Collections.Generic;

namespace ArenaEnhanced.Managers
{
    /// <summary>
    /// Sistema centralizado de object pooling para evitar allocations de memoria
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        [System.Serializable]
        public class PoolConfig
        {
            public string poolName;
            public GameObject prefab;
            public int initialSize = 10;
            public int maxSize = 50;
            public bool expandable = true;
        }
        
        [Header("Pool Configurations")]
        [SerializeField] private PoolConfig[] poolConfigs;
        
        private Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, PoolConfig> _configMap = new Dictionary<string, PoolConfig>();
        private Dictionary<string, int> _activeCounts = new Dictionary<string, int>();
        
        public static ObjectPoolManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            InitializePools();
        }
        
        private void InitializePools()
        {
            if (poolConfigs == null) return;
            
            foreach (PoolConfig config in poolConfigs)
            {
                if (config == null || config.prefab == null) continue;
                
                _configMap[config.poolName] = config;
                _pools[config.poolName] = new Queue<GameObject>();
                _activeCounts[config.poolName] = 0;
                
                // Pre-instantiate objects
                for (int i = 0; i < config.initialSize; i++)
                {
                    GameObject obj = CreateNewObject(config);
                    _pools[config.poolName].Enqueue(obj);
                }
            }
        }
        
        private GameObject CreateNewObject(PoolConfig config)
        {
            GameObject obj = Instantiate(config.prefab, transform);
            obj.SetActive(false);
            
            PooledObject pooledObj = obj.AddComponent<PooledObject>();
            pooledObj.PoolName = config.poolName;
            
            return obj;
        }
        
        public GameObject Spawn(string poolName, Vector3 position, Quaternion rotation)
        {
            if (!_pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{poolName}' not found");
                return null;
            }
            
            Queue<GameObject> pool = _pools[poolName];
            PoolConfig config = _configMap[poolName];
            
            GameObject obj;
            
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else if (config.expandable && _activeCounts[poolName] < config.maxSize)
            {
                obj = CreateNewObject(config);
            }
            else
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{poolName}' exhausted and cannot expand");
                return null;
            }
            
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            
            _activeCounts[poolName]++;
            
            return obj;
        }
        
        public void Despawn(string poolName, GameObject obj)
        {
            if (obj == null) return;
            
            if (!_pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{poolName}' not found");
                Destroy(obj);
                return;
            }
            
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            
            _pools[poolName].Enqueue(obj);
            _activeCounts[poolName]--;
        }
        
        public void Despawn(GameObject obj)
        {
            if (obj == null) return;
            
            PooledObject pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj != null)
            {
                Despawn(pooledObj.PoolName, obj);
            }
            else
            {
                Debug.LogWarning($"[ObjectPoolManager] Object does not have PooledObject component");
                Destroy(obj);
            }
        }
        
        public void Preload(string poolName, int count)
        {
            if (!_pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{poolName}' not found");
                return;
            }
            
            PoolConfig config = _configMap[poolName];
            Queue<GameObject> pool = _pools[poolName];
            
            for (int i = 0; i < count; i++)
            {
                if (pool.Count >= config.maxSize) break;
                GameObject obj = CreateNewObject(config);
                pool.Enqueue(obj);
            }
        }
        
        public void ClearPool(string poolName)
        {
            if (!_pools.ContainsKey(poolName)) return;
            
            Queue<GameObject> pool = _pools[poolName];
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            
            _activeCounts[poolName] = 0;
        }
        
        public void ClearAllPools()
        {
            foreach (string poolName in new List<string>(_pools.Keys))
            {
                ClearPool(poolName);
            }
        }
        
        public int GetAvailableCount(string poolName)
        {
            return _pools.ContainsKey(poolName) ? _pools[poolName].Count : 0;
        }
        
        public int GetActiveCount(string poolName)
        {
            return _activeCounts.ContainsKey(poolName) ? _activeCounts[poolName] : 0;
        }
        
        public int GetTotalCount(string poolName)
        {
            return GetAvailableCount(poolName) + GetActiveCount(poolName);
        }
        
        private void OnDestroy()
        {
            ClearAllPools();
            
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
    
    /// <summary>
    /// Componente que identifica objetos que pertenecen a un pool
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public string PoolName { get; set; }
        
        private void OnDisable()
        {
            // Return to pool when disabled, but only if the object is not being destroyed
            // and the pool manager instance is still valid
            if (ObjectPoolManager.Instance != null && this != null && gameObject != null)
            {
                // Only return if we're not already being destroyed
                if (!gameObject.activeInHierarchy && gameObject.scene.isLoaded)
                {
                    ObjectPoolManager.Instance.Despawn(gameObject);
                }
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de Object Pooling genérico para reutilizar objetos en lugar de crear/destruir constantemente.
    /// Elimina lag spikes causadas por garbage collection en gameplay intenso.
    /// </summary>
    public class GenericObjectPool : MonoBehaviour
    {
        [System.Serializable]
        public class Pool
        {
            [Tooltip("Tag único para identificar este pool")]
            public string tag;
            
            [Tooltip("Prefab a instanciar")]
            public GameObject prefab;
            
            [Tooltip("Cantidad de objetos a pre-instanciar")]
            public int size;
            
            [Tooltip("Máximo de objetos que puede crecer el pool (0 = ilimitado)")]
            public int maxSize;
        }

        [Header("Pool Configuration")]
        [SerializeField] private List<Pool> pools;
        
        [Header("Settings")]
        [Tooltip("Crear objeto padre para organizar los objetos del pool en hierarchy")]
        [SerializeField] private bool createPoolParent = true;
        
        [Tooltip("Desactivar objetos al retornarlos al pool")]
        [SerializeField] private bool deactivateOnReturn = true;

        // Diccionario de colas de objetos disponibles por tag
        private Dictionary<string, Queue<GameObject>> _poolDictionary;
        
        // Diccionario de objetos activos (para tracking)
        private Dictionary<GameObject, string> _activeObjects;
        
        // Diccionario de objetos padre para organización
        private Dictionary<string, Transform> _poolParents;
        
        // Singleton pattern
        public static GenericObjectPool Instance { get; private set; }

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

        /// <summary>
        /// Inicializa todos los pools configurados pre-instanciando los objetos
        /// </summary>
        private void InitializePools()
        {
            _poolDictionary = new Dictionary<string, Queue<GameObject>>();
            _activeObjects = new Dictionary<GameObject, string>();
            _poolParents = new Dictionary<string, Transform>();

            if (pools == null)
            {
                pools = new List<Pool>();
                return;
            }

            foreach (var pool in pools)
            {
                if (pool.prefab == null)
                {
                    Debug.LogWarning($"[ObjectPool] Pool '{pool.tag}' no tiene prefab asignado");
                    continue;
                }

                // Crear objeto padre para organización
                if (createPoolParent)
                {
                    GameObject parentGo = new GameObject($"Pool_{pool.tag}");
                    parentGo.transform.SetParent(transform);
                    _poolParents[pool.tag] = parentGo.transform;
                }

                // Pre-instanciar objetos
                Queue<GameObject> objectQueue = new Queue<GameObject>();
                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = CreateNewObject(pool);
                    objectQueue.Enqueue(obj);
                }

                _poolDictionary.Add(pool.tag, objectQueue);
            }
        }

        /// <summary>
        /// Crea un nuevo objeto para el pool
        /// </summary>
        private GameObject CreateNewObject(Pool pool)
        {
            GameObject obj = Instantiate(pool.prefab);
            
            if (_poolParents.ContainsKey(pool.tag))
            {
                obj.transform.SetParent(_poolParents[pool.tag]);
            }
            
            obj.SetActive(false);
            
            // Agregar componente PooledObject para referencia rápida
            var pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                pooledObj = obj.AddComponent<PooledObject>();
            }
            pooledObj.PoolTag = pool.tag;
            
            return obj;
        }

        /// <summary>
        /// Obtiene un objeto del pool especificado
        /// </summary>
        /// <param name="tag">Tag del pool</param>
        /// <param name="position">Posición donde spawnear</param>
        /// <param name="rotation">Rotación del objeto</param>
        /// <returns>GameObject del pool o null si no disponible</returns>
        public GameObject GetFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"[ObjectPool] Pool con tag '{tag}' no existe");
                return null;
            }

            GameObject objectToSpawn;
            Queue<GameObject> poolQueue = _poolDictionary[tag];
            Pool poolConfig = pools.Find(p => p.tag == tag);

            // Si hay objetos disponibles en la cola
            if (poolQueue.Count > 0)
            {
                objectToSpawn = poolQueue.Dequeue();
            }
            else
            {
                // Pool está vacío - verificar si podemos crecer
                int currentCount = _activeObjects.Count + poolQueue.Count;
                if (poolConfig.maxSize > 0 && currentCount >= poolConfig.maxSize)
                {
                    Debug.LogWarning($"[ObjectPool] Pool '{tag}' ha alcanzado su límite máximo ({poolConfig.maxSize})");
                    return null;
                }

                // Crear nuevo objeto
                objectToSpawn = CreateNewObject(poolConfig);
            }

            // Configurar y activar
            objectToSpawn.transform.SetPositionAndRotation(position, rotation);
            objectToSpawn.SetActive(true);
            
            // IMPORTANTE: Resetear velocidad del Rigidbody si existe
            var rb = objectToSpawn.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Tracking
            _activeObjects[objectToSpawn] = tag;

            // Notificar al componente que fue spawneado
            var pooledObj = objectToSpawn.GetComponent<PooledObject>();
            pooledObj?.OnSpawnFromPool();

            return objectToSpawn;
        }

        /// <summary>
        /// Retorna un objeto al pool
        /// </summary>
        /// <param name="objectToReturn">Objeto a retornar</param>
        public void ReturnToPool(GameObject objectToReturn)
        {
            if (objectToReturn == null) return;

            // Verificar si es un objeto pooleado
            var pooledObj = objectToReturn.GetComponent<PooledObject>();
            string tag = pooledObj?.PoolTag;

            if (string.IsNullOrEmpty(tag) || !_poolDictionary.ContainsKey(tag))
            {
                // No es un objeto del pool - destruir normalmente
                Destroy(objectToReturn);
                return;
            }

            // Remover del tracking de activos
            _activeObjects.Remove(objectToReturn);

            // Notificar al componente que será retornado
            pooledObj.OnReturnToPool();

            // Retornar a la cola
            if (deactivateOnReturn)
            {
                objectToReturn.SetActive(false);
            }

            // Mover al objeto padre del pool
            if (_poolParents.ContainsKey(tag))
            {
                objectToReturn.transform.SetParent(_poolParents[tag]);
            }

            _poolDictionary[tag].Enqueue(objectToReturn);
        }

        /// <summary>
        /// Retorna todos los objetos activos de un pool específico
        /// </summary>
        public void ReturnAllToPool(string tag)
        {
            if (!_poolDictionary.ContainsKey(tag)) return;

            // Copiar lista para evitar modificación durante iteración
            List<GameObject> toReturn = new List<GameObject>();
            
            foreach (var kvp in _activeObjects)
            {
                if (kvp.Value == tag)
                {
                    toReturn.Add(kvp.Key);
                }
            }

            foreach (var obj in toReturn)
            {
                ReturnToPool(obj);
            }
        }

        /// <summary>
        /// Obtiene la cantidad de objetos disponibles en un pool
        /// </summary>
        public int GetAvailableCount(string tag)
        {
            if (!_poolDictionary.ContainsKey(tag)) return 0;
            return _poolDictionary[tag].Count;
        }

        /// <summary>
        /// Obtiene la cantidad de objetos activos de un pool
        /// </summary>
        public int GetActiveCount(string tag)
        {
            int count = 0;
            foreach (var kvp in _activeObjects)
            {
                if (kvp.Value == tag) count++;
            }
            return count;
        }

        /// <summary>
        /// Verifica si existe un pool con el tag especificado
        /// </summary>
        public bool HasPool(string tag)
        {
            return _poolDictionary != null && _poolDictionary.ContainsKey(tag);
        }

        /// <summary>
        /// Crea un pool dinámicamente en tiempo de ejecución
        /// </summary>
        public void CreatePool(string tag, GameObject prefab, int size, int maxSize = 0)
        {
            if (_poolDictionary == null)
            {
                _poolDictionary = new Dictionary<string, Queue<GameObject>>();
                _activeObjects = new Dictionary<GameObject, string>();
                _poolParents = new Dictionary<string, Transform>();
            }

            if (_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"[ObjectPool] Pool '{tag}' ya existe");
                return;
            }

            if (prefab == null)
            {
                Debug.LogError($"[ObjectPool] No se puede crear pool '{tag}' - prefab es null");
                return;
            }

            // Crear objeto padre para organización
            if (createPoolParent)
            {
                GameObject parentGo = new GameObject($"Pool_{tag}");
                parentGo.transform.SetParent(transform);
                _poolParents[tag] = parentGo.transform;
            }

            // Pre-instanciar objetos
            Queue<GameObject> objectQueue = new Queue<GameObject>();
            for (int i = 0; i < size; i++)
            {
                GameObject obj = CreateNewObjectForTag(tag, prefab);
                objectQueue.Enqueue(obj);
            }

            _poolDictionary.Add(tag, objectQueue);
            
            // Agregar a la lista de pools para referencia
            if (pools == null)
            {
                pools = new List<Pool>();
            }
            var newPool = new Pool { tag = tag, prefab = prefab, size = size, maxSize = maxSize };
            pools.Add(newPool);
        }

        private GameObject CreateNewObjectForTag(string tag, GameObject prefab)
        {
            GameObject obj = Instantiate(prefab);
            
            if (_poolParents.ContainsKey(tag))
            {
                obj.transform.SetParent(_poolParents[tag]);
            }
            
            obj.SetActive(false);
            
            var pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                pooledObj = obj.AddComponent<PooledObject>();
            }
            pooledObj.PoolTag = tag;
            
            return obj;
        }
    }

    /// <summary>
    /// Componente que se agrega a los objetos del pool para tracking y callbacks
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        [HideInInspector]
        public string PoolTag { get; set; }

        /// <summary>
        /// Llamado cuando el objeto es spawneado del pool
        /// Sobrescribir en clases hijas para inicialización personalizada
        /// </summary>
        public virtual void OnSpawnFromPool()
        {
            // Resetear estado del objeto aquí
        }

        /// <summary>
        /// Llamado cuando el objeto es retornado al pool
        /// Sobrescribir en clases hijas para limpieza personalizada
        /// </summary>
        public virtual void OnReturnToPool()
        {
            // Limpiar estado del objeto aquí
        }

        /// <summary>
        /// Método helper para retornar este objeto al pool
        /// </summary>
        public void ReturnToPool()
        {
            GenericObjectPool.Instance?.ReturnToPool(gameObject);
        }
    }
}

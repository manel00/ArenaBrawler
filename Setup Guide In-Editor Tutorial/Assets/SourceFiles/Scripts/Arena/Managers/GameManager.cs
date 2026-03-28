using UnityEngine;

namespace ArenaEnhanced.Managers
{
    /// <summary>
    /// GameManager centralizado que gestiona el ciclo de vida de todos los managers
    /// Usa DontDestroyOnLoad solo una vez para evitar duplicados
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Manager References")]
        [Tooltip("Referencia al ObjectPoolManager")]
        [SerializeField] private ObjectPoolManager objectPoolManager;
        
        [Tooltip("Referencia al GameEventSystem")]
        [SerializeField] private GameEventSystem gameEventSystem;
        
        [Tooltip("Referencia al EnvironmentSpawner")]
        [SerializeField] private EnvironmentSpawner environmentSpawner;
        
        [Tooltip("Referencia al HordePressureSystem")]
        [SerializeField] private HordePressureSystem hordePressureSystem;

        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[GameManager] Inicializado correctamente");
        }

        private void Start()
        {
            // Initialize all managers
            InitializeManagers();
        }

        private void InitializeManagers()
        {
            if (objectPoolManager != null)
            {
                Debug.Log("[GameManager] ObjectPoolManager inicializado");
            }
            
            if (gameEventSystem != null)
            {
                Debug.Log("[GameManager] GameEventSystem inicializado");
            }
            
            if (environmentSpawner != null)
            {
                Debug.Log("[GameManager] EnvironmentSpawner inicializado");
            }
            
            if (hordePressureSystem != null)
            {
                Debug.Log("[GameManager] HordePressureSystem inicializado");
            }
        }

        /// <summary>
        /// Limpia todos los managers al salir del juego
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Debug.Log("[GameManager] Limpiando managers...");
                
                // Clear pools
                if (objectPoolManager != null)
                {
                    objectPoolManager.ClearAllPools();
                }
                
                // Clear events
                if (gameEventSystem != null)
                {
                    gameEventSystem.ClearAllEvents();
                }
                
                Instance = null;
            }
        }

        /// <summary>
        /// Reinicia todos los managers para una nueva partida
        /// </summary>
        public void ResetAllManagers()
        {
            Debug.Log("[GameManager] Reiniciando todos los managers...");
            
            if (objectPoolManager != null)
            {
                objectPoolManager.ClearAllPools();
            }
            
            if (hordePressureSystem != null)
            {
                hordePressureSystem.ResetPressure();
            }
            
            if (environmentSpawner != null)
            {
                environmentSpawner.ClearAllElements();
            }
        }

        // Getters for manager access
        public ObjectPoolManager GetObjectPoolManager() => objectPoolManager;
        public GameEventSystem GetGameEventSystem() => gameEventSystem;
        public EnvironmentSpawner GetEnvironmentSpawner() => environmentSpawner;
        public HordePressureSystem GetHordePressureSystem() => hordePressureSystem;
    }
}
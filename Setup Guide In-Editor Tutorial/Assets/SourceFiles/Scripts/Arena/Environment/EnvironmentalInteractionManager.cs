using UnityEngine;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Manager global para interacciones ambientales.
    /// Coordina hierba, agua y otros elementos interactivos.
    /// </summary>
    public class EnvironmentalInteractionManager : MonoBehaviour
    {
        public static EnvironmentalInteractionManager Instance { get; private set; }

        [Header("Global Settings")]
        [Tooltip("Activar/desactivar todas las interacciones")]
        [SerializeField] private bool enableInteractions = true;
        
        [Tooltip("Distancia máxima de actualización (culling)")]
        [SerializeField] private float cullDistance = 50f;
        
        [Tooltip("Máximo de elementos actualizados por frame")]
        [SerializeField] private int maxUpdatesPerFrame = 100;

        [Header("Grass Settings")]
        [Tooltip("Activar interacción con hierba")]
        [SerializeField] private bool enableGrassInteraction = true;

        [Header("Water Settings")]
        [Tooltip("Activar interacción con agua")]
        [SerializeField] private bool enableWaterInteraction = true;

        // Referencias a elementos interactivos
        private List<InteractiveGrass> _grassPatches = new List<InteractiveGrass>();
        private List<InteractiveWater> _waterSurfaces = new List<InteractiveWater>();
        private Transform _playerTransform;
        
        // Sistema de culling
        private int _currentUpdateIndex;
        private List<InteractiveGrass> _visibleGrass = new List<InteractiveGrass>();

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
            FindPlayer();
            CacheInteractiveElements();
        }

        private void Update()
        {
            if (!enableInteractions) return;

            UpdateCulling();
            ProcessGrassInteractions();
        }

        /// <summary>
        /// Busca el jugador en la escena
        /// </summary>
        private void FindPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        /// <summary>
        /// Cachea todos los elementos interactivos en la escena
        /// </summary>
        private void CacheInteractiveElements()
        {
            if (enableGrassInteraction)
            {
                _grassPatches.AddRange(FindObjectsByType<InteractiveGrass>(FindObjectsSortMode.None));
            }

            if (enableWaterInteraction)
            {
                _waterSurfaces.AddRange(FindObjectsByType<InteractiveWater>(FindObjectsSortMode.None));
            }

#if DEBUG
            Debug.Log($"[EnvironmentalManager] Cached: {_grassPatches.Count} grass, {_waterSurfaces.Count} water");
#endif
        }

        /// <summary>
        /// Actualiza culling de elementos
        /// </summary>
        private void UpdateCulling()
        {
            if (_playerTransform == null) return;

            Vector3 playerPos = _playerTransform.position;
            
            // Filtrar hierba visible
            _visibleGrass.Clear();
            foreach (var grass in _grassPatches)
            {
                if (grass == null) continue;
                
                float dist = Vector3.Distance(playerPos, grass.transform.position);
                if (dist < cullDistance)
                {
                    _visibleGrass.Add(grass);
                    // Activar componente
                    grass.enabled = true;
                }
                else
                {
                    // Desactivar para ahorrar performance
                    grass.enabled = false;
                }
            }
        }

        /// <summary>
        /// Procesa interacciones de hierba en lotes para performance
        /// </summary>
        private void ProcessGrassInteractions()
        {
            if (!enableGrassInteraction || _visibleGrass.Count == 0) return;

            // Procesar en lotes para distribuir carga
            int batchSize = Mathf.Min(maxUpdatesPerFrame, _visibleGrass.Count);
            
            for (int i = 0; i < batchSize; i++)
            {
                int index = (_currentUpdateIndex + i) % _visibleGrass.Count;
                var grass = _visibleGrass[index];
                
                if (grass != null && grass.enabled)
                {
                    // El componente de hierba se actualiza solo
                    // pero podemos agregar lógica adicional aquí
                }
            }

            _currentUpdateIndex = (_currentUpdateIndex + batchSize) % _visibleGrass.Count;
        }

        /// <summary>
        /// Registra una nueva hierba interactiva
        /// </summary>
        public void RegisterGrass(InteractiveGrass grass)
        {
            if (!_grassPatches.Contains(grass))
            {
                _grassPatches.Add(grass);
            }
        }

        /// <summary>
        /// Registra una nueva superficie de agua
        /// </summary>
        public void RegisterWater(InteractiveWater water)
        {
            if (!_waterSurfaces.Contains(water))
            {
                _waterSurfaces.Add(water);
            }
        }

        /// <summary>
        /// Crea un efecto de fuerza ambiental (explosión, impacto, etc.)
        /// </summary>
        public void ApplyEnvironmentalForce(Vector3 position, float radius, float force)
        {
            // Afectar hierba cercana
            if (enableGrassInteraction)
            {
                Collider[] hits = Physics.OverlapSphere(position, radius, LayerMask.GetMask("Grass"));
                foreach (var hit in hits)
                {
                    var grass = hit.GetComponent<InteractiveGrass>();
                    if (grass != null)
                    {
                        Vector3 dir = (grass.transform.position - position).normalized;
                        float dist = Vector3.Distance(position, grass.transform.position);
                        float normalizedForce = force * (1f - dist / radius);
                        
                        grass.ApplyForce(dir, normalizedForce);
                    }
                }
            }

            // Afectar agua cercana
            if (enableWaterInteraction)
            {
                foreach (var water in _waterSurfaces)
                {
                    if (water == null) continue;
                    
                    float dist = Vector3.Distance(position, water.transform.position);
                    if (dist < radius + maxWaveRadius)
                    {
                        water.CreateWave(position, force * 0.5f);
                    }
                }
            }
        }

        private float maxWaveRadius = 8f; // Referencia para cálculos

        /// <summary>
        /// Spawnea hierba interactiva en un área
        /// </summary>
        public void SpawnGrassInArea(Vector3 center, float radius, int count)
        {
            if (!enableGrassInteraction) return;

            // Esto requeriría un prefab de hierba
            // Implementación básica de ejemplo:
            for (int i = 0; i < count; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * radius;
                Vector3 pos = center + new Vector3(randomCircle.x, 0, randomCircle.y);
                
                // Raycast para encontrar altura del terreno
                if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
                {
                    // Aquí instanciarías el prefab de hierba
                    // var grass = Instantiate(grassPrefab, hit.point, Quaternion.identity);
                    // RegisterGrass(grass.GetComponent<InteractiveGrass>());
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_playerTransform != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_playerTransform.position, cullDistance);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(_playerTransform.position, 
                    _playerTransform.position + Vector3.up * 2f);
            }
        }
    }
}

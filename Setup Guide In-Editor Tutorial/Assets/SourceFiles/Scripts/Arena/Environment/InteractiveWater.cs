using UnityEngine;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de agua interactiva que genera ondas y splashes
    /// cuando entidades entran en contacto.
    /// </summary>
    public class InteractiveWater : MonoBehaviour
    {
        [Header("Wave Settings")]
        [Tooltip("Radio máximo de propagación de ondas")]
        [SerializeField] private float maxWaveRadius = 8f;
        
        [Tooltip("Amplitud máxima de ondas")]
        [SerializeField] private float waveAmplitude = 0.3f;
        
        [Tooltip("Duración de las ondas")]
        [SerializeField] private float waveDuration = 3f;
        
        [Tooltip("Suavizado de ondas")]
        [SerializeField] private float waveSmoothness = 0.5f;

        [Header("Splash Settings")]
        [Tooltip("Prefab de partículas de splash")]
        [SerializeField] private GameObject splashPrefab;
        
        [Tooltip("Escala del splash según velocidad")]
        [SerializeField] private float splashScaleMultiplier = 0.1f;
        
        [Tooltip("Velocidad mínima para generar splash")]
        [SerializeField] private float minVelocityForSplash = 2f;

        [Header("Material Settings")]
        [Tooltip("Material del agua (debe tener shader compatible)")]
        [SerializeField] private Material waterMaterial;
        
        [Tooltip("Color normal del agua")]
        [SerializeField] private Color waterColor = new Color(0.2f, 0.5f, 0.8f, 0.8f);
        
        [Tooltip("Color cuando hay turbulencia")]
        [SerializeField] private Color turbulentColor = new Color(0.25f, 0.55f, 0.85f, 0.85f);

        // Shader property IDs
        private static readonly int WaveCenters = Shader.PropertyToID("_WaveCenters");
        private static readonly int WaveRadii = Shader.PropertyToID("_WaveRadii");
        private static readonly int WaveAmplitudes = Shader.PropertyToID("_WaveAmplitudes");
        private static readonly int WaveCount = Shader.PropertyToID("_WaveCount");
        private static readonly int WaterColorProp = Shader.PropertyToID("_WaterColor");

        // Máximo de ondas simultáneas (limitado por shader)
        private const int MAX_WAVES = 8;
        
        private List<WaveData> _activeWaves = new List<WaveData>();
        private Material _instanceMaterial;
        private Renderer _renderer;
        
        // Trackear entidades en el agua para detectar entrada/salida
        private Dictionary<Transform, Vector3> _trackedEntities = new Dictionary<Transform, Vector3>();

        private struct WaveData
        {
            public Vector3 center;
            public float birthTime;
            public float amplitude;
            public float maxRadius;
        }

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _instanceMaterial = new Material(waterMaterial != null ? waterMaterial : _renderer.material);
                _renderer.material = _instanceMaterial;
            }

            // Asegurar que tenemos collider trigger para detección
            var col = GetComponent<Collider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }
            col.isTrigger = true;
        }

        private void Update()
        {
            UpdateWaves();
            UpdateShaderProperties();
            UpdateEntityTracking();
        }

        /// <summary>
        /// Actualiza el estado de todas las ondas activas
        /// </summary>
        private void UpdateWaves()
        {
            float currentTime = Time.time;
            
            // Remover ondas expiradas
            _activeWaves.RemoveAll(w => currentTime - w.birthTime > waveDuration);
            
            // Limitar cantidad de ondas
            if (_activeWaves.Count > MAX_WAVES)
            {
                _activeWaves.RemoveRange(0, _activeWaves.Count - MAX_WAVES);
            }
        }

        /// <summary>
        /// Envía datos de ondas al shader
        /// </summary>
        private void UpdateShaderProperties()
        {
            if (_instanceMaterial == null) return;

            float currentTime = Time.time;
            Vector4[] centers = new Vector4[MAX_WAVES];
            float[] radii = new float[MAX_WAVES];
            float[] amplitudes = new float[MAX_WAVES];

            int count = Mathf.Min(_activeWaves.Count, MAX_WAVES);
            
            for (int i = 0; i < count; i++)
            {
                var wave = _activeWaves[i];
                float age = currentTime - wave.birthTime;
                
                // Calcular radio actual basado en tiempo
                float progress = age / waveDuration;
                float currentRadius = progress * wave.maxRadius;
                
                // Decay de amplitud basado en tiempo y radio
                float amplitude = wave.amplitude * (1f - progress);
                amplitude *= Mathf.Exp(-currentRadius * waveSmoothness);

                centers[i] = new Vector4(wave.center.x, wave.center.y, wave.center.z, 1f);
                radii[i] = currentRadius;
                amplitudes[i] = amplitude;
            }

            _instanceMaterial.SetVectorArray(WaveCenters, centers);
            _instanceMaterial.SetFloatArray(WaveRadii, radii);
            _instanceMaterial.SetFloatArray(WaveAmplitudes, amplitudes);
            _instanceMaterial.SetInt(WaveCount, count);

            // Color basado en turbulencia
            float turbulence = count / (float)MAX_WAVES;
            _instanceMaterial.SetColor(WaterColorProp, Color.Lerp(waterColor, turbulentColor, turbulence));
        }

        /// <summary>
        /// Trackea entidades para detectar movimiento en el agua
        /// </summary>
        private void UpdateEntityTracking()
        {
            var entitiesToRemove = new List<Transform>();
            
            foreach (var kvp in _trackedEntities)
            {
                Transform entity = kvp.Key;
                Vector3 lastPos = kvp.Value;
                
                if (entity == null)
                {
                    entitiesToRemove.Add(entity);
                    continue;
                }

                Vector3 currentPos = entity.position;
                float velocity = (currentPos - lastPos).magnitude / Time.deltaTime;
                
                // Generar onda según velocidad
                if (velocity > 0.5f)
                {
                    float normalizedVelocity = Mathf.Clamp01(velocity / 10f);
                    CreateWave(currentPos, normalizedVelocity * waveAmplitude);
                }

                _trackedEntities[entity] = currentPos;
            }

            foreach (var entity in entitiesToRemove)
            {
                _trackedEntities.Remove(entity);
            }
        }

        /// <summary>
        /// Crea una nueva onda en la posición especificada
        /// </summary>
        public void CreateWave(Vector3 position, float amplitude)
        {
            // Proyectar posición al plano del agua
            Vector3 localPos = transform.InverseTransformPoint(position);
            localPos.y = 0;
            Vector3 worldCenter = transform.TransformPoint(localPos);

            var wave = new WaveData
            {
                center = worldCenter,
                birthTime = Time.time,
                amplitude = Mathf.Clamp01(amplitude) * waveAmplitude,
                maxRadius = maxWaveRadius
            };

            _activeWaves.Add(wave);
        }

        /// <summary>
        /// Crea un splash en la posición especificada
        /// </summary>
        public void CreateSplash(Vector3 position, float velocity)
        {
            if (splashPrefab == null) return;
            if (velocity < minVelocityForSplash) return;

            // Instanciar splash
            GameObject splash = Instantiate(splashPrefab, position, Quaternion.identity);
            
            // Escalar según velocidad
            float scale = velocity * splashScaleMultiplier;
            splash.transform.localScale = Vector3.one * scale;
            
            // Auto-destruir después de un tiempo
            Destroy(splash, 2f);
        }

        private void OnTriggerEnter(Collider other)
        {
            var combatant = other.GetComponentInParent<ArenaCombatant>();
            if (combatant == null) return;

            Transform entity = combatant.transform;
            
            // Registrar entidad
            if (!_trackedEntities.ContainsKey(entity))
            {
                _trackedEntities.Add(entity, entity.position);
            }

            // Crear onda de entrada
            CreateWave(entity.position, waveAmplitude * 0.5f);
            CreateSplash(entity.position, 5f);
        }

        private void OnTriggerExit(Collider other)
        {
            var combatant = other.GetComponentInParent<ArenaCombatant>();
            if (combatant == null) return;

            Transform entity = combatant.transform;
            
            // Onda de salida
            CreateWave(entity.position, waveAmplitude * 0.3f);
            
            // Remover de tracking
            _trackedEntities.Remove(entity);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.5f, 0.8f, 0.5f);
            Gizmos.DrawWireCube(transform.position, transform.localScale);
            
            // Visualizar ondas activas
            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                float currentTime = Time.time;
                
                foreach (var wave in _activeWaves)
                {
                    float age = currentTime - wave.birthTime;
                    float progress = age / waveDuration;
                    float radius = progress * wave.maxRadius;
                    
                    if (radius > 0.1f)
                    {
                        Gizmos.DrawWireSphere(wave.center, radius);
                    }
                }
            }
        }
    }
}

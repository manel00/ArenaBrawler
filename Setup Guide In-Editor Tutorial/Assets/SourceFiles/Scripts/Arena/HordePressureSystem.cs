using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de presión de horda que escala la dificultad con el tiempo
    /// </summary>
    public class HordePressureSystem : MonoBehaviour
    {
        [Header("Speed Scaling")]
        [Tooltip("Intervalo en segundos entre incrementos de velocidad")]
        [Range(10f, 120f)]
        [SerializeField] private float speedIncreaseInterval = 30f;
        
        [Tooltip("Porcentaje de incremento de velocidad por intervalo")]
        [Range(0.01f, 0.2f)]
        [SerializeField] private float speedIncreasePerInterval = 0.05f;
        
        [Tooltip("Multiplicador máximo de velocidad")]
        [Range(1f, 2f)]
        [SerializeField] private float maxSpeedMultiplier = 1.4f;
        
        [Header("UI")]
        [SerializeField] private UnityEngine.UI.Text intensityText;
        
        private float _currentSpeedMultiplier = 1f;
        private float _lastSpeedIncreaseTime;
        private Dictionary<ArenaCombatant, CachedEnemyComponents> _enemyCache = new Dictionary<ArenaCombatant, CachedEnemyComponents>();
        private StringBuilder _stringBuilder = new StringBuilder(64);
        
        // Cache for enemy components to avoid GetComponent calls
        private class CachedEnemyComponents
        {
            public BotController botController;
            public UnityEngine.AI.NavMeshAgent navAgent;
        }
        
        public static HordePressureSystem Instance { get; private set; }
        
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
            _lastSpeedIncreaseTime = Time.time;
        }
        
        private void Update()
        {
            // Check if it's time to increase speed
            if (Time.time - _lastSpeedIncreaseTime >= speedIncreaseInterval)
            {
                IncreaseSpeed();
                _lastSpeedIncreaseTime = Time.time;
            }
        }
        
        private void IncreaseSpeed()
        {
            if (_currentSpeedMultiplier >= maxSpeedMultiplier) return;
            
            _currentSpeedMultiplier = Mathf.Min(maxSpeedMultiplier, _currentSpeedMultiplier + speedIncreasePerInterval);
            
            // Apply to all cached enemies
            foreach (var kvp in _enemyCache)
            {
                ArenaCombatant enemy = kvp.Key;
                CachedEnemyComponents cached = kvp.Value;
                
                if (enemy != null && enemy.IsAlive)
                {
                    ApplySpeedToCachedEnemy(cached);
                }
            }
            
            Debug.Log($"[HordePressure] Speed increased to {_currentSpeedMultiplier:F2}x");
        }
        
        private void ApplySpeedToCachedEnemy(CachedEnemyComponents cached)
        {
            float baseSpeed = 6.5f;
            float newSpeed = baseSpeed * _currentSpeedMultiplier;
            
            if (cached.botController != null)
            {
                // Note: This requires making moveSpeed public or adding a setter
                // For now, we'll use reflection or a public method
                // Better: Add a SetSpeed method to BotController
            }
            
            if (cached.navAgent != null)
            {
                cached.navAgent.speed = newSpeed;
            }
        }
        
        public void RegisterEnemy(ArenaCombatant enemy)
        {
            if (enemy == null || _enemyCache.ContainsKey(enemy)) return;
            
            CachedEnemyComponents cached = new CachedEnemyComponents
            {
                botController = enemy.GetComponent<BotController>(),
                navAgent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>()
            };
            
            _enemyCache[enemy] = cached;
            ApplySpeedToCachedEnemy(cached);
        }
        
        public void UnregisterEnemy(ArenaCombatant enemy)
        {
            _enemyCache.Remove(enemy);
        }
        
        public void UpdateIntensityDisplay()
        {
            if (intensityText == null) return;
            
            int intensityPercent = Mathf.RoundToInt((_currentSpeedMultiplier - 1f) * 100f);
            
            // Use StringBuilder to avoid GC allocations
            _stringBuilder.Clear();
            _stringBuilder.Append("Intensity: +");
            _stringBuilder.Append(intensityPercent);
            _stringBuilder.Append("%");
            intensityText.text = _stringBuilder.ToString();
            
            // Color based on intensity
            if (intensityPercent < 20)
            {
                intensityText.color = Color.green;
            }
            else if (intensityPercent < 35)
            {
                intensityText.color = Color.yellow;
            }
            else
            {
                intensityText.color = Color.red;
            }
        }
        
        public float GetCurrentSpeedMultiplier() => _currentSpeedMultiplier;
        
        public void ResetPressure()
        {
            _currentSpeedMultiplier = 1f;
            _lastSpeedIncreaseTime = Time.time;
            
            // Reset all cached enemies
            foreach (var kvp in _enemyCache)
            {
                if (kvp.Key != null)
                {
                    ApplySpeedToCachedEnemy(kvp.Value);
                }
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
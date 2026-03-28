using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Zona de fuego que causa daño por tiempo (DoT) y puede activar otros elementos
    /// </summary>
    public class FireZone : MonoBehaviour
    {
        [Header("Fire Settings")]
        [Tooltip("Daño por segundo a entidades en la zona")]
        [Range(1f, 20f)]
        [SerializeField] private float damagePerSecond = 5f;
        
        [Tooltip("Duración del fuego en segundos")]
        [Range(5f, 60f)]
        [SerializeField] private float duration = 15f;
        
        [Tooltip("Radio de la zona de fuego")]
        [Range(1f, 5f)]
        [SerializeField] private float radius = 2f;
        
        
        [Header("Visual Effects")]
        [Tooltip("Efecto visual del fuego")]
        [SerializeField] private GameObject fireEffect;
        
        [Tooltip("Efecto visual del humo")]
        [SerializeField] private GameObject smokeEffect;
        
        private bool _isActive = true;
        private float _activeTimer;
        private HashSet<GameObject> _entitiesInZone = new HashSet<GameObject>();
        private ParticleSystem _fireParticles;
        private Light _fireLight;
        private Coroutine _deactivateCoroutine;
        
        // OverlapSphere buffer to avoid GC allocations
        private readonly Collider[] _overlapBuffer = new Collider[20];
        
        private void Awake()
        {
            // Cache components
            if (fireEffect != null)
            {
                _fireParticles = fireEffect.GetComponent<ParticleSystem>();
            }
            _fireLight = GetComponentInChildren<Light>();
        }
        
        private void OnEnable()
        {
            // Reset state when enabled from pool
            _isActive = true;
            _activeTimer = duration;
            _entitiesInZone.Clear();
            
            if (_fireParticles != null)
            {
                _fireParticles.Play();
            }
            if (_fireLight != null)
            {
                _fireLight.enabled = true;
            }
        }
        
        private void Update()
        {
            if (!_isActive) return;
            
            // Update timer
            _activeTimer -= Time.deltaTime;
            if (_activeTimer <= 0f)
            {
                DeactivateFire();
                return;
            }
            
            // Apply damage to entities in zone
            ApplyDamageToEntities();
            
            // Check for other barrels to ignite
            CheckIgniteNearbyBarrels();
        }
        
        private void ApplyDamageToEntities()
        {
            // Use HashSet to avoid allocations from RemoveAll
            List<GameObject> entitiesToRemove = null;
            
            foreach (GameObject entity in _entitiesInZone)
            {
                if (entity == null)
                {
                    if (entitiesToRemove == null)
                    {
                        entitiesToRemove = new List<GameObject>();
                    }
                    entitiesToRemove.Add(entity);
                    continue;
                }
                
                ArenaCombatant combatant = entity.GetComponent<ArenaCombatant>();
                if (combatant != null)
                {
                    combatant.TakeDamage(damagePerSecond * Time.deltaTime, (GameObject)null);
                }
            }
            
            // Remove null references
            if (entitiesToRemove != null)
            {
                foreach (GameObject entity in entitiesToRemove)
                {
                    _entitiesInZone.Remove(entity);
                }
            }
        }
        
        private void CheckIgniteNearbyBarrels()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, radius, _overlapBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _overlapBuffer[i];
                
                ExplosiveBarrel barrel = col.GetComponent<ExplosiveBarrel>();
                if (barrel != null && !barrel.IsExploded())
                {
                    barrel.ActivateByFire();
                }
                
                SpikeTrap trap = col.GetComponent<SpikeTrap>();
                if (trap != null && trap.IsActive())
                {
                    trap.ActivateByFire();
                }
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;
            
            // Add entity to zone
            _entitiesInZone.Add(other.gameObject);
            
            // Ignite barrels
            ExplosiveBarrel barrel = other.GetComponent<ExplosiveBarrel>();
            if (barrel != null && !barrel.IsExploded())
            {
                barrel.ActivateByFire();
            }
            
            // Activate traps
            SpikeTrap trap = other.GetComponent<SpikeTrap>();
            if (trap != null && trap.IsActive())
            {
                trap.ActivateByFire();
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            // Remove entity from zone
            _entitiesInZone.Remove(other.gameObject);
        }
        
        private void DeactivateFire()
        {
            _isActive = false;
            
            // Stop fire effects
            if (_fireParticles != null)
            {
                _fireParticles.Stop();
            }
            if (_fireLight != null)
            {
                _fireLight.enabled = false;
            }
            
            // Spawn smoke effect
            if (smokeEffect != null)
            {
                GameObject smoke = Instantiate(smokeEffect, transform.position, Quaternion.identity);
                // Smoke should self-destruct
            }
            
            // Return to pool instead of destroying
            _deactivateCoroutine = StartCoroutine(ReturnToPoolAfterDelay(2f));
        }
        
        private IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (EnvironmentSpawner.Instance != null)
            {
                EnvironmentSpawner.Instance.ReturnToPool(gameObject);
            }
            else
            {
                // Fallback: destroy if no pool
                Destroy(gameObject);
            }
        }
        
        public void Reactivate()
        {
            _isActive = true;
            _activeTimer = duration;
            _entitiesInZone.Clear();
            
            // Stop deactivate coroutine if running
            if (_deactivateCoroutine != null)
            {
                StopCoroutine(_deactivateCoroutine);
                _deactivateCoroutine = null;
            }
            
            if (_fireParticles != null)
            {
                _fireParticles.Play();
            }
            if (_fireLight != null)
            {
                _fireLight.enabled = true;
            }
            
            Debug.Log("[FireZone] Reactivated");
        }
        
        public bool IsActive() => _isActive;
        public float GetRemainingTime() => _activeTimer;
        
        private void OnDestroy()
        {
            if (_deactivateCoroutine != null)
            {
                StopCoroutine(_deactivateCoroutine);
            }
        }
    }
}
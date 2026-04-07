using UnityEngine;
using System.Collections;

namespace ArenaEnhanced
{
    /// <summary>
    /// Barril explosivo que puede ser activado por disparos, pisadas de jefes o fuego
    /// </summary>
    public class ExplosiveBarrel : MonoBehaviour
    {
        [Header("Explosion Settings")]
        [Tooltip("Radio de la explosión en unidades")]
        [Range(1f, 10f)]
        [SerializeField] private float explosionRadius = 3f;
        
        [Tooltip("Daño de la explosión")]
        [Range(10f, 100f)]
        [SerializeField] private float explosionDamage = 25f;
        
        [Tooltip("Fuerza de la explosión para empujar objetos")]
        [Range(100f, 1000f)]
        [SerializeField] private float explosionForce = 500f;
        
        [Tooltip("Tiempo de la mecha antes de explotar (0 = instantáneo)")]
        [Range(0f, 5f)]
        [SerializeField] private float fuseTime = 0f;
        
        [Header("Visual Effects")]
        [Tooltip("Efecto visual de explosión")]
        [SerializeField] private GameObject explosionEffect;
        
        [Tooltip("Efecto visual de fuego")]
        [SerializeField] private GameObject fireEffect;
        
        private bool _isExploded = false;
        private bool _fuseLit = false;
        private float _fuseTimer = 0f;
        private MeshRenderer _meshRenderer;
        private Collider _collider;
        private Coroutine _fuseCoroutine;
        
        // OverlapSphere buffer to avoid GC allocations
        private readonly Collider[] _overlapBuffer = new Collider[20];
        
        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _collider = GetComponent<Collider>();
        }
        
        private void OnEnable()
        {
            // Reset state when enabled from pool
            _isExploded = false;
            _fuseLit = false;
            _fuseTimer = 0f;
            
            if (_meshRenderer != null)
            {
                _meshRenderer.enabled = true;
            }
            if (_collider != null)
            {
                _collider.enabled = true;
            }
        }
        
        private void Update()
        {
            if (_fuseLit && !_isExploded)
            {
                _fuseTimer -= Time.deltaTime;
                if (_fuseTimer <= 0f)
                {
                    Explode();
                }
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (_isExploded) return;
            
            // Check if hit by bullet
            if (other.CompareTag("Bullet") || other.CompareTag("Projectile"))
            {
                IgniteFuse();
            }
            
            // Check if hit by fire
            if (other.CompareTag("Fire") || other.CompareTag("FireZone"))
            {
                IgniteFuse();
            }
            
            // Check if stepped by boss (T-Rex)
            if (other.CompareTag("Boss") || other.name.Contains("T-Rex"))
            {
                Explode();
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (_isExploded) return;
            
            // Check if stepped by boss
            if (collision.gameObject.CompareTag("Boss") || collision.gameObject.name.Contains("T-Rex"))
            {
                Explode();
            }
        }
        
        private void IgniteFuse()
        {
            if (_fuseLit || _isExploded) return;
            
            _fuseLit = true;
            _fuseTimer = fuseTime;
            
            if (fireEffect != null)
            {
                GameObject fire = Instantiate(fireEffect, transform.position, Quaternion.identity, transform);
                // Fire will be destroyed with parent
            }
        }
        
        public void Explode()
        {
            if (_isExploded) return;
            
            _isExploded = true;
            
            // Stop fuse coroutine if running
            if (_fuseCoroutine != null)
            {
                StopCoroutine(_fuseCoroutine);
                _fuseCoroutine = null;
            }
            
            // Spawn explosion effect
            if (explosionEffect != null)
            {
                GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
                // Explosion effect should self-destruct
            }
            
            // Apply damage to nearby entities using NonAlloc
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, _overlapBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col.gameObject == gameObject) continue;
                
                // Apply damage to combatants
                ArenaCombatant combatant = col.GetComponent<ArenaCombatant>();
                if (combatant != null)
                {
                    combatant.TakeDamage(explosionDamage, (GameObject)null, DamageType.Fire);
                }
                
                // Apply explosion force to rigidbodies
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 direction = col.transform.position - transform.position;
                    rb.AddForce(direction.normalized * explosionForce);
                }
                
                // Activate other barrels
                ExplosiveBarrel otherBarrel = col.GetComponent<ExplosiveBarrel>();
                if (otherBarrel != null && otherBarrel != this)
                {
                    otherBarrel.IgniteFuse();
                }
            }
            
            // Hide barrel
            if (_meshRenderer != null)
            {
                _meshRenderer.enabled = false;
            }
            if (_collider != null)
            {
                _collider.enabled = false;
            }
            
            // Return to pool instead of destroying
            StartCoroutine(ReturnToPoolAfterDelay(0.5f));
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
        
        public void ActivateByFire()
        {
            IgniteFuse();
        }
        
        public bool IsExploded() => _isExploded;
        
        private void OnDestroy()
        {
            if (_fuseCoroutine != null)
            {
                StopCoroutine(_fuseCoroutine);
            }
        }
    }
}
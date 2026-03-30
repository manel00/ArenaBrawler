using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Componente para proyectiles que se integra con el sistema de Object Pooling.
    /// Reemplaza la destrucción automática por retorno al pool.
    /// </summary>
    public class PooledProjectile : PooledObject
    {
        [Header("Projectile Settings")]
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private float speed = 40f;
        [SerializeField] private bool returnOnHit = true;
        
        [Header("VFX")]
        [SerializeField] private GameObject impactEffectPrefab;
        
        private float _spawnTime;
        private Rigidbody _rb;
        private bool _hasHit;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            
            _spawnTime = Time.time;
            _hasHit = false;
            
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        public override void OnReturnToPool()
        {
            base.OnReturnToPool();
            
            // Detener cualquier movimiento
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            
            _hasHit = false;
        }

        private void Update()
        {
            // Retornar al pool si excede lifetime
            if (Time.time - _spawnTime > lifetime)
            {
                ReturnToPool();
            }
        }

        /// <summary>
        /// Inicializa el proyectil con velocidad y dirección
        /// </summary>
        public void Launch(Vector3 direction, float customSpeed = -1)
        {
            float launchSpeed = customSpeed > 0 ? customSpeed : speed;
            
            if (_rb != null)
            {
                _rb.linearVelocity = direction * launchSpeed;
            }
            else
            {
                // Si no tiene Rigidbody, mover manualmente
                transform.position += direction * launchSpeed * Time.deltaTime;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit && returnOnHit) return;
            
            _hasHit = true;

            // Spawn impact effect si está configurado
            if (impactEffectPrefab != null)
            {
                // Usar pool para el efecto también si está disponible
                if (GenericObjectPool.Instance != null && 
                    GenericObjectPool.Instance.HasPool("ImpactEffect"))
                {
                    GameObject effect = GenericObjectPool.Instance.GetFromPool(
                        "ImpactEffect", 
                        transform.position, 
                        Quaternion.identity);
                    
                    if (effect != null)
                    {
                        // Auto-return después de un tiempo
                        var returnAfterTime = effect.GetComponent<ReturnToPoolAfterTime>();
                        if (returnAfterTime == null)
                        {
                            returnAfterTime = effect.AddComponent<ReturnToPoolAfterTime>();
                        }
                        returnAfterTime.delay = 2f;
                    }
                }
                else
                {
                    Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
                }
            }

            if (returnOnHit)
            {
                ReturnToPool();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasHit && returnOnHit) return;
            
            _hasHit = true;

            if (returnOnHit)
            {
                ReturnToPool();
            }
        }
    }

    /// <summary>
    /// Componente helper para retornar objetos al pool después de un tiempo
    /// </summary>
    public class ReturnToPoolAfterTime : MonoBehaviour
    {
        public float delay = 2f;
        private float _startTime;

        private void OnEnable()
        {
            _startTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - _startTime > delay)
            {
                var pooledObj = GetComponent<PooledObject>();
                if (pooledObj != null)
                {
                    pooledObj.ReturnToPool();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}

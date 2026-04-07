using System.Collections;
using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Proyectil de granada que explota al impactar con el suelo u objetos.
    /// Daño de área: 30 puntos en radio de 3 metros.
    /// </summary>
    public class GrenadeProjectile : MonoBehaviour
    {
        [Header("Explosion Settings")]
        [Tooltip("Radio de daño de la explosión")]
        [SerializeField] private float explosionRadius = 3f;
        
        [Tooltip("Daño máximo en el centro de la explosión")]
        [SerializeField] private float maxDamage = 30f;
        
        [Tooltip("Daño mínimo en el borde del radio")]
        [SerializeField] private float minDamage = 10f;
        
        [Tooltip("Fuerza de empuje (knockback) aplicada a los combatientes")]
        [SerializeField] private float knockbackForce = 300f;
        
        [Tooltip("Tiempo antes de auto-destruir si no explota")]
        [SerializeField] private float maxLifetime = 10f;
        
        [Tooltip("Delay antes de la explosión después del impacto")]
        [SerializeField] private float explosionDelay = 0.1f;
        
        [Tooltip("Layers que detonan la granada")]
        [SerializeField] private LayerMask detonationLayers;

        [Header("Visual Effects")]
        [Tooltip("Escala del efecto de explosión")]
        [SerializeField] private float explosionVFXScale = 1.5f;
        
        [Tooltip("Color del trail de la granada")]
        [SerializeField] private Color trailColor = new Color(1f, 0.2f, 0.2f, 0.8f);

        // Runtime state
        private ArenaCombatant _owner;
        private Rigidbody _rb;
        private TrailRenderer _trail;
        private bool _hasExploded = false;
        private bool _isArmed = false;
        private float _spawnTime;

        // Componentes de FX
        private AudioSource _audioSource;
        private ParticleSystem _fuseParticles;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            SetupComponents();
        }

        private void SetupComponents()
        {
            // Trail renderer para seguimiento visual
            _trail = GetComponent<TrailRenderer>();
            if (_trail == null)
            {
                _trail = gameObject.AddComponent<TrailRenderer>();
                _trail.time = 0.5f;
                _trail.startWidth = 0.15f;
                _trail.endWidth = 0.05f;
                _trail.material = new Material(Shader.Find("Sprites/Default"));
                _trail.startColor = trailColor;
                _trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
            }

            // Audio source para sonidos
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f;
                _audioSource.maxDistance = 50f;
            }

            // Configurar detonation layers por defecto si no están seteadas
            if (detonationLayers == 0)
            {
                detonationLayers = LayerMask.GetMask("Ground", "Default", "Environment", "Destructible");
            }
        }

        private void OnEnable()
        {
            _spawnTime = Time.time;
            _hasExploded = false;
            _isArmed = false;
            
            // Armar después de un pequeño delay (para evitar colisión inmediata con el lanzador)
            Invoke(nameof(ArmGrenade), 0.1f);
        }

        private void Update()
        {
            // Auto-destruir si excede tiempo de vida
            if (Time.time - _spawnTime > maxLifetime && !_hasExploded)
            {
                Explode();
            }
        }

        private void ArmGrenade()
        {
            _isArmed = true;
        }

        /// <summary>
        /// Inicializa el proyectil con el owner y velocidad inicial.
        /// </summary>
        public void Initialize(ArenaCombatant owner, Vector3 velocity)
        {
            _owner = owner;
            
            if (_rb != null)
            {
                _rb.linearVelocity = velocity;
                _rb.angularVelocity = Random.insideUnitSphere * 10f; // Rotación aleatoria
            }

            // Ignorar colisiones con el owner
            if (_owner != null)
            {
                var ownerColliders = _owner.GetComponentsInChildren<Collider>();
                var grenadeCollider = GetComponent<Collider>();
                
                if (grenadeCollider != null)
                {
                    foreach (var col in ownerColliders)
                    {
                        Physics.IgnoreCollision(col, grenadeCollider);
                    }
                }
            }

#if DEBUG
            Debug.Log($"[GrenadeProjectile] Initialized - Owner: {owner?.displayName}, Velocity: {velocity.magnitude:F1}");
#endif
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isArmed || _hasExploded) return;

            // Verificar si el layer puede detonar la granada
            if ((detonationLayers.value & (1 << collision.gameObject.layer)) != 0)
            {
                // Pequeño delay para que el proyectil "toque" el suelo antes de explotar
                StartCoroutine(ExplodeAfterDelay(explosionDelay));
            }
        }

        private IEnumerator ExplodeAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (!_hasExploded)
            {
                Explode();
            }
        }

        private void Explode()
        {
            if (_hasExploded) return;
            _hasExploded = true;

            Vector3 explosionCenter = transform.position;

#if DEBUG
            Debug.Log($"[GrenadeProjectile] EXPLOSION at {explosionCenter} - Radius: {explosionRadius}m");
#endif

            // 1. Aplicar daño de área
            AreaDamageHelper.ApplyAreaDamage(
                center: explosionCenter,
                radius: explosionRadius,
                minDamage: minDamage,
                maxDamage: maxDamage,
                owner: _owner,
                damageType: DamageType.Normal
            );

            // 2. Aplicar knockback
            AreaDamageHelper.ApplyAreaDamageWithKnockback(
                center: explosionCenter,
                radius: explosionRadius,
                damage: 0f, // Daño ya aplicado arriba
                knockbackForce: knockbackForce,
                owner: _owner
            );

            // 3. Spawn efectos visuales
            SpawnExplosionVFX(explosionCenter);

            // 4. Efectos de sonido
            PlayExplosionSound(explosionCenter);

            // 5. Notificar a listeners
            OnExploded?.Invoke(explosionCenter, explosionRadius);

            // Destruir o retornar al pool
            ReturnOrDestroy();
        }

        private void SpawnExplosionVFX(Vector3 position)
        {
            VFXManager.SpawnExplosionEffect(position, explosionVFXScale);

            var cameraShake = Camera.main?.GetComponent<CameraShake>();
            if (cameraShake != null)
            {
                cameraShake.Shake(0.3f, 0.2f);
            }
        }

        private void PlayExplosionSound(Vector3 position)
        {
            ArenaAudioManager.PlayExplosionSound(position);
        }

        private void ReturnOrDestroy()
        {
            if (_trail != null)
                _trail.enabled = false;

            var pooled = GetComponent<PooledObject>();
            if (pooled != null)
            {
                pooled.ReturnToPool();
            }
            else
            {
                Destroy(gameObject, 0.1f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            AreaDamageHelper.DrawDebugGizmos(transform.position, explosionRadius, new Color(1f, 0.5f, 0f, 0.3f));
        }

        public System.Action<Vector3, float> OnExploded;
    }
}

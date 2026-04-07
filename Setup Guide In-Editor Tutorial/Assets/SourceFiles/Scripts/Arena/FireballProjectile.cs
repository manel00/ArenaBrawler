using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ArenaEnhanced
{
    /// <summary>
    /// Fireball premium usando efecto Fly03-05 de KoreanTraditionalPattern_Effect.
    /// Proyectil físico con daño directo + AoE, movimiento horizontal exclusivo.
    /// </summary>
    public class FireballProjectile : MonoBehaviour
    {
        [Header("Damage")]
        public float directDamage = 20f;
        public float splashDamage = 8f;
        public float splashRadius = 5f;
        
        [Header("Movement")]
        public float speed = 25f;
        public float maxLifetime = 3f;
        
        [Header("Visual")]
        public string effectPrefabPath = "KoreanTraditionalPattern_Effect/Prefabs/Fly/Fly03-05";
        public float effectScale = 1.5f;
        public bool forcePlayParticles = true;
        
        private Material _cachedFireballMaterial;
        
        [Header("Explosion")]
        public GameObject explosionPrefab;
        public string explosionPath = "KoreanTraditionalPattern_Effect/Prefabs/Hit/Hit01-01";
        
        public ArenaCombatant owner { get; set; }
        
        private Rigidbody _rb;
        private SphereCollider _collider;
        private GameObject _visualEffect;
        private float _spawnTime;
        private bool _hasHit;
        private Vector3 _moveDirection;
        
        void Awake()
        {
            SetupPhysics();
        }
        
        void OnEnable()
        {
            _spawnTime = Time.time;
            _hasHit = false;
            _moveDirection = transform.forward;
            _moveDirection.y = 0;
            _moveDirection.Normalize();
            
            SpawnVisualEffect();
            ApplyInitialVelocity();
        }
        
        void SetupPhysics()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
            }
            _rb.useGravity = false;
            _rb.mass = 0.5f;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.constraints = RigidbodyConstraints.FreezePositionY;
            
            _collider = GetComponent<SphereCollider>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<SphereCollider>();
            }
            _collider.isTrigger = true;
            _collider.radius = 0.6f;
            _collider.center = Vector3.zero;
        }
        
        void SpawnVisualEffect()
        {
            if (_visualEffect != null)
            {
                Destroy(_visualEffect);
            }
            
            // Usar visual simple de esferas - el prefab KoreanTraditional es demasiado complejo
            CreateFallbackVisual();
            
            // Añadir luz dinámica
            if (_visualEffect != null)
            {
                var light = _visualEffect.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.5f, 0.2f);
                light.intensity = 2f;
                light.range = 6f;
                light.shadows = LightShadows.None;
            }
        }
        
        private void CreateFallbackVisual()
        {
            _visualEffect = new GameObject("FireballVisual");
            _visualEffect.transform.SetParent(transform, false);
            _visualEffect.transform.localPosition = Vector3.zero;
            _visualEffect.transform.localScale = Vector3.one * effectScale;
            
            // Esfera exterior (naranja con brillo)
            var outerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            outerSphere.name = "FireballOuter";
            outerSphere.transform.SetParent(_visualEffect.transform, false);
            outerSphere.transform.localPosition = Vector3.zero;
            outerSphere.transform.localScale = Vector3.one * 0.5f;
            Destroy(outerSphere.GetComponent<Collider>());
            
            var outerRenderer = outerSphere.GetComponent<Renderer>();
            var unlitShader = Shader.Find("Unlit/Color");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            if (unlitShader == null) unlitShader = Shader.Find("Standard");
            
            var outerMat = new Material(unlitShader);
            outerMat.color = new Color(1f, 0.3f, 0f, 0.6f);
            outerRenderer.material = outerMat;
            
            // Esfera central (naranja brillante)
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "FireballCore";
            sphere.transform.SetParent(_visualEffect.transform, false);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 0.35f;
            Destroy(sphere.GetComponent<Collider>());
            
            var renderer = sphere.GetComponent<Renderer>();
            var material = new Material(unlitShader);
            material.color = new Color(1f, 0.5f, 0.1f, 0.9f);
            renderer.material = material;
            
            // Esfera interior (amarillo/centro)
            var innerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            innerSphere.name = "FireballInner";
            innerSphere.transform.SetParent(_visualEffect.transform, false);
            innerSphere.transform.localPosition = Vector3.zero;
            innerSphere.transform.localScale = Vector3.one * 0.2f;
            Destroy(innerSphere.GetComponent<Collider>());
            
            var innerRenderer = innerSphere.GetComponent<Renderer>();
            var innerMaterial = new Material(unlitShader);
            innerMaterial.color = new Color(1f, 0.9f, 0.3f, 1f);
            innerRenderer.material = innerMaterial;
            
            // Trail de fuego
            var trail = _visualEffect.AddComponent<TrailRenderer>();
            trail.time = 0.3f;
            trail.startWidth = 0.4f;
            trail.endWidth = 0.05f;
            trail.startColor = new Color(1f, 0.4f, 0f, 0.8f);
            trail.endColor = new Color(1f, 0.1f, 0f, 0f);
            
            var trailMat = new Material(unlitShader);
            trailMat.color = new Color(1f, 0.3f, 0f, 0.6f);
            trail.material = trailMat;
        }
        
        void ApplyInitialVelocity()
        {
            if (_rb != null)
            {
                _rb.linearVelocity = _moveDirection * speed;
            }
        }
        
        void Update()
        {
            // Lifetime check
            if (Time.time - _spawnTime > maxLifetime)
            {
                ReturnToPoolOrDestroy();
                return;
            }
            
            // Mantener movimiento horizontal
            if (_rb != null)
            {
                Vector3 currentVel = _rb.linearVelocity;
                currentVel.y = 0;
                
                // Si la velocidad ha disminuido demasiado, reaplicar
                if (currentVel.magnitude < speed * 0.8f)
                {
                    currentVel = _moveDirection * speed;
                }
                
                _rb.linearVelocity = currentVel;
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (_hasHit) return;
            
            // Ignorar owner
            if (other.GetComponentInParent<ArenaCombatant>() == owner) return;
            if (other.transform.root.GetComponent<ArenaCombatant>() == owner) return;
            
            // Ignorar triggers de armas/pickups
            if (other.name.Contains("Pickup") || other.name.Contains("Weapon")) return;
            
            // Ignorar suelo durante primeros 0.1s (evitar colisión inmediata con suelo)
            if (Time.time - _spawnTime < 0.1f && other.gameObject.layer == LayerMask.NameToLayer("Ground")) return;
            
            ProcessHit(other);
        }
        
        void ProcessHit(Collider hitCollider)
        {
            _hasHit = true;
            
            Vector3 hitPos = transform.position;
            
            // Daño directo
            var target = hitCollider.GetComponentInParent<ArenaCombatant>();
            if (target != null && target != owner && target.IsAlive)
            {
                target.TakeDamage(directDamage, owner, DamageType.Fire);
            }
            
            // Daño de área (AoE)
            ApplyAreaDamage(hitPos);
            
            // Efecto de explosión
            SpawnExplosion(hitPos);
            
            // Retornar al pool o destruir
            ReturnToPoolOrDestroy();
        }
        
        void ApplyAreaDamage(Vector3 center)
        {
            // OPTIMIZACIÓN: Usar PhysicsLayers.CombatantMask en lugar de ~0
            Collider[] hits = Physics.OverlapSphere(center, splashRadius, PhysicsLayers.CombatantMask, QueryTriggerInteraction.Ignore);
            HashSet<ArenaCombatant> damagedTargets = new HashSet<ArenaCombatant>();
            
            foreach (var hit in hits)
            {
                var combatant = hit.GetComponentInParent<ArenaCombatant>();
                if (combatant != null && combatant != owner && combatant.IsAlive)
                {
                    if (!damagedTargets.Contains(combatant))
                    {
                        damagedTargets.Add(combatant);
                        combatant.TakeDamage(splashDamage, owner, DamageType.Fire);
                    }
                }
            }
        }
        
        void SpawnExplosion(Vector3 position)
        {
            // A) EXPLOSION MAS GRANDE - crear multiples esferas de explosion
            float explosionScale = 3f; // Escala aumentada de 2 a 3
            
            // Esfera principal de explosion (naranja)
            var explosionMain = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            explosionMain.name = "ExplosionMain";
            explosionMain.transform.position = position;
            explosionMain.transform.localScale = Vector3.one * explosionScale;
            Object.Destroy(explosionMain.GetComponent<Collider>());
            
            var mainRenderer = explosionMain.GetComponent<Renderer>();
            var unlitShader = Shader.Find("Unlit/Color");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            if (unlitShader == null) unlitShader = Shader.Find("Standard");
            
            var mainMat = new Material(unlitShader);
            mainMat.color = new Color(1f, 0.4f, 0f, 0.9f);
            mainRenderer.material = mainMat;
            Object.Destroy(explosionMain, 1.5f);
            
            // Segunda esfera interior (roja)
            var explosionInner = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            explosionInner.name = "ExplosionInner";
            explosionInner.transform.position = position;
            explosionInner.transform.localScale = Vector3.one * (explosionScale * 0.6f);
            Object.Destroy(explosionInner.GetComponent<Collider>());
            
            var innerRenderer = explosionInner.GetComponent<Renderer>();
            var innerMat = new Material(unlitShader);
            innerMat.color = new Color(1f, 0.8f, 0.2f, 1f);
            innerRenderer.material = innerMat;
            Object.Destroy(explosionInner, 1.5f);
            
            // Tercera esfera exterior (humo oscuro)
            var explosionOuter = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            explosionOuter.name = "ExplosionOuter";
            explosionOuter.transform.position = position;
            explosionOuter.transform.localScale = Vector3.one * (explosionScale * 1.2f);
            Object.Destroy(explosionOuter.GetComponent<Collider>());
            
            var outerRenderer = explosionOuter.GetComponent<Renderer>();
            var outerMat = new Material(unlitShader);
            outerMat.color = new Color(0.3f, 0.1f, 0f, 0.5f);
            outerRenderer.material = outerMat;
            Object.Destroy(explosionOuter, 1.5f);
            
            // Luz de explosion
            var lightObj = new GameObject("ExplosionLight");
            lightObj.transform.position = position;
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.5f, 0.2f);
            light.intensity = 5f;
            light.range = 12f;
            Object.Destroy(lightObj, 0.5f);
            
            // Intentar cargar prefab adicional si existe
            GameObject prefab = null;
            if (!string.IsNullOrEmpty(explosionPath))
            {
                prefab = Resources.Load<GameObject>(explosionPath);
            }
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("KoreanTraditionalPattern_Effect/Prefabs/Hit/Hit02-02");
            }
            if (prefab != null)
            {
                GameObject explosion = Instantiate(prefab, position, Quaternion.identity);
                explosion.transform.localScale = Vector3.one * explosionScale;
                var particleSystems = explosion.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in particleSystems)
                {
                    ps.Play(true);
                }
                Destroy(explosion, 2f);
            }
        }
        
        void OnDestroy()
        {
            // Limpiar efecto visual
            if (_visualEffect != null)
            {
                Destroy(_visualEffect);
            }
        }
        
        void ReturnToPoolOrDestroy()
        {
            // Intentar retornar al pool si es un objeto pooleado
            var pooledObj = GetComponent<PooledObject>();
            if (pooledObj != null && GenericObjectPool.Instance != null)
            {
                pooledObj.ReturnToPool();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, splashRadius);
        }
    }
}

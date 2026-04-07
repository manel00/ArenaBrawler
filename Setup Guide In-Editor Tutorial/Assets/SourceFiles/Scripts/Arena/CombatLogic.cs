using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ArenaEnhanced
{
    public class MeleeAttack : MonoBehaviour
    {
        public ArenaCombatant owner;
        public float damage = 15f;
        public float knockback = 12f;
        public float duration = 0.3f;
        private float _startTime;
        private BoxCollider _triggerCollider;
        private HashSet<ArenaCombatant> _hitTargets = new HashSet<ArenaCombatant>();

        private void Start()
        {
            _startTime = Time.time;
            SetupTriggerCollider();
        }

        private void SetupTriggerCollider()
        {
            _triggerCollider = gameObject.AddComponent<BoxCollider>();
            _triggerCollider.isTrigger = true;
            _triggerCollider.size = new Vector3(2.5f, 2f, 2f);
            _triggerCollider.center = Vector3.zero;
            Destroy(gameObject, duration);
        }

        private void Update()
        {
            if (Time.time - _startTime > duration)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (owner == null) return;
            var target = other.GetComponentInParent<ArenaCombatant>();
            if (target == null || !target.IsAlive || target == owner || target.teamId == owner.teamId) return;
            if (_hitTargets.Contains(target)) return;
            _hitTargets.Add(target);
            target.TakeDamage(damage * owner.damageMultiplier, owner);
            VFXManager.SpawnImpactEffect(target.transform.position + Vector3.up);
            Vector3 knockbackDir = (target.transform.position - owner.transform.position).normalized;
            knockbackDir.y = 0.3f;
            target.ApplyKnockback(knockbackDir * knockback * 100f);
        }
    }

    public enum PickupType { Heal, DamageBuff }

    public class ArenaPickup : MonoBehaviour
    {
        public PickupType type = PickupType.Heal;
        public float healAmount = 25f;
        public float buffMultiplier = 1.5f;
        public float buffDuration = 10f;
        public float rotationSpeed = 90f;
        public float bobAmp = 0.2f;
        public float bobSpeed = 2f;
        public float lifeTime = 20f;
        private Vector3 _basePos;

        private void Start()
        {
            _basePos = transform.position;
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            transform.position = new Vector3(_basePos.x, _basePos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmp, _basePos.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            var c = other.GetComponentInParent<ArenaCombatant>();
            if (c == null || !c.IsAlive) return;
            if (type == PickupType.Heal) c.Heal(healAmount);
            ArenaAudioManager.PlayPickup();
            Destroy(gameObject);
        }
    }

    public class DamageNumber : MonoBehaviour
    {
        public float lifetime = 1f;
        private float _startTime;
        private Vector3 _velocity;

        private void Start()
        {
            _startTime = Time.time;
            _velocity = Vector3.up * 3f + Random.insideUnitSphere * 0.5f;
        }

        private void Update()
        {
            float t = (Time.time - _startTime) / lifetime;
            if (t >= 1f) { Destroy(gameObject); return; }
            transform.position += _velocity * Time.deltaTime;
            _velocity.y -= 2f * Time.deltaTime;
            var text = GetComponent<TextMesh>();
            if (text != null)
            {
                Color c = text.color;
                c.a = 1f - t;
                text.color = c;
            }
        }
    }

    public static class RuntimeSpawner
    {
        // CACHE: Materiales reutilizables para evitar leaks y GC pressure
        private static Material _cachedTrailMaterial;
        private static Material _cachedProjectileTrailMaterial;
        private static Material _cachedDogMaterial;
        private static readonly Color FireballTrailStartColor = new Color(1f, 0.4f, 0f, 1f);
        private static readonly Color FireballTrailEndColor = new Color(1f, 0.1f, 0f, 0f);
        private static readonly Color ProjectileTrailStartColor = new Color(1f, 0.2f, 0.2f, 1f);
        
        private static Material GetTrailMaterial()
        {
            if (_cachedTrailMaterial == null)
            {
                // Intentar URP primero
                var urpShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (urpShader != null)
                {
                    _cachedTrailMaterial = new Material(urpShader);
                }
                else
                {
                    // Fallback a shaders estándar
                    var standardShader = Shader.Find("Particles/Standard Unlit");
                    if (standardShader == null)
                        standardShader = Shader.Find("Mobile/Particles/Alpha-Blended");
                    if (standardShader == null)
                        standardShader = Shader.Find("Sprites/Default");
                    
                    _cachedTrailMaterial = new Material(standardShader);
                }
                
                _cachedTrailMaterial.SetColor("_Color", new Color(1f, 0.4f, 0f, 0.8f));
            }
            return _cachedTrailMaterial;
        }
        
        private static Material GetProjectileTrailMaterial()
        {
            if (_cachedProjectileTrailMaterial == null)
            {
                // Usar shader básico compatible con todos los pipelines
                var shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                if (shader == null)
                    shader = Shader.Find("Standard");
                
                _cachedProjectileTrailMaterial = new Material(shader);
                _cachedProjectileTrailMaterial.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            }
            return _cachedProjectileTrailMaterial;
        }
        
        private static Material GetDogMaterial()
        {
            if (_cachedDogMaterial == null)
            {
                _cachedDogMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (_cachedDogMaterial == null || _cachedDogMaterial.shader.name.Contains("Error"))
                    _cachedDogMaterial = new Material(Shader.Find("Standard"));
                if (_cachedDogMaterial == null || _cachedDogMaterial.shader.name.Contains("Error"))
                    _cachedDogMaterial = new Material(Shader.Find("Diffuse"));
                _cachedDogMaterial.color = new Color(0.65f, 0.45f, 1f);
            }
            return _cachedDogMaterial;
        }

        public static void SpawnFireball(ArenaCombatant owner, Vector3 origin, Vector3 direction, float speed)
        {
            Vector3 spawnOrigin = origin + direction.normalized * 1.5f;
            spawnOrigin.y = 1.5f;
            
            // Intentar usar el sistema de pooling primero
            if (GenericObjectPool.Instance != null && GenericObjectPool.Instance.HasPool("Fireball"))
            {
                GameObject go = GenericObjectPool.Instance.GetFromPool("Fireball", spawnOrigin, Quaternion.LookRotation(direction));
                if (go != null)
                {
                    var projectile = go.GetComponent<FireballProjectile>();
                    if (projectile != null)
                    {
                        projectile.owner = owner;
                    }
                    
                    if (owner != null)
                    {
                        var ownerCol = owner.GetComponent<Collider>();
                        var projCol = go.GetComponent<Collider>();
                        if (ownerCol != null && projCol != null)
                            Physics.IgnoreCollision(ownerCol, projCol);
                    }
                    
                    ArenaAudioManager.PlayFireball();
                    return;
                }
            }

            // Crear fireball con nuevo FireballProjectile
            var goNew = new GameObject("Fireball");
            goNew.transform.position = spawnOrigin;
            goNew.transform.rotation = Quaternion.LookRotation(direction);

            // Añadir Rigidbody y configuración física (FireballProjectile se encarga de esto en Awake)
            var rbNew = goNew.AddComponent<Rigidbody>();
            rbNew.mass = 0.5f;
            rbNew.useGravity = false;
            rbNew.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Añadir el componente FireballProjectile
            var fireball = goNew.AddComponent<FireballProjectile>();
            fireball.owner = owner;

            if (owner != null)
            {
                var ownerCol = owner.GetComponent<Collider>();
                if (ownerCol != null)
                {
                    var projCol = goNew.GetComponent<Collider>();
                    if (projCol != null)
                        Physics.IgnoreCollision(ownerCol, projCol);
                }
            }

            ArenaAudioManager.PlayFireball();
        }
        
        private static void CreateFireballVFX(Transform parent)
        {
            // Sistema de partículas para el núcleo del fireball
            var core = new GameObject("FireballCore");
            core.transform.SetParent(parent, false);
            
            var ps = core.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 3f;
            main.startLifetime = 0.5f;
            main.startSize = 0.6f;
            main.startColor = new Color(1f, 0.4f, 0f, 0.9f);
            main.maxParticles = 30;
            main.playOnAwake = true;
            main.loop = true;
            
            var emission = ps.emission;
            emission.rateOverTime = 25f;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[] { 
                    new GradientColorKey(new Color(1f, 0.6f, 0f), 0f),
                    new GradientColorKey(new Color(1f, 0.2f, 0f), 1f)
                },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);
            
            // Trail renderer para la estela - usando material cacheado
            var trail = parent.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.3f;
            trail.startWidth = 0.4f;
            trail.endWidth = 0.05f;
            trail.material = GetTrailMaterial();
            trail.startColor = FireballTrailStartColor;
            trail.endColor = FireballTrailEndColor;
            
            ps.Play();
        }

        public static void SpawnWeaponProjectile(ArenaCombatant owner, Vector3 origin, Vector3 direction, WeaponData weaponData)
        {
            Debug.Log($"[SpawnWeaponProjectile] origin={origin}, direction={direction}, weapon={weaponData?.weaponName}");
            
            // Asegurar dirección estrictamente horizontal
            Vector3 horizontalDir = direction;
            horizontalDir.y = 0f;
            horizontalDir.Normalize();
            
            // Asegurar posición Y exacta del origen
            Vector3 spawnPos = origin;
            spawnPos.y = origin.y;
            
            string poolTag = weaponData != null && weaponData.type == WeaponType.Flamethrower ? "FlameProjectile" : "WeaponProjectile";
            if (GenericObjectPool.Instance != null && GenericObjectPool.Instance.HasPool(poolTag))
            {
                GameObject go = GenericObjectPool.Instance.GetFromPool(poolTag, spawnPos, Quaternion.LookRotation(horizontalDir));
                if (go != null)
                {
                    // FORZAR posición y rotación exactas
                    go.transform.position = spawnPos;
                    go.transform.rotation = Quaternion.LookRotation(horizontalDir, Vector3.up);
                    
                    var rb = go.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.Sleep(); // Resetear física completamente
                        rb.WakeUp();
                        rb.linearVelocity = horizontalDir * (weaponData != null ? weaponData.projectileSpeed : 35f);
                        Debug.Log($"[SpawnWeaponProjectile] POOL - velocity set to {rb.linearVelocity}");
                    }
                    var projectile = go.GetComponent<WeaponProjectile>();
                    if (projectile != null)
                    {
                        projectile.owner = owner;
                        projectile.weaponData = weaponData;
                    }
                    if (owner != null)
                    {
                        var ownerCol = owner.GetComponent<Collider>();
                        var projCol = go.GetComponent<Collider>();
                        if (ownerCol != null && projCol != null)
                            Physics.IgnoreCollision(ownerCol, projCol);
                    }
                    return;
                }
            }

            // Fallback: Crear proyectil
            var goNew = new GameObject($"Projectile_{(weaponData != null ? weaponData.weaponName : "Weapon")}");
            goNew.transform.position = spawnPos;
            goNew.transform.localScale = Vector3.one * 0.25f;
            goNew.transform.rotation = Quaternion.LookRotation(horizontalDir, Vector3.up);
            
            // Crear visual
            CreateProjectileVFX(goNew.transform, weaponData);
            
            var rbNew = goNew.AddComponent<Rigidbody>();
            rbNew.mass = 0.2f;
            rbNew.useGravity = false;
            rbNew.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rbNew.constraints = RigidbodyConstraints.FreezeRotation;
            
            float speed = weaponData != null ? weaponData.projectileSpeed : 35f;
            rbNew.linearVelocity = horizontalDir * speed;
            
            Debug.Log($"[SpawnWeaponProjectile] FALLBACK CREATED - pos={goNew.transform.position}, vel={rbNew.linearVelocity}");
            
            var sphereCol = goNew.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 0.25f;
            
            var projectileNew = goNew.AddComponent<WeaponProjectile>();
            projectileNew.owner = owner;
            projectileNew.weaponData = weaponData;
            
            if (owner != null)
            {
                var ownerColliders = owner.GetComponentsInChildren<Collider>();
                var projCol = goNew.GetComponent<Collider>();
                if (projCol != null)
                {
                    foreach (var c in ownerColliders)
                        Physics.IgnoreCollision(c, projCol);
                }
            }
        }
        
        private static void CreateProjectileVFX(Transform parent, WeaponData weaponData)
        {
            Color projectileColor = new Color(0.9f, 0.1f, 0.1f); // ROJO intenso para balas
            if (weaponData != null && weaponData.type == WeaponType.Flamethrower)
            {
                projectileColor = new Color(1f, 0.5f, 0.1f);
            }
            
            // Sistema de partículas en lugar de esfera primitiva
            var ps = CreateProjectileParticleSystem(parent, projectileColor);
            
            // Trail renderer simple
            var trail = parent.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.3f;
            trail.startWidth = 0.2f;
            trail.endWidth = 0.05f;
            trail.startColor = new Color(projectileColor.r, projectileColor.g, projectileColor.b, 1f);
            trail.endColor = new Color(projectileColor.r * 0.5f, 0f, 0f, 0f);
            
            var unlitShader = Shader.Find("Unlit/Color");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            if (unlitShader == null) unlitShader = Shader.Find("Standard");
            
            var trailMat = new Material(unlitShader);
            trailMat.color = new Color(projectileColor.r, projectileColor.g, projectileColor.b, 0.8f);
            trail.material = trailMat;
        }
        
        private static ParticleSystem CreateProjectileParticleSystem(Transform parent, Color color)
        {
            var go = new GameObject("ProjectileVisual");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 3f;
            main.startLifetime = 0.3f;
            main.startSize = 0.15f;
            main.startColor = new Color(color.r, color.g, color.b, 1f);
            main.maxParticles = 10;
            main.playOnAwake = true;
            main.loop = true;
            
            var emission = ps.emission;
            emission.rateOverTime = 20f;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[] { 
                    new GradientColorKey(new Color(color.r, color.g, color.b), 0f),
                    new GradientColorKey(new Color(color.r * 0.5f, color.g * 0.3f, 0f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);
            
            ps.Play();
            return ps;
        }

        public static void SpawnMelee(ArenaCombatant owner, Vector3 position, Vector3 forward)
        {
            var go = new GameObject("MeleeSwing");
            go.transform.position = position + forward * 1.2f;
            var melee = go.AddComponent<MeleeAttack>();
            melee.owner = owner;
            VFXManager.SpawnMeleeEffect(go.transform.position, forward);
            ArenaAudioManager.PlayMelee();
        }

        public static DogController SpawnDog(ArenaCombatant owner, Vector3 position)
        {
            if (!DogController.CanSpawnDog(owner))
                return null;
            Vector3 spawnPos = position;
            if (UnityEngine.AI.NavMesh.SamplePosition(position, out UnityEngine.AI.NavMeshHit hit, 4.0f, UnityEngine.AI.NavMesh.AllAreas))
                spawnPos = hit.position;
            var go = new GameObject("SummonedDog");
            go.transform.position = spawnPos;
            GameObject dogPrefab = Resources.Load<GameObject>("Models/Characters/police_dog_by_get3dmodels");
#if UNITY_EDITOR
            if (dogPrefab == null)
            {
                string dogPath = "Assets/Models/Characters/police_dog_by_get3dmodels.glb";
                dogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(dogPath);
            }
#endif
            if (dogPrefab != null)
            {
                var model = Object.Instantiate(dogPrefab, go.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * 0.5f;
                var renderers = model.GetComponentsInChildren<Renderer>();
                foreach(var r in renderers) {
                    if (r.sharedMaterial != null) {
                        var mat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                        if (mat == null || mat.shader.name.Contains("Error"))
                            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        if (mat == null || mat.shader.name.Contains("Error"))
                            mat = new Material(Shader.Find("Standard"));
                        mat.mainTexture = r.sharedMaterial.mainTexture;
                        mat.color = r.sharedMaterial.color;
                        mat.SetFloat("_RimAmount", 0.35f);
                        mat.SetColor("_RimColor", new Color(0.9f, 0.9f, 1f, 1f));
                        mat.SetFloat("_Smoothness", 0.4f);
                        mat.SetFloat("_Metallic", 0.1f);
                        r.material = mat;
                    }
                }
            }
            else
            {
                // Usar material cacheado en lugar de crear nuevo cada vez
                var slaveMat = GetDogMaterial();
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "RobotBody";
                body.transform.SetParent(go.transform);
                body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                body.transform.localScale = new Vector3(0.45f, 0.85f, 0.45f);
                body.GetComponent<Renderer>().sharedMaterial = slaveMat;
                Object.DestroyImmediate(body.GetComponent<Collider>());
            }
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = 0.5f;
            col.height = 1f;
            col.center = new Vector3(0, 0.5f, 0);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 32f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            var combatant = go.AddComponent<ArenaCombatant>();
            combatant.displayName = "Dog";
            combatant.teamId = owner != null ? owner.teamId : -1;
            combatant.maxHp = 20f;
            combatant.hp = 20f;
            combatant.countsForVictory = false;
            var dog = go.AddComponent<DogController>();
            dog.owner = owner;
            // NOTA: El registro se hace automáticamente en DogController.Awake()
            // NO llamar RegisterDog() aquí para evitar duplicación
            dog.detectDistance = 20f;
            dog.attackRange = 0.8f;
            if (owner != null)
            {
                var ownerCol = owner.GetComponent<Collider>();
                var dogCol = go.GetComponent<Collider>();
                if (ownerCol != null && dogCol != null)
                    Physics.IgnoreCollision(ownerCol, dogCol);
            }
            return dog;
        }
    }
}

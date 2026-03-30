using UnityEngine;

namespace ArenaEnhanced
{
    // ============================================================
    // PROJECTILE
    // ============================================================
    public class FireballProjectile : PooledObject
    {
        public ArenaCombatant owner;
        public float minDamage = 15f;
        public float maxDamage = 25f;
        public float splashRadius = 5f;
        public float splashMinDamage = 5f;
        public float splashMaxDamage = 10f;
        public float knockback = 8f;
        public float lifeTime = 5f;

        private Rigidbody _rb;
        private float _spawnTime;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            _spawnTime = Time.time;
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        private void Update()
        {
            if (Time.time - _spawnTime > lifeTime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponent<ArenaCombatant>();
            if (target == null) target = other.GetComponentInParent<ArenaCombatant>();

            if (target != null && owner != null && target != owner && target.teamId != owner.teamId && target.IsAlive)
            {
                float damage = Random.Range(20f, 30.1f);
                target.TakeDamage(damage, owner);
                ApplySplashDamage(target.transform.position, target);
                VFXManager.SpawnImpactEffect(transform.position);
                ReturnToPool();
            }
            else if (other.gameObject != (owner != null ? owner.gameObject : null))
            {
                ReturnToPool();
            }
        }

        private void ApplySplashDamage(Vector3 center, ArenaCombatant directTarget)
        {
            Collider[] hits = Physics.OverlapSphere(center, splashRadius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                var combatant = hit.GetComponentInParent<ArenaCombatant>();
                if (combatant == null || !combatant.IsAlive || combatant == owner || combatant == directTarget) continue;
                if (owner != null && combatant.teamId == owner.teamId) continue;

                float splashDamage = Random.Range(splashMinDamage, splashMaxDamage) * (owner != null ? owner.damageMultiplier : 1f);
                combatant.TakeDamage(splashDamage, owner);
            }
        }
    }

    public class WeaponProjectile : PooledObject
    {
        public ArenaCombatant owner;
        public WeaponData weaponData;
        public float lifeTime = 4f;

        private float _spawnTime;

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            _spawnTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - _spawnTime > lifeTime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponent<ArenaCombatant>();
            if (target == null) target = other.GetComponentInParent<ArenaCombatant>();

            if (target != null && owner != null && target != owner && target.teamId != owner.teamId && target.IsAlive)
            {
                float damage = weaponData != null ? weaponData.RollDamage() : 10f;
                target.TakeDamage(damage * owner.damageMultiplier, owner);

                // Knockback para la escopeta (2 metros aprox)
                if (weaponData != null && weaponData.weaponName.ToLower().Contains("shotgun"))
                {
                    Vector3 knockDir = (target.transform.position - transform.position).normalized;
                    knockDir.y = 0;
                    target.ApplyKnockback(knockDir * 12f);
                }

                if (weaponData != null && weaponData.splashRadius > 0f)
                {
                    ApplySplashDamage(target.transform.position, target);
                }

                VFXManager.SpawnImpactEffect(transform.position);
                ReturnToPool();
            }
            else if (other.gameObject != (owner != null ? owner.gameObject : null))
            {
                ReturnToPool();
            }
        }

        private void ApplySplashDamage(Vector3 center, ArenaCombatant directTarget)
        {
            if (weaponData == null) return;
            
            Collider[] hits = Physics.OverlapSphere(center, weaponData.splashRadius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                var combatant = hit.GetComponentInParent<ArenaCombatant>();
                if (combatant == null || !combatant.IsAlive || combatant == owner || combatant == directTarget) continue;
                if (owner != null && combatant.teamId == owner.teamId) continue;

                combatant.TakeDamage(weaponData.RollSplashDamage() * (owner != null ? owner.damageMultiplier : 1f), owner);
            }
        }
    }

    // ============================================================
    // MELEE ATTACK
    // ============================================================
    public class MeleeAttack : MonoBehaviour
    {
        public ArenaCombatant owner;
        public float damage = 15f;
        public float range = 2.5f;
        public float knockback = 12f;
        public float duration = 0.3f;

        private float _startTime;

        private void Start()
        {
            _startTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - _startTime > duration)
            {
                Destroy(gameObject);
                return;
            }

            if (owner == null) return;

            foreach (var c in ArenaCombatant.All)
            {
                if (c == null || !c.IsAlive || c == owner || c.teamId == owner.teamId) continue;
                float dist = Vector3.Distance(transform.position, c.transform.position);
                if (dist <= range)
                {
                    c.TakeDamage(damage * owner.damageMultiplier, owner);
                    VFXManager.SpawnImpactEffect(c.transform.position + Vector3.up);
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }

    // ============================================================
    // PICKUP
    // ============================================================
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
            else {
                // c.BuffDamage(buffMultiplier, buffDuration); 
            }

            ArenaAudioManager.PlayPickup();
            Destroy(gameObject);
        }
    }

    // ============================================================
    // DAMAGE NUMBER
    // ============================================================
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

    // ============================================================
    // SPAWNER
    // ============================================================
    public static class RuntimeSpawner
    {
        public static void SpawnFireball(ArenaCombatant owner, Vector3 origin, Vector3 direction, float speed)
        {
            // Intentar usar Object Pool primero
            if (GenericObjectPool.Instance != null && GenericObjectPool.Instance.HasPool("Fireball"))
            {
                GameObject go = GenericObjectPool.Instance.GetFromPool("Fireball", origin, Quaternion.identity);
                if (go != null)
                {
                    var rb = go.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = direction.normalized * speed;
                    }
                    
                    var projectile = go.GetComponent<FireballProjectile>();
                    if (projectile != null)
                    {
                        projectile.owner = owner;
                    }
                    
                    // Ignorar colisión con owner
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

            // Fallback: Crear tradicionalmente si no hay pool disponible
            var goNew = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            goNew.name = "Fireball";
            goNew.transform.position = origin;
            goNew.transform.localScale = Vector3.one * 0.45f;

            var renderer = goNew.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(1f, 0.35f, 0.1f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.1f) * 2f);
                renderer.material = mat;
            }

            var rbNew = goNew.AddComponent<Rigidbody>();
            rbNew.mass = 0.8f;
            rbNew.useGravity = false;
            rbNew.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rbNew.linearVelocity = direction.normalized * speed;

            var col = goNew.GetComponent<SphereCollider>();
            if (col != null) col.isTrigger = true;

            var trail = goNew.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 0.4f;
            trail.endWidth = 0.05f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(1f, 0.5f, 0.1f, 1f);
            trail.endColor = new Color(1f, 0.2f, 0f, 0f);

            var projectileNew = goNew.AddComponent<FireballProjectile>();
            projectileNew.owner = owner;

            if (owner != null)
            {
                var ownerCol = owner.GetComponent<Collider>();
                var projCol = goNew.GetComponent<Collider>();
                if (ownerCol != null && projCol != null)
                    Physics.IgnoreCollision(ownerCol, projCol);
            }

            ArenaAudioManager.PlayFireball();
        }

        public static void SpawnWeaponProjectile(ArenaCombatant owner, Vector3 origin, Vector3 direction, WeaponData weaponData)
        {
            string poolTag = weaponData != null && weaponData.type == WeaponType.Flamethrower ? "FlameProjectile" : "WeaponProjectile";
            
            // Intentar usar Object Pool primero
            if (GenericObjectPool.Instance != null && GenericObjectPool.Instance.HasPool(poolTag))
            {
                GameObject go = GenericObjectPool.Instance.GetFromPool(poolTag, origin, Quaternion.LookRotation(direction));
                if (go != null)
                {
                    var rb = go.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 finalDirection = new Vector3(direction.x, 0, direction.z).normalized;
                        rb.linearVelocity = finalDirection * (weaponData != null ? weaponData.projectileSpeed : 35f);
                    }
                    
                    var projectile = go.GetComponent<WeaponProjectile>();
                    if (projectile != null)
                    {
                        projectile.owner = owner;
                        projectile.weaponData = weaponData;
                    }
                    
                    // Ignorar colisión con owner
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

            // Fallback: Crear tradicionalmente
            var goNew = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            goNew.name = $"Projectile_{(weaponData != null ? weaponData.weaponName : "Weapon")}";
            goNew.transform.position = origin;
            goNew.transform.localScale = Vector3.one * 0.25f;

            var renderer = goNew.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                Color bulletColor = new Color(1f, 0.2f, 0.2f);
                
                if (weaponData != null && weaponData.weaponTexture != null)
                {
                    mat.mainTexture = weaponData.weaponTexture;
                }
                
                mat.color = bulletColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.1f, 0.1f) * 3f);
                
                if (weaponData != null && weaponData.type == WeaponType.Flamethrower)
                {
                    mat.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.1f) * 2.5f);
                }
                renderer.material = mat;
            }

            var rbNew = goNew.AddComponent<Rigidbody>();
            rbNew.mass = 0.2f;
            rbNew.useGravity = false;
            rbNew.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            Vector3 finalDir = new Vector3(direction.x, 0, direction.z).normalized;
            rbNew.linearVelocity = finalDir * (weaponData != null ? weaponData.projectileSpeed : 35f);

            var col = goNew.GetComponent<SphereCollider>();
            if (col != null) col.isTrigger = true;

            var trail = goNew.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 0.25f;
            trail.endWidth = 0.05f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            
            Color trailColor = new Color(1f, 0.1f, 0.1f, 1f);
            trail.startColor = trailColor;
            trail.endColor = new Color(1f, 0f, 0f, 0f);

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
            Vector3 spawnPos = position;
            if (UnityEngine.AI.NavMesh.SamplePosition(position, out UnityEngine.AI.NavMeshHit hit, 4.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            var go = new GameObject("SummonedDog");
            go.transform.position = spawnPos;

#if UNITY_EDITOR
            string dogPath = "Assets/Models/Characters/police_dog_by_get3dmodels.glb";
            
            GameObject dogPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(dogPath);

            if (dogPrefab != null)
            {
                // Usar el modelo 3D real del GLB
                var model = Object.Instantiate(dogPrefab, go.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                
                // ESCALA 20% del tamaño original
                model.transform.localScale = Vector3.one * 0.5f;
                
                var renderers = model.GetComponentsInChildren<Renderer>();
                
                // Aplicar material con Rim Lighting para destacar volumen 3D
                foreach(var r in renderers) {
                    if (r.sharedMaterial != null) {
                        var mat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                        mat.mainTexture = r.sharedMaterial.mainTexture;
                        mat.color = r.sharedMaterial.color;
                        
                        // Rim lighting para bordes brillantes
                        mat.SetFloat("_RimAmount", 0.35f);
                        mat.SetColor("_RimColor", new Color(0.9f, 0.9f, 1f, 1f));
                        
                        // Ajustes de iluminación
                        mat.SetFloat("_Smoothness", 0.4f);
                        mat.SetFloat("_Metallic", 0.1f);
                        
                        r.material = mat;
                    }
                }
                
                // Luz de relleno para destacar volumen
                var fillLight = go.AddComponent<Light>();
                fillLight.type = LightType.Point;
                fillLight.intensity = 0.6f;
                fillLight.range = 5f;
                fillLight.color = new Color(0.95f, 0.95f, 1f);
                
                Debug.Log("[Dog] Loaded real 3D model with 2x scale!");
            }
            else
#endif
            {
                // Fallback solo si no existe el modelo
                var slaveMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                slaveMat.color = new Color(0.65f, 0.45f, 1f);

                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "RobotBody";
                body.transform.SetParent(go.transform);
                body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                body.transform.localScale = new Vector3(0.45f, 0.85f, 0.45f);
                body.GetComponent<Renderer>().material = slaveMat;
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

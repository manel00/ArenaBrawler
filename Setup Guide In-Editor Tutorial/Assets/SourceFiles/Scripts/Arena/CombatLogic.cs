using UnityEngine;
using WoW.Armas;

namespace ArenaEnhanced
{
    // ============================================================
    // PROJECTILE
    // ============================================================
    public class FireballProjectile : MonoBehaviour
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

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponent<ArenaCombatant>();
            if (target == null) target = other.GetComponentInParent<ArenaCombatant>();

            if (target != null && owner != null && target != owner && target.teamId != owner.teamId && target.IsAlive)
            {
                float damage = Random.Range(minDamage, maxDamage) * owner.damageMultiplier;
                target.TakeDamage(damage, owner);
                ApplySplashDamage(target.transform.position, target);
                VFXManager.SpawnImpactEffect(transform.position);
                Destroy(gameObject);
            }
            else if (other.gameObject != (owner != null ? owner.gameObject : null))
            {
                Destroy(gameObject);
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

    public class WeaponProjectile : MonoBehaviour
    {
        public ArenaCombatant owner;
        public WeaponData weaponData;
        public float lifeTime = 4f;

        private void Start()
        {
            Destroy(gameObject, lifeTime);
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
                Destroy(gameObject);
            }
            else if (other.gameObject != (owner != null ? owner.gameObject : null))
            {
                Destroy(gameObject);
            }
        }

        private void ApplySplashDamage(Vector3 center, ArenaCombatant directTarget)
        {
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
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Fireball";
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * 0.45f;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(1f, 0.35f, 0.1f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.1f) * 2f);
                renderer.material = mat;
            }

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.8f;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = direction.normalized * speed;

            var col = go.GetComponent<SphereCollider>();
            if (col != null) col.isTrigger = true;

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 0.4f;
            trail.endWidth = 0.05f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(1f, 0.5f, 0.1f, 1f);
            trail.endColor = new Color(1f, 0.2f, 0f, 0f);

            var projectile = go.AddComponent<FireballProjectile>();
            projectile.owner = owner;

            if (owner != null)
            {
                var ownerCol = owner.GetComponent<Collider>();
                var projCol = go.GetComponent<Collider>();
                if (ownerCol != null && projCol != null)
                    Physics.IgnoreCollision(ownerCol, projCol);
            }

            ArenaAudioManager.PlayFireball();
        }

        public static void SpawnWeaponProjectile(ArenaCombatant owner, Vector3 origin, Vector3 direction, WeaponData weaponData)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"Projectile_{(weaponData != null ? weaponData.weaponName : "Weapon")}";
            go.transform.position = origin;
            // Más pequeño que la bola de fuego (0.45f vs 0.15f)
            go.transform.localScale = Vector3.one * 0.15f; 

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                
                // Color gris metálico para las balas
                Color bulletColor = new Color(0.7f, 0.72f, 0.75f);
                
                if (weaponData != null && weaponData.weaponTexture != null)
                {
                    mat.mainTexture = weaponData.weaponTexture;
                }
                
                mat.color = bulletColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", bulletColor * 1.8f);
                
                if (weaponData != null && weaponData.type == WeaponType.Flamethrower)
                {
                    mat.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.1f) * 2.5f);
                }
                renderer.material = mat;
            }

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.2f;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            // Garantizar que la velocidad sea estrictamente horizontal (paralela al plano XZ)
            Vector3 finalDirection = new Vector3(direction.x, 0, direction.z).normalized;
            rb.linearVelocity = finalDirection * (weaponData != null ? weaponData.projectileSpeed : 35f);

            var col = go.GetComponent<SphereCollider>();
            if (col != null) col.isTrigger = true;

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.25f; 
            trail.startWidth = 0.12f; // Proporcional al nuevo tamaño de la bala
            trail.endWidth = 0.02f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            
            // Estela roja según petición del usuario
            Color trailColor = new Color(1f, 0f, 0f, 0.7f); 
            trail.startColor = trailColor;
            trail.endColor = new Color(1f, 0f, 0f, 0f); // Desvanecimiento

            var projectile = go.AddComponent<WeaponProjectile>();
            projectile.owner = owner;
            projectile.weaponData = weaponData;

            if (owner != null)
            {
                var ownerColliders = owner.GetComponentsInChildren<Collider>();
                var projCol = go.GetComponent<Collider>();
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
            string robotPath = "Assets/Models/Characters/Domestic robot/Domestic Robot.obj";
            string texPath = "Assets/Models/Characters/Domestic robot/Domestic robot Texture.png";
            
            GameObject robotPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(robotPath);
            Texture2D robotTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            if (robotPrefab != null)
            {
                var model = Object.Instantiate(robotPrefab, go.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                
                var renderers = model.GetComponentsInChildren<Renderer>();
                float nativeHeight = 0f;
                foreach (var r in renderers) {
                    if (r.bounds.size.y > nativeHeight) nativeHeight = r.bounds.size.y;
                }

                if (nativeHeight > 0.001f) {
                    float targetHeight = 0.225f;
                    float scaleFactor = targetHeight / nativeHeight;
                    model.transform.localScale = Vector3.one * scaleFactor;
                    
                    float lowestPointY = float.MaxValue;
                    foreach(var r in renderers) {
                        if (r.bounds.min.y < lowestPointY) lowestPointY = r.bounds.min.y;
                    }
                    float offset = lowestPointY - model.transform.position.y;
                    model.transform.localPosition = new Vector3(0, -offset, 0);
                }
                else {
                    model.transform.localScale = Vector3.one * 0.05f; 
                }

                if (renderers.Length > 0 && robotTex != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.mainTexture = robotTex;
                    foreach(var r in renderers) r.material = mat;
                }
            }
            else
#endif
            {
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
            dog.moveSpeed = 4.5f;
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

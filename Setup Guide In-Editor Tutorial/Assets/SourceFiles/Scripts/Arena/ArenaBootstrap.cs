using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ArenaEnhanced
{
    // Las clases ArenaCombatant, PlayerController y ArenaHUD se han movido a sus propios archivos .cs
    // para mejorar la organización y evitar conflictos de nombres.

    // ============================================================
    // PROJECTILE
    // ============================================================
    public class FireballProjectile : MonoBehaviour
    {
        public ArenaCombatant owner;
        public float minDamage = 5f;
        public float maxDamage = 10f;
        public float knockback = 8f;
        public float lifeTime = 5f;

        private Rigidbody _rb;
        private TrailRenderer _trail;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _trail = GetComponentInChildren<TrailRenderer>();
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
                VFXManager.SpawnImpactEffect(transform.position);
                Destroy(gameObject);
            }
            else if (other.gameObject != owner.gameObject)
            {
                // Destroy on wall hit or other obstacles (excluding owner)
                Destroy(gameObject);
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
            rb.useGravity = false; // Desactivar gravedad para que vuele recto
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

            AudioManager.PlayFireball();
        }

        public static void SpawnMelee(ArenaCombatant owner, Vector3 position, Vector3 forward)
        {
            var go = new GameObject("MeleeSwing");
            go.transform.position = position + forward * 1.2f;

            var melee = go.AddComponent<MeleeAttack>();
            melee.owner = owner;

            VFXManager.SpawnMeleeEffect(go.transform.position, forward);
            AudioManager.PlayMelee();
        }
        public static DogController SpawnDog(ArenaCombatant owner, Vector3 position)
        {
            var go = new GameObject("SummonedDog");
            go.transform.position = position;

#if UNITY_EDITOR
            var modelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Domestic robot/Domestic Robot.obj");
            if (modelPrefab != null)
            {
                var model = Object.Instantiate(modelPrefab, go.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); // Half size robot dog
                
                // Texture setup
                var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Characters/Domestic robot/Domestic robot Texture.png");
                var renderers = model.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if (tex != null) mat.mainTexture = tex;
                    r.material = mat;
                }

                // Cleanup colliders
                var childCols = model.GetComponentsInChildren<Collider>(true);
                foreach (var c in childCols) Object.Destroy(c);
            }
            else
            {
                // Fallback to primitive
                var primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                primitive.transform.SetParent(go.transform);
                primitive.transform.localPosition = Vector3.up * 0.5f;
                primitive.transform.localScale = new Vector3(0.6f, 0.4f, 0.8f);
                Object.Destroy(primitive.GetComponent<Collider>());
            }
#endif

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
            combatant.maxHp = 45f;
            combatant.hp = 45f;
            combatant.countsForVictory = false;

            var dog = go.AddComponent<DogController>();
            dog.owner = owner;
            return dog;
        }
    }

    // Las clases BotController y DogController se han movido a sus propios archivos .cs

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
                // c.BuffDamage(buffMultiplier, buffDuration); // No incluido en la versión actual de ArenaCombatant
            }

            AudioManager.PlayPickup();
            Destroy(gameObject);
        }
    }

    // ============================================================
    // VFX MANAGER
    // ============================================================
    public static class VFXManager
    {
        public static void SpawnImpactEffect(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.5f;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(1f, 0.8f, 0.2f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.1f) * 3f);
                r.material = mat;
            }
            Object.Destroy(go.GetComponent<Collider>());
            Object.Destroy(go, 0.3f);
        }

        public static void SpawnDeathEffect(Vector3 pos)
        {
            for (int i = 0; i < 8; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = pos + Random.insideUnitSphere * 0.5f;
                go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.3f);
                var r = go.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.3f, 0.3f, 0.3f);
                Object.Destroy(go.GetComponent<Collider>());
                var rb = go.AddComponent<Rigidbody>();
                rb.linearVelocity = Random.insideUnitSphere * 5f + Vector3.up * 3f;
                Object.Destroy(go, 1.5f);
            }
        }

        public static void SpawnShieldEffect(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.up;
            go.transform.localScale = Vector3.one * 2.5f;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.3f, 0.6f, 1f, 0.3f);
                r.material = mat;
            }
            Object.Destroy(go.GetComponent<Collider>());
            Object.Destroy(go, 3f);
        }

        public static void SpawnDashEffect(Vector3 pos)
        {
            for (int i = 0; i < 5; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = pos + Vector3.up * 0.5f + Random.insideUnitSphere * 0.3f;
                go.transform.localScale = Vector3.one * 0.15f;
                var r = go.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.8f, 0.8f, 1f, 0.6f);
                Object.Destroy(go.GetComponent<Collider>());
                Object.Destroy(go, 0.4f);
            }
        }

        public static void SpawnMeleeEffect(Vector3 pos, Vector3 dir)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos + dir * 0.5f;
            go.transform.localScale = new Vector3(2f, 0.3f, 0.3f);
            go.transform.rotation = Quaternion.LookRotation(dir);
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(1f, 1f, 1f, 0.7f);
                r.material = mat;
            }
            Object.Destroy(go.GetComponent<Collider>());
            Object.Destroy(go, 0.2f);
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
    // AUDIO MANAGER
    // ============================================================
    public static class AudioManager
    {
        private static AudioSource _source;

        private static AudioSource GetSource()
        {
            if (_source == null)
            {
                var go = new GameObject("AudioManager");
                Object.DontDestroyOnLoad(go);
                _source = go.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.volume = 0.5f;
            }
            return _source;
        }

        public static void PlayFireball() => PlayTone(400f, 0.1f);
        public static void PlayHit() => PlayTone(200f, 0.08f);
        public static void PlayDeath() => PlayTone(100f, 0.3f);
        public static void PlayShield() => PlayTone(600f, 0.15f);
        public static void PlayDash() => PlayTone(500f, 0.08f);
        public static void PlayMelee() => PlayTone(250f, 0.1f);
        public static void PlayPickup() => PlayTone(800f, 0.1f);

        private static void PlayTone(float freq, float duration)
        {
            var src = GetSource();
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create("tone", samples, 1, sampleRate, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Max(0f, 1f - (float)i / samples);
            }
            clip.SetData(data, 0);
            src.PlayOneShot(clip);
        }
    }

    // ============================================================
    // CAMERA FOLLOW
    // ============================================================
    public class ArenaCameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 7f, -9f);
        public float smooth = 5f;
        public float rotationSpeed = 5f;
        public float minDistance = 5f;
        public float maxDistance = 15f;
        public float zoomSpeed = 2f;

        private float _currentDistance;
        private float _targetDistance;
        private Vector3 _currentOffset;

        private void Start()
        {
            _currentDistance = offset.magnitude;
            _targetDistance = _currentDistance;
            _currentOffset = offset.normalized;

            gameObject.tag = "MainCamera";
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Rotación de la cámara (WoW-style follow)
            // Se puede extender para orbitar con el mouse
            Vector3 desiredPosition = target.position + target.rotation * offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smooth * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * 1.5f);
        }
    }

    // ============================================================
    // GAME MANAGER
    // ============================================================
    // ArenaGameManager se ha movido a su propio archivo ArenaGameManager.cs
    // para evitar duplicidad y mejorar la organización.

    // ============================================================
    // BOOTSTRAP
    // ============================================================
    public class ArenaBootstrap : MonoBehaviour
    {
        public int bots = 3;

        private void Start()
        {
            BuildEnvironment();
            // SpawnWeaponsOnFloor(); // Removed to keep arena clean as requested
            BuildArenaMatch();
        }

        private void SpawnWeaponsOnFloor()
        {
#if UNITY_EDITOR
            var weaponsParent = new GameObject("GroundWeapons").transform;
            int numWeapons = 10;
            for (int i = 1; i <= numWeapons; i++)
            {
                // Create WeaponData scriptable object
                var data = ScriptableObject.CreateInstance<WoW.Armas.WeaponData>();
                data.weaponName = "Arma_" + i;
                data.damage = Random.Range(10f, 30f);
                data.weaponScale = new Vector3(0.5f, 0.5f, 0.5f);
                data.weaponColor = Color.white;
                
                // Texture path
                string texNum = i < 10 ? "0" + i : i.ToString();
                string texPath = $"Assets/SourceFiles/Textures/Alt/PolygonPrideWeapons_Texture_Alt_{texNum}.png";
                var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

                Vector3 randomPos = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
                
                // Manually create the pickup model wrapper
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = data.weaponName;
                go.transform.position = randomPos;
                go.transform.localScale = new Vector3(0.2f, 1f, 0.2f);
                go.transform.SetParent(weaponsParent);

                var renderer = go.GetComponent<Renderer>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (texture != null) mat.mainTexture = texture;
                renderer.material = mat;

                var pickup = go.AddComponent<WoW.Armas.WeaponPickup>();
                pickup.weaponData = data;

                var boxCol = go.GetComponent<Collider>();
                boxCol.isTrigger = true;

                var rb = go.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
            }
#endif
        }

        private void BuildArenaMatch()
        {
            var player = SpawnFighter("Player", new Vector3(0f, 1.2f, -6f), new Color(0.2f, 0.8f, 1f), 1, true);

            float radius = 10f;
            for (int i = 0; i < bots; i++)
            {
                float angle = (Mathf.PI * 2f / Mathf.Max(1, bots)) * i;
                Vector3 pos = new Vector3(Mathf.Cos(angle), 1.2f, Mathf.Sin(angle)) * radius;
                SpawnFighter($"Bot_{i + 1}", pos, new Color(1f, 0.35f, 0.35f), 10 + i, false);
            }

            SetupMainCamera(player.transform);

            // Setup Game Manager
            var gmGo = new GameObject("ArenaGameManager");
            var gm = gmGo.AddComponent<ArenaGameManager>();
            gm.player = player;

            // Setup Premium HUD
            var hudGo = new GameObject("ArenaHUD");
            var hud = hudGo.AddComponent<ArenaHUD>();
            hud.Initialize(player);
        }

        private void BuildEnvironment()
        {
 #if UNITY_EDITOR
            var envGroup = new GameObject("Environment");

            // Synty Ground Material
            var terrainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonGeneric/Materials/Generic_Overview_Map_Ground.mat");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "ArenaGround";
            ground.transform.SetParent(envGroup.transform);
            ground.transform.localScale = new Vector3(8f, 1f, 8f);
            if (terrainMat != null) ground.GetComponent<Renderer>().material = terrainMat;

            string[] treePaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_02.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_03.prefab"
            };
            string[] rockPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_04.prefab"
            };

            // Scatter Trees
            for (int i = 0; i < 50; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(treePaths[Random.Range(0, treePaths.Length)]);
                if (prefab != null)
                {
                    var tree = Instantiate(prefab, GetRandomPositionWithoutCenter(14f, 38f), Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.8f, 1.5f);
                    tree.transform.localScale = new Vector3(s, s, s);
                    var cols = tree.GetComponentsInChildren<Collider>();
                    foreach(var col in cols) Destroy(col); // Prevent getting stuck in leaves
                    tree.AddComponent<CapsuleCollider>().radius = 0.5f; // Only trunk collision
                }
            }

            // Scatter Rocks
            for (int i = 0; i < 20; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rockPaths[Random.Range(0, rockPaths.Length)]);
                if (prefab != null)
                {
                    var rock = Instantiate(prefab, GetRandomPositionWithoutCenter(10f, 38f), Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.8f, 2.0f);
                    rock.transform.localScale = new Vector3(s, s, s);
                }
            }
#endif
        }

        private Vector3 GetRandomPositionWithoutCenter(float minRadius, float maxRadius)
        {
            int maxAttempts = 50;
            for (int i = 0; i < maxAttempts; i++)
            {
                float x = Random.Range(-maxRadius, maxRadius);
                float z = Random.Range(-maxRadius, maxRadius);
                Vector3 pos = new Vector3(x, 0, z);
                if (pos.magnitude > minRadius) return pos;
            }
            return new Vector3(maxRadius, 0, maxRadius);
        }

        private static ArenaCombatant SpawnFighter(string fighterName, Vector3 position, Color color, int team, bool isPlayer)
        {
            var go = new GameObject(fighterName);
            go.transform.position = position;

#if UNITY_EDITOR
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerRobot.prefab");
            if (modelPrefab == null)
            {
                string[] guids = AssetDatabase.FindAssets("PlayerRobot t:Prefab");
                if (guids.Length > 0)
                {
                    modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            if (modelPrefab != null)
            {
                var model = Instantiate(modelPrefab, go.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0, -90, 0); // Corrected orientation: mesh faces +X, rotate to align with parent forward
                model.transform.localScale = new Vector3(1f, 1f, 1f);
                
                if (!isPlayer)
                {
                    // Cleanup components that might hijack camera/input on bots
                    var compsToDisable = model.GetComponentsInChildren<Behaviour>(true);
                    foreach (var c in compsToDisable)
                    {
                        string typeName = c.GetType().Name;
                        if (typeName.Contains("Camera") || typeName.Contains("Listener") || 
                            typeName.Contains("Input") || typeName.Contains("Cinemachine") || typeName.Contains("Audio"))
                        {
                            c.enabled = false;
                        }
                    }

                    // Randomize Bot Colors
                    var renderers = model.GetComponentsInChildren<Renderer>();
                    Color rndColor = Random.ColorHSV(0, 1, 0.4f, 1, 0.4f, 1);
                    foreach (var r in renderers)
                    {
                        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        mat.color = rndColor;
                        mat.SetFloat("_Metallic", Random.Range(0.2f, 0.9f));
                        mat.SetFloat("_Smoothness", Random.Range(0.2f, 0.8f));
                        r.material = mat;
                    }
                }

                // --- CRITICAL CLEANUP FOR BOTH PLAYER AND BOTS ---
                // Remove CharacterController and StarterAssets that fight with our physics
                var allComps = model.GetComponentsInChildren<Component>(true);
                foreach (var comp in allComps)
                {
                    if (comp == null) continue;
                    string t = comp.GetType().Name;
                    if (t == "CharacterController" || t == "ThirdPersonController" || t == "StarterAssetsInputs" || 
                        t == "PlayerInput" || t == "RespawnPlayer" || t == "CinemachineBrain" || t == "CinemachineCamera" ||
                        t == "Camera" || t == "AudioListener")
                    {
                        Object.Destroy(comp);
                    }
                }

                // Destroy all child colliders to prevent physics 'explosions' with root collider
                var childCols = model.GetComponentsInChildren<Collider>(true);
                foreach (var c in childCols) Object.Destroy(c);
            }
#endif
            // Add Capsule Collider to Root
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.4f;
            col.center = new Vector3(0, 0.9f, 0);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 70f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var combatant = go.AddComponent<ArenaCombatant>();
            combatant.displayName = fighterName;
            combatant.teamId = team;
            combatant.isPlayer = isPlayer;
            combatant.maxHp = 100f;
            combatant.hp = 100f;

            if (isPlayer) 
            {
                var pc = go.AddComponent<PlayerController>();
                pc.jumpForce = 15f; // Increased jump height to reach ring
            }
            else 
            {
                go.AddComponent<BotController>();
            }

            return combatant;
        }

        private static void SetupMainCamera(Transform target)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                // Try to find ANY camera that isn't a child of a bot
                var allCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
                cam = allCams.FirstOrDefault(c => c.transform.root == c.transform || c.transform.root.name == "ArenaManager");
                
                if (cam == null)
                {
                    var camGo = new GameObject("Main Camera");
                    cam = camGo.AddComponent<Camera>();
                    camGo.AddComponent<AudioListener>();
                    camGo.tag = "MainCamera";
                }
            }

            // Ensure only one audio listener is active
            var allListeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
            for (int i = 0; i < allListeners.Length; i++)
            {
                allListeners[i].enabled = (allListeners[i].gameObject == cam.gameObject);
            }

            var follow = cam.GetComponent<ArenaCameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<ArenaCameraFollow>();
            follow.target = target;
        }
    }
}
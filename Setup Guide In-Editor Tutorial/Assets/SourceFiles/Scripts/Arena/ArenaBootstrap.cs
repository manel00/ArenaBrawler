using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WoW.Armas;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ArenaEnhanced
{
    // ============================================================
    // Orchestrator
    // ============================================================

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
        [Header("Fallback (overridden by PlayerPrefs from WelcomeScreen)")]
        public int defaultBots = 3;
        public string defaultPlayerName = "Survivor";

        private const int WeaponsPerAlliedCombatant = 10;
        private static WeaponData[] _runtimeWeapons;

        private void Start()
        {
            Debug.Log("[ArenaBootstrap] Start: Initializing Horde Survival arena...");

            // Check if we came from welcome screen
            string fromWelcome = PlayerPrefs.GetString("FromWelcomeScreen", "false");
            string gameMode = PlayerPrefs.GetString("GameMode", "");

            if (fromWelcome != "true" || string.IsNullOrEmpty(gameMode))
            {
                Debug.Log("[ArenaBootstrap] Not from welcome screen, showing welcome UI...");
                ShowWelcomeScreen();
                return;
            }

            // Clear flag so next time we show welcome screen
            PlayerPrefs.SetString("FromWelcomeScreen", "false");
            PlayerPrefs.Save();

            BuildEnvironment();
            int botCount = PlayerPrefs.GetInt("BotCount", defaultBots);
            BuildArenaMatch(botCount);
        }

        private void ShowWelcomeScreen()
        {
            // Create welcome screen UI directly in this scene
            var welcomeGo = new GameObject("WelcomeScreen");
            var welcome = welcomeGo.AddComponent<WelcomeScreenUI>();
            welcome.OnStartGame = (botCount, playerName) => {
                PlayerPrefs.SetString("PlayerName", playerName);
                PlayerPrefs.SetInt("BotCount", botCount);
                PlayerPrefs.SetString("GameMode", "Solo");
                PlayerPrefs.SetString("FromWelcomeScreen", "true");
                PlayerPrefs.Save();

                // Reload scene to start game
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            };
        }

        private void SpawnWeaponsOnFloor(IReadOnlyList<ArenaCombatant> alliedCombatants)
        {
#if UNITY_EDITOR
            if (alliedCombatants == null || alliedCombatants.Count == 0) return;

            var existing = GameObject.Find("GroundWeapons");
            if (existing != null) Destroy(existing);

            var weaponsParent = new GameObject("GroundWeapons").transform;
            WeaponData[] availableWeapons = GetRuntimeWeapons();
            int totalWeapons = alliedCombatants.Count * WeaponsPerAlliedCombatant;

            for (int i = 0; i < totalWeapons; i++)
            {
                // Round-robin para distribución igual (1 rifle, 1 shotgun, 1 flamethrower, etc.)
                WeaponData selected = availableWeapons[i % availableWeapons.Length];
                Vector3 spawnPos = GetWeaponSpawnPosition(i, totalWeapons);
                var pickup = WeaponPickup.CreatePickup(selected, spawnPos, selected.DefaultAmmo);
                if (pickup != null)
                {
                    pickup.transform.SetParent(weaponsParent);
                }
            }

            Debug.Log($"[ArenaBootstrap] Spawned {totalWeapons} weapons for {alliedCombatants.Count} allied combatants.");
#endif
        }

        private void BuildArenaMatch(int overrideBotCount = -1)
        {
            // Read settings from WelcomeScreen (PlayerPrefs)
            string playerName = PlayerPrefs.GetString("PlayerName", defaultPlayerName);
            int botCount = overrideBotCount >= 0 ? overrideBotCount : PlayerPrefs.GetInt("BotCount", defaultBots);
#if DEBUG
            Debug.Log($"[ArenaBootstrap] BuildArenaMatch: Player={playerName}, AlliedBots={botCount}");
#endif
            List<ArenaCombatant> alliedCombatants = new List<ArenaCombatant>();

            // Spawn Player (Team 1)
            var player = SpawnFighter(playerName, new Vector3(0f, 1.2f, -6f), new Color(0.2f, 0.8f, 1f), 1, true);
            if (player != null)
            {
                player.displayName = playerName;
                alliedCombatants.Add(player);

                // Attach Ice Katana — player presses K to equip, 5 to attack
                if (player.GetComponent<KatanaWeapon>() == null)
                    player.gameObject.AddComponent<KatanaWeapon>();
            }

            // Spawn Allied Bots (also Team 1 - NO friendly fire)
            float radius = 8f;
            for (int i = 0; i < botCount; i++)
            {
                float angle = (Mathf.PI * 2f / Mathf.Max(1, botCount)) * i;
                Vector3 pos = new Vector3(Mathf.Cos(angle), 1.2f, Mathf.Sin(angle)) * radius;
                var bot = SpawnFighter($"Ally_{i + 1}", pos, new Color(0.4f, 1f, 0.4f), 1, false); // Same team as player!
                if (bot != null)
                {
                    bot.displayName = $"Ally Bot {i + 1}";
                    alliedCombatants.Add(bot);
                }
            }

            if (player != null)
            {
                SetupMainCamera(player.transform);
            }
            else
            {
#if DEBUG
                Debug.LogError("[ArenaBootstrap] BuildArenaMatch: Player spawn FAILED!");
#endif
                return;
            }

            SpawnWeaponsOnFloor(alliedCombatants);

            // Setup Game Manager
            var gmGo = new GameObject("ArenaGameManager");
            var gm = gmGo.AddComponent<ArenaGameManager>();
            gm.player = player;

            // Setup HUD
            GameObject hudPrefab = Resources.Load<GameObject>("Prefabs/UI/ArenaHUD");
            
            GameObject hudGo;
            if (hudPrefab != null)
            {
                hudGo = Instantiate(hudPrefab);
                hudGo.name = "ArenaHUD";
            }
            else
            {
                hudGo = new GameObject("ArenaHUD");
            }

            var hud = hudGo.GetComponent<ArenaHUD>() ?? hudGo.AddComponent<ArenaHUD>();
            hud.Initialize(player);

            // === HORDE WAVE MANAGER ===
            var waveGo = new GameObject("HordeWaveManager");
            var waveManager = waveGo.AddComponent<HordeWaveManager>();
            waveManager.arenaRadius = 38f;
            waveManager.StartHorde(player);

            Debug.Log("[ArenaBootstrap] Horde Survival started!");
        }

        private void BuildEnvironment()
        {
 #if UNITY_EDITOR
            var envGroup = new GameObject("Environment");

            // === EXPANDED ARENA GROUND (4x bigger) ===
            var terrainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Grass.mat");
            var accentMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_SandWavey.mat");
            var outerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_GrassFlowers.mat");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "ArenaGround";
            ground.transform.SetParent(envGroup.transform);
            ground.transform.localScale = new Vector3(64f, 1f, 64f); // 4x expanded
            if (terrainMat != null) ground.GetComponent<Renderer>().material = terrainMat;

            // Second ground tile for extra visual coverage at edges
            for (int ix = -1; ix <= 1; ix++)
            {
                for (int iz = -1; iz <= 1; iz++)
                {
                    if (ix == 0 && iz == 0) continue;
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    tile.name = "ArenaGround_Tile";
                    tile.transform.SetParent(envGroup.transform);
                    tile.transform.position = new Vector3(ix * 640f, 0f, iz * 640f);
                    tile.transform.localScale = new Vector3(64f, 1f, 64f);
                    Material tileMat = ((ix + iz) % 2 == 0) ? outerMat : accentMat;
                    if (tileMat == null) tileMat = terrainMat;
                    if (tileMat != null) tile.GetComponent<Renderer>().material = tileMat;
                    Object.DestroyImmediate(tile.GetComponent<Collider>());
                }
            }

            string[] treePaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_02.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_03.prefab"
            };
            string[] rockPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_04.prefab"
            };

            // Scatter Trees around the wide perimeter - all tagged as DESTRUCTIBLE
            for (int i = 0; i < 70; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(treePaths[Random.Range(0, treePaths.Length)]);
                if (prefab != null)
                {
                    var tree = Instantiate(prefab, GetRandomPositionWithoutCenter(20f, 55f), Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.8f, 1.5f);
                    tree.transform.localScale = new Vector3(s, s, s);
                    var cols = tree.GetComponentsInChildren<Collider>();
                    foreach (var col in cols) Destroy(col);
                    var treeCol = tree.AddComponent<CapsuleCollider>();
                    treeCol.radius = 0.5f;
                    treeCol.height = 4f;
                    treeCol.center = new Vector3(0, 2f, 0);
                    // === DESTRUCTIBLE - Bosses can shatter trees ===
                    tree.AddComponent<DestructibleEnvironment>();
                }
            }

            // Scatter Rocks - also DESTRUCTIBLE
            for (int i = 0; i < 30; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rockPaths[Random.Range(0, rockPaths.Length)]);
                if (prefab != null)
                {
                    var rock = Instantiate(prefab, GetRandomPositionWithoutCenter(15f, 55f), Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.8f, 2.2f);
                    rock.transform.localScale = new Vector3(s, s, s);
                    // === DESTRUCTIBLE
                    rock.AddComponent<DestructibleEnvironment>();
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
                
                // Exclude central white square (30x30m -> 15m radius/half-extent)
                // We use 16m for a small safety margin.
                if (Mathf.Abs(pos.x) < 16f && Mathf.Abs(pos.z) < 16f) continue;
                
                if (pos.magnitude > minRadius) return pos;
            }
            return new Vector3(maxRadius, 0, maxRadius);
        }

        private Vector3 GetWeaponSpawnPosition(int index, int totalWeapons)
        {
            float angle = (Mathf.PI * 2f / Mathf.Max(1, totalWeapons)) * index;
            float radius = Mathf.Lerp(10f, 32f, Random.value);
            Vector3 pos = new Vector3(Mathf.Cos(angle), 8f, Mathf.Sin(angle)) * radius;
            pos += new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-2.5f, 2.5f));

            if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Ignore))
            {
                pos = hit.point + Vector3.up * 0.65f;
            }
            else
            {
                pos.y = 0.65f;
            }

            return pos;
        }

#if UNITY_EDITOR
        private WeaponData[] GetRuntimeWeapons()
        {
            if (_runtimeWeapons != null && _runtimeWeapons.Length > 0) return _runtimeWeapons;

            GameObject assaultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Weapons/AssaultRifle_01.obj");
            GameObject shotgunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Weapons/Double Barrel Shotgun/ShortDoubleBarrel.fbx");
            GameObject flamethrowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Weapons/Pistola agua/model.obj");

            Material rifleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Simple_BlueDark.mat");
            Material shotgunMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_GoldGlow.mat");
            Material flamethrowerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Simple_Orange.mat");
            GameObject flameVFX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/FX/FX_Fire_01.prefab");

            _runtimeWeapons = new[]
            {
                CreateRuntimeWeapon(
                    "Assault Rifle",
                    WeaponType.Ranged,
                    WeaponFireMode.Projectile,
                    assaultPrefab,
                    rifleMat,
                    null,
                    new Color(0.25f, 0.55f, 1f),
                    new Vector3(2f, 2f, 2f), // Tamaño normal en suelo
                    180f, // rotationY - apunta hacia adelante
                    10f, // minDamage
                    25f, // maxDamage
                    26f, // range
                    0.01f, // cooldown - casi instantáneo
                    20, // maxAmmo
                    true, // infinite ammo!
                    1, // projectiles
                    1.5f, // spreadAngle
                    40f, // projectileSpeed
                    0f, // splash radius
                    0f,
                    0f,
                    0f,
                    0f),
                CreateRuntimeWeapon(
                    "Shotgun",
                    WeaponType.Ranged,
                    WeaponFireMode.Projectile,
                    shotgunPrefab,
                    shotgunMat,
                    null,
                    new Color(1f, 0.82f, 0.35f),
                    new Vector3(0.5f, 0.5f, 0.5f), // Tamaño normal en suelo
                    180f, // rotationY - apunta hacia adelante (igual que rifle)
                    10f, // minDamage
                    25f, // maxDamage
                    18f, // range
                    0.9f, // cooldown
                    20, // maxAmmo
                    false, // infinite
                    8, // projectiles
                    14f, // spreadAngle
                    35f, // projectileSpeed
                    0f, // splash radius
                    0f,
                    0f,
                    0f,
                    0f),
                CreateRuntimeWeapon(
                    "Flamethrower",
                    WeaponType.Flamethrower,
                    WeaponFireMode.Continuous,
                    flamethrowerPrefab,
                    flamethrowerMat,
                    null,
                    new Color(1f, 0.35f, 0.1f),
                    new Vector3(12f, 12f, 12f), // Tamaño normal en suelo
                    180f, // rotationY - apunta hacia adelante (igual que rifle)
                    0f, // minDamage
                    0f, // maxDamage
                    20f, // range
                    0.1f, // cooldown
                    0,
                    true,
                    1,
                    30f, // spreadAngle
                    0f, // projectileSpeed
                    0f,
                    0f,
                    0f,
                    5f, // minDps
                    25f, // maxDps
                    flameVFX)
            };

            return _runtimeWeapons;
        }

        private WeaponData CreateRuntimeWeapon(
            string weaponName,
            WeaponType type,
            WeaponFireMode fireMode,
            GameObject prefab,
            Material material,
            Texture2D texture,
            Color color,
            Vector3 scale,
            float rotationY,
            float minDamage,
            float maxDamage,
            float range,
            float cooldown,
            int maxAmmo,
            bool infiniteAmmo,
            int projectilesPerShot,
            float spreadAngle,
            float projectileSpeed,
            float splashRadius,
            float splashMin,
            float splashMax,
            float minDps,
            float maxDps,
            GameObject vfx = null)
        {
            var data = ScriptableObject.CreateInstance<WeaponData>();
            data.weaponName = weaponName;
            data.type = type;
            data.fireMode = fireMode;
            data.prefab = prefab;
            data.weaponMaterial = material;
            data.weaponTexture = texture;
            data.weaponColor = color;
            data.weaponScale = scale;
            data.rotationOffset = new Vector3(0, rotationY, 0);
            data.minDamage = minDamage;
            data.maxDamage = maxDamage;
            data.attackRange = range;
            data.attackCooldown = cooldown;
            data.maxAmmo = maxAmmo;
            data.infiniteAmmo = infiniteAmmo;
            data.projectilesPerShot = projectilesPerShot;
            data.spreadAngle = spreadAngle;
            data.projectileSpeed = projectileSpeed;
            data.splashRadius = splashRadius;
            data.splashMinDamage = splashMin;
            data.splashMaxDamage = splashMax;
            data.minDamagePerSecond = minDps;
            data.maxDamagePerSecond = maxDps;
            data.attackVFX = vfx;
            return data;
        }
#endif

        private static ArenaCombatant SpawnBoss(Vector3 position)
        {
            var go = new GameObject("Mech_Boss");
            go.transform.position = position;

#if UNITY_EDITOR
            string robotPath = "Assets/Models/Characters/Mech/Mech_FinnTheFrog.fbx";
            GameObject modelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(robotPath);

            if (modelPrefab != null)
            {
                var model = Object.Instantiate(modelPrefab, go.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Default rotation
                model.transform.localScale = Vector3.one * 1.25f; // Scaled to 25% of previous 5x (1.25x player)
                
                // Remove existing colliders
                foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            }
            else
            {
                Debug.LogError("[ArenaBootstrap] Boss Mech FBX not found at " + robotPath);
            }
#endif
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.8f * 1.25f;
            col.radius = 0.4f * 1.25f;
            col.center = new Vector3(0, 0.9f * 1.25f, 0);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 120f; // Adjusted mass for smaller size (but still heavy)
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var combatant = go.AddComponent<ArenaCombatant>();
            combatant.displayName = "Mech Boss";
            combatant.teamId = 99;
            combatant.isPlayer = false;
            combatant.maxHp = 200f;
            combatant.hp = 200f;

            go.AddComponent<BossController>();
            
            var hpBar = go.AddComponent<WorldHPBar>();
            hpBar.combatant = combatant;
            hpBar.label = "Mech Boss";

            return combatant;
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
                    modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (modelPrefab != null)
            {
                var model = Instantiate(modelPrefab, go.transform);
                model.transform.localPosition = Vector3.zero;
                // The PlayerRobot prefab was built for Cinemachine with a baked -90° Y offset.
                // We apply that offset here so transform.forward of the root matches the visual facing direction.
                model.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                model.transform.localScale = Vector3.one;
                
                if (!isPlayer)
                {
                    var compsToDisable = model.GetComponentsInChildren<Behaviour>(true);
                    foreach (var c in compsToDisable)
                    {
                        string typeName = c.GetType().Name;
                        if (typeName.Contains("Camera") || typeName.Contains("Listener") || 
                            typeName.Contains("Cinemachine") || typeName.Contains("ThirdPersonController") ||
                            typeName.Contains("StarterAssetsInputs") || typeName.Contains("PlayerInput") ||
                            typeName.Contains("RespawnPlayer") || typeName == "CharacterController" ||
                            typeName == "UniversalAdditionalCameraData")
                        {
                            c.enabled = false;
                        }
                    }

                    var renderers = model.GetComponentsInChildren<Renderer>();
                    Color rndColor = Random.ColorHSV(0, 1, 0.4f, 1, 0.4f, 1);
                    foreach (var r in renderers)
                    {
                        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        mat.color = rndColor;
                        r.material = mat;
                    }
                }

                // Refined cleanup: remove StarterAssets/Camera components in two passes to respect dependencies
                var allComps = model.GetComponentsInChildren<Component>(true);
                var toRemove = new System.Collections.Generic.List<Component>();

                foreach (var c in allComps)
                {
                    if (c == null || c is Transform) continue;
                    string typeName = c.GetType().Name;
                    
                    if (typeName.Contains("Camera") || typeName.Contains("AudioListener") || 
                        typeName.Contains("Cinemachine") || typeName.Contains("ThirdPersonController") ||
                        typeName.Contains("StarterAssetsInputs") || typeName.Contains("PlayerInput") ||
                        typeName.Contains("RespawnPlayer") || typeName == "CharacterController" ||
                        typeName == "UniversalAdditionalCameraData")
                    {
                        toRemove.Add(c);
                    }
                }

                // Pass 1: Remove Scripts first (they might depend on engine components)
                foreach (var c in toRemove) {
                    if (c != null && c is MonoBehaviour) {
                        try { Object.DestroyImmediate(c); } catch { }
                    }
                }
                // Pass 2: Remove remaining engine components
                foreach (var c in toRemove) {
                    if (c != null) {
                        try { Object.DestroyImmediate(c); } catch { }
                    }
                }

                // Explicitly destroy pure camera-rig children (no visual content, safe to delete)
                var cameraRigNames = new[] { "RobotCamera", "PlayerFollowCamera" };
                foreach (var rigName in cameraRigNames)
                {
                    var rigTransform = model.transform.Find(rigName);
                    if (rigTransform != null)
                        Object.DestroyImmediate(rigTransform.gameObject);
                }

                // 4. Setup Animation
                foreach (var anim in model.GetComponentsInChildren<Animator>(true))
                {
                    if (anim.gameObject.GetComponent<AnimationEventReceiver>() == null)
                        anim.gameObject.AddComponent<AnimationEventReceiver>();
                }
                Debug.Log($"[ArenaBootstrap] SpawnFighter: {fighterName} model cleaned and initialized.");

                foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            }
#endif
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
            go.AddComponent<PlayerWeaponSystem>();

            if (isPlayer) 
            {
                go.tag = "Player";
                var pc = go.AddComponent<PlayerController>();
                pc.jumpForce = 25f; // High enough to jump from green area onto white platform
            }
            else 
            {
                go.AddComponent<BotController>();
                // Add floating HP bar above enemy head
                var hpBar = go.AddComponent<WorldHPBar>();
                hpBar.combatant = combatant;
                hpBar.label = fighterName;
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
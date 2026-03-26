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

        private void Start()
        {
            Debug.Log("[ArenaBootstrap] Start: Initializing Horde Survival arena...");
            BuildEnvironment();
            
            // Wait for user to choose bots if in arena directly
            var hud = FindAnyObjectByType<ArenaHUD>();
            if (hud != null)
            {
                hud.Initialize(null); // Initial setup
                hud.ShowMatchSetup((botCount) => {
                    BuildArenaMatch(botCount);
                });
            }
            else
            {
                BuildArenaMatch();
            }
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

        private void BuildArenaMatch(int overrideBotCount = -1)
        {
            // Read settings from WelcomeScreen (PlayerPrefs)
            string playerName = PlayerPrefs.GetString("PlayerName", defaultPlayerName);
            int botCount = overrideBotCount >= 0 ? overrideBotCount : PlayerPrefs.GetInt("BotCount", defaultBots);
            Debug.Log($"[ArenaBootstrap] BuildArenaMatch: Player={playerName}, AlliedBots={botCount}");

            // Spawn Player (Team 1)
            var player = SpawnFighter(playerName, new Vector3(0f, 1.2f, -6f), new Color(0.2f, 0.8f, 1f), 1, true);
            if (player != null)
                player.displayName = playerName;

            // Spawn Allied Bots (also Team 1 - NO friendly fire)
            float radius = 8f;
            for (int i = 0; i < botCount; i++)
            {
                float angle = (Mathf.PI * 2f / Mathf.Max(1, botCount)) * i;
                Vector3 pos = new Vector3(Mathf.Cos(angle), 1.2f, Mathf.Sin(angle)) * radius;
                var bot = SpawnFighter($"Ally_{i + 1}", pos, new Color(0.4f, 1f, 0.4f), 1, false); // Same team as player!
                if (bot != null) bot.displayName = $"Ally Bot {i + 1}";
            }

            if (player != null)
            {
                SetupMainCamera(player.transform);
            }
            else
            {
                Debug.LogError("[ArenaBootstrap] BuildArenaMatch: Player spawn FAILED!");
                return;
            }

            // Setup Game Manager
            var gmGo = new GameObject("ArenaGameManager");
            var gm = gmGo.AddComponent<ArenaGameManager>();
            gm.player = player;

            // Setup HUD
            var hudGo = new GameObject("ArenaHUD");
            var hud = hudGo.AddComponent<ArenaHUD>();
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
            var terrainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonGeneric/Materials/Generic_Overview_Map_Ground.mat");
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
                    if (terrainMat != null) tile.GetComponent<Renderer>().material = terrainMat;
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
                            typeName.Contains("Input") || typeName.Contains("Cinemachine") || typeName.Contains("Audio"))
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

                // Refined cleanup: remove StarterAssets/Camera components while preserving ALL GameObjects
                var allComps = model.GetComponentsInChildren<Component>(true);
                foreach (var c in allComps)
                {
                    if (c == null || c is Transform) continue;
                    string typeName = c.GetType().Name;
                    
                    bool shouldRemove = false;
                    if (typeName.Contains("Camera") || typeName.Contains("AudioListener") || 
                        typeName.Contains("Cinemachine") || typeName.Contains("ThirdPersonController") ||
                        typeName.Contains("StarterAssetsInputs") || typeName.Contains("PlayerInput") ||
                        typeName.Contains("RespawnPlayer") || typeName == "CharacterController" ||
                        typeName == "UniversalAdditionalCameraData")
                    {
                        shouldRemove = true;
                    }

                    if (shouldRemove)
                    {
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

            if (isPlayer) 
            {
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
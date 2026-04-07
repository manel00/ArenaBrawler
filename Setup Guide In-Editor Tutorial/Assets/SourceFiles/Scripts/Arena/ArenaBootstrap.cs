using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ArenaEnhanced
{
    /// <summary>
    /// Bootstrap principal del Arena - orquesta la inicialización del juego
    /// Refactorizado para usar módulos especializados
    /// </summary>
    public class ArenaBootstrap : MonoBehaviour
    {
        [Header("Fallback Settings")]
        public int defaultBots = 3;
        public string defaultPlayerName = "Survivor";
        public Action<string, int, string> OnStartGame;

        [Header("Testing")]
        [Tooltip("Auto-start game immediately (skip welcome screen for testing)")]
        public bool autoStartForTesting = false;

        private void Awake()
        {
            // Eliminar TimmyRobot INMEDIATAMENTE si existe (no queremos ese personaje)
            var timmyRobot = GameObject.Find("TimmyRobot");
            if (timmyRobot != null)
            {
                DestroyImmediate(timmyRobot);
            }
        }

        private void Start()
        {
            SetupObjectPools();
            
            if (autoStartForTesting)
            {
                StartGame("original");
                return;
            }

            if (PlayerPrefs.GetString("FromWelcomeScreen", "false") == "true")
            {
                PlayerPrefs.DeleteKey("FromWelcomeScreen");
                PlayerPrefs.Save();
                
                string selectedMap = PlayerPrefs.GetString("SelectedMap", "original");
                StartGame(selectedMap);
                return;
            }
            
            ShowWelcomeScreen();
        }

        private void StartGame(string mapId)
        {
            ArenaEnvironmentBuilder.BuildEnvironment(mapId);
            BuildArenaMatch(mapId);
        }

        private void ShowWelcomeScreen()
        {
            var welcomeGo = new GameObject("WelcomeScreen");
            var welcome = welcomeGo.AddComponent<WelcomeScreenUI>();
            
            welcome.OnStartGame = (playerName, botCount, selectedMap) => {
                PlayerPrefs.SetString("PlayerName", playerName);
                PlayerPrefs.SetInt("BotCount", botCount);
                PlayerPrefs.SetString("SelectedMap", selectedMap);
                PlayerPrefs.SetString("GameMode", "Solo");
                PlayerPrefs.SetString("FromWelcomeScreen", "true");
                PlayerPrefs.Save();

                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            };
        }

        private void BuildArenaMatch(string mapId)
        {
            Vector3 playerSpawn = new Vector3(0f, 1.2f, -6f);
            float botRadius = 8f;
            float arenaRadius = 38f;
            
            switch (mapId)
            {
                case "forest":
                case "forestarena":
                    playerSpawn = new Vector3(0f, 1.2f, -10f);
                    botRadius = 10f;
                    arenaRadius = 45f;
                    break;
                case "rocky":
                case "rockycanyon":
                    playerSpawn = new Vector3(0f, 2f, -12f);
                    botRadius = 12f;
                    arenaRadius = 50f;
                    break;
                case "deadwoods":
                    playerSpawn = new Vector3(0f, 1.2f, -8f);
                    botRadius = 9f;
                    arenaRadius = 42f;
                    break;
                case "mushroom":
                case "mushroomgrove":
                    playerSpawn = new Vector3(0f, 1.2f, -10f);
                    botRadius = 10f;
                    arenaRadius = 44f;
                    break;
                case "water":
                case "waterarena":
                    playerSpawn = new Vector3(0f, 1.5f, -8f);
                    botRadius = 10f;
                    arenaRadius = 40f;
                    break;
                case "korean":
                case "koreantemple":
                    playerSpawn = new Vector3(0f, 1.2f, -15f);
                    botRadius = 12f;
                    arenaRadius = 48f;
                    break;
                case "volcanic":
                    playerSpawn = new Vector3(0f, 1.2f, -8f);
                    botRadius = 8f;
                    arenaRadius = 35f;
                    break;
            }
            
            string playerName = PlayerPrefs.GetString("PlayerName", defaultPlayerName);
            int botCount = PlayerPrefs.GetInt("BotCount", defaultBots);
            
            List<ArenaCombatant> alliedCombatants = new List<ArenaCombatant>();

            // Spawn jugador con suelo garantizado (raycast desde arriba)
            Vector3 safePlayerSpawn = FindGroundPosition(playerSpawn);
            var player = ArenaFighterSpawner.SpawnPlayer(playerName, safePlayerSpawn, 1);
            if (player != null)
            {
                player.displayName = playerName;
                alliedCombatants.Add(player);

                if (player.GetComponent<KatanaWeapon>() == null)
                    player.gameObject.AddComponent<KatanaWeapon>();
            }

            for (int i = 0; i < botCount; i++)
            {
                float angle = (Mathf.PI * 2f / Mathf.Max(1, botCount)) * i;
                Vector3 pos = new Vector3(Mathf.Cos(angle), 1.2f, Mathf.Sin(angle)) * botRadius;
                var bot = ArenaFighterSpawner.SpawnAllyBot($"Ally_{i + 1}", pos, 1, i);
                if (bot != null) alliedCombatants.Add(bot);
            }

            if (player != null)
            {
                SetupMainCamera(player.transform);
                SetupHUD(player);
                SetupMinimap(player, alliedCombatants);
            }
            else
            {
                return;
            }

            ArenaWeaponSpawner.SpawnWeaponsOnFloor(alliedCombatants);
            SetupGameManager(player);
            SetupHordeManager(player, arenaRadius);
        }

        private void SetupMainCamera(Transform target)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var allCams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
                cam = allCams.FirstOrDefault(c => c.transform.root == c.transform || c.transform.root.name == "ArenaManager");
                
                if (cam == null)
                {
                    var camGo = new GameObject("Main Camera");
                    cam = camGo.AddComponent<Camera>();
                    camGo.AddComponent<AudioListener>();
                    camGo.tag = "MainCamera";
                    cam.clearFlags = CameraClearFlags.Skybox;
                    cam.fieldOfView = 60f;
                    cam.nearClipPlane = 0.1f;
                    cam.farClipPlane = 1000f;
                }
            }

            var allListeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
            for (int i = 0; i < allListeners.Length; i++)
            {
                allListeners[i].enabled = (allListeners[i].gameObject == cam.gameObject);
            }

            var follow = cam.GetComponent<ArenaCameraFollow>() ?? cam.gameObject.AddComponent<ArenaCameraFollow>();
            follow.target = target;
        }

        private void SetupHUD(ArenaCombatant player)
        {
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
        }

        private void SetupMinimap(ArenaCombatant player, List<ArenaCombatant> allies)
        {
            var minimapGo = new GameObject("MinimapSystem");
            minimapGo.AddComponent<MinimapSystem>();

            foreach (var ally in allies)
            {
                if (ally != null && !ally.isPlayer)
                {
                    if (MinimapSystem.Instance != null)
                    {
                        MinimapSystem.Instance.RegisterEntity(ally.transform, MinimapSystem.IconType.Ally);
                    }
                }
            }
        }

        private void SetupGameManager(ArenaCombatant player)
        {
            var gmGo = new GameObject("ArenaGameManager");
            var gm = gmGo.AddComponent<ArenaGameManager>();
            gm.player = player;
        }

        private void SetupHordeManager(ArenaCombatant player, float arenaRadius)
        {
            var waveGo = new GameObject("HordeWaveManager");
            var waveManager = waveGo.AddComponent<HordeWaveManager>();
            waveManager.arenaRadius = arenaRadius;
            waveManager.StartHorde(player);
        }

        private void SetupObjectPools()
        {
            // Verificar si ya existe (puede haber persistido de una escena anterior)
            if (GenericObjectPool.Instance != null) return;

            var poolGo = new GameObject("GenericObjectPool");
            UnityEngine.Object.DontDestroyOnLoad(poolGo); // Persistir entre recargas de escena
            
            var pool = poolGo.AddComponent<GenericObjectPool>();
            
            // Crear pool de Fireball dinámicamente
            var fireballPrefab = CreateFireballPrefab();
            UnityEngine.Object.DontDestroyOnLoad(fireballPrefab); // El prefab también debe persistir
            pool.CreatePool("Fireball", fireballPrefab, size: 10, maxSize: 20);
            
            // Crear pool de WeaponProjectile para rifles y escopetas
            var weaponProjectilePrefab = CreateWeaponProjectilePrefab();
            UnityEngine.Object.DontDestroyOnLoad(weaponProjectilePrefab);
            pool.CreatePool("WeaponProjectile", weaponProjectilePrefab, size: 20, maxSize: 50);
        }

        /// <summary>
        /// Encuentra el suelo real en una posición X,Z usando raycast desde arriba.
        /// </summary>
        private Vector3 FindGroundPosition(Vector3 desiredPos)
        {
            Vector3 rayStart = new Vector3(desiredPos.x, 100f, desiredPos.z);
            
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 200f, ~0))
            {
                // Asegurar que el suelo es horizontal (no precipicio)
                if (hit.normal.y > 0.5f)
                {
                    return hit.point + Vector3.up * 1.2f;
                }
            }
            
            // Fallback: usar altura original o 1.2f
            return new Vector3(desiredPos.x, Mathf.Max(desiredPos.y, 1.2f), desiredPos.z);
        }

        private GameObject CreateFireballPrefab()
        {
            var prefab = new GameObject("FireballPrefab");
            prefab.SetActive(false);
            
            // Rigidbody
            var rb = prefab.AddComponent<Rigidbody>();
            rb.mass = 0.8f;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            // Collider trigger
            var col = prefab.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
            
            // Componente de proyectil Fireball
            var projectile = prefab.AddComponent<FireballProjectile>();
            projectile.effectScale = 2f;
            projectile.effectPrefabPath = "KoreanTraditionalPattern_Effect/Prefabs/Fly/Fly03-05";
            projectile.speed = 25f;
            projectile.directDamage = 20f;
            projectile.splashDamage = 8f;
            projectile.splashRadius = 5f;
            
            return prefab;
        }
        
        private GameObject CreateWeaponProjectilePrefab()
        {
            var prefab = new GameObject("WeaponProjectilePrefab");
            prefab.SetActive(false);
            
            // Rigidbody
            var rb = prefab.AddComponent<Rigidbody>();
            rb.mass = 0.2f;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            
            // Collider trigger
            var col = prefab.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.25f;
            
            // Componente de proyectil de arma
            var projectile = prefab.AddComponent<WeaponProjectile>();
            
            // Efecto visual simple (trail)
            var trail = prefab.AddComponent<TrailRenderer>();
            trail.time = 0.3f;
            trail.startWidth = 0.2f;
            trail.endWidth = 0.05f;
            
            return prefab;
        }
    }
}

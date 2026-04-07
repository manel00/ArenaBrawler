using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ArenaEnhanced
{
    /// <summary>
    /// Main game manager for Horde Survival mode.
    /// Tracks player death (game over), wave victory, and restart.
    /// Integrates with HordeWaveManager.
    /// </summary>
    public class ArenaGameManager : MonoBehaviour
    {
        public static ArenaGameManager Instance { get; private set; }

        public ArenaCombatant player;
        public bool ended;
        public string endText = string.Empty;

        private void Awake()
        {
            Instance = this;
            // Subscribe immediately in Awake to catch early deaths
            ArenaCombatant.Died += OnCombatantDied;
        }

        private void OnEnable()
        {
            // Already subscribed in Awake, but ensure we're subscribed
            ArenaCombatant.Died -= OnCombatantDied;
            ArenaCombatant.Died += OnCombatantDied;
            // Auto-find player if not assigned
            if (player == null)
                FindPlayer();
        }

        private void Start()
        {
            // Ensure player is found on start as well
            if (player == null)
                FindPlayer();
        }

        private void FindPlayer()
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
            {
                player = playerGo.GetComponent<ArenaCombatant>();
            }
        }

        private void OnDisable()
        {
            ArenaCombatant.Died -= OnCombatantDied;
        }

        private float _playerSearchTimer = 0f;
        private const float PLAYER_SEARCH_INTERVAL = 1f;

        private void Update()
        {
            // Buscar jugador si no lo hemos encontrado aún
            if (player == null)
            {
                _playerSearchTimer += Time.deltaTime;
                if (_playerSearchTimer >= PLAYER_SEARCH_INTERVAL)
                {
                    _playerSearchTimer = 0f;
                    FindPlayer();
                }
            }

            bool restartPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) restartPressed = true;
#endif
            if (Input.GetKeyDown(KeyCode.R)) restartPressed = true;

            if (ended && restartPressed)
            {
                // If the player died, we respawn them in the same session
                if (endText.Contains("DERROTADO") && player != null)
                {
                    ended = false;
                    endText = string.Empty;
                    RespawnPlayerRandomly();
                }
                else
                {
                    // If they won or other cases, reload the scene
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
            }
        }

        private void RespawnPlayerRandomly()
        {
            // Generar posición aleatoria dentro de 50m del centro (como pide el usuario)
            Vector2 randomPos = Random.insideUnitCircle * Random.Range(5f, 45f); // 5-45m del centro
            Vector3 rayStart = new Vector3(randomPos.x, 100f, randomPos.y);
            
            // Raycast desde arriba para encontrar suelo
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 200f, ~0))
            {
                // Verificar que el suelo es más o menos horizontal (no precipicio)
                if (hit.normal.y > 0.5f)
                {
                    Vector3 spawnPos = hit.point + Vector3.up * 1.5f;
                    player.Respawn(spawnPos);
                    Debug.Log($"[ArenaGameManager] Player respawned at {spawnPos}");
                    return;
                }
            }
            
            // Fallback: intentar en el centro (0,0)
            if (Physics.Raycast(new Vector3(0f, 100f, 0f), Vector3.down, out RaycastHit centerHit, 200f))
            {
                Vector3 centerSpawn = centerHit.point + Vector3.up * 1.5f;
                player.Respawn(centerSpawn);
                Debug.Log($"[ArenaGameManager] Player respawned at center {centerSpawn}");
                return;
            }
            
            // Último recurso: forzar posición segura
            Debug.LogError("[ArenaGameManager] CRITICAL: No ground found! Forcing spawn at (0, 2, 0)");
            player.Respawn(new Vector3(0f, 2f, 0f));
        }

        private void OnCombatantDied(ArenaCombatant killer, ArenaCombatant victim)
        {
            // Also check by tag if player reference is null
            if (player == null)
            {
                var playerGo = GameObject.FindGameObjectWithTag("Player");
                if (playerGo != null)
                {
                    player = playerGo.GetComponent<ArenaCombatant>();
                }
            }
            
            // Check if victim is player by multiple methods
            bool isPlayerVictim = victim != null && (
                victim == player || 
                victim.isPlayer || 
                victim.teamId == 0 ||
                victim.CompareTag("Player")
            );
            
            if (isPlayerVictim && !ended)
            {
                ended = true;
                endText = "HAS SIDO DERROTADO\n<size=24>Presiona [R] para reintentar</size>";
            }
        }

        /// <summary>
        /// Called by HordeWaveManager when all 3 waves are cleared.
        /// </summary>
        public void TriggerVictory()
        {
            if (!ended)
            {
                ended = true;
                endText = "¡BIODEATH CONQUISTADO!\n<size=24>Presiona [R] para reiniciar</size>";
            }
        }
    }
}
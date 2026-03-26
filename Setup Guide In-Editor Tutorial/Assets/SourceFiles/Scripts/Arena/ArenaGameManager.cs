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
        }

        private void OnEnable()
        {
            ArenaCombatant.Died += OnCombatantDied;
        }

        private void OnDisable()
        {
            ArenaCombatant.Died -= OnCombatantDied;
        }

        private void Update()
        {
            bool restartPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) restartPressed = true;
#endif
            if (Input.GetKeyDown(KeyCode.R)) restartPressed = true;

            if (ended && restartPressed)
            {
                Debug.Log($"[ArenaGameManager] Restart requested. Result: {endText}");
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
            // The white square is Central (30x30m), so 15m radius
            Vector2 rc = Random.insideUnitCircle * 14f; // 14f to stay slightly inside the 15f bounds
            Vector3 pos = new Vector3(rc.x, 50f, rc.y);
            
            if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 100f))
            {
                player.Respawn(hit.point + Vector3.up * 1.5f); // Spawn slightly above ground
            }
            else
            {
                player.Respawn(new Vector3(rc.x, 1.5f, rc.y));
            }
            
            Debug.Log($"[ArenaGameManager] Player respawned at {player.transform.position} on the white square.");
        }

        private void OnCombatantDied(ArenaCombatant killer, ArenaCombatant victim)
        {
            Debug.Log($"[ArenaGameManager] {victim?.displayName} died. Killer: {killer?.displayName}");
            if (victim != null && victim == player)
            {
                ended = true;
                endText = "HAS SIDO DERROTADO\n<size=24>Presiona [R] para reintentar</size>";
                Debug.Log("[ArenaGameManager] Player died - GAME OVER");
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
                Debug.Log("[ArenaGameManager] ALL WAVES CLEARED - VICTORY!");
            }
        }
    }
}
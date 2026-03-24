using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace ArenaEnhanced
{
    /// <summary>
    /// Gestiona el juego de la arena, incluyendo victoria/derrota y reinicio.
    /// </summary>
    public class ArenaGameManager : MonoBehaviour
    {
        public ArenaCombatant player;
        public bool ended;
        public string endText = string.Empty;

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
            // Esperar un par de segundos para que todo se inicialice antes de comprobar victoria
            if (!ended && Time.timeSinceLevelLoad > 2f) 
                CheckWin();

            if (ended && Input.GetKeyDown(KeyCode.R))
            {
                if (endText == "HAS SIDO DERROTADO" && player != null)
                {
                    ended = false;
                    endText = string.Empty;
                    RespawnPlayerRandomly();
                }
                else
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
            }
        }

        private void RespawnPlayerRandomly()
        {
            Vector2 randomCircle = Random.insideUnitCircle * 30f;
            Vector3 randomPos = new Vector3(randomCircle.x, 50f, randomCircle.y);
            if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, 100f))
            {
                player.Respawn(hit.point);
            }
            else
            {
                player.Respawn(new Vector3(randomCircle.x, 2f, randomCircle.y)); // Fallback
            }
        }

        private void OnCombatantDied(ArenaCombatant victim, ArenaCombatant killer)
        {
            if (victim != null && victim == player)
            {
                ended = true;
                endText = "HAS SIDO DERROTADO";
            }
        }

        private void CheckWin()
        {
            var alive = ArenaCombatant.All
                .Where(c => c != null && c.IsAlive && c.countsForVictory)
                .ToList();

            if (alive.Count <= 1)
            {
                ended = true;
                if (alive.Count == 1)
                {
                    endText = (alive[0] == player) ? "¡VICTORIA MAGISTRAL!" : $"GANADOR: {alive[0].displayName}";
                }
                else
                {
                    endText = "EMPATE";
                }
            }
        }
    }
}
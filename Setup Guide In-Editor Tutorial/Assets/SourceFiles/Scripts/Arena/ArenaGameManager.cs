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
            if (!ended) CheckWin();

            if (ended && Input.GetKeyDown(KeyCode.R))
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnCombatantDied(ArenaCombatant victim, ArenaCombatant killer)
        {
            if (victim != null && victim.isPlayer)
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
                    endText = alive[0].isPlayer ? "¡VICTORIA MAGISTRAL!" : $"GANADOR: {alive[0].displayName}";
                }
                else
                {
                    endText = "EMPATE";
                }
            }
        }
    }
}
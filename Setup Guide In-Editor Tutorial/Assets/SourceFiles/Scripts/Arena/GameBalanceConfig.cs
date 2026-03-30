using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Configuración de balance para armas y habilidades.
    /// Centraliza todos los valores de balance para fácil ajuste.
    /// </summary>
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Arena/Game Balance Config")]
    public class GameBalanceConfig : ScriptableObject
    {
        [Header("Player Settings")]
        [Tooltip("Vida máxima del jugador")]
        public float playerMaxHealth = 100f;
        
        [Tooltip("Velocidad de movimiento base")]
        public float playerMoveSpeed = 12.5f;
        
        [Tooltip("Stamina máxima")]
        public float playerMaxStamina = 100f;
        
        [Tooltip("Regeneración de stamina por segundo")]
        public float staminaRegenRate = 20f;
        
        [Tooltip("Costo de stamina para dash")]
        public float dashStaminaCost = 25f;
        
        [Header("Weapon Balance")]
        [Tooltip("Multiplicador de daño global")]
        public float globalDamageMultiplier = 1f;
        
        [Tooltip("Multiplicador de cadencia de fuego")]
        public float globalFireRateMultiplier = 1f;
        
        [Tooltip("Multiplicador de velocidad de proyectil")]
        public float globalProjectileSpeedMultiplier = 1f;
        
        [Header("Difficulty Scaling")]
        [Tooltip("Multiplicador de vida de enemigos por oleada")]
        public float enemyHealthPerWave = 0.15f;
        
        [Tooltip("Multiplicador de daño de enemigos por oleada")]
        public float enemyDamagePerWave = 0.1f;
        
        [Tooltip("Multiplicador de velocidad de enemigos por oleada")]
        public float enemySpeedPerWave = 0.05f;
        
        [Header("Katana Balance")]
        [Tooltip("Daño por hit del combo rápido")]
        public float katanaRapidDamage = 18f;
        
        [Tooltip("Daño del ataque cargado")]
        public float katanaChargedDamage = 90f;
        
        [Tooltip("Cooldown después del combo")]
        public float katanaComboCooldown = 1.1f;
        
        [Tooltip("Cooldown después del ataque cargado")]
        public float katanaChargedCooldown = 1.8f;
        
        [Header("Fireball Balance")]
        [Tooltip("Daño base del fireball")]
        public float fireballDamage = 35f;
        
        [Tooltip("Velocidad del fireball")]
        public float fireballSpeed = 25f;
        
        [Tooltip("Cooldown del fireball")]
        public float fireballCooldown = 2f;
        
        [Header("Dog Summon Balance")]
        [Tooltip("Vida del perro")]
        public float dogMaxHealth = 150f;
        
        [Tooltip("Daño del perro")]
        public float dogDamage = 20f;
        
        [Tooltip("Cooldown de invocación")]
        public float dogSummonCooldown = 10f;
        
        [Tooltip("Duración del perro")]
        public float dogDuration = 30f;
        
        [Header("Melee Balance")]
        [Tooltip("Daño de puñetazo")]
        public float punchDamage = 15f;
        
        [Tooltip("Daño de patada")]
        public float kickDamage = 25f;
        
        [Tooltip("Cooldown de melee")]
        public float meleeCooldown = 0.5f;
        
        [Header("Weapon Specific")]
        [Tooltip("Daño del rifle de asalto")]
        public float assaultRifleDamage = 15f;
        
        [Tooltip("Daño por perdigón de escopeta")]
        public float shotgunPelletDamage = 12f;
        
        [Tooltip("Daño DPS del flamethrower")]
        public float flamethrowerDPS = 25f;
        
        /// <summary>
        /// Obtiene el multiplicador de dificultad para una oleada específica
        /// </summary>
        public float GetDifficultyMultiplier(int waveNumber)
        {
            return 1f + (waveNumber - 1) * 0.1f;
        }
        
        /// <summary>
        /// Aplica el multiplicador global al daño
        /// </summary>
        public float ApplyDamageModifiers(float baseDamage)
        {
            return baseDamage * globalDamageMultiplier;
        }
    }
}

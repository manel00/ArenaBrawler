using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// ScriptableObject base para todas las habilidades del juego.
    /// Permite crear habilidades genéricas con diferentes comportamientos.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbility", menuName = "Arena/Ability")]
    public class AbilityData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Nombre único de la habilidad")]
        public string abilityName = "New Ability";
        
        [Tooltip("Descripción visible para el jugador")]
        [TextArea(2, 4)]
        public string description = "Ability description";
        
        [Tooltip("Icono para la UI")]
        public Sprite icon;
        
        [Header("Input")]
        [Tooltip("Tecla asignada (1-9)")]
        [Range(1, 9)]
        public int keyBinding = 1;
        
        [Header("Cooldown & Cost")]
        [Tooltip("Tiempo de recarga en segundos")]
        public float cooldown = 1f;
        
        [Tooltip("Costo de stamina")]
        public float staminaCost = 0f;
        
        [Tooltip("Costo de mana (si aplica)")]
        public float manaCost = 0f;
        
        [Header("Damage")]
        [Tooltip("Daño base")]
        public float damage = 0f;
        
        [Tooltip("Rango de ataque")]
        public float range = 5f;
        
        [Header("VFX")]
        [Tooltip("Prefab de efecto visual al activar")]
        public GameObject vfxPrefab;
        
        [Tooltip("Sonido al activar")]
        public AudioClip soundEffect;
        
        [Header("Behaviour")]
        [Tooltip("Tipo de habilidad")]
        public AbilityType abilityType = AbilityType.Instant;
        
        [Tooltip("Si requiere target válido")]
        public bool requiresTarget = false;
        
        [Tooltip("Si puede usarse en movimiento")]
        public bool canUseWhileMoving = true;
        
        [Tooltip("Tiempo de casteo (0 para instantáneo)")]
        public float castTime = 0f;
        
        /// <summary>
        /// Ejecuta la habilidad. Override en clases hijas para comportamiento específico.
        /// </summary>
        public virtual bool Execute(ArenaCombatant caster, Vector3 targetPosition, ArenaCombatant target = null)
        {
            // Base implementation - consume costs and spawn VFX
            if (caster == null) return false;
            
            // Check cooldown (would need a cooldown tracker per caster)
            // Check costs
            if (staminaCost > 0 && caster.TryGetComponent(out PlayerController pc))
            {
                if (pc.GetCurrentStamina() < staminaCost) return false;
            }
            
            // Spawn VFX
            if (vfxPrefab != null)
            {
                Vector3 spawnPos = caster.transform.position + caster.transform.forward * 1.5f;
                var vfx = Instantiate(vfxPrefab, spawnPos, caster.transform.rotation);
                Destroy(vfx, 3f);
            }
            
            // Play sound
            if (soundEffect != null)
            {
                ArenaAudioManager.PlaySound(soundEffect);
            }
            
            return true;
        }
    }
    
    public enum AbilityType
    {
        Instant,        // Instantáneo (Fireball)
        Channeled,      // Canalizado (mantiene para continuar)
        CastTime,       // Tiempo de casteo
        Toggle,         // On/Off (modos)
        Passive         // Siempre activo
    }
}

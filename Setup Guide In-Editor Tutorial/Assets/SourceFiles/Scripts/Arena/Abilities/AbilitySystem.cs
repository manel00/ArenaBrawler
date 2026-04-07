using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de gestión de habilidades para combatientes.
    /// Reemplaza el switch-case de TryCastAbility con un sistema genérico.
    /// </summary>
    public class AbilitySystem : MonoBehaviour
    {
        [Header("Abilities")]
        [SerializeField] private AbilityData[] abilities = new AbilityData[9]; // Slots 1-9
        
        [Header("Cooldown Tracking")]
        [SerializeField] private float[] cooldownsEndTime = new float[9];
        
        private ArenaCombatant _combatant;
        private PlayerController _playerController;
        
        private void Awake()
        {
            _combatant = GetComponent<ArenaCombatant>();
            _playerController = GetComponent<PlayerController>();
        }
        
        private void OnEnable()
        {
            // No suscribirse si somos el jugador (PlayerController ya maneja habilidades directamente)
            // Esto evita el bug de doble invocación de perros y otras habilidades
            if (_playerController != null)
            {
                Debug.Log("[AbilitySystem] Disabled on Player - abilities handled by PlayerController");
                return;
            }
            
            InputManager.OnAbilityPressed += OnAbilityPressed;
        }
        
        private void OnDisable()
        {
            // Solo desuscribirse si no somos el jugador
            if (_playerController != null) return;
            
            InputManager.OnAbilityPressed -= OnAbilityPressed;
        }
        
        /// <summary>
        /// Intenta usar una habilidad del slot especificado
        /// </summary>
        public bool TryUseAbility(int slotIndex)
        {
            // Convert to 0-based index (slot 1 = index 0)
            int index = slotIndex - 1;
            if (index < 0 || index >= abilities.Length) return false;
            
            var ability = abilities[index];
            if (ability == null) return false;
            
            // Check cooldown
            if (Time.time < cooldownsEndTime[index])
            {
#if DEBUG
                Debug.Log($"[AbilitySystem] {ability.abilityName} on cooldown");
#endif
                return false;
            }
            
            // Check if can use while moving
            if (!ability.canUseWhileMoving)
            {
                // Would need movement check here
            }
            
            // Execute
            Vector3 targetPos = transform.position + transform.forward * ability.range;
            bool success = ability.Execute(_combatant, targetPos);
            
            if (success)
            {
                cooldownsEndTime[index] = Time.time + ability.cooldown;
            }
            
            return success;
        }
        
        /// <summary>
        /// Asigna una habilidad a un slot
        /// </summary>
        public void SetAbility(int slotIndex, AbilityData ability)
        {
            int index = slotIndex - 1;
            if (index >= 0 && index < abilities.Length)
            {
                abilities[index] = ability;
            }
        }
        
        /// <summary>
        /// Obtiene la habilidad de un slot
        /// </summary>
        public AbilityData GetAbility(int slotIndex)
        {
            int index = slotIndex - 1;
            if (index >= 0 && index < abilities.Length)
                return abilities[index];
            return null;
        }
        
        /// <summary>
        /// Obtiene el porcentaje de cooldown restante (0-1)
        /// </summary>
        public float GetCooldownPercentage(int slotIndex)
        {
            int index = slotIndex - 1;
            if (index < 0 || index >= abilities.Length) return 0f;
            
            var ability = abilities[index];
            if (ability == null || ability.cooldown <= 0) return 0f;
            
            float remaining = Mathf.Max(0, cooldownsEndTime[index] - Time.time);
            return remaining / ability.cooldown;
        }
        
        private void OnAbilityPressed(int abilityIndex)
        {
            // Si este objeto tiene un PlayerController, no procesar aquí
            // porque el PlayerController maneja sus propias habilidades directamente
            if (_playerController != null && _playerController.enabled)
            {
                return;
            }
            
            TryUseAbility(abilityIndex);
        }
    }
}

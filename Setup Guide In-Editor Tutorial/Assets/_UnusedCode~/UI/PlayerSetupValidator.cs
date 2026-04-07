using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Asegura que el jugador tenga todos los componentes necesarios
    /// Ejecutar esto si hay errores de componentes faltantes
    /// </summary>
    public class PlayerSetupValidator : MonoBehaviour
    {
        [Header("Configuración del Jugador")]
        public float maxHealth = 100f;
        public int teamId = 1;
        
        private void Awake()
        {
            ValidatePlayerSetup();
        }
        
        private void ValidatePlayerSetup()
        {
            Debug.Log("[PlayerSetupValidator] Validando configuración del jugador...");
            
            // 1. Asegurar tag Player
            if (!CompareTag("Player"))
            {
                tag = "Player";
                Debug.Log("✅ Tag 'Player' asignado");
            }
            
            // 2. ArenaCombatant
            var combatant = GetComponent<ArenaCombatant>();
            if (combatant == null)
            {
                combatant = gameObject.AddComponent<ArenaCombatant>();
                combatant.teamId = teamId;
                combatant.displayName = "Player";
                combatant.maxHp = maxHealth;
                combatant.hp = maxHealth;
                Debug.Log("✅ ArenaCombatant agregado");
            }
            
            // 3. Rigidbody
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.freezeRotation = true;
                Debug.Log("✅ Rigidbody agregado");
            }
            
            // 4. Collider
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.height = 2f;
                capsule.radius = 0.5f;
                capsule.center = new Vector3(0, 1, 0);
                Debug.Log("✅ CapsuleCollider agregado");
            }
            
            // 5. PlayerController
            var controller = GetComponent<PlayerController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<PlayerController>();
                Debug.Log("✅ PlayerController agregado");
            }
            
            // 6. PlayerWeaponSystem
            var weaponSystem = GetComponent<PlayerWeaponSystem>();
            if (weaponSystem == null)
            {
                weaponSystem = gameObject.AddComponent<PlayerWeaponSystem>();
                Debug.Log("✅ PlayerWeaponSystem agregado");
            }
            
            // 7. KatanaWeapon
            var katana = GetComponent<KatanaWeapon>();
            if (katana == null)
            {
                katana = gameObject.AddComponent<KatanaWeapon>();
                Debug.Log("✅ KatanaWeapon agregado");
            }
            
            Debug.Log("[PlayerSetupValidator] ✅ Configuración completada!");
        }
    }
}

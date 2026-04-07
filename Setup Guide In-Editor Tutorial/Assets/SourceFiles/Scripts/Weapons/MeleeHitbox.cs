using UnityEngine;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Hitbox para armas melee - detecta colisiones durante ataques
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MeleeHitbox : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private float hitCooldown = 0.1f;
        [SerializeField] private bool showDebugGizmos = false;
        
        // State
        private bool _isActive = false;
        private WeaponData _weaponData;
        private ArenaCombatant _owner;
        private float _lastHitTime;
        private HashSet<ArenaCombatant> _hitTargets = new HashSet<ArenaCombatant>();
        
        // Components
        private Collider _collider;
        
        public bool IsActive => _isActive;
        
        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
            }
            
            _collider.isTrigger = true;
            SetActive(false);
        }
        
        public void Initialize(ArenaCombatant owner, WeaponData weaponData)
        {
            _owner = owner;
            _weaponData = weaponData;
            
            // Configurar collider según el arma
            SetupCollider();
        }
        
        private void SetupCollider()
        {
            if (_weaponData == null) return;
            
            // Ajustar tamaño del collider basado en el arma
            // Por defecto, ajustamos a un tamaño adecuado para una espada
            Vector3 size = new Vector3(0.15f, 0.05f, 0.8f);
            Vector3 center = new Vector3(0, 0, 0.4f);
            
            if (_collider is BoxCollider box)
            {
                box.size = size;
                box.center = center;
            }
            else if (_collider is CapsuleCollider cap)
            {
                cap.height = 0.8f;
                cap.radius = 0.08f;
                cap.direction = 2; // Z axis
                cap.center = center;
            }
        }
        
        public void SetActive(bool active)
        {
            _isActive = active;
            _collider.enabled = active;
            
            if (active)
            {
                _hitTargets.Clear();
                _lastHitTime = Time.time;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;
            if (_owner == null || _weaponData == null) return;
            
            // Evitar hits múltiples muy rápidos
            if (Time.time - _lastHitTime < hitCooldown) return;
            
            // Buscar el combatant en el objeto o sus padres
            ArenaCombatant target = other.GetComponentInParent<ArenaCombatant>();
            if (target == null) return;
            
            // No dañarse a sí mismo
            if (target == _owner) return;
            
            // No dañar aliados
            if (target.teamId == _owner.teamId) return;
            
            // No dañar si ya está muerto
            if (!target.IsAlive) return;
            
            // Evitar golpear el mismo objetivo múltiples veces en un ataque
            if (_hitTargets.Contains(target)) return;
            
            // Registrar hit
            _hitTargets.Add(target);
            _lastHitTime = Time.time;
            
            // Aplicar daño
            float damage = _weaponData.RollDamage();
            if (_owner != null)
            {
                damage *= _owner.damageMultiplier;
            }
            
            target.TakeDamage(damage, _owner);
            
            // Efectos
            SpawnHitEffect(other.ClosestPointOnBounds(transform.position));
            PlayHitSound();
            
            Debug.Log($"[MeleeHitbox] Hit {target.name} for {damage:F1} damage");
        }
        
        private void SpawnHitEffect(Vector3 position)
        {
            if (_weaponData?.attackVFX != null)
            {
                Instantiate(_weaponData.attackVFX, position, Quaternion.identity);
            }
            
            // TODO: Usar VFX específico de impacto de espada
        }
        
        private void PlayHitSound()
        {
            if (_weaponData?.attackSound != null)
            {
                AudioSource.PlayClipAtPoint(_weaponData.attackSound, transform.position);
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            Gizmos.color = _isActive ? Color.red : Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (_collider is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (_collider is CapsuleCollider cap)
            {
                Vector3 center = cap.center;
                float radius = cap.radius;
                float height = cap.height;
                
                // Simplified capsule visualization
                Gizmos.DrawWireCube(center, new Vector3(radius * 2, radius * 2, height));
            }
            else
            {
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 0.5f);
            }
        }
    }
}

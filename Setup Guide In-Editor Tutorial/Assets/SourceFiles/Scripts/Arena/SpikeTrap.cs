using UnityEngine;
using System.Collections;

namespace ArenaEnhanced
{
    /// <summary>
    /// Trampa de pinchos que ralentiza y daña a los enemigos
    /// </summary>
    public class SpikeTrap : MonoBehaviour
    {
        [Header("Trap Settings")]
        [Tooltip("Daño que causa la trampa")]
        [Range(5f, 50f)]
        [SerializeField] private float damage = 10f;
        
        [Tooltip("Multiplicador de velocidad durante la ralentización (0-1)")]
        [Range(0.1f, 0.9f)]
        [SerializeField] private float slowMultiplier = 0.5f;
        
        [Tooltip("Duración de la ralentización en segundos")]
        [Range(1f, 10f)]
        [SerializeField] private float slowDuration = 3f;
        
        [Tooltip("Tiempo de reactivación de la trampa")]
        [Range(5f, 30f)]
        [SerializeField] private float reactivationCooldown = 10f;
        
        [Header("Visual Effects")]
        [Tooltip("Efecto visual de los pinchos")]
        [SerializeField] private GameObject spikeEffect;
        
        [Tooltip("Efecto visual de sangre")]
        [SerializeField] private GameObject bloodEffect;
        
        private bool _isActive = true;
        private bool _isOnCooldown = false;
        private MeshRenderer _meshRenderer;
        private Collider _collider;
        private Coroutine _cooldownCoroutine;
        private Color _originalColor;
        
        // OverlapSphere buffer to avoid GC allocations
        private readonly Collider[] _overlapBuffer = new Collider[10];
        
        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _collider = GetComponent<Collider>();
            
            if (_meshRenderer != null)
            {
                _originalColor = _meshRenderer.material.color;
            }
        }
        
        private void OnEnable()
        {
            // Reset state when enabled from pool
            _isActive = true;
            _isOnCooldown = false;
            
            if (_meshRenderer != null)
            {
                _meshRenderer.material.color = _originalColor;
            }
            if (_collider != null)
            {
                _collider.enabled = true;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive || _isOnCooldown) return;
            
            // Check if enemy or boss steps on trap
            if (other.CompareTag("Enemy") || other.CompareTag("Boss"))
            {
                ActivateTrap(other.gameObject);
            }
            // Also affect player and allies
            else if (other.CompareTag("Player") || other.CompareTag("Ally"))
            {
                ActivateTrap(other.gameObject);
            }
        }
        
        private void ActivateTrap(GameObject target)
        {
            if (target == null) return;
            
            _isActive = false;
            
            // Spawn spike effect
            if (spikeEffect != null)
            {
                GameObject spike = Instantiate(spikeEffect, transform.position, Quaternion.identity);
                // Spike effect should self-destruct
            }
            
            // Spawn blood effect
            if (bloodEffect != null)
            {
                GameObject blood = Instantiate(bloodEffect, transform.position, Quaternion.identity);
                // Blood effect should self-destruct
            }
            
            // Apply damage and slow to target
            ArenaCombatant combatant = target.GetComponent<ArenaCombatant>();
            if (combatant != null)
            {
                combatant.TakeDamage(damage, (GameObject)null);
                
                // Apply slow effect
                StartCoroutine(ApplySlowEffect(combatant));
            }
            
            // Apply slow to movement controller
            PlayerController playerController = target.GetComponent<PlayerController>();
            if (playerController != null)
            {
                StartCoroutine(SlowPlayerMovement(playerController));
            }
            
            // Start cooldown
            _cooldownCoroutine = StartCoroutine(ReactivationCooldown());
            
            Debug.Log($"[SpikeTrap] Activated on {target.name}");
        }
        
        private IEnumerator ApplySlowEffect(ArenaCombatant combatant)
        {
            if (combatant == null) yield break;
            
            float originalSpeed = combatant.moveSpeed;
            combatant.moveSpeed *= slowMultiplier;
            
            yield return new WaitForSeconds(slowDuration);
            
            if (combatant != null)
            {
                combatant.moveSpeed = originalSpeed;
            }
        }
        
        private IEnumerator SlowPlayerMovement(PlayerController playerController)
        {
            if (playerController == null) yield break;
            
            float originalSpeed = playerController.moveSpeed;
            playerController.moveSpeed *= slowMultiplier;
            
            yield return new WaitForSeconds(slowDuration);
            
            if (playerController != null)
            {
                playerController.moveSpeed = originalSpeed;
            }
        }
        
        private IEnumerator ReactivationCooldown()
        {
            _isOnCooldown = true;
            
            // Visual feedback for cooldown
            if (_meshRenderer != null)
            {
                _meshRenderer.material.color = Color.gray;
            }
            
            yield return new WaitForSeconds(reactivationCooldown);
            
            if (_meshRenderer != null)
            {
                _meshRenderer.material.color = _originalColor;
            }
            
            _isActive = true;
            _isOnCooldown = false;
            _cooldownCoroutine = null;
            
            Debug.Log("[SpikeTrap] Reactivated");
        }
        
        public void ActivateByFire()
        {
            // Fire can trigger the trap
            if (!_isActive || _isOnCooldown) return;
            
            // Find nearest enemy to trigger using NonAlloc
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, 1f, _overlapBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col.CompareTag("Enemy") || col.CompareTag("Boss"))
                {
                    ActivateTrap(col.gameObject);
                    break;
                }
            }
        }
        
        public bool IsActive() => _isActive && !_isOnCooldown;
        
        private void OnDestroy()
        {
            if (_cooldownCoroutine != null)
            {
                StopCoroutine(_cooldownCoroutine);
            }
        }
    }
}
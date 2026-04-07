using System.Collections.Generic;
using UnityEngine;
using ArenaEnhanced.Interfaces;
using System.Collections; // Added for HashSet

namespace ArenaEnhanced
{
    /// <summary>
    /// Representa un combatiente en la arena (jugador o bot)
    /// </summary>
    public class ArenaCombatant : MonoBehaviour, IDamageable
    {
        // OPTIMIZATION: Changed from List to HashSet for O(1) Contains lookups
        private static readonly HashSet<ArenaCombatant> _allCombatants = new HashSet<ArenaCombatant>();
        public static HashSet<ArenaCombatant> All => _allCombatants;
        public static event System.Action<ArenaCombatant, ArenaCombatant> Died;

        [Header("Data & Identity")]
        [Tooltip("Datos del combatiente desde ScriptableObject")]
        [SerializeField] private CombatantData data;
        
        [Tooltip("Nombre para mostrar en UI")]
        [SerializeField] public string displayName = "Fighter";
        
        [Tooltip("ID del equipo (0 = jugador, 1+ = enemigos)")]
        [SerializeField] public int teamId = 0;
        
        [Tooltip("Si es controlado por el jugador")]
        [SerializeField] public bool isPlayer = false;
        
        [Tooltip("Si tiene barrera que bloquea daño")]
        [SerializeField] private bool hasBarrier = false;
        
        [Tooltip("Si cuenta para la condición de victoria")]
        [SerializeField] public bool countsForVictory = true;
        
        [Header("Runtime Stats")]
        [Tooltip("Salud máxima")]
        [SerializeField] public float maxHp = 100f;
        
        [Tooltip("Salud actual")]
        [SerializeField] public float hp = 100f;
        
        [Tooltip("Velocidad de movimiento")]
        [SerializeField] public float moveSpeed = 5f;
        
        [Tooltip("Daño base")]
        [SerializeField] private float damage = 10f;
        
        [Tooltip("Rango de ataque")]
        [SerializeField] private float attackRange = 2f;
        
        [Tooltip("Cooldown entre ataques")]
        [SerializeField] private float attackCooldown = 0.5f;
        
        [Tooltip("Multiplicador de daño")]
        [SerializeField] public float damageMultiplier = 1f;

        [Header("Shield")]
        [Tooltip("Si el escudo está activo")]
        [SerializeField] private bool shieldActive;
        
        [Tooltip("Tiempo cuando termina el escudo")]
        [SerializeField] private float shieldEndTime;
        
        [Tooltip("Porcentaje de absorción del escudo (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float shieldAbsorption = 0.8f;

        private float lastAttackTime;
        private Vector3 _spawnPosition;
        private float _knockbackEndTime;
        public bool IsInKnockback => Time.time < _knockbackEndTime;

        // Public properties for read-only access
        public string DisplayName => displayName;
        public int TeamId => teamId;
        public bool IsPlayer => isPlayer;
        public bool HasBarrier => hasBarrier;
        public float MoveSpeed => moveSpeed;
        public float Damage => damage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;

        // IDamageable implementation
        public bool IsAlive => hp > 0.01f;
        public float CurrentHealth => hp;
        public float MaxHealth => maxHp;
        public float HpPercent => hp / maxHp;
        
        // Events
        public event System.Action<float, GameObject, DamageType> OnDamageReceived;
        public event System.Action<GameObject> OnDeath;
        public event System.Action<ArenaCombatant> OnDeathCombatant;
        public event System.Action<float, float> OnHealthChanged;

        private void InitializeFromData()
        {
            if (data == null) return;
            displayName = data.displayName;
            maxHp = data.maxHp;
            hp = maxHp;
            moveSpeed = data.moveSpeed;
            damage = data.damage;
            attackRange = data.attackRange;
            attackCooldown = data.attackCooldown;
        }

        private void Start()
        {
            if (data != null) InitializeFromData();
            if (hp <= 0.01f) hp = maxHp;
            _spawnPosition = transform.position;
            OnHealthChanged?.Invoke(hp, maxHp);
        }

        private void OnEnable()
        {
            if (!_allCombatants.Contains(this)) 
            {
                _allCombatants.Add(this);
#if DEBUG
                Debug.Log($"[ArenaCombatant] {displayName} añadido a la lista global. Total: {_allCombatants.Count}");
#endif
            }
            
            // OPTIMIZATION: Registrar en SpatialGrid para búsquedas O(1)
            SpatialGrid.RegisterCombatant(this);
        }

        private void OnDisable()
        {
            if (_allCombatants.Contains(this))
            {
                _allCombatants.Remove(this);
#if DEBUG
                Debug.Log($"[ArenaCombatant] {displayName} removido de la lista global. Total: {_allCombatants.Count}");
#endif
            }
            
            // OPTIMIZATION: Desregistrar del SpatialGrid
            SpatialGrid.UnregisterCombatant(this);
        }

        private void Update()
        {
            if (shieldEndTime > 0f && Time.time > shieldEndTime)
            {
                shieldActive = false;
                shieldEndTime = 0f;
            }
            
            // OPTIMIZATION: Actualizar posición en SpatialGrid periódicamente
            if (Time.frameCount % 5 == 0) // Cada 5 frames
            {
                SpatialGrid.UpdateCombatantPosition(this);
            }
        }

        /// <summary>
        /// Recibe daño (implementación de IDamageable)
        /// </summary>
        public void TakeDamage(float amount, GameObject source = null, DamageType damageType = DamageType.Normal)
        {
            if (!IsAlive || hasBarrier) return;

            float finalDamage = amount;
            if (shieldActive) finalDamage *= (1f - shieldAbsorption);

            hp = Mathf.Max(0, hp - finalDamage);
            
#if DEBUG
            Debug.Log($"[ArenaCombatant] {displayName} (isPlayer={isPlayer}) recibió {finalDamage} daño. HP: {hp}/{maxHp}");
#endif
            
            OnHealthChanged?.Invoke(hp, maxHp);
            OnDamageReceived?.Invoke(finalDamage, source, damageType);

            // Show Damage Popup with type
            DamagePopup.Create(transform.position + Vector3.up * 1.5f, finalDamage, damageType);

            if (hp <= 0)
            {
#if DEBUG
                Debug.Log($"[ArenaCombatant] {displayName} (isPlayer={isPlayer}) HP <= 0, invoking Died event!");
#endif
                ArenaCombatant sourceCombatant = source?.GetComponent<ArenaCombatant>();
                Die();
                Died?.Invoke(sourceCombatant, this);
#if DEBUG
                Debug.Log($"[ArenaCombatant] Died event invoked. Subscribers: {Died?.GetInvocationList().Length ?? 0}");
#endif
            }
        }

        /// <summary>
        /// Recibe daño con fuente ArenaCombatant (compatibilidad)
        /// </summary>
        public void TakeDamage(float amount, ArenaCombatant source = null, DamageType damageType = DamageType.Normal)
        {
            TakeDamage(amount, source?.gameObject, damageType);
        }

        /// <summary>
        /// Recibe daño (overload legacy para compatibilidad)
        /// </summary>
        public void TakeDamage(float amount, GameObject source)
        {
            TakeDamage(amount, source, DamageType.Normal);
        }

        /// <summary>
        /// Recibe daño (overload legacy para compatibilidad con ArenaCombatant source)
        /// </summary>
        public void TakeDamage(float amount, ArenaCombatant source)
        {
            TakeDamage(amount, source?.gameObject, DamageType.Normal);
        }

        public void ActivateShield(float duration)
        {
            shieldActive = true;
            shieldEndTime = Time.time + duration;
        }

        /// <summary>
        /// Cura al combatiente
        /// </summary>
        public void Heal(float amount)
        {
            if (!IsAlive) return;

            hp = Mathf.Min(maxHp, hp + amount);
            OnHealthChanged?.Invoke(hp, maxHp);

            // Show healing popup
            DamagePopup.Create(transform.position + Vector3.up * 1.5f, amount, DamageType.Heal);

#if DEBUG
            Debug.Log($"[ArenaCombatant] {displayName} se curó {amount}. HP: {hp}/{maxHp}");
#endif
        }

        /// <summary>
        /// Ataca a otro combatiente
        /// </summary>
        public bool TryAttack(ArenaCombatant target)
        {
            if (target == null || !target.IsAlive || !IsAlive) return false;
            if (Time.time - lastAttackTime < attackCooldown) return false;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > attackRange) return false;

            lastAttackTime = Time.time;
            target.TakeDamage(damage * damageMultiplier, gameObject);

            return true;
        }

        private void Die()
        {
            if (hp > 0) hp = 0;
#if DEBUG
            Debug.Log($"[ArenaCombatant] {displayName} ha muerto.");
#endif
            
            OnDeath?.Invoke(gameObject);
            OnDeathCombatant?.Invoke(this);

            if (isPlayer)
            {
                var pc = GetComponent<PlayerController>();
                if (pc != null) pc.enabled = false;

                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.constraints = RigidbodyConstraints.None;
                    rb.AddTorque(-transform.right * 5f, ForceMode.Impulse);
                }
            }
            else
            {
                Destroy(gameObject, 2f);
            }
        }

        public void Respawn(Vector3 position)
        {
            hp = maxHp;

            // Re-enable CharacterController before moving (it may have been disabled during fight)
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false; // disable first to bypass physics overlap
            
            transform.position = position;
            transform.rotation = Quaternion.identity;

            if (cc != null) cc.enabled = true;
            
            if (isPlayer)
            {
                // Re-enable PlayerController script if it was blocked
                var pc = GetComponent<PlayerController>();
                if (pc != null) pc.enabled = true;

                // Reset rigidbody if present
                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.constraints = RigidbodyConstraints.FreezeRotation; 
                }

                var anim = GetComponentInChildren<Animator>();
                if (anim != null) anim.enabled = true;
            }

            gameObject.SetActive(true);
            OnHealthChanged?.Invoke(hp, maxHp);

#if DEBUG
            Debug.Log($"[ArenaCombatant] {displayName} respawneado en {position}.");
#endif
        }

        /// <summary>
        /// Aplica una fuerza de retroceso al combatiente
        /// </summary>
        public void ApplyKnockback(Vector3 force)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                _knockbackEndTime = Time.time + 0.5f;
                rb.AddForce(force, ForceMode.Impulse);
                
                var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null && agent.enabled)
                {
                    StartCoroutine(TemporaryDisableAgent(agent, 0.4f));
                }
            }
        }

        private System.Collections.IEnumerator TemporaryDisableAgent(UnityEngine.AI.NavMeshAgent agent, float duration)
        {
            agent.enabled = false;
            yield return new WaitForSeconds(duration);
            if (agent != null && IsAlive) agent.enabled = true;
        }

        // Context menu for debugging
        [ContextMenu("Take 10 Damage")]
        private void DebugTakeDamage()
        {
            TakeDamage(10f, gameObject);
        }

        [ContextMenu("Heal to Full")]
        private void DebugHeal()
        {
            Heal(maxHp);
        }

        [ContextMenu("Print Stats")]
        private void DebugPrintStats()
        {
            Debug.Log($"[ArenaCombatant] {displayName} - HP: {hp}/{maxHp}, Damage: {damage}, Speed: {moveSpeed}");
        }
    }
}
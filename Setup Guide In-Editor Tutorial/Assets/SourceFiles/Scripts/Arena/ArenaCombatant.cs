using System.Collections.Generic;
using UnityEngine;
using ArenaEnhanced.Interfaces;

namespace ArenaEnhanced
{
    /// <summary>
    /// Representa un combatiente en la arena (jugador o bot)
    /// </summary>
    public class ArenaCombatant : MonoBehaviour, IDamageable
    {
        private static readonly List<ArenaCombatant> _allCombatants = new List<ArenaCombatant>();
        public static List<ArenaCombatant> All => _allCombatants;
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
        public event System.Action<float, GameObject> OnDamageReceived;
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
                Debug.Log($"[ArenaCombatant] {displayName} añadido a la lista global. Total: {_allCombatants.Count}");
            }
        }

        private void OnDisable()
        {
            if (_allCombatants.Contains(this))
            {
                _allCombatants.Remove(this);
                Debug.Log($"[ArenaCombatant] {displayName} removido de la lista global. Total: {_allCombatants.Count}");
            }
        }

        private void Update()
        {
            if (shieldEndTime > 0f && Time.time > shieldEndTime)
            {
                shieldActive = false;
                shieldEndTime = 0f;
            }
        }

        /// <summary>
        /// Recibe daño (implementación de IDamageable)
        /// </summary>
        public void TakeDamage(float amount, GameObject source = null)
        {
            if (!IsAlive || hasBarrier) return;

            float finalDamage = amount;
            if (shieldActive) finalDamage *= (1f - shieldAbsorption);

            hp = Mathf.Max(0, hp - finalDamage);
            
            Debug.Log($"[ArenaCombatant] {displayName} recibió {finalDamage} daño. HP: {hp}/{maxHp}. Invocando OnHealthChanged...");
            
            OnHealthChanged?.Invoke(hp, maxHp);
            OnDamageReceived?.Invoke(finalDamage, source);

            // Show Damage Popup
            DamagePopup.Create(transform.position + Vector3.up * 1.5f, finalDamage);

            if (hp <= 0)
            {
                ArenaCombatant sourceCombatant = source?.GetComponent<ArenaCombatant>();
                Die();
                Died?.Invoke(sourceCombatant, this);
            }
        }

        /// <summary>
        /// Recibe daño con fuente ArenaCombatant (compatibilidad)
        /// </summary>
        public void TakeDamage(float amount, ArenaCombatant source = null)
        {
            TakeDamage(amount, source?.gameObject);
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

            Debug.Log($"[ArenaCombatant] {displayName} se curó {amount}. HP: {hp}/{maxHp}");
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
            Debug.Log($"[ArenaCombatant] {displayName} ha muerto.");
            
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

            Debug.Log($"[ArenaCombatant] {displayName} respawneado en {position}.");
        }

        /// <summary>
        /// Aplica una fuerza de retroceso al combatiente
        /// </summary>
        public void ApplyKnockback(Vector3 force)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
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
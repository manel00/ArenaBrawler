using System.Collections.Generic;
using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Representa un combatiente en la arena (jugador o bot)
    /// </summary>
    public class ArenaCombatant : MonoBehaviour
    {
        private static readonly List<ArenaCombatant> _allCombatants = new List<ArenaCombatant>();
        public static List<ArenaCombatant> All => _allCombatants;
        public static event System.Action<ArenaCombatant, ArenaCombatant> Died;

        [Header("Data & Identity")]
        public CombatantData data;
        public string displayName = "Fighter";
        public int teamId = 0;
        public bool isPlayer = false;
        public bool HasBarrier = false;
        public bool countsForVictory = true;
        public int level = 1;

        [Header("Runtime Stats")]
        public float maxHp = 100f;
        public float hp = 100f;
        public float moveSpeed = 5f;
        public float damage = 10f;
        public float attackRange = 2f;
        public float attackCooldown = 0.5f;
        public float damageMultiplier = 1f;
        
        [Header("Shield")]
        public bool shieldActive;
        public float shieldEndTime;
        public float shieldAbsorption = 0.8f;

        private float lastAttackTime;
        private Vector3 _spawnPosition;

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

        // Propiedades
        public bool IsAlive => hp > 0.01f;
        public bool Alive => IsAlive; // Alias para compatibilidad con ArenaEnhanced.cs
        public float HpPercent => hp / maxHp;

        // Eventos
        public System.Action<ArenaCombatant> OnDeath;
        public System.Action<float, float> OnHealthChanged;

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
        /// Recibe daño
        /// </summary>
        public void TakeDamage(float amount, ArenaCombatant source = null)
        {
            if (!IsAlive || HasBarrier) return;

            float finalDamage = amount;
            if (shieldActive) finalDamage *= (1f - shieldAbsorption);

            hp = Mathf.Max(0, hp - finalDamage);
            OnHealthChanged?.Invoke(hp, maxHp);

            Debug.Log($"[ArenaCombatant] {displayName} recibió {finalDamage} de daño. HP: {hp}/{maxHp}");

            if (hp <= 0)
            {
                Die();
                Died?.Invoke(source, this);
            }
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
            target.TakeDamage(damage * damageMultiplier, this);

            return true;
        }

        private void Die()
        {
            if (hp > 0) hp = 0; // Ensure it is exactly 0
            Debug.Log($"[ArenaCombatant] {displayName} ha muerto.");
            OnDeath?.Invoke(this);

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
            transform.position = position;
            transform.rotation = Quaternion.identity;
            
            if (isPlayer)
            {
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

            Debug.Log($"[ArenaCombatant] {displayName} respawneado.");
        }
    }
}
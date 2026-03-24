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

        [Header("Identity")]
        public string displayName = "Fighter";
        public int teamId = 0;
        public bool isPlayer = false;
        public bool HasBarrier = false;
        public bool countsForVictory = true;
        public int level = 1;

        [Header("Stats")]
        public float maxHp = 100f;
        public float hp = 100f;
        public float moveSpeed = 5f;
        
        [Header("Combate")]
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

        // Propiedades
        public bool IsAlive => hp > 0.01f;
        public bool Alive => IsAlive; // Alias para compatibilidad con ArenaEnhanced.cs
        public float HpPercent => hp / maxHp;

        // Eventos
        public System.Action<ArenaCombatant> OnDeath;
        public System.Action<float, float> OnHealthChanged;

        private void Awake()
        {
            hp = Mathf.Clamp(hp, 0f, maxHp);
            _spawnPosition = transform.position;
        }

        private void OnEnable()
        {
            if (!_allCombatants.Contains(this)) _allCombatants.Add(this);
        }

        private void OnDisable()
        {
            _allCombatants.Remove(this);
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
                if (source != null) Died?.Invoke(source, this);
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

        /// <summary>
        /// Muerte del combatiente
        /// </summary>
        private void Die()
        {
            Debug.Log($"[ArenaCombatant] {displayName} ha muerto.");
            OnDeath?.Invoke(this);

            // Opcional: destruir después de un tiempo si no es jugador
            if (!isPlayer) Destroy(gameObject, 2f);
        }

        /// <summary>
        /// Respawn del combatiente
        /// </summary>
        public void Respawn(Vector3 position)
        {
            hp = maxHp;
            transform.position = position;
            gameObject.SetActive(true);
            OnHealthChanged?.Invoke(hp, maxHp);

            Debug.Log($"[ArenaCombatant] {displayName} respawneado.");
        }
    }
}
using UnityEngine;
using UnityEngine.AI;

namespace ArenaEnhanced
{
    /// <summary>
    /// AI brain for all horde enemies.
    /// Ground enemies (Mono, Piranha, Sabrewulf): chase player/bots using NavMeshAgent.
    /// Flying enemies (Abeja): hover and swoop towards nearest target.
    /// Boss enemies (T-Rex family): chase and stomp - destroys environment on collision.
    /// No friendly fire: team 99 vs teams 1+ (players and allied bots).
    /// </summary>
    [RequireComponent(typeof(ArenaCombatant))]
    public class HordeEnemyAI : MonoBehaviour
    {
        [Header("Config")]
        public bool isFlying = false;
        public bool isBoss = false;
        public bool isDestructive = false; // Bosses destroy environment

        [Header("Combat")]
        public float attackRange = 1.8f;
        public float attackDamage = 12f;
        public float attackCooldown = 1.2f;
        public float moveSpeed = 4.8f; // Aumentado 20% (de 4f a 4.8f)
        public float flyingHoverHeight = 2.5f;
        public float detectRadius = 60f;

        [HideInInspector] public ArenaCombatant combatant;

        private NavMeshAgent _agent;
        private Rigidbody _rb;
        private ArenaCombatant _target;
        private float _nextAttackTime;
        private float _targetRefreshTimer;
        private const float TargetRefreshInterval = 1.5f;

        private void Awake()
        {
            combatant = GetComponent<ArenaCombatant>();
            _agent = GetComponent<NavMeshAgent>();
            _rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (combatant == null || !combatant.IsAlive) return;

            // Periodic target re-acquisition
            _targetRefreshTimer -= Time.deltaTime;
            if (_targetRefreshTimer <= 0f)
            {
                _target = FindNearestEnemy();
                _targetRefreshTimer = TargetRefreshInterval;
            }

            if (_target == null || !_target.IsAlive)
            {
                _target = FindNearestEnemy();
                if (_target == null) return;
            }

            float dist = Vector3.Distance(transform.position, _target.transform.position);

            if (isFlying)
                UpdateFlyingMovement(dist);
            else
                UpdateGroundMovement(dist);

            // Face target
            Vector3 dir = (_target.transform.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);

            // Attack
            if (dist <= attackRange && Time.time >= _nextAttackTime)
                PerformAttack();
        }

        private void UpdateGroundMovement(float dist)
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.speed = moveSpeed;
                _agent.SetDestination(_target.transform.position);

                // Sync rigidbody with NavMeshAgent desired velocity
                if (_rb != null)
                {
                    Vector3 vel = _agent.desiredVelocity;
                    vel.y = _rb.linearVelocity.y;
                    _rb.linearVelocity = vel;
                    _agent.nextPosition = transform.position;
                }
            }
            else if (_rb != null)
            {
                // Fallback: direct rigidbody movement
                Vector3 dir = (_target.transform.position - transform.position).normalized;
                dir.y = 0f;
                _rb.linearVelocity = new Vector3(dir.x * moveSpeed, _rb.linearVelocity.y, dir.z * moveSpeed);
            }
        }

        private void UpdateFlyingMovement(float dist)
        {
            if (_rb == null) return;

            Vector3 targetPos = _target.transform.position + Vector3.up * flyingHoverHeight;
            Vector3 dir = (targetPos - transform.position).normalized;
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, dir * moveSpeed, Time.deltaTime * 4f);
        }

        private void PerformAttack()
        {
            _nextAttackTime = Time.time + attackCooldown;

            if (_target != null && _target.IsAlive && _target.teamId != combatant.teamId)
            {
                _target.TakeDamage(attackDamage, combatant);
                VFXManager.SpawnMeleeEffect((_target.transform.position + transform.position) * 0.5f, transform.forward);
            }
        }

        private ArenaCombatant FindNearestEnemy()
        {
            ArenaCombatant best = null;
            float bestDist = detectRadius;

            foreach (var c in ArenaCombatant.All)
            {
                if (c == null || !c.IsAlive) continue;
                if (c.teamId == combatant.teamId) continue; // Same team - skip
                if (c.teamId == 99) continue; // Other enemies - skip (no infighting)

                float d = Vector3.Distance(transform.position, c.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }
            return best;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isDestructive) return;

            // Boss destroys tagged destructible environment objects
            var dest = collision.gameObject.GetComponent<DestructibleEnvironment>();
            if (dest != null)
            {
                dest.Shatter();
            }
        }
    }
}

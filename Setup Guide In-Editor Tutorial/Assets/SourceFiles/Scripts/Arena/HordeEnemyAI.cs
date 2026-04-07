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
        public float moveSpeed = 10.08f; // Reducido 30% (de 14.4f a 10.08f)
        public float flyingHoverHeight = 2.5f;
        public float detectRadius = 60f;

        [HideInInspector] public ArenaCombatant combatant;

        private NavMeshAgent _agent;
        private Rigidbody _rb;
        private ArenaCombatant _target;
        private float _nextAttackTime;
        private float _targetRefreshTimer;
        private const float TargetRefreshInterval = 1.5f;
        
        // OPTIMIZATION: Cache de target y solo recalcular cuando muere o está muy lejos
        private float _targetValidationTimer;
        private const float TargetValidationInterval = 0.5f;
        private float _targetLostDistanceSqr = 400f; // 20m^2 - recalcular si el target se aleja mucho

        private void Awake()
        {
            combatant = GetComponent<ArenaCombatant>();
            _agent = GetComponent<NavMeshAgent>();
            _rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (combatant == null || !combatant.IsAlive) return;

            // OPTIMIZATION: Validar target actual más frecuentemente que buscar nuevo
            _targetValidationTimer -= Time.deltaTime;
            if (_targetValidationTimer <= 0f)
            {
                _targetValidationTimer = TargetValidationInterval;
                
                // Si no hay target o está muerto o muy lejos, buscar nuevo
                if (_target == null || !_target.IsAlive || IsTargetTooFar())
                {
                    _target = FindNearestEnemy();
                }
            }

            // Periodic full search (más lento, para cambiar de target si hay uno mejor)
            _targetRefreshTimer -= Time.deltaTime;
            if (_targetRefreshTimer <= 0f)
            {
                _targetRefreshTimer = TargetRefreshInterval;
                
                // Solo buscar nuevo target si el actual es válido pero quizás hay uno mejor
                if (_target != null && _target.IsAlive)
                {
                    var betterTarget = FindBetterTarget();
                    if (betterTarget != null && betterTarget != _target)
                    {
                        float distToCurrent = Vector3.SqrMagnitude(transform.position - _target.transform.position);
                        float distToBetter = Vector3.SqrMagnitude(transform.position - betterTarget.transform.position);
                        
                        // Solo cambiar si el nuevo es significativamente más cercano (30% más cerca)
                        if (distToBetter < distToCurrent * 0.7f)
                        {
                            _target = betterTarget;
                        }
                    }
                }
                else
                {
                    _target = FindNearestEnemy();
                }
            }

            if (_target == null || !_target.IsAlive)
            {
                _target = FindNearestEnemy();
                // If still no target, move toward center to find player
                if (_target == null)
                {
                    MoveTowardCenter();
                    return;
                }
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
        
        /// <summary>
        /// Verifica si el target actual está demasiado lejos como para seguirlo
        /// </summary>
        private bool IsTargetTooFar()
        {
            if (_target == null) return true;
            return Vector3.SqrMagnitude(transform.position - _target.transform.position) > _targetLostDistanceSqr;
        }
        
        /// <summary>
        /// Busca un target potencialmente mejor que el actual (más cercano)
        /// </summary>
        private ArenaCombatant FindBetterTarget()
        {
            ArenaCombatant nearest = null;
            float bestSq = detectRadius * detectRadius;
            Vector3 myPos = transform.position;
            
            // OPTIMIZATION: Early exit si hay pocos combatientes
            int count = 0;
            foreach (var c in ArenaCombatant.All)
            {
                if (++count > 20) break; // Limitar búsqueda en hordas grandes
                
                if (c == null || !c.IsAlive || c == combatant || c.teamId == combatant.teamId) continue;
                if (c.teamId == 99) continue; // Other enemies - skip (no infighting)

                float sq = (c.transform.position - myPos).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    nearest = c;
                }
            }
            return nearest;
        }

        private void UpdateGroundMovement(float dist)
        {
            // No mover durante knockback
            if (combatant != null && combatant.IsInKnockback) return;
            if (_rb == null) return;

            Vector3 toTarget = _target.transform.position - transform.position;
            toTarget.y = 0f;
            
            // Detección de obstáculos simple
            Vector3 moveDir = toTarget.normalized;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, moveDir, out RaycastHit hit, 1.5f))
            {
                if (!hit.collider.CompareTag("Enemy") && !hit.collider.CompareTag("Player"))
                {
                    // Intentar desviarse ligeramente
                    Vector3 alternativeDir = Quaternion.Euler(0, 30, 0) * moveDir;
                    if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, alternativeDir, 1.5f))
                    {
                        moveDir = alternativeDir;
                    }
                    else
                    {
                        alternativeDir = Quaternion.Euler(0, -30, 0) * moveDir;
                        if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, alternativeDir, 1.5f))
                        {
                            moveDir = alternativeDir;
                        }
                    }
                }
            }

            // Aceleración suave hacia la velocidad objetivo
            Vector3 targetVelocity = moveDir * moveSpeed;
            targetVelocity.y = _rb.linearVelocity.y;
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, Time.deltaTime * 5f);
        }

        private void UpdateFlyingMovement(float dist)
        {
            if (_rb == null) return;

            Vector3 targetPos = _target.transform.position + Vector3.up * flyingHoverHeight;
            Vector3 dir = (targetPos - transform.position).normalized;
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, dir * moveSpeed, Time.deltaTime * 4f);
        }

        /// <summary>
        /// Mueve al enemigo hacia el centro del mapa cuando no tiene objetivo
        /// </summary>
        private void MoveTowardCenter()
        {
            if (_rb == null) return;
            if (combatant != null && combatant.IsInKnockback) return;

            Vector3 toCenter = -transform.position;
            toCenter.y = 0f;
            
            // Si ya está cerca del centro, no mover
            if (toCenter.sqrMagnitude < 4f) // 2m del centro
            {
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
                return;
            }

            Vector3 moveDir = toCenter.normalized;
            Vector3 targetVelocity = moveDir * (moveSpeed * 0.8f); // 80% de velocidad hacia centro
            targetVelocity.y = _rb.linearVelocity.y;
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, Time.deltaTime * 5f);

            // Rotar hacia el centro
            if (moveDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 8f);
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
            ArenaCombatant nearest = null;
            float bestSq = detectRadius * detectRadius;
            Vector3 myPos = transform.position;
            
            // OPTIMIZATION: Early exit y limitar búsqueda
            int count = 0;
            int maxChecks = 30; // Limitar búsqueda para performance
            
            foreach (var c in ArenaCombatant.All)
            {
                if (++count > maxChecks) break;
                
                if (c == null || !c.IsAlive || c == combatant) continue;
                if (c.teamId == combatant.teamId) continue; // Same team - skip
                if (c.teamId == 99) continue; // Other enemies - skip (no infighting)

                float sq = (c.transform.position - myPos).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    nearest = c;
                }
            }
            return nearest;
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

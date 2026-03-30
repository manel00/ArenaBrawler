using UnityEngine;
using System.Collections;

namespace ArenaEnhanced
{
    /// <summary>
    /// Controlador avanzado para bots en la arena con IA mejorada
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ArenaCombatant))]
    public class BotController : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Velocidad de movimiento del bot")]
        [Range(1f, 20f)]
        [SerializeField] private float moveSpeed = 6.5f;
        
        [Tooltip("Aceleración del bot")]
        [Range(5f, 50f)]
        [SerializeField] private float acceleration = 22f;
        
        [Tooltip("Intensidad del strafe lateral")]
        [Range(0f, 1f)]
        [SerializeField] private float strafeStrength = 0.35f;
        
        [Header("Combat")]
        [Tooltip("Distancia máxima de detección de enemigos")]
        [Range(5f, 50f)]
        [SerializeField] private float detectDistance = 25f;
        
        [Tooltip("Distancia preferida de combate")]
        [Range(2f, 20f)]
        [SerializeField] private float preferredDistance = 8f;
        
        [Tooltip("Velocidad de la bola de fuego")]
        [Range(10f, 50f)]
        [SerializeField] private float fireballSpeed = 20f;
        
        [Tooltip("Cooldown entre disparos")]
        [Range(0.5f, 3f)]
        [SerializeField] private float fireCooldown = 1.2f;
        
        [Header("Survival")]
        [Tooltip("Multiplicador de salud del bot")]
        [Range(0.5f, 3f)]
        [SerializeField] private float healthMultiplier = 1.5f;
        
        [Tooltip("Distancia a la que evade jefes")]
        [Range(5f, 30f)]
        [SerializeField] private float bossEvadeDistance = 15f;
        
        [Tooltip("Distancia de seguimiento del jugador")]
        [Range(1f, 15f)]
        [SerializeField] private float playerFollowDistance = 5f;
        
        [Tooltip("Distancia mínima entre bots para evitar agrupación")]
        [Range(1f, 10f)]
        [SerializeField] private float groupSpreadDistance = 3f;
        
        [Header("Tactical AI")]
        [Tooltip("Activar comportamiento de flanqueo")]
        [SerializeField] private bool enableFlanking = true;
        
        [Tooltip("Distancia lateral para flanquear (metros)")]
        [Range(2f, 15f)]
        [SerializeField] private float flankingDistance = 6f;
        
        [Tooltip("Probabilidad de intentar flanquear")]
        [Range(0f, 1f)]
        [SerializeField] private float flankingChance = 0.6f;
        
        [Tooltip("Priorizar enemigos con baja vida")]
        [SerializeField] private bool prioritizeLowHealth = true;
        
        [Tooltip("Multiplicador de puntuación para vida baja")]
        [Range(1f, 5f)]
        [SerializeField] private float lowHealthBonusMultiplier = 2f;

        [Header("Dependencies")]
        [SerializeField] private Transform playerTransform;

        private Rigidbody _rb;
        private ArenaCombatant _combatant;
        private int _currentPoints;
        private int _currentLevel;
        private Vector3 _flatVelocity;
        private float _nextFire;
        private float _strafeSeed;
        private Animator _animator;
        private PlayerWeaponSystem _weaponSystem;
        private ArenaCombatant _currentTarget;
        
        // Raycast buffers to avoid GC allocations
        private readonly RaycastHit[] _obstacleHitBuffer = new RaycastHit[1];
        private readonly RaycastHit[] _edgeHitBuffer = new RaycastHit[1];
        private readonly RaycastHit[] _combatHitBuffer = new RaycastHit[1];

        private static readonly int HashSpeed = Animator.StringToHash("Speed");

        private float _currentFlankAngle = 0f;
        private float _targetFlankAngle = 0f;
        private float _lastFlankChangeTime = 0f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _combatant = GetComponent<ArenaCombatant>();
            _animator = GetComponentInChildren<Animator>();
            _weaponSystem = GetComponent<PlayerWeaponSystem>();
            _nextFire = Time.time + Random.Range(0.2f, 1f);
            _strafeSeed = Random.Range(0f, 10f);
            
            // Apply health multiplier
            if (_combatant != null)
            {
                _combatant.maxHp *= healthMultiplier;
                _combatant.hp = _combatant.maxHp;
            }
        }
        
        private void Start()
        {
            // Try to find player if not assigned
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }
        }

        private void FixedUpdate()
        {
            if (_combatant == null || !_combatant.IsAlive) return;

            // Check for boss threats first
            if (ShouldEvadeBoss())
            {
                EvadeBoss();
                return;
            }

            // Find best target
            _currentTarget = FindBestTarget();
            
            if (_currentTarget == null)
            {
                // No enemies, follow player
                FollowPlayer();
                return;
            }

            // Execute combat behavior
            ExecuteCombat(_currentTarget);
        }
        
        public void SetPlayerTransform(Transform player)
        {
            playerTransform = player;
        }

        private bool ShouldEvadeBoss()
        {
            // Check for nearby bosses
            foreach (var c in ArenaCombatant.All)
            {
                if (c == null || !c.IsAlive || c == _combatant) continue;
                
                // Check if it's a boss (by name or tag)
                if (c.name.Contains("T-Rex") || c.CompareTag("Boss"))
                {
                    float distance = Vector3.Distance(transform.position, c.transform.position);
                    if (distance < bossEvadeDistance)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void EvadeBoss()
        {
            
            // Find nearest boss
            ArenaCombatant nearestBoss = null;
            float nearestDist = float.MaxValue;
            
            foreach (var c in ArenaCombatant.All)
            {
                if (c == null || !c.IsAlive || c == _combatant) continue;
                
                if (c.name.Contains("T-Rex") || c.CompareTag("Boss"))
                {
                    float distance = Vector3.Distance(transform.position, c.transform.position);
                    if (distance < nearestDist)
                    {
                        nearestDist = distance;
                        nearestBoss = c;
                    }
                }
            }
            
            if (nearestBoss != null)
            {
                // Run away from boss
                Vector3 awayDir = (transform.position - nearestBoss.transform.position).normalized;
                awayDir.y = 0;
                
                _flatVelocity = Vector3.MoveTowards(_flatVelocity, awayDir * moveSpeed * 1.2f, acceleration * Time.fixedDeltaTime);
                ApplyHorizontalVelocity();
                
                if (_animator != null)
                {
                    _animator.SetFloat(HashSpeed, _flatVelocity.magnitude);
                }
                
                // Look at boss while running away
                Vector3 look = nearestBoss.transform.position - transform.position;
                look.y = 0;
                if (look.sqrMagnitude > 0.01f)
                {
                    Quaternion rot = Quaternion.LookRotation(look.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rot, 5f * Time.fixedDeltaTime);
                }
            }
            
        }

        private void FollowPlayer()
        {
            if (playerTransform == null) return;
            
            Vector3 toPlayer = playerTransform.position - transform.position;
            float dist = toPlayer.magnitude;
            
            if (dist > playerFollowDistance)
            {
                // Move towards player
                Vector3 moveDir = toPlayer.normalized;
                
                // Avoid grouping with other bots
                moveDir = AvoidBotGrouping(moveDir);
                moveDir = AvoidObstacles(moveDir);
                
                _flatVelocity = Vector3.MoveTowards(_flatVelocity, moveDir * moveSpeed, acceleration * Time.fixedDeltaTime);
            }
            else
            {
                // Stay near player, strafe slightly
                Vector3 side = Vector3.Cross(Vector3.up, toPlayer.normalized);
                Vector3 moveDir = side * Mathf.Sin((Time.time * 0.8f) + _strafeSeed) * 0.3f;
                
                _flatVelocity = Vector3.MoveTowards(_flatVelocity, moveDir * moveSpeed, acceleration * Time.fixedDeltaTime);
            }
            
            ApplyHorizontalVelocity();
            
            if (_animator != null)
            {
                _animator.SetFloat(HashSpeed, _flatVelocity.magnitude);
            }
            
            // Look at player
            Vector3 lookAtPlayer = playerTransform.position - transform.position;
            lookAtPlayer.y = 0;
            if (lookAtPlayer.sqrMagnitude > 0.01f)
            {
                Quaternion rot = Quaternion.LookRotation(lookAtPlayer.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 8f * Time.fixedDeltaTime);
            }
        }

        private ArenaCombatant FindBestTarget()
        {
            ArenaCombatant bestTarget = null;
            float bestScore = float.MinValue;
            
            foreach (var c in ArenaCombatant.All)
            {
                if (c == null || !c.IsAlive || c == _combatant || c.teamId == _combatant.teamId) continue;
                
                float distance = Vector3.Distance(transform.position, c.transform.position);
                if (distance > detectDistance) continue;
                
                // Score calculation - TACTICAL PRIORITIZATION
                float score = 0f;
                
                // 1. Prioritize low health enemies (HIGH PRIORITY)
                if (prioritizeLowHealth)
                {
                    float healthPercent = c.hp / c.maxHp;
                    float lowHealthBonus = (1f - healthPercent) * 100f * lowHealthBonusMultiplier;
                    score += lowHealthBonus;
                    
                    // Extra bonus for very low health (finishing targets)
                    if (healthPercent < 0.25f) score += 50f;
                }
                
                // 2. Distance factor (closer is better, but not too close)
                float distanceFactor = 1f - Mathf.Clamp01(distance / detectDistance);
                score += distanceFactor * 25f;
                
                // 3. Prefer enemies the player is attacking
                if (playerTransform != null)
                {
                    float playerDist = Vector3.Distance(playerTransform.position, c.transform.position);
                    if (playerDist < 10f) score += 30f;
                }
                
                // 4. Deprioritize bosses unless they're the only target
                if (c.name.Contains("T-Rex") || c.CompareTag("Boss"))
                {
                    score -= 80f;
                }
                
                // 5. Prefer targets that are already being attacked by allies
                // (focus fire indirectly through target persistence)
                if (_currentTarget == c) score += 15f;
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = c;
                }
            }
            
            return bestTarget;
        }

        private void ExecuteCombat(ArenaCombatant target)
        {
            Vector3 toTarget = target.transform.position - transform.position;
            float dist = toTarget.magnitude;
            Vector3 moveDir = Vector3.zero;
            
            // Calculate optimal distance based on weapon
            float desiredDistance = preferredDistance;
            if (_weaponSystem != null && _weaponSystem.HasWeapon)
            {
                float weaponRange = _weaponSystem.CurrentRange;
                // Stay at 60% of weapon range for optimal damage
                desiredDistance = Mathf.Clamp(weaponRange * 0.6f, 4f, 14f);
            }

            // FLANKING BEHAVIOR
            if (enableFlanking && dist < detectDistance * 0.8f)
            {
                // Change flank angle periodically or when target changes
                if (Time.time - _lastFlankChangeTime > 3f || 
                    (_currentTarget != null && _currentTarget != target))
                {
                    _lastFlankChangeTime = Time.time;
                    if (Random.value < flankingChance)
                    {
                        // Random angle between -90 and 90 degrees for flanking
                        _targetFlankAngle = Random.Range(-70f, 70f);
                    }
                    else
                    {
                        _targetFlankAngle = 0f; // Direct approach
                    }
                }
                
                // Smoothly interpolate current flank angle
                _currentFlankAngle = Mathf.Lerp(_currentFlankAngle, _targetFlankAngle, Time.fixedDeltaTime * 2f);
                
                // Calculate flanking position
                Vector3 targetForward = target.transform.forward;
                Vector3 targetRight = target.transform.right;
                
                // Position: target position + rotated offset
                Quaternion rotation = Quaternion.Euler(0f, _currentFlankAngle, 0f);
                Vector3 flankOffset = rotation * (targetForward * -flankingDistance);
                Vector3 desiredPos = target.transform.position + flankOffset;
                
                // Move towards flanking position
                Vector3 toFlankPos = desiredPos - transform.position;
                
                // Distance control - approach or retreat
                if (dist > desiredDistance * 1.2f)
                {
                    // Too far - close in
                    moveDir = (toFlankPos.normalized + toTarget.normalized).normalized;
                }
                else if (dist < desiredDistance * 0.7f)
                {
                    // Too close - back up while flanking
                    moveDir = (toFlankPos.normalized - toTarget.normalized * 0.5f).normalized;
                }
                else
                {
                    // Good distance - pure flanking movement
                    moveDir = toFlankPos.normalized;
                }
            }
            else
            {
                // STANDARD DISTANCE CONTROL (no flanking)
                if (dist > desiredDistance) moveDir = toTarget.normalized;
                else if (dist < desiredDistance * 0.6f) moveDir = -toTarget.normalized;
            }

            // Add strafing for unpredictability
            Vector3 side = Vector3.Cross(Vector3.up, toTarget.normalized);
            moveDir += side * Mathf.Sin((Time.time * 1.2f) + _strafeSeed) * strafeStrength * 0.3f;
            
            // Avoid obstacles
            moveDir = AvoidObstacles(moveDir.normalized);

            _flatVelocity = Vector3.MoveTowards(_flatVelocity, moveDir * moveSpeed, acceleration * Time.fixedDeltaTime);
            ApplyHorizontalVelocity();

            if (_animator != null)
            {
                _animator.SetFloat(HashSpeed, _flatVelocity.magnitude);
            }

            // Look at target
            Vector3 look = Vector3.Scale(toTarget, new Vector3(1f, 0f, 1f));
            if (look.sqrMagnitude > 0.01f)
            {
                Quaternion rot = Quaternion.LookRotation(look.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 10f * Time.fixedDeltaTime);
            }

            // ATTACK
            bool canAttack = _weaponSystem != null && _weaponSystem.HasWeapon;
            if (canAttack)
            {
                _weaponSystem.AttackTarget(target);
                return;
            }

            // Fallback fireball attack
            if (dist <= detectDistance && Time.time >= _nextFire)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 1f;
                int hitCount = Physics.RaycastNonAlloc(rayOrigin, toTarget.normalized, _combatHitBuffer, dist);
                bool blocked = hitCount > 0 && _combatHitBuffer[0].collider.GetComponentInParent<ArenaCombatant>() != target;

                if (!blocked)
                {
                    Vector3 origin = transform.position + Vector3.up * 1.1f + transform.forward * 0.75f;
                    Vector3 dir = (target.transform.position + Vector3.up * 0.8f - origin).normalized;
                    RuntimeSpawner.SpawnFireball(_combatant, origin, dir, fireballSpeed);
                }
                _nextFire = Time.time + fireCooldown + Random.Range(-0.2f, 0.3f);
            }
        }

        private Vector3 AvoidBotGrouping(Vector3 desired)
        {
            Vector3 separation = Vector3.zero;
            int count = 0;
            
            foreach (var c in ArenaCombatant.All)
            {
                if (c == null || c == _combatant || c.teamId != _combatant.teamId) continue;
                
                float distance = Vector3.Distance(transform.position, c.transform.position);
                if (distance < groupSpreadDistance && distance > 0.1f)
                {
                    Vector3 away = (transform.position - c.transform.position).normalized;
                    separation += away / distance;
                    count++;
                }
            }
            
            if (count > 0)
            {
                separation /= count;
                desired = (desired + separation * 0.5f).normalized;
            }
            
            return desired;
        }

        private void ApplyHorizontalVelocity()
        {
            if (_rb == null) return;
            
            Vector3 vel = _rb.linearVelocity;
            vel.x = _flatVelocity.x;
            vel.z = _flatVelocity.z;
            _rb.linearVelocity = vel;
        }

        private Vector3 AvoidObstacles(Vector3 desired)
        {
            if (desired.sqrMagnitude < 0.001f) return desired;
            Vector3 origin = transform.position + Vector3.up * 0.7f;
            
            // Obstacle detection using NonAlloc
            int hitCount = Physics.RaycastNonAlloc(origin, desired, _obstacleHitBuffer, 1.8f);
            bool obstructed = hitCount > 0;
            
            // Edge detection: look ahead and down
            Vector3 groundCheckPos = transform.position + desired.normalized * 1.5f + Vector3.up * 0.5f;
            int edgeHitCount = Physics.RaycastNonAlloc(groundCheckPos, Vector3.down, _edgeHitBuffer, 2f);
            bool isEdge = edgeHitCount == 0;

            if (!obstructed && !isEdge) return desired;

            Vector3 right = Vector3.Cross(Vector3.up, desired).normalized;
            
            // Check alternatives
            bool altRightSafe = IsSafe(origin, right, 1.2f);
            bool altLeftSafe = IsSafe(origin, -right, 1.2f);

            if (altRightSafe) return (desired + right).normalized;
            if (altLeftSafe) return (desired - right).normalized;
            return -desired;
        }

        private bool IsSafe(Vector3 origin, Vector3 dir, float dist)
        {
            // Obstacle check using NonAlloc
            int hitCount = Physics.RaycastNonAlloc(origin, dir, _obstacleHitBuffer, dist);
            if (hitCount > 0) return false;
            
            // Edge check using NonAlloc
            Vector3 groundCheckPos = transform.position + dir.normalized * 1.5f + Vector3.up * 0.5f;
            int edgeHitCount = Physics.RaycastNonAlloc(groundCheckPos, Vector3.down, _edgeHitBuffer, 2f);
            if (edgeHitCount == 0) return false;
            
            return true;
        }
    }
}
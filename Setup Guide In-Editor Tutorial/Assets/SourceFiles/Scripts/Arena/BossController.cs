using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Controlador de IA para el Boss Mech
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ArenaCombatant))]
    public class BossController : MonoBehaviour
    {
        [Header("Stats")]
        public float moveSpeed = 9.6f; // Aumentado 20% (de 8f a 9.6f)
        public float acceleration = 25f;
        public float detectDistance = 100f; 
        public float attackRange = 3f;
        public float attackCooldown = 2f;
        public float minDamage = 10f;
        public float maxDamage = 20f;

        private ArenaCombatant _combatant;
        private Rigidbody _rb;
        private ArenaCombatant _currentTarget;
        private float _nextAttack;
        private float _nextSearchTime;
        private Vector3 _flatVelocity;
        private Animator _animator;

        // Animator hashes
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashAttackType = Animator.StringToHash("AttackType");
        private static readonly int HashAttackTrigger = Animator.StringToHash("AttackTrigger");

        private void Awake()
        {
            _combatant = GetComponent<ArenaCombatant>();
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponentInChildren<Animator>();
            
            // Configuración física
            _rb.useGravity = true;
            _rb.isKinematic = false;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void Start()
        {
            // Set stats for Boss
            _combatant.maxHp = 200f;
            _combatant.hp = 200f;
            _combatant.teamId = 99; // Unique team to attack everyone
            _combatant.displayName = "Mech Boss";
        }

        private void FixedUpdate()
        {
            if (_combatant == null || !_combatant.IsAlive)
            {
                _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
                return;
            }

            if (Time.time >= _nextSearchTime)
            {
                _currentTarget = NearestTarget();
                _nextSearchTime = Time.time + 0.5f;
            }

            if (_currentTarget == null)
            {
                _flatVelocity = Vector3.MoveTowards(_flatVelocity, Vector3.zero, acceleration * Time.fixedDeltaTime);
                ApplyMovement();
                UpdateAnimator(0f);
                return;
            }

            Vector3 toTarget = _currentTarget.transform.position - transform.position;
            float dist = toTarget.magnitude;
            Vector3 moveDir = Vector3.zero;

            float stopDist = attackRange * 0.85f;

            if (dist > stopDist)
            {
                moveDir = toTarget.normalized;
                
                // Raycast edge detection (visual/AI stop)
                Vector3 groundCheckPos = transform.position + moveDir * 2.5f + Vector3.up * 0.5f;
                if (!Physics.Raycast(groundCheckPos, Vector3.down, 3f))
                {
                    moveDir = Vector3.zero; // Stop at edges
                }
            }

            // Boundary constraints (hard cleanup)
            float mapLimit = 14.5f; // Based on Ground scale 30 (range -15 to 15)
            Vector3 pos = transform.position;
            if (Mathf.Abs(pos.x) > mapLimit || Mathf.Abs(pos.z) > mapLimit)
            {
                pos.x = Mathf.Clamp(pos.x, -mapLimit, mapLimit);
                pos.z = Mathf.Clamp(pos.z, -mapLimit, mapLimit);
                transform.position = pos;
                moveDir = Vector3.zero;
            }

            _flatVelocity = Vector3.MoveTowards(_flatVelocity, moveDir * moveSpeed, acceleration * Time.fixedDeltaTime);
            ApplyMovement();

            Vector3 lookDir = Vector3.Scale(toTarget, new Vector3(1, 0, 1));
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.fixedDeltaTime);
            }

            UpdateAnimator(_flatVelocity.magnitude);

            if (dist <= attackRange && Time.time >= _nextAttack)
            {
                AttackTarget();
            }
        }

        private void ApplyMovement()
        {
            Vector3 vel = _rb.linearVelocity;
            vel.x = _flatVelocity.x;
            vel.z = _flatVelocity.z;
            _rb.linearVelocity = vel;
        }

        private void UpdateAnimator(float speed)
        {
            if (_animator != null)
            {
                _animator.SetFloat(HashSpeed, speed);
            }
        }

        private void AttackTarget()
        {
            float damage = Random.Range(minDamage, maxDamage);
            _currentTarget.TakeDamage(damage, _combatant);
            _nextAttack = Time.time + attackCooldown;

            if (_animator != null)
            {
                _animator.SetFloat(HashAttackType, 0);
                _animator.SetFloat(HashAttackTrigger, 1.0f);
                Invoke(nameof(ResetMeleeTrigger), 0.1f);
            }
        }
        
        private void ResetMeleeTrigger()
        {
            if (_animator != null) _animator.SetFloat(HashAttackTrigger, 0.0f);
        }

        private ArenaCombatant NearestTarget()
        {
            ArenaCombatant nearest = null;
            float bestSq = float.MaxValue;
            var all = ArenaCombatant.All;
            
            for (int i = 0; i < all.Count; i++)
            {
                var c = all[i];
                if (c == null || !c.IsAlive || c == _combatant) 
                    continue; 
                
                float sq = (c.transform.position - transform.position).sqrMagnitude;
                if (sq < bestSq && sq <= detectDistance * detectDistance)
                {
                    bestSq = sq;
                    nearest = c;
                }
            }
            return nearest;
        }
    }
}

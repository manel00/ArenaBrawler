using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Controlador para perros invocados
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ArenaCombatant))]
    public class DogController : MonoBehaviour
    {
        public ArenaCombatant owner;
        public float moveSpeed = 9f;
        public float acceleration = 30f;
        public float detectDistance = 18f;
        public float attackRange = 1.8f;
        public float attackCooldown = 1.5f;
        public float lifeDuration = 20f;

        private ArenaCombatant _combatant;
        private Rigidbody _rb;
        private Vector3 _flatVelocity;
        private float _nextAttack;

        private void Awake()
        {
            _combatant = GetComponent<ArenaCombatant>();
            _rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            Destroy(gameObject, lifeDuration);
        }

        private void FixedUpdate()
        {
            if (_combatant == null || !_combatant.IsAlive) return;

            var target = NearestEnemy();
            if (target == null && owner != null && owner.IsAlive)
            {
                target = owner; // Follow player if no enemy
            }
            if (target == null)
            {
                _flatVelocity = Vector3.MoveTowards(_flatVelocity, Vector3.zero, acceleration * Time.fixedDeltaTime);
                ApplyHorizontalVelocity();
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            float dist = toTarget.magnitude;

            Vector3 desired = AvoidObstacles(toTarget.normalized);
            _flatVelocity = Vector3.MoveTowards(_flatVelocity, desired * moveSpeed, acceleration * Time.fixedDeltaTime);
            ApplyHorizontalVelocity();

            Vector3 look = Vector3.Scale(toTarget, new Vector3(1f, 0f, 1f));
            if (look.sqrMagnitude > 0.01f)
            {
                Quaternion rot = Quaternion.LookRotation(look.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 14f * Time.fixedDeltaTime);
            }

            if (dist <= attackRange && Time.time >= _nextAttack && target != owner)
            {
                target.TakeDamage(3f, _combatant);
                _nextAttack = Time.time + attackCooldown;
            }
        }

        private void ApplyHorizontalVelocity()
        {
            Vector3 vel = _rb.linearVelocity;
            vel.x = _flatVelocity.x;
            vel.z = _flatVelocity.z;
            _rb.linearVelocity = vel;
        }

        private ArenaCombatant NearestEnemy()
        {
            ArenaCombatant nearest = null;
            float best = float.MaxValue;
            foreach (var c in ArenaCombatant.All)
            {
                if (c == null || !c.IsAlive || c == _combatant || c.teamId == _combatant.teamId) continue;
                float sq = (c.transform.position - transform.position).sqrMagnitude;
                if (sq < best && sq <= detectDistance * detectDistance)
                {
                    best = sq;
                    nearest = c;
                }
            }
            return nearest;
        }

        private Vector3 AvoidObstacles(Vector3 desired)
        {
            if (desired.sqrMagnitude < 0.001f) return desired;
            Vector3 origin = transform.position + Vector3.up * 0.35f;
            if (!Physics.SphereCast(origin, 0.25f, desired, out _, 1.2f)) return desired;

            Vector3 right = Vector3.Cross(Vector3.up, desired).normalized;
            if (!Physics.SphereCast(origin, 0.2f, right, out _, 1f)) return (desired + right).normalized;
            if (!Physics.SphereCast(origin, 0.2f, -right, out _, 1f)) return (desired - right).normalized;
            return -desired;
        }
    }
}

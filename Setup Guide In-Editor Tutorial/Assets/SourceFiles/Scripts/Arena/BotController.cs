using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Controlador avanzado para bots en la arena (Fuego y Strafe)
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ArenaCombatant))]
    public class BotController : MonoBehaviour
    {
        public float moveSpeed = 6.5f;
        public float acceleration = 22f;
        public float detectDistance = 25f;
        public float preferredDistance = 8f;
        public float fireballSpeed = 20f;
        public float fireCooldown = 1.2f;
        public float strafeStrength = 0.35f;

        private Rigidbody _rb;
        private ArenaCombatant _combatant;
        private Vector3 _flatVelocity;
        private float _nextFire;
        private float _strafeSeed;
        private Animator _animator;

        private static readonly int HashSpeed = Animator.StringToHash("Speed");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _combatant = GetComponent<ArenaCombatant>();
            _animator = GetComponentInChildren<Animator>();
            _nextFire = Time.time + Random.Range(0.2f, 1f);
            _strafeSeed = Random.Range(0f, 10f);
        }

        private void FixedUpdate()
        {
            if (_combatant == null || !_combatant.IsAlive) return;

            var target = NearestEnemy();
            if (target == null)
            {
                _flatVelocity = Vector3.MoveTowards(_flatVelocity, Vector3.zero, acceleration * Time.fixedDeltaTime);
                ApplyHorizontalVelocity();
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            float dist = toTarget.magnitude;
            Vector3 moveDir = Vector3.zero;

            if (dist > preferredDistance) moveDir = toTarget.normalized;
            else if (dist < preferredDistance * 0.6f) moveDir = -toTarget.normalized;

            Vector3 side = Vector3.Cross(Vector3.up, toTarget.normalized);
            moveDir += side * Mathf.Sin((Time.time * 1.2f) + _strafeSeed) * strafeStrength;
            moveDir = AvoidObstacles(moveDir.normalized);

            _flatVelocity = Vector3.MoveTowards(_flatVelocity, moveDir * moveSpeed, acceleration * Time.fixedDeltaTime);
            ApplyHorizontalVelocity();

            if (_animator != null)
            {
                _animator.SetFloat(HashSpeed, _flatVelocity.magnitude);
            }

            Vector3 look = Vector3.Scale(toTarget, new Vector3(1f, 0f, 1f));
            if (look.sqrMagnitude > 0.01f)
            {
                Quaternion rot = Quaternion.LookRotation(look.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 10f * Time.fixedDeltaTime);
            }

            if (dist <= detectDistance && Time.time >= _nextFire)
            {
                bool blocked = Physics.Raycast(transform.position + Vector3.up * 1f, toTarget.normalized, out RaycastHit hit, dist) &&
                               hit.collider.GetComponentInParent<ArenaCombatant>() != target;

                if (!blocked)
                {
                    Vector3 origin = transform.position + Vector3.up * 1.1f + transform.forward * 0.75f;
                    Vector3 dir = (target.transform.position + Vector3.up * 0.8f - origin).normalized;
                    RuntimeSpawner.SpawnFireball(_combatant, origin, dir, fireballSpeed);
                }
                _nextFire = Time.time + fireCooldown + Random.Range(-0.2f, 0.3f);
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
            Vector3 origin = transform.position + Vector3.up * 0.7f;
            
            // Obstacle detection
            bool obstructed = Physics.Raycast(origin, desired, 1.8f);
            
            // Edge detection: look ahead and down
            Vector3 groundCheckPos = transform.position + desired.normalized * 1.5f + Vector3.up * 0.5f;
            bool isEdge = !Physics.Raycast(groundCheckPos, Vector3.down, 2f);

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
            // Obstacle check
            if (Physics.Raycast(origin, dir, dist)) return false;
            
            // Edge check
            Vector3 groundCheckPos = transform.position + dir.normalized * 1.5f + Vector3.up * 0.5f;
            if (!Physics.Raycast(groundCheckPos, Vector3.down, 2f)) return false;
            
            return true;
        }
    }
}
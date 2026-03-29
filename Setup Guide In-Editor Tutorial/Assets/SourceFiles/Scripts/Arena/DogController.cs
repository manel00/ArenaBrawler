using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Controlador físico para robots esclavos (Domestic Robot)
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ArenaCombatant))]
    public class DogController : MonoBehaviour
    {
        public ArenaCombatant owner;
        public float moveSpeed = 6.5f;
        public float acceleration = 35f;
        public float detectDistance = 20f; 
        public float attackRange = 1.3f;
        public float attackCooldown = 1.5f;
        public float lifeDuration = 60f;

        private ArenaCombatant _combatant;
        private Rigidbody _rb;
        private ArenaCombatant _currentTarget;
        private float _nextAttack;
        private float _nextSearchTime;
        private Vector3 _flatVelocity;

        private void Awake()
        {
            _combatant = GetComponent<ArenaCombatant>();
            _rb = GetComponent<Rigidbody>();
            
            // Configuración física básica
            _rb.useGravity = true;
            _rb.isKinematic = false;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void Start()
        {
            Destroy(gameObject, lifeDuration);
        }

        private void FixedUpdate()
        {
            if (_combatant == null || !_combatant.IsAlive)
            {
                _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
                return;
            }

            // Búsqueda de objetivo (Enemigo o Dueño)
            if (Time.time >= _nextSearchTime)
            {
                _currentTarget = NearestEnemy();
                if (_currentTarget == null && owner != null && owner.IsAlive)
                {
                    _currentTarget = owner;
                }
                _nextSearchTime = Time.time + 0.2f;
            }

            if (_currentTarget == null)
            {
                _flatVelocity = Vector3.MoveTowards(_flatVelocity, Vector3.zero, acceleration * Time.fixedDeltaTime);
                ApplyMovement();
                return;
            }

            Vector3 toTarget = _currentTarget.transform.position - transform.position;
            float dist = toTarget.magnitude;
            Vector3 moveDir = Vector3.zero;

            // Lógica de seguimiento: si es enemigo, se lanza. Si es dueño, se acerca y para.
            float stopDist = (_currentTarget == owner) ? 2.5f : attackRange * 0.85f;

            if (dist > stopDist)
            {
                moveDir = toTarget.normalized;

                // Edge detection
                Vector3 groundCheckPos = transform.position + moveDir * 1.2f + Vector3.up * 0.5f;
                if (!Physics.Raycast(groundCheckPos, Vector3.down, 2.5f))
                {
                    moveDir = Vector3.zero; // Stop at edges
                }
            }

            // Aplicar velocidad horizontal
            _flatVelocity = Vector3.MoveTowards(_flatVelocity, moveDir * moveSpeed, acceleration * Time.fixedDeltaTime);
            ApplyMovement();

            // Rotación hacia el objetivo
            Vector3 lookDir = Vector3.Scale(toTarget, new Vector3(1, 0, 1));
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.fixedDeltaTime);
            }

            // Ataque (solo si el objetivo no es el dueño)
            if (_currentTarget != owner && dist <= attackRange && Time.time >= _nextAttack)
            {
                float damage = Random.Range(20f, 30.1f);
                _currentTarget.TakeDamage(damage, _combatant);
                _nextAttack = Time.time + attackCooldown;

                var anim = GetComponentInChildren<Animator>();
                if (anim != null) anim.SetTrigger("Attack");
                
                Debug.Log($"[Robot] Atacando a {_currentTarget.name} con {damage:F1} de daño");
            }
        }

        private void ApplyMovement()
        {
            Vector3 vel = _rb.linearVelocity;
            vel.x = _flatVelocity.x;
            vel.z = _flatVelocity.z;
            _rb.linearVelocity = vel;
        }

        private ArenaCombatant NearestEnemy()
        {
            ArenaCombatant nearest = null;
            float bestSq = float.MaxValue;
            var all = ArenaCombatant.All;
            
            for (int i = 0; i < all.Count; i++)
            {
                var c = all[i];
                if (c == null || !c.IsAlive || c == _combatant || (owner != null && c == owner) || c.teamId == _combatant.teamId) 
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

using System.Collections.Generic;
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
        // Límite de perros por owner
        private static readonly Dictionary<ArenaCombatant, HashSet<DogController>> _dogsByOwner = new Dictionary<ArenaCombatant, HashSet<DogController>>();
        private const int MAX_DOGS_PER_OWNER = 5;
        
        public ArenaCombatant owner;
        public float moveSpeed = 6.5f;
        public float acceleration = 35f;
        public float detectDistance = 20f; 
        public float attackRange = 1.3f;
        public float attackCooldown = 1.5f;
        public float lifeDuration = 5f;

        private GameBalanceConfig _balanceConfig;

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
            
            // Load balance config
            _balanceConfig = Resources.Load<GameBalanceConfig>("Configs/GameBalanceConfig");
            if (_balanceConfig != null)
            {
                _combatant.maxHp = _balanceConfig.dogMaxHealth;
                _combatant.hp = _balanceConfig.dogMaxHealth;
                lifeDuration = _balanceConfig.dogDuration;
                Debug.Log("[DogController] Loaded balance config values");
            }
            
            // Configuración física básica
            _rb.useGravity = true;
            _rb.isKinematic = false;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            
            // Registrar este perro en el tracking
            RegisterDog();
        }
        
        private void OnDestroy()
        {
            // Desregistrar cuando se destruya
            UnregisterDog();
        }
        
        /// <summary>
        /// Registra este perro en el tracking del owner
        /// </summary>
        public void RegisterDog()
        {
            if (owner == null) return;
            
            if (!_dogsByOwner.TryGetValue(owner, out var dogSet))
            {
                dogSet = new HashSet<DogController>();
                _dogsByOwner[owner] = dogSet;
            }
            dogSet.Add(this);
        }
        
        /// <summary>
        /// Desregistra este perro del tracking
        /// </summary>
        private void UnregisterDog()
        {
            if (owner == null) return;
            
            if (_dogsByOwner.TryGetValue(owner, out var dogSet))
            {
                dogSet.Remove(this);
                if (dogSet.Count == 0)
                {
                    _dogsByOwner.Remove(owner);
                }
            }
        }
        
        /// <summary>
        /// Verifica si el owner puede spawnear más perros
        /// </summary>
        public static bool CanSpawnDog(ArenaCombatant owner)
        {
            if (owner == null) return true;
            
            if (!_dogsByOwner.TryGetValue(owner, out var dogSet))
            {
                return true; // No tiene perros aún
            }
            
            // Limpiar nulls
            dogSet.RemoveWhere(d => d == null);
            
            return dogSet.Count < MAX_DOGS_PER_OWNER;
        }
        
        /// <summary>
        /// Obtiene el número de perros activos de un owner
        /// </summary>
        public static int GetDogCount(ArenaCombatant owner)
        {
            if (owner == null) return 0;
            
            if (!_dogsByOwner.TryGetValue(owner, out var dogSet))
            {
                return 0;
            }
            
            dogSet.RemoveWhere(d => d == null);
            return dogSet.Count;
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

            // Búsqueda de objetivo usando EnemyFinder
            if (Time.time >= _nextSearchTime)
            {
                _currentTarget = EnemyFinder.FindNearest(transform.position, _combatant, detectDistance, _combatant?.teamId);
                if (_currentTarget == null && owner != null && owner.IsAlive)
                {
                    _currentTarget = owner;
                }
                _nextSearchTime = Time.time + 0.2f;
            }

            if (_currentTarget == null)
            {
                _flatVelocity = Vector3.MoveTowards(_flatVelocity, Vector3.zero, acceleration * Time.fixedDeltaTime);
                _rb.ApplyHorizontalVelocity(_flatVelocity);
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

                // Edge detection usando EnemyFinder
                if (!EnemyFinder.CheckEdgeSafe(transform.position, moveDir, 1.2f, 2.5f))
                {
                    moveDir = Vector3.zero;
                }
            }

            // Aplicar velocidad horizontal
            _flatVelocity = Vector3.MoveTowards(_flatVelocity, moveDir * moveSpeed, acceleration * Time.fixedDeltaTime);
            _rb.ApplyHorizontalVelocity(_flatVelocity);

            // Rotación hacia el objetivo usando EnemyFinder
            EnemyFinder.RotateTowards(transform, _currentTarget.transform.position, 8f);

            // Ataque (solo si el objetivo no es el dueño)
            if (_currentTarget != owner && dist <= attackRange && Time.time >= _nextAttack)
            {
                float damage = _balanceConfig != null ? Random.Range(_balanceConfig.dogMinDamage, _balanceConfig.dogMaxDamage) : Random.Range(15f, 25f);
                _currentTarget.TakeDamage(damage, _combatant);
                _nextAttack = Time.time + attackCooldown;

                var anim = GetComponentInChildren<Animator>();
                if (anim != null) anim.SetTrigger("Attack");
            }
        }

        // Método ApplyMovement eliminado - usar _rb.ApplyHorizontalVelocity(_flatVelocity)
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace ArenaEnhanced
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] public float moveSpeed = 25f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] public float jumpForce = 12.5f;
        [SerializeField] private float gravityMultiplier = 12.5f;
        
        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayer;
        
        [Header("Dash")]
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDistance = 2.5f;
        [SerializeField] private float dashCooldown = 0.5f;
        [SerializeField] private float dashIFramesDuration = 0.2f;
        [SerializeField] private float dashStaminaCost = 25f;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaRegenRate = 20f;
        
        [Header("Kill Plane - Caída")]
        [SerializeField] private float arenaSurfaceY = 0f;
        [SerializeField] private float fallDeathThreshold = 2f;
        
        private Rigidbody _rb;
        private Animator _animator;
        private ArenaCombatant _combatant;
        private PlayerWeaponSystem _weaponSystem;
        private Collider _col;
        
        private bool _isGrounded;
        private bool _wasGrounded;
        private Vector3 _moveInput;
        private float _nextActionTime;
        
        private float _currentStamina;
        private float _lastDashTime = -999f;
        private bool _canDash = true;
        private Coroutine _dashCoroutine;
        private Coroutine _iFramesCoroutine;
        
        // OPTIMIZACIÓN: Buffer aumentado para SphereCastNonAlloc (más preciso que Raycast)
        private readonly RaycastHit[] _groundHitBuffer = new RaycastHit[3];
        
        // Parámetros de SphereCast para mejor detección de suelo
        private const float GroundCheckRadius = 0.3f;
        private const float GroundCheckExtraDistance = 0.15f;
        
        private static readonly int AnimIDSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIDGrounded = Animator.StringToHash("Grounded");
        private static readonly int AnimIDJump = Animator.StringToHash("Jump");
        private static readonly int AnimIDFreeFall = Animator.StringToHash("FreeFall");
        private static readonly int AnimIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        private static readonly int AnimIDDash = Animator.StringToHash("Dash");
        
        // Nuevos parámetros para animaciones One Hand Up
        private static readonly int AnimIDIsWeaponDrawn = Animator.StringToHash("IsWeaponDrawn");
        private static readonly int AnimIDAttack = Animator.StringToHash("Attack");
        private static readonly int AnimIDAttackType = Animator.StringToHash("AttackType");
        
        public float GetStaminaPercentage() => _currentStamina / maxStamina;
        public float GetDashCooldownPercentage() => _canDash ? 0f : Mathf.Clamp01((Time.time - _lastDashTime) / dashCooldown);
        public int GetCurrentStamina() => Mathf.RoundToInt(_currentStamina);
        public bool CanDash() => _canDash && _currentStamina >= dashStaminaCost;
        public bool IsGrounded => _isGrounded;
        
        private void Awake()
        {
            // Inicializar capas de física
            PhysicsLayers.Initialize();
            
            // Si no se asignó groundLayer, usar el cacheado de PhysicsLayers
            if (groundLayer == 0)
                groundLayer = PhysicsLayers.GroundMask | PhysicsLayers.EnvironmentMask;
            
            _rb = GetComponent<Rigidbody>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
                Debug.Log("[PlayerController] Added missing Rigidbody");
            }
            _rb.freezeRotation = true;
            
            _col = GetComponent<Collider>();
            if (_col == null)
            {
                _col = gameObject.AddComponent<CapsuleCollider>();
                Debug.Log("[PlayerController] Added missing CapsuleCollider");
            }
            
            _combatant = GetComponent<ArenaCombatant>();
            if (_combatant == null)
            {
                _combatant = gameObject.AddComponent<ArenaCombatant>();
                _combatant.teamId = 1;
                _combatant.displayName = "Player";
            }
            
            _weaponSystem = GetComponent<PlayerWeaponSystem>();
            if (_weaponSystem == null)
            {
                _weaponSystem = gameObject.AddComponent<PlayerWeaponSystem>();
            }
            
            // Buscar el Animator en el modelo hijo
            _animator = GetComponentInChildren<Animator>(true);
            if (_animator == null)
            {
                Debug.LogWarning("[PlayerController] No Animator found in children!");
            }
            else
            {
                Debug.Log("[PlayerController] Animator found: " + _animator.gameObject.name);
            }
            
            // Asegurar que el Animator esté habilitado
            if (_animator != null && !_animator.enabled)
            {
                _animator.enabled = true;
                Debug.Log("[PlayerController] Animator enabled");
            }
            
            if (GetComponent<KatanaWeapon>() == null)
                gameObject.AddComponent<KatanaWeapon>();

            if (GetComponent<GrenadeSystem>() == null)
                gameObject.AddComponent<GrenadeSystem>();
                
            if (!CompareTag("Player"))
            {
                tag = "Player";
            }
        }
        
        private void OnEnable()
        {
            InputManager.OnJumpPressed += Jump;
            InputManager.OnDashPressed += OnDashPressed;
            InputManager.OnDropWeaponPressed += DropCurrentWeapon;
            InputManager.OnPickUpWeaponPressed += TryPickUpNearbyWeapon;
            InputManager.OnAbilityPressed += OnAbilityPressed;
            InputManager.OnWeaponAttackPressed += OnWeaponAttackPressed;
        }

        private void OnDisable()
        {
            InputManager.OnJumpPressed -= Jump;
            InputManager.OnDashPressed -= OnDashPressed;
            InputManager.OnDropWeaponPressed -= DropCurrentWeapon;
            InputManager.OnPickUpWeaponPressed -= TryPickUpNearbyWeapon;
            InputManager.OnAbilityPressed -= OnAbilityPressed;
            InputManager.OnWeaponAttackPressed -= OnWeaponAttackPressed;
        }

        private void OnDashPressed()
        {
            if (_canDash && _currentStamina >= dashStaminaCost)
                PerformDash();
        }

        private void OnAbilityPressed(int abilityIndex)
        {
            TryCastAbility(abilityIndex);
        }

        private void OnWeaponAttackPressed(int weaponIndex)
        {
            TryCastAbility(4);
        }
        
        private void Start()
        {
            _currentStamina = maxStamina;
        }
        
        private void Update()
        {
            if (_combatant == null || !_combatant.IsAlive) return;

            RegenerateStamina();
            HandleInput();
            CheckGround();
            CheckFallDeath();
            UpdateAnimator();
        }
        
        private void FixedUpdate()
        {
            if (_combatant == null || !_combatant.IsAlive) return;

            Move();
            ApplyExtraGravity();
        }
        
        private void OnDestroy()
        {
            if (_dashCoroutine != null)
                StopCoroutine(_dashCoroutine);
            if (_iFramesCoroutine != null)
                StopCoroutine(_iFramesCoroutine);
        }

        private void RegenerateStamina()
        {
            if (_currentStamina < maxStamina)
            {
                _currentStamina = Mathf.Min(maxStamina, _currentStamina + staminaRegenRate * Time.deltaTime);
            }
        }

        private void ApplyExtraGravity()
        {
            if (_rb.linearVelocity.y < 0)
            {
                _rb.AddForce(Vector3.down * gravityMultiplier, ForceMode.Acceleration);
            }
        }
        
        private void HandleInput()
        {
            float horizontal = 0f;
            float vertical = 0f;
            
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal = -1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal = 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical = 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical = -1f;
            }
#else
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
#endif
            
            _moveInput = new Vector3(horizontal, 0f, vertical).normalized;
        }
        
        private void PerformDash()
        {
            _canDash = false;
            _currentStamina -= dashStaminaCost;
            _lastDashTime = Time.time;
            
            if (_animator != null)
                _animator.SetTrigger(AnimIDDash);
            
            _dashCoroutine = StartCoroutine(DashRoutine());
        }

        private IEnumerator DashRoutine()
        {
            float dashTime = dashDistance / dashSpeed;
            Vector3 dashDirection = transform.forward;
            
            _iFramesCoroutine = StartCoroutine(DashIFrames());
            
            while (dashTime > 0)
            {
                _rb.linearVelocity = new Vector3(dashDirection.x * dashSpeed, _rb.linearVelocity.y, dashDirection.z * dashSpeed);
                dashTime -= Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(dashCooldown);
            _canDash = true;
            _dashCoroutine = null;
        }

        private IEnumerator DashIFrames()
        {
            int originalLayer = gameObject.layer;
            // OPTIMIZACIÓN: Usar PhysicsLayers cacheado
            gameObject.layer = PhysicsLayers.Invincible;
            
            yield return new WaitForSeconds(dashIFramesDuration);
            
            gameObject.layer = originalLayer;
            _iFramesCoroutine = null;
        }

        private void TryCastAbility(int index)
        {
            if (_combatant == null || !_combatant.IsAlive) 
                return;

            bool success = false;
            switch (index)
            {
                case 1: 
                    success = CastFireball(); 
                    break;
                case 2: 
                    success = SummonDog(); 
                    break;
                case 3: 
                    // Katana attack handled by KatanaWeapon component via InputManager
                    // No fallback melee - katana must be equipped via KatanaWeapon
                    break;
                case 4: 
                    success = PerformWeaponAttack(); 
                    break;
                case 6:
                    success = ThrowGrenade();
                    break;
            }
        }

        private bool CastFireball()
        {
            // Dirección exactamente hacia adelante del personaje
            Vector3 direction = transform.forward;
            direction.y = 0;
            direction.Normalize();
            
            // Spawn 1.5m ENFRENTE del personaje, a la altura del pecho
            Vector3 spawnPos = transform.position + direction * 1.5f + Vector3.up * 1.1f;
            
            Debug.Log($"[CastFireball] Player at: {transform.position}, Spawn 1.5m FRONT at: {spawnPos}, Direction: {direction}");
            
            RuntimeSpawner.SpawnFireball(_combatant, spawnPos, direction, 20f);
            
            return true;
        }

        private bool SummonDog()
        {
            // Verificar límite de perros
            if (!DogController.CanSpawnDog(_combatant))
            {
#if DEBUG
                Debug.Log($"[PlayerController] Límite de perros alcanzado ({DogController.GetDogCount(_combatant)}/5)");
#endif
                // Mostrar feedback visual opcional aquí
                return false;
            }
            
            Vector3 spawnPos = transform.position + transform.forward * 2f;
            RuntimeSpawner.SpawnDog(_combatant, spawnPos);
            return true;
        }

        private bool PerformWeaponAttack()
        {
            if (_weaponSystem == null || !_weaponSystem.HasWeapon) return false;
            return _weaponSystem.Attack();
        }

        private bool ThrowGrenade()
        {
            var grenadeSystem = GetComponent<GrenadeSystem>();
            if (grenadeSystem == null) return false;
            
            // El GrenadeSystem maneja su propio input de carga/lanzamiento
            // Esta función es llamada por el InputManager para iniciar la carga
            // La lógica real está en GrenadeSystem.Update()
            return true;
        }

        private void DropCurrentWeapon()
        {
            if (_weaponSystem == null || !_weaponSystem.HasWeapon) return;
            _weaponSystem.DropCurrentWeapon();
        }

        private void TryPickUpNearbyWeapon()
        {
            if (_weaponSystem == null || _weaponSystem.HasWeapon) return;
            if (_weaponSystem.NearbyWeapon != null)
                _weaponSystem.TryPickUpWeapon(_weaponSystem.NearbyWeapon);
        }
        
        private void Move()
        {
            if (Mathf.Abs(_moveInput.x) > 0.01f)
            {
                float turn = _moveInput.x * rotationSpeed * Time.fixedDeltaTime * 20f;
                Quaternion deltaRotation = Quaternion.Euler(0, turn, 0);
                _rb.MoveRotation(_rb.rotation * deltaRotation);
            }

            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();
            
            float targetSpeed = _moveInput.z * moveSpeed;
            Vector3 targetVelocity = forward * targetSpeed;
            
            _rb.linearVelocity = new Vector3(targetVelocity.x, _rb.linearVelocity.y, targetVelocity.z);
        }

        private void UpdateAnimator()
        {
            if (_animator == null) 
            {
                // Intentar encontrar el animator de nuevo
                _animator = GetComponentInChildren<Animator>(true);
                if (_animator == null) return;
            }

            float currentHorizontalSpeed = new Vector3(_rb.linearVelocity.x, 0.0f, _rb.linearVelocity.z).magnitude;
            
            _animator.SetFloat(AnimIDSpeed, currentHorizontalSpeed);
            _animator.SetFloat(AnimIDMotionSpeed, 1f);
            _animator.SetBool(AnimIDGrounded, _isGrounded);
            
            // Asegurar que IsWeaponDrawn esté activo para las animaciones de espada
            _animator.SetBool(AnimIDIsWeaponDrawn, true);
            
            if (_isGrounded)
            {
                _animator.SetBool(AnimIDJump, false);
                _animator.SetBool(AnimIDFreeFall, false);
            }
            else if (_rb.linearVelocity.y < -0.1f)
            {
                _animator.SetBool(AnimIDFreeFall, true);
            }
        }
        
        private void CheckGround()
        {
            if (_col == null) 
            {
                _isGrounded = false;
                return;
            }
            
            // Raycast desde los pies del personaje hacia abajo
            Vector3 rayOrigin = _col.bounds.center - Vector3.up * (_col.bounds.extents.y - 0.05f);
            float rayLength = 0.35f;
            
            RaycastHit hit;
            _isGrounded = Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength, groundLayer, QueryTriggerInteraction.Ignore);
            
            // DEBUG - solo cuando cambia estado para evitar spam
            if (_isGrounded != _wasGrounded)
            {
                Debug.DrawRay(rayOrigin, Vector3.down * rayLength, _isGrounded ? Color.green : Color.red, 0.5f);
                if (_isGrounded)
                    Debug.Log($"[CheckGround] Grounded! Hit: {hit.collider.name}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }
            _wasGrounded = _isGrounded;
        }
        
        private void CheckFallDeath()
        {
            if (transform.position.y < arenaSurfaceY - fallDeathThreshold)
            {
                if (_combatant != null && _combatant.IsAlive)
                {
                    _combatant.TakeDamage(_combatant.CurrentHealth, (GameObject)null);
                }
            }
        }

        private void ResetMeleeTrigger()
        {
            if (_animator != null) _animator.SetFloat("AttackTrigger", 0.0f);
        }

        public void Jump()
        {
            if (_isGrounded)
            {
                float characterHeight = 2f;
                if (_col != null) characterHeight = _col.bounds.size.y;
                
                float targetHeight = characterHeight * 3.5f;
                float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * targetHeight);
                
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpVelocity, _rb.linearVelocity.z);
                _isGrounded = false; 
                
                if (_animator != null)
                    _animator.SetBool(AnimIDJump, true);
            }
        }
    }
}
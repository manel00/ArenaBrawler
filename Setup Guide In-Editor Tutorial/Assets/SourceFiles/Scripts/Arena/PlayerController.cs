using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace ArenaEnhanced
{
    /// <summary>
    /// Controlador del jugador para la arena
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimiento")]
        [Tooltip("Velocidad de movimiento del jugador")]
        [SerializeField] public float moveSpeed = 12.5f;
        
        [Tooltip("Velocidad de rotación")]
        [SerializeField] private float rotationSpeed = 10f;
        
        [Tooltip("Fuerza del salto")]
        [SerializeField] public float jumpForce = 12.5f;
        
        [Tooltip("Multiplicador de gravedad")]
        [SerializeField] private float gravityMultiplier = 12.5f;
        
        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private float groundCheckDistance = 0.2f;
        
        [Header("Tab-Target & GCD")]
        [SerializeField] private float globalCooldown = 1.0f;
        
        [Header("Dash")]
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDistance = 2.5f;
        [SerializeField] private float dashCooldown = 0.5f;
        [SerializeField] private float dashIFramesDuration = 0.2f;
        [SerializeField] private float dashStaminaCost = 25f;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaRegenRate = 20f;
        
        private Rigidbody _rb;
        private Animator _animator;
        private ArenaCombatant _combatant;
        private PlayerWeaponSystem _weaponSystem;
        private Collider _col;
        
        private bool _isGrounded;
        private Vector3 _moveInput;
        private float _nextActionTime;
        
        // Dash system
        private float _currentStamina;
        private float _lastDashTime = -999f;
        private bool _canDash = true;
        private Coroutine _dashCoroutine;
        private Coroutine _iFramesCoroutine;
        
        // Raycast buffer for ground check
        private readonly RaycastHit[] _groundHitBuffer = new RaycastHit[1];
        
        // Animator parameter hashes for performance
        private static readonly int AnimIDSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIDGrounded = Animator.StringToHash("Grounded");
        private static readonly int AnimIDJump = Animator.StringToHash("Jump");
        private static readonly int AnimIDFreeFall = Animator.StringToHash("FreeFall");
        private static readonly int AnimIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        private static readonly int AnimIDDash = Animator.StringToHash("Dash");
        
        // Melee sequence tracking
        private int _nextPunchSide = 0; // 0: Left, 1: Right
        private int _nextKickSide = 2;  // 2: Left, 3: Right
        
        // Public accessors for HUD (read-only)
        public float GetStaminaPercentage() => _currentStamina / maxStamina;
        public float GetDashCooldownPercentage() => _canDash ? 0f : Mathf.Clamp01((Time.time - _lastDashTime) / dashCooldown);
        public int GetCurrentStamina() => Mathf.RoundToInt(_currentStamina);
        public bool CanDash() => _canDash && _currentStamina >= dashStaminaCost;
        public bool IsGrounded => _isGrounded;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
            _col = GetComponent<Collider>();
            _combatant = GetComponent<ArenaCombatant>();
            _weaponSystem = GetComponent<PlayerWeaponSystem>();
            _animator = GetComponentInChildren<Animator>();
            
            // Asegurar que el jugador siempre tenga el KatanaWeapon
            if (GetComponent<KatanaWeapon>() == null)
                gameObject.AddComponent<KatanaWeapon>();
        }
        
        private void OnEnable()
        {
            // Subscribe to InputManager events
            InputManager.OnJumpPressed += Jump;
            InputManager.OnDashPressed += OnDashPressed;
            InputManager.OnDropWeaponPressed += DropCurrentWeapon;
            InputManager.OnPickUpWeaponPressed += TryPickUpNearbyWeapon;
            InputManager.OnAbilityPressed += OnAbilityPressed;
            InputManager.OnWeaponAttackPressed += OnWeaponAttackPressed;
        }

        private void OnDisable()
        {
            // Unsubscribe from InputManager events
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
            if (abilityIndex == 5) return; // 5 es para katana, manejado por KatanaWeapon
            TryCastAbility(abilityIndex);
        }

        private void OnWeaponAttackPressed(int weaponIndex)
        {
            TryCastAbility(4);
        }
        
        private void Start()
        {
            Debug.Log($"[PlayerController] Started for {gameObject.name}. Animator found: {_animator != null}");
            _currentStamina = maxStamina;
        }
        
        private void Update()
        {
            if (_combatant == null || !_combatant.IsAlive) return;

            RegenerateStamina();
            HandleInput();
            CheckGround();
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
            // Clean up coroutines
            if (_dashCoroutine != null)
            {
                StopCoroutine(_dashCoroutine);
            }
            if (_iFramesCoroutine != null)
            {
                StopCoroutine(_iFramesCoroutine);
            }
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
        
        /// <summary>
        /// Procesa la entrada del jugador (movimiento + habilidades como fallback)
        /// </summary>
        private void HandleInput()
        {
            float horizontal = 0f;
            float vertical = 0f;
            
#if ENABLE_INPUT_SYSTEM
            // Nuevo Input System - solo movimiento
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal = 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical = 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical = -1f;
            }
#else
            // Input System antiguo
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
#endif
            
            _moveInput = new Vector3(horizontal, 0f, vertical).normalized;
            
            // Fallback para habilidades si InputManager no funciona
            HandleAbilityInputFallback();
        }
        
        /// <summary>
        /// Maneja input de habilidades directamente como fallback
        /// </summary>
        private void HandleAbilityInputFallback()
        {
            // Habilidades 1-4 (excluyendo 5 que es katana)
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryCastAbility(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryCastAbility(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryCastAbility(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TryCastAbility(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) TryCastAbility(6);
            if (Input.GetKeyDown(KeyCode.Alpha7)) TryCastAbility(7);
            if (Input.GetKeyDown(KeyCode.Alpha8)) TryCastAbility(8);
            if (Input.GetKeyDown(KeyCode.Alpha9)) TryCastAbility(9);
            
            // Salto con Space (fallback)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
            
            // Dash con F (fallback)
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (_canDash && _currentStamina >= dashStaminaCost)
                    PerformDash();
            }
        }

        private void PerformDash()
        {
            _canDash = false;
            _currentStamina -= dashStaminaCost;
            _lastDashTime = Time.time;
            
            if (_animator != null)
            {
                _animator.SetTrigger(AnimIDDash);
            }
            
            _dashCoroutine = StartCoroutine(DashRoutine());
        }

        private IEnumerator DashRoutine()
        {
            float dashTime = dashDistance / dashSpeed;
            Vector3 dashDirection = transform.forward;
            
            // I-frames
            _iFramesCoroutine = StartCoroutine(DashIFrames());
            
            while (dashTime > 0)
            {
                _rb.linearVelocity = new Vector3(dashDirection.x * dashSpeed, _rb.linearVelocity.y, dashDirection.z * dashSpeed);
                dashTime -= Time.deltaTime;
                yield return null;
            }
            
            
            // Cooldown
            yield return new WaitForSeconds(dashCooldown);
            _canDash = true;
            _dashCoroutine = null;
        }

        private IEnumerator DashIFrames()
        {
            // Disable collision with enemies during i-frames
            int originalLayer = gameObject.layer;
            gameObject.layer = LayerMask.NameToLayer("Invincible");
            
            yield return new WaitForSeconds(dashIFramesDuration);
            
            gameObject.layer = originalLayer;
            _iFramesCoroutine = null;
        }

        private void TryCastAbility(int index)
        {
            Debug.Log($"[PlayerController] TryCastAbility called with index: {index}");
            
            // Cooldowns desactivados
            // if (index != 5 && Time.time < _nextActionTime) 
            // {
            //     Debug.Log("[PlayerController] Ability blocked by cooldown");
            //     return;
            // }
            
            if (_combatant == null || !_combatant.IsAlive) 
            {
                Debug.Log("[PlayerController] Combatant null or dead");
                return;
            }

            bool success = false;
            switch (index)
            {
                case 1: 
                    Debug.Log("[PlayerController] Casting Fireball...");
                    success = CastFireball(); 
                    break;
                case 2: 
                    Debug.Log("[PlayerController] Summoning Dog...");
                    success = SummonDog(); 
                    break;
                case 4: success = PerformWeaponAttack(); break;
                case 5: success = PerformMelee(); break;
            }

            Debug.Log($"[PlayerController] Ability {index} execution result: {success}");
            
            // Cooldowns desactivados
            // if (success)
            // {
            //     if (index != 4)
            //     {
            //         _nextActionTime = Time.time + globalCooldown;
            //     }
            // }
        }

        private bool CastFireball()
        {
            Vector3 direction = transform.forward;
            direction.y = 0;
            direction.Normalize();

            RuntimeSpawner.SpawnFireball(_combatant, transform.position + transform.forward * 1.5f + Vector3.up, direction, 35f);
            return true;
        }

        private bool SummonDog()
        {
            RuntimeSpawner.SpawnDog(_combatant, transform.position + transform.forward * 2f);
            return true;
        }

        private bool PerformMelee()
        {
            // Si la katana está equipada, no ejecutar melee (la katana tiene su propio sistema)
            var katana = GetComponent<KatanaWeapon>();
            if (katana != null && katana.IsEquipped) return false;
            
            // Randomly choose between Punch and Kick category, but alternate sides within each category
            int attackType;
            if (Random.value < 0.5f)
            {
                attackType = _nextPunchSide;
                _nextPunchSide = 1 - _nextPunchSide; // Alternates 0 and 1
            }
            else
            {
                attackType = _nextKickSide;
                _nextKickSide = 5 - _nextKickSide; // Alternates 2 and 3
            }

            RuntimeSpawner.SpawnMelee(_combatant, transform.position, transform.forward);
            return true;
        }

        private bool PerformWeaponAttack()
        {
            if (_weaponSystem == null || !_weaponSystem.HasWeapon) return false;
            return _weaponSystem.Attack();
        }

        private void DropCurrentWeapon()
        {
            if (_weaponSystem == null || !_weaponSystem.HasWeapon) return;
            _weaponSystem.DropCurrentWeapon();
            Debug.Log("[PlayerController] Weapon dropped with Q key.");
        }

        private void TryPickUpNearbyWeapon()
        {
            if (_weaponSystem == null || _weaponSystem.HasWeapon) return;
            if (_weaponSystem.NearbyWeapon != null)
            {
                _weaponSystem.TryPickUpWeapon(_weaponSystem.NearbyWeapon);
                Debug.Log($"[PlayerController] Weapon picked up with E key: {_weaponSystem.currentWeaponData?.weaponName}");
            }
        }
        
        /// <summary>
        /// Mueve al jugador
        /// </summary>
        private void Move()
        {
            // Rotación: Girar sobre el eje Y basado en la entrada horizontal
            if (Mathf.Abs(_moveInput.x) > 0.01f)
            {
                // Un multiplicador más alto para que el giro se sienta natural y rápido
                float turn = _moveInput.x * rotationSpeed * Time.fixedDeltaTime * 20f;
                Quaternion deltaRotation = Quaternion.Euler(0, turn, 0);
                _rb.MoveRotation(_rb.rotation * deltaRotation);
            }

            // Movimiento: Avanzar/Retroceder basado en la entrada vertical (en espacio local)
            // Usamos transform.forward para que el personaje siempre se mueva hacia donde mira
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();
            
            float targetSpeed = _moveInput.z * moveSpeed;
            Vector3 targetVelocity = forward * targetSpeed;
            
            _rb.linearVelocity = new Vector3(targetVelocity.x, _rb.linearVelocity.y, targetVelocity.z);
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;

            // Calculamos la velocidad actual en el plano horizontal
            float currentHorizontalSpeed = new Vector3(_rb.linearVelocity.x, 0.0f, _rb.linearVelocity.z).magnitude;

            _animator.SetFloat(AnimIDSpeed, currentHorizontalSpeed);
            _animator.SetFloat(AnimIDMotionSpeed, 1f);
            _animator.SetBool(AnimIDGrounded, _isGrounded);
            
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
            Vector3 origin = _col != null ? _col.bounds.center : transform.position;
            float distance = _col != null ? _col.bounds.extents.y + 0.1f : groundCheckDistance;
            
            // Use NonAlloc version to avoid GC allocations
            int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, _groundHitBuffer, distance, groundLayer, QueryTriggerInteraction.Ignore);
            _isGrounded = hitCount > 0;
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
                {
                    _animator.SetBool(AnimIDJump, true);
                }
            }
        }
    }
}
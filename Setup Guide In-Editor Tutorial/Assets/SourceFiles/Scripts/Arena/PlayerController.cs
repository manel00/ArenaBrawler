using UnityEngine;
using System.Collections;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
        private WoW.Armas.PlayerWeaponSystem _weaponSystem;
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
            _weaponSystem = GetComponent<WoW.Armas.PlayerWeaponSystem>();
            _animator = GetComponentInChildren<Animator>();
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
        /// Procesa la entrada del jugador
        /// </summary>
        private void HandleInput()
        {
            float horizontal = 0f;
            float vertical = 0f;
            
#if ENABLE_INPUT_SYSTEM
            // Nuevo Input System
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal = 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical = 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical = -1f;

                if (Keyboard.current.spaceKey.wasPressedThisFrame) Jump();
                if (Keyboard.current.qKey.wasPressedThisFrame) DropCurrentWeapon();
                if (Keyboard.current.eKey.wasPressedThisFrame) TryPickUpNearbyWeapon();
                if (Keyboard.current.fKey.wasPressedThisFrame && _canDash && _currentStamina >= dashStaminaCost) PerformDash();
            }
#else
            // Input System antiguo
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");

            if (Input.GetKeyDown(KeyCode.Space)) Jump();
            if (Input.GetKeyDown(KeyCode.Q)) DropCurrentWeapon();
            if (Input.GetKeyDown(KeyCode.E)) TryPickUpNearbyWeapon();
            if (Input.GetKeyDown(KeyCode.F) && _canDash && _currentStamina >= dashStaminaCost) PerformDash();
#endif

            int abilityIndex = GetPressedAbilityKey();
            if (abilityIndex >= 0 && abilityIndex != 4)
            {
                TryCastAbility(abilityIndex);
            }

            if (IsWeaponAttackHeld())
            {
                TryCastAbility(4);
            }
            
            _moveInput = new Vector3(horizontal, 0f, vertical).normalized;
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

        private int GetPressedAbilityKey()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return -1;

            if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame) return 0;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) return 1;
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) return 2;
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) return 3;
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) return 4;
            if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) return 5;
            if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) return 6;
            if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) return 7;
            if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) return 8;
            if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame) return 9;
#else
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return 2;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return 3;
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) return 4;
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) return 5;
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) return 6;
            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) return 7;
            if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) return 8;
            if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) return 9;
#endif
            return -1;
        }

        private bool IsWeaponAttackHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return kb.digit4Key.isPressed || kb.numpad4Key.isPressed;
#else
            return Input.GetKey(KeyCode.Alpha4) || Input.GetKey(KeyCode.Keypad4);
#endif
        }

        private void TryCastAbility(int index)
        {
            if (index != 5 && Time.time < _nextActionTime) return;
            if (_combatant == null || !_combatant.IsAlive) return;

            bool success = false;
            switch (index)
            {
                case 1: success = CastFireball(); break;
                case 2: success = SummonDog(); break;
                case 4: success = PerformWeaponAttack(); break;
            }

            if (success)
            {
                if (index != 4)
                {
                    _nextActionTime = Time.time + globalCooldown;
                }
            }
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
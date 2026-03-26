using UnityEngine;

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
        public float moveSpeed = 12.5f;
        public float rotationSpeed = 10f;
        public float jumpForce = 12.5f;
        public float gravityMultiplier = 12.5f;

        
        [Header("Ground Check")]
        public LayerMask groundLayer = ~0;
        public float groundCheckDistance = 0.2f;

        [Header("Tab-Target & GCD")]
        public float globalCooldown = 1.0f;
        public float targetSearchDistance = 25f;

        private Rigidbody rb;
        private Animator animator;
        private ArenaCombatant _combatant;
        private Collider _col;
        
        private bool isGrounded;
        private Vector3 moveInput;
        private float _nextActionTime;
        private ArenaCombatant _currentTarget;
        public ArenaCombatant CurrentTarget => _currentTarget;
        
        // Animator parameter hashes for performance
        private readonly int _animIDSpeed = Animator.StringToHash("Speed");
        private readonly int _animIDGrounded = Animator.StringToHash("Grounded");
        private readonly int _animIDJump = Animator.StringToHash("Jump");
        private readonly int _animIDFreeFall = Animator.StringToHash("FreeFall");
        private readonly int _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        
        // Melee sequence tracking
        private int _nextPunchSide = 0; // 0: Left, 1: Right
        private int _nextKickSide = 2;  // 2: Left, 3: Right
        
        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            _col = GetComponent<Collider>();
            _combatant = GetComponent<ArenaCombatant>();
            
            // Find animator in children since the prefab puts it on a 'Robot' child
            animator = GetComponentInChildren<Animator>();
            Debug.Log($"[PlayerController] Started for {gameObject.name}. Animator found: {animator != null}");
        }
        
        private void Update()
        {
            if (_combatant != null && !_combatant.IsAlive) return;

            HandleInput();
            CheckGround();
            UpdateAnimator();
        }
        
        private void FixedUpdate()
        {
            if (_combatant != null && !_combatant.IsAlive) return;

            Move();
            ApplyExtraGravity();
        }

        private void ApplyExtraGravity()
        {
            if (rb.linearVelocity.y < 0)
            {
                rb.AddForce(Vector3.down * gravityMultiplier, ForceMode.Acceleration);
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
                if (Keyboard.current.tabKey.wasPressedThisFrame) FindNextTarget();
                
                if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) TryCastAbility(1);
                if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) TryCastAbility(2);
                if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) TryCastAbility(3);
            }
#else
            // Input System antiguo
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");

            if (Input.GetKeyDown(KeyCode.Space)) Jump();
            if (Input.GetKeyDown(KeyCode.Tab)) FindNextTarget();
            
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) TryCastAbility(1);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) TryCastAbility(2);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) TryCastAbility(3);
#endif
            
            moveInput = new Vector3(horizontal, 0f, vertical).normalized;
        }

        private void TryCastAbility(int index)
        {
            if (Time.time < _nextActionTime) return;
            if (_combatant != null && !_combatant.IsAlive) return;

            bool success = false;
            switch (index)
            {
                case 1: success = CastFireball(); break;
                case 2: success = SummonDog(); break;
                case 3: success = PerformMelee(); break;
            }

            if (success)
            {
                _nextActionTime = Time.time + globalCooldown;
            }
        }

        private bool CastFireball()
        {
            Vector3 direction = transform.forward;
            if (_currentTarget != null)
            {
                direction = (_currentTarget.transform.position + Vector3.up - (transform.position + Vector3.up)).normalized;
                // Face target smoothly
                transform.forward = new Vector3(direction.x, 0, direction.z).normalized;
            }

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

            // [Antigravity] Melee animations are currently disabled because they are placeholders with no clips.
            // if (animator != null)
            // {
            //     animator.SetFloat("AttackType", (float)attackType);
            //     animator.SetFloat("AttackTrigger", 1.0f);
            //     Invoke(nameof(ResetMeleeTrigger), 0.1f);
            // }

            RuntimeSpawner.SpawnMelee(_combatant, transform.position, transform.forward);
            return true;
        }

        private void FindNextTarget()
        {
            ArenaCombatant bestMatch = null;
            float closestDist = targetSearchDistance;
            var all = ArenaCombatant.All;

            foreach (var c in all)
            {
                if (c == null || !c.IsAlive || c == _combatant || c.teamId == _combatant.teamId) continue;

                float dist = Vector3.Distance(transform.position, c.transform.position);
                if (dist < closestDist)
                {
                    // En un sistema real de Tab mejorado, usaríamos ángulos o prioridades de cámara, 
                    // pero para el MVP el más cercano es lo esperado.
                    closestDist = dist;
                    bestMatch = c;
                }
            }

            _currentTarget = bestMatch;
            if (_currentTarget != null)
                Debug.Log($"[PlayerController] Target seleccionado: {_currentTarget.displayName}");
            else
                Debug.Log("[PlayerController] No hay objetivos válidos en rango.");
        }
        
        /// <summary>
        /// Mueve al jugador
        /// </summary>
        private void Move()
        {
            // Rotación: Girar sobre el eje Y basado en la entrada horizontal
            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                // Un multiplicador más alto para que el giro se sienta natural y rápido
                float turn = moveInput.x * rotationSpeed * Time.fixedDeltaTime * 20f;
                Quaternion deltaRotation = Quaternion.Euler(0, turn, 0);
                rb.MoveRotation(rb.rotation * deltaRotation);
            }

            // Movimiento: Avanzar/Retroceder basado en la entrada vertical (en espacio local)
            // Usamos transform.forward para que el personaje siempre se mueva hacia donde mira
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();
            
            float targetSpeed = moveInput.z * moveSpeed;
            Vector3 targetVelocity = forward * targetSpeed;
            
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }

        private void UpdateAnimator()
        {
            if (animator == null) return;

            // Calculamos la velocidad actual en el plano horizontal
            float currentHorizontalSpeed = new Vector3(rb.linearVelocity.x, 0.0f, rb.linearVelocity.z).magnitude;

            animator.SetFloat(_animIDSpeed, currentHorizontalSpeed);
            animator.SetFloat(_animIDMotionSpeed, 1f);
            animator.SetBool(_animIDGrounded, isGrounded);
            
            if (isGrounded)
            {
                animator.SetBool(_animIDJump, false);
                animator.SetBool(_animIDFreeFall, false);
            }
            else if (rb.linearVelocity.y < -0.1f)
            {
                animator.SetBool(_animIDFreeFall, true);
            }
        }
        
        private void CheckGround()
        {
            if (_col != null)
            {
                float distance = _col.bounds.extents.y + 0.1f;
                isGrounded = Physics.Raycast(_col.bounds.center, Vector3.down, distance, groundLayer, QueryTriggerInteraction.Ignore);
            }
            else
            {
                isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
            }
        }
        
        private void ResetMeleeTrigger()
        {
            if (animator != null) animator.SetFloat("AttackTrigger", 0.0f);
        }

        public void Jump()
        {
            if (isGrounded)
            {
                float characterHeight = 2f;
                if (_col != null) characterHeight = _col.bounds.size.y;
                
                float targetHeight = characterHeight * 3.5f;
                float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * targetHeight);
                
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);
                isGrounded = false; 
                
                if (animator != null)
                {
                    animator.SetBool(_animIDJump, true);
                }
            }
        }
    }
}
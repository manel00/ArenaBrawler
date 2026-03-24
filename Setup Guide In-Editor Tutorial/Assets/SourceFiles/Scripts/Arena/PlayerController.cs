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
        
        private Rigidbody rb;
        private Animator animator;
        private bool isGrounded;
        private Vector3 moveInput;
        
        // Animator parameter hashes for performance
        private readonly int _animIDSpeed = Animator.StringToHash("Speed");
        private readonly int _animIDGrounded = Animator.StringToHash("Grounded");
        private readonly int _animIDJump = Animator.StringToHash("Jump");
        private readonly int _animIDFreeFall = Animator.StringToHash("FreeFall");
        private readonly int _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        
        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            
            // Find animator in children since the prefab puts it on a 'Robot' child
            animator = GetComponentInChildren<Animator>();
            Debug.Log($"[PlayerController] Started for {gameObject.name}. Animator found: {animator != null}");
        }
        
        private void Update()
        {
            HandleInput();
            CheckGround();
            UpdateAnimator();
        }
        
        private void FixedUpdate()
        {
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
                if (Keyboard.current.digit1Key.wasPressedThisFrame) CastFireball();
                if (Keyboard.current.digit2Key.wasPressedThisFrame) SummonDog();
            }
#else
            // Input System antiguo
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");

            if (Input.GetKeyDown(KeyCode.Space)) Jump();
            if (Input.GetKeyDown(KeyCode.Alpha1)) CastFireball();
            if (Input.GetKeyDown(KeyCode.Alpha2)) SummonDog();
            if (Input.GetKeyDown(KeyCode.Alpha3)) PerformMelee();
#endif
            
            moveInput = new Vector3(horizontal, 0f, vertical).normalized;
        }

        private void CastFireball()
        {
            var combatant = GetComponent<ArenaEnhanced.ArenaCombatant>();
            RuntimeSpawner.SpawnFireball(combatant, transform.position + transform.forward * 1.5f + Vector3.up, transform.forward, 35f);
        }

        private void SummonDog()
        {
            var combatant = GetComponent<ArenaEnhanced.ArenaCombatant>();
            RuntimeSpawner.SpawnDog(combatant, transform.position + transform.forward * 2f);
        }

        private void PerformMelee()
        {
            var combatant = GetComponent<ArenaEnhanced.ArenaCombatant>();
            if (combatant == null || !combatant.IsAlive) return;

            // Randomized attack animations (0-3: LeftPunch, RightPunch, LeftKick, RightKick)
            int attackType = Random.Range(0, 4);
            if (animator != null)
            {
                animator.SetInteger("AttackType", attackType);
                animator.SetTrigger("Attack");
            }

            RuntimeSpawner.SpawnMelee(combatant, transform.position, transform.forward);
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
            var col = GetComponent<Collider>();
            if (col != null)
            {
                float distance = col.bounds.extents.y + 0.1f;
                // Raycast downwards from the collider's center to ensure robustness regardless of pivot
                isGrounded = Physics.Raycast(col.bounds.center, Vector3.down, distance, groundLayer, QueryTriggerInteraction.Ignore);
            }
            else
            {
                // Fallback
                isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
            }
        }
        
        public void Jump()
        {
            if (isGrounded)
            {
                float characterHeight = 2f;
                var col = GetComponent<Collider>();
                if (col != null) characterHeight = col.bounds.size.y;
                
                float targetHeight = characterHeight * 3.5f;
                // required velocity formula v = sqrt(2 * g * h)
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
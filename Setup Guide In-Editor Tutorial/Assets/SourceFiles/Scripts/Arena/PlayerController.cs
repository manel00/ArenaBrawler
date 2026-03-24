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
        public float moveSpeed = 5f;
        public float rotationSpeed = 10f;
        public float jumpForce = 5f;
        
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
        }
        
        private void Update()
        {
            HandleInput();
            CheckGround();
            UpdateAnimator();
        }

        // --- Animation Event Handlers (Silence Warnings) ---
        public void OnFootstep(AnimationEvent animationEvent) { }
        public void OnLand(AnimationEvent animationEvent) { }
        
        private void FixedUpdate()
        {
            Move();
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
            }
#else
            // Input System antiguo
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
#endif
            
            moveInput = new Vector3(horizontal, 0f, vertical).normalized;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }

            // --- SKILL / ACTION INPUT ---
            if (Input.GetKeyDown(KeyCode.Alpha1)) CastFireball();
            if (Input.GetKeyDown(KeyCode.Alpha2)) SummonDog();
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
            // Improved Ground Check using Capsule Bottom
            var col = GetComponent<CapsuleCollider>();
            float radius = col != null ? col.radius * 0.9f : 0.4f;
            Vector3 origin = transform.position + Vector3.up * radius;
            isGrounded = Physics.CheckSphere(origin, radius + 0.1f, groundLayer, QueryTriggerInteraction.Ignore);
            
            // Debug Visualization
            // Debug.DrawRay(origin, Vector3.down * (radius + 0.1f), isGrounded ? Color.green : Color.red);
        }
        
        /// <summary>
        /// Salta si está en el suelo
        /// </summary>
        public void Jump()
        {
            if (isGrounded)
            {
                // Reset Y velocity for consistent jump feel
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isGrounded = false; 
                
                if (animator != null)
                {
                    animator.SetBool(_animIDJump, true);
                }
            }
        }
    }
}
using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Controlador de animaciones para enemigos - Mejora el movimiento visual
    /// </summary>
    public class EnemyAnimationController : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Velocidad de animación al caminar")]
        [Range(0.5f, 2f)]
        [SerializeField] private float walkAnimationSpeed = 1f;
        
        [Tooltip("Velocidad de animación al correr")]
        [Range(1f, 3f)]
        [SerializeField] private float runAnimationSpeed = 1.5f;
        
        [Tooltip("Umbral de velocidad para considerar idle")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float idleThreshold = 0.1f;
        
        
        [Header("Visual Effects")]
        [Tooltip("Efecto de partículas de polvo")]
        [SerializeField] private GameObject dustEffect;
        
        [Tooltip("Tasa de emisión de polvo")]
        [Range(1f, 20f)]
        [SerializeField] private float dustEmissionRate = 5f;
        
        private Animator _animator;
        private Rigidbody _rb;
        private Vector3 _lastPosition;
        private Vector3 _currentVelocity;
        private bool _isMoving = false;
        private ParticleSystem _dustParticles;
        private float _originalAnimationSpeed;
        
        // Animator parameter hashes (static for performance)
        private static readonly int AnimIDSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIDIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimIDMoveX = Animator.StringToHash("MoveX");
        private static readonly int AnimIDMoveZ = Animator.StringToHash("MoveZ");
        private static readonly int AnimIDTurn = Animator.StringToHash("Turn");
        
        // Cached values to avoid recalculation
        private float _cachedHorizontalSpeed;
        private float _lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.05f; // Update every 50ms instead of every frame
        
        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _rb = GetComponent<Rigidbody>();
            
            if (_animator != null)
            {
                _originalAnimationSpeed = _animator.speed;
            }
            
            // Setup dust particles
            if (dustEffect != null)
            {
                _dustParticles = dustEffect.GetComponent<ParticleSystem>();
                if (_dustParticles != null)
                {
                    var emission = _dustParticles.emission;
                    emission.rateOverTime = 0;
                }
            }
        }
        
        private void Start()
        {
            _lastPosition = transform.position;
            _lastUpdateTime = Time.time;
        }
        
        private void Update()
        {
            if (_animator == null) return;
            
            // Throttle updates to reduce CPU usage
            if (Time.time - _lastUpdateTime < UPDATE_INTERVAL) return;
            _lastUpdateTime = Time.time;
            
            // Calculate velocity
            Vector3 currentPosition = transform.position;
            _currentVelocity = (currentPosition - _lastPosition) / Time.deltaTime;
            _lastPosition = currentPosition;
            
            // Cache horizontal speed
            _cachedHorizontalSpeed = new Vector3(_currentVelocity.x, 0, _currentVelocity.z).magnitude;
            _isMoving = _cachedHorizontalSpeed > idleThreshold;
            
            // Update animator
            UpdateAnimator();
            
            // Update dust effects
            UpdateDustEffects();
        }
        
        private void UpdateAnimator()
        {
            // Set speed parameter
            _animator.SetFloat(AnimIDSpeed, _cachedHorizontalSpeed);
            
            // Set moving state
            _animator.SetBool(AnimIDIsMoving, _isMoving);
            
            // Calculate movement direction relative to facing direction
            Vector3 localVelocity = transform.InverseTransformDirection(_currentVelocity);
            _animator.SetFloat(AnimIDMoveX, localVelocity.x);
            _animator.SetFloat(AnimIDMoveZ, localVelocity.z);
            
            // Adjust animation speed based on movement speed
            if (_isMoving)
            {
                _animator.speed = _cachedHorizontalSpeed > 5f ? runAnimationSpeed : walkAnimationSpeed;
            }
            else
            {
                _animator.speed = _originalAnimationSpeed;
            }
        }
        
        private void UpdateDustEffects()
        {
            if (_dustParticles == null) return;
            
            var emission = _dustParticles.emission;
            
            if (_isMoving)
            {
                // Emit dust based on speed
                emission.rateOverTime = dustEmissionRate * _cachedHorizontalSpeed;
                
                // Position dust at feet
                _dustParticles.transform.position = transform.position;
            }
            else
            {
                emission.rateOverTime = 0;
            }
        }
        
        public void TriggerAttackAnimation()
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }
        }
        
        public void TriggerDeathAnimation()
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Die");
                _animator.SetBool("IsDead", true);
            }
        }
        
        public void TriggerHitAnimation()
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Hit");
            }
        }
        
        public bool IsMoving() => _isMoving;
        public float GetMovementSpeed() => _cachedHorizontalSpeed;
    }
}
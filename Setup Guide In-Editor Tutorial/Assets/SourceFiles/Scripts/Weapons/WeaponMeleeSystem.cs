using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de combate melee con animaciones DoubleL
    /// Gestiona ataques melee, combos y sistema sheathe/unsheathe
    /// </summary>
    public class WeaponMeleeSystem : MonoBehaviour
    {
        [Header("Configuración de Animación")]
        [SerializeField] private Animator animator;
        [SerializeField] private float comboResetTime = 2f;
        [SerializeField] private float autoSheatheTime = 3f;
        
        [Header("Puntos de Sujeción del Arma")]
        [SerializeField] private Transform handHoldPoint;
        [SerializeField] private Transform sheathePoint;
        
        [Header("Referencia al Arma")]
        [SerializeField] private GameObject weaponModel;
        [SerializeField] private MeleeHitbox meleeHitbox;
        
        // State
        private bool _isWeaponDrawn = false;
        private bool _isAttacking = false;
        private int _currentCombo = 0;
        private float _lastAttackTime;
        private float _lastActionTime;
        private WeaponData _currentWeapon;
        private PlayerWeaponSystem _weaponSystem;
        private ArenaCombatant _combatant;
        
        // Animator Hashes
        private static readonly int IsWeaponDrawnHash = Animator.StringToHash("IsWeaponDrawn");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");
        private static readonly int SheatheTriggerHash = Animator.StringToHash("Sheathe");
        private static readonly int UnsheatheTriggerHash = Animator.StringToHash("Unsheathe");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int ComboCountHash = Animator.StringToHash("ComboCount");
        
        public bool IsWeaponDrawn => _isWeaponDrawn;
        public bool IsAttacking => _isAttacking;
        public int CurrentCombo => _currentCombo;
        
        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            _weaponSystem = GetComponent<PlayerWeaponSystem>();
            _combatant = GetComponent<ArenaCombatant>();
            
            EnsureHoldPoints();
            
            if (_weaponSystem != null)
            {
                _weaponSystem.OnWeaponEquipped += OnWeaponEquipped;
                _weaponSystem.OnWeaponBroken += OnWeaponBroken;
            }
        }
        
        private void OnDestroy()
        {
            if (_weaponSystem != null)
            {
                _weaponSystem.OnWeaponEquipped -= OnWeaponEquipped;
                _weaponSystem.OnWeaponBroken -= OnWeaponBroken;
            }
        }
        
        private void Update()
        {
            if (!_isWeaponDrawn || _isAttacking) 
            {
                _lastActionTime = Time.time;
                return;
            }
            
            // Auto-sheathe después de inactividad
            if (Time.time - _lastActionTime > autoSheatheTime)
            {
                SheatheWeapon();
            }
            
            // Reset combo después de tiempo
            if (_currentCombo > 0 && Time.time - _lastAttackTime > comboResetTime)
            {
                ResetCombo();
            }
        }
        
        private void EnsureHoldPoints()
        {
            if (handHoldPoint == null)
            {
                Transform rightHand = FindDeepChild(transform, "Right_Hand") 
                    ?? FindDeepChild(transform, "RightHand")
                    ?? FindDeepChild(transform, "Hand_R");
                
                if (rightHand != null)
                {
                    handHoldPoint = rightHand;
                }
            }
            
            if (sheathePoint == null)
            {
                Transform spine = FindDeepChild(transform, "Spine") 
                    ?? FindDeepChild(transform, "Spine1")
                    ?? FindDeepChild(transform, "Spine2");
                
                if (spine != null)
                {
                    GameObject sheatheObj = new GameObject("SheathePoint");
                    sheatheObj.transform.SetParent(spine);
                    sheatheObj.transform.localPosition = new Vector3(0.15f, 0.05f, 0.2f);
                    sheatheObj.transform.localRotation = Quaternion.Euler(0, 90, 90);
                    sheathePoint = sheatheObj.transform;
                }
            }
        }
        
        private Transform FindDeepChild(Transform parent, string name)
        {
            Transform result = parent.Find(name);
            if (result != null) return result;
            
            foreach (Transform child in parent)
            {
                result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }
        
        private void OnWeaponEquipped(WeaponData weapon, int ammo)
        {
            _currentWeapon = weapon;
            
            if (weapon.type == WeaponType.Melee || weapon.type == WeaponType.MeleeSword)
            {
                SetupMeleeWeapon(weapon);
                DrawWeapon();
            }
        }
        
        private void OnWeaponBroken()
        {
            if (_isWeaponDrawn)
            {
                SheatheWeapon();
            }
            _currentWeapon = null;
        }
        
        private void SetupMeleeWeapon(WeaponData weapon)
        {
            // La espada ya es creada por PlayerWeaponSystem
            if (_weaponSystem.currentWeaponModel != null)
            {
                weaponModel = _weaponSystem.currentWeaponModel;
                
                // Añadir o actualizar hitbox
                meleeHitbox = weaponModel.GetComponent<MeleeHitbox>();
                if (meleeHitbox == null)
                {
                    meleeHitbox = weaponModel.AddComponent<MeleeHitbox>();
                }
                
                meleeHitbox.Initialize(_combatant, weapon);
                meleeHitbox.SetActive(false);
            }
        }
        
        /// <summary>
        /// Intenta realizar un ataque melee
        /// </summary>
        public bool TryAttack()
        {
            if (_currentWeapon == null) return false;
            if (!_isWeaponDrawn)
            {
                DrawWeapon();
                return false;
            }
            if (_isAttacking) return false;
            
            PerformAttack();
            return true;
        }
        
        private void PerformAttack()
        {
            _isAttacking = true;
            _lastAttackTime = Time.time;
            _lastActionTime = Time.time;
            
            if (animator == null)
            {
                Debug.LogError("[WeaponMeleeSystem] No Animator found - cannot perform attack animation");
                _isAttacking = false;
                return;
            }
            
            // Alternar entre Attack_A y Attack_B basado en combo
            int attackType = _currentCombo % 2; // 0 = A, 1 = B
            
            animator.SetInteger(AttackTypeHash, attackType);
            animator.SetInteger(ComboCountHash, _currentCombo);
            animator.SetTrigger(AttackTriggerHash);
            
            _currentCombo = (_currentCombo + 1) % 3; // Max 3 combos
        }
        
        /// <summary>
        /// Desenvaina el arma (llamado por animación o input)
        /// </summary>
        public void DrawWeapon()
        {
            if (_isWeaponDrawn || _currentWeapon == null) return;
            
            _isWeaponDrawn = true;
            animator.SetBool(IsWeaponDrawnHash, true);
            animator.SetTrigger(UnsheatheTriggerHash);
            
            MoveWeaponToHand();
            
            _lastActionTime = Time.time;
        }
        
        /// <summary>
        /// Guarda el arma (llamado por animación o tiempo)
        /// </summary>
        public void SheatheWeapon()
        {
            if (!_isWeaponDrawn || _isAttacking) return;
            
            _isWeaponDrawn = false;
            animator.SetBool(IsWeaponDrawnHash, false);
            animator.SetTrigger(SheatheTriggerHash);
            
            MoveWeaponToSheathe();
            ResetCombo();
        }
        
        private void MoveWeaponToHand()
        {
            if (weaponModel == null || handHoldPoint == null) return;
            
            weaponModel.transform.SetParent(handHoldPoint, false);
            weaponModel.transform.localPosition = Vector3.zero;
            weaponModel.transform.localRotation = Quaternion.identity;
            weaponModel.transform.localScale = Vector3.one * 0.5f;
        }
        
        private void MoveWeaponToSheathe()
        {
            if (weaponModel == null || sheathePoint == null) return;
            
            weaponModel.transform.SetParent(sheathePoint, false);
            weaponModel.transform.localPosition = Vector3.zero;
            weaponModel.transform.localRotation = Quaternion.identity;
            weaponModel.transform.localScale = Vector3.one * 0.4f;
        }
        
        private void ResetCombo()
        {
            _currentCombo = 0;
            animator.SetInteger(ComboCountHash, 0);
        }
        
        // Animation Events - Llamados desde el Animator
        
        /// <summary>
        /// Evento de animación: Inicio del ataque (activar hitbox)
        /// </summary>
        public void OnAttackStart()
        {
            if (meleeHitbox != null)
            {
                meleeHitbox.SetActive(true);
            }
        }
        
        /// <summary>
        /// Evento de animación: Impacto del ataque (momento óptimo de daño)
        /// </summary>
        public void OnAttackHit()
        {
            // El daño se aplica desde MeleeHitbox OnTriggerEnter
        }
        
        /// <summary>
        /// Evento de animación: Fin del ataque (desactivar hitbox)
        /// </summary>
        public void OnAttackEnd()
        {
            _isAttacking = false;
            
            if (meleeHitbox != null)
            {
                meleeHitbox.SetActive(false);
            }
            
            _lastActionTime = Time.time;
        }
        
        /// <summary>
        /// Actualiza parámetros de movimiento en el animator
        /// </summary>
        public void UpdateMovement(float speed, bool isMoving)
        {
            if (animator == null) return;
            
            animator.SetFloat(MoveSpeedHash, speed);
            animator.SetBool(IsMovingHash, isMoving);
        }
        
        // Debug
        private void OnDrawGizmosSelected()
        {
            if (handHoldPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(handHoldPoint.position, 0.05f);
            }
            
            if (sheathePoint != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(sheathePoint.position, 0.05f);
            }
        }
    }
}

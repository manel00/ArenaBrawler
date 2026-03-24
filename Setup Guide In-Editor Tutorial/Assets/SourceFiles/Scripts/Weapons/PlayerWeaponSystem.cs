using UnityEngine;

namespace WoW.Armas
{
    /// <summary>
    /// Sistema de armas del jugador - gestiona equipar, usar y recoger armas
    /// </summary>
    public class PlayerWeaponSystem : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private Transform weaponHoldPoint;
        [SerializeField] private LayerMask weaponPickupLayer = ~0;
        
        [Header("Arma Actual")]
        public WeaponData currentWeaponData;
        public int currentDurability;
        public GameObject currentWeaponModel;
        
        [Header("Input")]
        [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode dropKey = KeyCode.G;
        [SerializeField] private KeyCode pickupKey = KeyCode.E;
        
        [Header("Configuración de Ataque")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private Vector3 attackDirection = Vector3.forward;
        
        // Privadas
        private int _usesSinceLastPickup;
        private float _lastAttackTime;
        private bool _isAttacking;
        private WeaponPickup _nearbyWeapon;
        private const int MAX_USES_BEFORE_DROP = 5;
        
        // Events
        public System.Action<WeaponData, int> OnWeaponEquipped;
        public System.Action OnWeaponBroken;
        public System.Action<int, int> OnDurabilityChanged;
        
        // Propiedades públicas
        public int RemainingUses => MAX_USES_BEFORE_DROP - _usesSinceLastPickup;
        public bool HasWeapon => currentWeaponData != null;
        public bool HasNearbyWeapon => _nearbyWeapon != null;
        
        private void Update()
        {
            HandleWeaponInput();
            CheckForNearbyWeapons();
        }
        
        /// <summary>
        /// Gestiona la entrada del jugador para atacar, recoger y soltar armas
        /// </summary>
        private void HandleWeaponInput()
        {
            // Ataque
            if (Input.GetKeyDown(attackKey) && HasWeapon)
            {
                Attack();
            }
            
            // Soltar arma manualmente
            if (Input.GetKeyDown(dropKey) && HasWeapon)
            {
                DropCurrentWeapon();
            }
            
            // Recoger arma
            if (Input.GetKeyDown(pickupKey) && _nearbyWeapon != null && !HasWeapon)
            {
                PickUpWeapon(_nearbyWeapon);
            }
        }
        
        /// <summary>
        /// Detecta armas cercanas que se pueden recoger
        /// </summary>
        private void CheckForNearbyWeapons()
        {
            // Buscar armas cercanas con un overlap sphere
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f, weaponPickupLayer);
            
            WeaponPickup closestWeapon = null;
            float closestDistance = float.MaxValue;
            
            foreach (var hit in hitColliders)
            {
                var pickup = hit.GetComponent<WeaponPickup>();
                if (pickup != null)
                {
                    float distance = Vector3.Distance(transform.position, pickup.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestWeapon = pickup;
                    }
                }
            }
            
            _nearbyWeapon = closestWeapon;
        }
        
        /// <summary>
        /// Realiza un ataque con el arma actual
        /// </summary>
        public void Attack()
        {
            if (!HasWeapon) return;
            if (Time.time - _lastAttackTime < currentWeaponData.attackCooldown) return;
            
            _lastAttackTime = Time.time;
            _usesSinceLastPickup++;
            
            // Reducir durabilidad
            currentDurability--;
            OnDurabilityChanged?.Invoke(currentDurability, currentWeaponData.maxDurability);
            
            // Efecto visual del ataque
            PerformAttackVFX();
            
            // Verificar si el arma se rompe
            if (currentDurability <= 0)
            {
                BreakWeapon();
            }
            else if (_usesSinceLastPickup >= MAX_USES_BEFORE_DROP)
            {
                // El arma se rompe después de 5 usos
                BreakWeapon();
            }
        }
        
        /// <summary>
        /// Efectos visuales del ataque
        /// </summary>
        private void PerformAttackVFX()
        {
            // Spawn VFX si está configurado
            if (currentWeaponData.attackVFX != null)
            {
                Vector3 spawnPos = transform.position + transform.forward * attackRange * 0.5f;
                Instantiate(currentWeaponData.attackVFX, spawnPos, Quaternion.LookRotation(transform.forward));
            }
            
            // Sonido de ataque
            if (currentWeaponData.attackSound != null)
            {
                AudioSource.PlayClipAtPoint(currentWeaponData.attackSound, transform.position);
            }
            
            // Debug visual
            Debug.Log($"[PlayerWeaponSystem] Ataque con {currentWeaponData.weaponName}. Usos restantes: {RemainingUses}");
        }
        
        /// <summary>
        /// El arma se rompe y se suelta
        /// </summary>
        private void BreakWeapon()
        {
            Debug.Log($"[PlayerWeaponSystem] ¡El arma {currentWeaponData.weaponName} se ha roto!");
            
            // Destruir modelo visual del arma
            if (currentWeaponModel != null)
            {
                Destroy(currentWeaponModel);
            }
            
            OnWeaponBroken?.Invoke();
            
            // Limpiar estado del arma
            ClearCurrentWeapon();
        }
        
        /// <summary>
        /// Suelta el arma actual en el suelo
        /// </summary>
        public void DropCurrentWeapon()
        {
            if (!HasWeapon) return;
            
            // Calcular posición de drop (delante del jugador)
            Vector3 dropPosition = transform.position + transform.forward * 1.5f;
            dropPosition.y = 0.5f; // Altura del suelo
            
            // Crear pickup en el suelo
            WeaponPickup.CreatePickup(currentWeaponData, dropPosition, currentDurability);
            
            Debug.Log($"[PlayerWeaponSystem] Arma {currentWeaponData.weaponName} soltada en el suelo");
            
            // Limpiar estado
            ClearCurrentWeapon();
        }
        
        /// <summary>
        /// Intenta recoger un arma del suelo
        /// </summary>
        public void TryPickUpWeapon(WeaponPickup pickup)
        {
            if (pickup == null) return;
            
            // Si ya tenemos un arma, soltarla primero
            if (HasWeapon)
            {
                DropCurrentWeapon();
            }
            
            PickUpWeapon(pickup);
        }
        
        /// <summary>
        /// Recoge un arma del suelo
        /// </summary>
        private void PickUpWeapon(WeaponPickup pickup)
        {
            currentWeaponData = pickup.WeaponData;
            currentDurability = pickup.CurrentDurability;
            _usesSinceLastPickup = 0;
            
            // Crear modelo visual del arma
            CreateWeaponModel();
            
            Debug.Log($"[PlayerWeaponSystem] Recogida {currentWeaponData.weaponName} con {currentDurability} durabilidad");
            
            // Notificar evento
            OnWeaponEquipped?.Invoke(currentWeaponData, currentDurability);
            OnDurabilityChanged?.Invoke(currentDurability, currentWeaponData.maxDurability);
            
            // Destruir el pickup del suelo
            pickup.PickUp();
        }
        
        /// <summary>
        /// Crea el modelo visual del arma en la mano del jugador
        /// </summary>
        private void CreateWeaponModel()
        {
            // Destruir modelo anterior si existe
            if (currentWeaponModel != null)
            {
                Destroy(currentWeaponModel);
            }
            
            if (currentWeaponData.prefab != null)
            {
                // Usar el prefab del arma
                currentWeaponModel = Instantiate(currentWeaponData.prefab, weaponHoldPoint);
            }
            else
            {
                // Crear un cubo como modelo genérico
                currentWeaponModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                currentWeaponModel.transform.SetParent(weaponHoldPoint);
                currentWeaponModel.transform.localPosition = Vector3.zero;
                currentWeaponModel.transform.localRotation = Quaternion.identity;
                currentWeaponModel.transform.localScale = currentWeaponData.weaponScale * 0.3f;
                
                // Material con el color del arma
                var renderer = currentWeaponModel.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Standard"));
                    renderer.material.color = currentWeaponData.weaponColor;
                }
                
                // Remover collider del modelo visual
                var collider = currentWeaponModel.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }
            
            currentWeaponModel.transform.localPosition = Vector3.zero;
            currentWeaponModel.transform.localRotation = Quaternion.identity;
        }
        
        /// <summary>
        /// Limpia el estado del arma actual
        /// </summary>
        private void ClearCurrentWeapon()
        {
            if (currentWeaponModel != null)
            {
                Destroy(currentWeaponModel);
                currentWeaponModel = null;
            }
            
            currentWeaponData = null;
            currentDurability = 0;
            _usesSinceLastPickup = 0;
        }
        
        /// <summary>
        /// Equipa un arma directamente (para pruebas)
        /// </summary>
        public void EquipWeapon(WeaponData data)
        {
            if (data == null) return;
            
            // Si ya tenemos un arma, soltarla
            if (HasWeapon)
            {
                DropCurrentWeapon();
            }
            
            currentWeaponData = data;
            currentDurability = data.maxDurability;
            _usesSinceLastPickup = 0;
            
            CreateWeaponModel();
            
            OnWeaponEquipped?.Invoke(currentWeaponData, currentDurability);
            OnDurabilityChanged?.Invoke(currentDurability, currentWeaponData.maxDurability);
        }
        
        // Debug
        private void OnDrawGizmosSelected()
        {
            // Rango de detección de armas
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 2f);
            
            // Rango de ataque
            if (HasWeapon)
            {
                Gizmos.color = Color.red;
                Vector3 attackOrigin = transform.position + Vector3.up;
                Gizmos.DrawRay(attackOrigin, transform.forward * attackRange);
            }
        }
    }
}

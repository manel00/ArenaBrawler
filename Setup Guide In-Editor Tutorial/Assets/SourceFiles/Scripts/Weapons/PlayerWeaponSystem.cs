using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de armas del jugador - gestiona equipar, usar y recoger armas
    /// </summary>
    public class PlayerWeaponSystem : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private Transform weaponHoldPoint;
        [SerializeField] private LayerMask weaponPickupLayer = ~0;
        [SerializeField] private float pickupScanRadius = 2f;
        
        [Header("Arma Actual")]
        public WeaponData currentWeaponData;
        public int currentAmmo;
        public GameObject currentWeaponModel;
        
        private ArenaCombatant _owner;
        private float _lastAttackTime;
        private WeaponPickup _nearbyWeapon;
        private GameObject _flameEffectInstance;
        private bool _isAttackingThisFrame;

        // Sistema de lanzallamas masivo premium
        private FlamethrowerVFXController _flamethrowerVFX;
        private FlamethrowerDamageZone _flamethrowerDamage;
        private bool _hasFlamethrowerComponents = false;
        
        // VFX de fuego del lanzallamas (usando assets existentes)
        private GameObject _flamethrowerFireInstance;
        private static readonly string FLAMETHROWER_VFX_PATH = "SourceFiles/VFX/VFX_FloatUp";
        private static readonly Vector3 FLAMETHROWER_VFX_OFFSET = new Vector3(0, 0, 0.5f);
        private static readonly Vector3 FLAMETHROWER_VFX_SCALE = new Vector3(2f, 2f, 2f);

        private const float HeldWeaponTargetMaxSize = 0.38f;
        
        // Events
        public System.Action<WeaponData, int> OnWeaponEquipped;
        public System.Action OnWeaponBroken;
        public System.Action<int, int> OnAmmoChanged;
        public System.Action<int, int> OnDurabilityChanged;
        
        // Propiedades públicas
        public bool HasWeapon => currentWeaponData != null;
        public bool HasNearbyWeapon => _nearbyWeapon != null;
        public WeaponPickup NearbyWeapon => _nearbyWeapon;
        public float CurrentRange => currentWeaponData != null ? currentWeaponData.attackRange : 0f;

        private void Awake()
        {
            _owner = GetComponent<ArenaCombatant>();
            EnsureWeaponHoldPoint();
        }

        private void Start()
        {
            // Inicializar munición si ya tenemos un arma asignada en el Inspector
            if (currentWeaponData != null && currentAmmo == 0)
            {
                currentAmmo = currentWeaponData.maxAmmo;
                NotifyAmmoChanged();
            }
        }
        
        // Throttle para chequeo de armas cercanas
        private float _lastWeaponCheckTime = 0f;
        private const float WEAPON_CHECK_INTERVAL = 0.15f; // 6-7 veces por segundo es suficiente

        private void Update()
        {
            // Throttle weapon checks
            if (Time.time - _lastWeaponCheckTime >= WEAPON_CHECK_INTERVAL)
            {
                CheckForNearbyWeapons();
                _lastWeaponCheckTime = Time.time;
            }
            
            // Handle weapon input (fallback if InputManager not working)
            HandleWeaponInput();
            
            UpdateContinuousVFX();
            _isAttackingThisFrame = false; // Reset cada frame
        }
        
        /// <summary>
        /// Maneja el input de ataque directamente como fallback
        /// </summary>
        private void HandleWeaponInput()
        {
            // Solo tecla 4 para atacar (sin clic de ratón)
            if (Input.GetKey(KeyCode.Alpha4) || Input.GetKey(KeyCode.Keypad4))
            {
                Attack();
            }
            
            // Tecla Q para soltar arma
            if (Input.GetKeyDown(KeyCode.Q))
            {
                DropCurrentWeapon();
            }
            
            // Tecla E para recoger arma
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryPickUpNearbyWeapon();
            }
        }
        
        /// <summary>
        /// Intenta recoger un arma cercana
        /// </summary>
        public void TryPickUpNearbyWeapon()
        {
            if (HasWeapon || _nearbyWeapon == null) return;
            
            TryPickUpWeapon(_nearbyWeapon);
        }

        private void UpdateContinuousVFX()
        {
            // Si no estamos atacando este frame, desactivar efectos continuos
            if (!_isAttackingThisFrame)
            {
                // Legacy VFX
                if (_flameEffectInstance != null && _flameEffectInstance.activeSelf)
                {
                    _flameEffectInstance.SetActive(false);
                }
                
                // Stop flamethrower premium VFX
                StopFlamethrowerAttack();
            }
        }
        
        /// <summary>
        /// Detecta armas cercanas que se pueden recoger
        /// </summary>
        private void CheckForNearbyWeapons()
        {
            // Buscar armas cercanas con un overlap sphere
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupScanRadius, weaponPickupLayer);
            
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
        public bool Attack()
        {
#if DEBUG
            Debug.Log($"[PlayerWeaponSystem] Attack() llamado. HasWeapon: {HasWeapon}, currentWeapon: {(currentWeaponData != null ? currentWeaponData.weaponName : "null")}");
#endif
            return AttackTarget(null);
        }

        public bool AttackTarget(ArenaCombatant forcedTarget)
        {
            if (!HasWeapon) 
            {
#if DEBUG
                Debug.LogWarning("[PlayerWeaponSystem] AttackTarget falló: No tiene arma");
#endif
                return false;
            }
            
            // Para armas continuas (lanzallamas), no bloquear por cooldown - solo mantener activo
            if (currentWeaponData.fireMode == WeaponFireMode.Continuous)
            {
                _lastAttackTime = Time.time;
                Vector3 origin = GetAttackOrigin();
                Vector3 baseDirection = GetAimDirection(origin, forcedTarget);
                PerformContinuousAttack(origin, baseDirection);
                return true;
            }
            
            // Cooldowns desactivados - disparo libre
            // if (Time.time - _lastAttackTime < currentWeaponData.attackCooldown) 
            // {
            //     Debug.Log($"[PlayerWeaponSystem] AttackTarget falló: Cooldown activo");
            //     return false;
            // }
            if (currentWeaponData.UsesAmmo && currentAmmo == 0) 
            {
#if DEBUG
                Debug.LogWarning("[PlayerWeaponSystem] AttackTarget falló: Sin munición");
#endif
                BreakWeapon();
                return false;
            }
            
#if DEBUG
            Debug.Log($"[PlayerWeaponSystem] Disparando {currentWeaponData.weaponName}! Munición: {currentAmmo}, FireMode: {currentWeaponData.fireMode}");
#endif
            
            _lastAttackTime = Time.time;
            Vector3 origin2 = GetAttackOrigin();
            Vector3 baseDirection2 = GetAimDirection(origin2, forcedTarget);
            
            PerformAttackVFX();

            switch (currentWeaponData.fireMode)
            {
                case WeaponFireMode.Hitscan:
                    PerformHitscanShot(origin2, baseDirection2);
                    break;
                default:
#if DEBUG
                    Debug.Log("[PlayerWeaponSystem] Ejecutando PerformProjectileShot");
#endif
                    PerformProjectileShot(origin2, baseDirection2);
                    break;
            }

            ConsumeAmmoIfNeeded();
            return true;
        }

        private Vector3 GetAttackOrigin()
        {
            // Altura fija de 1.1m sobre el suelo para el punto de salida
            return transform.position + Vector3.up * 1.1f + transform.forward * 0.5f;
        }

        private Vector3 GetAimDirection(Vector3 origin, ArenaCombatant forcedTarget)
        {
            // Siempre disparar hacia adelante, proyectado en horizontal
            Vector3 forward = transform.forward;
            return new Vector3(forward.x, 0, forward.z).normalized;
        }

        private void PerformProjectileShot(Vector3 origin, Vector3 baseDirection)
        {
            if (currentWeaponData == null) return;
            
            int projectileCount = Mathf.Max(1, currentWeaponData.projectilesPerShot);
#if DEBUG
            Debug.Log($"[PlayerWeaponSystem] PerformProjectileShot: {projectileCount} proyectiles, origin={origin}, dir={baseDirection}");
#endif
            for (int i = 0; i < projectileCount; i++)
            {
                Vector3 dir = ApplySpread(baseDirection, currentWeaponData.spreadAngle, i, projectileCount);
#if DEBUG
                Debug.Log($"[PlayerWeaponSystem] Spawning proyectil {i+1}/{projectileCount}, dir={dir}");
#endif
                RuntimeSpawner.SpawnWeaponProjectile(_owner, origin, dir, currentWeaponData);
            }
        }

        private void PerformHitscanShot(Vector3 origin, Vector3 baseDirection)
        {
            if (currentWeaponData == null) return;
            
            int projectileCount = Mathf.Max(1, currentWeaponData.projectilesPerShot);
            for (int i = 0; i < projectileCount; i++)
            {
                Vector3 dir = ApplySpread(baseDirection, currentWeaponData.spreadAngle, i, projectileCount);
                Vector3 hitPoint = origin + dir * currentWeaponData.attackRange;
                ArenaCombatant hitCombatant = null;

                if (Physics.Raycast(origin, dir, out RaycastHit hit, currentWeaponData.attackRange, ~0, QueryTriggerInteraction.Ignore))
                {
                    hitPoint = hit.point;
                    hitCombatant = hit.collider.GetComponentInParent<ArenaCombatant>();
                }

                if (IsValidTarget(hitCombatant))
                {
                    hitCombatant.TakeDamage(currentWeaponData.RollDamage() * GetOwnerDamageMultiplier(), _owner);
                }

                ApplySplashDamage(hitPoint, hitCombatant);
            }
        }

        private void PerformContinuousAttack(Vector3 origin, Vector3 baseDirection)
        {
            _isAttackingThisFrame = true;
            
            // Usar sistema de lanzallamas masivo premium si está disponible
            if (IsFlamethrowerEquipped())
            {
                EnsureFlamethrowerComponents();
                
                if (_hasFlamethrowerComponents)
                {
                    // Activar VFX de fuego usando el controller
                    if (_flamethrowerVFX != null)
                    {
                        if (!_flamethrowerVFX.IsFiring)
                        {
                            _flamethrowerVFX.StartFiring();
                            Debug.Log("[PlayerWeaponSystem] Fire VFX started");
                        }
                    }
                    else
                    {
                        Debug.LogError("[PlayerWeaponSystem] _flamethrowerVFX is null!");
                    }
                    
                    // Activar zona de daño
                    if (_flamethrowerDamage != null && !_flamethrowerDamage.IsActive)
                    {
                        _flamethrowerDamage.Activate();
                    }
                    
                    return; // El daño se aplica por FlamethrowerDamageZone
                }
            }
            
            // Fallback al sistema antiguo (para compatibilidad)
            PerformLegacyContinuousAttack(origin, baseDirection);
        }
        
        private void PerformLegacyContinuousAttack(Vector3 origin, Vector3 baseDirection)
        {
            // VFX legacy desactivado
            // if (currentWeaponData.attackVFX != null)
            // {
            //     if (_flameEffectInstance == null)
            //     {
            //         _flameEffectInstance = Instantiate(currentWeaponData.attackVFX, weaponHoldPoint);
            //         _flameEffectInstance.transform.localPosition = new Vector3(0, 0, 0.8f);
            //         _flameEffectInstance.transform.localRotation = Quaternion.Euler(0, 0, 0);
            //         _flameEffectInstance.transform.localScale = Vector3.one * 1.5f;
            //     }
            //     
            //     if (!_flameEffectInstance.activeSelf)
            //         _flameEffectInstance.SetActive(true);
            // }

            float radius = Mathf.Max(1f, currentWeaponData.spreadAngle * 0.05f);
            RaycastHit[] hits = Physics.SphereCastAll(origin, radius, baseDirection, currentWeaponData.attackRange, ~0, QueryTriggerInteraction.Ignore);
            float tickDamage = currentWeaponData.RollDamagePerSecond() * Mathf.Max(0.05f, currentWeaponData.attackCooldown) * GetOwnerDamageMultiplier();

            foreach (var hit in hits)
            {
                var combatant = hit.collider.GetComponentInParent<ArenaCombatant>();
                if (!IsValidTarget(combatant)) continue;
                combatant.TakeDamage(tickDamage, _owner);
            }
        }
        
        private bool IsFlamethrowerEquipped()
        {
            return currentWeaponData != null && currentWeaponData.type == WeaponType.Flamethrower;
        }
        
        private void EnsureFlamethrowerComponents()
        {
            if (_hasFlamethrowerComponents) return;
            
            // Crear VFX Controller
            if (_flamethrowerVFX == null)
            {
                GameObject vfxObj = new GameObject("FlamethrowerVFX");
                vfxObj.transform.SetParent(weaponHoldPoint, false);
                vfxObj.transform.localPosition = Vector3.zero;
                _flamethrowerVFX = vfxObj.AddComponent<FlamethrowerVFXController>();
            }
            
            // Crear Damage Zone
            if (_flamethrowerDamage == null)
            {
                GameObject damageObj = new GameObject("FlamethrowerDamage");
                damageObj.transform.SetParent(weaponHoldPoint, false);
                _flamethrowerDamage = damageObj.AddComponent<FlamethrowerDamageZone>();
                
                // Configurar owner
                _flamethrowerDamage.SetOwner(_owner);
                
                // Configurar referencia cruzada
                if (currentWeaponData != null)
                {
                    // Usar RollDamagePerSecond para obtener el daño correcto (0.5)
                    float dps = currentWeaponData.RollDamagePerSecond();
                    _flamethrowerDamage.SetDamageParameters(
                        dps,  // Daño por segundo
                        30f,  // 30 metros de alcance
                        12f   // 12 grados de ángulo - coincide con VFX
                    );
                    Debug.Log($"[PlayerWeaponSystem] Flamethrower damage configured: {dps} DPS");
                }
            }
            
            _hasFlamethrowerComponents = true;
        }
        
        private void StopFlamethrowerAttack()
        {
            // Detener efectos de fuego
            if (_flamethrowerVFX != null && _flamethrowerVFX.IsFiring)
                _flamethrowerVFX.StopFiring();
            
            // Detener zona de daño
            if (_flamethrowerDamage != null && _flamethrowerDamage.IsActive)
                _flamethrowerDamage.Deactivate();
        }

        private Vector3 ApplySpread(Vector3 direction, float spreadAngle, int index, int total)
        {
            if (spreadAngle <= 0.001f || total <= 1) return direction.normalized;

            float t = total == 1 ? 0.5f : index / (float)(total - 1);
            float angle = Mathf.Lerp(-spreadAngle, spreadAngle, t);
            return Quaternion.AngleAxis(angle, Vector3.up) * direction.normalized;
        }

        private bool IsValidTarget(ArenaCombatant target)
        {
            return target != null && target.IsAlive && _owner != null && target != _owner && target.teamId != _owner.teamId;
        }

        private float GetOwnerDamageMultiplier()
        {
            return _owner != null ? _owner.damageMultiplier : 1f;
        }

        private void ApplySplashDamage(Vector3 center, ArenaCombatant directTarget)
        {
            if (currentWeaponData.splashRadius <= 0f) return;

            Collider[] hits = Physics.OverlapSphere(center, currentWeaponData.splashRadius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                var combatant = hit.GetComponentInParent<ArenaCombatant>();
                if (!IsValidTarget(combatant) || combatant == directTarget) continue;
                combatant.TakeDamage(currentWeaponData.RollSplashDamage() * GetOwnerDamageMultiplier(), _owner);
            }
        }

        private void ConsumeAmmoIfNeeded()
        {
            if (!currentWeaponData.UsesAmmo) return;

            currentAmmo = Mathf.Max(0, currentAmmo - 1);
            NotifyAmmoChanged();
            
            if (currentAmmo <= 0)
            {
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
                float effectRange = currentWeaponData != null ? currentWeaponData.attackRange : 2f;
                Vector3 spawnPos = transform.position + transform.forward * effectRange * 0.5f;
                Instantiate(currentWeaponData.attackVFX, spawnPos, Quaternion.LookRotation(transform.forward));
            }
            
            // Sonido de ataque
            if (currentWeaponData.attackSound != null)
            {
                AudioSource.PlayClipAtPoint(currentWeaponData.attackSound, transform.position);
            }
            
            // Debug visual
            string ammoText = currentWeaponData.UsesAmmo ? currentAmmo.ToString() : "∞";
            Debug.Log($"[PlayerWeaponSystem] Ataque con {currentWeaponData.weaponName}. Munición restante: {ammoText}");
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
            
            // Calcular posición de drop (a la izquierda del jugador, 1 metro)
            Vector3 dropPosition = transform.position - transform.right * 4f;
            dropPosition.y = 0.5f; // Altura del suelo
            
            // Crear pickup en el suelo
            WeaponPickup.CreatePickup(currentWeaponData, dropPosition, currentAmmo);
            
            Debug.Log($"[PlayerWeaponSystem] Arma {currentWeaponData.weaponName} soltada a la izquierda");
            
            // Limpiar estado
            ClearCurrentWeapon();
        }
        
        /// <summary>
        /// Intenta recoger un arma del suelo
        /// </summary>
        public void TryPickUpWeapon(WeaponPickup pickup)
        {
            if (pickup == null) return;
            if (HasWeapon) return;
            
            PickUpWeapon(pickup);
        }
        
        /// <summary>
        /// Recoge un arma del suelo
        /// </summary>
        private void PickUpWeapon(WeaponPickup pickup)
        {
            currentWeaponData = pickup.WeaponData;
            currentAmmo = pickup.CurrentAmmo;
            
            if (ArenaHUD.Instance != null) ArenaHUD.Instance.UpdateWeaponName(currentWeaponData.weaponName);
            if (!currentWeaponData.UsesAmmo) currentAmmo = -1;
            
            // Crear modelo visual del arma
            CreateWeaponModel();
            
            Debug.Log($"[PlayerWeaponSystem] Recogida {currentWeaponData.weaponName} con {FormatAmmoForLog()} de munición");
            
            // Notificar evento
            OnWeaponEquipped?.Invoke(currentWeaponData, currentAmmo);
            NotifyAmmoChanged();
            
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
            }
            
            currentWeaponModel.transform.localPosition = Vector3.zero;
            currentWeaponModel.transform.localRotation = Quaternion.identity;
            NormalizeHeldWeaponTransform(currentWeaponModel);
            ApplyHeldWeaponOffset(currentWeaponModel);
            ApplyWeaponAppearance(currentWeaponModel);
            RemoveHeldColliders(currentWeaponModel);
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
            
            if (_flameEffectInstance != null)
            {
                Destroy(_flameEffectInstance);
                _flameEffectInstance = null;
            }
            
            currentWeaponData = null;
            currentAmmo = 0;

            if (ArenaHUD.Instance != null) ArenaHUD.Instance.UpdateWeaponName("");
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
            currentAmmo = data.DefaultAmmo;
            
            if (ArenaHUD.Instance != null) ArenaHUD.Instance.UpdateWeaponName(currentWeaponData.weaponName);
            
            CreateWeaponModel();
            
            OnWeaponEquipped?.Invoke(currentWeaponData, currentAmmo);
            NotifyAmmoChanged();
        }

        private void NotifyAmmoChanged()
        {
            int maxAmmo = currentWeaponData != null ? currentWeaponData.DefaultAmmo : 0;
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
            OnDurabilityChanged?.Invoke(currentAmmo, maxAmmo);
        }

        private void EnsureWeaponHoldPoint()
        {
            if (weaponHoldPoint != null) return;

            Transform existing = transform.Find("WeaponHoldPoint");
            if (existing != null)
            {
                weaponHoldPoint = existing;
                return;
            }

            GameObject holdPoint = new GameObject("WeaponHoldPoint");
            holdPoint.transform.SetParent(transform);
            holdPoint.transform.localPosition = new Vector3(0.35f, 1.2f, 0.45f);
            holdPoint.transform.localRotation = Quaternion.identity;
            weaponHoldPoint = holdPoint.transform;
        }

        private void ApplyWeaponAppearance(GameObject model)
        {
            if (model == null || currentWeaponData == null) return;

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (currentWeaponData.weaponMaterial != null)
                {
                    renderer.material = currentWeaponData.weaponMaterial;
                }
                else
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = currentWeaponData.weaponColor;
                    if (currentWeaponData.weaponTexture != null) mat.mainTexture = currentWeaponData.weaponTexture;
                    renderer.material = mat;
                }
            }
        }

        private void RemoveHeldColliders(GameObject model)
        {
            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
            {
                Destroy(collider);
            }
        }

        private string FormatAmmoForLog()
        {
            return currentWeaponData != null && !currentWeaponData.UsesAmmo ? "munición infinita" : currentAmmo.ToString();
        }

        private void NormalizeHeldWeaponTransform(GameObject model)
        {
            if (model == null || currentWeaponData == null) return;
            
            // Tamaño INDEPENDIENTE en mano (no relacionado con tamaño en suelo)
            // Cada arma tiene su propio tamaño fijo para la mano
            float handScale = 0.5f; // Tamaño base
            
            if (currentWeaponData.weaponName != null)
            {
                string name = currentWeaponData.weaponName.ToLower();
                if (name.Contains("shotgun"))
                    handScale = 0.1f;
                else if (name.Contains("assault") || name.Contains("rifle"))
                    handScale = 0.4f;
                else if (name.Contains("flame"))
                    handScale = 2.5f;
            }
            
            model.transform.localScale = Vector3.one * handScale;
        }

        private float GetHeldScaleMultiplier()
        {
            if (currentWeaponData == null) return 1f;

            switch (currentWeaponData.type)
            {
                case WeaponType.Flamethrower:
                    return 0.95f;
                case WeaponType.Ranged:
                    return currentWeaponData.weaponName != null && currentWeaponData.weaponName.ToLower().Contains("shotgun") ? 1.08f : 1f;
                default:
                    return 1f;
            }
        }

        private void ApplyHeldWeaponOffset(GameObject model)
        {
            if (model == null || currentWeaponData == null) return;

            // Posición base y rotacion desde WeaponData
            Vector3 localPos = new Vector3(0.12f, -0.08f, 0.22f);
            Vector3 localRot = currentWeaponData.rotationOffset;

            string weaponName = currentWeaponData.weaponName != null ? currentWeaponData.weaponName.ToLower() : string.Empty;
            if (weaponName.Contains("shotgun"))
            {
                localPos = new Vector3(0.14f, -0.06f, 0.24f);
            }
            else if (weaponName.Contains("assault"))
            {
                localPos = new Vector3(0.12f, -0.08f, 0.22f);
            }
            else if (weaponName.Contains("flame"))
            {
                localPos = new Vector3(0.13f, -0.07f, 0.2f);
                // Flamethrower needs different rotation when held to point forward
                localRot = new Vector3(0, 90f, 0);
            }

            model.transform.localPosition = localPos;
            model.transform.localRotation = Quaternion.Euler(localRot);
        }
        
        // Debug
        private void OnDrawGizmosSelected()
        {
            // Rango de detección de armas
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupScanRadius);
            
            // Rango de ataque
            if (HasWeapon)
            {
                Gizmos.color = Color.red;
                Vector3 attackOrigin = transform.position + Vector3.up;
                Gizmos.DrawRay(attackOrigin, transform.forward * CurrentRange);
            }
        }
    }
}

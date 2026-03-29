using ArenaEnhanced;
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
        
        private void Update()
        {
            CheckForNearbyWeapons();
            UpdateContinuousVFX();
            _isAttackingThisFrame = false; // Reset cada frame
        }

        private void UpdateContinuousVFX()
        {
            if (_flameEffectInstance == null) return;
            
            // Si no estamos atacando este frame, desactivar el efecto
            if (!_isAttackingThisFrame)
            {
                if (_flameEffectInstance.activeSelf)
                    _flameEffectInstance.SetActive(false);
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
            Debug.Log($"[PlayerWeaponSystem] Attack() llamado. HasWeapon: {HasWeapon}, currentWeapon: {(currentWeaponData != null ? currentWeaponData.weaponName : "null")}");
            return AttackTarget(null);
        }

        public bool AttackTarget(ArenaCombatant forcedTarget)
        {
            if (!HasWeapon) 
            {
                Debug.LogWarning("[PlayerWeaponSystem] AttackTarget falló: No tiene arma");
                return false;
            }
            if (Time.time - _lastAttackTime < currentWeaponData.attackCooldown) 
            {
                Debug.Log($"[PlayerWeaponSystem] AttackTarget falló: Cooldown activo ({Time.time - _lastAttackTime:F2} < {currentWeaponData.attackCooldown:F2})");
                return false;
            }
            if (currentWeaponData.UsesAmmo && currentAmmo == 0) 
            {
                Debug.LogWarning("[PlayerWeaponSystem] AttackTarget falló: Sin munición");
                BreakWeapon();
                return false;
            }
            
            Debug.Log($"[PlayerWeaponSystem] Disparando {currentWeaponData.weaponName}! Munición: {currentAmmo}, FireMode: {currentWeaponData.fireMode}");
            
            _lastAttackTime = Time.time;
            Vector3 origin = GetAttackOrigin();
            Vector3 baseDirection = GetAimDirection(origin, forcedTarget);
            
            PerformAttackVFX();

            switch (currentWeaponData.fireMode)
            {
                case WeaponFireMode.Hitscan:
                    PerformHitscanShot(origin, baseDirection);
                    break;
                case WeaponFireMode.Continuous:
                    PerformContinuousAttack(origin, baseDirection);
                    break;
                default:
                    Debug.Log("[PlayerWeaponSystem] Ejecutando PerformProjectileShot");
                    PerformProjectileShot(origin, baseDirection);
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
            int projectileCount = Mathf.Max(1, currentWeaponData.projectilesPerShot);
            Debug.Log($"[PlayerWeaponSystem] PerformProjectileShot: {projectileCount} proyectiles, origin={origin}, dir={baseDirection}");
            for (int i = 0; i < projectileCount; i++)
            {
                Vector3 dir = ApplySpread(baseDirection, currentWeaponData.spreadAngle, i, projectileCount);
                Debug.Log($"[PlayerWeaponSystem] Spawning proyectil {i+1}/{projectileCount}, dir={dir}");
                RuntimeSpawner.SpawnWeaponProjectile(_owner, origin, dir, currentWeaponData);
            }
        }

        private void PerformHitscanShot(Vector3 origin, Vector3 baseDirection)
        {
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
            _isAttackingThisFrame = true; // Indicar que estamos atacando
            
            // Gestionar VFX
            if (currentWeaponData.attackVFX != null)
            {
                if (_flameEffectInstance == null)
                {
                    _flameEffectInstance = Instantiate(currentWeaponData.attackVFX, weaponHoldPoint);
                    _flameEffectInstance.transform.localPosition = new Vector3(0, 0, 0.5f); // Delante del arma
                    _flameEffectInstance.transform.localRotation = Quaternion.identity;
                }
                
                if (!_flameEffectInstance.activeSelf)
                    _flameEffectInstance.SetActive(true);
            }

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
            
            // Calcular posición de drop (delante del jugador)
            Vector3 dropPosition = transform.position + transform.forward * 1.5f;
            dropPosition.y = 0.5f; // Altura del suelo
            
            // Crear pickup en el suelo
            WeaponPickup.CreatePickup(currentWeaponData, dropPosition, currentAmmo);
            
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

            if (ArenaHUD.Instance != null) ArenaHUD.Instance.UpdateWeaponName("NO WEAPON");
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
            if (model == null) return;

            model.transform.localScale = Vector3.one;

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                model.transform.localScale = Vector3.one * 0.3f;
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float maxDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxDimension < 0.0001f)
            {
                model.transform.localScale = Vector3.one * 0.3f;
                return;
            }

            float typeMultiplier = GetHeldScaleMultiplier();
            float normalizedScale = (HeldWeaponTargetMaxSize / maxDimension) * typeMultiplier;
            normalizedScale = Mathf.Clamp(normalizedScale, 0.03f, 0.35f);
            model.transform.localScale = Vector3.one * normalizedScale;
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
            else if (weaponName.Contains("flame"))
            {
                localPos = new Vector3(0.13f, -0.07f, 0.2f);
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

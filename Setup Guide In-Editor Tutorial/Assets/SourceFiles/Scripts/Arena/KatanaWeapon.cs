using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaEnhanced
{
    /// <summary>
    /// Ice Katana weapon system for the player.
    /// K          -> equip / unequip
    /// 3 (tap)    -> 5-hit rapid samurai combo
    /// 3 (hold)   -> charged wide slash (more damage + ice knockback)
    ///
    /// Visual simulation: TrailRenderer on blade tip + LineRenderer slash arc + camera shake.
    /// No external animation clips required.
    /// </summary>
    [RequireComponent(typeof(ArenaCombatant))]
    public class KatanaWeapon : MonoBehaviour
    {
        // ── Combat stats ─────────────────────────────────────────────────────
        [Header("Katana Stats")]
        public float rapidDamagePerHit = 18f;
        public float chargedDamage = 65f;
        public float attackRange = 2.8f;
        public float comboWindow = 0.5f;
        public float chargeThreshold = 0.45f;
        public float cooldownAfterCombo = 1.1f;
        public float cooldownAfterCharge = 1.8f;

        private GameBalanceConfig _balanceConfig;

        // ── VFX ──────────────────────────────────────────────────────────────
        [Header("VFX")]
        public Color trailColorA         = new Color(0.5f, 0.9f, 1f,  1f);
        public Color trailColorB         = new Color(0.1f, 0.4f, 1f,  0f);
        public Color chargedTrailColorA  = new Color(1f,   1f,   1f,  1f);
        public Color chargedTrailColorB  = new Color(0.6f, 0.2f, 1f,  0f);

        // ── Internal refs ────────────────────────────────────────────────────
        private ArenaCombatant _combatant;
        private Animator       _animator;
        private Transform      _handBone;
        private Camera         _cam;

        private GameObject     _katanaRoot;
        private TrailRenderer  _bladeTrail;

        // ── Hand Positioning ───────────────────────────────────────────────────
        [Header("Hand Positioning (Local Space)")]
        [Tooltip("Position offset relative to hand bone")]
        public Vector3 handPositionOffset = new Vector3(0.05f, 0.02f, 0.02f);
        [Tooltip("Rotation offset relative to hand bone (Euler angles)")]
        public Vector3 handRotationOffset = new Vector3(0f, -90f, 90f);
        [Tooltip("Uniform scale of the katana model")]
        public float modelScale = 0.015f;

        // ── State ────────────────────────────────────────────────────────────
        private bool  _equipped   = false;
        private bool  _onCooldown = false;
        private float _key5DownAt = -1f;     // timestamp when 5 was first pressed

        // Public accessor for other systems
        public bool IsEquipped => _equipped;

        // Animator trigger hashes
        private static readonly int HASH_ATTACK  = Animator.StringToHash("Attack");
        private static readonly int HASH_ATTACK2 = Animator.StringToHash("Attack2");

        // ── Material caching para evitar memory leaks ────────────────────────
        // NOTA: No usar static para permitir limpieza correcta al recargar escenas
        private Material _cachedDefaultMaterial;
        private Material _cachedURPMaterial;
        private Material _cachedURPSimpleMaterial;
        private Material _cachedStandardMaterial;
        private Material _cachedDiffuseMaterial;

        private Material GetCachedDefaultMaterial()
        {
            if (_cachedDefaultMaterial == null)
                _cachedDefaultMaterial = new Material(Shader.Find("Sprites/Default"));
            return _cachedDefaultMaterial;
        }

        private Material GetCachedURPMaterial()
        {
            if (_cachedURPMaterial == null)
                _cachedURPMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            return _cachedURPMaterial;
        }

        private Material GetCachedURPSimpleMaterial()
        {
            if (_cachedURPSimpleMaterial == null)
                _cachedURPSimpleMaterial = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
            return _cachedURPSimpleMaterial;
        }

        private Material GetCachedStandardMaterial()
        {
            if (_cachedStandardMaterial == null)
                _cachedStandardMaterial = new Material(Shader.Find("Standard"));
            return _cachedStandardMaterial;
        }

        private Material GetCachedDiffuseMaterial()
        {
            if (_cachedDiffuseMaterial == null)
                _cachedDiffuseMaterial = new Material(Shader.Find("Diffuse"));
            return _cachedDiffuseMaterial;
        }

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _combatant = GetComponent<ArenaCombatant>();
            if (_combatant == null)
            {
                _combatant = gameObject.AddComponent<ArenaCombatant>();
                _combatant.teamId = 1;
                _combatant.displayName = "Player";
            }
            
            // Load balance config
            _balanceConfig = Resources.Load<GameBalanceConfig>("Configs/GameBalanceConfig");
            if (_balanceConfig != null)
            {
                rapidDamagePerHit = _balanceConfig.katanaRapidDamage;
                chargedDamage = _balanceConfig.katanaChargedDamage;
                cooldownAfterCombo = _balanceConfig.katanaComboCooldown;
                cooldownAfterCharge = _balanceConfig.katanaChargedCooldown;
            }
            
            _animator  = GetComponentInChildren<Animator>();
            _cam       = Camera.main;
            LocateHandBone();
        }

        private void OnEnable()
        {
            // Subscribe to InputManager events
            InputManager.OnKatanaEquipToggle += ToggleEquip;
            InputManager.OnKatanaAttackPressed += OnAttackPressed;
            InputManager.OnKatanaAttackReleased += OnAttackReleased;
        }

        private void OnDisable()
        {
            // Unsubscribe from InputManager events
            InputManager.OnKatanaEquipToggle -= ToggleEquip;
            InputManager.OnKatanaAttackPressed -= OnAttackPressed;
            InputManager.OnKatanaAttackReleased -= OnAttackReleased;
        }

        private void Start()
        {
            // Auto-equip katana on start - no need to press K
            if (!_equipped)
            {
                ToggleEquip();
            }
        }

        private void Update()
        {
            // NOTA: El input se maneja a través de eventos de InputManager:
            // - K: ToggleEquip (OnKatanaEquipToggle)
            // - 3: OnAttackPressed/Released (OnKatanaAttackPressed/Released)
            // NO agregar input directo aquí para evitar duplicación
        }

        private void LocateHandBone()
        {
            // Más nombres comunes de huesos de mano para diferentes modelos/rigs
            string[] boneNames = { 
                "RightHand", "Hand_R", "mixamorig:RightHand", "r_hand", 
                "Right_Hand", "hand_r", "Bip01_R_Hand", "bip001 r hand",
                "Hand_R_01", "R_Hand", "RightHandIndex1", "hand.R"
            };
            
            foreach (string n in boneNames)
            {
                var t = DeepFind(transform, n);
                if (t != null) { 
                    _handBone = t; 
                    return; 
                }
            }
            
            _handBone = FindBoneContaining(transform, "hand");
            if (_handBone != null)
            {
                return;
            }
            
            // Último fallback: usar el transform del jugador
            _handBone = transform;
        }

        private Transform DeepFind(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                var r = DeepFind(child, name);
                if (r != null) return r;
            }
            return null;
        }

        private Transform FindBoneContaining(Transform root, string partialName)
        {
            string lowerPartial = partialName.ToLower();
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLower().Contains(lowerPartial))
                    return child;
            }
            return null;
        }

        // ── K: toggle equip ──────────────────────────────────────────────────
        private void ToggleEquip()
        {
            _equipped = !_equipped;
            if (_equipped) DoEquip();
            else DoUnequip();
        }

        private void DoEquip()
        {
            BuildKatanaModel();
        }

        private void DoUnequip()
        {
            if (_katanaRoot != null)
                _katanaRoot.SetActive(false);
            StopAllCoroutines();
            _onCooldown = false;
        }

        // ── 3: tap vs hold ───────────────────────────────────────────────────
        private void OnAttackPressed()
        {
            if (!_equipped) return;
            _key5DownAt = Time.time;
        }

        private void OnAttackReleased()
        {
            if (!_equipped) return;
            if (_key5DownAt < 0f) return;

            float held = Time.time - _key5DownAt;
            _key5DownAt = -1f;

            if (held < chargeThreshold)
                StartCoroutine(RapidComboRoutine());
            else
                StartCoroutine(ChargedAttackRoutine());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Attack routines (sin cooldowns)
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator RapidComboRoutine()
        {
            for (int i = 0; i < 3; i++)
            {
                ExecuteSlash(rapidDamagePerHit, false, i);
                yield return new WaitForSeconds(comboWindow);
            }
        }

        private IEnumerator ChargedAttackRoutine()
        {
            // Brief charge visual
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                if (_bladeTrail != null)
                    _bladeTrail.startColor = Color.Lerp(trailColorA, chargedTrailColorA, t / 0.35f);
                yield return null;
            }

            ExecuteSlash(chargedDamage, true);
        }

        private void ExecuteSlash(float damage, bool isCharged, int comboIndex = 0)
        {
            // Validar combatant antes de usar
            if (_combatant == null)
            {
                return;
            }
            
            // Trigger animator if has matching parameters
            if (_animator != null)
            {
                bool hasAtk2 = false;
                foreach (var p in _animator.parameters)
                    if (p.name == "Attack2") { hasAtk2 = true; break; }

                _animator.SetTrigger(isCharged && hasAtk2 ? HASH_ATTACK2 : HASH_ATTACK);
            }

            HitArc(damage, isCharged);
            StartCoroutine(SlashVFXRoutine(isCharged, comboIndex));
            StartCoroutine(ShakeCamera(isCharged ? 0.18f : 0.07f, isCharged ? 0.2f : 0.1f));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Hitscan arc
        // ─────────────────────────────────────────────────────────────────────
        private void HitArc(float damage, bool isCharged)
        {
            if (_combatant == null) return;
            
            float halfAngle = isCharged ? 60f : 35f;
            var hits = Physics.OverlapSphere(transform.position, attackRange);

            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;

                var target = col.GetComponent<ArenaCombatant>();
                if (target == null || !target.IsAlive) continue;
                if (target.teamId == _combatant.teamId) continue;

                Vector3 toTarget = col.transform.position - transform.position;
                if (Vector3.Angle(transform.forward, toTarget) > halfAngle) continue;

                float finalDmg = damage * _combatant.damageMultiplier;
                target.TakeDamage(finalDmg, gameObject, DamageType.Ice);

                if (isCharged && target != null && target.IsAlive)
                    target.ApplyKnockback((toTarget.normalized + Vector3.up * 0.4f) * 9f);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Visual FX
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator SlashVFXRoutine(bool isCharged, int comboIndex)
        {
            // Alternate left/right for rapid hits
            float slashDir = (comboIndex % 2 == 0) ? 1f : -1f;
            float dur      = isCharged ? 0.22f : 0.12f;
            float angle    = isCharged ? 130f  : 75f;

            if (_bladeTrail != null)
            {
                _bladeTrail.emitting   = true;
                _bladeTrail.startColor = isCharged ? chargedTrailColorA : trailColorA;
                _bladeTrail.endColor   = isCharged ? chargedTrailColorB : trailColorB;
                _bladeTrail.startWidth = isCharged ? 0.14f : 0.07f;
                _bladeTrail.time       = isCharged ? 0.40f : 0.18f;
            }

            Quaternion startRot = _katanaRoot != null
                ? _katanaRoot.transform.localRotation
                : Quaternion.identity;

            // Draw arc glyph
            SpawnSlashArc(isCharged, slashDir);

            float el = 0f;
            while (el < dur)
            {
                float p = el / dur;
                float a = Mathf.Sin(p * Mathf.PI) * angle;
                if (_katanaRoot != null)
                    _katanaRoot.transform.localRotation = startRot * Quaternion.Euler(slashDir * a, 0, 0);
                el += Time.deltaTime;
                yield return null;
            }

            if (_katanaRoot != null)
                _katanaRoot.transform.localRotation = startRot;

            if (_bladeTrail != null)
            {
                _bladeTrail.emitting = false;
                yield return new WaitForSeconds(_bladeTrail.time);
                _bladeTrail.Clear();
            }
        }

        private void SpawnSlashArc(bool isCharged, float dir)
        {
            var go = new GameObject("SlashArc_FX");
            go.transform.position = transform.position + transform.forward * 0.6f + Vector3.up * 0.9f;
            go.transform.rotation = transform.rotation;

            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 18;
            lr.startWidth    = isCharged ? 0.18f : 0.09f;
            lr.endWidth      = 0.01f;
            lr.useWorldSpace = true;

            // Usar material cacheado y reutilizable - crear solo uno por instancia
            if (_cachedDefaultMaterial == null)
                _cachedDefaultMaterial = GetCachedDefaultMaterial();
            
            var mat = _cachedDefaultMaterial;
            if (mat == null || mat.shader.name.Contains("Error"))
            {
                if (_cachedStandardMaterial == null)
                    _cachedStandardMaterial = GetCachedStandardMaterial();
                mat = _cachedStandardMaterial;
            }
            
            // Reutilizar el mismo material pero cambiar el color vía property block para no crear instancias
            var instanceMat = mat;
            instanceMat.color = isCharged
                ? new Color(0.7f, 0.3f, 1f, 0.9f)
                : new Color(0.5f, 0.9f, 1f, 0.85f);
            lr.material = instanceMat;

            float halfAngle = isCharged ? 55f : 30f;
            float radius    = attackRange * 0.9f;

            for (int i = 0; i < 18; i++)
            {
                float t   = (float)i / 17f;
                float ang = dir * (t * halfAngle * 2f - halfAngle);
                Vector3 d = Quaternion.Euler(0, ang, 0) * transform.forward;
                lr.SetPosition(i, transform.position + Vector3.up * 0.9f + d * radius);
            }

            Destroy(go, 0.22f);
        }

        private IEnumerator ShakeCamera(float magnitude, float duration)
        {
            if (_cam == null) yield break;
            var orig = _cam.transform.localPosition;
            float el = 0f;
            while (el < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                _cam.transform.localPosition = orig + new Vector3(x, y, 0f);
                el += Time.deltaTime;
                yield return null;
            }
            _cam.transform.localPosition = orig;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Model construction - Loads real ice_sword_by_get3dmodels.glb
        // ─────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private const string KATANA_MODEL_PATH = "Assets/Models/Weapons/ice_sword_by_get3dmodels.glb";
#else
        private const string KATANA_MODEL_PATH = "Models/Weapons/ice_sword_by_get3dmodels";
#endif

        private void BuildKatanaModel()
        {
            DestroyKatanaModel();

            if (_handBone == null)
            {
                return;
            }

            _katanaRoot = new GameObject("KatanaRoot");
            _katanaRoot.transform.SetParent(_handBone, false);

            // Apply configured positioning
            _katanaRoot.transform.localPosition = handPositionOffset;
            _katanaRoot.transform.localRotation = Quaternion.Euler(handRotationOffset);
            _katanaRoot.transform.localScale = Vector3.one * modelScale;

            // Try to load the imported GLB prefab
            GameObject loadedModel = null;
            
            // Intentar cargar desde Resources en runtime
            loadedModel = Resources.Load<GameObject>("Models/Weapons/ice_sword_by_get3dmodels");
            
#if UNITY_EDITOR
            // Fallback a AssetDatabase en editor
            if (loadedModel == null)
            {
                loadedModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(KATANA_MODEL_PATH);
                if (loadedModel == null)
                {
                    // Try without .glb extension
                    loadedModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Weapons/ice_sword_by_get3dmodels");
                }
            }
#endif
            if (loadedModel != null)
            {
                // Instantiate the imported model inside KatanaRoot
                var modelInstance = Instantiate(loadedModel, _katanaRoot.transform);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                
                // Reset scale to 1 since we apply scale on parent
                modelInstance.transform.localScale = Vector3.one;
                modelInstance.name = "IceSwordModel";
            }
            else
            {
                // Fallback to procedural mesh if GLB not found
                BuildIceSwordMesh(_katanaRoot.transform);
            }
        }

        private Bounds CalculateBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
            
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private TrailRenderer FindOrCreateBladeTrail()
        {
            // Try to find existing trail or create one
            Transform bladeTip = DeepFind(_katanaRoot.transform, "BladeTip");
            if (bladeTip == null)
            {
                // Search for common blade tip names in the model
                string[] tipNames = { "tip", "Tip", "blade_tip", "Blade_Tip", "point", "end" };
                foreach (var name in tipNames)
                {
                    bladeTip = DeepFind(_katanaRoot.transform, name);
                    if (bladeTip != null) break;
                }
            }

            if (bladeTip == null)
            {
                // Create a trail at the end of the blade (estimated position)
                bladeTip = new GameObject("BladeTip").transform;
                bladeTip.SetParent(_katanaRoot.transform, false);
                bladeTip.localPosition = new Vector3(0f, 1.1f, 0f); // approximate blade tip
            }

            var trail = bladeTip.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = bladeTip.gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.18f;
                trail.startWidth = 0.07f;
                trail.endWidth = 0.005f;
                trail.startColor = trailColorA;
                trail.endColor = trailColorB;
                trail.emitting = false;
                trail.material = GetCachedDefaultMaterial();
            }

            return trail;
        }

        private void BuildIceSwordMesh(Transform parent)
        {
            // Blade (thin elongated cube, ice-blue)
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.SetParent(parent, false);
            blade.transform.localScale    = new Vector3(0.06f, 1.0f, 0.035f);
            blade.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            Destroy(blade.GetComponent<Collider>());

            var bladeMat = GetCachedURPMaterial();
            if (bladeMat == null || bladeMat.shader.name.Contains("Error"))
                bladeMat = GetCachedStandardMaterial();
            if (bladeMat == null || bladeMat.shader.name.Contains("Error"))
                bladeMat = GetCachedDiffuseMaterial();
            bladeMat.color = new Color(0.55f, 0.88f, 1f, 1f);
            blade.GetComponent<MeshRenderer>().material = bladeMat;

            // Guard
            var guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "Guard";
            guard.transform.SetParent(parent, false);
            guard.transform.localScale    = new Vector3(0.26f, 0.045f, 0.045f);
            guard.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            Destroy(guard.GetComponent<Collider>());
            var guardMat = new Material(bladeMat) { color = new Color(0.75f, 0.95f, 1f) };
            guard.GetComponent<MeshRenderer>().material = guardMat;

            // Handle
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Handle";
            handle.transform.SetParent(parent, false);
            handle.transform.localScale    = new Vector3(0.05f, 0.13f, 0.05f);
            handle.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            Destroy(handle.GetComponent<Collider>());
            var handleMat = new Material(bladeMat) { color = new Color(0.18f, 0.22f, 0.40f) };
            handle.GetComponent<MeshRenderer>().material = handleMat;

            // Pommel
            var pommel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pommel.name = "Pommel";
            pommel.transform.SetParent(parent, false);
            pommel.transform.localScale    = new Vector3(0.08f, 0.08f, 0.08f);
            pommel.transform.localPosition = new Vector3(0f, -0.30f, 0f);
            Destroy(pommel.GetComponent<Collider>());
            pommel.GetComponent<MeshRenderer>().material = guardMat;
        }

        private void DestroyKatanaModel()
        {
            if (_katanaRoot != null)
            {
                Destroy(_katanaRoot);
                _katanaRoot = null;
                _bladeTrail = null;
            }
        }

        private void OnDestroy() => DestroyKatanaModel();

        // ── Public status ─────────────────────────────────────────────────────
        public bool IsOnCooldown => _onCooldown;
    }
}

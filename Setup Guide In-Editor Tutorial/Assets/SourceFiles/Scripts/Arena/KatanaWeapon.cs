using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ArenaEnhanced
{
    /// <summary>
    /// Ice Katana weapon system for the player.
    /// K          -> equip / unequip
    /// 5 (tap)    -> 5-hit rapid samurai combo
    /// 5 (hold)   -> charged wide slash (more damage + ice knockback)
    ///
    /// Visual simulation: TrailRenderer on blade tip + LineRenderer slash arc + camera shake.
    /// No external animation clips required.
    /// </summary>
    [RequireComponent(typeof(ArenaCombatant))]
    public class KatanaWeapon : MonoBehaviour
    {
        // ── Combat stats ─────────────────────────────────────────────────────
        [Header("Katana Stats")]
        public float rapidDamagePerHit  = 18f;
        public float chargedDamage      = 90f;
        public float attackRange        = 2.8f;
        public float comboWindow        = 0.5f;      // seconds between combo hits
        public float chargeThreshold    = 0.45f;     // hold >= this -> charged attack
        public float cooldownAfterCombo = 1.1f;
        public float cooldownAfterCharge = 1.8f;

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

        // ── State ────────────────────────────────────────────────────────────
        private bool  _equipped   = false;
        private bool  _onCooldown = false;
        private float _key5DownAt = -1f;     // timestamp when 5 was first pressed

        // Animator trigger hashes
        private static readonly int HASH_ATTACK  = Animator.StringToHash("Attack");
        private static readonly int HASH_ATTACK2 = Animator.StringToHash("Attack2");

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _combatant = GetComponent<ArenaCombatant>();
            _animator  = GetComponentInChildren<Animator>();
            _cam       = Camera.main;
            LocateHandBone();
        }

        private void LocateHandBone()
        {
            string[] names = { "RightHand", "Hand_R", "mixamorig:RightHand", "r_hand" };
            foreach (string n in names)
            {
                var t = DeepFind(transform, n);
                if (t != null) { _handBone = t; return; }
            }
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

        // ─────────────────────────────────────────────────────────────────────
        private void Update()
        {
            PollEquipKey();
            if (_equipped && !_onCooldown) PollAttackKey();
        }

        // ── K: toggle equip ──────────────────────────────────────────────────
        private void PollEquipKey()
        {
            bool pressed = false;
#if ENABLE_INPUT_SYSTEM
            pressed = Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame;
#else
            pressed = Input.GetKeyDown(KeyCode.K);
#endif
            if (!pressed) return;

            _equipped = !_equipped;
            if (_equipped) DoEquip();
            else DoUnequip();
        }

        private void DoEquip()
        {
            BuildKatanaModel();
            Debug.Log("[Katana] EQUIPADA — [5] rapido = combo, [5] hold = cargado, [K] = guardar");
        }

        private void DoUnequip()
        {
            DestroyKatanaModel();
            Debug.Log("[Katana] GUARDADA");
        }

        // ── 5: tap vs hold ───────────────────────────────────────────────────
        private void PollAttackKey()
        {
            bool down = false, up = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                down = kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame;
                up   = kb.digit4Key.wasReleasedThisFrame || kb.numpad4Key.wasReleasedThisFrame;
            }
#else
            down = Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
            up   = Input.GetKeyUp(KeyCode.Alpha4)   || Input.GetKeyUp(KeyCode.Keypad4);
#endif

            if (down) _key5DownAt = Time.time;

            if (up && _key5DownAt >= 0f)
            {
                float held = Time.time - _key5DownAt;
                _key5DownAt = -1f;

                if (held < chargeThreshold)
                    StartCoroutine(RapidComboRoutine());
                else
                    StartCoroutine(ChargedAttackRoutine());
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Attack routines
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator RapidComboRoutine()
        {
            _onCooldown = true;
            for (int i = 0; i < 5; i++)
            {
                ExecuteSlash(rapidDamagePerHit, false, i);
                yield return new WaitForSeconds(comboWindow);
            }
            yield return new WaitForSeconds(cooldownAfterCombo);
            _onCooldown = false;
        }

        private IEnumerator ChargedAttackRoutine()
        {
            _onCooldown = true;

            // Brief charge visual
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                if (_bladeTrail != null)
                    _bladeTrail.startColor = Color.Lerp(trailColorA, chargedTrailColorA, t / 0.35f);
                yield return null;
            }

            ExecuteSlash(chargedDamage, true, 0);

            yield return new WaitForSeconds(cooldownAfterCharge);
            _onCooldown = false;
        }

        private void ExecuteSlash(float damage, bool isCharged, int comboIndex)
        {
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
                target.TakeDamage(finalDmg, gameObject);

                if (isCharged)
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

            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = isCharged
                ? new Color(0.7f, 0.3f, 1f, 0.9f)
                : new Color(0.5f, 0.9f, 1f, 0.85f);
            lr.material = mat;

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
        // Model construction
        // ─────────────────────────────────────────────────────────────────────
        private void BuildKatanaModel()
        {
            DestroyKatanaModel();

            _katanaRoot = new GameObject("KatanaRoot");
            _katanaRoot.transform.SetParent(_handBone, false);
            _katanaRoot.transform.localPosition = new Vector3(0.12f, -0.015f, 0.08f);
            _katanaRoot.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            _katanaRoot.transform.localScale    = Vector3.one * 0.15f;

            // Try to load the GLB from Resources (Unity imports GLB as Prefab)
            // The asset is at Assets/Models/Weapons/ice_sword_by_get3dmodels.glb
            // We can't load it via Resources.Load unless it's inside a Resources folder,
            // so we build a stylised placeholder that looks like an ice sword.
            BuildIceSwordMesh(_katanaRoot.transform);

            // Trail on blade tip
            var tip = new GameObject("BladeTip");
            tip.transform.SetParent(_katanaRoot.transform, false);
            tip.transform.localPosition = new Vector3(0f, 1.1f, 0f); // tip of the blade

            _bladeTrail            = tip.AddComponent<TrailRenderer>();
            _bladeTrail.time       = 0.18f;
            _bladeTrail.startWidth = 0.07f;
            _bladeTrail.endWidth   = 0.005f;
            _bladeTrail.startColor = trailColorA;
            _bladeTrail.endColor   = trailColorB;
            _bladeTrail.emitting   = false;
            _bladeTrail.material   = new Material(Shader.Find("Sprites/Default"));
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

            var bladeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (bladeMat == null || bladeMat.shader.name.Contains("Error"))
                bladeMat = new Material(Shader.Find("Standard"));
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
        public bool IsEquipped   => _equipped;
        public bool IsOnCooldown => _onCooldown;
    }
}

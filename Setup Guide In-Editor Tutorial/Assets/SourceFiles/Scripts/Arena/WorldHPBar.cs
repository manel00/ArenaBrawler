using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Floating billboard HP bar shown above non-player combatants.
    /// Creates its own 3D quads in world space - no Canvas needed.
    /// </summary>
    public class WorldHPBar : MonoBehaviour
    {
        public ArenaCombatant combatant;
        public float heightOffset = 2.4f;
        public string label = "";

        private Transform _barRoot;
        private Renderer _fillRenderer;
        private Transform _fillTransform;
        private Camera _cam;

        private static Material _bgMat;
        private static Material _fillMat;

        private void Start()
        {
            _cam = CameraCache.Main;

            // Shared materials (created once)
            if (_bgMat == null)
            {
                _bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                _bgMat.color = new Color(0.15f, 0f, 0f);
            }
            if (_fillMat == null)
            {
                _fillMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                _fillMat.color = new Color(0.9f, 0.2f, 0.2f);
            }

            // Root that will billboard-face the camera
            var rootGo = new GameObject("WorldHPBar_Root");
            rootGo.transform.SetParent(transform);
            rootGo.transform.localPosition = Vector3.up * heightOffset;
            _barRoot = rootGo.transform;

            // Background bar (slightly wider)
            var bg = new GameObject("HPBar_BG");
            bg.transform.SetParent(_barRoot);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(1.1f, 0.18f, 1f);
            var bgMf = bg.AddComponent<MeshFilter>();
            bgMf.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var bgMr = bg.AddComponent<MeshRenderer>();
            bgMr.material = _bgMat;

            // Foreground fill
            var fill = new GameObject("HPBar_Fill");
            fill.transform.SetParent(_barRoot);
            // Slightly in front of background to avoid z-fighting
            fill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            fill.transform.localScale = new Vector3(1f, 0.14f, 1f);
            var fillMf = fill.AddComponent<MeshFilter>();
            fillMf.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            _fillRenderer = fill.AddComponent<MeshRenderer>();
            _fillRenderer.material = new Material(_fillMat);
            _fillTransform = fill.transform;

            // Label (optional)
            if (!string.IsNullOrEmpty(label)) rootGo.name = $"WorldHPBar_{label}";
        }

        private void LateUpdate()
        {
            if (_barRoot == null) return;

            // Billboard: face camera
            if (_cam == null) _cam = CameraCache.Main;
            if (_cam != null)
                _barRoot.rotation = Quaternion.LookRotation(_barRoot.position - _cam.transform.position);

            if (combatant == null) return;

            float pct = combatant.maxHp > 0f ? Mathf.Clamp01(combatant.hp / combatant.maxHp) : 0f;

            // Scale fill on X axis and offset to stay left-aligned
            var s = _fillTransform.localScale;
            s.x = pct;
            _fillTransform.localScale = s;

            var p = _fillTransform.localPosition;
            p.x = -(1f - pct) * 0.5f;
            _fillTransform.localPosition = p;

            // Color: green -> yellow -> red
            _fillRenderer.material.color = Color.Lerp(new Color(0.9f, 0.15f, 0.1f), new Color(0.2f, 0.85f, 0.3f), pct);
        }
    }
}

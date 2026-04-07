using UnityEngine;
using TMPro;
using System.Collections;

namespace ArenaEnhanced
{
    public enum DamageType { Normal, Critical, Fire, Ice, Poison, Heal }

    public class DamagePopup : MonoBehaviour
    {
        [Header("Text Settings")]
        [SerializeField] private float normalFontSize = 4f;
        [SerializeField] private float criticalFontSize = 6f;
        [SerializeField] private float outlineWidth = 0.15f;

        [Header("Animation")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float fadeDuration = 1.2f;
        [SerializeField] private float drag = 1.5f;

        private TextMeshPro _textMesh;
        private float _fadeTimer;
        private Color _startColor;
        private Vector3 _moveVector;
        private DamageType _damageType;
        private float _verticalVelocity;

        // Colores por tipo de daño
        private static readonly Color ColorNormal = new Color32(255, 255, 255, 255);
        private static readonly Color ColorCritical = new Color32(255, 80, 0, 255);
        private static readonly Color ColorFire = new Color32(255, 100, 0, 255);
        private static readonly Color ColorIce = new Color32(0, 200, 255, 255);
        private static readonly Color ColorPoison = new Color32(150, 0, 200, 255);
        private static readonly Color ColorHeal = new Color32(0, 255, 100, 255);

        private static readonly Color OutlineNormal = Color.black;
        private static readonly Color OutlineCritical = new Color32(100, 0, 0, 255);

        public static void Create(Vector3 position, float damage, DamageType type = DamageType.Normal)
        {
            GameObject go = new GameObject("DamagePopup");
            go.transform.position = position + Vector3.up * 0.5f + Random.insideUnitSphere * 0.3f;
            var popup = go.AddComponent<DamagePopup>();
            popup.Setup(damage, type);
        }

        private void Awake()
        {
            _textMesh = gameObject.AddComponent<TextMeshPro>();
            _textMesh.alignment = TextAlignmentOptions.Center;
            _textMesh.fontSize = normalFontSize;
            _textMesh.sortingOrder = 500;
        }

        public void Setup(float damage, DamageType type = DamageType.Normal)
        {
            _damageType = type;
            _fadeTimer = fadeDuration;

            // Formato del texto
            bool isCrit = type == DamageType.Critical;
            bool isHeal = type == DamageType.Heal;

            if (damage < 1f && damage > 0 && !isHeal)
                _textMesh.text = damage.ToString("F1");
            else
                _textMesh.text = Mathf.Round(damage).ToString();

            // Prefijo para curación
            if (isHeal)
                _textMesh.text = "+" + _textMesh.text;

            // Configurar color y outline según tipo
            SetupVisuals(type, isCrit);

            // Movimiento inicial
            _moveVector = new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(-0.2f, 0.2f)) * moveSpeed;
            _verticalVelocity = 3f;
        }

        private void SetupVisuals(DamageType type, bool isCrit)
        {
            // Color base
            _startColor = type switch
            {
                DamageType.Critical => ColorCritical,
                DamageType.Fire => ColorFire,
                DamageType.Ice => ColorIce,
                DamageType.Poison => ColorPoison,
                DamageType.Heal => ColorHeal,
                _ => ColorNormal
            };

            _textMesh.color = _startColor;

            // Outline
            _textMesh.outlineColor = isCrit ? OutlineCritical : OutlineNormal;
            _textMesh.outlineWidth = isCrit ? outlineWidth * 2 : outlineWidth;

            // Tamaño de fuente
            _textMesh.fontSize = isCrit ? criticalFontSize : normalFontSize;

            // Efecto de sacudida para críticos
            if (isCrit)
            {
                _textMesh.fontStyle = FontStyles.Bold;
                StartCoroutine(CriticalShake());
            }
        }

        private IEnumerator CriticalShake()
        {
            float shakeDuration = 0.3f;
            float elapsed = 0;
            Vector3 basePos = transform.localPosition;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float intensity = (1 - elapsed / shakeDuration) * 0.2f;
                transform.localPosition = basePos + Random.insideUnitSphere * intensity;
                yield return null;
            }
        }

        private void Update()
        {
            // Movimiento con gravedad
            _verticalVelocity -= 9.8f * Time.deltaTime;
            _moveVector.y = _verticalVelocity;

            transform.position += _moveVector * Time.deltaTime;
            _moveVector -= _moveVector * drag * Time.deltaTime;

            // Fade out
            _fadeTimer -= Time.deltaTime;
            if (_fadeTimer <= 0)
            {
                Destroy(gameObject);
                return;
            }

            // Alpha
            float alpha = _fadeTimer / fadeDuration;
            _textMesh.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);

            // Escala dinámica
            float scaleProgress = 1f - (_fadeTimer / fadeDuration);
            float scale = 1f + Mathf.Sin(scaleProgress * Mathf.PI) * 0.3f;
            if (_damageType == DamageType.Critical)
                scale *= 1.2f;

            transform.localScale = Vector3.one * scale;

            // Mirar a cámara
            FaceCamera();
        }

        private void FaceCamera()
        {
            if (CameraCache.Main != null)
            {
                transform.rotation = Quaternion.LookRotation(
                    transform.position - CameraCache.Main.transform.position);
            }
        }
    }
}

using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de damage numbers flotantes con estilo.
    /// Soporta críticos, colores por tipo de daño, y animaciones variadas.
    /// </summary>
    public class StylishDamageNumbers : MonoBehaviour
    {
        public static StylishDamageNumbers Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private int poolSize = 30;

        [Header("Animation Settings")]
        [Tooltip("Duración de la animación")]
        [SerializeField] private float animationDuration = 1.2f;
        
        [Tooltip("Altura máxima que sube el número")]
        [SerializeField] private float floatHeight = 2.5f;
        
        [Tooltip("Velocidad de separación en hits múltiples")]
        [SerializeField] private float spreadSpeed = 0.8f;

        [Header("Styling")]
        [SerializeField] private Color normalDamageColor = new Color(1f, 0.3f, 0.1f);
        [SerializeField] private Color criticalDamageColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color healColor = new Color(0.2f, 0.9f, 0.3f);
        [SerializeField] private Color poisonColor = new Color(0.6f, 0.2f, 0.8f);
        [SerializeField] private Color fireColor = new Color(1f, 0.5f, 0.1f);
        
        [Tooltip("Outline del texto")]
        [SerializeField] private float outlineThickness = 0.15f;
        [SerializeField] private Color outlineColor = Color.black;

        private Queue<GameObject> _damageNumberPool = new Queue<GameObject>();
        private List<ActiveDamageNumber> _activeNumbers = new List<ActiveDamageNumber>();
        private Transform _canvasTransform;

        private struct ActiveDamageNumber
        {
            public GameObject gameObject;
            public TextMeshProUGUI text;
            public float startTime;
            public Vector3 startPosition;
            public Vector3 floatDirection;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Crear canvas si no existe
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("DamageNumbersCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 1000;
            }
            _canvasTransform = canvas.transform;

            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject go = CreateDamageNumberObject();
                _damageNumberPool.Enqueue(go);
            }
        }

        private GameObject CreateDamageNumberObject()
        {
            GameObject go = new GameObject("DamageNumber");
            go.transform.SetParent(_canvasTransform);
            go.SetActive(false);

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = 4;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            
            // Configurar outline
            text.outlineColor = outlineColor;
            text.outlineWidth = outlineThickness;

            return go;
        }

        /// <summary>
        /// Muestra un número de daño flotante
        /// </summary>
        public void ShowDamage(float damage, Vector3 worldPosition, DamageType type = DamageType.Normal, bool isCritical = false)
        {
            GameObject go = GetFromPool();
            if (go == null) return;

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            
            // Configurar texto y color
            string damageText = Mathf.RoundToInt(damage).ToString();
            if (isCritical) damageText += "!";
            text.text = damageText;
            text.color = GetColorForType(type, isCritical);

            // Configurar tamaño según importancia
            text.fontSize = isCritical ? 6 : 4;

            // Posición en mundo
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            go.transform.position = screenPos + Vector3.up * 50f;
            go.transform.localScale = Vector3.one;

            // Dirección aleatoria para separación
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 floatDir = new Vector3(randomDir.x, 1f, 0f);

            ActiveDamageNumber active = new ActiveDamageNumber
            {
                gameObject = go,
                text = text,
                startTime = Time.time,
                startPosition = go.transform.position,
                floatDirection = floatDir
            };

            _activeNumbers.Add(active);
            StartCoroutine(AnimateDamageNumber(active));
        }

        /// <summary>
        /// Muestra texto personalizado (para estados, curaciones, etc.)
        /// </summary>
        public void ShowText(string text, Vector3 worldPosition, Color color, float scale = 1f)
        {
            GameObject go = GetFromPool();
            if (go == null) return;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = 3.5f * scale;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            go.transform.position = screenPos + Vector3.up * 50f;

            ActiveDamageNumber active = new ActiveDamageNumber
            {
                gameObject = go,
                text = tmp,
                startTime = Time.time,
                startPosition = go.transform.position,
                floatDirection = Vector3.up
            };

            _activeNumbers.Add(active);
            StartCoroutine(AnimateDamageNumber(active));
        }

        private IEnumerator AnimateDamageNumber(ActiveDamageNumber active)
        {
            float elapsed = 0f;
            Vector3 startPos = active.startPosition;

            // Animación de subida
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;

                // Curva de animación: subida rápida al inicio, lenta al final
                float heightT = 1f - Mathf.Pow(1f - t, 3f);
                
                // Movimiento vertical
                Vector3 currentPos = startPos + active.floatDirection * floatHeight * heightT * 100f;
                
                // Separación horizontal
                currentPos.x += active.floatDirection.x * spreadSpeed * t * 50f;

                active.gameObject.transform.position = currentPos;

                // Fade out al final
                if (t > 0.7f)
                {
                    float fadeT = (t - 0.7f) / 0.3f;
                    Color c = active.text.color;
                    c.a = 1f - fadeT;
                    active.text.color = c;
                }

                // Scale effect
                float scaleT = Mathf.Sin(t * Mathf.PI);
                float scale = 1f + scaleT * 0.2f;
                active.gameObject.transform.localScale = Vector3.one * scale;

                yield return null;
            }

            // Retornar al pool
            ReturnToPool(active.gameObject);
            _activeNumbers.Remove(active);
        }

        private GameObject GetFromPool()
        {
            if (_damageNumberPool.Count > 0)
            {
                GameObject go = _damageNumberPool.Dequeue();
                go.SetActive(true);
                return go;
            }
            
            // Crear nuevo si pool está vacío
            return CreateDamageNumberObject();
        }

        private void ReturnToPool(GameObject go)
        {
            go.SetActive(false);
            
            // Resetear color alpha
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                Color c = text.color;
                c.a = 1f;
                text.color = c;
            }

            _damageNumberPool.Enqueue(go);
        }

        private Color GetColorForType(DamageType type, bool isCritical)
        {
            if (isCritical) return criticalDamageColor;
            
            switch (type)
            {
                case DamageType.Heal:
                    return healColor;
                case DamageType.Poison:
                    return poisonColor;
                case DamageType.Fire:
                    return fireColor;
                case DamageType.Critical:
                    return criticalDamageColor;
                default:
                    return normalDamageColor;
            }
        }
    }

    public enum DamageType
    {
        Normal,
        Critical,
        Heal,
        Poison,
        Fire,
        Ice,
        Electric
    }
}

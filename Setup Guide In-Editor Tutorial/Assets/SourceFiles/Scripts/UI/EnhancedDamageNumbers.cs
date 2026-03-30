using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de daño flotante mejorado con efectos visuales premium.
    /// Muestra números de daño con animaciones, colores por tipo, y critical hits.
    /// </summary>
    public class EnhancedDamageNumbers : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private int poolSize = 30;
        [SerializeField] private GameObject damageNumberPrefab;
        
        [Header("Animation Settings")]
        [SerializeField] private float floatDuration = 1.2f;
        [SerializeField] private float floatHeight = 2f;
        [SerializeField] private float spreadRange = 0.5f;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalDamageColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color criticalDamageColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color healingColor = new Color(0.2f, 1f, 0.4f);
        [SerializeField] private Color poisonDamageColor = new Color(0.6f, 0.2f, 1f);
        [SerializeField] private Color iceDamageColor = new Color(0.2f, 0.8f, 1f);
        
        [Header("Critical Hit Settings")]
        [SerializeField] private float criticalShakeIntensity = 5f;
        
        public static EnhancedDamageNumbers Instance { get; private set; }
        
        private Queue<GameObject> _pool = new Queue<GameObject>();
        private Canvas _worldCanvas;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            InitializePool();
            CreateWorldCanvas();
        }
        
        private void InitializePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                CreatePooledObject();
            }
        }
        
        private void CreatePooledObject()
        {
            GameObject go = Instantiate(damageNumberPrefab, transform);
            go.SetActive(false);
            _pool.Enqueue(go);
        }
        
        private void CreateWorldCanvas()
        {
            GameObject canvasGo = new GameObject("DamageNumbersCanvas");
            canvasGo.transform.SetParent(transform);
            
            _worldCanvas = canvasGo.AddComponent<Canvas>();
            _worldCanvas.renderMode = RenderMode.WorldSpace;
            _worldCanvas.sortingOrder = 100;
            
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
        }
        
        /// <summary>
        /// Muestra daño flotante en posición mundial
        /// </summary>
        public void ShowDamage(Vector3 worldPosition, float damage, bool isCritical = false, DamageType type = DamageType.Normal)
        {
            GameObject damageObj = GetFromPool();
            if (damageObj == null) return;
            
            // Posición con variación aleatoria
            Vector3 offset = new Vector3(
                Random.Range(-spreadRange, spreadRange),
                0,
                Random.Range(-spreadRange, spreadRange)
            );
            damageObj.transform.position = worldPosition + offset + Vector3.up * 0.5f;
            
            // Configurar visual
            TextMeshProUGUI textMesh = damageObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textMesh != null)
            {
                textMesh.text = Mathf.Ceil(damage).ToString();
                textMesh.color = GetColorForType(type, isCritical);
            }
            
            // Animar
            StartCoroutine(AnimateDamageNumber(damageObj, isCritical));
        }
        
        /// <summary>
        /// Muestra curación flotante
        /// </summary>
        public void ShowHealing(Vector3 worldPosition, float amount)
        {
            ShowDamage(worldPosition, amount, false, DamageType.Healing);
        }
        
        private GameObject GetFromPool()
        {
            if (_pool.Count == 0)
            {
                CreatePooledObject();
            }
            
            GameObject obj = _pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        
        private void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
        
        private Color GetColorForType(DamageType type, bool isCritical)
        {
            if (isCritical) return criticalDamageColor;
            
            return type switch
            {
                DamageType.Healing => healingColor,
                DamageType.Poison => poisonDamageColor,
                DamageType.Ice => iceDamageColor,
                _ => normalDamageColor
            };
        }
        
        private IEnumerator AnimateDamageNumber(GameObject obj, bool isCritical)
        {
            Vector3 startPos = obj.transform.position;
            Vector3 endPos = startPos + Vector3.up * floatHeight;
            float elapsed = 0f;
            
            // Shake inicial para critical hits
            if (isCritical)
            {
                float shakeDuration = 0.2f;
                float shakeElapsed = 0f;
                
                while (shakeElapsed < shakeDuration)
                {
                    shakeElapsed += Time.deltaTime;
                    Vector3 shake = Random.insideUnitSphere * criticalShakeIntensity * 0.1f;
                    shake.y = 0;
                    obj.transform.position = startPos + shake;
                    yield return null;
                }
            }
            
            // Animación de flotación
            while (elapsed < floatDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / floatDuration;
                
                // Curva de animación suave
                float smoothT = 1 - Mathf.Pow(1 - t, 3);
                obj.transform.position = Vector3.Lerp(startPos, endPos, smoothT);
                
                // Fade out al final
                if (t > 0.7f)
                {
                    float alpha = 1 - ((t - 0.7f) / 0.3f);
                    // Aplicar fade a textos
                    ApplyAlpha(obj, alpha);
                }
                
                yield return null;
            }
            
            ReturnToPool(obj);
        }
        
        private void ApplyAlpha(GameObject obj, float alpha)
        {
            TextMeshProUGUI[] texts = obj.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                Color c = text.color;
                c.a = alpha;
                text.color = c;
            }
        }
    }
    
    public enum DamageType
    {
        Normal,
        Critical,
        Healing,
        Poison,
        Ice,
        Fire,
        Electric
    }
}

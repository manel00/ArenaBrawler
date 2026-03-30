using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de transiciones animadas entre menus/paneles de UI.
    /// Soporta fade, slide, scale, y combinaciones.
    /// </summary>
    public class MenuTransitionManager : MonoBehaviour
    {
        public static MenuTransitionManager Instance { get; private set; }

        [Header("Transition Settings")]
        [Tooltip("Duración por defecto de las transiciones")]
        [SerializeField] private float defaultDuration = 0.4f;
        
        [Tooltip("Curva de animación por defecto")]
        [SerializeField] private AnimationCurve defaultCurve;

        [Header("Preset Animations")]
        [SerializeField] private TransitionPreset fadeInPreset;
        [SerializeField] private TransitionPreset slideFromLeftPreset;
        [SerializeField] private TransitionPreset slideFromRightPreset;
        [SerializeField] private TransitionPreset slideFromBottomPreset;
        [SerializeField] private TransitionPreset scaleUpPreset;
        [SerializeField] private TransitionPreset scaleBouncePreset;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Inicializar curva por defecto si es null
            if (defaultCurve == null || defaultCurve.length == 0)
            {
                defaultCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
        }

        /// <summary>
        /// Muestra un panel con transición animada
        /// </summary>
        public void ShowPanel(RectTransform panel, TransitionType type = TransitionType.Fade, 
            float? duration = null, System.Action onComplete = null)
        {
            if (panel == null) return;
            
            StartCoroutine(AnimatePanelIn(panel, type, duration ?? defaultDuration, onComplete));
        }

        /// <summary>
        /// Oculta un panel con transición animada
        /// </summary>
        public void HidePanel(RectTransform panel, TransitionType type = TransitionType.Fade,
            float? duration = null, System.Action onComplete = null)
        {
            if (panel == null) return;
            
            StartCoroutine(AnimatePanelOut(panel, type, duration ?? defaultDuration, onComplete));
        }

        /// <summary>
        /// Transición entre dos paneles (crossfade)
        /// </summary>
        public void SwitchPanels(RectTransform currentPanel, RectTransform newPanel,
            TransitionType hideType = TransitionType.SlideLeft,
            TransitionType showType = TransitionType.SlideFromRight,
            float? duration = null)
        {
            float dur = duration ?? defaultDuration;
            
            HidePanel(currentPanel, hideType, dur, () => {
                ShowPanel(newPanel, showType, dur);
            });
        }

        private IEnumerator AnimatePanelIn(RectTransform panel, TransitionType type, float duration, System.Action onComplete)
        {
            panel.gameObject.SetActive(true);
            
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            Vector2 originalAnchoredPos = panel.anchoredPosition;
            Vector3 originalScale = panel.localScale;

            // Setup inicial según tipo
            SetupInitialState(panel, type, canvasGroup);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curvedT = defaultCurve.Evaluate(t);

                ApplyTransition(panel, type, canvasGroup, curvedT, originalAnchoredPos, originalScale, true);
                yield return null;
            }

            // Estado final
            ApplyTransition(panel, type, canvasGroup, 1f, originalAnchoredPos, originalScale, true);
            onComplete?.Invoke();
        }

        private IEnumerator AnimatePanelOut(RectTransform panel, TransitionType type, float duration, System.Action onComplete)
        {
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();

            Vector2 originalAnchoredPos = panel.anchoredPosition;
            Vector3 originalScale = panel.localScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curvedT = defaultCurve.Evaluate(t);

                ApplyTransition(panel, type, canvasGroup, curvedT, originalAnchoredPos, originalScale, false);
                yield return null;
            }

            panel.gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        private void SetupInitialState(RectTransform panel, TransitionType type, CanvasGroup canvasGroup)
        {
            switch (type)
            {
                case TransitionType.Fade:
                    canvasGroup.alpha = 0f;
                    break;
                    
                case TransitionType.SlideFromLeft:
                    panel.anchoredPosition = new Vector2(-Screen.width, panel.anchoredPosition.y);
                    break;
                    
                case TransitionType.SlideFromRight:
                    panel.anchoredPosition = new Vector2(Screen.width, panel.anchoredPosition.y);
                    break;
                    
                case TransitionType.SlideFromBottom:
                    panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, -Screen.height);
                    break;
                    
                case TransitionType.ScaleUp:
                case TransitionType.ScaleBounce:
                    panel.localScale = Vector3.zero;
                    break;
            }
        }

        private void ApplyTransition(RectTransform panel, TransitionType type, CanvasGroup canvasGroup,
            float t, Vector2 originalPos, Vector3 originalScale, bool isIn)
        {
            float invertT = isIn ? t : 1f - t;

            switch (type)
            {
                case TransitionType.Fade:
                    canvasGroup.alpha = invertT;
                    break;
                    
                case TransitionType.SlideFromLeft:
                case TransitionType.SlideLeft:
                    float fromLeft = isIn ? Mathf.Lerp(-Screen.width, originalPos.x, t) 
                                          : Mathf.Lerp(originalPos.x, -Screen.width, t);
                    panel.anchoredPosition = new Vector2(fromLeft, originalPos.y);
                    break;
                    
                case TransitionType.SlideFromRight:
                case TransitionType.SlideRight:
                    float fromRight = isIn ? Mathf.Lerp(Screen.width, originalPos.x, t)
                                           : Mathf.Lerp(originalPos.x, Screen.width, t);
                    panel.anchoredPosition = new Vector2(fromRight, originalPos.y);
                    break;
                    
                case TransitionType.SlideFromBottom:
                case TransitionType.SlideBottom:
                    float fromBottom = isIn ? Mathf.Lerp(-Screen.height, originalPos.y, t)
                                            : Mathf.Lerp(originalPos.y, -Screen.height, t);
                    panel.anchoredPosition = new Vector2(originalPos.x, fromBottom);
                    break;
                    
                case TransitionType.ScaleUp:
                    panel.localScale = Vector3.Lerp(Vector3.zero, originalScale, invertT);
                    break;
                    
                case TransitionType.ScaleBounce:
                    float bounceT = isIn ? BounceEaseOut(t) : t;
                    panel.localScale = Vector3.Lerp(Vector3.zero, originalScale, bounceT);
                    break;
            }
        }

        private float BounceEaseOut(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
                return n1 * t * t;
            else if (t < 2f / d1)
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            else if (t < 2.5f / d1)
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            else
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }

        /// <summary>
        /// Efecto de ripple al hacer click en botones
        /// </summary>
        public void ButtonClickFeedback(RectTransform button)
        {
            if (button == null) return;
            StartCoroutine(ButtonPulse(button));
        }

        private IEnumerator ButtonPulse(RectTransform button)
        {
            Vector3 originalScale = button.localScale;
            float duration = 0.15f;
            float elapsed = 0f;

            // Scale down
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                button.localScale = Vector3.Lerp(originalScale, originalScale * 0.95f, t);
                yield return null;
            }

            // Scale back
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                button.localScale = Vector3.Lerp(originalScale * 0.95f, originalScale, t);
                yield return null;
            }

            button.localScale = originalScale;
        }
    }

    public enum TransitionType
    {
        Fade,
        SlideFromLeft,
        SlideLeft,
        SlideFromRight,
        SlideRight,
        SlideFromBottom,
        SlideBottom,
        ScaleUp,
        ScaleBounce
    }

    [System.Serializable]
    public class TransitionPreset
    {
        public TransitionType type;
        public float duration;
        public AnimationCurve curve;
    }
}

using UnityEngine;
using System.Collections;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de efectos visuales premium para la arena.
    /// Gestiona partículas, shakes de cámara, y efectos de pantalla.
    /// </summary>
    public class VisualEffectsManager : MonoBehaviour
    {
        public static VisualEffectsManager Instance { get; private set; }
        
        [Header("Screen Effects")]
        [SerializeField] private Material screenFlashMaterial;
        [SerializeField] private float flashDuration = 0.1f;
        
        [Header("Camera Shake")]
        [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private float defaultShakeDuration = 0.3f;
        [SerializeField] private float defaultShakeIntensity = 0.3f;
        
        [Header("Slow Motion")]
        [SerializeField] private float slowMotionTimeScale = 0.3f;
        [SerializeField] private float slowMotionDuration = 0.5f;
        
        private Camera _mainCamera;
        private Vector3 _originalCameraPosition;
        private Coroutine _shakeCoroutine;
        private float _originalTimeScale;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                _originalCameraPosition = _mainCamera.transform.localPosition;
            }
        }
        
        /// <summary>
        /// Activa un flash de pantalla (daño recibido, impacto crítico)
        /// </summary>
        public void ScreenFlash(Color flashColor, float duration = -1f)
        {
            if (duration < 0) duration = flashDuration;
            StartCoroutine(ScreenFlashCoroutine(flashColor, duration));
        }
        
        /// <summary>
        /// Shake de cámara para impactos pesados
        /// </summary>
        public void CameraShake(float intensity = -1f, float duration = -1f)
        {
            if (_mainCamera == null) return;
            
            if (intensity < 0) intensity = defaultShakeIntensity;
            if (duration < 0) duration = defaultShakeDuration;
            
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _mainCamera.transform.localPosition = _originalCameraPosition;
            }
            
            _shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
        }
        
        /// <summary>
        /// Efecto de cámara lenta para momentos impactantes
        /// </summary>
        public void TriggerSlowMotion(float targetScale = -1f, float duration = -1f)
        {
            if (targetScale < 0) targetScale = slowMotionTimeScale;
            if (duration < 0) duration = slowMotionDuration;
            
            StartCoroutine(SlowMotionCoroutine(targetScale, duration));
        }
        
        /// <summary>
        /// Efecto de impacto combinado (shake + flash)
        /// </summary>
        public void ImpactEffect(float shakeIntensity, Color flashColor)
        {
            CameraShake(shakeIntensity);
            ScreenFlash(flashColor);
        }
        
        /// <summary>
        /// Efecto de hit stop (pausa breve en impacto)
        /// </summary>
        public void HitStop(float stopDuration)
        {
            StartCoroutine(HitStopCoroutine(stopDuration));
        }
        
        private IEnumerator ScreenFlashCoroutine(Color color, float duration)
        {
            // Implementación básica - puede expandirse con post-processing
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - (elapsed / duration);
                // Aquí se aplicaría el efecto visual
                yield return null;
            }
        }
        
        private IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveValue = shakeCurve.Evaluate(t);
                
                Vector3 shakeOffset = Random.insideUnitSphere * intensity * curveValue;
                shakeOffset.z = 0; // Mantener profundidad de cámara
                
                _mainCamera.transform.localPosition = _originalCameraPosition + shakeOffset;
                
                yield return null;
            }
            
            _mainCamera.transform.localPosition = _originalCameraPosition;
        }
        
        private IEnumerator SlowMotionCoroutine(float targetScale, float duration)
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = targetScale;
            Time.fixedDeltaTime = 0.02f * targetScale;
            
            yield return new WaitForSecondsRealtime(duration);
            
            Time.timeScale = _originalTimeScale;
            Time.fixedDeltaTime = 0.02f;
        }
        
        private IEnumerator HitStopCoroutine(float stopDuration)
        {
            Time.timeScale = 0.01f;
            yield return new WaitForSecondsRealtime(stopDuration);
            Time.timeScale = 1f;
        }
    }
}

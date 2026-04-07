using System.Collections;
using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Efecto de sacudida de cámara para explosiones e impactos.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [Header("Shake Settings")]
        [Tooltip("Amplitud máxima de la sacudida")]
        [SerializeField] private float maxShakeAmount = 0.3f;
        
        [Tooltip("Frecuencia de la sacudida")]
        [SerializeField] private float shakeFrequency = 15f;
        
        [Tooltip("Suavizado de retorno a posición original")]
        [SerializeField] private float smoothReturn = 5f;

        // Runtime state
        private Transform _cameraTransform;
        private Vector3 _originalPosition;
        private Coroutine _shakeCoroutine;
        private float _currentShakeAmount;

        private void Awake()
        {
            _cameraTransform = GetComponent<Transform>();
            _originalPosition = _cameraTransform.localPosition;
        }

        /// <summary>
        /// Inicia una sacudida de cámara.
        /// </summary>
        /// <param name="duration">Duración de la sacudida</param>
        /// <param name="intensity">Intensidad (0-1)</param>
        public void Shake(float duration, float intensity)
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }
            _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, intensity));
        }

        private IEnumerator ShakeRoutine(float duration, float intensity)
        {
            _currentShakeAmount = maxShakeAmount * Mathf.Clamp01(intensity);
            float elapsed = 0f;
            Vector3 initialPos = _cameraTransform.localPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damper = 1f - (elapsed / duration);
                
                // Movimiento aleatorio
                float x = Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f;
                float y = Mathf.PerlinNoise(0f, Time.time * shakeFrequency) * 2f - 1f;
                
                Vector3 shakeOffset = new Vector3(x, y, 0f) * _currentShakeAmount * damper;
                _cameraTransform.localPosition = initialPos + shakeOffset;

                yield return null;
            }

            // Suave retorno a posición original
            while (Vector3.Distance(_cameraTransform.localPosition, initialPos) > 0.01f)
            {
                _cameraTransform.localPosition = Vector3.Lerp(
                    _cameraTransform.localPosition, 
                    initialPos, 
                    Time.deltaTime * smoothReturn
                );
                yield return null;
            }

            _cameraTransform.localPosition = initialPos;
            _shakeCoroutine = null;
        }

        private void OnDisable()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _cameraTransform.localPosition = _originalPosition;
            }
        }
    }
}

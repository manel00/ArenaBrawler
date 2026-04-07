using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Anima la luz del fireball para que parpée y cree efecto dinámico
    /// </summary>
    public class FireballLightAnimation : MonoBehaviour
    {
        private Light _light;
        private float _baseIntensity;
        private float _randomOffset;
        
        void Start()
        {
            _light = GetComponent<Light>();
            if (_light != null)
            {
                _baseIntensity = _light.intensity;
            }
            _randomOffset = Random.Range(0f, 100f);
        }
        
        void Update()
        {
            if (_light == null) return;
            
            // Animar intensidad con ruido para efecto de fuego realista
            float time = Time.time * 10f + _randomOffset;
            float noise = Mathf.PerlinNoise(time, 0f);
            _light.intensity = _baseIntensity * (0.7f + noise * 0.6f);
            
            // Pequeña variación de color
            float hueShift = Mathf.PerlinNoise(time * 0.5f, 1f) * 0.1f;
            _light.color = new Color(
                Mathf.Clamp01(1f + hueShift), 
                Mathf.Clamp01(0.5f - hueShift * 0.5f), 
                0.2f
            );
        }
    }
}

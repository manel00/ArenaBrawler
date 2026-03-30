using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Componente auxiliar para efectos de partículas en objetos pooled.
    /// Se agrega a prefabs de VFX para controlar su ciclo de vida.
    /// </summary>
    public class ParticleEffectController : MonoBehaviour
    {
        [Header("Particle Settings")]
        [SerializeField] private ParticleSystem[] particleSystems;
        [SerializeField] private float autoDestroyDelay = 2f;
        [SerializeField] private bool returnToPoolOnComplete = true;
        
        [Header("Scale Settings")]
        [SerializeField] private Vector3 baseScale = Vector3.one;
        
        private float _spawnTime;
        private bool _isPlaying;
        
        private void Awake()
        {
            if (particleSystems == null || particleSystems.Length == 0)
            {
                particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            }
        }
        
        private void OnEnable()
        {
            _spawnTime = Time.time;
            _isPlaying = true;
            
            PlayAllParticles();
        }
        
        private void Update()
        {
            if (!_isPlaying) return;
            
            // Verificar si todas las partículas terminaron
            bool allStopped = true;
            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;
                if (ps.IsAlive(true))
                {
                    allStopped = false;
                    break;
                }
            }
            
            // Auto-destruir después del delay o cuando terminen
            if (allStopped || Time.time - _spawnTime >= autoDestroyDelay)
            {
                StopEffect();
            }
        }
        
        /// <summary>
        /// Reproduce todas las partículas
        /// </summary>
        public void PlayAllParticles()
        {
            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;
                
                ps.Clear();
                ps.Play(true);
            }
            
            _isPlaying = true;
            _spawnTime = Time.time;
        }
        
        /// <summary>
        /// Detiene el efecto y retorna al pool
        /// </summary>
        public void StopEffect()
        {
            _isPlaying = false;
            
            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            
            if (returnToPoolOnComplete && GenericObjectPool.Instance != null)
            {
                GenericObjectPool.Instance.ReturnToPool(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Configura la escala del efecto
        /// </summary>
        public void SetScale(float scale)
        {
            transform.localScale = baseScale * scale;
        }
        
        /// <summary>
        /// Cambia el color de todas las partículas
        /// </summary>
        public void SetColor(Color color)
        {
            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;
                
                var main = ps.main;
                main.startColor = color;
            }
        }
    }
}

using UnityEngine;
using System.Collections;

namespace ArenaEnhanced
{
    /// <summary>
    /// Controlador premium del sistema de partículas del lanzallamas.
    /// Gestiona efectos masivos, denso y destructivo con 30m de alcance.
    /// </summary>
    public class FlamethrowerVFXController : MonoBehaviour
    {
        [Header("Particle System References")]
        [SerializeField] private ParticleSystem flameParticles;
        [SerializeField] private ParticleSystem sparkParticles;
        [SerializeField] private ParticleSystem smokeParticles;
        [SerializeField] private ParticleSystem heatDistortionParticles;
        
        [Header("Flame Configuration - 30m Massive Range")]
        [Tooltip("Alcance del lanzallamas en metros")]
        [SerializeField] private float flameRange = 30f;
        
        [Tooltip("Ángulo del cono de fuego en grados - más bajo = más horizontal")]
        [SerializeField] private float coneAngle = 12f;
        
        [Tooltip("Tasa de emisión de partículas (masivo = 2000-3000 para efecto denso)")]
        [SerializeField] private int emissionRate = 2000;
        
        [Header("Audio (Disabled by default)")]
        [SerializeField] private AudioClip flameStartSound;
        [SerializeField] private AudioClip flameLoopSound;
        [SerializeField] private AudioClip flameEndSound;
        
        // Internal state
        private bool _isFiring = false;
        private ParticleSystem.MainModule _flameMain;
        private ParticleSystem.EmissionModule _flameEmission;
        private ParticleSystem.ShapeModule _flameShape;
        private ParticleSystem.ColorOverLifetimeModule _flameColorOverLifetime;
        private ParticleSystem.SizeOverLifetimeModule _flameSizeOverLifetime;
        private ParticleSystem.VelocityOverLifetimeModule _flameVelocityOverLifetime;
        
        // Audio source (created dynamically if needed)
        private AudioSource _audioSource;
        
        private void Awake()
        {
            InitializeParticleSystems();
        }
        
        private void InitializeParticleSystems()
        {
            // Create or get flame particle system
            if (flameParticles == null)
            {
                flameParticles = CreateFlameParticleSystem();
            }
            
            // Cache modules for performance
            _flameMain = flameParticles.main;
            _flameEmission = flameParticles.emission;
            _flameShape = flameParticles.shape;
            _flameColorOverLifetime = flameParticles.colorOverLifetime;
            _flameSizeOverLifetime = flameParticles.sizeOverLifetime;
            _flameVelocityOverLifetime = flameParticles.velocityOverLifetime;
            
            // Configure the massive flame effect
            ConfigureMassiveFlame();
        }
        
        /// <summary>
        /// Crea el sistema de partículas de fuego masivo
        /// </summary>
        private ParticleSystem CreateFlameParticleSystem()
        {
            GameObject psObj = new GameObject("FlameParticles");
            psObj.transform.SetParent(transform, false);
            // Posición en la punta del arma (hacia adelante)
            psObj.transform.localPosition = new Vector3(0f, 0f, 0.5f);
            psObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            
            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
            
            // Stop default playback
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            return ps;
        }
        
        /// <summary>
        /// Configura el efecto de fuego masivo con especificaciones exactas
        /// </summary>
        private void ConfigureMassiveFlame()
        {
            // MAIN MODULE - Fuego que empieza muy delgado
            _flameMain.duration = 1f;
            _flameMain.loop = true;
            _flameMain.startLifetime = 0.8f;
            _flameMain.startSpeed = 35f;
            _flameMain.startSize = 0.15f;  // MUY PEQUEÑO al inicio
            _flameMain.startColor = Color.white;
            _flameMain.gravityModifier = -0.1f; // Ligera elevación
            _flameMain.playOnAwake = false;
            _flameMain.maxParticles = 1500;
            _flameMain.scalingMode = ParticleSystemScalingMode.Local;
            _flameMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            
            // EMISSION - Menos partículas, más densas
            _flameEmission.enabled = true;
            _flameEmission.rateOverTime = 600f;
            
            // SHAPE - Cono muy estrecho al inicio
            _flameShape.enabled = true;
            _flameShape.shapeType = ParticleSystemShapeType.Cone;
            _flameShape.angle = 3f;  // Cono muy cerrado
            _flameShape.length = 30f;
            _flameShape.radius = 0.02f;  // Radio muy pequeño
            _flameShape.radiusThickness = 1f;
            _flameShape.arc = 360f;
            
            // COLOR - 50% más transparente
            _flameColorOverLifetime.enabled = true;
            Gradient colorGradient = new Gradient();
            colorGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.yellow, 0f),
                    new GradientColorKey(Color.white, 0.3f),
                    new GradientColorKey(new Color(1f, 0.5f, 0f), 0.6f),
                    new GradientColorKey(Color.gray, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.5f, 0f),  // 50% transparente al inicio
                    new GradientAlphaKey(0.5f, 0.5f), // 50% transparente en medio
                    new GradientAlphaKey(0f, 1f)      // 0% al final
                }
            );
            _flameColorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);
            
            // SIZE - Expansión de pequeño a grande (delgado → ancho)
            _flameSizeOverLifetime.enabled = true;
            // Curva que va de 0.2 a 4.0 (delgado al inicio, ancho al final)
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.2f);   // Inicio: muy delgado
            sizeCurve.AddKey(0.3f, 0.8f); // Primer tercio: creciendo
            sizeCurve.AddKey(0.7f, 2.5f); // Segundo tercio: expandiéndose
            sizeCurve.AddKey(1f, 4.0f);   // Final: muy ancho
            _flameSizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            
            // RENDERER - Material 50% transparente
            ParticleSystemRenderer renderer = flameParticles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateFlameMaterial();
            
            // Asegurar que el material tenga transparencia al 50%
            if (renderer.material != null)
            {
                Color matColor = renderer.material.GetColor("_Color");
                matColor.a = 0.5f;
                renderer.material.SetColor("_Color", matColor);
            }
        }
        
        /// <summary>
        /// Crea un material procedural para las llamas
        /// </summary>
        private Material CreateFlameMaterial()
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            if (mat == null)
            {
                mat = new Material(Shader.Find("Particles/Standard Unlit"));
            }
            if (mat == null)
            {
                mat = new Material(Shader.Find("Mobile/Particles/Alpha-Blended"));
            }
            
            if (mat != null)
            {
                mat.SetFloat("_Mode", 2); // Fade mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                
                // Color base
                mat.SetColor("_Color", new Color(1f, 0.5f, 0.1f, 0.9f));
                
                // Emisión para glow
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0f, 1f));
            }
            
            return mat;
        }
        
        /// <summary>
        /// Activa el efecto de fuego
        /// </summary>
        public void StartFiring()
        {
            if (_isFiring) return;
            
            _isFiring = true;
            
            if (flameParticles != null && !flameParticles.isPlaying)
            {
                flameParticles.Play(true);
            }
            
            // Play start sound if audio enabled
            if (ArenaAudioManager.AudioEnabled && flameStartSound != null)
            {
                ArenaAudioManager.PlaySoundAtPosition(flameStartSound, transform.position, 0.8f);
            }
            
            // Start loop sound
            StartCoroutine(PlayLoopSound());
        }
        
        /// <summary>
        /// Detiene el efecto de fuego
        /// </summary>
        public void StopFiring()
        {
            if (!_isFiring) return;
            
            _isFiring = false;
            
            if (flameParticles != null && flameParticles.isPlaying)
            {
                flameParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            
            // Play end sound if audio enabled
            if (ArenaAudioManager.AudioEnabled && flameEndSound != null)
            {
                ArenaAudioManager.PlaySoundAtPosition(flameEndSound, transform.position, 0.6f);
            }
        }
        
        private IEnumerator PlayLoopSound()
        {
            if (!ArenaAudioManager.AudioEnabled || flameLoopSound == null) yield break;
            
            while (_isFiring)
            {
                ArenaAudioManager.PlaySoundAtPosition(flameLoopSound, transform.position, 0.5f);
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        /// <summary>
        /// Ajusta la intensidad del fuego (0-1)
        /// </summary>
        public void SetIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            
            if (_flameEmission.enabled)
            {
                _flameEmission.rateOverTime = new ParticleSystem.MinMaxCurve(
                    emissionRate * intensity * 0.8f,
                    emissionRate * intensity * 1.2f
                );
            }
        }
        
        /// <summary>
        /// Verifica si el sistema está activo
        /// </summary>
        public bool IsFiring => _isFiring;
        
        /// <summary>
        /// Obtiene el rango del lanzallamas
        /// </summary>
        public float Range => flameRange;
        
        /// <summary>
        /// Obtiene el ángulo del cono
        /// </summary>
        public float ConeAngle => coneAngle;
        
        private void OnDestroy()
        {
            StopFiring();
        }
    }
}

using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Controla la Brecha Dimensional - efecto visual de distorsión dimensional.
    /// Pulsa, distorsiona el aire, emite colores imposibles.
    /// </summary>
    public class DimensionalRift : MonoBehaviour
    {
        [Header("Visual Settings")]
        [Tooltip("Velocidad del pulso de la brecha")]
        [Range(0.1f, 3f)]
        public float pulseSpeed = 1f;
        
        [Tooltip("Intensidad del pulso")]
        [Range(0.5f, 3f)]
        public float pulseIntensity = 1.5f;
        
        [Tooltip("Escala base de la brecha")]
        public Vector3 baseScale = new Vector3(1f, 3f, 1f);
        
        [Header("Colors")]
        public Color colorA = new Color(0.5f, 0f, 1f, 1f); // Púrpura
        public Color colorB = new Color(1f, 0.8f, 0f, 1f); // Amarillo imposible
        public Color colorC = new Color(0f, 1f, 0.5f, 1f); // Bioluminiscencia
        
        [Header("Audio")]
        [Tooltip("Sonido de canto dimensional")]
        public AudioClip ambientSound;
        
        [Header("Proximity Effects")]
        [Tooltip("Distancia a la que afecta al jugador")]
        public float effectRadius = 15f;
        
        [Tooltip("Daño por segundo cerca de la brecha")]
        public float damagePerSecond = 2f;
        
        // Internal
        private Material _riftMaterial;
        private AudioSource _audioSource;
        private float _currentPulse;
        private Transform _playerTransform;
        
        // Shader property IDs
        private static readonly int PulseSpeedID = Shader.PropertyToID("_PulseSpeed");
        private static readonly int ColorAID = Shader.PropertyToID("_ColorA");
        private static readonly int ColorBID = Shader.PropertyToID("_ColorB");
        private static readonly int ColorCID = Shader.PropertyToID("_ColorC");
        private static readonly int DistortionID = Shader.PropertyToID("_DistortionStrength");
        
        private void Awake()
        {
            // Setup visual
            SetupVisuals();
            
            // Setup audio
            SetupAudio();
            
            // Find player
            FindPlayer();
        }
        
        private void SetupVisuals()
        {
            // Create sphere mesh for the rift
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.mesh = CreateDistortedSphere();
            }
            
            // Setup renderer with URP-compatible shader (avoiding GrabPass issues)
            var renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            // Use URP-compatible shader directly - avoid the problematic GrabPass shader
            _riftMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            _riftMaterial.SetFloat("_Surface", 1); // Transparent
            _riftMaterial.SetColor("_BaseColor", colorA);
            _riftMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            
            renderer.material = _riftMaterial;
            
            // Apply initial colors
            UpdateMaterialProperties();
            
            // Setup collider for detection
            var collider = gameObject.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
            }
            collider.radius = 1f;
        }
        
        private Mesh CreateDistortedSphere()
        {
            // Create a capsule-like shape for the rift
            Mesh mesh = new Mesh();
            
            int segments = 32;
            int rings = 16;
            
            Vector3[] vertices = new Vector3[(rings + 1) * (segments + 1)];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[rings * segments * 6];
            
            // Generate vertices
            for (int ring = 0; ring <= rings; ring++)
            {
                float v = (float)ring / rings;
                float y = (v - 0.5f) * 3f; // Stretch vertically
                
                for (int seg = 0; seg <= segments; seg++)
                {
                    float u = (float)seg / segments;
                    float angle = u * Mathf.PI * 2f;
                    
                    // Distort radius based on height for capsule shape
                    float radius = 1f;
                    if (Mathf.Abs(y) > 1f)
                    {
                        radius = Mathf.Max(0f, 1f - (Mathf.Abs(y) - 1f) * 0.5f);
                    }
                    
                    float x = Mathf.Cos(angle) * radius;
                    float z = Mathf.Sin(angle) * radius;
                    
                    int index = ring * (segments + 1) + seg;
                    vertices[index] = new Vector3(x, y, z);
                    normals[index] = new Vector3(x, 0, z).normalized;
                    uvs[index] = new Vector2(u, v);
                }
            }
            
            // Generate triangles
            int triIndex = 0;
            for (int ring = 0; ring < rings; ring++)
            {
                for (int seg = 0; seg < segments; seg++)
                {
                    int current = ring * (segments + 1) + seg;
                    int next = current + segments + 1;
                    
                    triangles[triIndex++] = current;
                    triangles[triIndex++] = next;
                    triangles[triIndex++] = current + 1;
                    
                    triangles[triIndex++] = current + 1;
                    triangles[triIndex++] = next;
                    triangles[triIndex++] = next + 1;
                }
            }
            
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        private void SetupAudio()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            _audioSource.spatialBlend = 1f;
            _audioSource.maxDistance = 50f;
            _audioSource.minDistance = 5f;
            _audioSource.loop = true;
            _audioSource.volume = 0.3f;
            
            if (ambientSound != null)
            {
                _audioSource.clip = ambientSound;
                _audioSource.Play();
            }
        }
        
        private void FindPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }
        
        private void Update()
        {
            // Animate pulse
            _currentPulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            
            // Apply pulse to scale
            float pulseScale = 1f + _currentPulse * 0.1f * pulseIntensity;
            transform.localScale = baseScale * pulseScale;
            
            // Update material properties
            if (_riftMaterial != null && _riftMaterial.shader.name.Contains("DimensionalRift"))
            {
                _riftMaterial.SetFloat(PulseSpeedID, pulseSpeed);
                _riftMaterial.SetFloat(DistortionID, 0.3f + _currentPulse * 0.2f);
            }
            
            // Rotate slowly
            transform.Rotate(Vector3.up, Time.deltaTime * 5f);
            
            // Check player proximity
            CheckPlayerProximity();
        }
        
        private void UpdateMaterialProperties()
        {
            if (_riftMaterial != null)
            {
                _riftMaterial.SetColor(ColorAID, colorA);
                _riftMaterial.SetColor(ColorBID, colorB);
                _riftMaterial.SetColor(ColorCID, colorC);
            }
        }
        
        private void CheckPlayerProximity()
        {
            if (_playerTransform == null) return;
            
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            
            if (distance < effectRadius)
            {
                // Player is near the rift - apply effects
                float normalizedDist = 1f - Mathf.Clamp01(distance / effectRadius);
                
                // Intensify visual effect
                if (_riftMaterial != null)
                {
                    _riftMaterial.SetFloat(DistortionID, 0.5f + normalizedDist * 0.5f);
                }
                
                // Apply damage if very close
                if (distance < effectRadius * 0.3f && damagePerSecond > 0)
                {
                    var combatant = _playerTransform.GetComponent<ArenaCombatant>();
                    if (combatant != null && combatant.IsAlive)
                    {
                        combatant.TakeDamage(damagePerSecond * Time.deltaTime, gameObject);
                    }
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(colorA.r, colorA.g, colorA.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, effectRadius);
            
            Gizmos.color = new Color(colorC.r, colorC.g, colorC.b, 0.5f);
            Gizmos.DrawWireSphere(transform.position, effectRadius * 0.3f);
        }
        
        /// <summary>
        /// Spawns the dimensional rift at the specified position.
        /// </summary>
        public static DimensionalRift Spawn(Vector3 position)
        {
            var go = new GameObject("DimensionalRift");
            go.transform.position = position;
            
            var rift = go.AddComponent<DimensionalRift>();
            return rift;
        }
    }
}

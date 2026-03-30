using UnityEngine;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de hierba interactiva que responde al movimiento de entidades.
    /// Las hierbas se inclinan y ondulan cuando el jugador/enemigos pasan cerca.
    /// </summary>
    public class InteractiveGrass : MonoBehaviour
    {
        [Header("Grass Settings")]
        [Tooltip("Radio de influencia donde la hierba reacciona")]
        [SerializeField] private float influenceRadius = 1.5f;
        
        [Tooltip("Fuerza máxima de inclinación de la hierba")]
        [SerializeField] private float maxBendAmount = 0.6f;
        
        [Tooltip("Velocidad de recuperación de la hierba")]
        [SerializeField] private float recoverySpeed = 3f;
        
        [Tooltip("Suavizado del movimiento de la hierba")]
        [SerializeField] private float smoothness = 5f;

        [Header("Visual Settings")]
        [Tooltip("Color base de la hierba")]
        [SerializeField] private Color grassColor = new Color(0.35f, 0.65f, 0.25f);
        
        [Tooltip("Color cuando está siendo pisada")]
        [SerializeField] private Color steppedColor = new Color(0.45f, 0.75f, 0.35f);

        [Header("Optimization")]
        [Tooltip("Actualizaciones por segundo (0 = cada frame)")]
        [SerializeField] private int updatesPerSecond = 30;
        
        [Tooltip("Solo reaccionar al jugador (más ligero)")]
        [SerializeField] private bool playerOnly = false;

        // Shader property IDs para performance
        private static readonly int BendAmount = Shader.PropertyToID("_BendAmount");
        private static readonly int BendDirection = Shader.PropertyToID("_BendDirection");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private Material _grassMaterial;
        private Vector3 _currentBendDirection;
        private float _currentBendAmount;
        private Vector3 _targetBendDirection;
        private float _targetBendAmount;
        private float _lastUpdateTime;
        private Renderer _renderer;
        
        // Lista de entidades que afectan esta hierba
        private List<Transform> _nearbyEntities = new List<Transform>();

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                // Crear material instancia para no afectar otros
                _grassMaterial = new Material(_renderer.material);
                _renderer.material = _grassMaterial;
                _grassMaterial.SetColor(BaseColor, grassColor);
            }
        }

        private void Update()
        {
            // Throttling para performance
            if (updatesPerSecond > 0)
            {
                float interval = 1f / updatesPerSecond;
                if (Time.time - _lastUpdateTime < interval) return;
                _lastUpdateTime = Time.time;
            }

            UpdateBendTarget();
            SmoothBend();
            ApplyBend();
        }

        /// <summary>
        /// Calcula la dirección y cantidad de inclinación basada en entidades cercanas
        /// </summary>
        private void UpdateBendTarget()
        {
            _targetBendAmount = 0f;
            _targetBendDirection = Vector3.zero;

            // Buscar entidades cercanas
            Collider[] hits = Physics.OverlapSphere(transform.position, influenceRadius, 
                LayerMask.GetMask("Player", "Enemy"), QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                // Si es solo jugador, ignorar enemigos
                if (playerOnly && !hit.CompareTag("Player")) continue;

                Vector3 toEntity = hit.transform.position - transform.position;
                toEntity.y = 0; // Solo en plano horizontal
                float distance = toEntity.magnitude;

                if (distance < influenceRadius && distance > 0.01f)
                {
                    // Calcular influencia basada en distancia
                    float influence = 1f - (distance / influenceRadius);
                    influence = influence * influence; // Curva cuadrática para suavizado

                    // Dirección opuesta a la entidad (la hierba se inclina "alejandose")
                    Vector3 awayDir = -toEntity.normalized;
                    
                    // Acumular influencias
                    _targetBendDirection += awayDir * influence;
                    _targetBendAmount = Mathf.Max(_targetBendAmount, influence * maxBendAmount);
                }
            }

            // Normalizar dirección
            if (_targetBendDirection.magnitude > 0.01f)
            {
                _targetBendDirection.Normalize();
            }
            else
            {
                // Sin influencia - recuperación gradual
                _targetBendAmount = 0f;
            }
        }

        /// <summary>
        /// Suaviza la transición entre estados de inclinación
        /// </summary>
        private void SmoothBend()
        {
            // Interpolar dirección
            _currentBendDirection = Vector3.Slerp(_currentBendDirection, _targetBendDirection, 
                Time.deltaTime * smoothness);

            // Si no hay dirección objetivo, recuperar
            if (_targetBendAmount < 0.01f)
            {
                _currentBendAmount = Mathf.Lerp(_currentBendAmount, 0f, Time.deltaTime * recoverySpeed);
                if (_currentBendAmount < 0.01f) _currentBendDirection = Vector3.zero;
            }
            else
            {
                _currentBendAmount = Mathf.Lerp(_currentBendAmount, _targetBendAmount, 
                    Time.deltaTime * smoothness);
            }
        }

        /// <summary>
        /// Aplica los valores calculados al shader
        /// </summary>
        private void ApplyBend()
        {
            if (_grassMaterial == null) return;

            _grassMaterial.SetFloat(BendAmount, _currentBendAmount);
            _grassMaterial.SetVector(BendDirection, _currentBendDirection);

            // Cambio de color sutil cuando está siendo pisada
            if (_currentBendAmount > 0.3f)
            {
                _grassMaterial.SetColor(BaseColor, Color.Lerp(grassColor, steppedColor, 
                    _currentBendAmount / maxBendAmount));
            }
            else
            {
                _grassMaterial.SetColor(BaseColor, grassColor);
            }
        }

        /// <summary>
        /// Método público para forzar reacción (ej: habilidades, explosiones)
        /// </summary>
        public void ApplyForce(Vector3 direction, float force)
        {
            _targetBendDirection = direction.normalized;
            _targetBendAmount = Mathf.Clamp01(force) * maxBendAmount;
        }

        private void OnDrawGizmosSelected()
        {
            // Visualizar radio de influencia en editor
            Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, influenceRadius);
            
            // Visualizar dirección actual de inclinación
            if (Application.isPlaying && _currentBendAmount > 0.01f)
            {
                Gizmos.color = Color.yellow;
                Vector3 start = transform.position + Vector3.up * 0.5f;
                Gizmos.DrawLine(start, start + _currentBendDirection * _currentBendAmount);
            }
        }
    }
}

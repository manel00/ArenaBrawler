using UnityEngine;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Zona de daño del lanzallamas masivo.
    /// Aplica daño continuo y efectos de estado (quemadura) a enemigos en el área del cono.
    /// </summary>
    public class FlamethrowerDamageZone : MonoBehaviour
    {
        [Header("Damage Configuration")]
        [Tooltip("Daño por segundo base")]
        [SerializeField] private float damagePerSecond = 25f;
        
        [Tooltip("Rango del daño (debe coincidir con VFX = 30m)")]
        [SerializeField] private float damageRange = 30f;
        
        [Tooltip("Ángulo del cono de daño en grados - debe coincidir con VFX")]
        [SerializeField] private float coneAngle = 12f;
        
        [Header("Burn Effect")]
        [Tooltip("Aplicar efecto de quemadura")]
        [SerializeField] private bool applyBurnEffect = true;
        
        [Tooltip("Daño DOT de quemadura por segundo")]
        [SerializeField] private float burnDamagePerSecond = 5f;
        
        [Tooltip("Duración de la quemadura en segundos")]
        [SerializeField] private float burnDuration = 3f;
        
        [Header("VFX Reference")]
        [SerializeField] private FlamethrowerVFXController vfxController;
        
        [Header("Performance")]
        [Tooltip("Intervalo entre chequeos de daño (optimización)")]
        [SerializeField] private float damageTickInterval = 0.1f;
        
        [Tooltip("Máximo de enemigos afectados por tick")]
        [SerializeField] private int maxTargetsPerTick = 20;
        
        [Tooltip("Capas de colisión para detectar enemigos (Everything = todos)")]
        [SerializeField] private LayerMask enemyLayers = ~0; // Default: todo
        
        [Header("Collision Check")]
        [Tooltip("Capas que bloquean el fuego (obstáculos)")]
        [SerializeField] private LayerMask obstacleLayers = ~0; // Todo bloquea por defecto
        
        [Tooltip("Requerir línea de visión (raycast) para aplicar daño")]
        [SerializeField] private bool requireLineOfSight = true;
        
        // Internal state
        private bool _isActive = false;
        private float _lastDamageTime = 0f;
        private ArenaCombatant _owner;
        private Transform _ownerTransform;
        
        // Caching
        private readonly Collider[] _hitBuffer = new Collider[30];
        private List<ArenaCombatant> _affectedTargets = new List<ArenaCombatant>();
        private Dictionary<ArenaCombatant, float> _burningTargets = new Dictionary<ArenaCombatant, float>();
        
        private void Awake()
        {
            _owner = GetComponentInParent<ArenaCombatant>();
            _ownerTransform = _owner != null ? _owner.transform : transform.parent;
            
            if (vfxController == null)
            {
                vfxController = GetComponent<FlamethrowerVFXController>();
            }
        }
        
        private void Update()
        {
            if (!_isActive) return;
            
            // Aplicar daño en intervalos para optimizar
            if (Time.time - _lastDamageTime >= damageTickInterval)
            {
                ApplyDamageToTargets();
                UpdateBurnEffects();
                _lastDamageTime = Time.time;
            }
        }
        
        /// <summary>
        /// Activa la zona de daño
        /// </summary>
        public void Activate()
        {
            _isActive = true;
            _lastDamageTime = Time.time;
        }
        
        /// <summary>
        /// Desactiva la zona de daño
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
            _affectedTargets.Clear();
        }
        
        /// <summary>
        /// Aplica daño a todos los objetivos en el área del cono
        /// </summary>
        private void ApplyDamageToTargets()
        {
            if (_ownerTransform == null)
            {
                Debug.LogWarning("[FlamethrowerDamage] _ownerTransform is NULL!");
                return;
            }
            
            // Origen a altura del arma (bajo, cerca del suelo)
            Vector3 origin = _ownerTransform.position + Vector3.up * 0.5f;
            Vector3 forward = _ownerTransform.forward;
            
            // Usar OverlapSphereNonAlloc con LayerMask para detectar enemigos
            int hitCount = Physics.OverlapSphereNonAlloc(origin, damageRange, _hitBuffer, enemyLayers);
            
            // DEBUG: Descomentar para debugging
            // Debug.Log($"[FlamethrowerDamage] Scanning from {origin}, range={damageRange}, angle={coneAngle}, layerMask={enemyLayers.value}, hits={hitCount}");
            
            int targetsProcessed = 0;
            int validCombatants = 0;
            
            for (int i = 0; i < hitCount && targetsProcessed < maxTargetsPerTick; i++)
            {
                Collider col = _hitBuffer[i];
                if (col == null) continue;
                
                // Ignorar al propietario
                if (col.transform == _ownerTransform) continue;
                
                // Verificar si es un combatiente válido
                ArenaCombatant target = col.GetComponent<ArenaCombatant>();
                if (target == null)
                {
                    target = col.GetComponentInParent<ArenaCombatant>();
                }
                
                if (target == null)
                {
                    Debug.Log($"[FlamethrowerDamage] Collider {col.name} has no ArenaCombatant");
                    continue;
                }
                
                validCombatants++;
                
                if (!target.IsAlive)
                {
                    // DEBUG: Descomentar para debugging
                    // Debug.Log($"[FlamethrowerDamage] Target {target.name} is not alive");
                    continue;
                }
                
                // Verificar equipo (no dañar aliados)
                if (_owner != null && target.teamId == _owner.teamId)
                {
                    // DEBUG: Descomentar para debugging
                    // Debug.Log($"[FlamethrowerDamage] Target {target.name} is same team ({target.teamId})");
                    continue;
                }
                
                // Verificar si está dentro del ángulo del cono
                Vector3 toTarget = target.transform.position - origin;
                float distance = toTarget.magnitude;
                
                // Dentro del rango
                if (distance > damageRange)
                {
                    // DEBUG: Descomentar para debugging
                    // Debug.Log($"[FlamethrowerDamage] Target {target.name} out of range ({distance:F1} > {damageRange})");
                    continue;
                }
                
                // Dentro del ángulo del cono
                float angle = Vector3.Angle(forward, toTarget);
                if (angle > coneAngle)
                {
                    // DEBUG: Descomentar para debugging
                    // Debug.Log($"[FlamethrowerDamage] Target {target.name} out of angle ({angle:F1}° > {coneAngle}°)");
                    continue;
                }
                
                // VERIFICAR LÍNEA DE VISIÓN - El fuego debe poder llegar al objetivo
                if (requireLineOfSight)
                {
                    Vector3 targetCenter = target.transform.position + Vector3.up * 1f; // Centro del cuerpo
                    Vector3 rayDirection = targetCenter - origin;
                    float rayDistance = rayDirection.magnitude;
                    
                    // Raycast para verificar obstáculos
                    if (Physics.Raycast(origin, rayDirection.normalized, out RaycastHit hitInfo, rayDistance, obstacleLayers))
                    {
                        // Si el raycast golpea algo que no es el objetivo, hay un obstáculo
                        if (hitInfo.collider.gameObject != target.gameObject && 
                            !hitInfo.collider.transform.IsChildOf(target.transform))
                        {
                            continue;
                        }
                    }
                }
                
                // Calcular daño
                float tickDamage = damagePerSecond * damageTickInterval;
                
                // Multiplicador por distancia (más cerca = más daño)
                float distanceMultiplier = 1f - (distance / damageRange) * 0.5f;
                tickDamage *= distanceMultiplier;
                
                // DEBUG: Descomentar para debugging
                // Debug.Log($"[FlamethrowerDamage] HIT! {target.name} at {distance:F1}m, angle={angle:F1}°, damage={tickDamage:F3}");
                
                // Aplicar daño
                target.TakeDamage(tickDamage, _owner != null ? _owner.gameObject : gameObject);
                
                // Aplicar efecto de quemadura
                if (applyBurnEffect)
                {
                    ApplyBurnEffect(target);
                }
                
                targetsProcessed++;
            }
            
            if (targetsProcessed == 0)
            {
                // DEBUG: Descomentar para debugging
                // Debug.Log($"[FlamethrowerDamage] No targets hit. Total colliders: {hitCount}, valid combatants: {validCombatants}");
            }
        }
        
        /// <summary>
        /// Aplica efecto de quemadura DOT
        /// </summary>
        private void ApplyBurnEffect(ArenaCombatant target)
        {
            if (!_burningTargets.ContainsKey(target))
            {
                _burningTargets[target] = Time.time + burnDuration;
                
                // Mostrar efecto visual de quemadura
                SpawnBurnEffect(target);
            }
            else
            {
                // Refrescar duración
                _burningTargets[target] = Time.time + burnDuration;
            }
        }
        
        /// <summary>
        /// Actualiza los efectos de quemadura activos
        /// </summary>
        private void UpdateBurnEffects()
        {
            List<ArenaCombatant> toRemove = new List<ArenaCombatant>();
            
            foreach (var kvp in _burningTargets)
            {
                ArenaCombatant target = kvp.Key;
                float endTime = kvp.Value;
                
                if (target == null || !target.IsAlive || Time.time >= endTime)
                {
                    toRemove.Add(target);
                    continue;
                }
                
                // Aplicar daño de quemadura
                float burnTick = burnDamagePerSecond * damageTickInterval;
                target.TakeDamage(burnTick, _owner != null ? _owner.gameObject : gameObject);
            }
            
            // Limpiar quemaduras terminadas
            foreach (var target in toRemove)
            {
                _burningTargets.Remove(target);
            }
        }
        
        /// <summary>
        /// Spawnea efecto visual de quemadura
        /// </summary>
        private void SpawnBurnEffect(ArenaCombatant target)
        {
            if (target == null) return;
            
            // Crear efecto de fuego en el enemigo
            Vector3 pos = target.transform.position + Vector3.up * 1f;
            
            // Usar VFXManager si está disponible
            VFXManagerPooled.SpawnImpact(pos);
        }
        
        /// <summary>
        /// Configura los parámetros de daño
        /// </summary>
        public void SetDamageParameters(float dps, float range, float angle)
        {
            damagePerSecond = dps;
            damageRange = range;
            coneAngle = angle;
        }
        
        /// <summary>
        /// Establece el owner del daño (quien dispara el lanzallamas)
        /// </summary>
        public void SetOwner(ArenaCombatant owner)
        {
            _owner = owner;
            _ownerTransform = owner != null ? owner.transform : transform.parent;
        }
        
        /// <summary>
        /// Dibuja gizmos en editor para visualizar el área de daño
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_ownerTransform == null) _ownerTransform = transform.parent;
            if (_ownerTransform == null) return;
            
            Gizmos.color = Color.red;
            
            // Dibujar cono de daño desde altura del arma (baja)
            Vector3 origin = _ownerTransform.position + Vector3.up * 0.5f;
            Vector3 forward = _ownerTransform.forward * damageRange;
            
            // Línea central
            Gizmos.DrawLine(origin, origin + forward);
            
            // Arco del cono
            int segments = 20;
            Vector3 prevPoint = origin + Quaternion.Euler(0, -coneAngle, 0) * forward;
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-coneAngle, coneAngle, i / (float)segments);
                Vector3 point = origin + Quaternion.Euler(0, angle, 0) * forward;
                Gizmos.DrawLine(prevPoint, point);
                Gizmos.DrawLine(origin, point);
                prevPoint = point;
            }
            
            // Círculo del rango máximo
            DrawWireCircle(origin + forward, damageRange * Mathf.Sin(coneAngle * Mathf.Deg2Rad), Color.red);
        }
        
        private void DrawWireCircle(Vector3 center, float radius, Color color)
        {
            Gizmos.color = color;
            int segments = 32;
            float angleStep = 360f / segments;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
                
                Vector3 p1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
                Vector3 p2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;
                
                Gizmos.DrawLine(p1, p2);
            }
        }
        
        public bool IsActive => _isActive;
        public float DamagePerSecond => damagePerSecond;
        public float Range => damageRange;
    }
}

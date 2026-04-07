using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de lanzamiento de granadas con mecánica de carga (hold) y trazado parabólico.
    /// Tecla 6: Mantener para cargar fuerza, soltar para lanzar.
    /// </summary>
    public class GrenadeSystem : MonoBehaviour
    {
        [Header("Grenade Settings")]
        [Tooltip("Prefab de la granada (debe tener GrenadeProjectile)")]
        [SerializeField] private GameObject grenadePrefab;
        
        [Tooltip("Fuerza mínima de lanzamiento")]
        [SerializeField] private float minThrowForce = 8f;
        
        [Tooltip("Fuerza máxima de lanzamiento")]
        [SerializeField] private float maxThrowForce = 25f;
        
        [Tooltip("Tiempo máximo de carga para fuerza máxima")]
        [SerializeField] private float maxChargeTime = 1.5f;
        
        [Tooltip("Offset de spawn desde el jugador")]
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0.5f);
        
        [Tooltip("Ángulo de lanzamiento en grados (sobre el plano horizontal)")]
        [SerializeField] private float throwAngle = 30f;

        [Header("Trajectory Visualization")]
        [Tooltip("Número de puntos en la línea de trayectoria")]
        [SerializeField] private int trajectoryPoints = 30;
        
        [Tooltip("Tiempo de simulación de la trayectoria")]
        [SerializeField] private float trajectoryTime = 2f;
        
        [Tooltip("Material de la línea de trayectoria")]
        [SerializeField] private Material trajectoryMaterial;
        
        [Tooltip("Color de la línea cuando está lista")]
        [SerializeField] private Color trajectoryReadyColor = Color.red;
        
        [Tooltip("Color de la línea cuando está cargando")]
        [SerializeField] private Color trajectoryChargingColor = Color.red;

        [Header("Animation")]
        [Tooltip("Nombre del trigger de animación para iniciar lanzamiento")]
        [SerializeField] private string throwAnimationTrigger = "ThrowGrenade";
        
        [Tooltip("Delay antes del lanzamiento real (para sincronizar con animación)")]
        [SerializeField] private float throwAnimationDelay = 0.3f;

        // Runtime state
        private bool _isCharging = false;
        private float _chargeStartTime;
        private float _currentCharge;
        private LineRenderer _trajectoryLine;
        private Animator _animator;
        private ArenaCombatant _owner;
        
        // Trajectory line pooling
        private static readonly Vector3[] TrajectoryBuffer = new Vector3[50];

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _owner = GetComponent<ArenaCombatant>();
            SetupTrajectoryLine();
        }

        private void SetupTrajectoryLine()
        {
            GameObject lineObj = new GameObject("GrenadeTrajectory");
            lineObj.transform.SetParent(transform);
            _trajectoryLine = lineObj.AddComponent<LineRenderer>();
            _trajectoryLine.positionCount = 0;
            _trajectoryLine.startWidth = 0.08f;
            _trajectoryLine.endWidth = 0.02f;
            _trajectoryLine.useWorldSpace = true;
            
            if (trajectoryMaterial == null)
            {
                trajectoryMaterial = new Material(Shader.Find("Sprites/Default"));
                trajectoryMaterial.color = trajectoryReadyColor;
            }
            _trajectoryLine.material = trajectoryMaterial;
            _trajectoryLine.enabled = false;
        }

        private void OnEnable()
        {
            InputManager.OnAbilityPressed += HandleAbilityPressed;
        }

        private void OnDisable()
        {
            InputManager.OnAbilityPressed -= HandleAbilityPressed;
            StopCharging();
        }

        private void Update()
        {
            // NOTA: El input se maneja a través del evento OnAbilityPressed de InputManager
            // No usar input directo aquí para evitar duplicación con el InputManager
            
            if (_isCharging)
            {
                UpdateCharge();
                UpdateTrajectory();
            }
        }

        private void HandleAbilityPressed(int abilityIndex)
        {
            // Alternativa: usar el evento del InputManager
            if (abilityIndex == 6 && !_isCharging)
            {
                StartCharging();
            }
        }

        private void StartCharging()
        {
            if (_isCharging) return;
            
            _isCharging = true;
            _chargeStartTime = Time.time;
            _currentCharge = 0f;
            _trajectoryLine.enabled = true;
            
#if DEBUG
            Debug.Log("[GrenadeSystem] Started charging grenade");
#endif
        }

        private void StopCharging()
        {
            _isCharging = false;
            _currentCharge = 0f;
            if (_trajectoryLine != null)
                _trajectoryLine.enabled = false;
        }

        private void UpdateCharge()
        {
            float chargeTime = Time.time - _chargeStartTime;
            _currentCharge = Mathf.Clamp01(chargeTime / maxChargeTime);
            
            // Actualizar color según carga
            if (_trajectoryLine != null && trajectoryMaterial != null)
            {
                Color currentColor = Color.Lerp(trajectoryChargingColor, trajectoryReadyColor, _currentCharge);
                trajectoryMaterial.color = currentColor;
            }
        }

        private void UpdateTrajectory()
        {
            if (_trajectoryLine == null) return;

            float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, _currentCharge);
            Vector3 startPos = GetSpawnPosition();
            Vector3 velocity = CalculateThrowVelocity(throwForce);
            
            SimulateTrajectory(startPos, velocity);
        }

        private void SimulateTrajectory(Vector3 startPos, Vector3 velocity)
        {
            int points = Mathf.Min(trajectoryPoints, TrajectoryBuffer.Length);
            float timeStep = trajectoryTime / points;
            
            TrajectoryBuffer[0] = startPos;
            
            Vector3 currentPos = startPos;
            Vector3 currentVel = velocity;
            
            for (int i = 1; i < points; i++)
            {
                currentVel += Physics.gravity * timeStep;
                currentPos += currentVel * timeStep;
                TrajectoryBuffer[i] = currentPos;
                
                // Detener si golpea el suelo
                if (Physics.Raycast(TrajectoryBuffer[i-1], currentVel.normalized, 
                    Vector3.Distance(TrajectoryBuffer[i-1], currentPos), LayerMask.GetMask("Ground")))
                {
                    points = i + 1;
                    break;
                }
            }
            
            _trajectoryLine.positionCount = points;
            for (int i = 0; i < points; i++)
            {
                _trajectoryLine.SetPosition(i, TrajectoryBuffer[i]);
            }
        }

        private void ReleaseAndThrow()
        {
            if (!_isCharging) return;
            
            float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, _currentCharge);
            
#if DEBUG
            Debug.Log($"[GrenadeSystem] Throwing grenade with force: {throwForce}, charge: {_currentCharge:P0}");
#endif
            
            // Trigger animación - verificar que el parámetro existe
            if (_animator != null && !string.IsNullOrEmpty(throwAnimationTrigger))
            {
                // Verificar si el parámetro existe para evitar error de Animator
                bool parameterExists = false;
                foreach (var param in _animator.parameters)
                {
                    if (param.name == throwAnimationTrigger && param.type == AnimatorControllerParameterType.Trigger)
                    {
                        parameterExists = true;
                        break;
                    }
                }
                
                if (parameterExists)
                {
                    _animator.SetTrigger(throwAnimationTrigger);
                }
                else
                {
                    Debug.LogWarning($"[GrenadeSystem] El parámetro '{throwAnimationTrigger}' no existe en el Animator.");
                }
            }
            
            // Lanzar con delay para sincronizar con animación
            StartCoroutine(ThrowAfterDelay(throwForce, throwAnimationDelay));
            
            StopCharging();
        }

        private IEnumerator ThrowAfterDelay(float throwForce, float delay)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);
            
            ThrowGrenade(throwForce);
        }

        private void ThrowGrenade(float throwForce)
        {
            Vector3 spawnPos = GetSpawnPosition();
            Vector3 velocity = CalculateThrowVelocity(throwForce);
            GameObject grenade = null;
            GrenadeProjectile projectile = null;
            
            if (grenadePrefab == null)
            {
                Debug.LogWarning("[GrenadeSystem] grenadePrefab no está asignado. Usando granada por defecto.");
                
                grenade = CreateDefaultGrenade(spawnPos);
                
                projectile = grenade.GetComponent<GrenadeProjectile>();
                if (projectile == null)
                    projectile = grenade.AddComponent<GrenadeProjectile>();
                
                projectile.Initialize(_owner, velocity);
                ArenaAudioManager.PlayAbilitySound("GrenadeThrow");
                return;
            }
            
            // Usar Object Pool si está disponible
            if (GenericObjectPool.Instance != null)
            {
                grenade = GenericObjectPool.Instance.GetFromPool("Grenade", spawnPos, Quaternion.identity);
            }
            
            // Fallback: crear instancia si el pool falló o no está disponible
            if (grenade == null)
            {
                grenade = Instantiate(grenadePrefab, spawnPos, Quaternion.identity);
            }
            
            if (grenade == null) 
            {
                Debug.LogError("[GrenadeSystem] Fallo al crear granada.");
                return;
            }
            
            // Configurar el proyectil
            projectile = grenade.GetComponent<GrenadeProjectile>();
            if (projectile == null)
                projectile = grenade.AddComponent<GrenadeProjectile>();
            
            projectile.Initialize(_owner, velocity);
            
            // Efectos de sonido
            ArenaAudioManager.PlayAbilitySound("GrenadeThrow");
        }

        private Vector3 GetSpawnPosition()
        {
            return transform.position + transform.rotation * spawnOffset;
        }

        private Vector3 CalculateThrowVelocity(float force)
        {
            // Calcular dirección con ángulo de elevación
            Vector3 forward = transform.forward;
            float radAngle = throwAngle * Mathf.Deg2Rad;
            
            Vector3 velocity = new Vector3(
                forward.x * Mathf.Cos(radAngle),
                Mathf.Sin(radAngle),
                forward.z * Mathf.Cos(radAngle)
            ).normalized * force;
            
            return velocity;
        }

        private GameObject CreateDefaultGrenade(Vector3 position)
        {
            var go = new GameObject("Grenade");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.25f;
            
            // Sistema de partículas en lugar de esfera primitiva
            var ps = CreateGrenadeParticleSystem(go.transform);
            
            // Collider esférico (sin renderer de esfera primitiva)
            var sphereCol = go.AddComponent<SphereCollider>();
            sphereCol.isTrigger = false;
            sphereCol.radius = 0.25f;
            // OPTIMIZACIÓN: Usar PhysicsMaterialCache en lugar de crear nuevo material cada vez
            sphereCol.material = PhysicsMaterialCache.Grenade;
            
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            return go;
        }
        
        private ParticleSystem CreateGrenadeParticleSystem(Transform parent)
        {
            var go = new GameObject("GrenadeVisual");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 3f;
            main.startLifetime = 0.5f;
            main.startSize = 0.25f;
            main.startColor = new Color(1f, 0.2f, 0.2f, 1f);
            main.maxParticles = 20;
            main.playOnAwake = true;
            main.loop = true;
            
            var emission = ps.emission;
            emission.rateOverTime = 15f;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[] { 
                    new GradientColorKey(new Color(1f, 0.2f, 0.2f), 0f),
                    new GradientColorKey(new Color(0.5f, 0.1f, 0.1f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.3f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);
            
            // Emisión propia para efecto de brillo
            var lights = ps.lights;
            lights.enabled = true;
            lights.ratio = 1f;
            
            ps.Play();
            return ps;
        }

        // Debug
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GetSpawnPosition(), 0.1f);
            
            // Mostrar arco de lanzamiento máximo
            Gizmos.color = Color.yellow;
            Vector3 startPos = GetSpawnPosition();
            Vector3 maxVel = CalculateThrowVelocity(maxThrowForce);
            Vector3 pos = startPos;
            Vector3 vel = maxVel;
            float dt = 0.05f;
            
            for (int i = 0; i < 40; i++)
            {
                Vector3 nextPos = pos + vel * dt;
                Gizmos.DrawLine(pos, nextPos);
                vel += Physics.gravity * dt;
                pos = nextPos;
            }
        }
    }
}

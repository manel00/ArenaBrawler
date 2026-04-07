using UnityEngine;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de efectos visuales usando prefabs de Particle Systems.
    /// Reemplaza el uso de CreatePrimitive por efectos de partículas profesionales.
    /// </summary>
    public static class VFXManager
    {
        // Cache de prefabs
        private static GameObject _impactEffectPrefab;
        private static GameObject _explosionEffectPrefab;
        private static GameObject _shieldEffectPrefab;
        private static GameObject _dashEffectPrefab;
        private static GameObject _meleeEffectPrefab;
        private static GameObject _debrisEffectPrefab;

        // POOL: Diccionario de pools para efectos frecuentes (reduce GC allocations)
        private static readonly Dictionary<string, Queue<GameObject>> _effectPools = new Dictionary<string, Queue<GameObject>>();
        private static readonly Dictionary<string, int> _poolLimits = new Dictionary<string, int>
        {
            { "Impact", 20 },
            { "Dash", 10 },
            { "Melee", 15 }
        };

        /// <summary>
        /// Obtiene un efecto del pool o crea uno nuevo
        /// </summary>
        private static GameObject GetPooledEffect(string poolName, GameObject prefab)
        {
            if (!_effectPools.ContainsKey(poolName))
                _effectPools[poolName] = new Queue<GameObject>();
            
            var pool = _effectPools[poolName];
            
            // Buscar un efecto disponible en el pool
            while (pool.Count > 0)
            {
                var pooled = pool.Dequeue();
                if (pooled != null)
                {
                    pooled.SetActive(true);
                    var ps = pooled.GetComponentInChildren<ParticleSystem>();
                    if (ps != null)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.Play(true);
                    }
                    return pooled;
                }
            }
            
            // Crear nuevo si no hay disponibles
            if (prefab != null)
                return Object.Instantiate(prefab);
            
            return null;
        }

        /// <summary>
        /// Retorna un efecto al pool para reutilización
        /// </summary>
        private static void ReturnEffectToPool(string poolName, GameObject effect, float delay)
        {
            if (effect == null) return;
            
            // Usar coroutine del objeto para delay
            var returner = effect.AddComponent<EffectPoolReturner>();
            returner.Setup(poolName, delay, _effectPools, _poolLimits);
        }

        /// <summary>
        /// Componente helper para retornar efectos al pool después de un delay
        /// </summary>
        private class EffectPoolReturner : MonoBehaviour
        {
            private string _poolName;
            private float _delay;
            private Dictionary<string, Queue<GameObject>> _pools;
            private Dictionary<string, int> _limits;
            
            public void Setup(string poolName, float delay, Dictionary<string, Queue<GameObject>> pools, Dictionary<string, int> limits)
            {
                _poolName = poolName;
                _delay = delay;
                _pools = pools;
                _limits = limits;
                Invoke(nameof(ReturnToPool), delay);
            }
            
            private void ReturnToPool()
            {
                if (gameObject == null) return;
                
                gameObject.SetActive(false);
                
                if (_pools.ContainsKey(_poolName))
                {
                    var pool = _pools[_poolName];
                    // Respetar el límite del pool
                    int limit = _limits.ContainsKey(_poolName) ? _limits[_poolName] : 20;
                    if (pool.Count < limit)
                    {
                        pool.Enqueue(gameObject);
                    }
                    else
                    {
                        Object.Destroy(gameObject);
                    }
                }
                else
                {
                    Object.Destroy(gameObject);
                }
                
                Destroy(this); // Remover este componente
            }
        }

        /// <summary>
        /// Carga un prefab desde Resources con cache
        /// </summary>
        private static GameObject GetCachedPrefab(ref GameObject cache, string path)
        {
            if (cache == null)
            {
                cache = Resources.Load<GameObject>(path);
            }
            return cache;
        }

        /// <summary>
        /// Intenta obtener un efecto de impacto desde prefabs KCISA
        /// </summary>
        private static GameObject GetImpactEffectPrefab()
        {
            var prefab = GetCachedPrefab(ref _impactEffectPrefab, "KoreanTraditionalPattern_Effect/Prefabs/Hit/Hit01-03");
            if (prefab != null) return prefab;

            prefab = GetCachedPrefab(ref _impactEffectPrefab, "Prefabs/ImpactEffect");
            if (prefab != null) return prefab;

            prefab = GetCachedPrefab(ref _impactEffectPrefab, "KoreanTraditionalPattern_Effect/Prefabs/Fly/Fly08-04");
            return prefab;
        }

        /// <summary>
        /// Intenta obtener un efecto de explosión
        /// </summary>
        private static GameObject GetExplosionEffectPrefab()
        {
            var prefab = GetCachedPrefab(ref _explosionEffectPrefab, "KoreanTraditionalPattern_Effect/Prefabs/Hit/Hit01-04");
            if (prefab != null) return prefab;

            prefab = GetCachedPrefab(ref _explosionEffectPrefab, "Prefabs/ExplosionEffect");
            return prefab;
        }

        public static void SpawnImpactEffect(Vector3 pos)
        {
            var prefab = GetImpactEffectPrefab();
            if (prefab != null)
            {
                // Usar pooling en lugar de Instantiate directo
                var go = GetPooledEffect("Impact", prefab);
                if (go != null)
                {
                    go.transform.position = pos;
                    go.transform.localScale = Vector3.one * 0.8f;
                    go.transform.rotation = Quaternion.identity;
                    
                    var ps = go.GetComponentInChildren<ParticleSystem>();
                    float duration = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 0.5f;
                    ReturnEffectToPool("Impact", go, duration);
                }
                return;
            }

            CreateProceduralImpactEffect(pos);
        }

        private static void CreateProceduralImpactEffect(Vector3 pos)
        {
            var go = new GameObject("ImpactEffect");
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            
            // Stop immediately to allow configuration
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            main.playOnAwake = false;
            main.duration = 0.2f;
            main.startLifetime = 0.3f;
            main.startSize = 0.5f;
            main.startColor = new Color(1f, 0.8f, 0.2f, 1f);
            main.maxParticles = 20;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);

            ps.Play();
            Object.Destroy(go, 0.5f);
        }

        public static void SpawnDeathEffect(Vector3 pos)
        {
            var prefab = GetCachedPrefab(ref _debrisEffectPrefab, "KoreanTraditionalPattern_Effect/Prefabs/Hit/Hit01-02");
            
            if (prefab != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var go = Object.Instantiate(prefab, pos + Random.insideUnitSphere * 0.5f, Random.rotation);
                    go.transform.localScale = Vector3.one * Random.Range(0.3f, 0.6f);
                    
                    var rb = go.GetComponent<Rigidbody>();
                    if (rb == null) rb = go.AddComponent<Rigidbody>();
                    rb.linearVelocity = Random.insideUnitSphere * 5f + Vector3.up * 3f;
                    
                    var ps = go.GetComponentInChildren<ParticleSystem>();
                    float duration = ps != null ? ps.main.duration + 1f : 1.5f;
                    Object.Destroy(go, duration);
                }
                return;
            }

            for (int i = 0; i < 8; i++)
            {
                CreateProceduralDebris(pos + Random.insideUnitSphere * 0.5f);
            }
        }

        private static void CreateProceduralDebris(Vector3 pos)
        {
            var go = new GameObject("Debris");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.3f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1f;
            main.startLifetime = 1.5f;
            main.startSize = Random.Range(0.1f, 0.3f);
            main.startColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            main.maxParticles = 1;
            main.playOnAwake = true;
            main.gravityModifier = 1f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.x = Random.Range(-3f, 3f);
            velocityOverLifetime.y = Random.Range(2f, 5f);
            velocityOverLifetime.z = Random.Range(-3f, 3f);

            ps.Play();
            Object.Destroy(go, 1.5f);
        }

        public static void SpawnShieldEffect(Transform parent)
        {
            if (parent == null) return;

            var prefab = GetCachedPrefab(ref _shieldEffectPrefab, "KoreanTraditionalPattern_Effect/Prefabs/Fly/Fly01-01");
            if (prefab != null)
            {
                var go = Object.Instantiate(prefab, parent);
                go.transform.localPosition = Vector3.up;
                go.transform.localScale = Vector3.one * 2f;
                
                var ps = go.GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.duration = 3f;
                    main.loop = false;
                }
                
                Object.Destroy(go, 3f);
                return;
            }

            CreateProceduralShieldEffect(parent);
        }

        private static void CreateProceduralShieldEffect(Transform parent)
        {
            var go = new GameObject("ShieldEffect");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.up;
            go.transform.localScale = Vector3.one * 2.5f;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 3f;
            main.startLifetime = 0.5f;
            main.startSize = 2.5f;
            main.startColor = new Color(0.3f, 0.6f, 1f, 0.5f);
            main.maxParticles = 50;
            main.playOnAwake = true;
            main.loop = true;

            var emission = ps.emission;
            emission.rateOverTime = 15f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;
            shape.radiusThickness = 0f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.3f, 0.6f, 1f), 0f), new GradientColorKey(new Color(0.1f, 0.3f, 0.8f), 1f) },
                new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);

            ps.Play();
            Object.Destroy(go, 3f);
        }

        public static void SpawnDashEffect(Vector3 pos)
        {
            var prefab = GetCachedPrefab(ref _dashEffectPrefab, "KoreanTraditionalPattern_Effect/Prefabs/Fly/Fly08-04");
            if (prefab != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    // Usar pooling para efectos de dash
                    var go = GetPooledEffect("Dash", prefab);
                    if (go != null)
                    {
                        go.transform.position = pos + Vector3.up * 0.5f + Random.insideUnitSphere * 0.3f;
                        go.transform.rotation = Quaternion.identity;
                        go.transform.localScale = Vector3.one * 0.5f;
                        
                        var ps = go.GetComponentInChildren<ParticleSystem>();
                        float duration = ps != null ? ps.main.duration + 0.5f : 0.4f;
                        ReturnEffectToPool("Dash", go, duration);
                    }
                }
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                CreateProceduralDashParticle(pos + Vector3.up * 0.5f + Random.insideUnitSphere * 0.3f);
            }
        }

        private static void CreateProceduralDashParticle(Vector3 pos)
        {
            var go = new GameObject("DashParticle");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.15f;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.4f;
            main.startLifetime = 0.4f;
            main.startSize = 0.15f;
            main.startColor = new Color(0.8f, 0.8f, 1f, 0.6f);
            main.maxParticles = 1;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.8f, 0.8f, 1f), 0f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);

            ps.Play();
            Object.Destroy(go, 0.4f);
        }

        public static void SpawnMeleeEffect(Vector3 pos, Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;

            var prefab = GetCachedPrefab(ref _meleeEffectPrefab, "KoreanTraditionalPattern_Effect/Prefabs/Slash/Slash01-01");
            if (prefab != null)
            {
                // Usar pooling para efectos melee
                var go = GetPooledEffect("Melee", prefab);
                if (go != null)
                {
                    go.transform.position = pos + dir * 0.5f;
                    go.transform.rotation = Quaternion.LookRotation(dir);
                    go.transform.localScale = new Vector3(2f, 0.3f, 0.3f);
                    
                    var ps = go.GetComponentInChildren<ParticleSystem>();
                    float duration = ps != null ? ps.main.duration + 0.2f : 0.2f;
                    ReturnEffectToPool("Melee", go, duration);
                }
                return;
            }

            CreateProceduralMeleeEffect(pos, dir);
        }

        private static void CreateProceduralMeleeEffect(Vector3 pos, Vector3 dir)
        {
            var go = new GameObject("MeleeEffect");
            go.transform.position = pos + dir * 0.5f;
            go.transform.localScale = new Vector3(2f, 0.3f, 0.3f);
            go.transform.rotation = Quaternion.LookRotation(dir);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            
            // Stop immediately to allow configuration
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            main.playOnAwake = false;
            main.duration = 0.2f;
            main.startLifetime = 0.2f;
            main.startSize = 0.3f;
            main.startColor = new Color(1f, 1f, 1f, 0.7f);
            main.maxParticles = 10;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(2f, 0.3f, 0.3f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);

            // Ahora sí iniciar el sistema después de toda la configuración
            ps.Play();
            Object.Destroy(go, 0.2f);
        }

        public static void SpawnExplosionEffect(Vector3 pos, float scale = 1f)
        {
            var prefab = GetExplosionEffectPrefab();
            if (prefab != null)
            {
                var go = Object.Instantiate(prefab, pos, Quaternion.identity);
                go.name = "ExplosionEffect";
                go.transform.localScale = Vector3.one * scale;
                
                var expand = go.AddComponent<ExplosionExpand>();
                expand.targetScale = scale * 2f;
                expand.duration = 0.4f;
                
                var ps = go.GetComponentInChildren<ParticleSystem>();
                float duration = ps != null ? ps.main.duration + 1f : 1f;
                Object.Destroy(go, duration);
                
                SpawnExplosionDebris(pos, scale);
                return;
            }

            CreateProceduralExplosionEffect(pos, scale);
        }

        private static void SpawnExplosionDebris(Vector3 pos, float scale)
        {
            var prefab = GetCachedPrefab(ref _debrisEffectPrefab, "KoreanTraditionalPattern_Effect/Prefabs/Hit/Hit01-02");
            
            for (int i = 0; i < 6; i++)
            {
                GameObject debris;
                if (prefab != null)
                {
                    debris = Object.Instantiate(prefab, pos + Random.insideUnitSphere * 0.3f, Random.rotation);
                }
                else
                {
                    debris = new GameObject("Debris");
                    debris.transform.position = pos + Random.insideUnitSphere * 0.3f;
                    
                    var ps = debris.AddComponent<ParticleSystem>();
                    var main = ps.main;
                    main.duration = 1f;
                    main.startLifetime = 1.5f;
                    main.startSize = Random.Range(0.05f, 0.2f);
                    main.startColor = new Color(0.4f, 0.2f, 0.1f, 1f);
                    main.maxParticles = 1;
                    main.playOnAwake = true;
                    main.gravityModifier = 1f;
                    
                    var emission = ps.emission;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
                    
                    ps.Play();
                }
                
                debris.transform.localScale = Vector3.one * Random.Range(0.05f, 0.2f) * scale;
                
                var rb = debris.GetComponent<Rigidbody>();
                if (rb == null) rb = debris.AddComponent<Rigidbody>();
                rb.linearVelocity = Random.insideUnitSphere * 8f + Vector3.up * 3f;
                
                Object.Destroy(debris, 1.5f);
            }
        }

        private static void CreateProceduralExplosionEffect(Vector3 pos, float scale)
        {
            var go = new GameObject("ExplosionEffect");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.1f;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.4f;
            main.startLifetime = 0.5f;
            main.startSize = 0.1f;
            main.startColor = new Color(1f, 0.4f, 0.1f, 1f);
            main.maxParticles = 50;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.1f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 2f)
            ));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[] { 
                    new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.3f, 0.1f, 0.05f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);

            var expand = go.AddComponent<ExplosionExpand>();
            expand.targetScale = scale * 2f;
            expand.duration = 0.4f;

            ps.Play();
            Object.Destroy(go, 1f);

            for (int i = 0; i < 12; i++)
            {
                CreateProceduralDebris(pos + Random.insideUnitSphere * 0.3f);
            }
        }
    }

    public class ExplosionExpand : MonoBehaviour
    {
        public float targetScale = 2f;
        public float duration = 0.4f;
        private float startTime;

        void Start()
        {
            startTime = Time.time;
        }

        void Update()
        {
            float t = (Time.time - startTime) / duration;
            if (t >= 1f)
            {
                var ps = GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var emission = ps.emission;
                    emission.enabled = false;
                }
                return;
            }

            float scaleT = Mathf.Sin(t * Mathf.PI);
            transform.localScale = Vector3.one * Mathf.Lerp(0.1f, targetScale, scaleT);
        }
    }
}

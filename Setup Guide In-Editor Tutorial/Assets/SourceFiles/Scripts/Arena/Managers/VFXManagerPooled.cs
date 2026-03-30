using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// VFX Manager with Object Pooling support.
    /// Reemplaza instanciación directa de efectos visuales con pooling para mejor performance.
    /// </summary>
    public static class VFXManagerPooled
    {
        private const string IMPACT_POOL_TAG = "ImpactEffect";
        private const string DEATH_POOL_TAG = "DeathEffect";
        private const string SHIELD_POOL_TAG = "ShieldEffect";
        private const string DASH_POOL_TAG = "DashEffect";
        private const string MELEE_POOL_TAG = "MeleeEffect";

        /// <summary>
        /// Spawnea un efecto de impacto usando pooling
        /// </summary>
        public static void SpawnImpact(Vector3 position)
        {
            if (GenericObjectPool.Instance == null || !GenericObjectPool.Instance.HasPool(IMPACT_POOL_TAG))
            {
                // Fallback a método anterior si no hay pool
                VFXManager.SpawnImpactEffect(position);
                return;
            }

            var effect = GenericObjectPool.Instance.GetFromPool(IMPACT_POOL_TAG, position, Quaternion.identity);
            if (effect != null)
            {
                // Auto-return después de duración
                var autoReturn = effect.GetComponent<AutoReturnToPool>();
                if (autoReturn == null)
                    autoReturn = effect.AddComponent<AutoReturnToPool>();
                autoReturn.Setup(0.3f);
            }
        }

        /// <summary>
        /// Spawnea un efecto de muerte usando pooling
        /// </summary>
        public static void SpawnDeath(Vector3 position)
        {
            if (GenericObjectPool.Instance == null || !GenericObjectPool.Instance.HasPool(DEATH_POOL_TAG))
            {
                VFXManager.SpawnDeathEffect(position);
                return;
            }

            var effect = GenericObjectPool.Instance.GetFromPool(DEATH_POOL_TAG, position, Quaternion.identity);
            if (effect != null)
            {
                var autoReturn = effect.GetComponent<AutoReturnToPool>();
                if (autoReturn == null)
                    autoReturn = effect.AddComponent<AutoReturnToPool>();
                autoReturn.Setup(1.5f);
            }
        }

        /// <summary>
        /// Spawnea un efecto de escudo usando pooling
        /// </summary>
        public static void SpawnShield(Transform parent, float duration = 3f)
        {
            if (parent == null) return;
            
            if (GenericObjectPool.Instance == null || !GenericObjectPool.Instance.HasPool(SHIELD_POOL_TAG))
            {
                VFXManager.SpawnShieldEffect(parent);
                return;
            }

            var effect = GenericObjectPool.Instance.GetFromPool(SHIELD_POOL_TAG, parent.position, Quaternion.identity);
            if (effect != null)
            {
                effect.transform.SetParent(parent);
                
                var autoReturn = effect.GetComponent<AutoReturnToPool>();
                if (autoReturn == null)
                    autoReturn = effect.AddComponent<AutoReturnToPool>();
                autoReturn.Setup(duration);
            }
        }

        /// <summary>
        /// Spawnea un efecto de dash usando pooling
        /// </summary>
        public static void SpawnDash(Vector3 position)
        {
            if (GenericObjectPool.Instance == null || !GenericObjectPool.Instance.HasPool(DASH_POOL_TAG))
            {
                VFXManager.SpawnDashEffect(position);
                return;
            }

            var effect = GenericObjectPool.Instance.GetFromPool(DASH_POOL_TAG, position, Quaternion.identity);
            if (effect != null)
            {
                var autoReturn = effect.GetComponent<AutoReturnToPool>();
                if (autoReturn == null)
                    autoReturn = effect.AddComponent<AutoReturnToPool>();
                autoReturn.Setup(0.4f);
            }
        }

        /// <summary>
        /// Spawnea un efecto de melee usando pooling
        /// </summary>
        public static void SpawnMelee(Vector3 position, Vector3 direction)
        {
            if (GenericObjectPool.Instance == null || !GenericObjectPool.Instance.HasPool(MELEE_POOL_TAG))
            {
                VFXManager.SpawnMeleeEffect(position, direction);
                return;
            }

            var effect = GenericObjectPool.Instance.GetFromPool(MELEE_POOL_TAG, position, Quaternion.LookRotation(direction));
            if (effect != null)
            {
                var autoReturn = effect.GetComponent<AutoReturnToPool>();
                if (autoReturn == null)
                    autoReturn = effect.AddComponent<AutoReturnToPool>();
                autoReturn.Setup(0.2f);
            }
        }
    }

    /// <summary>
    /// Componente que retorna automáticamente un objeto al pool después de un tiempo
    /// </summary>
    public class AutoReturnToPool : MonoBehaviour
    {
        private float _returnTime;
        private bool _isSetup = false;

        public void Setup(float duration)
        {
            _returnTime = Time.time + duration;
            _isSetup = true;
        }

        private void Update()
        {
            if (!_isSetup) return;
            
            if (Time.time >= _returnTime)
            {
                ReturnToPool();
                _isSetup = false;
            }
        }

        private void ReturnToPool()
        {
            GenericObjectPool.Instance?.ReturnToPool(gameObject);
        }
    }
}

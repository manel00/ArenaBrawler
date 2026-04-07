using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Proyectil de arma - separado de CombatLogic.cs para mejor organización
    /// </summary>
    public class WeaponProjectile : PooledObject
    {
        public ArenaCombatant owner;
        public WeaponData weaponData;
        public float lifeTime = 4f;

        private float _spawnTime;

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            _spawnTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - _spawnTime > lifeTime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == (owner != null ? owner.gameObject : null))
                return;

            // OPTIMIZACIÓN: Usar PhysicsLayers.IsEnvironment() para detección de ambiente
            bool isEnvironment = PhysicsLayers.IsEnvironment(other.gameObject);

            var target = other.GetComponent<ArenaCombatant>();
            if (target == null) target = other.GetComponentInParent<ArenaCombatant>();

            if (target != null && owner != null && target != owner && target.teamId != owner.teamId && target.IsAlive)
            {
                float damage = weaponData != null ? weaponData.RollDamage() : 10f;
                target.TakeDamage(damage * owner.damageMultiplier, owner);

                if (weaponData != null && weaponData.weaponName.ToLower().Contains("shotgun"))
                {
                    Vector3 knockDir = (target.transform.position - owner.transform.position).normalized;
                    knockDir.y = 0;
                    target.ApplyKnockback(knockDir * 250f);
                }

                if (weaponData != null && weaponData.splashRadius > 0f)
                {
                    AreaDamageHelper.ApplyFlatAreaDamage(
                        center: target.transform.position,
                        radius: weaponData.splashRadius,
                        damage: weaponData.RollSplashDamage(),
                        owner: owner,
                        directTarget: target
                    );
                }
            }
            else if (isEnvironment)
            {
                AreaDamageHelper.ApplyAreaDamageToEnemies(transform.position, 2f, 5f, owner);
            }

            VFXManager.SpawnImpactEffect(transform.position);
            ReturnToPool();
        }

        private void OnDrawGizmosSelected()
        {
            if (weaponData != null && weaponData.splashRadius > 0f)
            {
                AreaDamageHelper.DrawDebugGizmos(transform.position, weaponData.splashRadius, new Color(1f, 0.5f, 0f, 0.3f));
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Utilidad centralizada para aplicar daño de área (splash damage).
    /// Reemplaza la lógica duplicada en múltiples clases de proyectiles.
    /// </summary>
    public static class AreaDamageHelper
    {
        /// <summary>
        /// Aplica daño de área en un radio específico desde un punto central.
        /// </summary>
        /// <param name="center">Centro del área de daño</param>
        /// <param name="radius">Radio del daño</param>
        /// <param name="minDamage">Daño mínimo (en el borde del radio)</param>
        /// <param name="maxDamage">Daño máximo (en el centro)</param>
        /// <param name="owner">Combatiente que causó el daño</param>
        /// <param name="damageType">Tipo de daño</param>
        /// <param name="directTarget">Objetivo directo (excluido del daño de área)</param>
        /// <param name="alreadyDamaged">Set de objetivos ya dañados (para evitar duplicados)</param>
        /// <returns>Número de combatientes dañados</returns>
        public static int ApplyAreaDamage(
            Vector3 center,
            float radius,
            float minDamage,
            float maxDamage,
            ArenaCombatant owner,
            DamageType damageType = DamageType.Normal,
            ArenaCombatant directTarget = null,
            HashSet<ArenaCombatant> alreadyDamaged = null)
        {
            int hitCount = 0;
            // OPTIMIZACIÓN: Usar PhysicsLayers.DamageableMask en lugar de ~0 (todas las capas)
            Collider[] hits = Physics.OverlapSphere(center, radius, PhysicsLayers.DamageableMask, QueryTriggerInteraction.Ignore);
            
            foreach (var hit in hits)
            {
                var combatant = hit.GetComponentInParent<ArenaCombatant>();
                if (!IsValidTarget(combatant, owner, directTarget, alreadyDamaged))
                    continue;

                // Calcular daño basado en distancia
                float distance = Vector3.Distance(center, combatant.transform.position);
                float damageRatio = 1f - Mathf.Clamp01(distance / radius);
                float damage = Mathf.Lerp(minDamage, maxDamage, damageRatio);
                
                // Aplicar multiplicador de daño del owner
                if (owner != null)
                    damage *= owner.damageMultiplier;

                combatant.TakeDamage(damage, owner, damageType);
                
                // Registrar en el set de dañados si existe
                alreadyDamaged?.Add(combatant);
                hitCount++;
            }
            
            return hitCount;
        }

        /// <summary>
        /// Aplica daño de área con daño fijo (sin caída por distancia).
        /// </summary>
        public static int ApplyFlatAreaDamage(
            Vector3 center,
            float radius,
            float damage,
            ArenaCombatant owner,
            DamageType damageType = DamageType.Normal,
            ArenaCombatant directTarget = null)
        {
            int hitCount = 0;
            // OPTIMIZACIÓN: Usar PhysicsLayers.DamageableMask en lugar de ~0
            Collider[] hits = Physics.OverlapSphere(center, radius, PhysicsLayers.DamageableMask, QueryTriggerInteraction.Ignore);
            
            foreach (var hit in hits)
            {
                var combatant = hit.GetComponentInParent<ArenaCombatant>();
                if (!IsValidTarget(combatant, owner, directTarget, null))
                    continue;

                float finalDamage = damage * (owner != null ? owner.damageMultiplier : 1f);
                combatant.TakeDamage(finalDamage, owner, damageType);
                hitCount++;
            }
            
            return hitCount;
        }

        /// <summary>
        /// Aplica daño de área con empuje (knockback).
        /// </summary>
        public static int ApplyAreaDamageWithKnockback(
            Vector3 center,
            float radius,
            float damage,
            float knockbackForce,
            ArenaCombatant owner,
            DamageType damageType = DamageType.Normal,
            float knockbackUpwardModifier = 0.3f)
        {
            int hitCount = 0;
            // OPTIMIZACIÓN: Usar PhysicsLayers.DamageableMask
            Collider[] hits = Physics.OverlapSphere(center, radius, PhysicsLayers.DamageableMask, QueryTriggerInteraction.Ignore);
            
            foreach (var hit in hits)
            {
                var combatant = hit.GetComponentInParent<ArenaCombatant>();
                if (!IsValidTarget(combatant, owner, null, null))
                    continue;

                // Aplicar daño
                float finalDamage = damage * (owner != null ? owner.damageMultiplier : 1f);
                combatant.TakeDamage(finalDamage, owner, damageType);

                // Aplicar empuje
                var rb = combatant.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 knockbackDir = (combatant.transform.position - center).normalized;
                    knockbackDir.y = knockbackUpwardModifier;
                    
                    float distance = Vector3.Distance(center, combatant.transform.position);
                    float forceMultiplier = 1f - Mathf.Clamp01(distance / radius);
                    
                    rb.AddForce(knockbackDir * knockbackForce * forceMultiplier, ForceMode.Impulse);
                }
                
                hitCount++;
            }
            
            return hitCount;
        }

        /// <summary>
        /// Aplica daño de área a enemigos en un radio (versión simplificada para armas).
        /// </summary>
        public static void ApplyAreaDamageToEnemies(
            Vector3 center,
            float radius,
            float damage,
            ArenaCombatant owner)
        {
            // OPTIMIZACIÓN: Usar PhysicsLayers.CombatantMask para solo enemigos y jugadores
            Collider[] hits = Physics.OverlapSphere(center, radius, PhysicsLayers.CombatantMask, QueryTriggerInteraction.Ignore);
            
            foreach (var hit in hits)
            {
                var combatant = hit.GetComponentInParent<ArenaCombatant>();
                if (combatant == null || !combatant.IsAlive || combatant == owner) continue;
                if (owner != null && combatant.teamId == owner.teamId) continue;

                float finalDamage = damage * (owner != null ? owner.damageMultiplier : 1f);
                combatant.TakeDamage(finalDamage, owner);
            }
        }

        /// <summary>
        /// Valida si un combatiente es un objetivo válido para daño de área.
        /// </summary>
        private static bool IsValidTarget(
            ArenaCombatant combatant,
            ArenaCombatant owner,
            ArenaCombatant directTarget,
            HashSet<ArenaCombatant> alreadyDamaged)
        {
            if (combatant == null) return false;
            if (!combatant.IsAlive) return false;
            if (combatant == owner) return false;
            if (combatant == directTarget) return false;
            if (owner != null && combatant.teamId == owner.teamId) return false;
            if (alreadyDamaged != null && alreadyDamaged.Contains(combatant)) return false;
            
            return true;
        }

        /// <summary>
        /// Dibuja gizmos de debug para el área de daño.
        /// </summary>
        public static void DrawDebugGizmos(Vector3 center, float radius, Color color)
        {
#if UNITY_EDITOR
            Gizmos.color = color;
            Gizmos.DrawWireSphere(center, radius);
            Gizmos.color = new Color(color.r, color.g, color.b, 0.3f);
            Gizmos.DrawSphere(center, radius);
#endif
        }
    }
}

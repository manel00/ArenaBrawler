using UnityEngine;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Utilidad centralizada para encontrar enemigos cercanos.
    /// Elimina código duplicado entre BossController, DogController, BotController.
    /// </summary>
    public static class EnemyFinder
    {
        /// <summary>
        /// Encuentra el enemigo más cercano al owner, excluyendo aliados y al propio owner.
        /// Utiliza SpatialGrid para búsqueda O(1) en lugar de O(n).
        /// </summary>
        public static ArenaCombatant FindNearest(
            Vector3 position, 
            ArenaCombatant owner, 
            float maxDistance,
            int? teamId = null)
        {
            // Usar SpatialGrid para búsqueda optimizada O(1) vs O(n)
            int teamToIgnore = owner?.teamId ?? teamId ?? -1;
            return SpatialGrid.FindNearest(position, maxDistance, teamToIgnore, true);
        }

        /// <summary>
        /// Verifica si hay un boss cercano dentro de la distancia especificada.
        /// Utiliza SpatialGrid para búsqueda optimizada.
        /// </summary>
        public static bool HasBossNearby(Vector3 position, float maxDistance, out ArenaCombatant nearestBoss)
        {
            nearestBoss = null;
            float nearestDist = float.MaxValue;
            
            // Usar SpatialGrid para obtener candidatos cercanos
            var candidates = SpatialGrid.FindAllInRadius(position, maxDistance, -1, true);
            
            foreach (var c in candidates)
            {
                if (c == null || !c.IsAlive) continue;
                
                // Verificar si es boss por nombre o tag
                bool isBoss = c.name.Contains("T-Rex") || c.CompareTag("Boss");
                if (!isBoss) continue;
                
                float distance = Vector3.Distance(position, c.transform.position);
                if (distance < maxDistance && distance < nearestDist)
                {
                    nearestDist = distance;
                    nearestBoss = c;
                }
            }
            
            return nearestBoss != null;
        }

        /// <summary>
        /// Aplica velocidad horizontal a un Rigidbody, manteniendo la velocidad vertical.
        /// </summary>
        public static void ApplyHorizontalVelocity(this Rigidbody rb, Vector3 flatVelocity)
        {
            if (rb == null) return;
            Vector3 vel = rb.linearVelocity;
            vel.x = flatVelocity.x;
            vel.z = flatVelocity.z;
            rb.linearVelocity = vel;
        }

        /// <summary>
        /// Detecta si hay un borde (edge) en la dirección de movimiento.
        /// Retorna true si es seguro moverse (no hay borde).
        /// </summary>
        public static bool CheckEdgeSafe(Vector3 position, Vector3 direction, float lookAhead = 1.2f, float rayDistance = 2.5f)
        {
            Vector3 groundCheckPos = position + direction.normalized * lookAhead + Vector3.up * 0.5f;
            return Physics.Raycast(groundCheckPos, Vector3.down, rayDistance);
        }

        /// <summary>
        /// Rota suavemente el transform hacia una dirección objetivo.
        /// </summary>
        public static void RotateTowards(Transform transform, Vector3 targetPosition, float rotationSpeed, bool flattenY = true)
        {
            Vector3 lookDir = flattenY 
                ? Vector3.Scale(targetPosition - transform.position, new Vector3(1, 0, 1))
                : targetPosition - transform.position;
                
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }
}

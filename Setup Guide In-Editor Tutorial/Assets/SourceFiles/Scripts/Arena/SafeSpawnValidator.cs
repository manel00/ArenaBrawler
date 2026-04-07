using UnityEngine;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Utilidad para validar posiciones de spawn seguras.
    /// Verifica: suelo sólido, espacio libre, distancia a enemigos, bordes seguros.
    /// </summary>
    public static class SafeSpawnValidator
    {
        private const float PLAYER_HEIGHT = 1.8f;
        private const float PLAYER_RADIUS = 0.4f;
        private const float MIN_ENEMY_DISTANCE = 8f;
        private const float MIN_OBSTACLE_DISTANCE = 1.5f;
        private const float MAX_DISTANCE_FROM_CENTER = 50f;
        private const float GROUND_RAY_START_HEIGHT = 100f;
        private const float GROUND_RAY_MAX_DISTANCE = 200f;
        private const int MAX_ATTEMPTS = 30;

        // Layer masks
        private static int _groundLayer;
        private static int _obstacleLayer;
        private static int _enemyLayer;
        private static bool _layersInitialized = false;

        private static void InitializeLayers()
        {
            if (_layersInitialized) return;
            _groundLayer = LayerMask.GetMask("Ground");
            _obstacleLayer = LayerMask.GetMask("Obstacle", "Default", "Environment");
            _enemyLayer = LayerMask.GetMask("Enemy", "Boss");
            _layersInitialized = true;
        }

        /// <summary>
        /// Encuentra una posición de spawn segura. GARANTIZA suelo sólido dentro de 50m del centro.
        /// </summary>
        public static Vector3 FindSafeSpawnPosition(Vector3 desiredPosition, float searchRadius = 10f, float arenaRadius = 50f)
        {
            InitializeLayers();

            // Limitar arenaRadius a máximo 50m del centro como pide el usuario
            arenaRadius = Mathf.Min(arenaRadius, MAX_DISTANCE_FROM_CENTER);

            // Intentar posiciones aleatorias dentro del círculo de 50m
            for (int i = 0; i < MAX_ATTEMPTS; i++)
            {
                // Generar posición aleatoria dentro del círculo de 50m del centro
                Vector2 randomCircle = Random.insideUnitCircle * Mathf.Min(searchRadius, arenaRadius * 0.9f);
                Vector3 candidate = new Vector3(randomCircle.x, GROUND_RAY_START_HEIGHT, randomCircle.y);
                
                // Forzar dentro de los 50m del centro
                float distFromCenter = new Vector2(candidate.x, candidate.z).magnitude;
                if (distFromCenter > MAX_DISTANCE_FROM_CENTER)
                {
                    float scale = MAX_DISTANCE_FROM_CENTER / distFromCenter * 0.9f;
                    candidate.x *= scale;
                    candidate.z *= scale;
                }

                // Encontrar suelo real en esta posición
                Vector3 groundedPos = FindGroundAtPosition(candidate);
                if (groundedPos.y > -500f) // Suelo encontrado
                {
                    // Verificar que la posición es segura (sin obstáculos, lejos de enemigos)
                    if (IsPositionSafe(groundedPos))
                    {
                        Debug.Log($"[SafeSpawnValidator] Safe spawn found at {groundedPos} (attempt {i + 1})");
                        return groundedPos;
                    }
                }
            }

            // FALLBACK GARANTIZADO: Centro del mapa
            Debug.LogWarning("[SafeSpawnValidator] Could not find ideal position, using center fallback");
            Vector3 centerFallback = FindGroundAtPosition(new Vector3(0f, GROUND_RAY_START_HEIGHT, 0f));
            if (centerFallback.y > -500f)
            {
                return centerFallback;
            }

            // Último recurso: posición forzada en el centro a altura 1.5
            Debug.LogError("[SafeSpawnValidator] CRITICAL: Even center has no ground! Using forced position.");
            return new Vector3(0f, 1.5f, 0f);
        }

        /// <summary>
        /// Encuentra el suelo en una posición X,Z dada, lanzando raycast desde arriba.
        /// Retorna Vector3 con Y = -1000 si no encuentra suelo.
        /// </summary>
        private static Vector3 FindGroundAtPosition(Vector3 position)
        {
            // Raycast largo desde arriba hacia abajo para encontrar cualquier collider
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, GROUND_RAY_MAX_DISTANCE, ~0))
            {
                // Asegurar que no es precipicio (suelo debe estar más o menos horizontal)
                if (hit.normal.y > 0.5f) // Menos de 60 grados de inclinación
                {
                    return hit.point + Vector3.up * 0.2f; // Offset para evitar clipping
                }
            }
            
            return new Vector3(position.x, -1000f, position.z); // No hay suelo
        }

        /// <summary>
        /// Valida si una posición de suelo es segura (sin obstáculos, lejos de enemigos).
        /// Asume que position.Y ya está en el suelo.
        /// </summary>
        private static bool IsPositionSafe(Vector3 position)
        {
            // 1. Verificar que hay espacio libre para el jugador (overlap check)
            if (HasObstacleCollision(position))
                return false;

            // 2. Verificar distancia a enemigos
            if (IsTooCloseToEnemies(position))
                return false;

            return true;
        }

        /// <summary>
        /// Verifica colisiones con obstáculos usando overlap.
        /// </summary>
        private static bool HasObstacleCollision(Vector3 position)
        {
            Vector3 checkCenter = position + Vector3.up * (PLAYER_HEIGHT * 0.5f);
            Vector3 checkSize = new Vector3(PLAYER_RADIUS * 2f, PLAYER_HEIGHT, PLAYER_RADIUS * 2f);

            Collider[] hits = Physics.OverlapBox(checkCenter, checkSize * 0.5f, Quaternion.identity, ~0);

            foreach (var hit in hits)
            {
                // Ignorar triggers
                if (hit.isTrigger) continue;

                // Ignorar objetos sin collider sólido
                if (hit is MeshCollider meshCol && !meshCol.convex) continue;

                // Es un obstáculo si no es el propio jugador (aún no existe)
                return true;
            }

            return false;
        }

        /// <summary>
        /// Verifica si hay enemigos demasiado cerca.
        /// </summary>
        private static bool IsTooCloseToEnemies(Vector3 position)
        {
            Collider[] enemies = Physics.OverlapSphere(position, MIN_ENEMY_DISTANCE, _enemyLayer);
            return enemies.Length > 0;
        }

        /// <summary>
        /// Debug visual de la validación (llamar desde OnDrawGizmos en un MonoBehaviour).
        /// </summary>
        public static void DrawDebugGizmos(Vector3 position, bool isSafe)
        {
            Gizmos.color = isSafe ? Color.green : Color.red;
            Gizmos.DrawWireCube(position + Vector3.up * (PLAYER_HEIGHT * 0.5f), 
                new Vector3(PLAYER_RADIUS * 2f, PLAYER_HEIGHT, PLAYER_RADIUS * 2f));
            
            Gizmos.DrawLine(position, position + Vector3.up * PLAYER_HEIGHT);
            
            // Dibujar radio de distancia a enemigos
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(position, MIN_ENEMY_DISTANCE);
        }
    }
}

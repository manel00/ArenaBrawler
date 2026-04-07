using System.Collections.Generic;
using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de Grid Espacial para optimizar búsqueda de combatientes.
    /// Divide el mundo en celdas de tamaño fijo para búsquedas O(1) en lugar de O(n).
    /// </summary>
    public static class SpatialGrid
    {
        // Configuración del grid
        private const float CELL_SIZE = 15f; // 15m por celda
        private static readonly Dictionary<Vector2Int, HashSet<ArenaCombatant>> _cells = new Dictionary<Vector2Int, HashSet<ArenaCombatant>>();
        private static readonly Dictionary<ArenaCombatant, Vector2Int> _combatantCells = new Dictionary<ArenaCombatant, Vector2Int>();
        
        /// <summary>
        /// Registra un combatiente en el grid
        /// </summary>
        public static void RegisterCombatant(ArenaCombatant combatant)
        {
            if (combatant == null) return;
            
            Vector2Int cell = GetCell(combatant.transform.position);
            
            // Remover de celda anterior si existe
            if (_combatantCells.TryGetValue(combatant, out var oldCell))
            {
                if (_cells.TryGetValue(oldCell, out var oldSet))
                {
                    oldSet.Remove(combatant);
                    if (oldSet.Count == 0)
                        _cells.Remove(oldCell);
                }
            }
            
            // Agregar a nueva celda
            if (!_cells.TryGetValue(cell, out var set))
            {
                set = new HashSet<ArenaCombatant>();
                _cells[cell] = set;
            }
            set.Add(combatant);
            _combatantCells[combatant] = cell;
        }
        
        /// <summary>
        /// Desregistra un combatiente del grid
        /// </summary>
        public static void UnregisterCombatant(ArenaCombatant combatant)
        {
            if (combatant == null) return;
            
            if (_combatantCells.TryGetValue(combatant, out var cell))
            {
                if (_cells.TryGetValue(cell, out var set))
                {
                    set.Remove(combatant);
                    if (set.Count == 0)
                        _cells.Remove(cell);
                }
                _combatantCells.Remove(combatant);
            }
        }
        
        /// <summary>
        /// Actualiza la posición de un combatiente en el grid
        /// </summary>
        public static void UpdateCombatantPosition(ArenaCombatant combatant)
        {
            if (combatant == null) return;
            
            Vector2Int newCell = GetCell(combatant.transform.position);
            
            // Verificar si cambió de celda
            if (_combatantCells.TryGetValue(combatant, out var currentCell))
            {
                if (currentCell == newCell) return; // No cambió, nada que hacer
            }
            
            // Cambió de celda, re-registrar
            RegisterCombatant(combatant);
        }
        
        /// <summary>
        /// Encuentra los combatientes más cercanos dentro del radio especificado
        /// </summary>
        public static ArenaCombatant FindNearest(Vector3 position, float radius, int teamIdToIgnore = -1, bool requireAlive = true)
        {
            float radiusSqr = radius * radius;
            ArenaCombatant nearest = null;
            float bestDistSqr = float.MaxValue;
            
            // Calcular rango de celdas a revisar
            Vector2Int centerCell = GetCell(position);
            int cellsToCheck = Mathf.CeilToInt(radius / CELL_SIZE);
            
            for (int x = -cellsToCheck; x <= cellsToCheck; x++)
            {
                for (int y = -cellsToCheck; y <= cellsToCheck; y++)
                {
                    Vector2Int cell = new Vector2Int(centerCell.x + x, centerCell.y + y);
                    
                    if (_cells.TryGetValue(cell, out var combatants))
                    {
                        foreach (var c in combatants)
                        {
                            if (c == null) continue;
                            if (requireAlive && !c.IsAlive) continue;
                            if (teamIdToIgnore >= 0 && c.teamId == teamIdToIgnore) continue;
                            
                            float distSqr = (c.transform.position - position).sqrMagnitude;
                            if (distSqr < radiusSqr && distSqr < bestDistSqr)
                            {
                                bestDistSqr = distSqr;
                                nearest = c;
                            }
                        }
                    }
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Encuentra todos los combatientes dentro del radio
        /// </summary>
        public static List<ArenaCombatant> FindAllInRadius(Vector3 position, float radius, int teamIdToIgnore = -1, bool requireAlive = true)
        {
            float radiusSqr = radius * radius;
            var results = new List<ArenaCombatant>();
            
            Vector2Int centerCell = GetCell(position);
            int cellsToCheck = Mathf.CeilToInt(radius / CELL_SIZE);
            
            for (int x = -cellsToCheck; x <= cellsToCheck; x++)
            {
                for (int y = -cellsToCheck; y <= cellsToCheck; y++)
                {
                    Vector2Int cell = new Vector2Int(centerCell.x + x, centerCell.y + y);
                    
                    if (_cells.TryGetValue(cell, out var combatants))
                    {
                        foreach (var c in combatants)
                        {
                            if (c == null) continue;
                            if (requireAlive && !c.IsAlive) continue;
                            if (teamIdToIgnore >= 0 && c.teamId == teamIdToIgnore) continue;
                            
                            if ((c.transform.position - position).sqrMagnitude < radiusSqr)
                            {
                                results.Add(c);
                            }
                        }
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Obtiene la celda correspondiente a una posición del mundo
        /// </summary>
        private static Vector2Int GetCell(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / CELL_SIZE),
                Mathf.FloorToInt(position.z / CELL_SIZE)
            );
        }
        
        /// <summary>
        /// Limpia todo el grid
        /// </summary>
        public static void Clear()
        {
            _cells.Clear();
            _combatantCells.Clear();
        }
        
        /// <summary>
        /// Debug: Dibuja el grid en el editor
        /// </summary>
        public static void DrawGizmos()
        {
            Gizmos.color = Color.cyan;
            
            foreach (var kvp in _cells)
            {
                Vector2Int cell = kvp.Key;
                Vector3 center = new Vector3(
                    (cell.x + 0.5f) * CELL_SIZE,
                    0.5f,
                    (cell.y + 0.5f) * CELL_SIZE
                );
                
                Gizmos.DrawWireCube(center, new Vector3(CELL_SIZE, 1f, CELL_SIZE));
                
                // Mostrar número de combatientes en la celda
                // Nota: Esto requiere GUI.Label, mejor hacerlo en OnGUI
            }
        }
    }
}

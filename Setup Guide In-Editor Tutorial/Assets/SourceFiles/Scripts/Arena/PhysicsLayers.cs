using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Utilidad centralizada para gestionar máscaras de capas de física.
    /// Mejora el rendimiento al evitar cálculos repetidos de LayerMask.
    /// </summary>
    public static class PhysicsLayers
    {
        // Cache de LayerMasks calculados para evitar llamadas repetidas a GetMask
        private static int _defaultLayer = -1;
        private static int _groundLayer = -1;
        private static int _environmentLayer = -1;
        private static int _destructibleLayer = -1;
        private static int _enemyLayer = -1;
        private static int _playerLayer = -1;
        private static int _projectileLayer = -1;
        private static int _invincibleLayer = -1;
        
        // LayerMasks cacheados para uso en física
        private static LayerMask _groundMask;
        private static LayerMask _environmentMask;
        private static LayerMask _destructibleMask;
        private static LayerMask _enemyMask;
        private static LayerMask _combatantMask; // Enemies + Player
        private static LayerMask _damageableMask; // Todo lo que puede recibir daño
        private static LayerMask _obstacleMask; // Ground + Environment
        private static bool _initialized = false;
        
        /// <summary>
        /// Inicializa los índices de capas. Llamar al inicio del juego.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            
            _defaultLayer = LayerMask.NameToLayer("Default");
            _groundLayer = LayerMask.NameToLayer("Ground");
            _environmentLayer = LayerMask.NameToLayer("Environment");
            _destructibleLayer = LayerMask.NameToLayer("Destructible");
            _enemyLayer = LayerMask.NameToLayer("Enemy");
            _playerLayer = LayerMask.NameToLayer("Player");
            _projectileLayer = LayerMask.NameToLayer("Projectile");
            _invincibleLayer = LayerMask.NameToLayer("Invincible");
            
            // Construir máscaras combinadas
            _groundMask = GetMaskForLayer(_groundLayer);
            _environmentMask = GetMaskForLayer(_environmentLayer);
            _destructibleMask = GetMaskForLayer(_destructibleLayer);
            _enemyMask = GetMaskForLayer(_enemyLayer);
            _combatantMask = CombineMasks(_enemyMask, GetMaskForLayer(_playerLayer));
            _damageableMask = CombineMasks(_combatantMask, _destructibleMask);
            _obstacleMask = CombineMasks(_groundMask, _environmentMask);
            
            _initialized = true;
        }
        
        // Propiedades de acceso a layer indices
        public static int Default => EnsureInitialized(_defaultLayer);
        public static int Ground => EnsureInitialized(_groundLayer);
        public static int Environment => EnsureInitialized(_environmentLayer);
        public static int Destructible => EnsureInitialized(_destructibleLayer);
        public static int Enemy => EnsureInitialized(_enemyLayer);
        public static int Player => EnsureInitialized(_playerLayer);
        public static int Projectile => EnsureInitialized(_projectileLayer);
        public static int Invincible => EnsureInitialized(_invincibleLayer);
        
        // Propiedades de acceso a LayerMasks
        public static LayerMask GroundMask => EnsureInitializedMask(_groundMask);
        public static LayerMask EnvironmentMask => EnsureInitializedMask(_environmentMask);
        public static LayerMask DestructibleMask => EnsureInitializedMask(_destructibleMask);
        public static LayerMask EnemyMask => EnsureInitializedMask(_enemyMask);
        public static LayerMask CombatantMask => EnsureInitializedMask(_combatantMask);
        public static LayerMask DamageableMask => EnsureInitializedMask(_damageableMask);
        public static LayerMask ObstacleMask => EnsureInitializedMask(_obstacleMask);
        
        /// <summary>
        /// Crea una máscara para una capa específica.
        /// </summary>
        public static LayerMask GetMaskForLayer(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex > 31) return 0;
            return 1 << layerIndex;
        }
        
        /// <summary>
        /// Combina múltiples máscaras en una sola.
        /// </summary>
        public static LayerMask CombineMasks(params LayerMask[] masks)
        {
            LayerMask combined = 0;
            foreach (var mask in masks)
            {
                combined |= mask;
            }
            return combined;
        }
        
        /// <summary>
        /// Verifica si un GameObject está en una capa específica.
        /// </summary>
        public static bool IsInLayer(GameObject obj, int layer)
        {
            return obj != null && obj.layer == layer;
        }
        
        /// <summary>
        /// Verifica si un GameObject está en alguna de las capas de la máscara.
        /// </summary>
        public static bool IsInMask(GameObject obj, LayerMask mask)
        {
            return obj != null && ((1 << obj.layer) & mask) != 0;
        }
        
        /// <summary>
        /// Verifica si la capa es considerada suelo o ambiente.
        /// </summary>
        public static bool IsEnvironmentLayer(int layer)
        {
            return layer == Ground || layer == Environment || layer == Destructible;
        }
        
        /// <summary>
        /// Verifica si un objeto es parte del ambiente (suelo, paredes, destructibles).
        /// </summary>
        public static bool IsEnvironment(GameObject obj)
        {
            if (obj == null) return false;
            return IsEnvironmentLayer(obj.layer) || obj.GetComponent<DestructibleEnvironment>() != null;
        }
        
        /// <summary>
        /// Configura la matriz de colisión entre capas de física.
        /// Desactiva colisiones innecesarias para mejorar rendimiento.
        /// </summary>
        public static void ConfigureCollisionMatrix()
        {
            Initialize();
            
            // Projectiles no colisionan con otros projectiles
            Physics.IgnoreLayerCollision(Projectile, Projectile, true);
            
            // Invincible no colisiona con projectiles enemigos (ya gestionado por lógica de juego)
            Physics.IgnoreLayerCollision(Invincible, Projectile, false);
            
            // Enemies no colisionan entre sí (opcional, depende del diseño)
            // Physics.IgnoreLayerCollision(Enemy, Enemy, true);
        }
        
        // Helper methods para asegurar inicialización
        private static int EnsureInitialized(int value)
        {
            if (!_initialized) Initialize();
            return value;
        }
        
        private static LayerMask EnsureInitializedMask(LayerMask mask)
        {
            if (!_initialized) Initialize();
            return mask;
        }
    }
}

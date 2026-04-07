using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Cache centralizado de PhysicMaterials para evitar creación en runtime.
    /// Mejora el rendimiento y reduce GC pressure.
    /// </summary>
    public static class PhysicsMaterialCache
    {
        private static PhysicsMaterial _grenadeMaterial;
        private static PhysicsMaterial _projectileMaterial;
        private static PhysicsMaterial _bouncyMaterial;
        private static PhysicsMaterial _slipperyMaterial;
        private static PhysicsMaterial _roughMaterial;
        
        /// <summary>
        /// Material para granadas - balanceado entre rebote y fricción.
        /// </summary>
        public static PhysicsMaterial Grenade
        {
            get
            {
                if (_grenadeMaterial == null)
                {
                    _grenadeMaterial = new PhysicsMaterial("Grenade")
                    {
                        bounciness = 0.4f,
                        dynamicFriction = 0.6f,
                        staticFriction = 0.6f,
                        frictionCombine = PhysicsMaterialCombine.Average,
                        bounceCombine = PhysicsMaterialCombine.Average
                    };
                }
                return _grenadeMaterial;
            }
        }
        
        /// <summary>
        /// Material para proyectiles - sin rebote, baja fricción.
        /// </summary>
        public static PhysicsMaterial Projectile
        {
            get
            {
                if (_projectileMaterial == null)
                {
                    _projectileMaterial = new PhysicsMaterial("Projectile")
                    {
                        bounciness = 0f,
                        dynamicFriction = 0.1f,
                        staticFriction = 0.1f,
                        frictionCombine = PhysicsMaterialCombine.Minimum,
                        bounceCombine = PhysicsMaterialCombine.Minimum
                    };
                }
                return _projectileMaterial;
            }
        }
        
        /// <summary>
        /// Material muy elástico - alto rebote.
        /// </summary>
        public static PhysicsMaterial Bouncy
        {
            get
            {
                if (_bouncyMaterial == null)
                {
                    _bouncyMaterial = new PhysicsMaterial("Bouncy")
                    {
                        bounciness = 0.8f,
                        dynamicFriction = 0.3f,
                        staticFriction = 0.3f,
                        frictionCombine = PhysicsMaterialCombine.Average,
                        bounceCombine = PhysicsMaterialCombine.Maximum
                    };
                }
                return _bouncyMaterial;
            }
        }
        
        /// <summary>
        /// Material resbaladizo - muy baja fricción.
        /// </summary>
        public static PhysicsMaterial Slippery
        {
            get
            {
                if (_slipperyMaterial == null)
                {
                    _slipperyMaterial = new PhysicsMaterial("Slippery")
                    {
                        bounciness = 0f,
                        dynamicFriction = 0.05f,
                        staticFriction = 0.05f,
                        frictionCombine = PhysicsMaterialCombine.Minimum,
                        bounceCombine = PhysicsMaterialCombine.Minimum
                    };
                }
                return _slipperyMaterial;
            }
        }
        
        /// <summary>
        /// Material rugoso - alta fricción.
        /// </summary>
        public static PhysicsMaterial Rough
        {
            get
            {
                if (_roughMaterial == null)
                {
                    _roughMaterial = new PhysicsMaterial("Rough")
                    {
                        bounciness = 0.1f,
                        dynamicFriction = 0.9f,
                        staticFriction = 0.9f,
                        frictionCombine = PhysicsMaterialCombine.Maximum,
                        bounceCombine = PhysicsMaterialCombine.Average
                    };
                }
                return _roughMaterial;
            }
        }
        
        /// <summary>
        /// Crea un material personalizado con los parámetros especificados.
        /// </summary>
        public static PhysicsMaterial CreateCustom(
            string name,
            float dynamicFriction = 0.6f,
            float staticFriction = 0.6f,
            float bounciness = 0f,
            PhysicsMaterialCombine frictionCombine = PhysicsMaterialCombine.Average,
            PhysicsMaterialCombine bounceCombine = PhysicsMaterialCombine.Average)
        {
            return new PhysicsMaterial(name)
            {
                dynamicFriction = dynamicFriction,
                staticFriction = staticFriction,
                bounciness = Mathf.Clamp01(bounciness),
                frictionCombine = frictionCombine,
                bounceCombine = bounceCombine
            };
        }
        
        /// <summary>
        /// Limpia los materiales cacheados. Útil para recargas de escena.
        /// </summary>
        public static void ClearCache()
        {
            _grenadeMaterial = null;
            _projectileMaterial = null;
            _bouncyMaterial = null;
            _slipperyMaterial = null;
            _roughMaterial = null;
        }
    }
}

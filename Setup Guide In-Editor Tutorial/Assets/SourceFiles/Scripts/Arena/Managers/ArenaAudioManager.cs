using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Manager centralizado para efectos de sonido.
    /// NOTA: Audio temporalmente desactivado.
    /// </summary>
    public static class ArenaAudioManager
    {
        // Flag para desactivar todo el audio
        public static bool AudioEnabled = false;
        
        private static AudioSource _globalSource;
        
        /// <summary>
        /// Reproduce un sonido global (2D)
        /// </summary>
        public static void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (!AudioEnabled) return; // Audio desactivado
            if (clip == null) return;
            
            // Lazy initialization
            if (_globalSource == null)
            {
                var go = new GameObject("ArenaAudioManager");
                _globalSource = go.AddComponent<AudioSource>();
                Object.DontDestroyOnLoad(go);
            }
            
            _globalSource.PlayOneShot(clip, volume);
        }
        
        /// <summary>
        /// Reproduce un sonido 3D en una posición específica
        /// </summary>
        public static void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (!AudioEnabled) return; // Audio desactivado
            if (clip == null) return;
            
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
        
        // Métodos de conveniencia para sonidos comunes del juego
        // Nota: Audio desactivado por defecto, estos métodos no harán nada hasta que AudioEnabled = true
        public static void PlayFireball() { }
        public static void PlayPickup() { }
        public static void PlayMelee() { }
        
        /// <summary>
        /// Reproduce sonido de lanzamiento de granada
        /// </summary>
        public static void PlayGrenadeThrow() { }
        
        /// <summary>
        /// Reproduce sonido de explosión en posición específica
        /// </summary>
        public static void PlayExplosionSound(Vector3 position) { }
        
        /// <summary>
        /// Reproduce sonido genérico de habilidad
        /// </summary>
        public static void PlayAbilitySound(string abilityName) { }
    }
}

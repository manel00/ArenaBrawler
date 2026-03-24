using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Recibe eventos de animación de los modelos de StarterAssets
    /// </summary>
    public class AnimationEventReceiver : MonoBehaviour
    {
        public void OnFootstep(AnimationEvent animationEvent) { }
        public void OnLand(AnimationEvent animationEvent) { }
    }
}

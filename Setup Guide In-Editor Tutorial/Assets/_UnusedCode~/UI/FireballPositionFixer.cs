using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Corrects position for KCISA fireball - attach to spawned fireball
    /// </summary>
    public class FireballPositionFixer : MonoBehaviour
    {
        void Start()
        {
            // Find the actual visual effect and reposition it
            Transform visualEffect = null;
            
            // Look for ParticleSystem in children
            var ps = GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                visualEffect = ps.transform;
            }
            
            if (visualEffect != null)
            {
                // Reset visual effect to center on this object
                visualEffect.localPosition = Vector3.zero;
                Debug.Log("[FireballPositionFixer] Repositioned visual effect to center");
            }
        }
    }
}

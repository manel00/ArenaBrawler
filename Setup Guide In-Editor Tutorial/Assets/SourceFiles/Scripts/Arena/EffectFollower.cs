using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Hace que un efecto visual siga a un target (proyectil) en world space
    /// </summary>
    public class EffectFollower : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset;
        
        void LateUpdate()
        {
            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }
        
        void OnDestroy()
        {
            // Limpiar referencia cuando se destruya
            target = null;
        }
    }
}

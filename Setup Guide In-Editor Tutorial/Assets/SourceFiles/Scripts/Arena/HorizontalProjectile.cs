using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Mantiene el proyectil moviéndose estrictamente horizontal
    /// </summary>
    public class HorizontalProjectile : MonoBehaviour
    {
        public float spawnY = 1.1f;
        private Rigidbody _rb;
        private Vector3 _lastPosition;
        
        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _lastPosition = transform.position;
            
            // Asegurar posición Y inicial correcta
            Vector3 pos = transform.position;
            pos.y = spawnY;
            transform.position = pos;
        }
        
        void FixedUpdate()
        {
            if (_rb == null) return;
            
            // Forzar altura constante
            Vector3 pos = transform.position;
            pos.y = spawnY;
            transform.position = pos;
            
            // Mantener velocidad estrictamente horizontal
            Vector3 vel = _rb.linearVelocity;
            vel.y = 0f;
            _rb.linearVelocity = vel;
            
            // Asegurar que no hay rotación extraña
            transform.rotation = Quaternion.LookRotation(vel.normalized, Vector3.up);
        }
    }
}

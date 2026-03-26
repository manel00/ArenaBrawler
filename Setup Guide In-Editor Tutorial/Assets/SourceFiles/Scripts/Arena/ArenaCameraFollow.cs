using UnityEngine;

namespace ArenaEnhanced
{
    public class ArenaCameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 7f, -9f);
        public float smooth = 5f;
        public float rotationSpeed = 5f;
        public float minDistance = 5f;
        public float maxDistance = 15f;
        public float zoomSpeed = 2f;

        private float _currentDistance;
        private float _targetDistance;
        private Vector3 _currentOffset;

        private void Start()
        {
            _currentDistance = offset.magnitude;
            _targetDistance = _currentDistance;
            _currentOffset = offset.normalized;

            gameObject.tag = "MainCamera";
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Rotación de la cámara (WoW-style follow)
            Quaternion targetRotation = Quaternion.identity;
            var combatant = target.GetComponent<ArenaCombatant>();
            if (combatant == null || combatant.IsAlive)
            {
                targetRotation = target.rotation;
            }

            Vector3 desiredPosition = target.position + targetRotation * offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smooth * Time.deltaTime);
            
            if (combatant == null || combatant.IsAlive)
                transform.LookAt(target.position + Vector3.up * 1.5f);
        }
    }
}
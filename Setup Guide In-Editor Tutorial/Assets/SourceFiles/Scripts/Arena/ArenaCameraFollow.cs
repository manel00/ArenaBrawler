using UnityEngine;

/// <summary>
/// Cámara que sigue al jugador en la arena
/// </summary>
public class ArenaCameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    
    [Header("Configuración")]
    public Vector3 offset = new Vector3(0f, 7f, -9f);
    public float smoothSpeed = 20f;
    public bool lookAtTarget = true;
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        // Mantener el offset relativo a la rotación del target pero ignorando su escala
        Vector3 desiredPosition = target.position + (target.rotation * offset);
        
        // Seguir la posición suavemente
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // Mirar siempre al frente del personaje
        if (lookAtTarget)
        {
            transform.LookAt(target.position + Vector3.up * 1.5f);
        }
    }
    
    /// <summary>
    /// Cambia el objetivo de la cámara
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
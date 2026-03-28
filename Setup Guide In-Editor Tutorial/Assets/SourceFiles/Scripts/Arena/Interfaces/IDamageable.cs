using UnityEngine;

namespace ArenaEnhanced.Interfaces
{
    /// <summary>
    /// Interfaz para objetos que pueden recibir daño
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Aplica daño al objeto
        /// </summary>
        /// <param name="damage">Cantidad de daño a aplicar</param>
        /// <param name="source">Fuente del daño (opcional)</param>
        void TakeDamage(float damage, GameObject source = null);
        
        /// <summary>
        /// Verifica si el objeto está vivo
        /// </summary>
        bool IsAlive { get; }
        
        /// <summary>
        /// Salud actual del objeto
        /// </summary>
        float CurrentHealth { get; }
        
        /// <summary>
        /// Salud máxima del objeto
        /// </summary>
        float MaxHealth { get; }
        
        /// <summary>
        /// Evento que se dispara cuando el objeto recibe daño
        /// </summary>
        event System.Action<float, GameObject> OnDamageReceived;
        
        /// <summary>
        /// Evento que se dispara cuando el objeto muere
        /// </summary>
        event System.Action<GameObject> OnDeath;
    }
}
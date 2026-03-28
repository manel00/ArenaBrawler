using UnityEngine;

namespace ArenaEnhanced.Interfaces
{
    /// <summary>
    /// Interfaz para objetos que pueden ser interactuados por el jugador
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Realiza la interacción con el objeto
        /// </summary>
        /// <param name="interactor">Objeto que realiza la interacción (jugador, bot, etc.)</param>
        void Interact(GameObject interactor);
        
        /// <summary>
        /// Verifica si el objeto puede ser interactuado
        /// </summary>
        /// <param name="interactor">Objeto que intenta interactuar</param>
        /// <returns>True si se puede interactuar</returns>
        bool CanInteract(GameObject interactor);
        
        /// <summary>
        /// Obtiene el texto de interacción para mostrar en UI
        /// </summary>
        /// <returns>Texto descriptivo de la interacción</returns>
        string GetInteractionText();
        
        /// <summary>
        /// Obtiene la prioridad de interacción (mayor = más prioritario)
        /// </summary>
        int InteractionPriority { get; }
        
        /// <summary>
        /// Evento que se dispara cuando se realiza una interacción
        /// </summary>
        event System.Action<GameObject> OnInteracted;
    }
}
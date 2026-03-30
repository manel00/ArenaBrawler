using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de cache global para Camera.main y otras cámaras frecuentemente accedidas.
    /// Elimina el overhead de buscar la cámara principal cada frame.
    /// </summary>
    public static class CameraCache
    {
        private static Camera _mainCamera;
        private static Camera _uiCamera;
        private static Transform _mainCameraTransform;
        
        /// <summary>
        /// Obtiene la cámara principal cacheada. 
        /// La primera llamada busca Camera.main, subsiguientes usan cache.
        /// </summary>
        public static Camera Main
        {
            get
            {
                if (_mainCamera == null)
                {
                    _mainCamera = Camera.main;
                    _mainCameraTransform = _mainCamera?.transform;
                }
                return _mainCamera;
            }
        }
        
        /// <summary>
        /// Transform de la cámara principal cacheado
        /// </summary>
        public static Transform MainTransform
        {
            get
            {
                if (_mainCameraTransform == null && Main != null)
                {
                    _mainCameraTransform = _mainCamera.transform;
                }
                return _mainCameraTransform;
            }
        }
        
        /// <summary>
        /// Cámara UI (si existe)
        /// </summary>
        public static Camera UICamera
        {
            get
            {
                if (_uiCamera == null)
                {
                    _uiCamera = GameObject.FindGameObjectWithTag("UICamera")?.GetComponent<Camera>();
                }
                return _uiCamera;
            }
        }
        
        /// <summary>
        /// Invalida el cache. Llamar cuando la cámara principal cambia.
        /// </summary>
        public static void InvalidateCache()
        {
            _mainCamera = null;
            _mainCameraTransform = null;
            _uiCamera = null;
        }
        
        /// <summary>
        /// Pre-calienta el cache al inicio del juego
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void WarmupCache()
        {
            // Fuerza inicialización temprana
            var _ = Main;
        }
    }
}

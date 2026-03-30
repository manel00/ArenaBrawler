using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de minimapa con icons claros para jugador, aliados, enemigos y objetos importantes.
    /// </summary>
    public class MinimapSystem : MonoBehaviour
    {
        public static MinimapSystem Instance { get; private set; }

        [Header("Map Settings")]
        [Tooltip("Radio del mundo que se muestra en el minimapa")]
        [SerializeField] private float worldRadius = 80f;
        
        [Tooltip("Tamaño del minimapa en pantalla (píxeles)")]
        [SerializeField] private Vector2 mapSize = new Vector2(200, 200);
        
        [Tooltip("Posición en pantalla")]
        [SerializeField] private Vector2 mapPosition = new Vector2(30, 30);

        [Header("Visual Settings")]
        [SerializeField] private Sprite minimapBackground;
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.15f, 0.1f, 0.85f);
        [SerializeField] private Color borderColor = new Color(0.4f, 0.5f, 0.4f, 1f);
        [SerializeField] private float borderWidth = 3f;

        [Header("Icon Settings")]
        [SerializeField] private float playerIconSize = 16f;
        [SerializeField] private float allyIconSize = 12f;
        [SerializeField] private float enemyIconSize = 12f;
        [SerializeField] private float bossIconSize = 20f;
        [SerializeField] private float pickupIconSize = 10f;

        [Header("Icon Colors")]
        [SerializeField] private Color playerColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField] private Color allyColor = new Color(0.4f, 1f, 0.4f);
        [SerializeField] private Color enemyColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color bossColor = new Color(1f, 0.1f, 0.1f);
        [SerializeField] private Color pickupColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color weaponColor = new Color(0.8f, 0.6f, 1f);

        // UI References
        private RectTransform _minimapPanel;
        private RectTransform _iconsContainer;
        private Transform _playerTransform;
        private Dictionary<Transform, MinimapIcon> _trackedEntities = new Dictionary<Transform, MinimapIcon>();
        private List<MinimapIcon> _iconPool = new List<MinimapIcon>();

        private class MinimapIcon
        {
            public RectTransform rectTransform;
            public Image image;
            public Transform targetTransform;
            public IconType type;
            public float yOffset;
        }

        public enum IconType
        {
            Player,
            Ally,
            Enemy,
            Boss,
            Pickup,
            Weapon
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            BuildMinimapUI();
            FindPlayer();
        }

        private void BuildMinimapUI()
        {
            // Crear canvas si no existe
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("MinimapCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
            }

            // Panel principal del minimapa
            GameObject panelGO = new GameObject("MinimapPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            _minimapPanel = panelGO.AddComponent<RectTransform>();
            _minimapPanel.anchorMin = Vector2.zero;
            _minimapPanel.anchorMax = Vector2.zero;
            _minimapPanel.anchoredPosition = mapPosition;
            _minimapPanel.sizeDelta = mapSize;

            // Background
            Image bgImage = panelGO.AddComponent<Image>();
            if (minimapBackground != null)
                bgImage.sprite = minimapBackground;
            bgImage.color = backgroundColor;

            // Border
            GameObject borderGO = new GameObject("Border");
            borderGO.transform.SetParent(_minimapPanel, false);
            RectTransform borderRT = borderGO.AddComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.offsetMin = Vector2.zero;
            borderRT.offsetMax = Vector2.zero;
            
            Outline outline = borderGO.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(borderWidth, borderWidth);

            // Container para icons
            GameObject iconsGO = new GameObject("IconsContainer");
            iconsGO.transform.SetParent(_minimapPanel, false);
            _iconsContainer = iconsGO.AddComponent<RectTransform>();
            _iconsContainer.anchorMin = Vector2.zero;
            _iconsContainer.anchorMax = Vector2.one;
            _iconsContainer.offsetMin = Vector2.zero;
            _iconsContainer.offsetMax = Vector2.zero;
        }

        private void FindPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                RegisterEntity(_playerTransform, IconType.Player, 0f);
            }
        }

        private void Update()
        {
            if (_playerTransform == null) return;

            UpdateAllIcons();
        }

        private void UpdateAllIcons()
        {
            Vector3 playerPos = _playerTransform.position;
            float mapScale = mapSize.x / (worldRadius * 2f);

            foreach (var kvp in _trackedEntities)
            {
                Transform entity = kvp.Key;
                MinimapIcon icon = kvp.Value;

                if (entity == null)
                {
                    HideIcon(icon);
                    continue;
                }

                // Calcular posición relativa al jugador
                Vector3 offset = entity.position - playerPos;
                
                // Verificar si está dentro del rango
                float distance = offset.magnitude;
                if (distance > worldRadius)
                {
                    // Mostrar en borde si está cerca pero fuera
                    if (distance < worldRadius * 1.3f)
                    {
                        offset = offset.normalized * worldRadius * 0.95f;
                        icon.image.color = new Color(icon.image.color.r, icon.image.color.g, icon.image.color.b, 0.5f);
                    }
                    else
                    {
                        icon.rectTransform.gameObject.SetActive(false);
                        continue;
                    }
                }
                else
                {
                    icon.rectTransform.gameObject.SetActive(true);
                    // Restaurar color original
                    icon.image.color = GetColorForType(icon.type);
                }

                // Convertir a coordenadas del minimapa
                Vector2 mapPos = new Vector2(
                    offset.x * mapScale,
                    offset.z * mapScale
                );

                // Centrar en el panel
                mapPos += mapSize * 0.5f;

                // Aplicar posición
                icon.rectTransform.anchoredPosition = mapPos;

                // Rotar icono del jugador según su dirección
                if (icon.type == IconType.Player)
                {
                    float angle = entity.eulerAngles.y;
                    icon.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
                }
            }
        }

        /// <summary>
        /// Registra una entidad para mostrar en el minimapa
        /// </summary>
        public void RegisterEntity(Transform entity, IconType type, float yOffset = 0f)
        {
            if (entity == null || _trackedEntities.ContainsKey(entity)) return;

            MinimapIcon icon = GetOrCreateIcon();
            icon.targetTransform = entity;
            icon.type = type;
            icon.yOffset = yOffset;

            // Configurar visual
            icon.image.color = GetColorForType(type);
            icon.rectTransform.sizeDelta = GetIconSize(type) * Vector2.one;
            
            // Shape del icono según tipo
            SetupIconShape(icon, type);

            _trackedEntities.Add(entity, icon);
        }

        /// <summary>
        /// Remueve una entidad del minimapa
        /// </summary>
        public void UnregisterEntity(Transform entity)
        {
            if (entity == null || !_trackedEntities.ContainsKey(entity)) return;

            MinimapIcon icon = _trackedEntities[entity];
            HideIcon(icon);
            _trackedEntities.Remove(entity);
        }

        private MinimapIcon GetOrCreateIcon()
        {
            // Buscar icono inactivo en el pool
            foreach (var icon in _iconPool)
            {
                if (!icon.rectTransform.gameObject.activeSelf)
                {
                    icon.rectTransform.gameObject.SetActive(true);
                    return icon;
                }
            }

            // Crear nuevo icono
            GameObject go = new GameObject("Icon");
            go.transform.SetParent(_iconsContainer, false);
            
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.one * 0.5f;

            Image img = go.AddComponent<Image>();

            MinimapIcon newIcon = new MinimapIcon
            {
                rectTransform = rt,
                image = img
            };

            _iconPool.Add(newIcon);
            return newIcon;
        }

        private void HideIcon(MinimapIcon icon)
        {
            icon.rectTransform.gameObject.SetActive(false);
            icon.targetTransform = null;
        }

        private void SetupIconShape(MinimapIcon icon, IconType type)
        {
            // Configurar sprite según tipo
            // En un proyecto real, usarías sprites específicos
            // Aquí usamos el color para diferenciar
        }

        private float GetIconSize(IconType type)
        {
            switch (type)
            {
                case IconType.Player: return playerIconSize;
                case IconType.Ally: return allyIconSize;
                case IconType.Enemy: return enemyIconSize;
                case IconType.Boss: return bossIconSize;
                case IconType.Pickup: return pickupIconSize;
                case IconType.Weapon: return pickupIconSize;
                default: return 10f;
            }
        }

        private Color GetColorForType(IconType type)
        {
            switch (type)
            {
                case IconType.Player: return playerColor;
                case IconType.Ally: return allyColor;
                case IconType.Enemy: return enemyColor;
                case IconType.Boss: return bossColor;
                case IconType.Pickup: return pickupColor;
                case IconType.Weapon: return weaponColor;
                default: return Color.white;
            }
        }

        /// <summary>
        /// Actualiza todas las entidades enemigas desde ArenaCombatant.All
        /// </summary>
        public void RefreshEnemyTracking()
        {
            // Limpiar enemigos actuales
            var toRemove = new List<Transform>();
            foreach (var kvp in _trackedEntities)
            {
                if (kvp.Value.type == IconType.Enemy || kvp.Value.type == IconType.Boss)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var entity in toRemove)
            {
                UnregisterEntity(entity);
            }

            // Registrar enemigos actuales
            foreach (var combatant in ArenaCombatant.All)
            {
                if (combatant == null || !combatant.IsAlive) continue;
                if (combatant.isPlayer || combatant.teamId == 1) continue; // Ignorar jugador y aliados

                IconType type = combatant.name.Contains("T-Rex") || combatant.name.Contains("Boss") 
                    ? IconType.Boss 
                    : IconType.Enemy;

                RegisterEntity(combatant.transform, type);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_playerTransform != null)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
                Gizmos.DrawWireSphere(_playerTransform.position, worldRadius);
            }
        }
    }
}

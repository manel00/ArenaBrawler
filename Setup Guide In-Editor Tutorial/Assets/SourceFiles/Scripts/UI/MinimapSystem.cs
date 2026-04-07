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
        [SerializeField] private Vector2 mapSize = new Vector2(250, 250);
        
        [Tooltip("Posición en pantalla")]
        [SerializeField] private Vector2 mapPosition = new Vector2(-30, 30);

        [Header("Visual Settings")]
        [SerializeField] private Sprite minimapBackground;
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.15f, 0.1f, 0.85f);
        [SerializeField] private Color borderColor = new Color(0.4f, 0.5f, 0.4f, 1f);

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

            // Panel principal del minimapa (circular) - anclado a esquina inferior derecha
            GameObject panelGO = new GameObject("MinimapPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            _minimapPanel = panelGO.AddComponent<RectTransform>();
            _minimapPanel.anchorMin = new Vector2(1, 0);  // Esquina inferior derecha
            _minimapPanel.anchorMax = new Vector2(1, 0);  // Esquina inferior derecha
            _minimapPanel.pivot = new Vector2(1, 0);      // Pivot en esquina inferior derecha
            _minimapPanel.anchoredPosition = new Vector2(-20, 20);  // 20px de margen desde esquina
            _minimapPanel.sizeDelta = new Vector2(mapSize.x, mapSize.x); // Cuadrado para círculo perfecto

            // Background circular con máscara
            Image bgImage = panelGO.AddComponent<Image>();
            bgImage.sprite = CreateCircleSprite();
            bgImage.color = backgroundColor;
            bgImage.type = Image.Type.Simple;

            // Añadir Mask para que los iconos se recorten circularmente
            Mask mask = panelGO.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            // Borde circular - con padding para que no se corte
            GameObject borderGO = new GameObject("Border");
            borderGO.transform.SetParent(_minimapPanel, false);
            RectTransform borderRT = borderGO.AddComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.offsetMin = new Vector2(2, 2);   // Padding interior
            borderRT.offsetMax = new Vector2(-2, -2); // Padding interior
            
            Image borderImg = borderGO.AddComponent<Image>();
            borderImg.sprite = CreateCircleOutlineSprite();
            borderImg.color = borderColor;
            borderImg.type = Image.Type.Simple;

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
            switch (type)
            {
                case IconType.Player:
                    icon.image.sprite = CreateTriangleSprite();
                    break;
                case IconType.Ally:
                    icon.image.sprite = CreateCircleSprite();
                    break;
                case IconType.Enemy:
                case IconType.Boss:
                    icon.image.sprite = CreateCircleSprite();
                    break;
                default:
                    icon.image.sprite = CreateCircleSprite();
                    break;
            }
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

        /// <summary>
        /// Crea un sprite circular sólido
        /// </summary>
        private static Sprite CreateCircleSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = dist <= radius ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Crea un sprite de contorno circular
        /// </summary>
        private static Sprite CreateCircleOutlineSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float outerRadius = size / 2f;
            float innerRadius = size / 2f - 4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = (dist <= outerRadius && dist >= innerRadius) ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Crea un sprite triangular (apunta hacia arriba por defecto)
        /// </summary>
        private static Sprite CreateTriangleSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            // Triángulo apuntando hacia arriba
            Vector2 top = new Vector2(size / 2f, size - 4f);
            Vector2 bottomLeft = new Vector2(4f, 4f);
            Vector2 bottomRight = new Vector2(size - 4f, 4f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    // Usar barycentric coordinates para determinar si está dentro del triángulo
                    float denom = (bottomLeft.y - bottomRight.y) * (top.x - bottomRight.x) + (bottomRight.x - bottomLeft.x) * (top.y - bottomRight.y);
                    float a = ((bottomLeft.y - bottomRight.y) * (p.x - bottomRight.x) + (bottomRight.x - bottomLeft.x) * (p.y - bottomRight.y)) / denom;
                    float b = ((bottomRight.y - top.y) * (p.x - bottomRight.x) + (top.x - bottomRight.x) * (p.y - bottomRight.y)) / denom;
                    float c = 1 - a - b;

                    pixels[y * size + x] = (a >= 0 && b >= 0 && c >= 0) ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}

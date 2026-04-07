using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace ArenaEnhanced
{
    /// <summary>
    /// UI de pantalla de bienvenida funcional con EventSystem
    /// </summary>
    public class WelcomeScreenUI : MonoBehaviour
    {
        public Action<string, int, string> OnStartGame;
        
        [Header("Font")]
        public TMP_FontAsset fontAsset;
        
        private GameObject _mainPanel;
        private TMP_InputField _nameInput;
        private int _selectedBotCount = 3;
        private string _selectedMapId = "original";
        private Camera _camera;
        private List<Button> _botButtons = new List<Button>();
        private List<Button> _mapButtons = new List<Button>();
        private GameObject _eventSystem;
        
        private void Awake()
        {
            // Crear EventSystem PRIMERO (esencial para interacciones)
            CreateEventSystem();
            SetupCameraAndCanvas();
        }
        
        private void Start()
        {
            SetupFont();
            CreateBackground();
            CreateMainUI();
        }
        
        private void SetupFont()
        {
            if (fontAsset == null)
            {
                fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/InterRegular");
                if (fontAsset == null)
                    fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/InterBold");
                if (fontAsset == null)
                    fontAsset = Resources.Load<TMP_FontAsset>("InterRegular");
            }
        }
        
        private void CreateEventSystem()
        {
            // Verificar si ya existe
            var existing = FindAnyObjectByType<EventSystem>();
            if (existing != null) return;
            
            _eventSystem = new GameObject("EventSystem");
            _eventSystem.AddComponent<EventSystem>();
            _eventSystem.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(_eventSystem);
        }
        
        private void SetupCameraAndCanvas()
        {
            // Crear cámara
            var camObj = new GameObject("WelcomeCamera");
            _camera = camObj.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            _camera.orthographic = true;
            _camera.orthographicSize = 5;
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = 1000f;
            _camera.transform.position = new Vector3(0, 0, -10);
            
            // Crear Canvas con ScreenSpaceOverlay (más simple para UI)
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.pixelPerfect = false;
            
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            
            // Raycaster ESENCIAL para capturar clicks
            var raycaster = gameObject.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        }
        
        private void CreateBackground()
        {
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(transform, false);
            
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.12f, 0.18f, 1f);
            bgImg.raycastTarget = true;
        }
        
        private void CreateMainUI()
        {
            // Panel principal - DISEÑO PREMIUM
            _mainPanel = new GameObject("MainPanel");
            _mainPanel.transform.SetParent(transform, false);
            _mainPanel.layer = 5;
            
            var panelRT = _mainPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(650, 900); // Panel más ancho y alto
            
            var panelImg = _mainPanel.AddComponent<Image>();
            // Gradiente oscuro premium
            panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);
            panelImg.raycastTarget = true;
            
            // BORDE premium dorado/bronze
            var outline = _mainPanel.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.6f, 0.1f, 0.8f);
            outline.effectDistance = new Vector2(4, -4);
            
            // SOMBRA más pronunciada
            var shadow = _mainPanel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(12, -12);
            
            // ===== HEADER PREMIUM =====
            // TITULO con efecto de brillo
            var titleGo = CreateText("⚔ ARENA BRAWLER ⚔", 52, new Color(1f, 0.75f, 0.1f), FontStyles.Bold);
            titleGo.transform.SetParent(_mainPanel.transform, false);
            var titleRT = titleGo.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1);
            titleRT.anchorMax = new Vector2(0.5f, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(0, -35);
            titleRT.sizeDelta = new Vector2(600, 60);
            
            // Línea decorativa dorada
            CreateDivider(-105, new Color(1f, 0.6f, 0.1f, 0.6f));
            
            // Subtitulo elegante
            var subtitleGo = CreateText("HORDE SURVIVAL", 22, new Color(0.6f, 0.6f, 0.7f), FontStyles.Normal);
            subtitleGo.transform.SetParent(_mainPanel.transform, false);
            var subtitleRT = subtitleGo.GetComponent<RectTransform>();
            subtitleRT.anchorMin = new Vector2(0.5f, 1);
            subtitleRT.anchorMax = new Vector2(0.5f, 1);
            subtitleRT.pivot = new Vector2(0.5f, 1);
            subtitleRT.anchoredPosition = new Vector2(0, -115);
            subtitleRT.sizeDelta = new Vector2(600, 30);
            
            // ===== SECCIÓN PLAYER =====
            CreateSectionHeader("PLAYER NAME", -165);
            
            // INPUT NOMBRE - estilo premium
            var inputGo = CreateInputField(PlayerPrefs.GetString("PlayerName", "Survivor"));
            inputGo.transform.SetParent(_mainPanel.transform, false);
            var inputRT = inputGo.GetComponent<RectTransform>();
            inputRT.anchorMin = new Vector2(0.5f, 1);
            inputRT.anchorMax = new Vector2(0.5f, 1);
            inputRT.pivot = new Vector2(0.5f, 1);
            inputRT.anchoredPosition = new Vector2(0, -205);
            inputRT.sizeDelta = new Vector2(450, 45);
            _nameInput = inputGo.GetComponent<TMP_InputField>();
            
            // Línea decorativa
            CreateDivider(-265, new Color(0.3f, 0.3f, 0.4f, 0.4f));
            
            // ===== SECCIÓN BOTS =====
            CreateSectionHeader("ALLIED BOTS", -285);
            
            // BOTONES BOTS - compactos y premium
            for (int i = 0; i <= 10; i++)
            {
                var btnGo = new GameObject("BotBtn_" + i);
                btnGo.transform.SetParent(_mainPanel.transform, false);
                btnGo.layer = 5;
                
                var btnRT = btnGo.AddComponent<RectTransform>();
                float xPos = (i - 5) * 50f;
                btnRT.anchorMin = new Vector2(0.5f, 1);
                btnRT.anchorMax = new Vector2(0.5f, 1);
                btnRT.pivot = new Vector2(0.5f, 1);
                btnRT.anchoredPosition = new Vector2(xPos, -325);
                btnRT.sizeDelta = new Vector2(42, 42);
                
                var img = btnGo.AddComponent<Image>();
                bool isSelected = (i == 3);
                // Colores premium: dorado para seleccionado, gris oscuro para no seleccionado
                img.color = isSelected ? new Color(1f, 0.75f, 0.1f) : new Color(0.2f, 0.2f, 0.25f);
                img.raycastTarget = true;
                
                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = img;
                
                var colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
                colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
                colors.selectedColor = new Color(1f, 0.75f, 0.1f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.08f;
                btn.colors = colors;
                
                int botCount = i;
                btn.onClick.AddListener(() => OnBotSelected(botCount));
                
                // Texto del número
                var textGo = new GameObject("Text");
                textGo.transform.SetParent(btnGo.transform, false);
                var textRT = textGo.AddComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;
                var txt = textGo.AddComponent<TextMeshProUGUI>();
                txt.text = i.ToString();
                txt.fontSize = 20;
                txt.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                txt.color = isSelected ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.7f, 0.7f, 0.8f);
                txt.alignment = TextAlignmentOptions.Center;
                txt.raycastTarget = false;
                
                _botButtons.Add(btn);
            }
            
            // ===== SECCIÓN MAPAS =====
            CreateSectionHeader("SELECT MAP", -390);
            CreateMapGalleryPremium();
            
            // ===== BOTONES DE ACCIÓN =====
            // PLAY GAME - Botón grande y prominente
            var playBtn = CreatePremiumButton("▶ PLAY GAME", new Color(0.1f, 0.7f, 0.25f), new Color(0.15f, 0.9f, 0.35f));
            playBtn.transform.SetParent(_mainPanel.transform, false);
            var playBtnRT = playBtn.GetComponent<RectTransform>();
            playBtnRT.anchorMin = new Vector2(0.5f, 0);
            playBtnRT.anchorMax = new Vector2(0.5f, 0);
            playBtnRT.pivot = new Vector2(0.5f, 0);
            playBtnRT.anchoredPosition = new Vector2(0, 100);
            playBtnRT.sizeDelta = new Vector2(450, 70);
            
            var playButton = playBtn.GetComponent<Button>();
            playButton.onClick.AddListener(OnPlayClicked);
            
            // EXIT - Botón más pequeño y sutil
            var exitBtn = CreatePremiumButton("✕ EXIT", new Color(0.7f, 0.15f, 0.15f), new Color(0.9f, 0.25f, 0.25f));
            exitBtn.transform.SetParent(_mainPanel.transform, false);
            var exitBtnRT = exitBtn.GetComponent<RectTransform>();
            exitBtnRT.anchorMin = new Vector2(0.5f, 0);
            exitBtnRT.anchorMax = new Vector2(0.5f, 0);
            exitBtnRT.pivot = new Vector2(0.5f, 0);
            exitBtnRT.anchoredPosition = new Vector2(0, 20);
            exitBtnRT.sizeDelta = new Vector2(250, 45);
            
            var exitButton = exitBtn.GetComponent<Button>();
            exitButton.onClick.AddListener(() => Application.Quit());
        }
        
        private void CreateSectionHeader(string text, float yPos)
        {
            var label = CreateText(text, 20, new Color(0.8f, 0.6f, 0.2f), FontStyles.Bold);
            label.transform.SetParent(_mainPanel.transform, false);
            var labelRT = label.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0.5f, 1);
            labelRT.anchorMax = new Vector2(0.5f, 1);
            labelRT.pivot = new Vector2(0.5f, 1);
            labelRT.anchoredPosition = new Vector2(0, yPos);
            labelRT.sizeDelta = new Vector2(550, 25);
        }
        
        private void CreateDivider(float yPos, Color color)
        {
            var dividerGo = new GameObject("Divider");
            dividerGo.transform.SetParent(_mainPanel.transform, false);
            dividerGo.layer = 5;
            
            var dividerRT = dividerGo.AddComponent<RectTransform>();
            dividerRT.anchorMin = new Vector2(0.5f, 1);
            dividerRT.anchorMax = new Vector2(0.5f, 1);
            dividerRT.pivot = new Vector2(0.5f, 1);
            dividerRT.anchoredPosition = new Vector2(0, yPos);
            dividerRT.sizeDelta = new Vector2(550, 2);
            
            var img = dividerGo.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }
        
        private GameObject CreatePremiumButton(string text, Color normalColor, Color highlightColor)
        {
            var go = new GameObject("PremiumButton_" + text);
            go.layer = 5;
            
            var rect = go.AddComponent<RectTransform>();
            
            // Fondo con gradiente simulado
            var img = go.AddComponent<Image>();
            img.color = normalColor;
            img.raycastTarget = true;
            
            // Borde brillante
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.3f);
            outline.effectDistance = new Vector2(2, -2);
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.selectedColor = highlightColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            
            // Texto
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRT = textGo.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var txt = textGo.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = 28;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
            txt.raycastTarget = false;
            if (fontAsset != null)
                txt.font = fontAsset;
            
            return go;
        }
        
        private void CreateMapGalleryPremium()
        {
            var maps = new[]
            {
                new { id = "original", name = "ORIGINAL ARENA", color = new Color(0.25f, 0.55f, 0.25f), accent = new Color(0.4f, 0.8f, 0.4f) },
                new { id = "forestarena", name = "FOREST ARENA", color = new Color(0.15f, 0.45f, 0.25f), accent = new Color(0.3f, 0.8f, 0.5f) },
                new { id = "rockycanyon", name = "ROCKY CANYON", color = new Color(0.35f, 0.3f, 0.25f), accent = new Color(0.6f, 0.5f, 0.4f) },
                new { id = "deadwoods", name = "DEAD WOODS", color = new Color(0.12f, 0.1f, 0.08f), accent = new Color(0.25f, 0.22f, 0.18f) },
                new { id = "mushroomgrove", name = "MUSHROOM GROVE", color = new Color(0.25f, 0.2f, 0.35f), accent = new Color(0.5f, 0.35f, 0.6f) },
                new { id = "waterarena", name = "WATER ARENA", color = new Color(0.15f, 0.25f, 0.35f), accent = new Color(0.3f, 0.5f, 0.7f) },
                new { id = "koreantemple", name = "KOREAN TEMPLE", color = new Color(0.3f, 0.28f, 0.25f), accent = new Color(0.5f, 0.25f, 0.2f) },
                new { id = "volcanic", name = "VOLCANIC COAST", color = new Color(0.15f, 0.1f, 0.08f), accent = new Color(0.9f, 0.35f, 0.15f) }
            };
            
            float cardWidth = 200;
            float cardHeight = 120;
            float spacing = 15;
            float startX = -((maps.Length * cardWidth) + ((maps.Length - 1) * spacing)) / 2 + cardWidth / 2;
            
            for (int i = 0; i < maps.Length; i++)
            {
                var map = maps[i];
                var cardGo = new GameObject("MapCard_" + map.id);
                cardGo.transform.SetParent(_mainPanel.transform, false);
                cardGo.layer = 5;
                
                var cardRT = cardGo.AddComponent<RectTransform>();
                cardRT.anchorMin = new Vector2(0.5f, 1);
                cardRT.anchorMax = new Vector2(0.5f, 1);
                cardRT.pivot = new Vector2(0.5f, 1);
                cardRT.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing), -440);
                cardRT.sizeDelta = new Vector2(cardWidth, cardHeight);
                
                bool isSelected = (map.id == _selectedMapId);
                
                // Try to load thumbnail sprite
                Sprite thumbnailSprite = LoadMapThumbnail(map.id);
                
                // Card background - use thumbnail if available, otherwise color
                var img = cardGo.AddComponent<Image>();
                if (thumbnailSprite != null)
                {
                    img.sprite = thumbnailSprite;
                    img.type = Image.Type.Simple;
                    img.color = isSelected ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
                }
                else
                {
                    img.color = isSelected ? map.accent : map.color;
                }
                img.raycastTarget = true;
                
                // Borde premium
                var outline = cardGo.AddComponent<Outline>();
                outline.effectColor = isSelected ? new Color(1f, 0.85f, 0.2f, 0.9f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);
                outline.effectDistance = new Vector2(3, -3);
                
                // Sombra sutil
                var shadow = cardGo.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.4f);
                shadow.effectDistance = new Vector2(4, -4);
                
                // Button
                var btn = cardGo.AddComponent<Button>();
                btn.targetGraphic = img;
                
                var colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
                colors.pressedColor = new Color(0.9f, 0.9f, 0.9f);
                colors.selectedColor = Color.white;
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.1f;
                btn.colors = colors;
                
                string mapId = map.id;
                btn.onClick.AddListener(() => OnMapSelected(mapId));
                _mapButtons.Add(btn);
                
                // Semi-transparent overlay for text readability
                var overlayGo = new GameObject("TextOverlay");
                overlayGo.transform.SetParent(cardGo.transform, false);
                var overlayRT = overlayGo.AddComponent<RectTransform>();
                overlayRT.anchorMin = new Vector2(0, 0);
                overlayRT.anchorMax = new Vector2(1, 0.35f);
                overlayRT.offsetMin = Vector2.zero;
                overlayRT.offsetMax = Vector2.zero;
                var overlayImg = overlayGo.AddComponent<Image>();
                overlayImg.color = new Color(0, 0, 0, 0.6f);
                overlayImg.raycastTarget = false;
                
                // Map name text
                var textGo = new GameObject("MapName");
                textGo.transform.SetParent(overlayGo.transform, false);
                var textRT = textGo.AddComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = new Vector2(5, 2);
                textRT.offsetMax = new Vector2(-5, -2);
                var txt = textGo.AddComponent<TextMeshProUGUI>();
                txt.text = map.name;
                txt.fontSize = 14;
                txt.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.Center;
                txt.raycastTarget = false;
                if (fontAsset != null)
                    txt.font = fontAsset;
            }
        }
        
        /// <summary>
        /// Loads a map thumbnail sprite from Resources folder
        /// </summary>
        private Sprite LoadMapThumbnail(string mapId)
        {
            try
            {
                string path = $"MapThumbnails/thumbnail_{mapId}";
                Sprite sprite = Resources.Load<Sprite>(path);
                
                if (sprite == null)
                {
                    Debug.LogWarning($"[WelcomeScreen] Thumbnail not found: {path}");
                }
                else
                {
                    Debug.Log($"[WelcomeScreen] Loaded thumbnail: {path}");
                }
                
                return sprite;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WelcomeScreen] Error loading thumbnail for {mapId}: {e.Message}");
                return null;
            }
        }
        
        private void CreateBotButtons()
        {
            for (int i = 0; i <= 10; i++)
            {
                var btnGo = new GameObject("BotBtn_" + i);
                btnGo.transform.SetParent(_mainPanel.transform, false);
                btnGo.layer = 5;
                
                var btnRT = btnGo.AddComponent<RectTransform>();
                float xPos = (i - 5) * 50f;
                btnRT.anchorMin = new Vector2(0.5f, 1);
                btnRT.anchorMax = new Vector2(0.5f, 1);
                btnRT.pivot = new Vector2(0.5f, 1);
                btnRT.anchoredPosition = new Vector2(xPos, -355);
                btnRT.sizeDelta = new Vector2(45, 45);
                
                var img = btnGo.AddComponent<Image>();
                bool isSelected = (i == 3);
                img.color = isSelected ? new Color(1f, 0.85f, 0.2f) : new Color(0.25f, 0.25f, 0.3f);
                img.raycastTarget = true;
                
                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = img;
                
                var colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
                colors.pressedColor = new Color(0.9f, 0.9f, 0.9f);
                colors.selectedColor = new Color(1f, 0.85f, 0.2f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.1f;
                btn.colors = colors;
                
                int botCount = i;
                btn.onClick.AddListener(() => OnBotSelected(botCount));
                
                // Texto del numero
                var textGo = new GameObject("Text");
                textGo.transform.SetParent(btnGo.transform, false);
                var textRT = textGo.AddComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;
                var txt = textGo.AddComponent<TextMeshProUGUI>();
                txt.text = i.ToString();
                txt.fontSize = 22;
                txt.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                txt.color = isSelected ? Color.black : Color.white;
                txt.alignment = TextAlignmentOptions.Center;
                txt.raycastTarget = false;
                if (fontAsset != null)
                    txt.font = fontAsset;
                
                _botButtons.Add(btn);
            }
        }
        
        private void OnBotSelected(int count)
        {
            _selectedBotCount = count;
            PlayerPrefs.SetInt("BotCount", count);
            
            for (int i = 0; i < _botButtons.Count; i++)
            {
                var btn = _botButtons[i];
                var img = btn.GetComponent<Image>();
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                bool isSelected = (i == count);
                
                img.color = isSelected ? new Color(1f, 0.85f, 0.2f) : new Color(0.25f, 0.25f, 0.3f);
                txt.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                txt.color = isSelected ? Color.black : Color.white;
            }
            
            Debug.Log($"[WelcomeScreen] Selected {count} bots");
        }
        
        private GameObject CreateText(string content, int size, Color color, FontStyles style)
        {
            var go = new GameObject("Text_" + content.Replace(" ", ""));
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = size;
            txt.color = color;
            txt.fontStyle = style;
            txt.alignment = TextAlignmentOptions.Center;
            txt.raycastTarget = false;
            if (fontAsset != null)
                txt.font = fontAsset;
            return go;
        }
        
        private GameObject CreateInputField(string defaultText)
        {
            var go = new GameObject("NameInput");
            go.layer = 5;
            
            var rt = go.AddComponent<RectTransform>();
            
            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.12f, 1f);
            img.raycastTarget = true;
            
            // Borde
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.5f, 0.5f, 0.6f, 0.8f);
            outline.effectDistance = new Vector2(2, -2);
            
            // Input Field component
            var input = go.AddComponent<TMP_InputField>();
            
            // Text Area
            var textArea = new GameObject("Text Area");
            textArea.transform.SetParent(go.transform, false);
            var textAreaRT = textArea.AddComponent<RectTransform>();
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.offsetMin = new Vector2(15, 5);
            textAreaRT.offsetMax = new Vector2(-15, -5);
            
            // Placeholder
            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(textArea.transform, false);
            var phRT = phGo.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = Vector2.zero;
            phRT.offsetMax = Vector2.zero;
            var ph = phGo.AddComponent<TextMeshProUGUI>();
            ph.text = "Enter name...";
            ph.fontSize = 28;
            ph.fontStyle = FontStyles.Italic;
            ph.color = new Color(1, 1, 1, 0.4f);
            ph.alignment = TextAlignmentOptions.Center;
            ph.raycastTarget = false;
            if (fontAsset != null)
                ph.font = fontAsset;
            
            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(textArea.transform, false);
            var textRT = textGo.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var txt = textGo.AddComponent<TextMeshProUGUI>();
            txt.text = defaultText;
            txt.fontSize = 28;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
            txt.raycastTarget = false;
            if (fontAsset != null)
                txt.font = fontAsset;
            
            // Configurar input field
            input.textViewport = textAreaRT;
            input.placeholder = ph;
            input.textComponent = txt;
            input.text = defaultText;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 20;
            input.onValueChanged.AddListener(OnNameChanged);
            
            return go;
        }
        
        private void OnNameChanged(string newName)
        {
            PlayerPrefs.SetString("PlayerName", newName);
        }
        
        private GameObject CreateButton(string text, Color color)
        {
            var go = new GameObject("Btn_" + text.Replace(" ", ""));
            go.layer = 5;
            
            var rt = go.AddComponent<RectTransform>();
            
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            
            // Borde
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(1, 1, 1, 0.3f);
            outline.effectDistance = new Vector2(2, -2);
            
            // Sombra
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.4f);
            shadow.effectDistance = new Vector2(4, -4);
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.selectedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            
            // Texto
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRT = textGo.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var txt = textGo.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = 30;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
            txt.raycastTarget = false;
            if (fontAsset != null)
                txt.font = fontAsset;
            
            return go;
        }
        
        private void OnPlayClicked()
        {
            string playerName = _nameInput != null && !string.IsNullOrWhiteSpace(_nameInput.text) 
                ? _nameInput.text.Trim() 
                : "Survivor";
                
            Debug.Log($"[WelcomeScreen] START GAME: {playerName} with {_selectedBotCount} bots");
            
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.SetInt("BotCount", _selectedBotCount);
            PlayerPrefs.SetString("SelectedMap", _selectedMapId);
            PlayerPrefs.SetString("GameMode", "Solo");
            PlayerPrefs.SetString("FromWelcomeScreen", "true");
            PlayerPrefs.Save();
            
            OnStartGame?.Invoke(playerName, _selectedBotCount, _selectedMapId);
            Destroy(gameObject);
        }
        
        private void CreateMapGallery()
        {
            // Ya no se usa - reemplazado por CreateMapGalleryPremium
        }
        
        private void OnMapSelected(string mapId)
        {
            _selectedMapId = mapId;
            PlayerPrefs.SetString("SelectedMap", mapId);
            
            // Update visual selection
            var maps = new[] { "original", "forestarena", "rockycanyon", "deadwoods", "mushroomgrove", "waterarena", "koreantemple", "volcanic" };
            int mapCount = Mathf.Min(maps.Length, _mapButtons.Count);
            for (int i = 0; i < mapCount; i++)
            {
                var btn = _mapButtons[i];
                var img = btn.GetComponent<Image>();
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                var outline = btn.GetComponent<Outline>();
                bool isSelected = (maps[i] == mapId);
                
                // Restore original colors based on map
                var originalColors = new[] 
                { 
                    new Color(0.3f, 0.6f, 0.3f),    // original
                    new Color(0.2f, 0.5f, 0.3f),    // forestarena
                    new Color(0.5f, 0.45f, 0.4f),   // rockycanyon
                    new Color(0.15f, 0.15f, 0.12f), // deadwoods
                    new Color(0.4f, 0.3f, 0.5f),    // mushroomgrove
                    new Color(0.2f, 0.35f, 0.5f),   // waterarena
                    new Color(0.4f, 0.35f, 0.3f),   // koreantemple
                    new Color(0.2f, 0.12f, 0.08f)   // volcanic
                };
                
                img.color = isSelected ? new Color(1f, 0.85f, 0.2f) : originalColors[i];
                txt.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                txt.color = isSelected ? Color.black : Color.white;
                outline.effectColor = isSelected ? new Color(1f, 1f, 1f, 0.8f) : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
            
            Debug.Log($"[WelcomeScreen] Selected map: {mapId}");
        }
        
        private void OnDestroy()
        {
            if (_eventSystem != null)
                Destroy(_eventSystem);
        }
    }
}

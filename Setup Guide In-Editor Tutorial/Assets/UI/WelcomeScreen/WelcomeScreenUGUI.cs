using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Fallback Welcome Screen using UGUI - ensures compatibility and reliability
    /// Premium violet theme with procedural background
    /// </summary>
    public class WelcomeScreenUGUI : MonoBehaviour
    {
        [Header("Events")]
        public Action<int, string, string> OnStartGame;
        
        [Header("Font")]
        public TMP_FontAsset fontAsset;
        
        private int _selectedBotCount = 3;
        private string _selectedMapId = "original";
        private string _playerName = "Survivor";
        
        // UI References
        private TMP_InputField _nameInput;
        private List<Button> _botButtons = new List<Button>();
        private Button _playButton;
        private Button _exitButton;
        // Map buttons - 8 maps total
        private Button _mapOriginal;
        private Button _mapForestArena;
        private Button _mapRockyCanyon;
        private Button _mapDeadWoods;
        private Button _mapMushroomGrove;
        private Button _mapWaterArena;
        private Button _mapKoreanTemple;
        private Button _mapVolcanic;
        
        // Colors
        private readonly Color ColorGold = new Color(0.96f, 0.62f, 0.04f);
        private readonly Color ColorGoldLight = new Color(0.99f, 0.82f, 0.30f);
        private readonly Color ColorVioletDark = new Color(0.18f, 0.11f, 0.47f);
        private readonly Color ColorVioletLight = new Color(0.66f, 0.33f, 0.97f);
        private readonly Color ColorVioletSurface = new Color(0.25f, 0.15f, 0.50f);
        private readonly Color ColorTextLight = new Color(0.95f, 0.95f, 1f);
        private readonly Color ColorGreen = new Color(0.13f, 0.77f, 0.37f);
        private readonly Color ColorRed = new Color(0.94f, 0.27f, 0.27f);
        
        private void Awake()
        {
            SetupCanvas();
        }
        
        private void Start()
        {
            SetupFont();
            LoadSavedData();
            CreateUI();
        }
        
        private void SetupFont()
        {
            if (fontAsset == null)
            {
                // Load Inter font from project resources
                fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/InterRegular");
                if (fontAsset == null)
                {
                    fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/InterBold");
                }
                // Try alternative path
                if (fontAsset == null)
                {
                    fontAsset = Resources.Load<TMP_FontAsset>("InterRegular");
                }
            }
        }
        
        private void SetupCanvas()
        {
            // Create Canvas
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            gameObject.AddComponent<GraphicRaycaster>();
            
            // Ensure EventSystem exists
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            // Create temporary camera to avoid "No cameras rendering" warning
            SetupTemporaryCamera();
        }
        
        private void SetupTemporaryCamera()
        {
            // Only create if no camera exists
            if (Camera.main != null) return;
            
            var camGo = new GameObject("WelcomeScreenCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.02f, 0.12f); // Dark violet
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 10;
            cam.depth = -1; // Behind UI
            cam.transform.position = new Vector3(0, 0, -5);
        }
        
        private void LoadSavedData()
        {
            _playerName = PlayerPrefs.GetString("PlayerName", "Survivor");
            _selectedBotCount = PlayerPrefs.GetInt("BotCount", 3);
            _selectedMapId = PlayerPrefs.GetString("SelectedMap", "original");
        }
        
        private void CreateUI()
        {
            // Create background
            CreateBackground();
            
            // Create main panel (centered)
            var panel = CreatePanel();
            
            // Create header
            CreateHeader(panel);
            
            // Create player name section
            CreatePlayerNameSection(panel);
            
            // Create bot selector
            CreateBotSelector(panel);
            
            // Create map selector
            CreateMapSelector(panel);
            
            // Create action buttons
            CreateActionButtons(panel);
        }
        
        private void CreateBackground()
        {
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(transform, false);
            
            var rect = bgGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var img = bgGo.AddComponent<Image>();
            img.color = new Color(0.06f, 0.02f, 0.12f, 1f);
            
            // Add procedural background component
            var procBg = bgGo.AddComponent<ProceduralBackgroundUGUI>();
            procBg.targetImage = img;
        }
        
        private RectTransform CreatePanel()
        {
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(transform, false);
            
            var rect = panelGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(650, 750);
            
            var img = panelGo.AddComponent<Image>();
            img.color = new Color(0.18f, 0.11f, 0.47f, 0.95f);
            img.sprite = CreateRoundedRectSprite();
            img.type = Image.Type.Sliced;
            
            // Add outline
            var outline = panelGo.AddComponent<Outline>();
            outline.effectColor = ColorVioletLight;
            outline.effectDistance = new Vector2(2, -2);
            
            return rect;
        }
        
        private void CreateHeader(RectTransform parent)
        {
            // Title
            var titleGo = CreateText("⚔ ARENA BRAWLER ⚔", 48, ColorGold, true);
            titleGo.transform.SetParent(parent, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(600, 60);
            
            // Subtitle
            var subGo = CreateText("HORDE SURVIVAL", 16, new Color(0.9f, 0.85f, 1f), false);
            subGo.transform.SetParent(parent, false);
            var subRect = subGo.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1);
            subRect.anchorMax = new Vector2(0.5f, 1);
            subRect.pivot = new Vector2(0.5f, 1);
            subRect.anchoredPosition = new Vector2(0, -80);
            subRect.sizeDelta = new Vector2(600, 30);
            
            // Divider
            var divGo = new GameObject("Divider");
            divGo.transform.SetParent(parent, false);
            var divRect = divGo.AddComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0.5f, 1);
            divRect.anchorMax = new Vector2(0.5f, 1);
            divRect.pivot = new Vector2(0.5f, 1);
            divRect.anchoredPosition = new Vector2(0, -110);
            divRect.sizeDelta = new Vector2(550, 2);
            var divImg = divGo.AddComponent<Image>();
            divImg.color = ColorGold;
        }
        
        private void CreatePlayerNameSection(RectTransform parent)
        {
            // Label
            var labelGo = CreateText("PLAYER NAME", 14, ColorGold, true);
            labelGo.transform.SetParent(parent, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1);
            labelRect.anchorMax = new Vector2(0.5f, 1);
            labelRect.pivot = new Vector2(0.5f, 1);
            labelRect.anchoredPosition = new Vector2(0, -130);
            labelRect.sizeDelta = new Vector2(550, 25);
            
            // Input field background
            var inputBgGo = new GameObject("InputBackground");
            inputBgGo.transform.SetParent(parent, false);
            var inputBgRect = inputBgGo.AddComponent<RectTransform>();
            inputBgRect.anchorMin = new Vector2(0.5f, 1);
            inputBgRect.anchorMax = new Vector2(0.5f, 1);
            inputBgRect.pivot = new Vector2(0.5f, 1);
            inputBgRect.anchoredPosition = new Vector2(0, -160);
            inputBgRect.sizeDelta = new Vector2(550, 44);
            var inputBgImg = inputBgGo.AddComponent<Image>();
            inputBgImg.color = new Color(0.08f, 0.04f, 0.15f, 1f);
            inputBgImg.sprite = CreateRoundedRectSprite();
            inputBgImg.type = Image.Type.Sliced;
            
            // Input field
            var inputGo = new GameObject("NameInput");
            inputGo.transform.SetParent(inputBgGo.transform, false);
            var inputRect = inputGo.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = new Vector2(10, 5);
            inputRect.offsetMax = new Vector2(-10, -5);
            
            _nameInput = inputGo.AddComponent<TMP_InputField>();
            var textComp = inputGo.AddComponent<TextMeshProUGUI>();
            textComp.fontSize = 18;
            textComp.color = ColorTextLight;
            textComp.alignment = TextAlignmentOptions.Center;
            _nameInput.textComponent = textComp;
            _nameInput.text = _playerName;
            _nameInput.onValueChanged.AddListener(OnNameChanged);
        }
        
        private void CreateBotSelector(RectTransform parent)
        {
            // Label
            var labelGo = CreateText("ALLIED BOTS", 14, ColorGold, true);
            labelGo.transform.SetParent(parent, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1);
            labelRect.anchorMax = new Vector2(0.5f, 1);
            labelRect.pivot = new Vector2(0.5f, 1);
            labelRect.anchoredPosition = new Vector2(0, -230);
            labelRect.sizeDelta = new Vector2(550, 25);
            
            // Container
            var containerGo = new GameObject("BotContainer");
            containerGo.transform.SetParent(parent, false);
            var containerRect = containerGo.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 1);
            containerRect.anchorMax = new Vector2(0.5f, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = new Vector2(0, -260);
            containerRect.sizeDelta = new Vector2(550, 50);
            
            // Bot buttons
            for (int i = 0; i <= 10; i++)
            {
                var btn = CreateBotButton(i.ToString(), i);
                btn.transform.SetParent(containerRect, false);
                var btnRect = btn.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.5f, 0.5f);
                btnRect.anchorMax = new Vector2(0.5f, 0.5f);
                btnRect.pivot = new Vector2(0.5f, 0.5f);
                float xPos = (i - 5) * 48f;
                btnRect.anchoredPosition = new Vector2(xPos, 0);
                btnRect.sizeDelta = new Vector2(44, 44);
                
                var btnComp = btn.GetComponent<Button>();
                int botCount = i;
                btnComp.onClick.AddListener(() => OnBotSelected(botCount));
                _botButtons.Add(btnComp);
                
                // Set initial state
                UpdateBotButtonVisuals(btnComp, i == _selectedBotCount);
            }
        }
        
        private void CreateMapSelector(RectTransform parent)
        {
            // Label
            var labelGo = CreateText("SELECT MAP", 14, ColorGold, true);
            labelGo.transform.SetParent(parent, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1);
            labelRect.anchorMax = new Vector2(0.5f, 1);
            labelRect.pivot = new Vector2(0.5f, 1);
            labelRect.anchoredPosition = new Vector2(0, -340);
            labelRect.sizeDelta = new Vector2(550, 25);
            
            // Map buttons - 2 rows of 4
            float btnWidth = 120;
            float btnHeight = 50;
            float spacing = 15;
            float startX = -((4 * btnWidth) + (3 * spacing)) / 2 + btnWidth / 2;
            float row1Y = -380;
            float row2Y = -440;
            
            // Row 1
            _mapOriginal = CreateMapButton("ORIGINAL", true);
            SetupMapButton(_mapOriginal, parent, startX, row1Y, btnWidth, btnHeight, () => OnMapSelected("original"));
            
            _mapForestArena = CreateMapButton("FOREST", false);
            SetupMapButton(_mapForestArena, parent, startX + (btnWidth + spacing), row1Y, btnWidth, btnHeight, () => OnMapSelected("forestarena"));
            
            _mapRockyCanyon = CreateMapButton("ROCKY", false);
            SetupMapButton(_mapRockyCanyon, parent, startX + 2 * (btnWidth + spacing), row1Y, btnWidth, btnHeight, () => OnMapSelected("rockycanyon"));
            
            _mapDeadWoods = CreateMapButton("DEAD", false);
            SetupMapButton(_mapDeadWoods, parent, startX + 3 * (btnWidth + spacing), row1Y, btnWidth, btnHeight, () => OnMapSelected("deadwoods"));
            
            // Row 2
            _mapMushroomGrove = CreateMapButton("SHROOMS", false);
            SetupMapButton(_mapMushroomGrove, parent, startX, row2Y, btnWidth, btnHeight, () => OnMapSelected("mushroomgrove"));
            
            _mapWaterArena = CreateMapButton("WATER", false);
            SetupMapButton(_mapWaterArena, parent, startX + (btnWidth + spacing), row2Y, btnWidth, btnHeight, () => OnMapSelected("waterarena"));
            
            _mapKoreanTemple = CreateMapButton("TEMPLE", false);
            SetupMapButton(_mapKoreanTemple, parent, startX + 2 * (btnWidth + spacing), row2Y, btnWidth, btnHeight, () => OnMapSelected("koreantemple"));
            
            _mapVolcanic = CreateMapButton("VOLCANIC", false);
            SetupMapButton(_mapVolcanic, parent, startX + 3 * (btnWidth + spacing), row2Y, btnWidth, btnHeight, () => OnMapSelected("volcanic"));
        }
        
        private void SetupMapButton(Button btn, RectTransform parent, float x, float y, float w, float h, UnityEngine.Events.UnityAction onClick)
        {
            btn.transform.SetParent(parent, false);
            var rect = btn.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
            btn.onClick.AddListener(onClick);
        }
        
        private void CreateActionButtons(RectTransform parent)
        {
            // Play button
            _playButton = CreateActionButton("▶ PLAY GAME", ColorGreen, 280, 56);
            _playButton.transform.SetParent(parent, false);
            var playRect = _playButton.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, 0);
            playRect.anchorMax = new Vector2(0.5f, 0);
            playRect.pivot = new Vector2(0.5f, 0);
            playRect.anchoredPosition = new Vector2(0, 80);
            _playButton.onClick.AddListener(OnPlayClicked);
            
            // Exit button
            _exitButton = CreateOutlineButton("✕ EXIT", ColorRed, 180, 40);
            _exitButton.transform.SetParent(parent, false);
            var exitRect = _exitButton.GetComponent<RectTransform>();
            exitRect.anchorMin = new Vector2(0.5f, 0);
            exitRect.anchorMax = new Vector2(0.5f, 0);
            exitRect.pivot = new Vector2(0.5f, 0);
            exitRect.anchoredPosition = new Vector2(0, 20);
            _exitButton.onClick.AddListener(OnExitClicked);
        }
        
        private GameObject CreateText(string content, int fontSize, Color color, bool bold)
        {
            var go = new GameObject("Text_" + content.GetHashCode());
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            if (fontAsset != null)
                text.font = fontAsset;
            return go;
        }
        
        private GameObject CreateBotButton(string text, int index)
        {
            var go = new GameObject("Bot_" + index);
            
            var rect = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = ColorVioletSurface;
            img.sprite = CreateRoundedRectSprite();
            img.type = Image.Type.Sliced;
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            
            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var textComp = textGo.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = 16;
            textComp.color = ColorTextLight;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.raycastTarget = false;
            if (fontAsset != null)
                textComp.font = fontAsset;
            
            return go;
        }
        
        private Button CreateMapButton(string label, bool selected)
        {
            var go = new GameObject("Map_" + label);
            
            var rect = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = selected ? new Color(0.42f, 0.13f, 0.66f) : ColorVioletSurface;
            img.sprite = CreateRoundedRectSprite();
            img.type = Image.Type.Sliced;
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            
            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);
            var textComp = textGo.AddComponent<TextMeshProUGUI>();
            textComp.text = label;
            textComp.fontSize = 14;
            textComp.fontStyle = FontStyles.Bold;
            textComp.color = selected ? ColorGold : ColorTextLight;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.raycastTarget = false;
            if (fontAsset != null)
                textComp.font = fontAsset;
            
            return btn;
        }
        
        private Button CreateActionButton(string text, Color bgColor, float width, float height)
        {
            var go = new GameObject("Button_" + text.GetHashCode());
            
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            img.sprite = CreateRoundedRectSprite();
            img.type = Image.Type.Sliced;
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            
            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var textComp = textGo.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = 22;
            textComp.fontStyle = FontStyles.Bold;
            textComp.color = Color.white;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.raycastTarget = false;
            if (fontAsset != null)
                textComp.font = fontAsset;
            
            return btn;
        }
        
        private Button CreateOutlineButton(string text, Color outlineColor, float width, float height)
        {
            var go = new GameObject("Button_" + text.GetHashCode());
            
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.sprite = CreateRoundedRectSprite();
            img.type = Image.Type.Sliced;
            
            var outline = go.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2, -2);
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            
            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var textComp = textGo.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = 14;
            textComp.color = outlineColor;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.raycastTarget = false;
            if (fontAsset != null)
                textComp.font = fontAsset;
            
            return btn;
        }
        
        private Sprite CreateRoundedRectSprite()
        {
            // Create a simple white 4x4 texture and convert to sprite
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
        
        private void OnBotSelected(int count)
        {
            _selectedBotCount = count;
            PlayerPrefs.SetInt("BotCount", count);
            
            for (int i = 0; i < _botButtons.Count; i++)
            {
                UpdateBotButtonVisuals(_botButtons[i], i == count);
            }
            
            Debug.Log($"[WelcomeScreen] Selected {count} bots");
        }
        
        private void UpdateBotButtonVisuals(Button btn, bool selected)
        {
            var img = btn.GetComponent<Image>();
            var text = btn.GetComponentInChildren<TextMeshProUGUI>();
            
            if (selected)
            {
                img.color = ColorGold;
                text.color = new Color(0.1f, 0.1f, 0.1f);
                text.fontStyle = FontStyles.Bold;
            }
            else
            {
                img.color = ColorVioletSurface;
                text.color = ColorTextLight;
                text.fontStyle = FontStyles.Normal;
            }
        }
        
        private void OnMapSelected(string mapId)
        {
            _selectedMapId = mapId;
            PlayerPrefs.SetString("SelectedMap", mapId);
            
            UpdateMapButtonVisuals(_mapOriginal, mapId == "original");
            UpdateMapButtonVisuals(_mapForestArena, mapId == "forestarena");
            UpdateMapButtonVisuals(_mapRockyCanyon, mapId == "rockycanyon");
            UpdateMapButtonVisuals(_mapDeadWoods, mapId == "deadwoods");
            UpdateMapButtonVisuals(_mapMushroomGrove, mapId == "mushroomgrove");
            UpdateMapButtonVisuals(_mapWaterArena, mapId == "waterarena");
            UpdateMapButtonVisuals(_mapKoreanTemple, mapId == "koreantemple");
            UpdateMapButtonVisuals(_mapVolcanic, mapId == "volcanic");
            
            Debug.Log($"[WelcomeScreen] Selected map: {mapId}");
        }
        
        private void UpdateMapButtonVisuals(Button btn, bool selected)
        {
            var img = btn.GetComponent<Image>();
            var text = btn.GetComponentInChildren<TextMeshProUGUI>();
            
            if (selected)
            {
                img.color = new Color(0.42f, 0.13f, 0.66f);
                text.color = ColorGold;
            }
            else
            {
                img.color = ColorVioletSurface;
                text.color = ColorTextLight;
            }
        }
        
        private void OnNameChanged(string newName)
        {
            _playerName = newName.Trim();
            PlayerPrefs.SetString("PlayerName", _playerName);
        }
        
        private void OnPlayClicked()
        {
            string finalName = string.IsNullOrWhiteSpace(_playerName) ? "Survivor" : _playerName.Trim();
            
            Debug.Log($"[WelcomeScreen] START GAME: {finalName} with {_selectedBotCount} bots on {_selectedMapId}");
            
            PlayerPrefs.SetString("PlayerName", finalName);
            PlayerPrefs.SetInt("BotCount", _selectedBotCount);
            PlayerPrefs.SetString("SelectedMap", _selectedMapId);
            PlayerPrefs.SetString("GameMode", "Solo");
            PlayerPrefs.SetString("FromWelcomeScreen", "true");
            PlayerPrefs.Save();
            
            OnStartGame?.Invoke(_selectedBotCount, finalName, _selectedMapId);
            Destroy(gameObject);
        }
        
        private void OnExitClicked()
        {
            Debug.Log("[WelcomeScreen] Exit clicked");
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
        
        private void OnDestroy()
        {
            if (_playButton != null)
                _playButton.onClick.RemoveListener(OnPlayClicked);
            if (_exitButton != null)
                _exitButton.onClick.RemoveListener(OnExitClicked);
        }
    }
    
    /// <summary>
    /// Procedural background for UGUI version
    /// </summary>
    public class ProceduralBackgroundUGUI : MonoBehaviour
    {
        public Image targetImage;
        
        private void Start()
        {
            GenerateBackground();
        }
        
        private void GenerateBackground()
        {
            int width = Screen.width;
            int height = Screen.height;
            
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            
            // Generate radial gradient
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            float maxDist = Mathf.Max(width, height) * 0.7f;
            
            Color centerColor = new Color(0.42f, 0.13f, 0.66f, 1f); // Violet
            Color outerColor = new Color(0.06f, 0.02f, 0.12f, 1f); // Dark
            
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(dist / maxDist));
                    pixels[y * width + x] = Color.Lerp(centerColor, outerColor, t);
                }
            }
            
            tex.SetPixels(pixels);
            tex.Apply();
            
            if (targetImage != null)
            {
                targetImage.sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            }
        }
    }
}

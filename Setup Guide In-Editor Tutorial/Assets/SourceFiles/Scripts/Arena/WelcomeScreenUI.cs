using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace ArenaEnhanced
{
    public class WelcomeScreenUI : MonoBehaviour
    {
        public System.Action<string, int, string> OnStartGame;
        
        private string _selectedMapId = "original";
        private int _selectedBotCount = 3;
        private TMP_InputField _nameInput;
        private GameObject _mainPanel;
        private List<Button> _mapButtons = new List<Button>();
        private List<Button> _botButtons = new List<Button>();
        
        private void Awake()
        {
            Debug.Log("[WelcomeScreenUI] AWAKE - Starting initialization");
            
            CreateBackgroundCamera();
            CreateEventSystem();
            SetupCanvas();
            CreateMainUI();
            
            Debug.Log("[WelcomeScreenUI] Initialization complete");
        }
        
        private void CreateBackgroundCamera()
        {
            // Verificar si ya existe una cámara
            if (Camera.main != null) return;
            
            var camGo = new GameObject("MenuCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.transform.position = new Vector3(0, 1.5f, -10f);
            cam.transform.rotation = Quaternion.identity;
            
            Debug.Log("[WelcomeScreenUI] Background camera created");
        }
        
        private void CreateEventSystem()
        {
            var existing = FindAnyObjectByType<EventSystem>();
            if (existing != null) return;
            
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            Debug.Log("[WelcomeScreenUI] EventSystem created");
        }
        
        private void SetupCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            gameObject.AddComponent<GraphicRaycaster>();
            gameObject.layer = 5;
        }
        
        private void CreateMainUI()
        {
            _mainPanel = new GameObject("MainPanel");
            _mainPanel.transform.SetParent(transform, false);
            _mainPanel.layer = 5;
            
            var panelRT = _mainPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(700, 900);
            panelRT.anchoredPosition = Vector2.zero;
            
            var panelImg = _mainPanel.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            panelImg.raycastTarget = false;
            
            // Título
            CreateText("ARENA BRAWLER", 48, new Color(1f, 0.8f, 0.2f), 0, 400);
            CreateText("HORDE SURVIVAL", 24, new Color(0.7f, 0.7f, 0.7f), 0, 350);
            
            // Nombre
            CreateText("PLAYER NAME", 20, new Color(0.8f, 0.6f, 0.2f), 0, 290);
            CreateNameInput(0, 250);
            
            // Bots
            CreateText("ALLIED BOTS", 20, new Color(0.8f, 0.6f, 0.2f), 0, 190);
            CreateBotButtons(0, 150);
            
            // Mapas - reposicionados dentro del recuadro
            CreateText("SELECT MAP", 20, new Color(0.8f, 0.6f, 0.2f), 0, 90);
            CreateMapButtons();
            
            // Botones
            CreatePlayButton(0, -350);
            CreateExitButton(0, -410);
        }
        
        private void CreateText(string text, int fontSize, Color color, float x, float y)
        {
            var go = new GameObject("Text_" + text);
            go.transform.SetParent(_mainPanel.transform, false);
            go.layer = 5;
            
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(500, 40);
            
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontStyle = FontStyles.Bold;
        }
        
        private void CreateNameInput(float x, float y)
        {
            var go = new GameObject("NameInput");
            go.transform.SetParent(_mainPanel.transform, false);
            go.layer = 5;
            
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(400, 40);
            
            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            img.raycastTarget = true;
            
            _nameInput = go.AddComponent<TMP_InputField>();
            
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRT = textGo.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10, 5);
            textRT.offsetMax = new Vector2(-10, -5);
            
            var txt = textGo.AddComponent<TextMeshProUGUI>();
            txt.text = PlayerPrefs.GetString("PlayerName", "Survivor");
            txt.fontSize = 24;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
            
            _nameInput.textComponent = txt;
            _nameInput.text = txt.text;
        }
        
        private void CreateBotButtons(float x, float y)
        {
            float startX = -275;
            
            for (int i = 0; i <= 10; i++)
            {
                float btnX = startX + i * 50;
                
                var go = new GameObject("BotBtn_" + i);
                go.transform.SetParent(_mainPanel.transform, false);
                go.layer = 5;
                
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(btnX, y);
                rt.sizeDelta = new Vector2(45, 45);
                
                var img = go.AddComponent<Image>();
                bool isSelected = (i == 3);
                img.color = isSelected ? new Color(1f, 0.75f, 0.1f) : new Color(0.2f, 0.2f, 0.25f);
                img.raycastTarget = true;
                
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
                
                var colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
                colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
                btn.colors = colors;
                
                int botCount = i;
                btn.onClick.AddListener(() => OnBotSelected(botCount));
                
                var textGo = new GameObject("Text");
                textGo.transform.SetParent(go.transform, false);
                var textRT = textGo.AddComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;
                
                var txt = textGo.AddComponent<TextMeshProUGUI>();
                txt.text = i.ToString();
                txt.fontSize = 20;
                txt.color = isSelected ? Color.black : Color.white;
                txt.alignment = TextAlignmentOptions.Center;
                
                _botButtons.Add(btn);
            }
        }
        
        private void CreateMapButtons()
        {
            var maps = new[]
            {
                ("forestvalley", "FOREST VALLEY"),
                ("rockycanyon", "ROCKY CANYON"),
                ("waterarena", "WATER ARENA"),
                ("koreantemple", "KOREAN TEMPLE"),
                ("volcanic", "VOLCANIC")
            };
            
            float cardWidth = 150;
            float cardHeight = 100;
            float spacing = 12;
            float startX = -((4 * cardWidth) + (3 * spacing)) / 2 + cardWidth / 2;
            float startY = 10;
            
            for (int i = 0; i < maps.Length; i++)
            {
                int row = i / 4;
                int col = i % 4;
                float btnX = startX + col * (cardWidth + spacing);
                float btnY = startY - row * (cardHeight + spacing);
                
                var (id, name) = maps[i];
                
                var go = new GameObject("MapBtn_" + id);
                go.transform.SetParent(_mainPanel.transform, false);
                go.layer = 5;
                
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(btnX, btnY);
                rt.sizeDelta = new Vector2(cardWidth, cardHeight);
                
                bool isSelected = (id == _selectedMapId);
                
                // Cargar thumbnail
                var thumbnail = LoadMapThumbnail(id);
                
                var img = go.AddComponent<Image>();
                if (thumbnail != null)
                {
                    img.sprite = thumbnail;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                }
                
                // Borde de selección
                if (isSelected)
                {
                    var outline = go.AddComponent<Outline>();
                    outline.effectColor = new Color(1f, 0.85f, 0.2f);
                    outline.effectDistance = new Vector2(4, 4);
                }
                
                img.raycastTarget = true;
                
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
                
                var colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
                colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
                btn.colors = colors;
                
                string mapId = id;
                btn.onClick.AddListener(() => OnMapSelected(mapId));
                
                // Overlay para el nombre del mapa
                var overlayGo = new GameObject("NameOverlay");
                overlayGo.transform.SetParent(go.transform, false);
                var overlayRT = overlayGo.AddComponent<RectTransform>();
                overlayRT.anchorMin = new Vector2(0, 0);
                overlayRT.anchorMax = new Vector2(1, 0);
                overlayRT.pivot = new Vector2(0.5f, 0);
                overlayRT.anchoredPosition = new Vector2(0, 0);
                overlayRT.sizeDelta = new Vector2(0, 24);
                
                var overlayImg = overlayGo.AddComponent<Image>();
                overlayImg.color = new Color(0, 0, 0, 0.6f);
                overlayImg.raycastTarget = false;
                
                var textGo = new GameObject("MapName");
                textGo.transform.SetParent(overlayGo.transform, false);
                var textRT = textGo.AddComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;
                
                var txt = textGo.AddComponent<TextMeshProUGUI>();
                txt.text = name;
                txt.fontSize = 12;
                txt.fontStyle = FontStyles.Bold;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.Center;
                
                _mapButtons.Add(btn);
            }
        }
        
        private Sprite LoadMapThumbnail(string mapId)
        {
            string path = "MapThumbnails/thumbnail_" + mapId;
            var texture = Resources.Load<Texture2D>(path);
            
            if (texture == null)
            {
                Debug.LogWarning("[WelcomeScreenUI] Thumbnail not found: " + path);
                return null;
            }
            
            // Crear sprite desde textura
            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
            
            return sprite;
        }
        
        private void CreatePlayButton(float x, float y)
        {
            var go = new GameObject("PlayButton");
            go.transform.SetParent(_mainPanel.transform, false);
            go.layer = 5;
            
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(350, 50);
            
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.7f, 0.25f);
            img.raycastTarget = true;
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;
            
            btn.onClick.AddListener(OnPlayClicked);
            
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRT = textGo.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            
            var txt = textGo.AddComponent<TextMeshProUGUI>();
            txt.text = "PLAY GAME";
            txt.fontSize = 28;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
        }
        
        private void CreateExitButton(float x, float y)
        {
            var go = new GameObject("ExitButton");
            go.transform.SetParent(_mainPanel.transform, false);
            go.layer = 5;
            
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(200, 40);
            
            var img = go.AddComponent<Image>();
            img.color = new Color(0.7f, 0.15f, 0.15f);
            img.raycastTarget = true;
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;
            
            btn.onClick.AddListener(() => Application.Quit());
            
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRT = textGo.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            
            var txt = textGo.AddComponent<TextMeshProUGUI>();
            txt.text = "EXIT";
            txt.fontSize = 24;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
        }
        
        private void OnBotSelected(int count)
        {
            Debug.Log("[WelcomeScreenUI] Bot selected: " + count);
            _selectedBotCount = count;
            PlayerPrefs.SetInt("BotCount", count);
            
            for (int i = 0; i < _botButtons.Count; i++)
            {
                var btn = _botButtons[i];
                var img = btn.GetComponent<Image>();
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                bool isSelected = (i == count);
                
                img.color = isSelected ? new Color(1f, 0.75f, 0.1f) : new Color(0.2f, 0.2f, 0.25f);
                txt.color = isSelected ? Color.black : Color.white;
            }
        }
        
        private void OnMapSelected(string mapId)
        {
            Debug.Log("[WelcomeScreenUI] Map selected: " + mapId);
            string previousMapId = _selectedMapId;
            _selectedMapId = mapId;
            PlayerPrefs.SetString("SelectedMap", mapId);
            
            // Actualizar visual de todos los botones
            foreach (var btn in _mapButtons)
            {
                var img = btn.GetComponent<Image>();
                var outline = btn.GetComponent<Outline>();
                var mapName = btn.name.Replace("MapBtn_", "");
                bool isSelected = (mapName == mapId);
                
                // Actualizar outline
                if (isSelected)
                {
                    if (outline == null)
                    {
                        outline = btn.gameObject.AddComponent<Outline>();
                    }
                    outline.effectColor = new Color(1f, 0.85f, 0.2f);
                    outline.effectDistance = new Vector2(4, 4);
                    
                    // Efecto de escala sutil
                    var rt = btn.GetComponent<RectTransform>();
                    rt.localScale = new Vector3(1.05f, 1.05f, 1f);
                }
                else
                {
                    if (outline != null)
                    {
                        Destroy(outline);
                    }
                    var rt = btn.GetComponent<RectTransform>();
                    rt.localScale = Vector3.one;
                }
            }
        }
        
        private void OnPlayClicked()
        {
            string playerName = _nameInput != null && !string.IsNullOrWhiteSpace(_nameInput.text) 
                ? _nameInput.text.Trim() 
                : "Survivor";
                
            Debug.Log("[WelcomeScreenUI] PLAY clicked: " + playerName + ", bots: " + _selectedBotCount + ", map: " + _selectedMapId);
            
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.SetInt("BotCount", _selectedBotCount);
            PlayerPrefs.SetString("SelectedMap", _selectedMapId);
            PlayerPrefs.SetString("GameMode", "Solo");
            PlayerPrefs.Save();
            
            OnStartGame?.Invoke(playerName, _selectedBotCount, _selectedMapId);
            Destroy(gameObject);
        }
    }
}

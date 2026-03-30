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
        public Action<int, string> OnStartGame;
        
        private GameObject _mainPanel;
        private TMP_InputField _nameInput;
        private int _selectedBotCount = 3;
        private Camera _camera;
        private List<Button> _botButtons = new List<Button>();
        private GameObject _eventSystem;
        
        private void Awake()
        {
            // Crear EventSystem PRIMERO (esencial para interacciones)
            CreateEventSystem();
            SetupCameraAndCanvas();
        }
        
        private void Start()
        {
            CreateBackground();
            CreateMainUI();
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
            // Panel principal
            _mainPanel = new GameObject("MainPanel");
            _mainPanel.transform.SetParent(transform, false);
            _mainPanel.layer = 5; // UI layer
            
            var panelRT = _mainPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(600, 700);
            
            var panelImg = _mainPanel.AddComponent<Image>();
            panelImg.color = new Color(0.15f, 0.15f, 0.2f, 0.98f);
            panelImg.raycastTarget = true;
            
            // BORDE dorado
            var outline = _mainPanel.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.2f, 0.6f);
            outline.effectDistance = new Vector2(3, -3);
            
            // SOMBRA
            var shadow = _mainPanel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(8, -8);
            
            // TITULO
            var titleGo = CreateText("ARENA BRAWLER", 56, new Color(1f, 0.85f, 0.2f), FontStyles.Bold);
            titleGo.transform.SetParent(_mainPanel.transform, false);
            var titleRT = titleGo.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1);
            titleRT.anchorMax = new Vector2(0.5f, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(0, -40);
            titleRT.sizeDelta = new Vector2(550, 70);
            
            // Subtitulo - más separado del título
            var subtitleGo = CreateText("Horde Survival", 28, new Color(0.7f, 0.7f, 0.8f), FontStyles.Normal);
            subtitleGo.transform.SetParent(_mainPanel.transform, false);
            var subtitleRT = subtitleGo.GetComponent<RectTransform>();
            subtitleRT.anchorMin = new Vector2(0.5f, 1);
            subtitleRT.anchorMax = new Vector2(0.5f, 1);
            subtitleRT.pivot = new Vector2(0.5f, 1);
            subtitleRT.anchoredPosition = new Vector2(0, -115);
            subtitleRT.sizeDelta = new Vector2(550, 35);
            
            // Label Nombre - más separado
            var nameLabel = CreateText("PLAYER NAME", 24, new Color(1f, 0.85f, 0.2f), FontStyles.Bold);
            nameLabel.transform.SetParent(_mainPanel.transform, false);
            var nameLabelRT = nameLabel.GetComponent<RectTransform>();
            nameLabelRT.anchorMin = new Vector2(0.5f, 1);
            nameLabelRT.anchorMax = new Vector2(0.5f, 1);
            nameLabelRT.pivot = new Vector2(0.5f, 1);
            nameLabelRT.anchoredPosition = new Vector2(0, -180);
            nameLabelRT.sizeDelta = new Vector2(500, 30);
            
            // INPUT NOMBRE
            var inputGo = CreateInputField(PlayerPrefs.GetString("PlayerName", "Survivor"));
            inputGo.transform.SetParent(_mainPanel.transform, false);
            var inputRT = inputGo.GetComponent<RectTransform>();
            inputRT.anchorMin = new Vector2(0.5f, 1);
            inputRT.anchorMax = new Vector2(0.5f, 1);
            inputRT.pivot = new Vector2(0.5f, 1);
            inputRT.anchoredPosition = new Vector2(0, -225);
            inputRT.sizeDelta = new Vector2(500, 50);
            _nameInput = inputGo.GetComponent<TMP_InputField>();
            
            // Label Bots - más separado
            var botsLabel = CreateText("ALLIED BOTS", 24, new Color(1f, 0.85f, 0.2f), FontStyles.Bold);
            botsLabel.transform.SetParent(_mainPanel.transform, false);
            var botsLabelRT = botsLabel.GetComponent<RectTransform>();
            botsLabelRT.anchorMin = new Vector2(0.5f, 1);
            botsLabelRT.anchorMax = new Vector2(0.5f, 1);
            botsLabelRT.pivot = new Vector2(0.5f, 1);
            botsLabelRT.anchoredPosition = new Vector2(0, -310);
            botsLabelRT.sizeDelta = new Vector2(500, 30);
            
            // BOTONES BOTS
            CreateBotButtons();
            
            // BOTON PLAY
            var playBtn = CreateButton("PLAY GAME", new Color(0.15f, 0.65f, 0.2f));
            playBtn.transform.SetParent(_mainPanel.transform, false);
            var playBtnRT = playBtn.GetComponent<RectTransform>();
            playBtnRT.anchorMin = new Vector2(0.5f, 0);
            playBtnRT.anchorMax = new Vector2(0.5f, 0);
            playBtnRT.pivot = new Vector2(0.5f, 0);
            playBtnRT.anchoredPosition = new Vector2(0, 160);
            playBtnRT.sizeDelta = new Vector2(400, 75);
            
            var playButton = playBtn.GetComponent<Button>();
            playButton.onClick.AddListener(OnPlayClicked);
            
            // BOTON EXIT
            var exitBtn = CreateButton("EXIT", new Color(0.6f, 0.12f, 0.12f));
            exitBtn.transform.SetParent(_mainPanel.transform, false);
            var exitBtnRT = exitBtn.GetComponent<RectTransform>();
            exitBtnRT.anchorMin = new Vector2(0.5f, 0);
            exitBtnRT.anchorMax = new Vector2(0.5f, 0);
            exitBtnRT.pivot = new Vector2(0.5f, 0);
            exitBtnRT.anchoredPosition = new Vector2(0, 70);
            exitBtnRT.sizeDelta = new Vector2(300, 55);
            
            var exitButton = exitBtn.GetComponent<Button>();
            exitButton.onClick.AddListener(() => Application.Quit());
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
                
                _botButtons.Add(btn);
            }
        }
        
        private void OnBotSelected(int count)
        {
            _selectedBotCount = count;
            PlayerPrefs.SetInt("BotCount", count);
            
            for (int i = 0; i <= 10; i++)
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
            PlayerPrefs.SetString("GameMode", "Solo");
            PlayerPrefs.SetString("FromWelcomeScreen", "true");
            PlayerPrefs.Save();
            
            OnStartGame?.Invoke(_selectedBotCount, playerName);
            Destroy(gameObject);
        }
        
        private void OnDestroy()
        {
            if (_eventSystem != null)
                Destroy(_eventSystem);
        }
    }
}

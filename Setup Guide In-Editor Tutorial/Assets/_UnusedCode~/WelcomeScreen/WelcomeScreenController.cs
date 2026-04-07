using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Modern Welcome Screen controller using Unity UI Toolkit.
    /// Premium violet theme with glassmorphism design.
    /// </summary>
    public class WelcomeScreenController : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;
        
        [Header("UXML Source")]
        [SerializeField] private VisualTreeAsset welcomeScreenUXML;
        
        [Header("Events")]
        public Action<int, string> OnStartGame;
        
        // State
        private int _selectedBotCount = 3;
        private string _selectedMapId = "original";
        private string _playerName = "Survivor";
        
        // UI Element References
        private TextField _nameInput;
        private List<Button> _botButtons = new List<Button>();
        private List<Button> _mapButtons = new List<Button>();
        private Button _playButton;
        private Button _exitButton;
        
        // USS Class Names
        private const string BOT_SELECTED_CLASS = "bot-button--selected";
        private const string MAP_SELECTED_CLASS = "map-card--selected";
        
        private void Awake()
        {
            SetupUIDocument();
        }
        
        private void Start()
        {
            LoadSavedData();
            InitializeUI();
            RegisterCallbacks();
        }
        
        private void SetupUIDocument()
        {
            // Get or create UIDocument
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
            
            if (uiDocument == null)
                uiDocument = gameObject.AddComponent<UIDocument>();
            
            // Assign the UXML
            if (welcomeScreenUXML != null)
            {
                uiDocument.visualTreeAsset = welcomeScreenUXML;
            }
            else
            {
                // Try to load from default path
                welcomeScreenUXML = Resources.Load<VisualTreeAsset>("UI/WelcomeScreen/UI_WelcomeScreen");
                if (welcomeScreenUXML == null)
                {
                    Debug.LogError("[WelcomeScreen] Could not find UI_WelcomeScreen.uxml. Please assign it in the inspector.");
                }
                else
                {
                    uiDocument.visualTreeAsset = welcomeScreenUXML;
                }
            }
            
            // UI Toolkit will use default PanelSettings if none assigned
            // Don't create runtime PanelSettings as they may not persist correctly
        }
        
        /// <summary>
        /// Allows external assignment of UXML (e.g., from ArenaBootstrap)
        /// </summary>
        public void SetUXML(VisualTreeAsset uxml)
        {
            welcomeScreenUXML = uxml;
            if (uiDocument != null)
            {
                uiDocument.visualTreeAsset = uxml;
            }
        }
        
        private void LoadSavedData()
        {
            _playerName = PlayerPrefs.GetString("PlayerName", "Survivor");
            _selectedBotCount = PlayerPrefs.GetInt("BotCount", 3);
            _selectedMapId = PlayerPrefs.GetString("SelectedMap", "original");
        }
        
        private void InitializeUI()
        {
            var root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[WelcomeScreen] Root visual element is null!");
                return;
            }
            
            // Cache references
            _nameInput = root.Q<TextField>("name-input");
            _playButton = root.Q<Button>("play-button");
            _exitButton = root.Q<Button>("exit-button");
            
            // Cache bot buttons
            _botButtons.Clear();
            for (int i = 0; i <= 10; i++)
            {
                var btn = root.Q<Button>($"bot-{i}");
                if (btn != null)
                {
                    _botButtons.Add(btn);
                    // Set initial selection state
                    UpdateBotButtonState(btn, i == _selectedBotCount);
                }
            }
            
            // Cache map buttons
            _mapButtons.Clear();
            var mapOriginal = root.Q<Button>("map-original");
            var mapForest = root.Q<Button>("map-forest");
            if (mapOriginal != null) _mapButtons.Add(mapOriginal);
            if (mapForest != null) _mapButtons.Add(mapForest);
            
            // Set initial map selection
            UpdateMapButtonStates();
            
            // Set initial name
            if (_nameInput != null)
            {
                _nameInput.value = _playerName;
            }
        }
        
        private void RegisterCallbacks()
        {
            // Bot buttons
            for (int i = 0; i < _botButtons.Count; i++)
            {
                int botCount = i; // Capture for closure
                _botButtons[i].clicked += () => OnBotSelected(botCount);
            }
            
            // Map buttons
            foreach (var btn in _mapButtons)
            {
                btn.clicked += () => OnMapSelected(btn);
            }
            
            // Action buttons
            if (_playButton != null)
                _playButton.clicked += OnPlayClicked;
            
            if (_exitButton != null)
                _exitButton.clicked += OnExitClicked;
            
            // Name input
            if (_nameInput != null)
            {
                _nameInput.RegisterCallback<ChangeEvent<string>>(OnNameChanged);
            }
        }
        
        private void OnBotSelected(int count)
        {
            _selectedBotCount = count;
            PlayerPrefs.SetInt("BotCount", count);
            
            // Update visual state
            for (int i = 0; i < _botButtons.Count; i++)
            {
                UpdateBotButtonState(_botButtons[i], i == count);
            }
            
            Debug.Log($"[WelcomeScreen] Selected {count} bots");
        }
        
        private void UpdateBotButtonState(Button btn, bool selected)
        {
            if (selected)
            {
                btn.style.backgroundColor = new Color(0.96f, 0.62f, 0.04f); // Gold
                btn.style.borderLeftColor = new Color(0.99f, 0.82f, 0.30f);
                btn.style.borderRightColor = new Color(0.99f, 0.82f, 0.30f);
                btn.style.borderTopColor = new Color(0.99f, 0.82f, 0.30f);
                btn.style.borderBottomColor = new Color(0.99f, 0.82f, 0.30f);
                btn.style.color = new Color(0.06f, 0.02f, 0.14f); // Dark
                btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                btn.style.backgroundColor = new Color(0.18f, 0.11f, 0.47f); // Violet
                btn.style.borderLeftColor = new Color(0.24f, 0.14f, 0.40f);
                btn.style.borderRightColor = new Color(0.24f, 0.14f, 0.40f);
                btn.style.borderTopColor = new Color(0.24f, 0.14f, 0.40f);
                btn.style.borderBottomColor = new Color(0.24f, 0.14f, 0.40f);
                btn.style.color = new Color(0.91f, 0.84f, 1f); // Light
                btn.style.unityFontStyleAndWeight = FontStyle.Normal;
            }
        }
        
        private void OnMapSelected(Button selectedBtn)
        {
            // Determine map ID from button name
            if (selectedBtn.name.Contains("original"))
                _selectedMapId = "original";
            else if (selectedBtn.name.Contains("forest"))
                _selectedMapId = "forest";
            
            PlayerPrefs.SetString("SelectedMap", _selectedMapId);
            
            // Update visual states
            UpdateMapButtonStates();
            
            Debug.Log($"[WelcomeScreen] Selected map: {_selectedMapId}");
        }
        
        private void UpdateMapButtonStates()
        {
            foreach (var btn in _mapButtons)
            {
                bool isSelected = false;
                
                if (_selectedMapId == "original" && btn.name.Contains("original"))
                    isSelected = true;
                else if (_selectedMapId == "forest" && btn.name.Contains("forest"))
                    isSelected = true;
                
                if (isSelected)
                {
                    btn.style.backgroundColor = new Color(0.42f, 0.13f, 0.66f); // Primary violet
                    btn.style.borderLeftColor = new Color(0.66f, 0.33f, 0.97f);
                    btn.style.borderRightColor = new Color(0.66f, 0.33f, 0.97f);
                    btn.style.borderTopColor = new Color(0.66f, 0.33f, 0.97f);
                    btn.style.borderBottomColor = new Color(0.66f, 0.33f, 0.97f);
                    var label = btn.Q<Label>();
                    if (label != null) label.style.color = new Color(0.96f, 0.62f, 0.04f); // Gold
                }
                else
                {
                    btn.style.backgroundColor = new Color(0.18f, 0.11f, 0.47f); // Dark violet
                    btn.style.borderLeftColor = new Color(0.24f, 0.14f, 0.40f);
                    btn.style.borderRightColor = new Color(0.24f, 0.14f, 0.40f);
                    btn.style.borderTopColor = new Color(0.24f, 0.14f, 0.40f);
                    btn.style.borderBottomColor = new Color(0.24f, 0.14f, 0.40f);
                    var label = btn.Q<Label>();
                    if (label != null) label.style.color = new Color(1f, 1f, 1f); // White
                }
            }
        }
        
        private void OnNameChanged(ChangeEvent<string> evt)
        {
            _playerName = evt.newValue?.Trim() ?? "Survivor";
            PlayerPrefs.SetString("PlayerName", _playerName);
        }
        
        private void OnPlayClicked()
        {
            // Validate name
            string finalName = string.IsNullOrWhiteSpace(_playerName) ? "Survivor" : _playerName.Trim();
            
            Debug.Log($"[WelcomeScreen] START GAME: {finalName} with {_selectedBotCount} bots on {_selectedMapId}");
            
            // Save all settings
            PlayerPrefs.SetString("PlayerName", finalName);
            PlayerPrefs.SetInt("BotCount", _selectedBotCount);
            PlayerPrefs.SetString("SelectedMap", _selectedMapId);
            PlayerPrefs.SetString("GameMode", "Solo");
            PlayerPrefs.SetString("FromWelcomeScreen", "true");
            PlayerPrefs.Save();
            
            // Trigger event
            OnStartGame?.Invoke(_selectedBotCount, finalName);
            
            // Clean up
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
            // Unregister callbacks to prevent memory leaks
            if (_playButton != null)
                _playButton.clicked -= OnPlayClicked;
            
            if (_exitButton != null)
                _exitButton.clicked -= OnExitClicked;
            
            foreach (var btn in _botButtons)
            {
                if (btn != null)
                    btn.clicked -= () => { };
            }
            
            foreach (var btn in _mapButtons)
            {
                if (btn != null)
                    btn.clicked -= () => { };
            }
        }
    }
}

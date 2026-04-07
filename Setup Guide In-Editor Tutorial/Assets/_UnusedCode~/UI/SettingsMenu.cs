using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de menú de ajustes completo.
    /// Gráficos, audio, controles e idioma.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        public static SettingsMenu Instance { get; private set; }

        [Header("Menu Panels")]
        [SerializeField] private RectTransform mainPanel;
        [SerializeField] private RectTransform graphicsPanel;
        [SerializeField] private RectTransform audioPanel;
        [SerializeField] private RectTransform controlsPanel;
        [SerializeField] private RectTransform languagePanel;

        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Slider shadowDistanceSlider;
        [SerializeField] private TMP_Text shadowDistanceText;

        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider voiceVolumeSlider;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private TMP_Text masterVolumeText;
        [SerializeField] private TMP_Text musicVolumeText;
        [SerializeField] private TMP_Text sfxVolumeText;
        [SerializeField] private TMP_Text voiceVolumeText;

        [Header("Language Settings")]
        [SerializeField] private TMP_Dropdown languageDropdown;

        [Header("Control Settings")]
        [SerializeField] private Transform controlsContainer;
        [SerializeField] private GameObject keyBindingPrefab;

        [Header("Buttons")]
        [SerializeField] private Button graphicsTabButton;
        [SerializeField] private Button audioTabButton;
        [SerializeField] private Button controlsTabButton;
        [SerializeField] private Button languageTabButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button backButton;

        // Settings data
        private SettingsData _currentSettings;
        private SettingsData _originalSettings;
        private Resolution[] _availableResolutions;
        private bool _isRebindingKey = false;

        public bool IsOpen => mainPanel != null && mainPanel.gameObject.activeSelf;

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
            LoadSettings();
            SetupUI();
            SetupListeners();
            ShowPanel(null); // Hide all initially
        }

        #region Initialization

        private void SetupUI()
        {
            SetupQualityDropdown();
            SetupResolutionDropdown();
            SetupLanguageDropdown();
            SetupControlsBindings();
            
            // Apply current settings to UI
            ApplySettingsToUI();
        }

        private void SetupQualityDropdown()
        {
            if (qualityDropdown == null) return;
            
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string> { "Low", "Medium", "High", "Ultra" });
        }

        private void SetupResolutionDropdown()
        {
            if (resolutionDropdown == null) return;
            
            resolutionDropdown.ClearOptions();
            _availableResolutions = Screen.resolutions;
            
            List<string> options = new List<string>();
            int currentIndex = 0;
            
            for (int i = 0; i < _availableResolutions.Length; i++)
            {
                Resolution res = _availableResolutions[i];
                string option = $"{res.width}x{res.height} @{res.refreshRateRatio.value:F2}Hz";
                options.Add(option);
                
                if (res.width == Screen.currentResolution.width && 
                    res.height == Screen.currentResolution.height)
                {
                    currentIndex = i;
                }
            }
            
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentIndex;
        }

        private void SetupLanguageDropdown()
        {
            if (languageDropdown == null || LocalizationManager.Instance == null) return;
            
            languageDropdown.ClearOptions();
            List<string> options = new List<string>();
            
            foreach (var lang in LocalizationManager.Instance.GetAvailableLanguages())
            {
                options.Add(LocalizationManager.Instance.GetLanguageName(lang));
            }
            
            languageDropdown.AddOptions(options);
            languageDropdown.value = (int)LocalizationManager.Instance.CurrentLanguage;
        }

        private void SetupControlsBindings()
        {
            if (controlsContainer == null || keyBindingPrefab == null) return;
            
            // Clear existing
            foreach (Transform child in controlsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create binding entries
            CreateKeyBindingEntry("Move Up", KeyCode.W);
            CreateKeyBindingEntry("Move Down", KeyCode.S);
            CreateKeyBindingEntry("Move Left", KeyCode.A);
            CreateKeyBindingEntry("Move Right", KeyCode.D);
            CreateKeyBindingEntry("Jump", KeyCode.Space);
            CreateKeyBindingEntry("Dash", KeyCode.LeftShift);
            CreateKeyBindingEntry("Attack", KeyCode.Mouse0);
            CreateKeyBindingEntry("Drop Weapon", KeyCode.G);
            CreateKeyBindingEntry("Ability 1", KeyCode.Alpha1);
            CreateKeyBindingEntry("Ability 2", KeyCode.Alpha2);
            CreateKeyBindingEntry("Ability 3", KeyCode.Alpha3);
            CreateKeyBindingEntry("Pause", KeyCode.Escape);
        }

        private void CreateKeyBindingEntry(string actionName, KeyCode defaultKey)
        {
            GameObject entry = Instantiate(keyBindingPrefab, controlsContainer);
            
            TextMeshProUGUI actionText = entry.transform.Find("ActionText")?.GetComponent<TextMeshProUGUI>();
            Button keyButton = entry.transform.Find("KeyButton")?.GetComponent<Button>();
            TextMeshProUGUI keyText = keyButton?.transform.Find("KeyText")?.GetComponent<TextMeshProUGUI>();
            
            if (actionText != null)
                actionText.text = actionName;
            
            if (keyText != null)
                keyText.text = defaultKey.ToString();
            
            if (keyButton != null)
            {
                keyButton.onClick.AddListener(() => StartRebinding(actionName, keyButton, keyText));
            }
        }

        private void SetupListeners()
        {
            // Tab buttons
            if (graphicsTabButton != null)
                graphicsTabButton.onClick.AddListener(() => ShowPanel(graphicsPanel));
            if (audioTabButton != null)
                audioTabButton.onClick.AddListener(() => ShowPanel(audioPanel));
            if (controlsTabButton != null)
                controlsTabButton.onClick.AddListener(() => ShowPanel(controlsPanel));
            if (languageTabButton != null)
                languageTabButton.onClick.AddListener(() => ShowPanel(languagePanel));
            
            // Action buttons
            if (saveButton != null)
                saveButton.onClick.AddListener(SaveSettings);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelSettings);
            if (backButton != null)
                backButton.onClick.AddListener(CloseMenu);
            
            // Graphics listeners
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (vsyncToggle != null)
                vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            if (shadowDistanceSlider != null)
                shadowDistanceSlider.onValueChanged.AddListener(OnShadowDistanceChanged);
            
            // Audio listeners
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            if (voiceVolumeSlider != null)
                voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
            if (muteToggle != null)
                muteToggle.onValueChanged.AddListener(OnMuteChanged);
            
            // Language listener
            if (languageDropdown != null)
                languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        #endregion

        #region UI Updates

        private void ApplySettingsToUI()
        {
            // Graphics
            if (qualityDropdown != null)
                qualityDropdown.value = _currentSettings.qualityLevel;
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = _currentSettings.fullscreen;
            if (vsyncToggle != null)
                vsyncToggle.isOn = _currentSettings.vsync;
            if (shadowDistanceSlider != null)
            {
                shadowDistanceSlider.value = _currentSettings.shadowDistance;
                UpdateShadowDistanceText(_currentSettings.shadowDistance);
            }
            
            // Audio
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = _currentSettings.masterVolume;
                UpdateVolumeText(masterVolumeText, _currentSettings.masterVolume);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = _currentSettings.musicVolume;
                UpdateVolumeText(musicVolumeText, _currentSettings.musicVolume);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = _currentSettings.sfxVolume;
                UpdateVolumeText(sfxVolumeText, _currentSettings.sfxVolume);
            }
            if (voiceVolumeSlider != null)
            {
                voiceVolumeSlider.value = _currentSettings.voiceVolume;
                UpdateVolumeText(voiceVolumeText, _currentSettings.voiceVolume);
            }
            if (muteToggle != null)
                muteToggle.isOn = _currentSettings.mute;
        }

        private void UpdateShadowDistanceText(float value)
        {
            if (shadowDistanceText != null)
                shadowDistanceText.text = $"{value:F0}m";
        }

        private void UpdateVolumeText(TMP_Text text, float value)
        {
            if (text != null)
                text.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        #endregion

        #region Event Handlers

        private void OnQualityChanged(int value)
        {
            _currentSettings.qualityLevel = value;
            QualitySettings.SetQualityLevel(value);
        }

        private void OnResolutionChanged(int value)
        {
            if (value < 0 || value >= _availableResolutions.Length) return;
            
            Resolution res = _availableResolutions[value];
            Screen.SetResolution(res.width, res.height, _currentSettings.fullscreen);
            _currentSettings.resolutionIndex = value;
        }

        private void OnFullscreenChanged(bool value)
        {
            _currentSettings.fullscreen = value;
            Screen.fullScreen = value;
        }

        private void OnVSyncChanged(bool value)
        {
            _currentSettings.vsync = value;
            QualitySettings.vSyncCount = value ? 1 : 0;
        }

        private void OnShadowDistanceChanged(float value)
        {
            _currentSettings.shadowDistance = value;
            QualitySettings.shadowDistance = value;
            UpdateShadowDistanceText(value);
        }

        private void OnMasterVolumeChanged(float value)
        {
            _currentSettings.masterVolume = value;
            AudioListener.volume = value;
            UpdateVolumeText(masterVolumeText, value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            _currentSettings.musicVolume = value;
            UpdateVolumeText(musicVolumeText, value);
            // Aplicar a AudioMixer si existe
            ApplyToAudioMixer("MusicVolume", value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            _currentSettings.sfxVolume = value;
            UpdateVolumeText(sfxVolumeText, value);
            ApplyToAudioMixer("SFXVolume", value);
        }

        private void OnVoiceVolumeChanged(float value)
        {
            _currentSettings.voiceVolume = value;
            UpdateVolumeText(voiceVolumeText, value);
            ApplyToAudioMixer("VoiceVolume", value);
        }

        private void OnMuteChanged(bool value)
        {
            _currentSettings.mute = value;
            AudioListener.pause = value;
        }

        private void OnLanguageChanged(int value)
        {
            Language lang = (Language)value;
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLanguage(lang);
            }
        }

        private void ApplyToAudioMixer(string paramName, float value)
        {
            // Si tienes AudioMixer, descomenta:
            // if (audioMixer != null)
            //     audioMixer.SetFloat(paramName, Mathf.Log10(value) * 20);
        }

        #endregion

        #region Key Rebinding

        private void StartRebinding(string actionName, Button button, TextMeshProUGUI keyText)
        {
            if (_isRebindingKey) return;
            
            _isRebindingKey = true;
            keyText.text = "Press key...";
            
            StartCoroutine(WaitForKeyPress(actionName, button, keyText));
        }

        private System.Collections.IEnumerator WaitForKeyPress(string actionName, Button button, TextMeshProUGUI keyText)
        {
            yield return new WaitForSeconds(0.1f);
            
            while (_isRebindingKey)
            {
                // Detectar tecla presionada
                foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(keyCode))
                    {
                        keyText.text = keyCode.ToString();
                        _currentSettings.keyBindings[actionName] = keyCode;
                        _isRebindingKey = false;
                        yield break;
                    }
                }
                yield return null;
            }
        }

        #endregion

        #region Menu Control

        private void ShowPanel(RectTransform panel)
        {
            // Hide all
            if (graphicsPanel != null) graphicsPanel.gameObject.SetActive(false);
            if (audioPanel != null) audioPanel.gameObject.SetActive(false);
            if (controlsPanel != null) controlsPanel.gameObject.SetActive(false);
            if (languagePanel != null) languagePanel.gameObject.SetActive(false);
            
            // Show requested
            if (panel != null)
            {
                panel.gameObject.SetActive(true);
                
                // Animación de entrada
                MenuTransitionManager.Instance?.ShowPanel(panel, TransitionType.Fade, 0.2f);
            }
        }

        public void OpenMenu()
        {
            if (mainPanel == null) return;
            
            _originalSettings = _currentSettings.Clone();
            mainPanel.gameObject.SetActive(true);
            ShowPanel(graphicsPanel);
            
            MenuTransitionManager.Instance?.ShowPanel(mainPanel, TransitionType.SlideFromBottom);
        }

        public void CloseMenu()
        {
            if (mainPanel == null) return;
            
            MenuTransitionManager.Instance?.HidePanel(mainPanel, TransitionType.SlideBottom, onComplete: () => {
                mainPanel.gameObject.SetActive(false);
            });
        }

        private void SaveSettings()
        {
            string json = JsonUtility.ToJson(_currentSettings);
            PlayerPrefs.SetString("GameSettings", json);
            PlayerPrefs.Save();
            
            _originalSettings = _currentSettings.Clone();
            
#if DEBUG
            Debug.Log("[Settings] Settings saved");
#endif
            
            CloseMenu();
        }

        private void CancelSettings()
        {
            _currentSettings = _originalSettings.Clone();
            ApplySettingsToUI();
            CloseMenu();
        }

        #endregion

        #region Load/Save

        private void LoadSettings()
        {
            string json = PlayerPrefs.GetString("GameSettings", "");
            
            if (!string.IsNullOrEmpty(json))
            {
                _currentSettings = JsonUtility.FromJson<SettingsData>(json);
            }
            else
            {
                _currentSettings = SettingsData.Default();
            }
            
            _originalSettings = _currentSettings.Clone();
            
            // Apply loaded settings
            ApplyLoadedSettings();
        }

        private void ApplyLoadedSettings()
        {
            // Graphics
            QualitySettings.SetQualityLevel(_currentSettings.qualityLevel);
            Screen.fullScreen = _currentSettings.fullscreen;
            QualitySettings.vSyncCount = _currentSettings.vsync ? 1 : 0;
            QualitySettings.shadowDistance = _currentSettings.shadowDistance;
            
            // Audio
            AudioListener.volume = _currentSettings.masterVolume;
            AudioListener.pause = _currentSettings.mute;
        }

        #endregion
    }

    [System.Serializable]
    public class SettingsData
    {
        public int qualityLevel = 2; // High
        public int resolutionIndex = 0;
        public bool fullscreen = true;
        public bool vsync = true;
        public float shadowDistance = 100f;
        
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public float voiceVolume = 1f;
        public bool mute = false;
        
        public string language = "Spanish";
        
        public Dictionary<string, KeyCode> keyBindings = new Dictionary<string, KeyCode>();

        public static SettingsData Default()
        {
            return new SettingsData
            {
                qualityLevel = QualitySettings.GetQualityLevel(),
                fullscreen = Screen.fullScreen,
                masterVolume = 1f,
                musicVolume = 0.8f,
                sfxVolume = 1f,
                voiceVolume = 1f,
                shadowDistance = 100f,
                keyBindings = new Dictionary<string, KeyCode>()
            };
        }

        public SettingsData Clone()
        {
            return new SettingsData
            {
                qualityLevel = this.qualityLevel,
                resolutionIndex = this.resolutionIndex,
                fullscreen = this.fullscreen,
                vsync = this.vsync,
                shadowDistance = this.shadowDistance,
                masterVolume = this.masterVolume,
                musicVolume = this.musicVolume,
                sfxVolume = this.sfxVolume,
                voiceVolume = this.voiceVolume,
                mute = this.mute,
                language = this.language,
                keyBindings = new Dictionary<string, KeyCode>(this.keyBindings)
            };
        }
    }
}

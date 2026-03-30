using UnityEngine;
using System.Collections.Generic;
using System;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de localización multi-idioma.
    /// Soporta español, inglés y extensión a más idiomas.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Idioma por defecto")]
        [SerializeField] private Language defaultLanguage = Language.Spanish;

        [Tooltip("Idioma actual")]
        [SerializeField] private Language currentLanguage;

        [Header("Debug")]
        [SerializeField] private bool showMissingKeys = true;

        // Diccionario de traducciones: [idioma][clave] = traducción
        private Dictionary<Language, Dictionary<string, string>> _translations;
        
        // Evento cuando cambia el idioma
        public static event Action<Language> OnLanguageChanged;

        public Language CurrentLanguage => currentLanguage;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeTranslations();
            
            // Cargar idioma guardado
            string savedLang = PlayerPrefs.GetString("SelectedLanguage", defaultLanguage.ToString());
            if (Enum.TryParse<Language>(savedLang, out Language lang))
            {
                currentLanguage = lang;
            }
            else
            {
                currentLanguage = defaultLanguage;
            }
        }

        private void Start()
        {
            OnLanguageChanged?.Invoke(currentLanguage);
        }

        /// <summary>
        /// Inicializa todas las traducciones
        /// </summary>
        private void InitializeTranslations()
        {
            _translations = new Dictionary<Language, Dictionary<string, string>>();

            // Español
            _translations[Language.Spanish] = new Dictionary<string, string>
            {
                // General
                ["game_title"] = "Arena Brawler",
                ["play"] = "JUGAR",
                ["settings"] = "AJUSTES",
                ["quit"] = "SALIR",
                ["back"] = "VOLVER",
                ["save"] = "GUARDAR",
                ["cancel"] = "CANCELAR",
                ["apply"] = "APLICAR",
                ["yes"] = "SÍ",
                ["no"] = "NO",
                ["ok"] = "OK",
                ["loading"] = "Cargando...",
                ["wave"] = "Ola",
                ["level"] = "Nivel",
                ["points"] = "Puntos",
                ["health"] = "Salud",
                ["ammo"] = "Munición",
                ["weapon"] = "Arma",
                
                // Menú principal
                ["welcome"] = "¡Bienvenido!",
                ["select_bots"] = "Selecciona aliados",
                ["enter_name"] = "Introduce tu nombre",
                ["start_game"] = "INICIAR JUEGO",
                
                // HUD
                ["victory"] = "¡VICTORIA!",
                ["defeat"] = "¡DERROTA!",
                ["game_over"] = "GAME OVER",
                ["restart"] = "REINTENTAR",
                ["wave_complete"] = "¡Ola completada!",
                ["boss_approaching"] = "¡Jefe en camino!",
                
                // Controles
                ["controls"] = "CONTROLES",
                ["keyboard"] = "Teclado",
                ["gamepad"] = "Mando",
                ["move"] = "Mover",
                ["attack"] = "Atacar",
                ["jump"] = "Saltar",
                ["dash"] = "Dash",
                ["interact"] = "Interactuar",
                ["drop_weapon"] = "Soltar arma",
                ["aim"] = "Apuntar",
                
                // Ajustes
                ["graphics"] = "GRÁFICOS",
                ["audio"] = "AUDIO",
                ["language"] = "IDIOMA",
                ["controls_settings"] = "CONTROLES",
                
                // Gráficos
                ["quality"] = "Calidad",
                ["low"] = "Baja",
                ["medium"] = "Media",
                ["high"] = "Alta",
                ["ultra"] = "Ultra",
                ["resolution"] = "Resolución",
                ["fullscreen"] = "Pantalla completa",
                ["vsync"] = "V-Sync",
                ["shadows"] = "Sombras",
                ["effects"] = "Efectos",
                
                // Audio
                ["master_volume"] = "Volumen maestro",
                ["music_volume"] = "Música",
                ["sfx_volume"] = "Efectos",
                ["voice_volume"] = "Voces",
                ["mute"] = "Silenciar",
                
                // Idiomas
                ["spanish"] = "Español",
                ["english"] = "Inglés",
                ["french"] = "Francés",
                ["german"] = "Alemán",
                ["italian"] = "Italiano",
                ["portuguese"] = "Portugués",
                
                // Armas
                ["assault_rifle"] = "Rifle de Asalto",
                ["shotgun"] = "Escopeta",
                ["flamethrower"] = "Lanzallamas",
                ["katana"] = "Katana de Hielo",
                ["no_weapon"] = "Sin arma",
                
                // Enemigos
                ["normal_enemy"] = "Enemigo",
                ["boss"] = "Jefe",
                ["wave_enemies"] = "Enemigos restantes",
                
                // Feedback
                ["critical_hit"] = "¡GOLPE CRÍTICO!",
                ["new_record"] = "¡Nuevo récord!",
                ["level_up"] = "¡Subida de nivel!",
                ["weapon_broken"] = "¡Arma rota!",
                ["low_ammo"] = "¡Poca munición!",
            };

            // Inglés
            _translations[Language.English] = new Dictionary<string, string>
            {
                // General
                ["game_title"] = "Arena Brawler",
                ["play"] = "PLAY",
                ["settings"] = "SETTINGS",
                ["quit"] = "QUIT",
                ["back"] = "BACK",
                ["save"] = "SAVE",
                ["cancel"] = "CANCEL",
                ["apply"] = "APPLY",
                ["yes"] = "YES",
                ["no"] = "NO",
                ["ok"] = "OK",
                ["loading"] = "Loading...",
                ["wave"] = "Wave",
                ["level"] = "Level",
                ["points"] = "Points",
                ["health"] = "Health",
                ["ammo"] = "Ammo",
                ["weapon"] = "Weapon",
                
                // Main menu
                ["welcome"] = "Welcome!",
                ["select_bots"] = "Select allies",
                ["enter_name"] = "Enter your name",
                ["start_game"] = "START GAME",
                
                // HUD
                ["victory"] = "VICTORY!",
                ["defeat"] = "DEFEAT!",
                ["game_over"] = "GAME OVER",
                ["restart"] = "RETRY",
                ["wave_complete"] = "Wave complete!",
                ["boss_approaching"] = "Boss approaching!",
                
                // Controls
                ["controls"] = "CONTROLS",
                ["keyboard"] = "Keyboard",
                ["gamepad"] = "Gamepad",
                ["move"] = "Move",
                ["attack"] = "Attack",
                ["jump"] = "Jump",
                ["dash"] = "Dash",
                ["interact"] = "Interact",
                ["drop_weapon"] = "Drop weapon",
                ["aim"] = "Aim",
                
                // Settings
                ["graphics"] = "GRAPHICS",
                ["audio"] = "AUDIO",
                ["language"] = "LANGUAGE",
                ["controls_settings"] = "CONTROLS",
                
                // Graphics
                ["quality"] = "Quality",
                ["low"] = "Low",
                ["medium"] = "Medium",
                ["high"] = "High",
                ["ultra"] = "Ultra",
                ["resolution"] = "Resolution",
                ["fullscreen"] = "Fullscreen",
                ["vsync"] = "V-Sync",
                ["shadows"] = "Shadows",
                ["effects"] = "Effects",
                
                // Audio
                ["master_volume"] = "Master volume",
                ["music_volume"] = "Music",
                ["sfx_volume"] = "SFX",
                ["voice_volume"] = "Voice",
                ["mute"] = "Mute",
                
                // Languages
                ["spanish"] = "Spanish",
                ["english"] = "English",
                ["french"] = "French",
                ["german"] = "German",
                ["italian"] = "Italian",
                ["portuguese"] = "Portuguese",
                
                // Weapons
                ["assault_rifle"] = "Assault Rifle",
                ["shotgun"] = "Shotgun",
                ["flamethrower"] = "Flamethrower",
                ["katana"] = "Ice Katana",
                ["no_weapon"] = "No weapon",
                
                // Enemies
                ["normal_enemy"] = "Enemy",
                ["boss"] = "Boss",
                ["wave_enemies"] = "Enemies remaining",
                
                // Feedback
                ["critical_hit"] = "CRITICAL HIT!",
                ["new_record"] = "New record!",
                ["level_up"] = "Level up!",
                ["weapon_broken"] = "Weapon broken!",
                ["low_ammo"] = "Low ammo!",
            };

            // Francés (ejemplo de extensión)
            _translations[Language.French] = new Dictionary<string, string>
            {
                ["game_title"] = "Arena Brawler",
                ["play"] = "JOUER",
                ["settings"] = "PARAMÈTRES",
                ["quit"] = "QUITTER",
                ["back"] = "RETOUR",
                ["save"] = "SAUVEGARDER",
                ["wave"] = "Vague",
                ["victory"] = "VICTOIRE!",
                ["defeat"] = "DÉFAITE!",
                // ... más traducciones
            };

            // Alemán
            _translations[Language.German] = new Dictionary<string, string>
            {
                ["game_title"] = "Arena Brawler",
                ["play"] = "SPIELEN",
                ["settings"] = "EINSTELLUNGEN",
                ["quit"] = "BEENDEN",
                ["wave"] = "Welle",
                ["victory"] = "SIEG!",
            };

            // Italiano
            _translations[Language.Italian] = new Dictionary<string, string>
            {
                ["game_title"] = "Arena Brawler",
                ["play"] = "GIOCA",
                ["settings"] = "IMPOSTAZIONI",
                ["wave"] = "Onda",
                ["victory"] = "VITTORIA!",
            };

            // Portugués
            _translations[Language.Portuguese] = new Dictionary<string, string>
            {
                ["game_title"] = "Arena Brawler",
                ["play"] = "JOGAR",
                ["settings"] = "CONFIGURAÇÕES",
                ["wave"] = "Onda",
                ["victory"] = "VITÓRIA!",
            };
        }

        /// <summary>
        /// Obtiene una traducción por clave
        /// </summary>
        public string Get(string key)
        {
            if (_translations.ContainsKey(currentLanguage) && 
                _translations[currentLanguage].ContainsKey(key))
            {
                return _translations[currentLanguage][key];
            }

            // Fallback a español
            if (_translations[Language.Spanish].ContainsKey(key))
            {
                return _translations[Language.Spanish][key];
            }

            // Si no existe, retornar clave
            if (showMissingKeys)
            {
                Debug.LogWarning($"[Localization] Missing key: {key} for language: {currentLanguage}");
            }
            return $"[{key}]";
        }

        /// <summary>
        /// Cambia el idioma actual
        /// </summary>
        public void SetLanguage(Language language)
        {
            if (language == currentLanguage) return;
            
            currentLanguage = language;
            PlayerPrefs.SetString("SelectedLanguage", language.ToString());
            PlayerPrefs.Save();
            
            OnLanguageChanged?.Invoke(language);
            
#if DEBUG
            Debug.Log($"[Localization] Language changed to: {language}");
#endif
        }

        /// <summary>
        /// Obtiene el nombre del idioma en su propio idioma
        /// </summary>
        public string GetLanguageName(Language language)
        {
            string key = language.ToString().ToLower();
            if (_translations[currentLanguage].ContainsKey(key))
            {
                return _translations[currentLanguage][key];
            }
            return language.ToString();
        }

        /// <summary>
        /// Lista de idiomas disponibles
        /// </summary>
        public Language[] GetAvailableLanguages()
        {
            return new[] 
            { 
                Language.Spanish, 
                Language.English, 
                Language.French, 
                Language.German,
                Language.Italian,
                Language.Portuguese
            };
        }
    }

    public enum Language
    {
        Spanish,
        English,
        French,
        German,
        Italian,
        Portuguese
    }

    /// <summary>
    /// Componente para UI localizada - se actualiza automáticamente al cambiar idioma
    /// </summary>
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string localizationKey;
        [SerializeField] private TMPro.TextMeshProUGUI textComponent;

        private void Awake()
        {
            if (textComponent == null)
                textComponent = GetComponent<TMPro.TextMeshProUGUI>();
            
            LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        }

        private void Start()
        {
            UpdateText();
        }

        private void OnDestroy()
        {
            LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(Language newLanguage)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            if (textComponent == null || LocalizationManager.Instance == null) return;
            
            textComponent.text = LocalizationManager.Instance.Get(localizationKey);
        }

        public void SetKey(string key)
        {
            localizationKey = key;
            UpdateText();
        }
    }
}

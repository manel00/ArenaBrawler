using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text;

namespace ArenaEnhanced
{
    /// <summary>
    /// HUD principal de la arena - Construye y actualiza toda la UI de juego en runtime.
    /// Auto-construye todos los elementos si no existen (self-healing).
    /// </summary>
    public class ArenaHUD : MonoBehaviour
    {
        // ── Runtime refs (built by SelfHeal) ─────────────────────────────────
        private Image      _healthBar;
        private Image      _staminaBar;
        private TextMeshProUGUI _waveText;
        private TextMeshProUGUI _pointsText;
        private TextMeshProUGUI _waveAnnounceText;
        private GameObject _gameOverPanel;
        private TextMeshProUGUI _gameOverText;

        // ── Serialized optional overrides ────────────────────────────────────
        [Header("Points")]
        [Header("Ability Bar")]
        [SerializeField] private AbilitySlot[] abilitySlots = new AbilitySlot[7];
        [Header("Dash cooldown overlay")]
        [SerializeField] private Image dashCooldownOverlay;
        [Header("Player dependency")]
        [SerializeField] private PlayerController playerController;

        private int   _currentPoints;
        private int   _currentLevel;
        private float _lastStamina = -1f;
        private readonly StringBuilder _sb = new StringBuilder(32);

        private static readonly int[] LevelThresholds = { 100, 200, 300, 400, 500 };

        public static ArenaHUD Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Ensure this GameObject has a Canvas so Unity can render UGUI
            EnsureCanvas();
            BuildAllPanels();
        }

        private void EnsureCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Panel builders
        // ─────────────────────────────────────────────────────────────────────
        private void BuildAllPanels()
        {
            BuildPlayerHUD();
            BuildWaveHUD();
            BuildAnnouncePanel();
            BuildGameOverPanel();
            BuildAbilityBar();
        }

        // ── Health + Stamina (top-left) ───────────────────────────────────────
        private void BuildPlayerHUD()
        {
            var panel = MakePanel("PlayerHUD_Panel",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(30, -30), new Vector2(380, 80));

            _healthBar  = MakeBar(panel, "HealthBar",  new Vector2(0, -2),  new Vector2(360, 28), new Color(0.85f, 0.15f, 0.15f));
            _staminaBar = MakeBar(panel, "StaminaBar", new Vector2(0, -40), new Vector2(360, 16), new Color(0.2f,  0.6f, 1f));

            // Labels
            var hl = MakeLabel(panel, "HP_Label", new Vector2(0, -2), new Vector2(60, 28), 18, "HP", Color.white, TextAlignmentOptions.Left);
            var sl = MakeLabel(panel, "ST_Label", new Vector2(0, -40), new Vector2(80, 16), 14, "STA", Color.cyan,  TextAlignmentOptions.Left);
        }

        // ── Wave / Points (top-right) ─────────────────────────────────────────
        private void BuildWaveHUD()
        {
            var panel = MakePanel("WaveHUD_Panel",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-30, -30), new Vector2(340, 80));

            _waveText   = MakeLabel(panel, "WaveText",   new Vector2(0, -2),  new Vector2(340, 36), 32, "Ola 1", Color.white,  TextAlignmentOptions.Right);
            _pointsText = MakeLabel(panel, "PointsText", new Vector2(0, -42), new Vector2(340, 28), 22, "0 pts | Lv.0", Color.yellow, TextAlignmentOptions.Right);
        }

        // ── Wave announcement (center) ────────────────────────────────────────
        private void BuildAnnouncePanel()
        {
            var panel = MakePanel("WaveAnnounceHUD_Panel",
                new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(700, 120));

            _waveAnnounceText = MakeLabel(panel, "AnnounceText", Vector2.zero, new Vector2(700, 120),
                72, "", Color.red, TextAlignmentOptions.Center);
            _waveAnnounceText.fontStyle = FontStyles.Bold;
            panel.gameObject.SetActive(false); // hidden until needed
        }

        // ── Game Over overlay (center) ────────────────────────────────────────
        private void BuildGameOverPanel()
        {
            _gameOverPanel = MakePanel("GameOverHUD_Panel",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero).gameObject;

            // Dark semi-transparent background
            var bg = _gameOverPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.72f);

            _gameOverText = MakeLabel(_gameOverPanel.GetComponent<RectTransform>(),
                "GameOverText", Vector2.zero, new Vector2(900, 300),
                56, "", Color.white, TextAlignmentOptions.Center);
            _gameOverText.fontStyle = FontStyles.Bold;

            _gameOverPanel.SetActive(false);
        }

        // ── Ability bar (bottom-center) ───────────────────────────────────────
        private void BuildAbilityBar()
        {
            var panel = MakePanel("SkillHUD_Panel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 20), new Vector2(600, 80));

            string[] keys   = { "1","2","3","4","5","Q","E" };
            string[] names  = { "Atk","Def","Dash","Skill","Ult","Swap","Block" };
            for (int i = 0; i < 7; i++)
            {
                float x = (i - 3) * 82f;
                var slot = MakeSlot(panel, $"Slot_{i}", new Vector2(x, 0));
                if (abilitySlots[i] == null) abilitySlots[i] = new AbilitySlot();
                abilitySlots[i].Rebuild(slot.gameObject, keys[i], names[i]);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────
        private void Start()
        {
            if (playerController == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) playerController = go.GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            UpdateHealth();
            UpdateStamina();
            UpdateGameOver();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Update methods
        // ─────────────────────────────────────────────────────────────────────
        private void UpdateHealth()
        {
            if (_healthBar == null || playerController == null) return;
            var combatant = playerController.GetComponent<ArenaCombatant>();
            if (combatant != null && combatant.maxHp > 0)
                _healthBar.fillAmount = combatant.hp / combatant.maxHp;
        }

        private void UpdateStamina()
        {
            if (_staminaBar == null || playerController == null) return;
            float stamina = playerController.GetStaminaPercentage();
            if (!Mathf.Approximately(stamina, _lastStamina))
            {
                _staminaBar.fillAmount = stamina;
                _lastStamina = stamina;
            }
        }

        private void UpdateGameOver()
        {
            if (_gameOverPanel == null) return;
            var gm = ArenaGameManager.Instance;
            if (gm == null) return;

            bool shouldShow = gm.ended;
            if (_gameOverPanel.activeSelf != shouldShow)
            {
                _gameOverPanel.SetActive(shouldShow);
                if (shouldShow && _gameOverText != null)
                    _gameOverText.text = gm.endText;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────
        public void Initialize(ArenaCombatant player)
        {
            if (player != null)
                playerController = player.GetComponent<PlayerController>();
        }

        public void AddPoints(int points)
        {
            _currentPoints += points;
            CheckLevelUp();
            RefreshPointsDisplay();
        }

        public void ResetPoints()
        {
            _currentPoints = 0;
            _currentLevel  = 0;
            RefreshPointsDisplay();
            ApplyUpgrades();
        }

        public void ShowMatchSetup(System.Action<int> onBotCountSelected)
        {
            onBotCountSelected?.Invoke(3);
        }

        public void ShowWaveAnnouncement(int currentWave, int totalWaves)
        {
            if (_waveText != null)
                _waveText.text = $"Ola {currentWave}/{totalWaves}";

            if (_waveAnnounceText != null)
                StartCoroutine(ShowAnnouncement($"OLA {currentWave}"));
        }

        public void UpdateWeaponName(string weaponName) { /* optional */ }
        public void SetPlayerController(PlayerController c) => playerController = c;
        public int GetCurrentPoints() => _currentPoints;
        public int GetCurrentLevel()  => _currentLevel;

        // ─────────────────────────────────────────────────────────────────────
        // Internal helpers
        // ─────────────────────────────────────────────────────────────────────
        private IEnumerator ShowAnnouncement(string text)
        {
            if (_waveAnnounceText == null) yield break;
            _waveAnnounceText.transform.parent.gameObject.SetActive(true);
            _waveAnnounceText.text = text;
            yield return new WaitForSeconds(2.5f);
            _waveAnnounceText.transform.parent.gameObject.SetActive(false);
        }

        private void CheckLevelUp()
        {
            int newLevel = 0;
            for (int i = 0; i < LevelThresholds.Length; i++)
                if (_currentPoints >= LevelThresholds[i]) newLevel = i + 1;
            if (newLevel > _currentLevel) { _currentLevel = newLevel; ApplyUpgrades(); }
        }

        private void ApplyUpgrades()
        {
            if (playerController == null) return;
            float dmgMult   = 1f + _currentLevel * 0.05f;
            float speedMult = 1f + _currentLevel * 0.03f;
            var combatant = playerController.GetComponent<ArenaCombatant>();
            if (combatant != null) combatant.damageMultiplier = dmgMult;
            playerController.moveSpeed = 12.5f * speedMult;
        }

        private void RefreshPointsDisplay()
        {
            if (_pointsText == null) return;
            _sb.Clear();
            _sb.Append(_currentPoints).Append(" pts | Lv.").Append(_currentLevel);
            _pointsText.text = _sb.ToString();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // UI factory helpers
        // ─────────────────────────────────────────────────────────────────────
        private RectTransform MakePanel(string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin       = anchorMin;
            rt.anchorMax       = anchorMax;
            rt.pivot           = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta       = sizeDelta;
            return rt;
        }

        private Image MakeBar(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size, Color fill)
        {
            // Background
            var bgGo = new GameObject(name + "_BG");
            bgGo.transform.SetParent(parent, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0,1); bgRT.anchorMax = new Vector2(0,1); bgRT.pivot = new Vector2(0,1);
            bgRT.anchoredPosition = anchoredPos; bgRT.sizeDelta = size;
            bgGo.AddComponent<Image>().color = new Color(0,0,0,0.5f);

            // Fill
            var fillGo = new GameObject(name + "_Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRT = fillGo.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
            var img = fillGo.AddComponent<Image>();
            img.color = fill;
            img.type  = Image.Type.Filled;
            img.fillMethod  = Image.FillMethod.Horizontal;
            img.fillAmount  = 1f;
            return img;
        }

        private TextMeshProUGUI MakeLabel(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size,
            float fontSize, string defaultText, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0,1);
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.alignment = align;
            tmp.text      = defaultText;
            return tmp;
        }

        private RectTransform MakeSlot(RectTransform parent, string name, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f,0); rt.anchorMax = new Vector2(0.5f,0); rt.pivot = new Vector2(0.5f,0);
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = new Vector2(74, 74);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.08f, 0.88f);
            return rt;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    [System.Serializable]
    public class AbilitySlot
    {
        private string _abilityName;
        private string _keyBinding;
        private Image              _cooldownOverlay;
        private TextMeshProUGUI    _nameText;
        private TextMeshProUGUI    _keyText;
        private float _cdRemaining;
        private float _cdMax;

        public void Rebuild(GameObject root, string key, string abilityName)
        {
            _keyBinding   = key;
            _abilityName  = abilityName;

            var parentRT = root.GetComponent<RectTransform>();

            // Key badge
            var kGo = new GameObject("Key");
            kGo.transform.SetParent(root.transform, false);
            var kRT = kGo.AddComponent<RectTransform>();
            kRT.anchorMin = new Vector2(0,1); kRT.anchorMax = new Vector2(0,1); kRT.pivot = new Vector2(0,1);
            kRT.anchoredPosition = new Vector2(4,-4); kRT.sizeDelta = new Vector2(22,18);
            _keyText = kGo.AddComponent<TextMeshProUGUI>();
            _keyText.fontSize = 13; _keyText.color = Color.white; _keyText.text = key;
            _keyText.alignment = TextAlignmentOptions.Center;

            // Ability name
            var nGo = new GameObject("Name");
            nGo.transform.SetParent(root.transform, false);
            var nRT = nGo.AddComponent<RectTransform>();
            nRT.anchorMin = new Vector2(0,0); nRT.anchorMax = new Vector2(1,0); nRT.pivot = new Vector2(0.5f,0);
            nRT.anchoredPosition = new Vector2(0,4); nRT.sizeDelta = new Vector2(0,16);
            _nameText = nGo.AddComponent<TextMeshProUGUI>();
            _nameText.fontSize = 11; _nameText.color = new Color(0.8f,0.8f,0.8f); _nameText.text = abilityName;
            _nameText.alignment = TextAlignmentOptions.Center;

            // Cooldown overlay
            var cGo = new GameObject("CDOverlay");
            cGo.transform.SetParent(root.transform, false);
            var cRT = cGo.AddComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one;
            cRT.offsetMin = Vector2.zero; cRT.offsetMax = Vector2.zero;
            _cooldownOverlay = cGo.AddComponent<Image>();
            _cooldownOverlay.color = new Color(0,0,0,0.7f);
            _cooldownOverlay.type  = Image.Type.Filled;
            _cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            _cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
            _cooldownOverlay.gameObject.SetActive(false);
        }

        public void SetCooldown(float remaining, float max)
        {
            _cdRemaining = remaining; _cdMax = max;
            if (_cooldownOverlay == null) return;
            if (remaining > 0 && max > 0)
            {
                _cooldownOverlay.fillAmount = remaining / max;
                _cooldownOverlay.gameObject.SetActive(true);
            }
            else _cooldownOverlay.gameObject.SetActive(false);
        }

        public void UpdateSlot() { /* called externally if needed */ }
    }
}
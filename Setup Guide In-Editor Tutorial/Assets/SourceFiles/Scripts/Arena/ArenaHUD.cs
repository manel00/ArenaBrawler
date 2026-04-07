using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

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
        private TextMeshProUGUI _waveText;
        private TextMeshProUGUI _pointsText;
        private TextMeshProUGUI _waveAnnounceText;
        private GameObject _gameOverPanel;
        private TextMeshProUGUI _gameOverText;
        private TextMeshProUGUI _weaponNameText;

        // ── Action Buttons ────────────────────────────────────────────────────
        private Button _rescueButton;
        private Button _exitButton;
        private GameObject _confirmDialog;

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
        private readonly StringBuilder _sb = new StringBuilder(32);

        private static readonly int[] LevelThresholds = { 100, 200, 300, 400, 500 };

        public static ArenaHUD Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Ensure EventSystem exists for UI interaction
            EnsureEventSystem();

            // Ensure this GameObject has a Canvas so Unity can render UGUI
            EnsureCanvas();
            BuildAllPanels();
        }

        private void EnsureEventSystem()
        {
            // Check if EventSystem already exists in scene
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                // Create EventSystem GameObject
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(esGo);
            }
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

            // Labels
            var hl = MakeLabel(panel, "HP_Label", new Vector2(0, -2), new Vector2(60, 28), 18, "HP", Color.white, TextAlignmentOptions.Left);
        }

        // ── Wave / Points (top-right) ─────────────────────────────────────────
        private void BuildWaveHUD()
        {
            var panel = MakePanel("WaveHUD_Panel",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-30, -30), new Vector2(440, 80));

            // Action buttons on the left side of the panel
            BuildActionButtons(panel);

            _waveText   = MakeLabel(panel, "WaveText",   new Vector2(-100, -2),  new Vector2(340, 36), 32, "Ola 1", Color.white,  TextAlignmentOptions.Right);
            _pointsText = MakeLabel(panel, "PointsText", new Vector2(-100, -42), new Vector2(340, 28), 22, "0 pts | Lv.0", Color.yellow, TextAlignmentOptions.Right);
        }

        // ── Action Buttons (left side of wave HUD) ────────────────────────────
        private void BuildActionButtons(RectTransform parent)
        {
            var buttonPanel = MakePanel("ActionButtons_Panel",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, -2), new Vector2(90, 40));
            buttonPanel.SetParent(parent, false);

            // Rescue button (left) - Orange color for rescue/help
            _rescueButton = MakeIconButton(buttonPanel, "RescueButton", new Vector2(0, 0), new Vector2(40, 40),
                new Color(1f, 0.6f, 0f), "R", OnRescueClicked);

            // Exit button (right) - Red color for exit
            _exitButton = MakeIconButton(buttonPanel, "ExitButton", new Vector2(46, 0), new Vector2(40, 40),
                new Color(0.9f, 0.2f, 0.2f), "X", OnExitClicked);
        }

        private Button MakeIconButton(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size,
            Color color, string iconText, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = 5; // UI layer
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(color.r, color.g, color.b, 0.85f);
            img.sprite = GetWhiteSprite();

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;

            btn.onClick.AddListener(onClick);

            // Add EventTrigger to unlock cursor on hover
            var trigger = go.AddComponent<EventTrigger>();
            var pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            pointerEnter.callback.AddListener((e) => { UnlockCursorForUI(true); });
            trigger.triggers.Add(pointerEnter);

            var pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            pointerExit.callback.AddListener((e) => { UnlockCursorForUI(false); });
            trigger.triggers.Add(pointerExit);

            // Icon text
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            iconGo.layer = 5;
            var iconRT = iconGo.AddComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            var iconTmp = iconGo.AddComponent<TextMeshProUGUI>();
            iconTmp.fontSize = 24;
            iconTmp.color = Color.white;
            iconTmp.alignment = TextAlignmentOptions.Center;
            iconTmp.fontStyle = FontStyles.Bold;
            iconTmp.text = iconText;

            return btn;
        }

        private void UnlockCursorForUI(bool unlock)
        {
            if (unlock)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // Check if mouse is still over any UI element before locking
                if (!IsPointerOverUIElement())
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        private bool IsPointerOverUIElement()
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }

        private void OnRescueClicked()
        {
            if (playerController == null)
            {
                playerController = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
                if (playerController == null) return;
            }

            Vector3 safePosition = FindSafePosition();
            if (safePosition != Vector3.zero)
            {
                playerController.transform.position = safePosition;
                // Optional: Add visual feedback
                StartCoroutine(ShowRescueFeedback());
            }
        }

        private Vector3 FindSafePosition()
        {
            const int maxAttempts = 30;
            const float minEnemyDistance = 12f;
            const float arenaRadius = 35f;

            // Get all enemies from HordeWaveManager
            var enemies = new List<Transform>();
            if (HordeWaveManager.Instance != null)
            {
                // Find all enemies by tag
                var enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
                foreach (var enemy in enemyObjects)
                {
                    if (enemy != null && enemy.activeInHierarchy)
                        enemies.Add(enemy.transform);
                }
            }

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 randomPoint = Random.insideUnitCircle * arenaRadius;
                Vector3 candidate = new Vector3(randomPoint.x, 50f, randomPoint.y);

                // Raycast to ground
                if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
                {
                    Vector3 groundPos = hit.point + Vector3.up * 1.5f;

                    // Check distance to all enemies
                    bool farFromEnemies = true;
                    foreach (var enemy in enemies)
                    {
                        if (enemy == null) continue;
                        float dist = Vector3.Distance(new Vector3(groundPos.x, 0, groundPos.z), 
                                                        new Vector3(enemy.position.x, 0, enemy.position.z));
                        if (dist < minEnemyDistance)
                        {
                            farFromEnemies = false;
                            break;
                        }
                    }

                    // Check for obstacles (capsule cast at player height)
                    bool noObstacles = !Physics.CheckCapsule(groundPos + Vector3.up * 0.5f, 
                                                              groundPos + Vector3.up * 1.5f, 
                                                              0.4f, ~0, QueryTriggerInteraction.Ignore);

                    if (farFromEnemies && noObstacles)
                        return groundPos;
                }
            }

            // Fallback: return center arena at safe height
            return new Vector3(0, 1.5f, 0);
        }

        private IEnumerator ShowRescueFeedback()
        {
            // Brief screen flash effect
            var flashGo = new GameObject("RescueFlash");
            flashGo.transform.SetParent(transform, false);
            var flashRT = flashGo.AddComponent<RectTransform>();
            flashRT.anchorMin = Vector2.zero;
            flashRT.anchorMax = Vector2.one;
            flashRT.offsetMin = Vector2.zero;
            flashRT.offsetMax = Vector2.zero;
            var flashImg = flashGo.AddComponent<Image>();
            flashImg.color = new Color(1f, 0.8f, 0.2f, 0.3f);

            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 0.3f * (1f - elapsed / duration);
                flashImg.color = new Color(1f, 0.8f, 0.2f, alpha);
                yield return null;
            }

            Destroy(flashGo);
        }

        private void OnExitClicked()
        {
            if (_confirmDialog == null)
                BuildConfirmationDialog();
            else
                _confirmDialog.SetActive(true);

            Time.timeScale = 0f;
        }

        private void BuildConfirmationDialog()
        {
            _confirmDialog = new GameObject("ConfirmDialog");
            _confirmDialog.transform.SetParent(transform, false);
            _confirmDialog.layer = 5;

            // Semi-transparent overlay
            var overlay = MakePanel("DialogOverlay",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            overlay.SetParent(_confirmDialog.transform, false);
            var overlayImg = overlay.gameObject.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.7f);

            // Dialog box
            var dialogBox = MakePanel("DialogBox",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400, 200));
            dialogBox.SetParent(_confirmDialog.transform, false);
            var boxImg = dialogBox.gameObject.AddComponent<Image>();
            boxImg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            var boxOutline = dialogBox.gameObject.AddComponent<Outline>();
            boxOutline.effectColor = new Color(0.3f, 0.3f, 0.4f);
            boxOutline.effectDistance = new Vector2(2, -2);

            // Title text
            var title = MakeLabel(dialogBox, "ConfirmTitle", new Vector2(0, -20), new Vector2(400, 40),
                28, "¿SALIR DEL JUEGO?", new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;

            // Message text
            var message = MakeLabel(dialogBox, "ConfirmMessage", new Vector2(0, -70), new Vector2(360, 60),
                18, "Se perderá el progreso actual.\n¿Estás seguro?", Color.white, TextAlignmentOptions.Center);

            // Yes button
            var yesBtn = MakeDialogButton(dialogBox, "YesButton", new Vector2(-100, -140), new Vector2(140, 45),
                new Color(0.9f, 0.2f, 0.2f), "SALIR", () =>
                {
                    Time.timeScale = 1f;
                    SceneManager.LoadScene("GetStarted_Scene");
                });

            // No button
            var noBtn = MakeDialogButton(dialogBox, "NoButton", new Vector2(100, -140), new Vector2(140, 45),
                new Color(0.2f, 0.6f, 0.2f), "CONTINUAR", () =>
                {
                    Time.timeScale = 1f;
                    _confirmDialog.SetActive(false);
                });
        }

        private Button MakeDialogButton(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size,
            Color color, string text, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.sprite = GetWhiteSprite();

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;

            btn.onClick.AddListener(onClick);

            // Button text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRT = textGo.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            textTmp.fontSize = 18;
            textTmp.color = Color.white;
            textTmp.alignment = TextAlignmentOptions.Center;
            textTmp.fontStyle = FontStyles.Bold;
            textTmp.text = text;

            return btn;
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

            // Centrar el texto en la pantalla
            var textRect = _gameOverText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;

            _gameOverPanel.SetActive(false);
        }

        // ── Ability bar (bottom-center) ───────────────────────────────────────
        private void BuildAbilityBar()
        {
            var panel = MakePanel("SkillHUD_Panel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 20), new Vector2(600, 100));

            // Weapon name label above ability slots
            _weaponNameText = MakeLabel(panel, "WeaponName", new Vector2(0, 80), new Vector2(600, 20),
                14, "", new Color(1f, 0.82f, 0.35f), TextAlignmentOptions.Center);
            _weaponNameText.fontStyle = FontStyles.Bold;

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
            // Suscribirse al evento de salud del jugador
            SubscribeToHealthEvents();
        }
        
        private void SubscribeToHealthEvents()
        {
            // Buscar jugador si no lo tenemos
            if (playerController == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) 
                    playerController = go.GetComponent<PlayerController>();
            }
            
            // Suscribirse al evento de salud
            if (playerController != null)
            {
                var combatant = playerController.GetComponent<ArenaCombatant>();
                if (combatant != null)
                {
                    combatant.OnHealthChanged -= OnPlayerHealthChanged; // Evitar duplicados
                    combatant.OnHealthChanged += OnPlayerHealthChanged;
                }
            }
        }

        private void OnPlayerHealthChanged(float currentHp, float maxHp)
        {
            if (_healthBar != null && maxHp > 0)
            {
                float fillAmount = currentHp / maxHp;
                _healthBar.fillAmount = fillAmount;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            // Desuscribirse del evento
            if (playerController != null)
            {
                var combatant = playerController.GetComponent<ArenaCombatant>();
                if (combatant != null)
                    combatant.OnHealthChanged -= OnPlayerHealthChanged;
            }
        }

        private void Update()
        {
            UpdateHealth();
            UpdateGameOver();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Update methods
        // ─────────────────────────────────────────────────────────────────────
        private void UpdateHealth()
        {
            if (_healthBar == null) return;
            if (playerController == null) return;
            var combatant = playerController.GetComponent<ArenaCombatant>();
            if (combatant != null && combatant.maxHp > 0)
                _healthBar.fillAmount = combatant.hp / combatant.maxHp;
        }

        private void UpdateStamina()
        {
            // Stamina bar removed - not used
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
            if (player == null) return;
            
            playerController = player.GetComponent<PlayerController>();
            SubscribeToHealthEvents();
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

        public void UpdateWeaponName(string weaponName) 
        { 
            if (_weaponNameText != null)
            {
                _weaponNameText.text = string.IsNullOrEmpty(weaponName) ? "" : $"[{weaponName}]";
            }
        }
        public void SetPlayerController(PlayerController c) 
        { 
            if (c == null) return;
            
            // Desuscribir del anterior
            if (playerController != null)
            {
                var oldCombatant = playerController.GetComponent<ArenaCombatant>();
                if (oldCombatant != null)
                    oldCombatant.OnHealthChanged -= OnPlayerHealthChanged;
            }
            
            playerController = c;
            SubscribeToHealthEvents();
        }
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
            img.sprite = GetWhiteSprite(); // IMPORTANTE: Necesita sprite para Fill
            img.color = fill;
            img.type  = Image.Type.Filled;
            img.fillMethod  = Image.FillMethod.Horizontal;
            img.fillOrigin  = (int)Image.OriginHorizontal.Left;
            img.fillAmount  = 1f;
            return img;
        }

        private static Sprite _whiteSprite;
        private Sprite GetWhiteSprite()
        {
            if (_whiteSprite == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var white = new Color[16];
                for (int i = 0; i < 16; i++) white[i] = Color.white;
                tex.SetPixels(white);
                tex.Apply();
                _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            }
            return _whiteSprite;
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
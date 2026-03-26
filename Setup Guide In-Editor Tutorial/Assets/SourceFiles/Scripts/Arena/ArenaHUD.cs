using UnityEngine;
using UnityEngine.UI;

namespace ArenaEnhanced
{
    public class ArenaHUD : MonoBehaviour
    {
        public static ArenaHUD Instance { get; private set; }

        private ArenaCombatant player;
        private ArenaCombatant target; 

        [Header("Prefab Links (Optional - Falls back to procedural if null)")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Text playerHpText;
        [SerializeField] private Image playerHpFill;
        [SerializeField] private GameObject targetFrame;
        [SerializeField] private Text targetNameText;
        [SerializeField] private Image targetHpFill;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverText;
        [SerializeField] private Text waveCounterText;
        [SerializeField] private GameObject waveAnnouncePanel;
        [SerializeField] private Text waveAnnounceText;
        [SerializeField] private GameObject matchSetupPanel;
        [SerializeField] private Slider botSlider;
        [SerializeField] private Text botSliderLabel;
        [SerializeField] private Button startButton;

        private float _visualHp;
        private float _visualTargetHp;
        private float _waveAnnounceHideTime;
        private ArenaGameManager _gm;

        private void Awake() { Instance = this; }

        public void Initialize(ArenaCombatant mainPlayer)
        {
            player = mainPlayer;
            _visualHp = player != null ? player.hp : 0;
            
            if (canvas == null)
            {
                Debug.Log("[ArenaHUD] No component links found. Generating Procedural UI...");
                CreateCanvasUI();
            }
        }

        public void ShowMatchSetup(System.Action<int> onStart)
        {
            if (matchSetupPanel != null)
            {
                matchSetupPanel.SetActive(true);
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() => {
                    int bots = (int)botSlider.value;
                    PlayerPrefs.SetInt("BotCount", bots);
                    PlayerPrefs.Save();
                    matchSetupPanel.SetActive(false);
                    onStart?.Invoke(bots);
                });
            }
        }

        private void Update()
        {
            if (canvas == null) return;

            // --- PLAYER FRAME UPDATE ---
            if (player != null && playerHpFill != null)
            {
                _visualHp = Mathf.Lerp(_visualHp, player.hp, Time.deltaTime * 5f);
                playerHpFill.fillAmount = Mathf.Clamp01(_visualHp / player.maxHp);
                if (playerHpText != null) 
                    playerHpText.text = $"HP: {Mathf.Ceil(player.hp)} / {Mathf.Ceil(player.maxHp)}";
                
                float ratio = player.hp / player.maxHp;
                playerHpFill.color = Color.Lerp(new Color(0.8f, 0.2f, 0.2f), new Color(0.2f, 0.9f, 0.3f), ratio);
            }

            // --- TARGET FRAME UPDATE ---
            var pc = player != null ? player.GetComponent<PlayerController>() : null;
            var currentTarget = pc != null ? pc.CurrentTarget : null;

            if (currentTarget != null && currentTarget.IsAlive)
            {
                if (targetFrame != null)
                {
                    if (!targetFrame.activeSelf)
                    {
                        targetFrame.SetActive(true);
                        _visualTargetHp = currentTarget.hp;
                    }

                    _visualTargetHp = Mathf.Lerp(_visualTargetHp, currentTarget.hp, Time.deltaTime * 5f);
                    if (targetHpFill != null) targetHpFill.fillAmount = Mathf.Clamp01(_visualTargetHp / currentTarget.maxHp);
                    if (targetNameText != null) targetNameText.text = currentTarget.displayName.ToUpper();

                    float tRatio = currentTarget.hp / currentTarget.maxHp;
                    if (targetHpFill != null) targetHpFill.color = Color.Lerp(new Color(0.8f, 0.2f, 0.2f), new Color(0.1f, 0.7f, 0.9f), tRatio);
                }
            }
            else
            {
                if (targetFrame != null && targetFrame.activeSelf) targetFrame.SetActive(false);
            }

            // --- GAME OVER UPDATE ---
            if (_gm == null) _gm = Object.FindAnyObjectByType<ArenaGameManager>();
            
            if (_gm != null && _gm.ended)
            {
                if (gameOverPanel != null && !gameOverPanel.activeSelf)
                {
                    gameOverPanel.SetActive(true);
                    gameOverPanel.transform.SetAsLastSibling(); 
                    if (gameOverText != null) gameOverText.text = _gm.endText + "\n<size=24>Press [R] to Restart</size>";
                }
            }
            else
            {
                if (gameOverPanel != null && gameOverPanel.activeSelf) gameOverPanel.SetActive(false);
            }

            // --- WAVE ANNOUNCEMENT ---
            if (waveAnnouncePanel != null)
                waveAnnouncePanel.SetActive(Time.time < _waveAnnounceHideTime);

            // --- MATCH SETUP SLIDER UPDATE ---
            if (matchSetupPanel != null && matchSetupPanel.activeSelf && botSliderLabel != null && botSlider != null)
            {
                botSliderLabel.text = $"ALLIED BOTS: {(int)botSlider.value}";
            }

            // --- WAVE COUNTER ---
            if (waveCounterText != null && HordeWaveManager.Instance != null)
            {
                int rem = HordeWaveManager.Instance.EnemiesRemaining();
                waveCounterText.text = $"WAVE {HordeWaveManager.Instance.CurrentWave}/{HordeWaveManager.Instance.TotalWaves}  |  Enemies: {rem}";
            }
        }

        public void ShowWaveAnnouncement(int wave, int total)
        {
            if (waveAnnounceText != null) waveAnnounceText.text = $"WAVE {wave} / {total}";
            _waveAnnounceHideTime = Time.time + 3f;
            if (waveAnnouncePanel != null) waveAnnouncePanel.SetActive(true);
        }

        // --- PROCEDURAL UI GENERATION (CONSOLIDATED) ---
        private void CreateCanvasUI()
        {
            var cGo = new GameObject("ArenaHUD_Procedural");
            canvas = cGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            cGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cGo.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Build Player Frame (Resized to 25%: 300x75 -> 75x18.75, pos 30,-30 -> 7.5,-7.5)
            var pRoot = CreateBox(canvas.transform, "PlayerHUD", new Vector2(7.5f, -7.5f), new Vector2(75f, 18.75f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            pRoot.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            var hpBg = CreateBox(pRoot.transform, "HP_BG", new Vector2(2.5f, -8.75f), new Vector2(70f, 7.5f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            hpBg.GetComponent<Image>().color = new Color(0.2f, 0, 0, 0.8f);
            playerHpFill = CreateBar(hpBg.transform, "HP_Fill", new Color(0.2f, 0.9f, 0.3f));
            playerHpText = CreateText(hpBg.transform, "HP_Label", "", font, Color.white, 8, TextAnchor.MiddleCenter);
            CreateText(pRoot.transform, "Name_Label", player.displayName.ToUpper(), font, Color.yellow, 10, TextAnchor.MiddleLeft).rectTransform.anchoredPosition = new Vector2(2.5f, -1.25f);

            // Build Skills root (Resized to 25%: 300x80 -> 75x20, pos 0,40 -> 0,10)
            var sRoot = CreateBox(canvas.transform, "SkillHUD", new Vector2(0, 10), new Vector2(75f, 20f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            sRoot.GetComponent<Image>().color = new Color(0, 0, 0, 0.4f);

            // Build Target Frame (Resized to 25%: 400x60 -> 100x15, pos 0,-50 -> 0,-12.5)
            targetFrame = CreateBox(canvas.transform, "TargetHUD", new Vector2(0, -12.5f), new Vector2(100f, 15f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            targetFrame.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            var tHpBg = CreateBox(targetFrame.transform, "TargetHP_BG", new Vector2(0, -8.75f), new Vector2(95f, 5f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            tHpBg.GetComponent<Image>().color = new Color(0.2f, 0, 0, 0.8f);
            targetHpFill = CreateBar(tHpBg.transform, "TargetHP_Fill", new Color(0.1f, 0.7f, 0.9f));
            targetNameText = CreateText(targetFrame.transform, "TargetName", "TARGET", font, Color.white, 10, TextAnchor.MiddleCenter);
            targetFrame.SetActive(false);

            // Build Wave + Status HUD (Resized to 25%: 250x50 -> 62.5x12.5, pos -30,-30 -> -7.5,-7.5)
            var wRoot = CreateBox(canvas.transform, "WaveHUD", new Vector2(-7.5f, -7.5f), new Vector2(62.5f, 12.5f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            wRoot.GetComponent<Image>().color = new Color(0,0,0,0.6f);
            waveCounterText = CreateText(wRoot.transform, "WaveCounter", "WAVE 1/3", font, new Color(1, 0.6f, 0), 10, TextAnchor.MiddleCenter);

            // Announcement (Resized to 25%: 500x90 -> 125x22.5, pos 0,80 -> 0,20)
            waveAnnouncePanel = CreateBox(canvas.transform, "WaveAnnounce", new Vector2(0, 20), new Vector2(125f, 22.5f), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f);
            waveAnnouncePanel.GetComponent<Image>().color = new Color(0.8f, 0.1f, 0.05f, 0.85f);
            waveAnnounceText = CreateText(waveAnnouncePanel.transform, "WaveAnnounceText", "WAVE START", font, Color.white, 14, TextAnchor.MiddleCenter);
            waveAnnouncePanel.SetActive(false);

            // Game Over
            gameOverPanel = CreateBox(canvas.transform, "GameOverPanel", Vector2.zero, new Vector2(400, 150), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f);
            gameOverPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.9f);
            gameOverText = CreateText(gameOverPanel.transform, "GameOverText", "GAME OVER", font, Color.red, 36, TextAnchor.MiddleCenter);
            gameOverPanel.SetActive(false);

            // Match Setup (Condensed)
            matchSetupPanel = CreateBox(canvas.transform, "MatchSetupPanel", Vector2.zero, new Vector2(450, 250), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f);
            matchSetupPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.95f);
            CreateText(matchSetupPanel.transform, "Title", "ARENA CONFIG", font, Color.yellow, 28, TextAnchor.MiddleCenter).rectTransform.anchoredPosition = new Vector2(0, 80);
            botSliderLabel = CreateText(matchSetupPanel.transform, "Label", "ALLIED BOTS: 3", font, Color.white, 20, TextAnchor.MiddleCenter);
            
            var sGo = new GameObject("Slider"); sGo.transform.SetParent(matchSetupPanel.transform, false);
            var rt = sGo.AddComponent<RectTransform>(); rt.anchoredPosition = new Vector2(0, -20); rt.sizeDelta = new Vector2(300, 20);
            botSlider = sGo.AddComponent<Slider>(); botSlider.minValue = 0; botSlider.maxValue = 10; botSlider.wholeNumbers = true;
            botSlider.value = PlayerPrefs.GetInt("BotCount", 3);
            
            var bGo = CreateBox(matchSetupPanel.transform, "StartBtn", new Vector2(0, -80), new Vector2(200, 50), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f);
            bGo.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);
            startButton = bGo.AddComponent<Button>();
            CreateText(bGo.transform, "BtnText", "FIGHT!", font, Color.white, 24, TextAnchor.MiddleCenter);
            matchSetupPanel.SetActive(false);
        }

        private GameObject CreateBox(Transform parent, string n, Vector2 p, Vector2 s, Vector2 min, Vector2 max, Vector2 piv)
        {
            var go = new GameObject(n); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>(); rt.anchorMin = min; rt.anchorMax = max; rt.pivot = piv;
            rt.anchoredPosition = p; rt.sizeDelta = s; go.AddComponent<Image>();
            return go;
        }

        private Text CreateText(Transform parent, string n, string txt, Font f, Color c, int s, TextAnchor a)
        {
            var go = new GameObject(n); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>(); t.text = txt; t.font = f; t.color = c; t.fontSize = s; t.alignment = a;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Image CreateBar(Transform parent, string n, Color c)
        {
            var go = new GameObject(n); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>(); img.color = c; img.type = Image.Type.Filled; img.fillMethod = Image.FillMethod.Horizontal;
            return img;
        }
    }
}

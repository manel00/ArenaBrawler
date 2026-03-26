using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ArenaEnhanced
{
    /// <summary>
    /// Manages the Welcome Screen UI with self-correcting layout, scaling, and HIERARCHY.
    /// Ensures 1080p-relative size, correct background, and premium visual components.
    /// </summary>
    public class WelcomeScreenManager : MonoBehaviour
    {
        [Header("UI References")]
        public InputField playerNameInput;
        public Text botCountLabel;
        public Dropdown botCountDropdown;
        public Button playSoloButton;
        public Button lanButton;
        public Text lanButtonLabel;
        public Text versionText;

        [Header("Settings")]
        public string arenaSceneName = "GetStarted_Scene";
        public int defaultBotCount = 3;
        public int maxBots = 10;

        private int _botCount;

        private void Awake()
        {
            SetupCanvas();
            SetupControlPanel();
            AutoLinkComponents();
            StyleUIElements();
            Debug.Log("[WelcomeScreenManager] Awake complete: UI Self-Healing and Styling applied.");
        }

        private void SetupCanvas()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = 10f;

                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;
                }
            }
        }

        private void SetupControlPanel()
        {
            GameObject panel = GameObject.Find("UI_ControlPanel");
            if (panel == null) return;

            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(400, 600);
            rt.anchoredPosition = Vector2.zero;

            var img = panel.GetComponent<Image>();
            if (img != null) img.color = new Color(0, 0, 0, 0.85f);

            // Position primary elements
            FixChild(panel, "Text_Title", 210f, 380f, 100f);
            FixChild(panel, "Input_PlayerName", 60f, 320f, 50f);
            FixChild(panel, "Text_BotLabel", -10f, 300f, 30f);
            FixChild(panel, "Dropdown_BotCount", -50f, 300f, 30f);
            FixChild(panel, "Button_PlaySolo", -160f, 320f, 70f);
            FixChild(panel, "Button_PlayLAN", -240f, 320f, 70f);
            FixChild(panel, "Text_Version", -285f, 380f, 20f);
        }

        private void FixChild(GameObject parent, string name, float y, float w, float h)
        {
            var t = parent.transform.Find(name);
            if (t != null)
            {
                var rt = t.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, y);
                rt.sizeDelta = new Vector2(w, h);
            }
        }

        private void AutoLinkComponents()
        {
            if (playerNameInput == null) playerNameInput = FindComponentByName<InputField>("Input_PlayerName");
            if (botCountLabel == null) botCountLabel = FindComponentByName<Text>("Text_BotLabel");
            if (botCountDropdown == null) botCountDropdown = FindComponentByName<Dropdown>("Dropdown_BotCount");
            if (playSoloButton == null) playSoloButton = FindComponentByName<Button>("Button_PlaySolo");
            if (lanButton == null) lanButton = FindComponentByName<Button>("Button_PlayLAN");
            if (lanButtonLabel == null) lanButtonLabel = FindComponentByName<Text>("Text_LAN");
            if (versionText == null) versionText = FindComponentByName<Text>("Text_Version");
        }

        private void StyleUIElements()
        {
            // Input Field Decoration
            if (playerNameInput != null)
            {
                var img = playerNameInput.GetComponent<Image>();
                if (img != null) img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

                // Rebuild InputField Hierarchy if broken
                if (playerNameInput.transform.childCount < 2)
                    RebuildInputField(playerNameInput);
            }

            // Dropdown
            if (botCountDropdown != null)
            {
                var img = botCountDropdown.GetComponent<Image>();
                if (img != null) img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

                if (botCountDropdown.transform.childCount < 2)
                    RebuildDropdown(botCountDropdown);
            }

            // Buttons
            StyleButton(playSoloButton, new Color(0.15f, 0.45f, 0.15f, 1f));
            StyleButton(lanButton, new Color(0.3f, 0.3f, 0.3f, 1f));

            // Labels
            StyleText(botCountLabel, 18, Color.white);
            StyleText(versionText, 14, new Color(1, 1, 1, 0.6f));
            StyleText(lanButtonLabel, 16, Color.gray);
            
            var titleText = FindComponentByName<Text>("Text_Title");
            if (titleText != null) StyleText(titleText, 32, new Color(1f, 0.2f, 0.2f, 1f), true);
        }

        private void RebuildInputField(InputField field)
        {
            // Clear
            foreach (Transform child in field.transform) Destroy(child.gameObject);

            // Placeholder
            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(field.transform);
            var phText = phGo.AddComponent<Text>();
            phText.text = "Enter Name...";
            phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phText.fontStyle = FontStyle.Italic;
            phText.alignment = TextAnchor.MiddleCenter;
            phText.color = new Color(1, 1, 1, 0.3f);
            var phRT = phGo.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one; phRT.offsetMin = Vector2.zero; phRT.offsetMax = Vector2.zero;

            // Text
            var tGo = new GameObject("Text");
            tGo.transform.SetParent(field.transform);
            var tText = tGo.AddComponent<Text>();
            tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = Color.white;
            var tRT = tGo.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one; tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;

            field.placeholder = phText;
            field.textComponent = tText;
        }

        private void RebuildDropdown(Dropdown dropdown)
        {
            // Clear existing
            foreach (Transform child in dropdown.transform) 
            {
                if (child.name == "Label" || child.name == "Arrow" || child.name == "Template")
                    Destroy(child.gameObject);
            }

            // Background for the main dropdown box
            var img = dropdown.GetComponent<Image>() ?? dropdown.gameObject.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            // Label (current value)
            var lGo = new GameObject("Label");
            lGo.transform.SetParent(dropdown.transform, false);
            var lText = lGo.AddComponent<Text>();
            lText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lText.alignment = TextAnchor.MiddleCenter;
            lText.color = Color.white;
            lText.fontSize = 16;
            var lRT = lGo.GetComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one; 
            lRT.offsetMin = new Vector2(10, 0); lRT.offsetMax = new Vector2(-30, 0);

            // Arrow
            var aGo = new GameObject("Arrow");
            aGo.transform.SetParent(dropdown.transform, false);
            var aText = aGo.AddComponent<Text>();
            aText.text = "▼";
            aText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            aText.alignment = TextAnchor.MiddleCenter;
            aText.color = new Color(1, 1, 1, 0.5f);
            var aRT = aGo.GetComponent<RectTransform>();
            aRT.anchorMin = new Vector2(1, 0); aRT.anchorMax = new Vector2(1, 1); 
            aRT.offsetMin = new Vector2(-30, 0); aRT.offsetMax = Vector2.zero;

            // Template (The list that opens)
            var tGo = new GameObject("Template");
            tGo.transform.SetParent(dropdown.transform, false);
            var tRT = tGo.AddComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0, 0); tRT.anchorMax = new Vector2(1, 0);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.anchoredPosition = new Vector2(0, -2);
            tRT.sizeDelta = new Vector2(0, 200);

            var tImg = tGo.AddComponent<Image>();
            tImg.color = new Color(0.1f, 0.1f, 0.1f, 0.98f);
            var scrollRect = tGo.AddComponent<ScrollRect>();

            // Viewport
            var vGo = new GameObject("Viewport");
            vGo.transform.SetParent(tGo.transform, false);
            vGo.AddComponent<Mask>().showMaskGraphic = false;
            vGo.AddComponent<Image>().color = Color.clear;
            var vRT = vGo.GetComponent<RectTransform>();
            vRT.anchorMin = Vector2.zero; vRT.anchorMax = Vector2.one; 
            vRT.offsetMin = Vector2.zero; vRT.offsetMax = Vector2.zero;

            // Content
            var cGo = new GameObject("Content");
            cGo.transform.SetParent(vGo.transform, false);
            var cRT = cGo.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.anchoredPosition = Vector2.zero;
            cRT.sizeDelta = new Vector2(0, 30);
            
            // Item (The prototype)
            var iGo = new GameObject("Item");
            iGo.transform.SetParent(cGo.transform, false);
            var it = iGo.AddComponent<Toggle>();
            var iRT = iGo.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0, 1); iRT.anchorMax = new Vector2(1, 1);
            iRT.pivot = new Vector2(0.5f, 1);
            iRT.anchoredPosition = Vector2.zero;
            iRT.sizeDelta = new Vector2(0, 30);

            // Item Background
            var ibGo = new GameObject("Item Background");
            ibGo.transform.SetParent(iGo.transform, false);
            var ibImg = ibGo.AddComponent<Image>();
            ibImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var ibRT = ibGo.GetComponent<RectTransform>();
            ibRT.anchorMin = Vector2.zero; ibRT.anchorMax = Vector2.one; 
            ibRT.offsetMin = Vector2.zero; ibRT.offsetMax = Vector2.zero;

            // Item Label
            var ilGo = new GameObject("Item Label");
            ilGo.transform.SetParent(iGo.transform, false);
            var ilText = ilGo.AddComponent<Text>();
            ilText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ilText.alignment = TextAnchor.MiddleCenter;
            ilText.color = Color.white;
            ilText.fontSize = 14;
            var ilRT = ilGo.GetComponent<RectTransform>();
            ilRT.anchorMin = Vector2.zero; ilRT.anchorMax = Vector2.one; 
            ilRT.offsetMin = new Vector2(10, 0); ilRT.offsetMax = new Vector2(-10, 0);

            it.targetGraphic = ibImg;
            it.graphic = ilGo.GetComponent<Image>(); // Placeholder toggle graphic

            scrollRect.content = cRT;
            scrollRect.viewport = vRT;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            dropdown.captionText = lText;
            dropdown.template = tRT;
            dropdown.itemText = ilText;

            tGo.SetActive(false); // Hide template last
            
            Debug.Log("[WelcomeScreenManager] Dropdown rebuilt and template assigned.");
        }

        private void StyleButton(Button b, Color baseCol)
        {
            if (b == null) return;
            var img = b.GetComponent<Image>();
            if (img != null) img.color = baseCol;

            var cb = b.colors;
            cb.normalColor = baseCol;
            cb.highlightedColor = baseCol * 1.2f;
            cb.pressedColor = baseCol * 0.8f;
            b.colors = cb;

            var txt = b.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.color = Color.white;
                txt.fontSize = 20;
                txt.alignment = TextAnchor.MiddleCenter;
                var shadow = txt.gameObject.GetComponent<Shadow>() ?? txt.gameObject.AddComponent<Shadow>();
                shadow.effectColor = Color.black;
            }
        }

        private void StyleText(Text t, int size, Color col, bool bold = false)
        {
            if (t == null) return;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = col;
            t.alignment = TextAnchor.MiddleCenter;
            if (bold) t.fontStyle = FontStyle.Bold;
            var shadow = t.gameObject.GetComponent<Shadow>() ?? t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
        }

        private T FindComponentByName<T>(string name) where T : Component
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<T>() : null;
        }

        private void Start()
        {
            string savedName = PlayerPrefs.GetString("PlayerName", "Survivor");
            _botCount = PlayerPrefs.GetInt("BotCount", defaultBotCount);

            if (playerNameInput != null)
                playerNameInput.text = savedName;

            if (botCountDropdown != null)
            {
                botCountDropdown.options.Clear();
                for (int i = 0; i <= maxBots; i++)
                {
                    botCountDropdown.options.Add(new Dropdown.OptionData(i.ToString()));
                }
                botCountDropdown.value = Mathf.Clamp(_botCount, 0, maxBots);
                botCountDropdown.onValueChanged.AddListener(OnBotCountDropdownChanged);
            }

            RefreshBotLabel();

            if (playSoloButton != null)
                playSoloButton.onClick.AddListener(StartSolo);

            if (lanButton != null)
            {
                lanButton.interactable = false;
                if (lanButtonLabel != null)
                    lanButtonLabel.text = "LAN Multiplayer\n<size=12><color=#FF6B35>[Coming Soon]</color></size>";
            }
        }

        private void OnBotCountDropdownChanged(int index)
        {
            _botCount = index;
            RefreshBotLabel();
        }

        private void RefreshBotLabel()
        {
            if (botCountLabel != null)
                botCountLabel.text = $"Allied Bots: {_botCount}";
        }

        public void StartSolo()
        {
            string playerName = "Survivor";
            if (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text))
                playerName = playerNameInput.text.Trim();

            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.SetInt("BotCount", _botCount);
            PlayerPrefs.SetString("GameMode", "Solo");
            PlayerPrefs.Save();

            Debug.Log($"[WelcomeScreen] Starting Solo: Name={playerName}, Bots={_botCount}");
            SceneManager.LoadScene(arenaSceneName);
        }
    }
}

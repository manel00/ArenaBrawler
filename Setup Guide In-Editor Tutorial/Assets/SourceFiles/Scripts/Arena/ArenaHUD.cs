using UnityEngine;
using UnityEngine.UI;

namespace ArenaEnhanced
{
    public class ArenaHUD : MonoBehaviour
    {
        private ArenaCombatant player;
        private ArenaCombatant target; 

        // UI Elements
        private Canvas canvas;
        private Text playerHpText;
        private Image playerHpFill;
        private Image playerHpBack;
        
        private GameObject targetFrame;
        private Text targetNameText;
        private Image targetHpFill;

        private Text gameOverText;
        private GameObject gameOverPanel;

        private float _visualHp;

        public void Initialize(ArenaCombatant mainPlayer)
        {
            player = mainPlayer;
            _visualHp = player != null ? player.hp : 0;
            CreateCanvasUI();
        }

        private void Update()
        {
            if (player == null || canvas == null) return;

            // Smooth HP transition
            _visualHp = Mathf.Lerp(_visualHp, player.hp, Time.deltaTime * 5f);
            
            // --- PLAYER FRAME UPDATE ---
            if (playerHpFill != null)
            {
                playerHpFill.fillAmount = Mathf.Clamp01(_visualHp / player.maxHp);
                playerHpText.text = $"HP: {Mathf.Ceil(player.hp)} / {Mathf.Ceil(player.maxHp)}";
                
                // Color shift based on health
                float ratio = player.hp / player.maxHp;
                playerHpFill.color = Color.Lerp(new Color(0.8f, 0.2f, 0.2f), new Color(0.2f, 0.9f, 0.3f), ratio);
            }

            // --- GAME OVER UPDATE ---
            var gm = Object.FindAnyObjectByType<ArenaGameManager>();
            if (gm != null && gm.ended)
            {
                if (gameOverPanel != null && !gameOverPanel.activeSelf)
                {
                    gameOverPanel.SetActive(true);
                    gameOverText.text = gm.endText + "\n<size=24>Presiona [R] para reiniciar</size>";
                }
            }
        }

        private void CreateCanvasUI()
        {
            var canvasGo = new GameObject("ArenaCanvas_PremiumHUD");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1f;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // --- TOP HUD (Player Stats) ---
            var playerRoot = CreateUIBox(canvasGo.transform, "PlayerHUD", new Vector2(30, -30), new Vector2(300, 75), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            playerRoot.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

            // HP Bar Premium
            playerHpBack = CreateUIBox(playerRoot.transform, "HP_BG", new Vector2(10, -35), new Vector2(280, 30), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1)).GetComponent<Image>();
            playerHpBack.color = new Color(0.2f, 0, 0, 0.8f);
            
            playerHpFill = CreateUIBar(playerHpBack.transform, "HP_Fill", Vector2.zero, Vector2.zero, new Color(0.2f, 0.9f, 0.3f), Vector2.zero, Vector2.one);
            playerHpText = CreateUIText(playerHpBack.transform, "HP_Label", Vector2.zero, Vector2.zero, "", defaultFont, Color.white, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

            CreateUIText(playerRoot.transform, "Name_Label", new Vector2(10, -5), new Vector2(280, 20), player.displayName.ToUpper(), defaultFont, Color.yellow, 18, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1));

            // --- BOTTOM HUD (Skills) ---
            var skillRoot = CreateUIBox(canvasGo.transform, "SkillHUD", new Vector2(0, 40), new Vector2(250, 80), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            skillRoot.GetComponent<Image>().color = new Color(0, 0, 0, 0.4f);

            CreateSkillIcon(skillRoot.transform, "Skill_1", new Vector2(-60, 0), "1", "Fireball", "Assets/Tutorials/Media/1.0 Icon.png", defaultFont);
            CreateSkillIcon(skillRoot.transform, "Skill_2", new Vector2(60, 0), "2", "Summon", "Assets/Tutorials/Media/2.0 Icon.png", defaultFont);

            // --- GAME OVER PANEL ---
            gameOverPanel = CreateUIBox(canvasGo.transform, "GameOverPanel", Vector2.zero, new Vector2(400, 150), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            gameOverPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.9f);
            gameOverText = CreateUIText(gameOverPanel.transform, "GameOverText", Vector2.zero, Vector2.zero, "", defaultFont, Color.red, 36, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            gameOverPanel.SetActive(false);
        }

        private GameObject CreateUIBox(Transform parent, string name, Vector2 pos, Vector2 size, Vector2 ancMin, Vector2 ancMax, Vector2 piv)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = piv;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.AddComponent<Image>();
            return go;
        }

        private Text CreateUIText(Transform parent, string name, Vector2 pos, Vector2 size, string text, Font font, Color color, int sizePt, TextAnchor align, Vector2 ancMin, Vector2 ancMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            if (ancMin == ancMax) { rt.anchoredPosition = pos; rt.sizeDelta = size; }
            else { rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.one; }

            var t = go.AddComponent<Text>();
            t.text = text; t.font = font; t.color = color; t.fontSize = sizePt; t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Image CreateUIBar(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Vector2 ancMin, Vector2 ancMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            return img;
        }

        private void CreateSkillIcon(Transform parent, string name, Vector2 pos, string key, string label, string iconPath, Font font)
        {
            var slot = CreateUIBox(parent, name, pos, new Vector2(60, 60), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            slot.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            // Icon
#if UNITY_EDITOR
            var iconImg = new GameObject("Icon");
            iconImg.transform.SetParent(slot.transform, false);
            var rt = iconImg.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(5, 5); rt.offsetMax = new Vector2(-5, -5);
            var img = iconImg.AddComponent<Image>();
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite != null) img.sprite = sprite;
            else img.color = new Color(1, 1, 1, 0.1f);
#endif

            var keyLabel = CreateUIText(slot.transform, "Key", new Vector2(5, -5), new Vector2(30, 30), key, font, Color.white, 14, TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(0, 1));
            keyLabel.gameObject.AddComponent<Outline>().effectColor = Color.black;
            
            var nameLabel = CreateUIText(slot.transform, "Name", new Vector2(0, -50), new Vector2(150, 20), label, font, Color.white, 12, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        }
    }
}

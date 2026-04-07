using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

namespace ArenaEnhanced
{
    public enum ToastType { Info, Success, Warning, Error, Achievement, LevelUp }

    public class ToastNotificationManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform toastContainer;
        [SerializeField] private GameObject toastPrefab;

        [Header("Settings")]
        [SerializeField] private int maxToastCount = 5;
        [SerializeField] private float toastDuration = 3f;
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float slideInDistance = 100f;
        [SerializeField] private float spacing = 70f;

        [Header("Colors")]
        [SerializeField] private Color infoColor = new Color32(100, 200, 255, 255);
        [SerializeField] private Color successColor = new Color32(100, 255, 100, 255);
        [SerializeField] private Color warningColor = new Color32(255, 200, 50, 255);
        [SerializeField] private Color errorColor = new Color32(255, 80, 80, 255);
        [SerializeField] private Color achievementColor = new Color32(255, 215, 0, 255);
        [SerializeField] private Color levelUpColor = new Color32(150, 50, 255, 255);

        [Header("Icons (using Text)")]
        [SerializeField] private string infoIcon = "ℹ";
        [SerializeField] private string successIcon = "✓";
        [SerializeField] private string warningIcon = "⚠";
        [SerializeField] private string errorIcon = "✕";
        [SerializeField] private string achievementIcon = "★";
        [SerializeField] private string levelUpIcon = "▲";

        private Queue<GameObject> _activeToasts = new Queue<GameObject>();
        private static ToastNotificationManager _instance;

        public static ToastNotificationManager Instance
        {
            get
            {
                if (_instance == null)
                    CreateInstance();
                return _instance;
            }
        }

        private static void CreateInstance()
        {
            var go = new GameObject("ToastNotificationManager");
            _instance = go.AddComponent<ToastNotificationManager>();
            _instance.SetupCanvas();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            SetupCanvas();
            SetupContainer();
        }

        private void SetupCanvas()
        {
            if (canvas == null)
            {
                canvas = gameObject.GetComponent<Canvas>();
                if (canvas == null)
                    canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // On top of everything

            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (gameObject.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        private void SetupContainer()
        {
            if (toastContainer == null)
            {
                var containerGo = new GameObject("ToastContainer");
                toastContainer = containerGo.AddComponent<RectTransform>();
                toastContainer.SetParent(canvas.transform);
                toastContainer.anchorMin = new Vector2(1, 1);
                toastContainer.anchorMax = new Vector2(1, 1);
                toastContainer.pivot = new Vector2(1, 1);
                toastContainer.anchoredPosition = new Vector2(-20, -20);
                toastContainer.sizeDelta = new Vector2(400, 600);
            }
        }

        // ============ PUBLIC API ============

        public static void Show(string message, ToastType type = ToastType.Info)
        {
            Instance.ShowToast(message, type);
        }

        public static void ShowAchievement(string title, string description)
        {
            Instance.ShowToast($"<b>{title}</b>\n{description}", ToastType.Achievement);
        }

        public static void ShowLevelUp(int level)
        {
            Instance.ShowToast($"<size=24><b>LEVEL UP!</b></size>\nLevel {level} reached!", ToastType.LevelUp);
        }

        public static void ShowSuccess(string message)
        {
            Instance.ShowToast(message, ToastType.Success);
        }

        public static void ShowError(string message)
        {
            Instance.ShowToast(message, ToastType.Error);
        }

        public static void ShowWarning(string message)
        {
            Instance.ShowToast(message, ToastType.Warning);
        }

        // ============ PRIVATE METHODS ============

        private void ShowToast(string message, ToastType type)
        {
            // Remove oldest if at max capacity
            while (_activeToasts.Count >= maxToastCount)
            {
                var oldest = _activeToasts.Dequeue();
                if (oldest != null)
                    StartCoroutine(RemoveToast(oldest, true));
            }

            // Create toast
            GameObject toast = CreateToastObject(message, type);
            _activeToasts.Enqueue(toast);

            // Animate in
            StartCoroutine(AnimateToastIn(toast));

            // Auto remove
            StartCoroutine(AutoRemove(toast));
        }

        private GameObject CreateToastObject(string message, ToastType type)
        {
            GameObject toast = new GameObject("Toast");
            toast.transform.SetParent(toastContainer, false);

            var rt = toast.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380, 60);
            rt.anchoredPosition = new Vector2(slideInDistance, -_activeToasts.Count * spacing);

            // Background image
            var bg = toast.AddComponent<UnityEngine.UI.Image>();
            bg.color = GetBackgroundColor(type);
            bg.sprite = null;
            bg.type = UnityEngine.UI.Image.Type.Sliced;

            // Create text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(toast.transform, false);

            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(50, 5);
            textRt.offsetMax = new Vector2(-10, -5);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{GetIcon(type)} {message}";
            tmp.fontSize = 18;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.richText = true;
            tmp.overflowMode = TextOverflowModes.Overflow;

            // Add outline effect
            tmp.outlineColor = new Color32(0, 0, 0, 128);
            tmp.outlineWidth = 0.2f;

            // Start invisible
            bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, 0);
            tmp.color = new Color(1, 1, 1, 0);

            return toast;
        }

        private Color GetBackgroundColor(ToastType type)
        {
            return type switch
            {
                ToastType.Info => infoColor,
                ToastType.Success => successColor,
                ToastType.Warning => warningColor,
                ToastType.Error => errorColor,
                ToastType.Achievement => achievementColor,
                ToastType.LevelUp => levelUpColor,
                _ => infoColor
            };
        }

        private string GetIcon(ToastType type)
        {
            return type switch
            {
                ToastType.Info => infoIcon,
                ToastType.Success => successIcon,
                ToastType.Warning => warningIcon,
                ToastType.Error => errorIcon,
                ToastType.Achievement => achievementIcon,
                ToastType.LevelUp => levelUpIcon,
                _ => infoIcon
            };
        }

        private IEnumerator AnimateToastIn(GameObject toast)
        {
            var rt = toast.GetComponent<RectTransform>();
            var bg = toast.GetComponent<UnityEngine.UI.Image>();
            var tmp = toast.GetComponentInChildren<TextMeshProUGUI>();

            float elapsed = 0;
            Vector2 startPos = new Vector2(slideInDistance, rt.anchoredPosition.y);
            Vector2 endPos = new Vector2(0, rt.anchoredPosition.y);
            Color targetColor = GetBackgroundColor(GetToastType(toast));

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                float smoothT = Mathf.SmoothStep(0, 1, t);

                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);
                bg.color = new Color(targetColor.r, targetColor.g, targetColor.b, smoothT * 0.9f);
                tmp.color = new Color(1, 1, 1, smoothT);

                yield return null;
            }

            rt.anchoredPosition = endPos;
            bg.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0.9f);
            tmp.color = Color.white;
        }

        private IEnumerator AutoRemove(GameObject toast)
        {
            yield return new WaitForSeconds(toastDuration);

            if (toast != null)
            {
                StartCoroutine(RemoveToast(toast, false));
            }
        }

        private IEnumerator RemoveToast(GameObject toast, bool immediate)
        {
            if (toast == null) yield break;

            var rt = toast.GetComponent<RectTransform>();
            var bg = toast.GetComponent<UnityEngine.UI.Image>();
            var tmp = toast.GetComponentInChildren<TextMeshProUGUI>();

            float elapsed = 0;
            Vector2 startPos = rt.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(slideInDistance * 0.5f, 0);

            while (elapsed < fadeOutDuration)
            {
                if (toast == null) yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                float smoothT = Mathf.SmoothStep(0, 1, t);

                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);

                float alpha = 1 - smoothT;
                bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, alpha * 0.9f);
                tmp.color = new Color(1, 1, 1, alpha);

                yield return null;
            }

            if (toast != null)
                Destroy(toast);

            // Reorder remaining toasts
            if (!immediate)
                StartCoroutine(ReorderToasts());
        }

        private IEnumerator ReorderToasts()
        {
            yield return new WaitForSeconds(0.1f);

            var toasts = new List<GameObject>(_activeToasts);
            _activeToasts.Clear();

            for (int i = 0; i < toasts.Count; i++)
            {
                if (toasts[i] != null)
                {
                    _activeToasts.Enqueue(toasts[i]);
                    var rt = toasts[i].GetComponent<RectTransform>();
                    StartCoroutine(MoveToPosition(rt, new Vector2(0, -i * spacing)));
                }
            }
        }

        private IEnumerator MoveToPosition(RectTransform rt, Vector2 targetPos)
        {
            Vector2 startPos = rt.anchoredPosition;
            float elapsed = 0;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                if (rt == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }

            if (rt != null)
                rt.anchoredPosition = targetPos;
        }

        private ToastType GetToastType(GameObject toast)
        {
            var tmp = toast.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp == null) return ToastType.Info;

            string text = tmp.text;
            if (text.Contains(levelUpIcon)) return ToastType.LevelUp;
            if (text.Contains(achievementIcon)) return ToastType.Achievement;
            if (text.Contains(errorIcon)) return ToastType.Error;
            if (text.Contains(warningIcon)) return ToastType.Warning;
            if (text.Contains(successIcon)) return ToastType.Success;
            return ToastType.Info;
        }

        // Debug methods
        [ContextMenu("Test Info")]
        private void TestInfo() => Show("This is an info message", ToastType.Info);

        [ContextMenu("Test Success")]
        private void TestSuccess() => Show("Quest completed successfully!", ToastType.Success);

        [ContextMenu("Test Warning")]
        private void TestWarning() => Show("Low health warning!", ToastType.Warning);

        [ContextMenu("Test Error")]
        private void TestError() => Show("Failed to connect!", ToastType.Error);

        [ContextMenu("Test Achievement")]
        private void TestAchievement() => ShowAchievement("First Blood", "Defeat your first enemy");

        [ContextMenu("Test Level Up")]
        private void TestLevelUp() => ShowLevelUp(5);
    }
}

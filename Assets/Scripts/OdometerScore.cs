using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public sealed class OdometerScore : MonoBehaviour
    {
        private sealed class Reel
        {
            public RectTransform viewport;
            public Text current, next;
            public double position, start, target;
            public float elapsed;
        }

        private static readonly string[] digits = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        private static readonly VisualSettings.ScorePanelData defaults = new();
        private readonly List<Reel> reels = new();
        private readonly List<Text> separators = new();
        private VisualSettings settings;
        private VisualSettings.ScorePanelData Style => settings != null ? settings.scorePanel : defaults;
        private Text source;
        private RectTransform root;
        private bool sourceWasEnabled, initialized, useCommas;
        private int value;
        private Font cachedFont;
        private int cachedFontSize;
        private FontStyle cachedFontStyle;
        private float digitWidth, commaWidth, height;

        public float PreferredWidth => reels.Count * digitWidth +
            (useCommas ? (reels.Count - 1) / 3 * commaWidth : 0f) +
            Mathf.Max(0, reels.Count - 1) * Style.digitSpacing;

        public bool IsAnimating
        {
            get
            {
                if (!isActiveAndEnabled) return false;
                foreach (Reel reel in reels) if (reel.position != reel.target) return true;
                return false;
            }
        }

        private void Awake()
        {
            source = GetComponent<Text>();
            sourceWasEnabled = source.enabled;
            GameObject go = new GameObject("Digit Reels", typeof(RectTransform));
            root = go.GetComponent<RectTransform>();
            root.SetParent(transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
        }

        private void OnEnable()
        {
            if (source != null) source.enabled = false;
            if (root != null) root.gameObject.SetActive(true);
        }

        public void Configure(VisualSettings visuals)
        {
            settings = visuals;
        }

        public void SetValue(int number, bool commas, bool instant = false)
        {
            number = Mathf.Max(0, number);
            if (initialized && number == value && commas == useCommas && !instant) return;
            instant |= !initialized || number < value || Style.rollDuration <= 0f;
            value = number;
            useCommas = commas;
            initialized = true;
            string text = number.ToString(CultureInfo.InvariantCulture);
            while (reels.Count > text.Length)
            {
                Destroy(reels[reels.Count - 1].viewport.gameObject);
                reels.RemoveAt(reels.Count - 1);
            }
            while (reels.Count < text.Length) AddReel();
            for (int i = 0; i < reels.Count; i++)
            {
                Reel reel = reels[i];
                int digit = text[text.Length - 1 - i] - '0';
                if (instant)
                {
                    reel.position = reel.start = reel.target = digit;
                    continue;
                }
                if ((int)reel.target % 10 == digit) continue;
                // Retarget from the current fractional position, preserving motion during rapid pickups.
                double whole = System.Math.Floor(reel.position);
                double target = whole + (digit - (int)whole % 10 + 10) % 10;
                if (target < reel.position) target += 10;
                reel.start = reel.position;
                reel.target = target;
                reel.elapsed = 0f;
            }
            Render();
        }

        private void LateUpdate()
        {
            foreach (Reel reel in reels)
            {
                if (reel.position == reel.target) continue;
                reel.elapsed += Time.unscaledDeltaTime;
                float t = Style.rollDuration <= 0f ? 1f : Mathf.Clamp01(reel.elapsed / Style.rollDuration);
                reel.position = t >= 1f ? reel.target : reel.start +
                    (reel.target - reel.start) * Mathf.SmoothStep(0f, 1f, t);
            }
            Render();
        }

        private void AddReel()
        {
            GameObject go = new GameObject($"Digit {reels.Count + 1}", typeof(RectTransform), typeof(RectMask2D));
            RectTransform viewport = go.GetComponent<RectTransform>();
            viewport.SetParent(root, false);
            reels.Add(new Reel { viewport = viewport, current = CreateText("Current", viewport), next = CreateText("Next", viewport) });
        }

        private Text CreateText(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }

        private void Render()
        {
            if (source == null || source.font == null || reels.Count == 0) return;
            if (cachedFont != source.font || cachedFontSize != source.fontSize || cachedFontStyle != source.fontStyle)
            {
                cachedFont = source.font;
                cachedFontSize = source.fontSize;
                cachedFontStyle = source.fontStyle;
                TextGenerationSettings generation = source.GetGenerationSettings(Vector2.zero);
                digitWidth = 0f;
                foreach (string digit in digits)
                    digitWidth = Mathf.Max(digitWidth, source.cachedTextGeneratorForLayout.GetPreferredWidth(digit, generation) / source.pixelsPerUnit);
                commaWidth = source.cachedTextGeneratorForLayout.GetPreferredWidth(",", generation) / source.pixelsPerUnit;
            }
            height = Mathf.Max(1f, source.fontSize * Style.digitHeightMultiplier);
            float x = -PreferredWidth * 0.5f;
            int separator = 0;
            float direction = Style.rollUp ? 1f : -1f;
            for (int i = reels.Count - 1; i >= 0; i--)
            {
                Reel reel = reels[i];
                reel.viewport.sizeDelta = new Vector2(digitWidth, height);
                reel.viewport.anchoredPosition = new Vector2(x + digitWidth * 0.5f, 0f);
                double whole = System.Math.Floor(reel.position);
                float fraction = (float)(reel.position - whole);
                StyleText(reel.current, digitWidth);
                StyleText(reel.next, digitWidth);
                reel.current.text = digits[(int)whole % 10];
                reel.next.text = digits[((int)whole + 1) % 10];
                reel.current.rectTransform.anchoredPosition = Vector2.up * (fraction * height * direction);
                reel.next.rectTransform.anchoredPosition = Vector2.up * ((fraction - 1f) * height * direction);
                x += digitWidth;
                if (i > 0) x += Style.digitSpacing;
                if (!useCommas || i == 0 || i % 3 != 0) continue;
                if (separator == separators.Count) separators.Add(CreateText("Comma", root));
                Text comma = separators[separator++];
                comma.gameObject.SetActive(true);
                StyleText(comma, commaWidth);
                comma.text = ",";
                comma.rectTransform.anchoredPosition = new Vector2(x + commaWidth * 0.5f, 0f);
                x += commaWidth;
            }
            for (int i = separator; i < separators.Count; i++) separators[i].gameObject.SetActive(false);
        }

        private void StyleText(Text text, float width)
        {
            text.font = source.font;
            text.fontSize = source.fontSize;
            text.fontStyle = source.fontStyle;
            text.color = source.color;
            text.lineSpacing = source.lineSpacing;
            text.rectTransform.sizeDelta = new Vector2(width, height);
        }

        private void OnDisable()
        {
            if (source != null) source.enabled = sourceWasEnabled;
            if (root != null) root.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }
    }
}

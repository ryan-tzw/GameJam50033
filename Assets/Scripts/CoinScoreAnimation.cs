using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class CoinScoreAnimation : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        private static readonly VisualSettings.CoinFlightData defaultVisualCoinFlight = new();
        private VisualSettings.CoinFlightData visualCoinFlight => visualSettings != null ? visualSettings.coinFlight : defaultVisualCoinFlight;
        private static readonly VisualSettings.MilestoneData defaultMilestones = new();
        private VisualSettings.MilestoneData milestoneStyle => visualSettings != null ? visualSettings.milestones : defaultMilestones;
        [SerializeField] private RectTransform effectLayer;
        [SerializeField] private RectTransform scoreTarget;

        private sealed class Flight
        {
            public Image image;
            public Sprite[] frames;
            public float fps, elapsed, spinStart, bend;
            public int points;
            public Vector2 viewportStart;
            public Vector2 launchVelocity;
            public float scatterGravity, scatterDuration, attractionDelay;
            public Action<bool> arrived;
        }

        private sealed class Popup
        {
            public Text text;
            public float elapsed;
            public Vector2 direction;
            public float rotation;
        }

        private readonly List<Flight> flights = new();
        private readonly List<Popup> popups = new();
        private sealed class Milestone
        {
            public int tiles, points;
            public bool major;
        }
        private readonly Queue<Milestone> milestoneQueue = new();
        private Text milestoneText;
        private RectTransform milestoneRect;
        private Image milestoneFill, milestoneTopBorder, milestoneBottomBorder, milestoneLeftStar, milestoneRightStar;
        private bool milestoneMajor;
        private float milestoneElapsed;
        private Canvas canvas;
        private Vector3 originalTargetScale;
        private float pulseRemaining;
        private float pulseScale = 1f;
        public bool HasFlyingCoins => flights.Count > 0;

        private void Awake()
        {
            canvas = effectLayer != null ? effectLayer.GetComponentInParent<Canvas>() : null;
            if (scoreTarget != null)
            {
                originalTargetScale = scoreTarget.localScale;
            }
            if (effectLayer != null && scoreTarget != null) CreateMilestoneBanner();
        }

        public bool TryFly(Vector3 worldPosition, float worldHeight, Sprite[] frames, float fps,
            int points, Action<bool> arrived, bool scatter = false, float attractionDelay = 0f)
        {
            Camera camera = Camera.main;
            if (!isActiveAndEnabled || canvas == null || effectLayer == null || scoreTarget == null ||
                visualCoinFlight.flyingCoinPrefab == null || camera == null || frames == null || frames.Length == 0)
            {
                return false;
            }

            Vector3 screen = camera.WorldToScreenPoint(worldPosition);
            Vector3 top = camera.WorldToScreenPoint(worldPosition + camera.transform.up * worldHeight);
            Image image = Instantiate(visualCoinFlight.flyingCoinPrefab, effectLayer);
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = Vector2.one * Mathf.Abs(top.y - screen.y) / canvas.scaleFactor;
            image.sprite = frames[Mathf.FloorToInt(Time.time * fps) % frames.Length];
            image.rectTransform.anchoredPosition = ScreenToLocal(screen);
            Vector2 launchVelocity = Vector2.zero;
            if (scatter)
            {
                float angle = UnityEngine.Random.Range(-visualCoinFlight.scatterSpread * 0.5f,
                    visualCoinFlight.scatterSpread * 0.5f) * Mathf.Deg2Rad;
                float speed = visualCoinFlight.scatterLaunchSpeed * UnityEngine.Random.Range(0.8f, 1.2f);
                launchVelocity = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * speed;
            }
            flights.Add(new Flight
            {
                image = image, frames = frames, fps = fps, spinStart = Time.time,
                viewportStart = new Vector2(screen.x / Screen.width, screen.y / Screen.height),
                bend = visualCoinFlight.curvature * UnityEngine.Random.Range(0.7f, 1.3f) *
                    (UnityEngine.Random.value < 0.5f ? -1f : 1f),
                launchVelocity = launchVelocity,
                scatterGravity = scatter ? Mathf.Max(0f, visualCoinFlight.scatterGravity) : 0f,
                scatterDuration = scatter ? Mathf.Max(0.01f, visualCoinFlight.scatterDuration) : 0f,
                attractionDelay = Mathf.Max(0f, attractionDelay),
                points = points, arrived = arrived
            });
            return true;
        }

        private Vector2 ScreenToLocal(Vector2 screen)
        {
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(effectLayer, screen, uiCamera, out Vector2 local);
            return local;
        }

        private Vector2 TargetPosition()
        {
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return ScreenToLocal(RectTransformUtility.WorldToScreenPoint(uiCamera,
                scoreTarget.TransformPoint(scoreTarget.rect.center)));
        }

        private void Update()
        {
            if (PauseMenuController.IsPaused) return;
            float delta = Time.unscaledDeltaTime;
            for (int i = flights.Count - 1; i >= 0; i--)
            {
                Flight flight = flights[i];
                flight.elapsed += delta;
                // Sprite playback continues during scatter, the staggered wait, and attraction.
                flight.image.sprite = flight.frames[Mathf.FloorToInt(
                    (flight.spinStart + flight.elapsed) * flight.fps) % flight.frames.Length];
                Vector2 start = ScreenToLocal(new Vector2(flight.viewportStart.x * Screen.width,
                    flight.viewportStart.y * Screen.height));
                float flightElapsed = flight.elapsed - flight.scatterDuration - flight.attractionDelay;
                Vector2 scatterEndVelocity = Vector2.zero;
                if (flight.scatterDuration > 0f)
                {
                    // Keep falling throughout the staggered wait; freeze only the attraction's starting point.
                    float scatterTime = Mathf.Min(flight.elapsed, flight.scatterDuration + flight.attractionDelay);
                    start += flight.launchVelocity * scatterTime +
                        Vector2.down * (0.5f * flight.scatterGravity * scatterTime * scatterTime);
                    scatterEndVelocity = flight.launchVelocity + Vector2.down * flight.scatterGravity * scatterTime;
                    flight.image.rectTransform.anchoredPosition = start;
                }
                if (flightElapsed < 0f)
                {
                    continue;
                }
                float t = Mathf.Clamp01(flightElapsed / visualCoinFlight.flightDuration);
                float progress = Mathf.Clamp01(visualCoinFlight.flightProgress.Evaluate(t));
                Vector2 end = TargetPosition();
                Vector2 direction = (end - start).normalized;
                Vector2 control = (start + end) * 0.5f + new Vector2(-direction.y, direction.x) * flight.bend;
                flight.image.rectTransform.anchoredPosition = (1f - progress) * (1f - progress) * start +
                    2f * (1f - progress) * progress * control + progress * progress * end +
                    // Carry launch motion into attraction, then release it smoothly as the counter takes over.
                    scatterEndVelocity * (flightElapsed * (1f - t) * (1f - t));
                flight.image.rectTransform.localScale = Vector3.one *
                    Mathf.Lerp(1f, visualCoinFlight.arrivalScale, Mathf.InverseLerp(0.75f, 1f, progress));
                if (t >= 1f)
                {
                    flights.RemoveAt(i);
                    Destroy(flight.image.gameObject);
                    flight.arrived(true);
                    ShowPopup(flight.points);
                    PulseScore();
                }
            }

            for (int i = popups.Count - 1; i >= 0; i--)
            {
                Popup popup = popups[i];
                popup.elapsed += delta;
                float t = Mathf.Clamp01(popup.elapsed / visualCoinFlight.popupDuration);
                popup.text.rectTransform.anchoredPosition = TargetPosition() + visualCoinFlight.popupOffset +
                    popup.direction * visualCoinFlight.popupRise * t;
                popup.text.rectTransform.localRotation = Quaternion.Euler(0f, 0f, popup.rotation * t);
                Color color = visualCoinFlight.popupColor;
                color.a *= 1f - t;
                popup.text.color = color;
                if (t >= 1f)
                {
                    Destroy(popup.text.gameObject);
                    popups.RemoveAt(i);
                }
            }

            UpdateMilestone(delta);
            if (scoreTarget != null)
            {
                pulseRemaining = Mathf.Max(0f, pulseRemaining - delta);
                float pulse = Mathf.Sin(Mathf.PI * pulseRemaining / visualCoinFlight.pulseDuration);
                scoreTarget.localScale = originalTargetScale * (1f + visualCoinFlight.pulseStrength * pulseScale * pulse);
                if (pulseRemaining <= 0f) pulseScale = 1f;
            }
        }

        private void PulseScore(float scale = 1f)
        {
            pulseRemaining = visualCoinFlight.pulseDuration;
            pulseScale = Mathf.Max(pulseScale, scale);
        }

        public void ShowMilestone(int tiles, int points, bool major)
        {
            if (!isActiveAndEnabled || effectLayer == null || scoreTarget == null) return;
            // Bonuses are already awarded; queue only presentation so crossed milestones stay readable.
            milestoneQueue.Enqueue(new Milestone { tiles = tiles, points = points, major = major });
            if (!milestoneRect.gameObject.activeSelf) BeginMilestoneBanner();
        }

        private void CreateMilestoneBanner()
        {
            var go = new GameObject("Milestone Banner", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(effectLayer, false);
            milestoneRect = go.GetComponent<RectTransform>();
            milestoneRect.anchorMin = milestoneRect.anchorMax = new Vector2(0.5f, 1f);
            milestoneRect.pivot = new Vector2(0.5f, 1f);
            var group = go.GetComponent<CanvasGroup>();
            group.blocksRaycasts = group.interactable = false;
            milestoneFill = BannerImage("Fill");
            milestoneFill.rectTransform.anchorMin = Vector2.zero;
            milestoneFill.rectTransform.anchorMax = Vector2.one;
            milestoneFill.rectTransform.sizeDelta = Vector2.zero;
            milestoneTopBorder = BannerImage("Top Border");
            milestoneBottomBorder = BannerImage("Bottom Border");
            milestoneLeftStar = BannerImage("Left Star");
            milestoneRightStar = BannerImage("Right Star");
            var textObject = new GameObject("Milestone Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(milestoneRect, false);
            milestoneText = textObject.GetComponent<Text>();
            milestoneText.alignment = TextAnchor.MiddleCenter;
            milestoneText.supportRichText = true;
            milestoneText.raycastTarget = false;
            milestoneText.resizeTextForBestFit = true;
            milestoneText.resizeTextMinSize = 10;
            milestoneText.horizontalOverflow = HorizontalWrapMode.Overflow;
            Text scoreStyle = scoreTarget.GetComponent<Text>();
            milestoneText.font = visualSettings != null ? visualSettings.menuButtons.font : scoreStyle != null ? scoreStyle.font : null;
            if (milestoneText.font != null && milestoneText.font.dynamic)
            {
                const string glyphs = "MILESTONE REACHED / 0123456789 TILES + PTS,";
                milestoneText.font.RequestCharactersInTexture(glyphs, milestoneStyle.fontSize);
                milestoneText.font.RequestCharactersInTexture(glyphs, Mathf.RoundToInt(milestoneStyle.fontSize * milestoneStyle.majorScale));
            }
            go.SetActive(false);
        }

        private Image BannerImage(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(milestoneRect, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private void BeginMilestoneBanner()
        {
            Milestone milestone = milestoneQueue.Dequeue();
            milestoneMajor = milestone.major;
            string format = visualSettings == null || visualSettings.scorePanel.useCommaSeparators ? "N0" : "0";
            string tiles = milestone.tiles.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            string points = milestone.points.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            string separator = $"<color=#{ColorUtility.ToHtmlStringRGB(milestoneStyle.headingColor)}> // </color>";
            milestoneText.text = $"MILESTONE REACHED{separator}{tiles} TILES{separator}" +
                $"<color=#{ColorUtility.ToHtmlStringRGB(milestoneStyle.bonusColor)}>+{points} PTS</color>";
            float scale = milestone.major ? milestoneStyle.majorScale : 1f;
            milestoneElapsed = 0f;
            LayoutMilestoneBanner();
            milestoneRect.anchoredPosition = new Vector2(BannerOffscreenX(true), -milestoneStyle.topInset);
            milestoneRect.gameObject.SetActive(true);
            PulseScore(scale);
        }

        private void LayoutMilestoneBanner()
        {
            var style = milestoneStyle;
            float width = effectLayer.rect.width;
            float emphasis = milestoneMajor ? style.majorScale : 1f;
            milestoneRect.sizeDelta = new Vector2(width, style.bannerHeight);
            milestoneFill.color = new Color(style.panelColor.r, style.panelColor.g, style.panelColor.b, style.panelOpacity);
            for (int i = 0; i < 2; i++)
            {
                Image border = i == 0 ? milestoneTopBorder : milestoneBottomBorder;
                border.color = style.borderColor;
                border.rectTransform.anchorMin = new Vector2(0f, i == 0 ? 1f : 0f);
                border.rectTransform.anchorMax = new Vector2(1f, i == 0 ? 1f : 0f);
                border.rectTransform.pivot = new Vector2(0.5f, i == 0 ? 1f : 0f);
                border.rectTransform.sizeDelta = new Vector2(0f, style.borderWidth);
            }
            Text scoreStyle = scoreTarget.GetComponent<Text>();
            milestoneText.font = visualSettings != null ? visualSettings.menuButtons.font : scoreStyle != null ? scoreStyle.font : null;
            milestoneText.color = visualSettings != null ? visualSettings.menuButtons.textColor : Color.white;
            milestoneText.fontSize = milestoneText.resizeTextMaxSize = Mathf.RoundToInt(style.fontSize * emphasis);
            float starSize = style.starSprite != null ? style.starSize * emphasis : 0f;
            float starSpace = starSize > 0f ? starSize + style.starGap : 0f;
            float textWidth = Mathf.Max(1f, Mathf.Min(milestoneText.preferredWidth + 4f,
                width - 2f * (style.horizontalPadding + starSpace)));
            milestoneText.rectTransform.sizeDelta = new Vector2(textWidth, style.bannerHeight);
            for (int i = 0; i < 2; i++)
            {
                Image star = i == 0 ? milestoneLeftStar : milestoneRightStar;
                star.enabled = style.starSprite != null;
                star.sprite = style.starSprite;
                star.color = style.starColor;
                star.preserveAspect = true;
                star.rectTransform.sizeDelta = Vector2.one * starSize;
                star.rectTransform.anchoredPosition = Vector2.right * (i == 0 ? -1f : 1f) *
                    (textWidth * 0.5f + style.starGap + starSize * 0.5f);
            }
        }

        private void UpdateMilestone(float delta)
        {
            if (milestoneRect == null || !milestoneRect.gameObject.activeSelf) return;
            milestoneElapsed += delta;
            LayoutMilestoneBanner();
            float enter = Mathf.Max(0.01f, milestoneStyle.slideInDuration);
            float hold = Mathf.Max(0.05f, milestoneStyle.duration);
            float leave = Mathf.Max(0.01f, milestoneStyle.slideOutDuration);
            float x = 0f;
            if (milestoneElapsed < enter) x = BannerOffscreenX(true) * Mathf.Pow(1f - milestoneElapsed / enter, 3f);
            else if (milestoneElapsed > enter + hold)
                x = BannerOffscreenX(false) * Mathf.Pow(Mathf.Clamp01((milestoneElapsed - enter - hold) / leave), 3f);
            milestoneRect.anchoredPosition = new Vector2(x, -milestoneStyle.topInset);
            if (milestoneElapsed < enter + hold + leave) return;
            milestoneRect.gameObject.SetActive(false);
            if (milestoneQueue.Count > 0) BeginMilestoneBanner();
        }

        private float BannerOffscreenX(bool right)
        {
            // The effects layer may be inset; use screen edges so no part of the banner peeks in early.
            float edge = ScreenToLocal(new Vector2(right ? Screen.width : 0f, Screen.height)).x;
            return edge - effectLayer.rect.center.x + (right ? 1f : -1f) * (milestoneRect.rect.width * 0.5f + 4f);
        }

        private void ShowPopup(int points)
        {
            Text style = scoreTarget.GetComponent<Text>();
            GameObject go = new GameObject("Coin Score Popup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(effectLayer, false);
            Text text = go.GetComponent<Text>();
            text.font = style != null ? style.font : null;
            text.fontSize = visualCoinFlight.popupFontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = visualCoinFlight.popupColor;
            text.text = $"+{points}";
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(160f, 60f);
            text.rectTransform.anchoredPosition = TargetPosition() + visualCoinFlight.popupOffset;
            float angle = UnityEngine.Random.Range(-visualCoinFlight.popupConeAngle * 0.5f,
                visualCoinFlight.popupConeAngle * 0.5f);
            float radians = angle * Mathf.Deg2Rad;
            popups.Add(new Popup
            {
                text = text,
                direction = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)),
                // Rightward travel tilts clockwise; leftward travel tilts counterclockwise.
                rotation = -Mathf.Sign(angle) * UnityEngine.Random.Range(0.4f, 1f) *
                    visualCoinFlight.popupMaxRotation
            });
        }

        private void OnDisable()
        {
            // Cancelling visuals must reveal secured points rather than leave the HUD behind.
            foreach (Flight flight in flights)
            {
                if (flight.image != null)
                {
                    Destroy(flight.image.gameObject);
                }
                flight.arrived(false);
            }
            flights.Clear();
            foreach (Popup popup in popups)
            {
                Destroy(popup.text.gameObject);
            }
            popups.Clear();
            milestoneQueue.Clear();
            if (milestoneRect != null) milestoneRect.gameObject.SetActive(false);
            pulseRemaining = 0f;
            pulseScale = 1f;
            if (scoreTarget != null)
            {
                scoreTarget.localScale = originalTargetScale;
            }
        }

        private void OnDestroy()
        {
            if (milestoneRect != null) Destroy(milestoneRect.gameObject);
        }
    }
}

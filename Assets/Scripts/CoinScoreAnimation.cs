using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class CoinScoreAnimation : MonoBehaviour
    {
        [SerializeField] private RectTransform effectLayer;
        [SerializeField] private RectTransform scoreTarget;
        [SerializeField] private Image flyingCoinPrefab;
        [SerializeField, Min(0.05f)] private float flightDuration = 0.5f;
        [SerializeField, Min(0f), Tooltip("Curve bend in Canvas units.")]
        private float curvature = 100f;
        [SerializeField] private AnimationCurve flightProgress = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 3f, 3f));
        [SerializeField, Range(0f, 1f)] private float arrivalScale = 0.1f;
        [Header("Counter Feedback")]
        [SerializeField, Min(0f)] private float pulseStrength = 0.15f;
        [SerializeField, Min(0.01f)] private float pulseDuration = 0.18f;
        [SerializeField, Min(0.05f)] private float popupDuration = 0.55f;
        [SerializeField] private Vector2 popupOffset = new Vector2(90f, 0f);
        [SerializeField, Min(0f)] private float popupRise = 40f;
        [SerializeField, Min(1)] private int popupFontSize = 26;
        [SerializeField] private Color popupColor = new Color(1f, 0.85f, 0.4f);

        private sealed class Flight
        {
            public Image image;
            public Sprite[] frames;
            public float fps, elapsed, spinStart, bend;
            public int points;
            public Vector2 viewportStart;
            public Action arrived;
        }

        private sealed class Popup
        {
            public Text text;
            public float elapsed;
        }

        private readonly List<Flight> flights = new();
        private readonly List<Popup> popups = new();
        private Canvas canvas;
        private Vector3 originalTargetScale;
        private float pulseRemaining;
        public bool HasFlyingCoins => flights.Count > 0;

        private void Awake()
        {
            canvas = effectLayer != null ? effectLayer.GetComponentInParent<Canvas>() : null;
            if (scoreTarget != null)
            {
                originalTargetScale = scoreTarget.localScale;
            }
        }

        public bool TryFly(Vector3 worldPosition, float worldHeight, Sprite[] frames, float fps,
            int points, Action arrived)
        {
            Camera camera = Camera.main;
            if (!isActiveAndEnabled || canvas == null || effectLayer == null || scoreTarget == null ||
                flyingCoinPrefab == null || camera == null || frames == null || frames.Length == 0)
            {
                return false;
            }

            Vector3 screen = camera.WorldToScreenPoint(worldPosition);
            Vector3 top = camera.WorldToScreenPoint(worldPosition + camera.transform.up * worldHeight);
            Image image = Instantiate(flyingCoinPrefab, effectLayer);
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = Vector2.one * Mathf.Abs(top.y - screen.y) / canvas.scaleFactor;
            image.sprite = frames[Mathf.FloorToInt(Time.time * fps) % frames.Length];
            image.rectTransform.anchoredPosition = ScreenToLocal(screen);
            flights.Add(new Flight
            {
                image = image, frames = frames, fps = fps, spinStart = Time.time,
                viewportStart = new Vector2(screen.x / Screen.width, screen.y / Screen.height),
                bend = curvature * UnityEngine.Random.Range(0.7f, 1.3f) *
                    (UnityEngine.Random.value < 0.5f ? -1f : 1f),
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
            float delta = Time.unscaledDeltaTime;
            for (int i = flights.Count - 1; i >= 0; i--)
            {
                Flight flight = flights[i];
                flight.elapsed += delta;
                float t = Mathf.Clamp01(flight.elapsed / flightDuration);
                float progress = Mathf.Clamp01(flightProgress.Evaluate(t));
                Vector2 start = ScreenToLocal(new Vector2(flight.viewportStart.x * Screen.width,
                    flight.viewportStart.y * Screen.height));
                Vector2 end = TargetPosition();
                Vector2 direction = (end - start).normalized;
                Vector2 control = (start + end) * 0.5f + new Vector2(-direction.y, direction.x) * flight.bend;
                flight.image.rectTransform.anchoredPosition = (1f - progress) * (1f - progress) * start +
                    2f * (1f - progress) * progress * control + progress * progress * end;
                flight.image.rectTransform.localScale = Vector3.one *
                    Mathf.Lerp(1f, arrivalScale, Mathf.InverseLerp(0.75f, 1f, progress));
                flight.image.sprite = flight.frames[Mathf.FloorToInt(
                    (flight.spinStart + flight.elapsed) * flight.fps) % flight.frames.Length];
                if (t >= 1f)
                {
                    flights.RemoveAt(i);
                    Destroy(flight.image.gameObject);
                    flight.arrived();
                    ShowPopup(flight.points);
                    pulseRemaining = pulseDuration;
                }
            }

            for (int i = popups.Count - 1; i >= 0; i--)
            {
                Popup popup = popups[i];
                popup.elapsed += delta;
                float t = Mathf.Clamp01(popup.elapsed / popupDuration);
                popup.text.rectTransform.anchoredPosition = TargetPosition() + popupOffset + Vector2.up * popupRise * t;
                Color color = popupColor;
                color.a *= 1f - t;
                popup.text.color = color;
                if (t >= 1f)
                {
                    Destroy(popup.text.gameObject);
                    popups.RemoveAt(i);
                }
            }

            if (scoreTarget != null)
            {
                pulseRemaining = Mathf.Max(0f, pulseRemaining - delta);
                float pulse = Mathf.Sin(Mathf.PI * pulseRemaining / pulseDuration);
                scoreTarget.localScale = originalTargetScale * (1f + pulseStrength * pulse);
            }
        }

        private void ShowPopup(int points)
        {
            Text style = scoreTarget.GetComponent<Text>();
            GameObject go = new GameObject("Coin Score Popup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(effectLayer, false);
            Text text = go.GetComponent<Text>();
            text.font = style != null ? style.font : null;
            text.fontSize = popupFontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = popupColor;
            text.text = $"+{points}";
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(160f, 60f);
            text.rectTransform.anchoredPosition = TargetPosition() + popupOffset;
            popups.Add(new Popup { text = text });
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
                flight.arrived();
            }
            flights.Clear();
            foreach (Popup popup in popups)
            {
                Destroy(popup.text.gameObject);
            }
            popups.Clear();
            pulseRemaining = 0f;
            if (scoreTarget != null)
            {
                scoreTarget.localScale = originalTargetScale;
            }
        }
    }
}

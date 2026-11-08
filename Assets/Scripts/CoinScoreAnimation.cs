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
                    pulseRemaining = visualCoinFlight.pulseDuration;
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

            if (scoreTarget != null)
            {
                pulseRemaining = Mathf.Max(0f, pulseRemaining - delta);
                float pulse = Mathf.Sin(Mathf.PI * pulseRemaining / visualCoinFlight.pulseDuration);
                scoreTarget.localScale = originalTargetScale * (1f + visualCoinFlight.pulseStrength * pulse);
            }
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
            pulseRemaining = 0f;
            if (scoreTarget != null)
            {
                scoreTarget.localScale = originalTargetScale;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jam50033
{
    public sealed class StripSceneTransition : MonoBehaviour
    {
        private static StripSceneTransition active;
        private readonly List<BaseInputModule> pausedMenus = new();
        private VisualSettings.TransitionData settings;
        private RectTransform canvasRect;
        private RectTransform[] strips;
        private float resumeTimeScale;
        private bool ownsPause;
        public static bool IsRunning => active != null;

        public static void Load(string sceneName, VisualSettings visuals)
        {
            if (active != null) return;
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Cannot load transition destination: {sceneName}");
                return;
            }

            GameObject root = new GameObject("Strip Scene Transition");
            DontDestroyOnLoad(root);
            active = root.AddComponent<StripSceneTransition>();
            active.settings = visuals != null ? visuals.sceneTransition : new VisualSettings.TransitionData();
            active.StartCoroutine(active.ChangeScene(sceneName));
        }

        private IEnumerator ChangeScene(string sceneName)
        {
            resumeTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            ownsPause = true;
            Time.timeScale = 0f;
            PauseMenus();
            CreateStrips();
            SettingsMusic outgoingMusic = FindFirstObjectByType<SettingsMusic>();
            if (outgoingMusic != null && outgoingMusic.isActiveAndEnabled) outgoingMusic.FadeOutForScene();
            yield return Wipe(false);
            // Usually the short fade finishes during the cover; respect longer Inspector tuning too.
            while (outgoingMusic != null && outgoingMusic.isActiveAndEnabled && !outgoingMusic.IsSilent)
                yield return null;

            // The overlay survives unloading and remains fully covered until the destination is ready.
            SceneManager.sceneLoaded += OnSceneLoaded;
            yield return SceneManager.LoadSceneAsync(sceneName);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            yield return null;
            yield return Wipe(true);

            // A submit press during the reveal must not become a gameplay hard drop.
            while (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
            {
                yield return null;
            }
            RestoreGameplay();
            Destroy(gameObject);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Start the destination beneath the covered screen, so it is moving throughout the reveal.
            Time.timeScale = resumeTimeScale;
            ownsPause = false;
            PauseMenus();
        }

        private void PauseMenus()
        {
            // Keep EventSystems active so destination menus can select their default button in Start.
            foreach (BaseInputModule menu in FindObjectsByType<BaseInputModule>(FindObjectsSortMode.None))
            {
                if (!menu.enabled) continue;
                pausedMenus.Add(menu);
                menu.enabled = false;
            }
        }

        private void CreateStrips()
        {
            GameObject overlay = new GameObject("Transition Canvas", typeof(RectTransform), typeof(Canvas));
            overlay.transform.SetParent(transform, false);
            Canvas canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            canvasRect = overlay.GetComponent<RectTransform>();
            int count = Mathf.Clamp(settings.stripCount, 1, 64);
            strips = new RectTransform[count];
            Color tint = settings.color;
            tint.a = 1f;
            for (int i = 0; i < count; i++)
            {
                GameObject strip = new GameObject($"Strip {i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                strip.transform.SetParent(overlay.transform, false);
                Image image = strip.GetComponent<Image>();
                image.color = tint;
                image.raycastTarget = false;
                RectTransform rect = image.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f - (i + 1f) / count);
                rect.anchorMax = new Vector2(1f, 1f - (float)i / count);
                // Small overlaps prevent seams at fractional screen heights and Canvas pixel rounding.
                rect.sizeDelta = new Vector2(2f, 2f);
                rect.anchoredPosition = Vector2.left * (Screen.width + 2f);
                strips[i] = rect;
            }
        }

        private IEnumerator Wipe(bool reveal)
        {
            float duration = Mathf.Max(0.01f, settings.duration);
            float stagger = Mathf.Max(0f, settings.stripStagger);
            float total = duration + (strips.Length - 1) * stagger;
            float elapsed = 0f;
            while (elapsed < total)
            {
                PositionStrips(elapsed, duration, stagger, reveal);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            PositionStrips(total, duration, stagger, reveal);
        }

        private void PositionStrips(float elapsed, float duration, float stagger, bool reveal)
        {
            float width = canvasRect.rect.width + 2f;
            for (int i = 0; i < strips.Length; i++)
            {
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - i * stagger) / duration));
                strips[i].anchoredPosition = Vector2.right * ((reveal ? progress : progress - 1f) * width);
            }
        }

        private void RestoreGameplay()
        {
            foreach (BaseInputModule menu in pausedMenus)
            {
                if (menu != null) menu.enabled = true;
            }
            pausedMenus.Clear();
            // Once the destination is running, preserve any pause it creates (for example, a death).
            if (ownsPause)
            {
                Time.timeScale = resumeTimeScale;
                ownsPause = false;
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (active != this) return;
            RestoreGameplay();
            active = null;
        }
    }
}

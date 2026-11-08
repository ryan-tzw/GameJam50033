using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Jam50033
{
    public sealed class AudioMenuController : MonoBehaviour
    {
        [Serializable]
        private sealed class VolumeRow
        {
            public Slider slider;
            public SlicedQueuePanel panel;
            public Text label;
            public Text value;
            public Image track;
            public Image fill;
            public Image handle;
        }

        [SerializeField] private VisualSettings visualSettings;
        [SerializeField] private AudioSettings audioSettings;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text heading;
        [SerializeField] private VolumeRow[] rows;
        [SerializeField] private Button backButton;
        [SerializeField] private Button returnButton;
        [SerializeField] private GameObject[] sourceWidgets;
        private static readonly string[] parameters = { "MasterVolume", "MusicVolume", "SFXVolume" };
        private static readonly string[] labels = { "MASTER", "MUSIC", "SFX" };
        private const string preferencePrefix = "Neverland.Audio.";
        private static AudioMenuController active;
        private bool[] sourceStates;
        private bool open, opening, closing;
        private float nextAdjustmentSoundTime;
        private AudioMixer Mixer => audioSettings.musicMixer.audioMixer;
        public static bool IsOpen => active != null && active.open;

        private void Awake() => panel.SetActive(false);

        private void Start()
        {
            Mixer.updateMode = AudioMixerUpdateMode.UnscaledTime;
            for (int i = 0; i < rows.Length; i++)
            {
                int channel = i;
                // The existing mixer levels are the defaults; preferences do not change clip balancing.
                Mixer.GetFloat(parameters[i], out float decibels);
                float initial = PlayerPrefs.GetFloat(preferencePrefix + parameters[i], Mathf.Pow(10f, decibels / 20f));
                rows[i].slider.SetValueWithoutNotify(Mathf.Clamp01(initial));
                ApplyVolume(i, rows[i].slider.value, false);
                rows[i].slider.onValueChanged.AddListener(value => ApplyVolume(channel, value, true));
            }
        }

        public void Open()
        {
            if (IsOpen || ControlsMenuController.IsOpen || StripSceneTransition.IsRunning) return;
            sourceStates = new bool[sourceWidgets.Length];
            for (int i = 0; i < sourceWidgets.Length; i++)
            {
                sourceStates[i] = sourceWidgets[i].activeSelf;
                sourceWidgets[i].SetActive(false);
            }
            active = this;
            open = opening = true;
            closing = false;
            nextAdjustmentSoundTime = 0f;
        }

        public void Back()
        {
            if (open) closing = true;
        }

        private void Update()
        {
            if (!open) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Back();
        }

        private void LateUpdate()
        {
            if (returnButton != null) StyleButton(returnButton);
            if (!open) return;
            ApplyStyle();
            if (opening)
            {
                opening = false;
                panel.SetActive(true);
                rows[0].slider.Select();
            }
            // Restore focus after input processing; a Back submit cannot reopen AUDIO in the same frame.
            if (!closing || (Keyboard.current != null &&
                (Keyboard.current.spaceKey.isPressed || Keyboard.current.escapeKey.isPressed))) return;
            Close();
        }

        private void ApplyVolume(int index, float value, bool save)
        {
            value = Mathf.Clamp01(value);
            Mixer.SetFloat(parameters[index], value <= 0f ? -80f : Mathf.Log10(value) * 20f);
            rows[index].value.text = Mathf.RoundToInt(value * 100f) + "%";
            if (save)
            {
                PlayerPrefs.SetFloat(preferencePrefix + parameters[index], value);
                // Apply the mixer first so the preview reflects the newly selected level.
                if (open && Time.unscaledTime >= nextAdjustmentSoundTime)
                {
                    MenuAudio.PlaySelect();
                    nextAdjustmentSoundTime = Time.unscaledTime + audioSettings.menu.adjustmentSoundInterval;
                }
            }
        }

        private void ApplyStyle()
        {
            VisualSettings.AudioMenuData style = visualSettings.audioMenu;
            VisualSettings.MenuButtonData button = visualSettings.menuButtons;
            VisualSettings.PauseMenuData menu = visualSettings.pauseMenu;
            panel.GetComponent<Image>().color = menu.overlayColor;
            heading.font = menu.headingFont;
            heading.fontSize = menu.headingSize;
            heading.color = menu.headingColor;
            heading.rectTransform.anchoredPosition = style.headingPosition;
            for (int i = 0; i < rows.Length; i++)
            {
                VolumeRow row = rows[i];
                var rect = (RectTransform)row.slider.transform;
                rect.sizeDelta = style.rowSize;
                rect.anchoredPosition = new Vector2(0f, style.firstRowY - i * style.rowSpacing);
                row.slider.colors = button.colors;
                Color fill = button.panelColor;
                fill.a = button.panelOpacity;
                row.panel.Configure(fill, button.outlineColor, button.outlineWidth, button.cornerRadius, 0f,
                    visualSettings.nextPreview.panelFillSprite, visualSettings.nextPreview.panelOutlineSprite);
                StyleText(row.label, style.fontSize);
                StyleText(row.value, style.fontSize);
                row.label.text = labels[i];
                row.track.color = style.trackColor;
                row.fill.color = style.fillColor;
                row.handle.color = button.textColor;
            }
            backButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, style.backButtonY);
            StyleButton(backButton);
        }

        private void StyleText(Text text, int size)
        {
            VisualSettings.MenuButtonData style = visualSettings.menuButtons;
            text.font = style.font;
            text.fontSize = size;
            text.fontStyle = style.fontStyle;
            text.color = style.textColor;
        }

        private void StyleButton(Button button)
        {
            MenuButtonStyle style = button.GetComponent<MenuButtonStyle>();
            StyleText(button.GetComponentInChildren<Text>(), style != null && style.IsIconButton
                ? visualSettings.pauseMenu.captionFontSize : visualSettings.menuButtons.fontSize);
            button.colors = visualSettings.menuButtons.colors;
        }

        private void Close()
        {
            if (!open) return;
            open = opening = closing = false;
            if (active == this) active = null;
            panel.SetActive(false);
            for (int i = 0; i < sourceWidgets.Length; i++)
                if (sourceWidgets[i] != null) sourceWidgets[i].SetActive(sourceStates[i]);
            PlayerPrefs.Save();
            if (returnButton != null && returnButton.isActiveAndEnabled) returnButton.Select();
        }

        private void OnDisable() => Close();
    }
}

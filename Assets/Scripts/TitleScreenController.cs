using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class TitleScreenController : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        [SerializeField] private AudioSettings audioSettings;
        [SerializeField] private Button startButton;
        [SerializeField] private Button[] iconButtons = System.Array.Empty<Button>();
        [SerializeField] private Image[] icons = System.Array.Empty<Image>();
        [SerializeField] private string gameSceneName = "GameScene";

        private bool loading;

        private void Awake()
        {
            MenuAudio.Initialize(audioSettings);
        }

        private void Start()
        {
            if (startButton != null)
            {
                startButton.Select();
            }
        }

        public void StartGame()
        {
            if (loading)
            {
                return;
            }

            loading = true;
            MenuAudio.PlayGameStart(audioSettings);
            startButton.interactable = false;
            StartCoroutine(LoadGame());
        }

        private void LateUpdate()
        {
            var style = visualSettings.pauseMenu;
            for (int i = 0; i < iconButtons.Length; i++)
            {
                Button button = iconButtons[i];
                button.GetComponent<RectTransform>().anchoredPosition = visualSettings.titleMenu.iconRowPosition +
                    Vector2.right * ((i - (iconButtons.Length - 1) * 0.5f) * (style.iconButtonSize + style.iconButtonGap));
                Text caption = button.GetComponentInChildren<Text>();
                caption.fontSize = style.captionFontSize;
                float height = style.captionFontSize * 1.8f;
                caption.rectTransform.anchorMin = caption.rectTransform.anchorMax = caption.rectTransform.pivot = Vector2.one * 0.5f;
                caption.rectTransform.sizeDelta = new Vector2(style.iconButtonSize + style.iconButtonGap, height);
                caption.rectTransform.anchoredPosition = Vector2.down * (style.iconButtonSize * 0.5f + style.captionGap + height * 0.5f);
                icons[i].sprite = i == 0 ? style.audioIcon : style.controlsIcon;
                icons[i].color = style.iconColor;
                icons[i].preserveAspect = true;
                icons[i].rectTransform.sizeDelta = Vector2.one * style.iconSize;
            }
        }

        private IEnumerator LoadGame()
        {
            // Release the menu's Space press before gameplay can interpret it as a hard drop.
            yield return null;
            while (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
            {
                yield return null;
            }

            StripSceneTransition.Load(gameSceneName, visualSettings);
        }
    }
}

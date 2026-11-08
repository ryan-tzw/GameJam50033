using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Jam50033
{
    [DefaultExecutionOrder(-200)]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        [SerializeField] private AudioSettings audioSettings;
        [SerializeField] private GameOverController gameOver;
        [SerializeField] private SettingsMusic music;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text heading;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        private static PauseMenuController active;
        private bool paused, opening, leaving;
        private float previousTimeScale;
        private Button[] buttons;
        public static bool IsPaused => active != null && active.paused;

        private void Awake()
        {
            buttons = new[] { resumeButton, restartButton, mainMenuButton };
            panel.SetActive(false);
            MenuAudio.Initialize(audioSettings);
        }

        private void Update()
        {
            if (StripSceneTransition.IsRunning || leaving || (gameOver != null && gameOver.IsGameOver)) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (paused) Resume();
                else Pause();
            }
        }

        public void Pause()
        {
            if (paused || leaving || Time.timeScale <= 0f || StripSceneTransition.IsRunning ||
                (gameOver != null && gameOver.IsGameOver)) return;
            previousTimeScale = Time.timeScale;
            paused = true;
            active = this;
            Time.timeScale = 0f;
            music?.SetPaused(true);
            opening = true;
        }

        private void LateUpdate()
        {
            if (!paused) return;
            VisualSettings.PauseMenuData style = visualSettings.pauseMenu;
            panel.GetComponent<Image>().color = style.overlayColor;
            heading.text = "PAUSED";
            heading.font = style.headingFont;
            heading.fontSize = style.headingSize;
            heading.color = style.headingColor;
            heading.rectTransform.anchoredPosition = style.headingPosition;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, style.firstButtonY - i * style.buttonSpacing);
                Text text = buttons[i].GetComponentInChildren<Text>();
                text.font = visualSettings.menuButtons.font;
                text.fontSize = visualSettings.menuButtons.fontSize;
                text.fontStyle = visualSettings.menuButtons.fontStyle;
                text.color = visualSettings.menuButtons.textColor;
                buttons[i].colors = visualSettings.menuButtons.colors;
            }
            if (!opening) return;
            // Focus after the UI update so a gameplay submit cannot activate the newly opened menu.
            opening = false;
            panel.SetActive(true);
            resumeButton.Select();
        }

        public void Resume()
        {
            if (!paused || leaving) return;
            leaving = true;
            StartCoroutine(ResumeAfterRelease());
        }

        private IEnumerator ResumeAfterRelease()
        {
            // Space selects Resume, but must never hard-drop a piece on the same press.
            while (Keyboard.current != null && (Keyboard.current.spaceKey.isPressed || Keyboard.current.escapeKey.isPressed))
                yield return null;
            Close();
        }

        private void Close()
        {
            if (!paused) return;
            paused = opening = leaving = false;
            if (active == this) active = null;
            panel.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            music?.SetPaused(false);
            Time.timeScale = previousTimeScale;
        }

        public void RestartGame() => LoadScene(gameObject.scene.path);
        public void ReturnToMainMenu() => LoadScene("TitleScene");

        private void LoadScene(string scene)
        {
            if (!paused || leaving) return;
            leaving = true;
            resumeButton.interactable = restartButton.interactable = mainMenuButton.interactable = false;
            // Keep the old world frozen until the wipe unloads it.
            StripSceneTransition.Load(scene, visualSettings);
        }

        private void OnDisable() => Close();
    }
}

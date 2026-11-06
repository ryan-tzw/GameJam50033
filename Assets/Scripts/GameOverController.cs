using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class GameOverController : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField, Min(0f), Tooltip("Extra distance below the camera, in world units, before the run ends.")]
        private float fallMargin = 0.25f;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private string titleSceneName = "TitleScene";

        private Collider2D playerCollider;
        private bool gameOver;
        private bool loading;
        private float previousTimeScale;
        private bool menuPending;

        public bool IsGameOver => gameOver;

        private void Awake()
        {
            playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
            gameOverPanel.SetActive(false);
        }

        private void LateUpdate()
        {
            if (menuPending)
            {
                menuPending = false;
                OpenMenu();
            }

            if (gameOver || player == null || playerCollider == null || gameplayCamera == null)
            {
                return;
            }

            float depth = gameplayCamera.WorldToViewportPoint(player.transform.position).z;
            float screenBottom = gameplayCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, depth)).y;
            if (playerCollider.bounds.max.y >= screenBottom - fallMargin)
            {
                return;
            }

            PauseGameplay();
            OpenMenu();
        }

        public bool BeginFatalHit()
        {
            if (gameOver)
            {
                return false;
            }

            PauseGameplay();
            return true;
        }

        public void FinishFatalHit()
        {
            if (gameOver)
            {
                menuPending = true;
            }
        }

        private void PauseGameplay()
        {
            gameOver = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        private void OpenMenu()
        {
            // Open after this frame's UI update, so a gameplay Space press cannot submit the new menu.
            gameOverPanel.SetActive(true);
            restartButton.Select();
        }

        public void RestartGame()
        {
            BeginSceneLoad(gameObject.scene.path);
        }

        public void ReturnToMainMenu()
        {
            BeginSceneLoad(titleSceneName);
        }

        private void BeginSceneLoad(string sceneName)
        {
            if (!gameOver || loading)
            {
                return;
            }

            loading = true;
            restartButton.interactable = false;
            mainMenuButton.interactable = false;
            StartCoroutine(LoadScene(sceneName));
        }

        private IEnumerator LoadScene(string sceneName)
        {
            yield return null;
            while (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
            {
                yield return null;
            }

            yield return SceneManager.LoadSceneAsync(sceneName);
        }

        private void OnDestroy()
        {
            // Keep gameplay paused until its scene unloads, then restore time for the destination scene.
            if (gameOver)
            {
                Time.timeScale = previousTimeScale;
            }
        }
    }
}

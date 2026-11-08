using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class GameOverController : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        [SerializeField] private PlayerSettings playerSettings;
        private static readonly PlayerSettings.FallDeathData defaultPlayerFallDeath = new();
        private PlayerSettings.FallDeathData playerFallDeath => playerSettings != null ? playerSettings.fallDeath : defaultPlayerFallDeath;
        [SerializeField] private PlayerController player;
        [SerializeField] private RunScore runScore;
        [SerializeField] private Camera gameplayCamera;

        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private string titleSceneName = "TitleScene";

        private Collider2D playerCollider;
        private bool gameOver;
        private bool loading;
        private float previousTimeScale;
        private bool menuPending;
        private PlayerDeathEffect deathEffect;
        private bool deathEffectStarted;

        public bool IsGameOver => gameOver;

        private void Awake()
        {
            playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
            deathEffect = player != null ? player.GetComponent<PlayerDeathEffect>() : null;
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
            if (playerCollider.bounds.max.y >= screenBottom - playerFallDeath.fallMargin)
            {
                return;
            }

            PauseGameplay();
            player.StopForFatalHit();
            BeginDeathEffect();
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
                BeginDeathEffect();
            }
        }

        private void BeginDeathEffect()
        {
            if (deathEffectStarted)
            {
                return;
            }

            deathEffectStarted = true;
            StartCoroutine(ShowMenuAfterDeath());
        }

        private IEnumerator ShowMenuAfterDeath()
        {
            if (deathEffect != null)
            {
                yield return deathEffect.Play();
            }

            // Flights keep using unscaled time while gameplay and the final score are frozen.
            while (runScore != null && runScore.HasFlyingCoins)
            {
                yield return null;
            }

            menuPending = true;
        }

        private void PauseGameplay()
        {
            if (runScore != null)
            {
                runScore.FinishRun();
            }
            gameOver = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        private void OpenMenu()
        {
            // Open after this frame's UI update, so a gameplay Space press cannot submit the new menu.
            gameOverPanel.SetActive(true);
            if (runScore != null)
            {
                runScore.FinishRun();
            }
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

            StripSceneTransition.Load(sceneName, visualSettings);
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

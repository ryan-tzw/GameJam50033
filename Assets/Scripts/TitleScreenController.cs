using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class TitleScreenController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private string gameSceneName = "GameScene";

        private bool loading;

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
            startButton.interactable = false;
            StartCoroutine(LoadGame());
        }

        private IEnumerator LoadGame()
        {
            // Release the menu's Space press before gameplay can interpret it as a hard drop.
            yield return null;
            while (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
            {
                yield return null;
            }

            yield return SceneManager.LoadSceneAsync(gameSceneName);
        }
    }
}

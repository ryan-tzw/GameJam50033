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

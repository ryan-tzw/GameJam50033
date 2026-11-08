using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Jam50033
{
    public sealed class MenuAudio : MonoBehaviour
    {
        private static MenuAudio instance;
        private AudioSettings settings;
        private AudioSource source;
        private GameObject previousSelection;

        public static void Initialize(AudioSettings audioSettings)
        {
            if (audioSettings == null) return;
            if (instance == null)
            {
                // Keep UI playback alive across the scene wipe, including the title's start sound.
                GameObject root = new GameObject("Menu Audio");
                DontDestroyOnLoad(root);
                instance = root.AddComponent<MenuAudio>();
                instance.source = root.AddComponent<AudioSource>();
                instance.source.playOnAwake = false;
                instance.source.spatialBlend = 0f;
                instance.source.ignoreListenerPause = true;
            }
            instance.settings = audioSettings;
            // Include closed submenus, and avoid duplicate listeners when several menus initialize.
            foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                button.onClick.RemoveListener(instance.PlayButtonPress);
                button.onClick.AddListener(instance.PlayButtonPress);
            }
        }

        public static void PlayGameStart(AudioSettings audioSettings)
        {
            Initialize(audioSettings);
            if (audioSettings == null) return;
            instance.Play(audioSettings.menu.gameStartClip, audioSettings.menu.gameStartVolume);
        }

        public static void PlaySelect()
        {
            if (instance != null && instance.settings != null)
                instance.Play(instance.settings.menu.selectClip, instance.settings.menu.selectVolume);
        }

        private void LateUpdate()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            Keyboard keyboard = Keyboard.current;
            bool navigating = keyboard != null && (keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed ||
                keyboard.wKey.isPressed || keyboard.sKey.isPressed || keyboard.leftArrowKey.isPressed ||
                keyboard.rightArrowKey.isPressed || keyboard.aKey.isPressed || keyboard.dKey.isPressed);
            // Initial focus and attempts to move past the last item stay silent. Held-key repeats still count.
            if (selected != previousSelection && previousSelection != null && selected != null &&
                selected.activeInHierarchy && selected.GetComponent<Selectable>() != null && navigating && settings != null)
            {
                PlaySelect();
            }
            previousSelection = selected;
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip == null || volume <= 0f) return;
            source.outputAudioMixerGroup = settings.effectsMixer;
            source.PlayOneShot(clip, volume);
        }

        private void PlayButtonPress()
        {
            if (settings != null) Play(settings.menu.pressClip, settings.menu.pressVolume);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}

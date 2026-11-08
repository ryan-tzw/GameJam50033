using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SettingsMusic : MonoBehaviour
    {
        [SerializeField] private AudioSettings audioSettings;
        [SerializeField] private bool titleScreen;
        private AudioSource source;
        private bool gameOver;
        private bool paused;

        private void OnEnable()
        {
            source = GetComponent<AudioSource>();
            ApplySettings();
            if (Application.IsPlaying(gameObject) && source.clip != null && !source.isPlaying)
            {
                source.Play();
            }
        }

        private void Update()
        {
            ApplySettings();
        }

        private void ApplySettings()
        {
            if (audioSettings == null || source == null) return;

            AudioClip clip = gameOver ? audioSettings.gameOverMusic :
                titleScreen ? audioSettings.titleMusic : audioSettings.gameMusic;
            bool changed = source.clip != clip;
            if (changed) source.clip = clip;
            source.volume = gameOver ? audioSettings.gameOverMusicVolume :
                titleScreen ? audioSettings.titleMusicVolume : audioSettings.gameMusicVolume;
            if (paused) source.volume *= audioSettings.pausedMusicMultiplier;
            source.outputAudioMixerGroup = audioSettings.musicMixer;
            source.loop = true;
            source.playOnAwake = false;
            if (changed && Application.IsPlaying(gameObject) && clip != null)
            {
                source.Play();
            }
        }

        public void PlayGameOver()
        {
            if (!Application.IsPlaying(gameObject) || titleScreen || gameOver || audioSettings == null) return;
            gameOver = true;
            ApplySettings();
        }

        public void SetPaused(bool value)
        {
            paused = value;
            ApplySettings();
        }

        private void OnDisable()
        {
            if (source != null) source.Stop();
            gameOver = false;
            paused = false;
        }
    }
}

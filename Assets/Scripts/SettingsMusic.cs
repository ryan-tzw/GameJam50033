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
        private bool playingGameOver;
        private bool exitingScene;
        private float fadeGain = 1f;

        public bool IsSilent => fadeGain <= 0f;
        private AudioClip RequestedClip => gameOver ? audioSettings.gameOverMusic :
            titleScreen ? audioSettings.titleMusic : audioSettings.gameMusic;

        private void OnEnable()
        {
            source = GetComponent<AudioSource>();
            exitingScene = false;
            playingGameOver = gameOver;
            fadeGain = Application.IsPlaying(gameObject) && audioSettings != null && audioSettings.musicFadeInDuration > 0f ? 0f : 1f;
            if (audioSettings != null) source.clip = RequestedClip;
            ApplySettings();
            if (Application.IsPlaying(gameObject) && source.clip != null && !source.isPlaying)
            {
                PlayFromStartOffset();
            }
        }

        private void Update()
        {
            if (audioSettings == null || source == null) return;
            AudioClip requested = RequestedClip;
            if (!Application.IsPlaying(gameObject))
            {
                source.clip = requested;
                playingGameOver = gameOver;
                fadeGain = 1f;
            }
            else
            {
                bool changingTrack = source.clip != requested;
                if (!changingTrack && !exitingScene) playingGameOver = gameOver;
                float target = exitingScene || changingTrack ? 0f : 1f;
                float duration = target == 0f ? audioSettings.musicFadeOutDuration : audioSettings.musicFadeInDuration;
                fadeGain = duration <= 0f ? target : Mathf.MoveTowards(fadeGain, target, Time.unscaledDeltaTime / duration);
                if (changingTrack && !exitingScene && fadeGain <= 0f)
                {
                    // Switch only at silence, preserving the outgoing track's own level during its fade.
                    source.clip = requested;
                    playingGameOver = gameOver;
                    if (audioSettings.musicFadeInDuration <= 0f) fadeGain = 1f;
                    ApplySettings();
                    if (requested != null) PlayFromStartOffset();
                    else source.Stop();
                }
            }
            ApplySettings();
        }

        private void ApplySettings()
        {
            if (audioSettings == null || source == null) return;

            source.volume = playingGameOver ? audioSettings.gameOverMusicVolume :
                titleScreen ? audioSettings.titleMusicVolume : audioSettings.gameMusicVolume;
            if (paused) source.volume *= audioSettings.pausedMusicMultiplier;
            source.volume *= fadeGain;
            source.outputAudioMixerGroup = audioSettings.musicMixer;
            source.loop = true;
            source.playOnAwake = false;
        }

        private void PlayFromStartOffset()
        {
            float offset = audioSettings == null ? 0f : playingGameOver ? audioSettings.gameOverMusicStartOffset :
                titleScreen ? audioSettings.titleMusicStartOffset : audioSettings.gameMusicStartOffset;
            AudioClip clip = source.clip;
            // Seek only when starting a track; native looping and pause-volume changes never reapply it.
            float seconds = Mathf.Clamp(offset, 0f, Mathf.Max(0f, clip.length - 1f / clip.frequency));
            source.timeSamples = Mathf.Clamp(Mathf.FloorToInt(seconds * clip.frequency), 0, Mathf.Max(0, clip.samples - 1));
            source.Play();
        }

        public void PlayGameOver()
        {
            if (!Application.IsPlaying(gameObject) || titleScreen || gameOver || audioSettings == null) return;
            gameOver = true;
        }

        public void FadeOutForScene() => exitingScene = true;

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
            exitingScene = false;
        }
    }
}

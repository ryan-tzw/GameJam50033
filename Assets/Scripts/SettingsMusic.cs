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

            AudioClip clip = titleScreen ? audioSettings.titleMusic : audioSettings.gameMusic;
            bool changed = source.clip != clip;
            if (changed) source.clip = clip;
            source.volume = titleScreen ? audioSettings.titleMusicVolume : audioSettings.gameMusicVolume;
            source.outputAudioMixerGroup = audioSettings.musicMixer;
            source.loop = true;
            source.playOnAwake = false;
            if (changed && Application.IsPlaying(gameObject) && clip != null)
            {
                source.Play();
            }
        }
    }
}

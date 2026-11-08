using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class TetrominoAudio : MonoBehaviour
    {
        [SerializeField] private AudioSettings audioSettings;
        private static readonly AudioSettings.TetrominoData defaultAudioTetromino = new();
        private AudioSettings.TetrominoData audioTetromino => audioSettings != null ? audioSettings.tetromino : defaultAudioTetromino;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        public void PlayMove() => Play(audioTetromino.moveClip, audioTetromino.moveVolume);
        public void PlayRotate() => Play(audioTetromino.rotateClip, audioTetromino.rotateVolume);
        public void PlaySoftDrop() => Play(audioTetromino.softDropClip, audioTetromino.softDropVolume);
        public void PlayHardDrop() => Play(audioTetromino.hardDropClip, audioTetromino.hardDropVolume);
        public void PlayReject() => Play(audioTetromino.rejectClip, audioTetromino.rejectVolume);
        public void PlayHold() => Play(audioTetromino.holdClip, audioTetromino.holdVolume);
        public void PlayNoHold() => Play(audioTetromino.noHoldClip, audioTetromino.noHoldVolume);
        public void PlayConnect()
        {
            if (audioTetromino.connectClips != null && audioTetromino.connectClips.Length > 0)
            {
                Play(audioTetromino.connectClips[Random.Range(0, audioTetromino.connectClips.Length)], audioTetromino.connectVolume);
            }
        }

        private void Play(AudioClip clip, float volume)
        {
            if (isActiveAndEnabled && source != null && clip != null)
            {
                // The scene owns this source so a discarded piece cannot cut its sound short.
                if (audioSettings != null) source.outputAudioMixerGroup = audioSettings.effectsMixer;
                source.PlayOneShot(clip, volume);
            }
        }
    }
}

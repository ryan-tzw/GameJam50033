using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class TetrominoAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip moveClip;
        [SerializeField, Range(0f, 1f)] private float moveVolume = 0.7f;
        [SerializeField] private AudioClip rotateClip;
        [SerializeField, Range(0f, 1f)] private float rotateVolume = 0.7f;
        [SerializeField] private AudioClip softDropClip;
        [SerializeField, Range(0f, 1f)] private float softDropVolume = 0.5f;
        [SerializeField] private AudioClip hardDropClip;
        [SerializeField, Range(0f, 1f)] private float hardDropVolume = 0.8f;
        [SerializeField] private AudioClip[] connectClips = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] private float connectVolume = 0.8f;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        public void PlayMove() => Play(moveClip, moveVolume);
        public void PlayRotate() => Play(rotateClip, rotateVolume);
        public void PlaySoftDrop() => Play(softDropClip, softDropVolume);
        public void PlayHardDrop() => Play(hardDropClip, hardDropVolume);
        public void PlayConnect()
        {
            if (connectClips != null && connectClips.Length > 0)
            {
                Play(connectClips[Random.Range(0, connectClips.Length)], connectVolume);
            }
        }

        private void Play(AudioClip clip, float volume)
        {
            if (isActiveAndEnabled && source != null && clip != null)
            {
                // The scene owns this source so a discarded piece cannot cut its sound short.
                source.PlayOneShot(clip, volume);
            }
        }
    }
}

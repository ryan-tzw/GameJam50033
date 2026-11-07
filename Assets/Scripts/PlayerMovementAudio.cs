using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerMovementAudio : MonoBehaviour
    {
        [SerializeField] private AudioSettings audioSettings;
        private static readonly AudioSettings.PlayerData defaultAudioPlayer = new();
        private AudioSettings.PlayerData audioPlayer => audioSettings != null ? audioSettings.player : defaultAudioPlayer;
        [SerializeField] private PlayerController player;
        [SerializeField] private AudioSource audioSource;

        private Animator animator;
        private float nextStepTime;
        private float nextClimbTime;
        private static readonly int Running = Animator.StringToHash("Running");
        private static readonly int Climbing = Animator.StringToHash("Climbing");
        private static readonly int ClimbPlayback = Animator.StringToHash("ClimbPlayback");

        private void OnEnable()
        {
            animator = GetComponent<Animator>();
            nextStepTime = 0f;
            nextClimbTime = 0f;
            if (player != null)
            {
                player.Landed += PlayLanding;
            }
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.Landed -= PlayLanding;
            }
        }

        private void LateUpdate()
        {
            if (Time.deltaTime <= 0f)
            {
                return;
            }

            PlayStep();
            PlayClimb();
        }

        public void PlayStep()
        {
            if (player != null && player.IsGrounded && player.State == PlayerController.MovementState.Running &&
                animator.GetBool(Running) && Time.time >= nextStepTime)
            {
                Play(audioPlayer.stepSound, audioPlayer.stepVolume);
                nextStepTime = Time.time + Mathf.Max(0.01f, audioPlayer.stepInterval);
            }
        }

        public void PlayClimb()
        {
            if (player != null && player.State == PlayerController.MovementState.Climbing &&
                animator.GetBool(Climbing) && animator.GetFloat(ClimbPlayback) > 0f && Time.time >= nextClimbTime)
            {
                Play(audioPlayer.climbSound, audioPlayer.climbVolume);
                nextClimbTime = Time.time + Mathf.Max(0.01f, audioPlayer.climbInterval);
            }
        }

        private void PlayLanding()
        {
            Play(audioPlayer.landingSound, audioPlayer.landingVolume);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (Time.timeScale > 0f && player != null && player.isActiveAndEnabled &&
                audioSource != null && clip != null)
            {
                if (audioSettings != null) audioSource.outputAudioMixerGroup = audioSettings.effectsMixer;
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}

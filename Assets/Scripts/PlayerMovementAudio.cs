using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerMovementAudio : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip stepSound;
        [SerializeField, Range(0f, 1f)] private float stepVolume = 0.3f;
        [SerializeField, Min(0.01f), Tooltip("Seconds between running footstep sounds. Larger values play less often.")]
        private float stepInterval = 0.33333334f;
        [SerializeField] private AudioClip climbSound;
        [SerializeField, Range(0f, 1f)] private float climbVolume = 0.5f;
        [SerializeField, Min(0.01f), Tooltip("Seconds between climbing sounds while moving. Larger values play less often.")]
        private float climbInterval = 0.375f;
        [SerializeField] private AudioClip landingSound;
        [SerializeField, Range(0f, 1f)] private float landingVolume = 0.7f;

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
                Play(stepSound, stepVolume);
                nextStepTime = Time.time + Mathf.Max(0.01f, stepInterval);
            }
        }

        public void PlayClimb()
        {
            if (player != null && player.State == PlayerController.MovementState.Climbing &&
                animator.GetBool(Climbing) && animator.GetFloat(ClimbPlayback) > 0f && Time.time >= nextClimbTime)
            {
                Play(climbSound, climbVolume);
                nextClimbTime = Time.time + Mathf.Max(0.01f, climbInterval);
            }
        }

        private void PlayLanding()
        {
            Play(landingSound, landingVolume);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (Time.timeScale > 0f && player != null && player.isActiveAndEnabled &&
                audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}

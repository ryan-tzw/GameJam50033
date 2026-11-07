using System.Collections;
using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerHitReaction : MonoBehaviour
    {
        [SerializeField] private PlayerSettings playerSettings;
        [SerializeField] private AudioSettings audioSettings;
        private static readonly PlayerSettings.HurtData defaultPlayerHurt = new();
        private PlayerSettings.HurtData playerHurt => playerSettings != null ? playerSettings.hurt : defaultPlayerHurt;
        private static readonly AudioSettings.PlayerData defaultAudioPlayer = new();
        private AudioSettings.PlayerData audioPlayer => audioSettings != null ? audioSettings.player : defaultAudioPlayer;
        [SerializeField] private GameOverController gameOver;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Animator animator;

        [Header("Audio")]
        [SerializeField, Tooltip("Scene-owned SFX source so playback continues after the player is disabled.")]
        private AudioSource audioSource;

        private Material originalMaterial;
        private MaterialPropertyBlock originalProperties;
        private MaterialPropertyBlock flashProperties;
        private Vector3 originalPosition;
        private AnimatorUpdateMode originalUpdateMode;
        private float originalSpeed;
        private bool reacting;
        private Rigidbody2D body;
        private float originalGravity;
        private RigidbodyInterpolation2D originalInterpolation;
        private bool knockbackActive;
        private static readonly int Hurt = Animator.StringToHash("Hurt");
        private static readonly int HurtState = Animator.StringToHash("Base Layer.Hurt");
        private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

        public bool IsReacting => reacting;

        public void Hit(HazardBrickVisuals hazard)
        {
            if (!isActiveAndEnabled || reacting || gameOver == null || visual == null || animator == null ||
                playerHurt.hurtClip == null || playerHurt.flashMaterial == null || !gameOver.BeginFatalHit())
            {
                return;
            }

            GetComponent<PlayerController>().StopForFatalHit();
            if (audioSource != null && audioPlayer.hitSound != null)
            {
                if (audioSettings != null) audioSource.outputAudioMixerGroup = audioSettings.effectsMixer;
                audioSource.PlayOneShot(audioPlayer.hitSound, audioPlayer.hitVolume);
            }
            hazard.FlashOnHit();
            Vector2 direction = GetComponent<Collider2D>().bounds.center -
                hazard.GetComponent<Collider2D>().bounds.center;
            direction = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector2.left;
            StartCoroutine(PlayHurt(direction));
        }

        private IEnumerator PlayHurt(Vector2 direction)
        {
            reacting = true;
            body = GetComponent<Rigidbody2D>();
            originalGravity = body.gravityScale;
            originalInterpolation = body.interpolation;
            knockbackActive = true;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.None;
            Vector2 startPosition = body.position;
            StageArea stage = GetComponent<PlayerController>().StageArea;
            float cellSize = stage != null ? stage.CellSize : GetComponent<Collider2D>().bounds.size.y * 0.5f;
            Vector2 endPosition = startPosition + direction * (playerHurt.knockbackDistance * cellSize);
            originalMaterial = visual.sharedMaterial;
            originalPosition = visual.transform.localPosition;
            originalProperties = new MaterialPropertyBlock();
            flashProperties = new MaterialPropertyBlock();
            visual.GetPropertyBlock(originalProperties);
            visual.GetPropertyBlock(flashProperties);
            originalUpdateMode = animator.updateMode;
            originalSpeed = animator.speed;
            visual.sharedMaterial = playerHurt.flashMaterial;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.speed = playerHurt.hurtClip.length / Mathf.Max(0.01f, playerHurt.hurtDuration);
            animator.SetBool(Hurt, true);
            animator.Play(HurtState, 0, 0f);
            animator.Update(0f);

            float height = GetComponent<Collider2D>().bounds.size.y;
            float elapsed = 0f;
            while (elapsed < playerHurt.hurtDuration)
            {
                // Gameplay physics is paused during death, so move explicitly using unscaled time.
                float progress = Mathf.Clamp01(elapsed / Mathf.Min(playerHurt.knockbackDuration, playerHurt.hurtDuration));
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                Vector2 position = Vector2.Lerp(startPosition, endPosition, eased);
                body.position = position;
                transform.position = new Vector3(position.x, position.y, transform.position.z);
                float flash = Mathf.Clamp01(1f - elapsed / Mathf.Max(0.01f, playerHurt.whiteFlashDuration));
                flashProperties.SetFloat(FlashAmount, flash);
                visual.SetPropertyBlock(flashProperties);
                float phase = elapsed * playerHurt.shakeFrequency * Mathf.PI * 2f;
                float strength = playerHurt.shakeStrength * height * (1f - elapsed / playerHurt.hurtDuration);
                Vector3 offset = new Vector3(Mathf.Sin(phase), Mathf.Sin(phase * 1.3f), 0f) * strength;
                // Shake only the authored Visual child; the collider and bridge stay still.
                visual.transform.localPosition = originalPosition + visual.transform.parent.InverseTransformVector(offset);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            body.position = endPosition;
            transform.position = new Vector3(endPosition.x, endPosition.y, transform.position.z);
            Physics2D.SyncTransforms();
            RestoreVisuals();
            gameOver.FinishFatalHit();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            RestoreVisuals();
            if (knockbackActive && body != null)
            {
                body.gravityScale = originalGravity;
                body.interpolation = originalInterpolation;
            }
            knockbackActive = false;
        }

        private void RestoreVisuals()
        {
            if (!reacting)
            {
                return;
            }

            if (visual != null)
            {
                visual.sharedMaterial = originalMaterial;
                visual.SetPropertyBlock(originalProperties);
                visual.transform.localPosition = originalPosition;
            }

            if (animator != null)
            {
                animator.updateMode = originalUpdateMode;
                animator.speed = originalSpeed;
            }

            reacting = false;
        }
    }
}

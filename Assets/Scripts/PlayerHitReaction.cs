using System.Collections;
using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerHitReaction : MonoBehaviour
    {
        [SerializeField] private GameOverController gameOver;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip hurtClip;
        [SerializeField] private Material flashMaterial;
        [Header("Hurt Reaction")]
        [SerializeField, Min(0.01f), Tooltip("How long to hold the hit pose before finishing the reaction.")]
        private float hurtDuration = 0.375f;
        [SerializeField, Min(0.01f)] private float whiteFlashDuration = 0.1f;
        [SerializeField, Min(0f), Tooltip("Maximum sprite displacement as a fraction of the player's collider height.")]
        private float shakeStrength = 0.025f;
        [SerializeField, Min(0f), Tooltip("Shake cycles per second.")]
        private float shakeFrequency = 25f;
        [Header("Knockback")]
        [SerializeField, Min(0f), Tooltip("Distance away from the hazard, in stage cells.")]
        private float knockbackDistance = 1.5f;
        [SerializeField, Min(0.01f), Tooltip("Seconds to complete knockback, capped by the hurt reaction duration.")]
        private float knockbackDuration = 0.15f;

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
                hurtClip == null || flashMaterial == null || !gameOver.BeginFatalHit())
            {
                return;
            }

            GetComponent<PlayerController>().StopForFatalHit();
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
            Vector2 endPosition = startPosition + direction * (knockbackDistance * cellSize);
            originalMaterial = visual.sharedMaterial;
            originalPosition = visual.transform.localPosition;
            originalProperties = new MaterialPropertyBlock();
            flashProperties = new MaterialPropertyBlock();
            visual.GetPropertyBlock(originalProperties);
            visual.GetPropertyBlock(flashProperties);
            originalUpdateMode = animator.updateMode;
            originalSpeed = animator.speed;
            visual.sharedMaterial = flashMaterial;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.speed = hurtClip.length / Mathf.Max(0.01f, hurtDuration);
            animator.SetBool(Hurt, true);
            animator.Play(HurtState, 0, 0f);
            animator.Update(0f);

            float height = GetComponent<Collider2D>().bounds.size.y;
            float elapsed = 0f;
            while (elapsed < hurtDuration)
            {
                // Gameplay physics is paused during death, so move explicitly using unscaled time.
                float progress = Mathf.Clamp01(elapsed / Mathf.Min(knockbackDuration, hurtDuration));
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                Vector2 position = Vector2.Lerp(startPosition, endPosition, eased);
                body.position = position;
                transform.position = new Vector3(position.x, position.y, transform.position.z);
                float flash = Mathf.Clamp01(1f - elapsed / Mathf.Max(0.01f, whiteFlashDuration));
                flashProperties.SetFloat(FlashAmount, flash);
                visual.SetPropertyBlock(flashProperties);
                float phase = elapsed * shakeFrequency * Mathf.PI * 2f;
                float strength = shakeStrength * height * (1f - elapsed / hurtDuration);
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

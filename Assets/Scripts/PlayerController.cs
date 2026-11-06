using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class AdventurerController : MonoBehaviour
    {
        [SerializeField] private StageArea stageArea;
        [SerializeField, Min(0.1f)] private float heightInCells = 2f;
        [SerializeField, Min(0.1f)] private float widthInCells = 0.8f;
        [SerializeField, Tooltip("Desired horizontal world position. The bridge scrolls beneath this position.")]
        private float targetX;
        [SerializeField, Min(0f), Tooltip("Horizontal recovery velocity per unit of displacement.")]
        private float followStrength = 6f;
        [SerializeField, Min(0f), Tooltip("Maximum horizontal recovery speed, in stage cells per second.")]
        private float maxRecoverySpeed = 4f;
        [SerializeField, Min(0.001f), Tooltip("Distance below the capsule to check for support, in cells.")]
        private float groundProbeDistance = 0.08f;
        [SerializeField] private LayerMask groundLayers = ~0;
        [Header("Visuals")]
        [SerializeField, Tooltip("Automatically sized and aligned parent. Edit the Visual child for sprite adjustments.")]
        private Transform visualRoot;
        [SerializeField] private Sprite standingSprite;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Animator animator;

        private Rigidbody2D body;
        private CapsuleCollider2D capsule;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private static readonly int GroundedParameter = Animator.StringToHash("Grounded");
        private static readonly int RunningParameter = Animator.StringToHash("Running");

        public bool IsGrounded { get; private set; }

        private void OnEnable()
        {
            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            FitToStage();
        }

        private void FixedUpdate()
        {
            if (!Application.IsPlaying(gameObject) || stageArea == null || stageArea.CellSize <= 0f)
            {
                return;
            }

            float size = stageArea.CellSize;
            float speedLimit = maxRecoverySpeed * size;
            // zero displacement means zero forward velocity; recovery only follows a displacement.
            float velocityX = Mathf.Clamp((targetX - body.position.x) * followStrength, -speedLimit, speedLimit);
            body.linearVelocity = new Vector2(velocityX, body.linearVelocity.y);

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;
            int count = capsule.Cast(Vector2.down, filter, groundHits, groundProbeDistance * size);
            IsGrounded = false;
            for (int i = 0; i < count; i++)
            {
                // side contacts must not turn a wall into standing ground.
                if (groundHits[i].normal.y > 0.65f && body.linearVelocity.y <= 0.1f)
                {
                    IsGrounded = true;
                    break;
                }
            }
        }

        private void Update()
        {
            if (!Application.IsPlaying(gameObject) || animator == null || stageArea == null)
            {
                return;
            }

            bool running = Mathf.Abs(stageArea.BridgeFrameDisplacement) > 0.00001f ||
                Mathf.Abs(body.linearVelocity.x) > 0.01f;
            animator.SetBool(GroundedParameter, IsGrounded);
            animator.SetBool(RunningParameter, running);
        }

        private void LateUpdate()
        {
            FitToStage();
        }

        private void FitToStage()
        {
            if (stageArea == null || capsule == null || visualRoot == null || visual == null || standingSprite == null ||
                stageArea.CellSize <= 0f)
            {
                return;
            }

            float size = stageArea.CellSize;
            float height = heightInCells * size;
            Vector2 colliderSize = new Vector2(Mathf.Min(widthInCells, heightInCells) * size, height);
            if (capsule.size != colliderSize)
            {
                capsule.size = colliderSize;
                capsule.offset = Vector2.up * (height * 0.5f);
            }

            float scale = height / standingSprite.bounds.size.y;
            visualRoot.localScale = Vector3.one * scale;
            if (visual.sprite != null)
            {
                // align frames on the automatic parent, preserving authored Visual adjustments.
                visualRoot.localPosition = new Vector3(
                    -visual.sprite.bounds.center.x * scale, -visual.sprite.bounds.min.y * scale, 0f);
            }
        }

        private void OnValidate()
        {
            heightInCells = Mathf.Max(0.1f, heightInCells);
            widthInCells = Mathf.Clamp(widthInCells, 0.1f, heightInCells);
            followStrength = Mathf.Max(0f, followStrength);
            maxRecoverySpeed = Mathf.Max(0f, maxRecoverySpeed);
            groundProbeDistance = Mathf.Max(0.001f, groundProbeDistance);
            FitToStage();
        }
    }
}

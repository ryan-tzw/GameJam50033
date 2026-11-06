using System.Collections.Generic;
using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        public enum MovementState { Running, Climbing, Falling, Staggered }

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
        [Header("Climbing")]
        [SerializeField, Min(0.1f), Tooltip("Upward speed while the upper half touches the wall, in stage cells per second.")]
        private float climbSpeed = 2.5f;
        [SerializeField, Min(0.1f), Tooltip("Upward speed when only the lower half touches the wall, in stage cells per second.")]
        private float stepSpeed = 4f;
        [SerializeField, Range(0.01f, 0.5f), Tooltip("Distance ahead to detect a climbable wall, in cells.")]
        private float wallProbeDistance = 0.15f;
        [SerializeField] private MovementState movementState = MovementState.Falling;
        [Header("Landing Recovery")]
        [SerializeField, Min(0.1f), Tooltip("Minimum free-fall distance that causes a landing stagger, in stage cells.")]
        private float staggerFallDistance = 3f;
        [SerializeField, Min(0.01f), Tooltip("Seconds spent recovering while moving with the bridge.")]
        private float staggerDuration = 0.375f;
        [SerializeField] private AnimationClip staggerClip;
        [Header("Visuals")]
        [SerializeField, Tooltip("Automatically sized and aligned parent. Edit the Visual child for sprite adjustments.")]
        private Transform visualRoot;
        [SerializeField] private Sprite standingSprite;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Animator animator;

        private Rigidbody2D body;
        private CapsuleCollider2D capsule;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private readonly RaycastHit2D[] wallHits = new RaycastHit2D[8];
        private readonly HashSet<Vector2Int> bridgeCells = new();
        private Vector2Int climbAnchor;
        private float normalGravity;
        private bool climbBlocked;
        private bool fatalHit;
        private float fallStartY;
        private float staggerRemaining;
        private static readonly int GroundedParameter = Animator.StringToHash("Grounded");
        private static readonly int RunningParameter = Animator.StringToHash("Running");
        private static readonly int ClimbingParameter = Animator.StringToHash("Climbing");
        private static readonly int ClimbPlaybackParameter = Animator.StringToHash("ClimbPlayback");
        private static readonly int ClimbState = Animator.StringToHash("Base Layer.Climb");
        private static readonly int StaggeredParameter = Animator.StringToHash("Staggered");
        private static readonly int StaggerPlaybackParameter = Animator.StringToHash("StaggerPlayback");
        private static readonly int StaggerState = Animator.StringToHash("Base Layer.Stagger");

        public bool IsGrounded { get; private set; }
        public MovementState State => movementState;

        private void OnEnable()
        {
            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            normalGravity = body.gravityScale;
            movementState = MovementState.Falling;
            climbBlocked = false;
            fatalHit = false;
            fallStartY = body.position.y;
            staggerRemaining = 0f;
            if (Application.IsPlaying(gameObject) && animator != null)
            {
                animator.SetBool(StaggeredParameter, false);
            }
            FitToStage();
        }

        private void OnDisable()
        {
            if (body != null && movementState == MovementState.Climbing)
            {
                body.gravityScale = normalGravity;
            }
        }

        private void FixedUpdate()
        {
            if (fatalHit || !Application.IsPlaying(gameObject) || stageArea == null || stageArea.CellSize <= 0f)
            {
                return;
            }

            float size = stageArea.CellSize;
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

            if (movementState == MovementState.Staggered)
            {
                staggerRemaining -= Time.fixedDeltaTime;
                if (IsGrounded && staggerRemaining > 0f)
                {
                    MoveWithBridge();
                    return;
                }

                // Losing support ends recovery immediately and starts a fresh fall.
                fallStartY = body.position.y;
                movementState = IsGrounded ? MovementState.Running : MovementState.Falling;
            }

            if (movementState == MovementState.Falling)
            {
                fallStartY = Mathf.Max(fallStartY, body.position.y);
                if (IsGrounded && fallStartY - body.position.y >= staggerFallDistance * size)
                {
                    movementState = MovementState.Staggered;
                    staggerRemaining = staggerDuration;
                    if (animator != null)
                    {
                        animator.SetBool(StaggeredParameter, true);
                        animator.SetFloat(StaggerPlaybackParameter,
                            staggerClip != null ? staggerClip.length / staggerDuration : 1f);
                        animator.Play(StaggerState, 0, 0f);
                    }

                    MoveWithBridge();
                    return;
                }
            }

            RefreshBridgeCells();
            if (movementState != MovementState.Climbing)
            {
                TryStartClimbing(filter, size);
            }

            if (movementState == MovementState.Climbing)
            {
                // Climbing and stepping must not accumulate distance toward a landing stagger.
                fallStartY = body.position.y;
                if (UpdateClimbing(filter, size))
                {
                    return;
                }
            }

            if (IsGrounded || movementState != MovementState.Falling)
            {
                fallStartY = body.position.y;
            }
            movementState = IsGrounded ? MovementState.Running : MovementState.Falling;
            float speedLimit = maxRecoverySpeed * size;
            // zero displacement means zero forward velocity; recovery only follows a displacement.
            float velocityX = Mathf.Clamp((targetX - body.position.x) * followStrength, -speedLimit, speedLimit);
            body.linearVelocity = new Vector2(velocityX, body.linearVelocity.y);
        }

        private void MoveWithBridge()
        {
            body.linearVelocity = new Vector2(stageArea.BridgeVelocity, body.linearVelocity.y);
        }

        private void RefreshBridgeCells()
        {
            bridgeCells.Clear();
            foreach (Brick brick in Brick.ActiveBricks)
            {
                if (brick == null || brick.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                Tetromino owner = brick.GetComponentInParent<Tetromino>();
                if ((owner == null && brick.StageArea == stageArea) ||
                    (owner != null && owner.IsPlaced && owner.StageArea == stageArea))
                {
                    bridgeCells.Add(stageArea.WorldPositionToCell(brick.transform.position));
                }
            }
        }

        private void TryStartClimbing(ContactFilter2D filter, float size)
        {
            int count = capsule.Cast(Vector2.right, filter, wallHits, wallProbeDistance * size);
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = wallHits[i];
                Brick brick = hit.collider.GetComponent<Brick>();
                if (brick == null || hit.normal.x > -0.3f)
                {
                    continue;
                }

                Vector2Int cell = stageArea.WorldPositionToCell(brick.transform.position);
                if (!bridgeCells.Contains(cell))
                {
                    continue;
                }

                climbAnchor = cell;
                movementState = MovementState.Climbing;
                body.gravityScale = 0f;
                return;
            }
        }

        private bool UpdateClimbing(ContactFilter2D filter, float size)
        {
            if (!bridgeCells.Contains(climbAnchor))
            {
                EndClimbing();
                return false;
            }

            // Keep the original wall segment: a gap must not promote us to a disconnected upper tile.
            Vector2Int topCell = climbAnchor;
            while (bridgeCells.Contains(topCell + Vector2Int.up))
            {
                topCell += Vector2Int.up;
            }

            Vector3 wallTop = stageArea.CellToWorldPosition(topCell);
            float skin = size * 0.01f;
            float feetTargetY = wallTop.y + size * 0.5f + skin;
            bool canStand = HasStandingClearance(topCell, size);
            if (!canStand)
            {
                // Stop with the head below the break, rather than lifting the body into the gap.
                feetTargetY -= capsule.bounds.size.y;
            }

            // Once supported on top, recover toward targetX instead of chasing the scrolling wall's centre.
            if (canStand && IsGrounded && capsule.bounds.min.y >= wallTop.y + size * 0.5f - skin)
            {
                EndClimbing();
                return false;
            }

            bool aboveTop = canStand && capsule.bounds.min.y >= feetTargetY - skin * 0.5f;
            float movementSpeed = HasUpperBodyContact() ? climbSpeed : stepSpeed;
            float desiredX = aboveTop ? wallTop.x : wallTop.x - size * 0.5f - capsule.bounds.extents.x - skin;
            float horizontalLimit = aboveTop ? Mathf.Max(maxRecoverySpeed, movementSpeed) * size : float.PositiveInfinity;
            float velocityX = Mathf.Clamp((desiredX - body.position.x) / Time.fixedDeltaTime,
                -horizontalLimit, horizontalLimit);
            float velocityY = Mathf.Clamp((feetTargetY - capsule.bounds.min.y) / Time.fixedDeltaTime,
                0f, movementSpeed * size);

            Vector2 velocity = new Vector2(velocityX, velocityY);
            float distance = velocity.magnitude * Time.fixedDeltaTime;
            if (distance > 0f)
            {
                Vector2 direction = velocity.normalized;
                int count = capsule.Cast(direction, filter, wallHits, distance + skin);
                for (int i = 0; i < count; i++)
                {
                    if (Vector2.Dot(wallHits[i].normal, direction) < -0.1f)
                    {
                        distance = Mathf.Min(distance, Mathf.Max(0f, wallHits[i].distance - skin));
                    }
                }

                velocity = direction * (distance / Time.fixedDeltaTime);
            }

            bool needsVerticalMovement = feetTargetY - capsule.bounds.min.y > skin * 0.5f;
            bool needsHorizontalMovement = aboveTop && Mathf.Abs(desiredX - body.position.x) > skin * 2f;
            climbBlocked = (!canStand && !needsVerticalMovement) ||
                (needsVerticalMovement && velocity.y < size * 0.001f) ||
                (needsHorizontalMovement && Mathf.Abs(velocity.x) < size * 0.001f);
            body.linearVelocity = velocity;
            return true;
        }

        private bool HasStandingClearance(Vector2Int topCell, float size)
        {
            float halfWidth = capsule.bounds.extents.x / size;
            int firstColumn = Mathf.FloorToInt(topCell.x + 0.5f - halfWidth + 0.0001f);
            int lastColumn = Mathf.CeilToInt(topCell.x + 0.5f + halfWidth - 0.0001f) - 1;
            int rows = Mathf.CeilToInt(capsule.bounds.size.y / size - 0.0001f);
            for (int y = 1; y <= rows; y++)
            {
                for (int x = firstColumn; x <= lastColumn; x++)
                {
                    if (bridgeCells.Contains(new Vector2Int(x, topCell.y + y)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void EndClimbing()
        {
            climbBlocked = false;
            body.gravityScale = normalGravity;
            body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
            movementState = IsGrounded ? MovementState.Running : MovementState.Falling;
        }

        private void Update()
        {
            if (fatalHit || !Application.IsPlaying(gameObject) || animator == null || stageArea == null)
            {
                return;
            }

            bool running = Mathf.Abs(stageArea.BridgeFrameDisplacement) > 0.00001f ||
                Mathf.Abs(body.linearVelocity.x) > 0.01f;
            bool climbing = movementState == MovementState.Climbing;
            bool showClimb = climbing && (climbBlocked || HasUpperBodyContact());

            // Low steps still use climb movement, with a running pose instead of grabbing empty space.
            animator.SetBool(GroundedParameter, IsGrounded || (climbing && !showClimb));
            animator.SetBool(RunningParameter, running || climbing);
            animator.SetBool(ClimbingParameter, showClimb);
            animator.SetFloat(ClimbPlaybackParameter, climbBlocked ? 0f : 1f);
            animator.SetBool(StaggeredParameter, movementState == MovementState.Staggered);
            if (climbing && climbBlocked)
            {
                animator.Play(ClimbState, 0, 0f);
            }
        }

        private bool HasUpperBodyContact()
        {
            Bounds bounds = capsule.bounds;
            float size = stageArea.CellSize;
            float halfCell = size * 0.5f;
            float reach = wallProbeDistance * size;
            // Cover the entire upper half, including the clearance maintained beside the wall.
            foreach (Vector2Int cell in bridgeCells)
            {
                Vector3 center = stageArea.CellToWorldPosition(cell);
                if (center.y + halfCell > bounds.center.y + size * 0.0001f &&
                    center.y - halfCell < bounds.max.y &&
                    center.x + halfCell >= bounds.min.x &&
                    center.x - halfCell <= bounds.max.x + reach)
                {
                    return true;
                }
            }

            return false;
        }

        private void LateUpdate()
        {
            FitToStage();
        }

        public void StopForFatalHit()
        {
            fatalHit = true;
            body.linearVelocity = Vector2.zero;
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
            climbSpeed = Mathf.Max(0.1f, climbSpeed);
            stepSpeed = Mathf.Max(0.1f, stepSpeed);
            wallProbeDistance = Mathf.Clamp(wallProbeDistance, 0.01f, 0.5f);
            staggerFallDistance = Mathf.Max(0.1f, staggerFallDistance);
            staggerDuration = Mathf.Max(0.01f, staggerDuration);
            FitToStage();
        }
    }
}

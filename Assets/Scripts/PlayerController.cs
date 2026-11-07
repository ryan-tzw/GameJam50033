using System.Collections.Generic;
using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerSettings playerSettings;
        private static readonly PlayerSettings.MovementData defaultPlayerMovement = new();
        private PlayerSettings.MovementData playerMovement => playerSettings != null ? playerSettings.movement : defaultPlayerMovement;
        public enum MovementState { Running, Climbing, Falling, Staggered }

        [SerializeField] private StageArea stageArea;

        [SerializeField] private MovementState movementState = MovementState.Falling;

        [SerializeField, Range(0f, 1f)] private float momentum;

        [Header("Visuals")]
        [SerializeField, Tooltip("Automatically sized and aligned parent. Edit the Visual child for sprite adjustments.")]
        private Transform visualRoot;

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
        private bool fastRunReady;
        private static readonly int FastRunningParameter = Animator.StringToHash("FastRunning");
        private static readonly int GroundedParameter = Animator.StringToHash("Grounded");
        private static readonly int RunningParameter = Animator.StringToHash("Running");
        private static readonly int ClimbingParameter = Animator.StringToHash("Climbing");
        private static readonly int ClimbPlaybackParameter = Animator.StringToHash("ClimbPlayback");
        private static readonly int ClimbState = Animator.StringToHash("Base Layer.Climb");
        private static readonly int StaggeredParameter = Animator.StringToHash("Staggered");
        private static readonly int StaggerPlaybackParameter = Animator.StringToHash("StaggerPlayback");
        private static readonly int StaggerState = Animator.StringToHash("Base Layer.Stagger");

        public bool IsGrounded { get; private set; }
        public event System.Action Landed;
        public MovementState State => movementState;
        public StageArea StageArea => stageArea;
        public float Momentum => momentum;
        public bool IsFastRunning => !fatalHit && IsGrounded && movementState == MovementState.Running && fastRunReady;
        public float MomentumSpeedMultiplier => !fatalHit && IsGrounded && movementState == MovementState.Running
            ? 1f + momentum * playerMovement.maximumMomentumSpeedBonus : 1f;

        private void OnEnable()
        {
            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            normalGravity = body.gravityScale;
            movementState = MovementState.Falling;
            IsGrounded = false;
            climbBlocked = false;
            fatalHit = false;
            fallStartY = body.position.y;
            staggerRemaining = 0f;
            momentum = playerMovement.initialMomentum;
            fastRunReady = false;
            if (Application.IsPlaying(gameObject) && animator != null)
            {
                animator.SetBool(StaggeredParameter, false);
            }
            FitToStage();
        }

        private void OnDisable()
        {
            momentum = 0f;
            fastRunReady = false;
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
            filter.SetLayerMask(playerMovement.groundLayers);
            filter.useTriggers = false;
            int count = capsule.Cast(Vector2.down, filter, groundHits, playerMovement.groundProbeDistance * size);
            bool wasGrounded = IsGrounded;
            IsGrounded = false;
            for (int i = 0; i < count; i++)
            {
                // Floor contact remains valid during small upward corrections at brick seams.
                // Side contacts must not turn a wall into standing ground.
                if (groundHits[i].normal.y > 0.65f)
                {
                    IsGrounded = true;
                    break;
                }
            }

            if (IsGrounded && !wasGrounded && movementState == MovementState.Falling)
            {
                Landed?.Invoke();
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
                if (IsGrounded && fallStartY - body.position.y >= playerMovement.staggerFallDistance * size)
                {
                    movementState = MovementState.Staggered;
                    momentum = Mathf.Max(0f, momentum - playerMovement.landingMomentumPenalty);
                    staggerRemaining = playerMovement.staggerDuration;
                    if (animator != null)
                    {
                        animator.SetBool(StaggeredParameter, true);
                        animator.SetFloat(StaggerPlaybackParameter,
                            playerMovement.staggerClip != null ? playerMovement.staggerClip.length / playerMovement.staggerDuration : 1f);
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
            if (movementState == MovementState.Falling)
            {
                // Air resistance removes forward motion relative to the scrolling bridge.
                float driftVelocity = Mathf.MoveTowards(body.linearVelocity.x, stageArea.BridgeVelocity,
                    playerMovement.airDriftAcceleration * Time.fixedDeltaTime);
                body.linearVelocity = new Vector2(driftVelocity, body.linearVelocity.y);
                return;
            }

            float speedLimit = playerMovement.maxRecoverySpeed * size;
            // zero displacement means zero forward velocity; recovery only follows a displacement.
            float velocityX = Mathf.Clamp((playerMovement.targetX - body.position.x) * playerMovement.followStrength, -speedLimit, speedLimit);
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
            int count = capsule.Cast(Vector2.right, filter, wallHits, playerMovement.wallProbeDistance * size);
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

            // Follow the wall upward, bridging a single missing tile when allowed.
            Vector2Int topCell = climbAnchor;
            while (true)
            {
                if (bridgeCells.Contains(topCell + Vector2Int.up))
                {
                    topCell += Vector2Int.up;
                }
                else if (playerMovement.climbGapAllowance > 0 && bridgeCells.Contains(topCell + Vector2Int.up * 2))
                {
                    topCell += Vector2Int.up * 2;
                }
                else
                {
                    break;
                }
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

            // Once supported on top, recover toward playerMovement.targetX instead of chasing the scrolling wall's centre.
            if (canStand && IsGrounded && capsule.bounds.min.y >= wallTop.y + size * 0.5f - skin)
            {
                EndClimbing();
                return false;
            }

            bool aboveTop = canStand && capsule.bounds.min.y >= feetTargetY - skin * 0.5f;
            float movementSpeed = HasUpperBodyContact() ? playerMovement.climbSpeed : playerMovement.stepSpeed;
            float desiredX = aboveTop ? wallTop.x : wallTop.x - size * 0.5f - capsule.bounds.extents.x - skin;
            float horizontalLimit = aboveTop ? Mathf.Max(playerMovement.maxRecoverySpeed, movementSpeed) * size : float.PositiveInfinity;
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
            if (Application.IsPlaying(gameObject) && !fatalHit && stageArea != null)
            {
                UpdateMomentum(Time.deltaTime);
                UpdateRunningAnimationSpeed();
            }

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
            animator.SetBool(FastRunningParameter, IsFastRunning);
            if (climbing && climbBlocked)
            {
                animator.Play(ClimbState, 0, 0f);
            }
        }

        private void UpdateMomentum(float deltaTime)
        {
            bool running = movementState == MovementState.Running && IsGrounded &&
                Mathf.Abs(stageArea.BridgeVelocity) > 0.00001f;
            float rate = running ? 1f / Mathf.Max(0.1f, playerMovement.momentumBuildTime) :
                movementState == MovementState.Climbing ? -playerMovement.climbMomentumDecay :
                movementState == MovementState.Staggered ? -playerMovement.staggerMomentumDecay :
                movementState == MovementState.Falling ? -playerMovement.fallMomentumDecay : 0f;
            momentum = Mathf.Clamp01(momentum + rate * deltaTime);
        }

        private void UpdateRunningAnimationSpeed()
        {
            if (!IsGrounded || movementState != MovementState.Running || stageArea.CellSize <= 0f)
            {
                fastRunReady = false;
                return;
            }

            float speed = Mathf.Abs(stageArea.BridgeVelocity) / stageArea.CellSize;
            if (speed >= playerMovement.fastRunStartSpeed)
            {
                fastRunReady = true;
            }
            else if (speed < playerMovement.fastRunStopSpeed)
            {
                fastRunReady = false;
            }
        }

        private bool HasUpperBodyContact()
        {
            Bounds bounds = capsule.bounds;
            float size = stageArea.CellSize;
            float halfCell = size * 0.5f;
            float reach = playerMovement.wallProbeDistance * size;
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
            momentum = 0f;
            fastRunReady = false;
            body.linearVelocity = Vector2.zero;
        }

        private void FitToStage()
        {
            if (stageArea == null || capsule == null || visualRoot == null || visual == null || playerMovement.standingSprite == null ||
                stageArea.CellSize <= 0f)
            {
                return;
            }

            float size = stageArea.CellSize;
            float height = playerMovement.heightInCells * size;
            float colliderHeight = Mathf.Max(Mathf.Min(playerMovement.widthInCells, playerMovement.heightInCells), playerMovement.heightInCells - playerMovement.headClearance) * size;
            Vector2 colliderSize = new Vector2(Mathf.Min(playerMovement.widthInCells, playerMovement.heightInCells) * size, colliderHeight);
            if (capsule.size != colliderSize)
            {
                capsule.size = colliderSize;
            }
            capsule.offset = Vector2.up * (colliderHeight * 0.5f);

            float scale = height / playerMovement.standingSprite.bounds.size.y;
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
            FitToStage();
        }
    }
}

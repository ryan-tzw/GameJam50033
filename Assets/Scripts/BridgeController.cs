using UnityEngine;

namespace Jam50033
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BridgeController : MonoBehaviour
    {
        [SerializeField] private GameplaySettings gameplaySettings;
        [SerializeField] private VisualSettings visualSettings;
        private static readonly GameplaySettings.BridgeData defaultGameplayBridge = new();
        private GameplaySettings.BridgeData gameplayBridge => gameplaySettings != null ? gameplaySettings.bridge : defaultGameplayBridge;
        private static readonly VisualSettings.BridgeGizmosData defaultVisualBridgeGizmos = new();
        private VisualSettings.BridgeGizmosData visualBridgeGizmos => visualSettings != null ? visualSettings.bridgeGizmos : defaultVisualBridgeGizmos;
        [SerializeField] private StageArea stageArea;
        [SerializeField] private PlayerController player;
        [SerializeField] private RunDifficulty difficulty;

        private float distanceInCells;
        private float currentSpeed;
        private float speedVelocity;

        public float Offset => transform.localPosition.x;
        public float ScrollSpeed => currentSpeed;
        public float FrameDisplacement { get; private set; }

        private void Awake()
        {
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            currentSpeed = gameplayBridge.baseSpeed;
        }

        private void Update()
        {
            if (stageArea == null || stageArea.CellSize <= 0f || Time.deltaTime <= 0f)
            {
                FrameDisplacement = 0f;
                return;
            }

            float multiplier = difficulty != null ? difficulty.SpeedMultiplier : 1f;
            multiplier *= player != null && player.isActiveAndEnabled ? player.MomentumSpeedMultiplier : 1f;
            float targetSpeed = GetTargetSpeed() * multiplier;
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity,
                gameplayBridge.smoothingTime, Mathf.Infinity, Time.deltaTime);
            currentSpeed = Mathf.Clamp(currentSpeed, gameplayBridge.baseSpeed * multiplier, Mathf.Max(gameplayBridge.baseSpeed, gameplayBridge.maxSpeed) * multiplier);
            distanceInCells += currentSpeed * Time.deltaTime;
            float nextOffset = -distanceInCells * stageArea.CellSize;
            FrameDisplacement = nextOffset - Offset;
            transform.localPosition = new Vector3(nextOffset, 0f, 0f);
        }

        private float GetTargetSpeed()
        {
            // Traversal needs its normal pace while the player climbs, steps, or falls.
            if (player == null || !player.isActiveAndEnabled || !player.IsGrounded ||
                player.State != PlayerController.MovementState.Running)
            {
                return gameplayBridge.baseSpeed;
            }

            float blend = Mathf.InverseLerp(gameplayBridge.lowerLeadThreshold, gameplayBridge.upperLeadThreshold, GetBridgeRightEdgeX());
            return Mathf.Lerp(gameplayBridge.baseSpeed, Mathf.Max(gameplayBridge.baseSpeed, gameplayBridge.maxSpeed), blend);
        }

        private float GetBridgeRightEdgeX()
        {
            float rightmostX = float.NegativeInfinity;
            float size = stageArea.CellSize;
            foreach (Brick brick in Brick.ActiveBricks)
            {
                if (brick == null || brick.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                Tetromino owner = brick.GetComponentInParent<Tetromino>();
                // Floating obstacles must not count as bridge built ahead by the player.
                if (owner != null && owner.GetComponent<NaturalStructure>() != null)
                {
                    continue;
                }

                if ((owner == null && brick.StageArea != stageArea) ||
                    (owner != null && (!owner.IsPlaced || owner.StageArea != stageArea)))
                {
                    continue;
                }

                Vector3 position = owner == null ? stageArea.CellToWorldPosition(brick.GetGridCell()) : brick.transform.position;
                float tileRight = stageArea.transform.InverseTransformPoint(position).x + size * 0.5f;
                rightmostX = Mathf.Max(rightmostX, tileRight);
            }

            return rightmostX;
        }

        private void OnDrawGizmosSelected()
        {
            if (stageArea == null || stageArea.CellSize <= 0f)
            {
                return;
            }

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = stageArea.transform.localToWorldMatrix;
            DrawThresholdLine(gameplayBridge.lowerLeadThreshold, visualBridgeGizmos.lowerThresholdColor);
            DrawThresholdLine(gameplayBridge.upperLeadThreshold, visualBridgeGizmos.upperThresholdColor);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        private void DrawThresholdLine(float x, Color color)
        {
            float halfHeight = stageArea.Rows * stageArea.CellSize * 0.5f;
            Gizmos.color = color;
            Gizmos.DrawLine(new Vector3(x, -halfHeight, 0f), new Vector3(x, halfHeight, 0f));
        }

    }
}

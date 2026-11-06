using UnityEngine;
using UnityEngine.Serialization;

namespace Jam50033
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BridgeController : MonoBehaviour
    {
        [SerializeField] private StageArea stageArea;
        [SerializeField] private PlayerController player;
        [SerializeField] private RunDifficulty difficulty;
        [FormerlySerializedAs("bridgeSpeed")]
        [SerializeField, Min(0f), Tooltip("Normal leftward speed in stage cells per second.")]
        private float baseSpeed = 0.5f;
        [SerializeField, Min(0f), Tooltip("Maximum leftward speed when the bridge extends far ahead, in cells per second.")]
        private float maxSpeed = 3f;
        [SerializeField, Min(0f), Tooltip("Bridge lead in cells where acceleration starts, measured ahead of the player.")]
        private float lowerLeadThreshold = 8f;
        [SerializeField, Min(0f), Tooltip("Bridge lead in cells where the target speed reaches maximum.")]
        private float upperLeadThreshold = 16f;
        [SerializeField, Min(0.01f), Tooltip("Approximate time in seconds to approach the target scrolling speed.")]
        private float smoothingTime = 1f;
        [Header("Threshold Gizmos")]
        [SerializeField] private Color lowerThresholdColor = new Color(1f, 0.85f, 0.1f, 0.8f);
        [SerializeField] private Color upperThresholdColor = new Color(1f, 0.25f, 0.2f, 0.8f);

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
            currentSpeed = baseSpeed;
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
                smoothingTime, Mathf.Infinity, Time.deltaTime);
            currentSpeed = Mathf.Clamp(currentSpeed, baseSpeed * multiplier, Mathf.Max(baseSpeed, maxSpeed) * multiplier);
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
                return baseSpeed;
            }

            float blend = Mathf.InverseLerp(lowerLeadThreshold, upperLeadThreshold, GetBridgeLead());
            return Mathf.Lerp(baseSpeed, Mathf.Max(baseSpeed, maxSpeed), blend);
        }

        private float GetBridgeLead()
        {
            float playerX = stageArea.transform.InverseTransformPoint(player.transform.position).x;
            float rightmostX = playerX;
            float size = stageArea.CellSize;
            foreach (Brick brick in Brick.ActiveBricks)
            {
                if (brick == null || brick.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                Tetromino owner = brick.GetComponentInParent<Tetromino>();
                if ((owner == null && brick.StageArea != stageArea) ||
                    (owner != null && (!owner.IsPlaced || owner.StageArea != stageArea)))
                {
                    continue;
                }

                Vector3 position = owner == null ? stageArea.CellToWorldPosition(brick.GetGridCell()) : brick.transform.position;
                float tileRight = stageArea.transform.InverseTransformPoint(position).x + size * 0.5f;
                rightmostX = Mathf.Max(rightmostX, tileRight);
            }

            return (rightmostX - playerX) / size;
        }

        private void OnDrawGizmosSelected()
        {
            if (stageArea == null || player == null || stageArea.CellSize <= 0f)
            {
                return;
            }

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = stageArea.transform.localToWorldMatrix;
            DrawThresholdLine(lowerLeadThreshold, lowerThresholdColor);
            DrawThresholdLine(upperLeadThreshold, upperThresholdColor);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        private void DrawThresholdLine(float lead, Color color)
        {
            float playerX = stageArea.transform.InverseTransformPoint(player.transform.position).x;
            float x = playerX + lead * stageArea.CellSize;
            float halfHeight = stageArea.Rows * stageArea.CellSize * 0.5f;
            Gizmos.color = color;
            Gizmos.DrawLine(new Vector3(x, -halfHeight, 0f), new Vector3(x, halfHeight, 0f));
        }

        private void OnValidate()
        {
            baseSpeed = Mathf.Max(0f, baseSpeed);
            maxSpeed = Mathf.Max(baseSpeed, maxSpeed);
            lowerLeadThreshold = Mathf.Max(0f, lowerLeadThreshold);
            upperLeadThreshold = Mathf.Max(lowerLeadThreshold + 0.01f, upperLeadThreshold);
            smoothingTime = Mathf.Max(0.01f, smoothingTime);
        }
    }
}

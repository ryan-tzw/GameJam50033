using UnityEngine;
using UnityEngine.InputSystem;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class Tetromino : MonoBehaviour
    {
        public enum Shape { I, O, T, S, Z, J, L }

        [SerializeField] private Shape shape = Shape.T;
        [SerializeField, Min(0f), Tooltip("Horizontal movement in stage cells per second.")]
        private float moveSpeed = 2f;
        [SerializeField, Min(1f), Tooltip("Speed multiplier while A is held.")]
        private float speedUpMultiplier = 3f;
        [SerializeField] private StageArea stageArea;
        [SerializeField] private Brick[] bricks;

        private int rotationSteps;

        private static readonly Vector2Int[][] ShapeCells =
        {
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) },
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            new[] { new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0) },
            new[] { new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) },
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) }
        };

        private void OnEnable()
        {
            UpdateLayout();
        }

        private void Update()
        {
            if (Application.IsPlaying(gameObject))
            {
                UpdateMovement(Time.deltaTime);
            }

            UpdateLayout();
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            speedUpMultiplier = Mathf.Max(1f, speedUpMultiplier);
            UpdateLayout();
        }

        private void UpdateMovement(float deltaTime)
        {
            if (stageArea == null || stageArea.CellSize <= 0f)
            {
                return;
            }

            float cellSize = stageArea.CellSize;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.wasPressedThisFrame)
                {
                    TryRotate(-1);
                }

                if (keyboard.eKey.wasPressedThisFrame)
                {
                    TryRotate(1);
                }
            }

            Vector2Int firstCell = GetRotatedCell(0, rotationSteps);
            int minimumX = firstCell.x;
            int minimumY = firstCell.y;
            int maximumY = firstCell.y;
            for (int i = 1; i < 4; i++)
            {
                Vector2Int cell = GetRotatedCell(i, rotationSteps);
                minimumX = Mathf.Min(minimumX, cell.x);
                minimumY = Mathf.Min(minimumY, cell.y);
                maximumY = Mathf.Max(maximumY, cell.y);
            }

            int firstRow = -minimumY;
            int lastRow = stageArea.Rows - 1 - maximumY;
            if (firstRow > lastRow)
            {
                return;
            }

            Vector3 position = stageArea.transform.InverseTransformPoint(transform.position);
            int row = Mathf.RoundToInt(position.y / cellSize + stageArea.Rows * 0.5f - 0.5f);
            if (keyboard != null)
            {
                if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                {
                    row++;
                }

                if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                {
                    row--;
                }
            }

            row = Mathf.Clamp(row, firstRow, lastRow);
            position.y = (row + 0.5f - stageArea.Rows * 0.5f) * cellSize;
            float currentSpeed = moveSpeed;
            if (keyboard != null && keyboard.aKey.isPressed)
            {
                currentSpeed *= speedUpMultiplier;
            }

            position.x -= currentSpeed * cellSize * deltaTime;

            // stop when the leftmost brick reaches the stage boundary.
            float leftLimit = (-stageArea.Columns * 0.5f - minimumX + 0.5f) * cellSize;
            position.x = Mathf.Max(position.x, leftLimit);
            transform.position = stageArea.transform.TransformPoint(position);
        }

        private void TryRotate(int direction)
        {
            if (stageArea == null || stageArea.CellSize <= 0f)
            {
                return;
            }

            int nextRotation = (rotationSteps + direction + 4) % 4;
            float cellSize = stageArea.CellSize;
            Vector3 position = stageArea.transform.InverseTransformPoint(transform.position) / cellSize;
            float halfWidth = stageArea.Columns * 0.5f;
            float halfHeight = stageArea.Rows * 0.5f;

            for (int i = 0; i < 4; i++)
            {
                Vector2Int cell = GetRotatedCell(i, nextRotation);
                float x = position.x + cell.x;
                float y = position.y + cell.y;
                if (x - 0.5f < -halfWidth - 0.0001f || x + 0.5f > halfWidth + 0.0001f ||
                    y - 0.5f < -halfHeight - 0.0001f || y + 0.5f > halfHeight + 0.0001f)
                {
                    return;
                }
            }

            rotationSteps = nextRotation;
        }

        private Vector2Int GetRotatedCell(int index, int steps)
        {
            Vector2Int cell = ShapeCells[(int)shape][index];
            if (shape == Shape.O)
            {
                return cell;
            }

            // the I-piece turns around a point between cells to stay grid-aligned.
            Vector2 pivot = shape == Shape.I ? new Vector2(0.5f, 0.5f) : Vector2.zero;
            Vector2 offset = (Vector2)cell - pivot;
            for (int i = 0; i < steps; i++)
            {
                offset = new Vector2(offset.y, -offset.x);
            }

            return Vector2Int.RoundToInt(offset + pivot);
        }

        private void UpdateLayout()
        {
            if (stageArea == null || bricks == null || bricks.Length != 4)
            {
                return;
            }

            float cellSize = stageArea.CellSize;
            if (cellSize <= 0f)
            {
                return;
            }

            for (int i = 0; i < bricks.Length; i++)
            {
                if (bricks[i] == null)
                {
                    continue;
                }

                Vector2Int cell = GetRotatedCell(i, rotationSteps);
                bricks[i].transform.localPosition = new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);
                bricks[i].FitToCell(cellSize);
            }
        }
    }
}

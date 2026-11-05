using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class Tetromino : MonoBehaviour
    {
        public enum Shape { I, O, T, S, Z, J, L }

        [SerializeField] private Shape shape = Shape.T;
        [FormerlySerializedAs("moveSpeed")]
        [SerializeField, Min(0f), Tooltip("Extra leftward speed relative to the bridge, in cells per second.")]
        private float relativeMoveSpeed = 2f;
        [SerializeField, Min(1f), Tooltip("Speed multiplier while A is held.")]
        private float speedUpMultiplier = 3f;
        [SerializeField] private StageArea stageArea;
        [SerializeField] private Brick[] bricks;

        private int rotationSteps;
        private bool isPlaced;
        private bool isDiscarded;
        private float lastMoveSpeed;
        private Vector2Int placedCell;
        private readonly HashSet<Vector2Int> occupiedCells = new();

        public bool IsPlaced => isPlaced;
        public bool IsDiscarded => isDiscarded;

        public void Initialize(StageArea stage, Shape selectedShape, int spawnRow, float spawnInset)
        {
            stageArea = stage;
            shape = selectedShape;
            rotationSteps = 0;
            isPlaced = false;
            isDiscarded = false;
            lastMoveSpeed = relativeMoveSpeed;

            Vector2Int[] cells = ShapeCells[(int)shape];
            int maximumX = cells[0].x;
            int minimumY = cells[0].y;
            int maximumY = cells[0].y;
            foreach (Vector2Int cell in cells)
            {
                maximumX = Mathf.Max(maximumX, cell.x);
                minimumY = Mathf.Min(minimumY, cell.y);
                maximumY = Mathf.Max(maximumY, cell.y);
            }

            int row = Mathf.Clamp(spawnRow, -minimumY, stageArea.Rows - 1 - maximumY);
            float column = stageArea.Columns - 1 - maximumX - spawnInset;
            float size = stageArea.CellSize;
            // spawning stays fixed to the stage while the bridge grid scrolls.
            transform.position = stageArea.transform.TransformPoint(new Vector3(
                (column + 0.5f - stageArea.Columns * 0.5f) * size,
                (row + 0.5f - stageArea.Rows * 0.5f) * size, 0f));
            UpdateLayout();
        }

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
            lastMoveSpeed = relativeMoveSpeed;
            UpdateLayout();
        }

        private void Update()
        {
            if (Application.IsPlaying(gameObject))
            {
                // apply the bridge displacement before evaluating motion relative to its grid.
                if (!isPlaced && stageArea != null)
                {
                    transform.position += stageArea.transform.TransformVector(Vector3.right * stageArea.BridgeFrameDisplacement);
                }

                if (isDiscarded && stageArea != null)
                {
                    transform.position += stageArea.transform.TransformVector(Vector3.left * (lastMoveSpeed * stageArea.CellSize * Time.deltaTime));
                }
                else
                {
                    UpdateMovement(Time.deltaTime);
                }
            }

            UpdateLayout();

            if (Application.IsPlaying(gameObject))
            {
                UpdateDiscardState();
            }
        }

        private void OnValidate()
        {
            relativeMoveSpeed = Mathf.Max(0f, relativeMoveSpeed);
            speedUpMultiplier = Mathf.Max(1f, speedUpMultiplier);
            UpdateLayout();
        }

        private void UpdateMovement(float deltaTime)
        {
            if (isPlaced || isDiscarded || stageArea == null || stageArea.CellSize <= 0f)
            {
                return;
            }

            float cellSize = stageArea.CellSize;
            RefreshOccupiedCells();
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

            Vector3 origin = GetGridOrigin();
            float halfHeight = stageArea.Rows * cellSize * 0.5f;
            int firstRow = Mathf.CeilToInt((-halfHeight - origin.y) / cellSize + 0.5f - minimumY - 0.0001f);
            int lastRow = Mathf.FloorToInt((halfHeight - origin.y) / cellSize - 0.5f - maximumY + 0.0001f);
            if (firstRow > lastRow)
            {
                return;
            }

            Vector3 position = stageArea.transform.InverseTransformPoint(transform.position);
            int previousRow = Mathf.RoundToInt((position.y - origin.y) / cellSize);
            int row = previousRow;
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
            Vector3 adjustedPosition = position;
            adjustedPosition.y = origin.y + row * cellSize;
            if (OverlapsOccupiedCells(adjustedPosition, rotationSteps))
            {
                row = previousRow;
                adjustedPosition.y = origin.y + row * cellSize;
            }

            position = adjustedPosition;
            float currentSpeed = relativeMoveSpeed;
            if (keyboard != null && keyboard.aKey.isPressed)
            {
                currentSpeed *= speedUpMultiplier;
            }

            float nextX = position.x - currentSpeed * cellSize * deltaTime;
            lastMoveSpeed = currentSpeed;
            float attachmentEndX = nextX;
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                float screenLeft = GetLeftScreenEdge(mainCamera);
                Vector3 leftEdgePosition = new Vector3(screenLeft, transform.position.y, transform.position.z);
                float localScreenLeft = stageArea.transform.InverseTransformPoint(leftEdgePosition).x;
                float discardX = localScreenLeft - (minimumX - 0.5f) * cellSize;
                attachmentEndX = Mathf.Max(attachmentEndX, discardX);
            }

            if (TryFindAttachment((position.x - origin.x) / cellSize,
                (attachmentEndX - origin.x) / cellSize, row, out Vector2Int placement))
            {
                placedCell = placement;
                isPlaced = true;
                position = origin + new Vector3(placement.x * cellSize, placement.y * cellSize, 0f);
                if (stageArea.BridgeRoot != null)
                {
                    transform.SetParent(stageArea.BridgeRoot, true);
                }
            }
            else
            {
                position.x = nextX;
            }

            transform.position = stageArea.transform.TransformPoint(position);
        }

        private float GetLeftScreenEdge(Camera camera)
        {
            float depth = camera.WorldToViewportPoint(transform.position).z;
            return camera.ViewportToWorldPoint(new Vector3(0f, 0.5f, depth)).x;
        }

        private void UpdateDiscardState()
        {
            Camera mainCamera = Camera.main;
            if (isPlaced || mainCamera == null || bricks == null)
            {
                return;
            }

            float leftEdge = GetLeftScreenEdge(mainCamera);
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool hasBounds = false;
            foreach (Brick brick in bricks)
            {
                if (brick == null)
                {
                    continue;
                }

                Bounds brickBounds = brick.GetComponent<SpriteRenderer>().bounds;
                if (!hasBounds)
                {
                    bounds = brickBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(brickBounds);
                }
            }

            if (!hasBounds)
            {
                return;
            }

            if (bounds.min.x < leftEdge)
            {
                isDiscarded = true;
            }

            // keep the discarded piece visible until its trailing edge leaves the camera.
            if (isDiscarded && bounds.max.x < leftEdge)
            {
                Destroy(gameObject);
            }
        }

        private Vector3 GetGridOrigin()
        {
            float cellSize = stageArea.CellSize;
            return new Vector3((0.5f - stageArea.Columns * 0.5f) * cellSize + stageArea.BridgeOffset,
                (0.5f - stageArea.Rows * 0.5f) * cellSize, 0f);
        }

        private void RefreshOccupiedCells()
        {
            occupiedCells.Clear();
            foreach (Brick brick in Brick.ActiveBricks)
            {
                if (brick == null || brick.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                Tetromino owner = brick.GetComponentInParent<Tetromino>();
                if (owner != null)
                {
                    if (owner == this || !owner.IsPlaced || owner.stageArea != stageArea)
                    {
                        continue;
                    }

                    owner.UpdateLayout();
                    occupiedCells.Add(stageArea.WorldPositionToCell(brick.transform.position));
                }
                else if (brick.StageArea == stageArea)
                {
                    occupiedCells.Add(brick.GetGridCell());
                }
            }
        }

        private bool OverlapsOccupiedCells(Vector3 position, int rotation)
        {
            Vector3 offset = (position - GetGridOrigin()) / stageArea.CellSize;
            for (int i = 0; i < 4; i++)
            {
                Vector2Int cell = GetRotatedCell(i, rotation);
                foreach (Vector2Int occupiedCell in occupiedCells)
                {
                    if (Mathf.Abs(offset.x + cell.x - occupiedCell.x) < 0.9999f &&
                        Mathf.Abs(offset.y + cell.y - occupiedCell.y) < 0.9999f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryFindAttachment(float fromX, float toX, int row, out Vector2Int placement)
        {
            placement = default;
            if (occupiedCells.Count == 0)
            {
                return false;
            }

            bool found = false;
            Vector3 origin = GetGridOrigin();
            float cellSize = stageArea.CellSize;
            float halfHeight = stageArea.Rows * cellSize * 0.5f;

            // examine every grid position that shares an edge with the existing structure.
            foreach (Vector2Int existingCell in occupiedCells)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2Int cell = GetRotatedCell(i, rotationSteps);
                    for (int side = 0; side < 4; side++)
                    {
                        Vector2Int neighbor = side switch
                        {
                            0 => Vector2Int.right,
                            1 => Vector2Int.up,
                            2 => Vector2Int.left,
                            _ => Vector2Int.down
                        };
                        Vector2Int candidate = existingCell + neighbor - cell;
                        if (candidate.y != row || candidate.x > fromX + 0.0001f || candidate.x < toX - 0.0001f ||
                            (found && candidate.x <= placement.x))
                        {
                            continue;
                        }

                        bool valid = true;
                        for (int j = 0; j < 4; j++)
                        {
                            Vector2Int occupiedCell = candidate + GetRotatedCell(j, rotationSteps);
                            float y = origin.y + occupiedCell.y * cellSize;
                            if (occupiedCells.Contains(occupiedCell) ||
                                y - cellSize * 0.5f < -halfHeight - 0.0001f ||
                                y + cellSize * 0.5f > halfHeight + 0.0001f)
                            {
                                valid = false;
                                break;
                            }
                        }

                        if (valid)
                        {
                            placement = candidate;
                            found = true;
                        }
                    }
                }
            }

            return found;
        }

        private void TryRotate(int direction)
        {
            if (isPlaced || isDiscarded || stageArea == null || stageArea.CellSize <= 0f)
            {
                return;
            }

            int nextRotation = (rotationSteps + direction + 4) % 4;
            RefreshOccupiedCells();
            float cellSize = stageArea.CellSize;
            Vector3 position = stageArea.transform.InverseTransformPoint(transform.position) / cellSize;
            float halfWidth = stageArea.Columns * 0.5f;
            float halfHeight = stageArea.Rows * 0.5f;

            for (int i = 0; i < 4; i++)
            {
                Vector2Int cell = GetRotatedCell(i, nextRotation);
                float x = position.x + cell.x;
                float y = position.y + cell.y;
                if (x + 0.5f > halfWidth + 0.0001f ||
                    y - 0.5f < -halfHeight - 0.0001f || y + 0.5f > halfHeight + 0.0001f)
                {
                    return;
                }
            }

            Vector3 localPosition = stageArea.transform.InverseTransformPoint(transform.position);
            if (OverlapsOccupiedCells(localPosition, nextRotation))
            {
                return;
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

            if (isPlaced)
            {
                Vector3 origin = GetGridOrigin();
                transform.position = stageArea.transform.TransformPoint(origin +
                    new Vector3(placedCell.x * cellSize, placedCell.y * cellSize, 0f));
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

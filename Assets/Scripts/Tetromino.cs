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
        [SerializeField, Tooltip("Instantly place at the ghost position, or discard if no placement is available.")]
        private Key hardDropKey = Key.Space;
        [SerializeField, Range(0f, 1f)] private float ghostOpacity = 0.2f;
        [Header("Vertical Input Repeat")]
        [SerializeField, Min(0f), Tooltip("Seconds before held Up/Down or W/S starts repeating.")]
        private float verticalRepeatDelay = 0.25f;
        [SerializeField, Min(0.01f), Tooltip("Seconds between repeated vertical moves while a direction is held.")]
        private float verticalRepeatInterval = 0.08f;
        [Header("Shape Colors")]
        [SerializeField] private Color iColor = new Color32(0x28, 0xC8, 0xDF, 0xFF);
        [SerializeField] private Color oColor = new Color32(0xE9, 0xC8, 0x42, 0xFF);
        [SerializeField] private Color tColor = new Color32(0xA6, 0x6A, 0xE3, 0xFF);
        [SerializeField] private Color sColor = new Color32(0x56, 0xC7, 0x77, 0xFF);
        [SerializeField] private Color zColor = new Color32(0xE6, 0x5C, 0x63, 0xFF);
        [SerializeField] private Color jColor = new Color32(0x52, 0x7B, 0xDA, 0xFF);
        [SerializeField] private Color lColor = new Color32(0xE9, 0x9A, 0x47, 0xFF);
        [Header("References")]
        [SerializeField] private StageArea stageArea;
        [SerializeField] private Brick[] bricks;

        private int rotationSteps;
        private bool isPlaced;
        private bool isDiscarded;
        private float lastMoveSpeed;
        private Vector2Int placedCell;
        private readonly HashSet<Vector2Int> occupiedCells = new();
        private readonly List<Collider2D> placementOverlaps = new();
        private readonly Vector2Int[] ghostCells = new Vector2Int[4];
        private TetrominoGhost ghost;
        private int heldVerticalDirection;
        private float verticalRepeatRemaining;
        private TetrominoAudio soundEffects;
        private bool wasSoftDropping;
        private float softDropDistance;

        public bool IsPlaced => isPlaced;
        public bool IsDiscarded => isDiscarded;
        internal StageArea StageArea => stageArea;

        internal void SetSoundEffects(TetrominoAudio audio)
        {
            soundEffects = audio;
        }

        public void Initialize(StageArea stage, Shape selectedShape, int spawnRow, float spawnInset)
        {
            stageArea = stage;
            shape = selectedShape;
            rotationSteps = 0;
            isPlaced = false;
            isDiscarded = false;
            lastMoveSpeed = relativeMoveSpeed;
            heldVerticalDirection = 0;
            verticalRepeatRemaining = 0f;
            wasSoftDropping = false;
            softDropDistance = 0f;

            transform.position = stage.transform.TransformPoint(
                GetSpawnPosition(stage, shape, spawnRow, spawnInset, out _));
            UpdateLayout();
            UpdateGhost();
        }

        internal static Vector3 GetSpawnPosition(StageArea stage, Shape selectedShape, int spawnRow,
            float spawnInset, out Bounds bounds)
        {
            Vector2Int[] cells = ShapeCells[(int)selectedShape];
            int minimumX = cells[0].x;
            int maximumX = cells[0].x;
            int minimumY = cells[0].y;
            int maximumY = cells[0].y;
            foreach (Vector2Int cell in cells)
            {
                minimumX = Mathf.Min(minimumX, cell.x);
                maximumX = Mathf.Max(maximumX, cell.x);
                minimumY = Mathf.Min(minimumY, cell.y);
                maximumY = Mathf.Max(maximumY, cell.y);
            }

            int row = Mathf.Clamp(spawnRow, -minimumY, stage.Rows - 1 - maximumY);
            float column = stage.Columns - 1 - maximumX - spawnInset;
            float size = stage.CellSize;
            // spawning stays fixed to the stage while the bridge grid scrolls.
            Vector3 position = new Vector3(
                (column + 0.5f - stage.Columns * 0.5f) * size,
                (row + 0.5f - stage.Rows * 0.5f) * size, 0f);
            bounds = new Bounds(position + new Vector3(
                (minimumX + maximumX) * 0.5f * size,
                (minimumY + maximumY) * 0.5f * size, 0f),
                new Vector3((maximumX - minimumX + 1) * size, (maximumY - minimumY + 1) * size, 0f));
            return position;
        }

        public void AssignHazards(int count, Brick hazardPrefab)
        {
            if (count <= 0 || hazardPrefab == null || bricks == null || bricks.Length != 4)
            {
                return;
            }

            count = Mathf.Min(count, 2);
            int[] indices = { 0, 1, 2, 3 };
            for (int i = 0; i < count; i++)
            {
                // Partial shuffle selects distinct bricks with equal probability.
                int selected = Random.Range(i, indices.Length);
                int index = indices[selected];
                indices[selected] = indices[i];
                indices[i] = index;
                Brick original = bricks[index];
                bricks[index] = Instantiate(hazardPrefab, transform);
                original.gameObject.SetActive(false);
                Destroy(original.gameObject);
            }

            // The ghost caches renderer references, so rebuild it after replacing bricks.
            if (ghost != null)
            {
                ghost.gameObject.SetActive(false);
                Destroy(ghost.gameObject);
                ghost = null;
            }

            UpdateLayout();
            UpdateGhost();
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
            ghostOpacity = Mathf.Clamp01(ghostOpacity);
            verticalRepeatDelay = Mathf.Max(0f, verticalRepeatDelay);
            verticalRepeatInterval = Mathf.Max(0.01f, verticalRepeatInterval);
            UpdateLayout();
        }

        private void LateUpdate()
        {
            UpdateGhost();
        }

        private void UpdateGhost()
        {
            if (!Application.IsPlaying(gameObject))
            {
                return;
            }

            if (isPlaced || isDiscarded)
            {
                if (ghost != null)
                {
                    Destroy(ghost.gameObject);
                    ghost = null;
                }

                return;
            }

            if (stageArea == null || stageArea.CellSize <= 0f || bricks == null || bricks.Length != 4 || ghostOpacity <= 0f)
            {
                if (ghost != null)
                {
                    ghost.Hide();
                }

                return;
            }

            RefreshOccupiedCells();
            float size = stageArea.CellSize;
            for (int i = 0; i < 4; i++)
            {
                ghostCells[i] = GetRotatedCell(i, rotationSteps);
            }

            if (!TryGetDropPlacement(out Vector2Int placement))
            {
                if (ghost != null)
                {
                    ghost.Hide();
                }

                return;
            }

            if (ghost == null)
            {
                GameObject ghostObject = new GameObject("Placement Ghost");
                ghostObject.transform.SetParent(transform, false);
                ghost = ghostObject.AddComponent<TetrominoGhost>();
                ghost.Initialize(bricks);
            }

            ghost.Show(stageArea.CellToWorldPosition(placement), stageArea.transform.rotation, size, ghostCells, ghostOpacity);
        }

        private bool TryGetDropPlacement(out Vector2Int placement)
        {
            float size = stageArea.CellSize;
            Vector3 origin = GetGridOrigin();
            Vector3 position = stageArea.transform.InverseTransformPoint(transform.position);
            int row = Mathf.RoundToInt((position.y - origin.y) / size);
            int minimumX = GetRotatedCell(0, rotationSteps).x;
            for (int i = 1; i < 4; i++)
            {
                minimumX = Mathf.Min(minimumX, GetRotatedCell(i, rotationSteps).x);
            }

            float endX = GetAttachmentEndX(float.NegativeInfinity, minimumX);
            return TryFindAttachment((position.x - origin.x) / size,
                (endX - origin.x) / size, row, out placement);
        }

        private void HardDrop()
        {
            if (isPlaced || isDiscarded || stageArea == null || stageArea.CellSize <= 0f)
            {
                return;
            }

            soundEffects?.PlayHardDrop();
            RefreshOccupiedCells();
            if (TryGetDropPlacement(out Vector2Int placement))
            {
                PlaceAtCell(placement);
                return;
            }

            isDiscarded = true;
            Destroy(gameObject);
        }

        private void UpdateMovement(float deltaTime)
        {
            if (Time.timeScale <= 0f || isPlaced || isDiscarded || stageArea == null || stageArea.CellSize <= 0f)
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
            int verticalInput = ReadVerticalInput(keyboard, deltaTime);
            bool attachPressed = keyboard != null &&
                ((verticalInput > 0 && (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)) ||
                 (verticalInput < 0 && (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)));

            // Connect only on a fresh press from an already adjacent row, never on a move or held repeat.
            if (attachPressed && TryAttachVertically(position, previousRow, verticalInput))
            {
                return;
            }

            int row = previousRow + verticalInput;
            row = Mathf.Clamp(row, firstRow, lastRow);
            Vector3 adjustedPosition = position;
            adjustedPosition.y = origin.y + row * cellSize;
            if (OverlapsOccupiedCells(adjustedPosition, rotationSteps))
            {
                row = previousRow;
                adjustedPosition.y = origin.y + row * cellSize;
            }

            position = adjustedPosition;
            if (row != previousRow)
            {
                soundEffects?.PlayMove();
            }
            if (keyboard != null && hardDropKey != Key.None && keyboard[hardDropKey].wasPressedThisFrame)
            {
                transform.position = stageArea.transform.TransformPoint(position);
                HardDrop();
                return;
            }

            float currentSpeed = relativeMoveSpeed;
            bool softDropping = keyboard != null && keyboard.aKey.isPressed;
            if (softDropping)
            {
                currentSpeed *= speedUpMultiplier;
            }

            float previousX = position.x;
            float nextX = position.x - currentSpeed * cellSize * deltaTime;
            lastMoveSpeed = currentSpeed;
            float attachmentEndX = GetAttachmentEndX(nextX, minimumX);

            if (TryFindAttachment((position.x - origin.x) / cellSize,
                (attachmentEndX - origin.x) / cellSize, row, out Vector2Int placement, checkPlayer: false))
            {
                if (CanPlaceAtCell(placement))
                {
                    PlaceAtCell(placement);
                    return;
                }

                // An unsafe lock must stop at the bridge rather than pass through its occupied cells.
                position.x = origin.x + placement.x * cellSize;
            }
            else
            {
                position.x = nextX;
            }

            transform.position = stageArea.transform.TransformPoint(position);
            float travelled = Mathf.Max(0f, (previousX - position.x) / cellSize);
            if (softDropping && travelled > 0f)
            {
                softDropDistance += travelled;
                if (!wasSoftDropping || softDropDistance >= 1f)
                {
                    soundEffects?.PlaySoftDrop();
                    softDropDistance = wasSoftDropping ? softDropDistance % 1f : 0f;
                }
            }
            else
            {
                softDropDistance = 0f;
            }
            wasSoftDropping = softDropping && travelled > 0f;
        }

        private int ReadVerticalInput(Keyboard keyboard, float deltaTime)
        {
            bool upPressed = keyboard != null && (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame);
            bool downPressed = keyboard != null && (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame);
            bool upHeld = upPressed || (keyboard != null && (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed));
            bool downHeld = downPressed || (keyboard != null && (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed));
            int direction = (upHeld ? 1 : 0) - (downHeld ? 1 : 0);
            if (direction == 0)
            {
                heldVerticalDirection = 0;
                verticalRepeatRemaining = 0f;
                return 0;
            }

            if (direction != heldVerticalDirection || (direction > 0 ? upPressed : downPressed))
            {
                heldVerticalDirection = direction;
                verticalRepeatRemaining = verticalRepeatDelay;
                return direction;
            }

            verticalRepeatRemaining -= deltaTime;
            if (verticalRepeatRemaining > 0f)
            {
                return 0;
            }

            // Emit at most one move per frame, even after a long frame or a pause.
            verticalRepeatRemaining = verticalRepeatInterval;
            return direction;
        }

        private float GetAttachmentEndX(float requestedX, int minimumX)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                float screenLeft = GetLeftScreenEdge(mainCamera);
                Vector3 leftEdgePosition = new Vector3(screenLeft, transform.position.y, transform.position.z);
                float localScreenLeft = stageArea.transform.InverseTransformPoint(leftEdgePosition).x;
                float discardX = localScreenLeft - (minimumX - 0.5f) * stageArea.CellSize;
                return Mathf.Max(requestedX, discardX);
            }

            return requestedX;
        }

        private void PlaceAtCell(Vector2Int cell)
        {
            placedCell = cell;
            isPlaced = true;
            if (stageArea.BridgeRoot != null)
            {
                transform.SetParent(stageArea.BridgeRoot, true);
            }

            transform.position = stageArea.CellToWorldPosition(cell);
            soundEffects?.PlayConnect();
        }

        private bool CanPlaceAtCell(Vector2Int position, bool checkPlayer = true)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector2Int cell = position + GetRotatedCell(i, rotationSteps);
                if (occupiedCells.Contains(cell) || cell.y < 0 || cell.y >= stageArea.Rows ||
                    (checkPlayer && OverlapsPlayer(cell)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool OverlapsPlayer(Vector2Int cell)
        {
            // Ignore exact edge contact so placing a supporting tile beneath the feet remains valid.
            float size = stageArea.CellSize * 0.9999f;
            Vector3 scale = stageArea.transform.lossyScale;
            Vector2 boxSize = new Vector2(size * Mathf.Abs(scale.x), size * Mathf.Abs(scale.y));
            Vector3 center = stageArea.CellToWorldPosition(cell);
            Vector3 diagonalA = stageArea.transform.TransformVector(new Vector3(size, size, 0f) * 0.5f);
            Vector3 diagonalB = stageArea.transform.TransformVector(new Vector3(size, -size, 0f) * 0.5f);
            Vector2 extents = new Vector2(Mathf.Max(Mathf.Abs(diagonalA.x), Mathf.Abs(diagonalB.x)),
                Mathf.Max(Mathf.Abs(diagonalA.y), Mathf.Abs(diagonalB.y)));
            ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
            int count = Physics2D.OverlapBox(center, boxSize,
                stageArea.transform.eulerAngles.z, filter, placementOverlaps);
            for (int i = 0; i < count; i++)
            {
                Collider2D overlap = placementOverlaps[i];
                if (overlap.gameObject.scene == gameObject.scene && overlap.GetComponentInParent<PlayerController>() != null)
                {
                    // Capsule queries also report their contact margin; require overlapping interiors.
                    Bounds bounds = overlap.bounds;
                    if (bounds.min.x < center.x + extents.x && bounds.max.x > center.x - extents.x &&
                        bounds.min.y < center.y + extents.y && bounds.max.y > center.y - extents.y)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryAttachVertically(Vector3 position, int row, int direction)
        {
            float cellSize = stageArea.CellSize;
            float column = (position.x - GetGridOrigin().x) / cellSize;
            // W/S always snaps to the nearest column; only placement validity can reject it.
            Vector2Int placement = new Vector2Int(Mathf.RoundToInt(column), row);
            if (!CanPlaceAtCell(placement))
            {
                return false;
            }

            Camera mainCamera = Camera.main;
            float screenLeft = mainCamera != null ? GetLeftScreenEdge(mainCamera) : float.NegativeInfinity;
            bool hasContact = false;
            Vector2Int contactDirection = direction > 0 ? Vector2Int.up : Vector2Int.down;
            for (int i = 0; i < 4; i++)
            {
                Vector2Int offset = GetRotatedCell(i, rotationSteps);
                Vector2Int cell = placement + offset;
                float currentLeft = stageArea.transform.TransformPoint(position +
                    new Vector3((offset.x - 0.5f) * cellSize, 0f, 0f)).x;
                float snappedLeft = stageArea.CellToWorldPosition(cell).x - cellSize * 0.5f;
                if (currentLeft < screenLeft || snappedLeft < screenLeft)
                {
                    return false;
                }

                if (occupiedCells.Contains(cell + contactDirection))
                {
                    hasContact = true;
                }
            }

            if (!hasContact)
            {
                return false;
            }

            PlaceAtCell(placement);
            return true;
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
            // Placement runs between physics steps; include the player's latest transform in overlap queries.
            Physics2D.SyncTransforms();
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

        private bool TryFindAttachment(float fromX, float toX, int row, out Vector2Int placement, bool checkPlayer = true)
        {
            placement = default;
            if (occupiedCells.Count == 0)
            {
                return false;
            }

            bool found = false;
            // leftward movement only locks against a brick directly to the left.
            foreach (Vector2Int existingCell in occupiedCells)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2Int cell = GetRotatedCell(i, rotationSteps);
                    Vector2Int candidate = existingCell + Vector2Int.right - cell;
                    if (candidate.y != row || candidate.x > fromX + 0.0001f || candidate.x < toX - 0.0001f ||
                        (found && candidate.x <= placement.x))
                    {
                        continue;
                    }

                    if (CanPlaceAtCell(candidate, checkPlayer: false))
                    {
                        placement = candidate;
                        found = true;
                    }
                }
            }

            return found && (!checkPlayer || CanPlaceAtCell(placement));
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
            soundEffects?.PlayRotate();
        }

        private Vector2Int GetRotatedCell(int index, int steps)
        {
            Vector2Int cell = ShapeCells[(int)shape][index];
            // I and O turn around a point between cells; O keeps its footprint while cycling hazard positions.
            Vector2 pivot = shape == Shape.I || shape == Shape.O ? new Vector2(0.5f, 0.5f) : Vector2.zero;
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

            Color shapeColor = shape switch
            {
                Shape.I => iColor,
                Shape.O => oColor,
                Shape.T => tColor,
                Shape.S => sColor,
                Shape.Z => zColor,
                Shape.J => jColor,
                _ => lColor
            };

            for (int i = 0; i < bricks.Length; i++)
            {
                if (bricks[i] == null)
                {
                    continue;
                }

                Vector2Int cell = GetRotatedCell(i, rotationSteps);
                bricks[i].transform.localPosition = new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);
                bricks[i].FitToCell(cellSize);
                bricks[i].SetColor(shapeColor);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class HazardClusterSpawner : MonoBehaviour
    {
        [SerializeField] private StageArea stageArea;
        [SerializeField] private Tetromino tetrominoPrefab;
        [SerializeField] private Brick hazardBrickPrefab;
        [SerializeField, Min(0f)] private float initialDelay = 8f;
        [SerializeField, Min(0.1f)] private float minimumSpawnInterval = 6f;
        [SerializeField, Min(0.1f)] private float maximumSpawnInterval = 10f;
        [SerializeField, Min(0)] private int minimumSpawnRow = 1;
        [SerializeField, Min(0)] private int maximumSpawnRow = 12;
        [SerializeField, Min(0f), Tooltip("Distance beyond the camera's right edge, in cells.")]
        private float spawnOffset = 2f;
        [SerializeField] private Color spawnGizmoColor = new Color(1f, 0.3f, 0.3f, 0.8f);

        private float spawnRemaining;
        private readonly HashSet<Vector2Int> occupiedCells = new();

        private void Start()
        {
            if (stageArea == null || tetrominoPrefab == null || hazardBrickPrefab == null)
            {
                Debug.LogError("Assign the stage, Tetromino prefab and hazard brick prefab for hazard clusters.", this);
                enabled = false;
                return;
            }

            spawnRemaining = initialDelay;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || Time.deltaTime <= 0f || stageArea.CellSize <= 0f || Camera.main == null)
            {
                return;
            }

            spawnRemaining -= Time.deltaTime;
            if (spawnRemaining > 0f)
            {
                return;
            }

            // If the incoming region is crowded, wait instead of spawning overlapping terrain.
            spawnRemaining = TrySpawn()
                ? Random.Range(minimumSpawnInterval, maximumSpawnInterval) : 0.5f;
        }

        private bool TrySpawn()
        {
            Tetromino.Shape shape = (Tetromino.Shape)Random.Range(0, 7);
            int rotation = Random.Range(0, 4);
            Vector2Int minimum = Tetromino.GetShapeCell(shape, 0, rotation);
            Vector2Int maximum = minimum;
            for (int i = 1; i < 4; i++)
            {
                Vector2Int cell = Tetromino.GetShapeCell(shape, i, rotation);
                minimum = Vector2Int.Min(minimum, cell);
                maximum = Vector2Int.Max(maximum, cell);
            }

            int firstRow = Mathf.Max(minimumSpawnRow, -minimum.y);
            int lastRow = Mathf.Min(maximumSpawnRow, stageArea.Rows - 1 - maximum.y);
            if (firstRow > lastRow)
            {
                return false;
            }

            occupiedCells.Clear();
            foreach (Brick brick in Brick.ActiveBricks)
            {
                if (brick == null || brick.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                Tetromino owner = brick.GetComponentInParent<Tetromino>();
                if ((owner != null && owner.StageArea == stageArea) ||
                    (owner == null && brick.StageArea == stageArea))
                {
                    occupiedCells.Add(stageArea.WorldPositionToCell(brick.transform.position));
                    if (owner != null && !owner.IsPlaced)
                    {
                        // Incoming pieces can sit between columns; reserve both overlapping cells.
                        float columnPosition = (stageArea.transform.InverseTransformPoint(brick.transform.position).x -
                            stageArea.BridgeOffset) / stageArea.CellSize + stageArea.Columns * 0.5f - 0.5f;
                        int row = stageArea.WorldPositionToCell(brick.transform.position).y;
                        occupiedCells.Add(new Vector2Int(Mathf.FloorToInt(columnPosition), row));
                        occupiedCells.Add(new Vector2Int(Mathf.CeilToInt(columnPosition), row));
                    }
                }
            }

            float size = stageArea.CellSize;
            // Round outward on the moving bridge grid, with the entire shape beyond the screen.
            int column = Mathf.CeilToInt((GetSpawnX() - stageArea.BridgeOffset) / size +
                stageArea.Columns * 0.5f - 0.5f - minimum.x);
            int rowCount = lastRow - firstRow + 1;
            int startRow = Random.Range(0, rowCount);
            for (int attempt = 0; attempt < rowCount; attempt++)
            {
                Vector2Int origin = new Vector2Int(column, firstRow + (startRow + attempt) % rowCount);
                bool blocked = false;
                for (int i = 0; i < 4; i++)
                {
                    if (occupiedCells.Contains(origin + Tetromino.GetShapeCell(shape, i, rotation)))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (blocked)
                {
                    continue;
                }

                Tetromino cluster = Instantiate(tetrominoPrefab, stageArea.BridgeRoot);
                cluster.name = $"Hazard Cluster {shape}";
                cluster.InitializeHazardCluster(stageArea, shape, rotation, origin, hazardBrickPrefab);
                return true;
            }

            return false;
        }

        private float GetSpawnX()
        {
            Camera camera = Camera.main;
            float depth = camera.WorldToViewportPoint(stageArea.transform.position).z;
            Vector3 rightEdge = camera.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth));
            return stageArea.transform.InverseTransformPoint(rightEdge).x +
                (spawnOffset + 0.5f) * stageArea.CellSize;
        }

        private void OnDrawGizmosSelected()
        {
            if (stageArea == null || Camera.main == null || stageArea.CellSize <= 0f)
            {
                return;
            }

            float size = stageArea.CellSize;
            float x = GetSpawnX();
            float bottom = (Mathf.Clamp(minimumSpawnRow, 0, stageArea.Rows - 1) - stageArea.Rows * 0.5f) * size;
            float top = (Mathf.Clamp(maximumSpawnRow, 0, stageArea.Rows - 1) + 1 - stageArea.Rows * 0.5f) * size;
            Color previous = Gizmos.color;
            Gizmos.color = spawnGizmoColor;
            Gizmos.DrawLine(stageArea.transform.TransformPoint(new Vector3(x, bottom, 0f)),
                stageArea.transform.TransformPoint(new Vector3(x, top, 0f)));
            Gizmos.color = previous;
        }

        private void OnValidate()
        {
            minimumSpawnInterval = Mathf.Max(0.1f, minimumSpawnInterval);
            maximumSpawnInterval = Mathf.Max(minimumSpawnInterval, maximumSpawnInterval);
            maximumSpawnRow = Mathf.Max(minimumSpawnRow, maximumSpawnRow);
        }
    }
}

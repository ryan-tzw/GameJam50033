using System.Collections.Generic;
using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class CoinSpawner : MonoBehaviour
    {
        [SerializeField] private StageArea stageArea;
        [SerializeField] private RunScore runScore;
        [SerializeField] private CoinPickup coinPrefab;
        [SerializeField, Min(0f)] private float initialDelay = 1f;
        [SerializeField, Min(0.1f)] private float minimumSpawnInterval = 3f;
        [SerializeField, Min(0.1f)] private float maximumSpawnInterval = 5f;
        [SerializeField, Min(0)] private int minimumSpawnRow = 3;
        [SerializeField, Min(0)] private int maximumSpawnRow = 12;
        [SerializeField, Min(0f), Tooltip("Distance beyond the camera's right edge, in tiles.")]
        private float spawnOffset = 2f;
        [Header("Pattern Weights (relative frequencies)")]
        [SerializeField, Min(0f)] private float lineWeight = 50f;
        [SerializeField, Min(0f)] private float singleOrPairWeight = 25f;
        [SerializeField, Min(0f)] private float tetrominoWeight = 15f;
        [SerializeField, Min(1)] private int minimumLineLength = 3;
        [SerializeField, Min(1)] private int maximumLineLength = 6;

        private float spawnRemaining;
        private readonly List<Vector2Int> pattern = new();

        private void Start()
        {
            if (stageArea == null || runScore == null || coinPrefab == null)
            {
                Debug.LogError("Assign the stage, score and coin prefab for coin patterns.", this);
                enabled = false;
                return;
            }

            spawnRemaining = initialDelay;
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f || stageArea.CellSize <= 0f || Camera.main == null)
            {
                return;
            }

            spawnRemaining -= Time.deltaTime;
            if (spawnRemaining <= 0f)
            {
                TrySpawn();
                spawnRemaining = Random.Range(minimumSpawnInterval, maximumSpawnInterval);
            }
        }

        private void TrySpawn()
        {
            pattern.Clear();
            float total = lineWeight + singleOrPairWeight + tetrominoWeight;
            if (total <= 0f)
            {
                return;
            }

            float choice = Random.value * total;
            if (choice < lineWeight + singleOrPairWeight)
            {
                int count = choice < lineWeight ? Random.Range(minimumLineLength, maximumLineLength + 1)
                    : Random.Range(1, 3);
                for (int i = 0; i < count; i++)
                {
                    pattern.Add(new Vector2Int(i, 0));
                }
            }
            else
            {
                Tetromino.Shape shape = (Tetromino.Shape)Random.Range(0, 7);
                int rotation = Random.Range(0, 4);
                Vector2Int minimum = new Vector2Int(int.MaxValue, int.MaxValue);
                for (int i = 0; i < 4; i++)
                {
                    Vector2Int offset = Tetromino.GetShapeCell(shape, i, rotation);
                    pattern.Add(offset);
                    minimum = Vector2Int.Min(minimum, offset);
                }

                for (int i = 0; i < pattern.Count; i++)
                {
                    pattern[i] -= minimum;
                }
            }

            int height = 0;
            foreach (Vector2Int offset in pattern)
            {
                height = Mathf.Max(height, offset.y);
            }

            int firstRow = Mathf.Clamp(minimumSpawnRow, 0, stageArea.Rows - 1);
            int lastRow = Mathf.Min(maximumSpawnRow, stageArea.Rows - 1 - height);
            if (lastRow < firstRow)
            {
                return;
            }

            Camera camera = Camera.main;
            float depth = camera.WorldToViewportPoint(stageArea.transform.position).z;
            Vector3 right = camera.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth));
            float localX = stageArea.transform.InverseTransformPoint(right).x;
            int column = Mathf.CeilToInt((localX - stageArea.BridgeOffset) / stageArea.CellSize +
                stageArea.Columns * 0.5f + spawnOffset);
            Vector2Int origin = new Vector2Int(column, Random.Range(firstRow, lastRow + 1));
            // Keep each pattern intact; skip a group if any coin would start buried in terrain.
            foreach (Vector2Int offset in pattern)
            {
                Vector2Int cell = origin + offset;
                Vector3 world = stageArea.CellToWorldPosition(cell);
                bool blocked = false;
                foreach (Brick brick in Brick.ActiveBricks)
                {
                    if (brick != null && brick.gameObject.scene == gameObject.scene &&
                        Mathf.Abs(brick.transform.position.x - world.x) < stageArea.CellSize * 0.9f &&
                        Mathf.Abs(brick.transform.position.y - world.y) < stageArea.CellSize * 0.9f)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (blocked)
                {
                    return;
                }
            }

            foreach (Vector2Int offset in pattern)
            {
                CoinPickup coin = Instantiate(coinPrefab, stageArea.BridgeRoot);
                coin.Initialize(stageArea, runScore, origin + offset);
            }
        }

        private void OnValidate()
        {
            minimumSpawnInterval = Mathf.Max(0.1f, minimumSpawnInterval);
            maximumSpawnInterval = Mathf.Max(minimumSpawnInterval, maximumSpawnInterval);
            maximumSpawnRow = Mathf.Max(minimumSpawnRow, maximumSpawnRow);
            minimumLineLength = Mathf.Max(1, minimumLineLength);
            maximumLineLength = Mathf.Max(minimumLineLength, maximumLineLength);
        }
    }
}

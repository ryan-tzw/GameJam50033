using System.Collections.Generic;
using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class CoinSpawner : MonoBehaviour
    {
        [SerializeField] private SpawnSettings spawnSettings;
        private static readonly SpawnSettings.CoinsData defaultSpawnCoins = new();
        private SpawnSettings.CoinsData spawnCoins => spawnSettings != null ? spawnSettings.coins : defaultSpawnCoins;
        [SerializeField] private StageArea stageArea;
        [SerializeField] private RunScore runScore;

        private float spawnRemaining;
        private readonly List<Vector2Int> pattern = new();

        private void Start()
        {
            if (stageArea == null || runScore == null || spawnCoins.coinPrefab == null)
            {
                Debug.LogError("Assign the stage, score and coin prefab for coin patterns.", this);
                enabled = false;
                return;
            }

            spawnRemaining = spawnCoins.initialDelay;
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
                spawnRemaining = Random.Range(spawnCoins.minimumSpawnInterval, spawnCoins.maximumSpawnInterval);
            }
        }

        private void TrySpawn()
        {
            pattern.Clear();
            float total = spawnCoins.lineWeight + spawnCoins.singleOrPairWeight + spawnCoins.tetrominoWeight;
            if (total <= 0f)
            {
                return;
            }

            float choice = Random.value * total;
            if (choice < spawnCoins.lineWeight + spawnCoins.singleOrPairWeight)
            {
                int count = choice < spawnCoins.lineWeight ? Random.Range(spawnCoins.minimumLineLength, spawnCoins.maximumLineLength + 1)
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

            int firstRow = Mathf.Clamp(spawnCoins.minimumSpawnRow, 0, stageArea.Rows - 1);
            int lastRow = Mathf.Min(spawnCoins.maximumSpawnRow, stageArea.Rows - 1 - height);
            if (lastRow < firstRow)
            {
                return;
            }

            Camera camera = Camera.main;
            float depth = camera.WorldToViewportPoint(stageArea.transform.position).z;
            Vector3 right = camera.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth));
            float localX = stageArea.transform.InverseTransformPoint(right).x;
            int column = Mathf.CeilToInt((localX - stageArea.BridgeOffset) / stageArea.CellSize +
                stageArea.Columns * 0.5f + spawnCoins.spawnOffset);
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
                CoinPickup coin = Instantiate(spawnCoins.coinPrefab, stageArea.BridgeRoot);
                coin.Initialize(stageArea, runScore, origin + offset);
            }
        }

    }
}

using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class FloatingIslandSpawner : MonoBehaviour
    {
        [SerializeField] private SpawnSettings spawnSettings;
        [SerializeField] private StageArea stageArea;
        private float spawnRemaining;

        private void Start()
        {
            if (spawnSettings == null || stageArea == null || spawnSettings.islands.islandPrefab == null)
            {
                Debug.LogError("Assign the stage, spawn settings and floating island prefab.", this);
                enabled = false;
                return;
            }

            spawnRemaining = spawnSettings.islands.initialDelay;
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
                spawnRemaining = TrySpawn() ? Random.Range(spawnSettings.islands.minimumSpawnInterval,
                    spawnSettings.islands.maximumSpawnInterval) : 0.5f;
            }
        }

        private bool TrySpawn()
        {
            SpawnSettings.IslandsData settings = spawnSettings.islands;
            int firstRow = Mathf.Max(0, settings.minimumSpawnRow);
            int lastRow = Mathf.Min(settings.maximumSpawnRow, stageArea.Rows - 3);
            if (lastRow < firstRow)
            {
                return false;
            }

            Camera camera = Camera.main;
            float depth = camera.WorldToViewportPoint(stageArea.transform.position).z;
            float rightX = stageArea.transform.InverseTransformPoint(
                camera.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth))).x;
            int column = Mathf.CeilToInt((rightX - stageArea.BridgeOffset) / stageArea.CellSize +
                stageArea.Columns * 0.5f + settings.spawnOffset);
            int rowCount = lastRow - firstRow + 1;
            int startRow = Random.Range(0, rowCount);
            for (int attempt = 0; attempt < rowCount; attempt++)
            {
                Vector2Int origin = new Vector2Int(column, firstRow + (startRow + attempt) % rowCount);
                bool clear = true;
                // Six bricks plus the chest above the middle brick reserve one complete group.
                for (int i = 0; i < 7; i++)
                {
                    Vector2Int offset = i < 6 ? new Vector2Int(i % 3, i / 3) : new Vector2Int(1, 2);
                    if (!SpawnClearance.IsClear(stageArea, origin + offset, spawnSettings.minimumGroupGapInTiles))
                    {
                        clear = false;
                        break;
                    }
                }

                if (!clear)
                {
                    continue;
                }

                FloatingIsland island = Instantiate(settings.islandPrefab, stageArea.BridgeRoot);
                island.Initialize(stageArea, origin);
                return true;
            }

            return false;
        }

        [ContextMenu("Spawn Test Island Near Player")]
        public void SpawnTestIslandNearPlayer()
        {
            if (!Application.isPlaying || Time.timeScale <= 0f || stageArea == null ||
                stageArea.CellSize <= 0f || spawnSettings == null || spawnSettings.islands.islandPrefab == null)
            {
                return;
            }

            PlayerController player = null;
            foreach (PlayerController candidate in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (candidate.StageArea == stageArea)
                {
                    player = candidate;
                    break;
                }
            }
            if (player == null || Camera.main == null || stageArea.Rows < 3)
            {
                return;
            }

            Bounds playerBounds = player.GetComponent<Collider2D>().bounds;
            float size = stageArea.CellSize;
            Vector3 feet = new Vector3(playerBounds.max.x, playerBounds.min.y, playerBounds.center.z);
            Vector2Int first = stageArea.WorldPositionToCell(feet +
                stageArea.transform.TransformVector(new Vector3(1.5f, 0.5f, 0f) * size));
            first.y = Mathf.Clamp(first.y, 0, stageArea.Rows - 3);
            for (int column = 0; column < 8; column++)
            {
                Vector2Int origin = first + Vector2Int.right * column;
                Vector3 rightEdge = stageArea.CellToWorldPosition(origin + Vector2Int.right * 2) +
                    stageArea.transform.right * size * 0.5f;
                if (Camera.main.WorldToViewportPoint(rightEdge).x > 1f)
                {
                    break;
                }

                bool clear = true;
                for (int i = 0; i < 7; i++)
                {
                    Vector2Int offset = i < 6 ? new Vector2Int(i % 3, i / 3) : new Vector2Int(1, 2);
                    // Manual test placement may sit directly on the bridge, but must never overlap it.
                    if (!SpawnClearance.IsClear(stageArea, origin + offset, 0f))
                    {
                        clear = false;
                        break;
                    }
                }
                if (clear)
                {
                    FloatingIsland island = Instantiate(spawnSettings.islands.islandPrefab, stageArea.BridgeRoot);
                    island.name = "Test Island";
                    island.Initialize(stageArea, origin);
                    return;
                }
            }

            Debug.LogWarning("No clear space just ahead of the player. Try again on a flatter stretch of bridge.", this);
        }
    }
}

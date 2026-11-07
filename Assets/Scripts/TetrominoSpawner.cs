using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunDifficulty), typeof(TetrominoAudio))]
    public sealed class TetrominoSpawner : MonoBehaviour
    {
        [SerializeField] private SpawnSettings spawnSettings;
        private static readonly SpawnSettings.PiecesData defaultSpawnPieces = new();
        private SpawnSettings.PiecesData spawnPieces => spawnSettings != null ? spawnSettings.pieces : defaultSpawnPieces;
        [SerializeField] private StageArea stageArea;

        private Tetromino activePiece;
        private int preferredSpawnRow;
        private RunDifficulty difficulty;
        [SerializeField, Min(0), Tooltip("Successfully spawned pieces this run. Resets when restarting.")]
        private int spawnedCount;

        private void Awake()
        {
            difficulty = GetComponent<RunDifficulty>();
            spawnedCount = 0;
            preferredSpawnRow = spawnPieces.spawnRow;
        }

        private void Start()
        {
            if (stageArea == null || spawnPieces.tetrominoPrefab == null || spawnPieces.hazardBrickPrefab == null)
            {
                Debug.LogError("Assign the stage, Tetromino prefab and hazard brick prefab on the spawner.", this);
                enabled = false;
                return;
            }

            SpawnNext();
        }

        private void LateUpdate()
        {
            if (activePiece != null && activePiece.IsPlaced)
            {
                preferredSpawnRow = activePiece.PlacedRow;
            }

            if (activePiece == null || activePiece.IsPlaced || activePiece.IsDiscarded)
            {
                SpawnNext();
            }
        }

        private void SpawnNext()
        {
            if (Time.timeScale <= 0f || stageArea.CellSize <= 0f || !TryFindSpawnRow(out int row))
            {
                return;
            }

            Tetromino.Shape shape = (Tetromino.Shape)Random.Range(0, 7);
            int hazards = spawnedCount < spawnPieces.hazardFreePieces ? 0 : difficulty.RollHazardCount();
            activePiece = Instantiate(spawnPieces.tetrominoPrefab);
            activePiece.SetSoundEffects(GetComponent<TetrominoAudio>());
            activePiece.name = $"Tetromino {shape}";
            activePiece.Initialize(stageArea, shape, row, spawnPieces.spawnInset - spawnPieces.spawnRightOffset);
            activePiece.AssignHazards(hazards, spawnPieces.hazardBrickPrefab);
            spawnedCount++;
        }

        private bool TryFindSpawnRow(out int row)
        {
            row = Mathf.Clamp(preferredSpawnRow, 0, stageArea.Rows - 1);
            if (!IsSpawnRegionOccupied(GetSpawnRegion(row)))
            {
                return true;
            }

            // Keep supplying pieces when an obstacle passes through the usual spawn lane.
            int preferred = row;
            for (int distance = 1; distance < stageArea.Rows; distance++)
            {
                int above = preferred + distance;
                if (above < stageArea.Rows && !IsSpawnRegionOccupied(GetSpawnRegion(above)))
                {
                    row = above;
                    return true;
                }

                int below = preferred - distance;
                if (below >= 0 && !IsSpawnRegionOccupied(GetSpawnRegion(below)))
                {
                    row = below;
                    return true;
                }
            }

            return false;
        }

        private Bounds GetSpawnRegion(int row)
        {
            Tetromino.GetSpawnPosition(stageArea, Tetromino.Shape.I, row,
                spawnPieces.spawnInset - spawnPieces.spawnRightOffset, out Bounds region);
            // reserve enough room for any shape before choosing the next piece.
            for (int i = 1; i < 7; i++)
            {
                Tetromino.GetSpawnPosition(stageArea, (Tetromino.Shape)i, row,
                    spawnPieces.spawnInset - spawnPieces.spawnRightOffset, out Bounds bounds);
                region.Encapsulate(bounds);
            }

            return region;
        }

        private bool IsSpawnRegionOccupied(Bounds region)
        {
            float halfCell = stageArea.CellSize * 0.5f;
            PlacementEntryZone entryZone = stageArea.GetComponent<PlacementEntryZone>();
            foreach (Brick brick in Brick.ActiveBricks)
            {
                if (brick == null || brick.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                Tetromino owner = brick.GetComponentInParent<Tetromino>();
                Vector3 worldPosition;
                if (owner != null)
                {
                    if (!owner.IsPlaced || owner.StageArea != stageArea)
                    {
                        continue;
                    }

                    worldPosition = brick.transform.position;
                }
                else
                {
                    if (brick.StageArea != stageArea)
                    {
                        continue;
                    }

                    worldPosition = stageArea.CellToWorldPosition(brick.GetGridCell());
                }

                if (entryZone != null && !entryZone.IsActivePlacementPosition(worldPosition))
                {
                    continue;
                }

                Vector3 position = stageArea.transform.InverseTransformPoint(worldPosition);
                // touching edges are safe; only overlapping cell interiors block spawning.
                if (position.x + halfCell > region.min.x + 0.0001f &&
                    position.x - halfCell < region.max.x - 0.0001f &&
                    position.y + halfCell > region.min.y + 0.0001f &&
                    position.y - halfCell < region.max.y - 0.0001f)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (stageArea == null || stageArea.CellSize <= 0f)
            {
                return;
            }

            Bounds region = GetSpawnRegion(Application.isPlaying ? preferredSpawnRow : spawnPieces.spawnRow);
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = stageArea.transform.localToWorldMatrix;
            Gizmos.color = spawnPieces.spawnGizmoColor;
            Gizmos.DrawWireCube(region.center, region.size);
            Gizmos.matrix = previousMatrix;
        }

    }
}

using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class TetrominoSpawner : MonoBehaviour
    {
        [SerializeField] private StageArea stageArea;
        [SerializeField] private Tetromino tetrominoPrefab;
        [SerializeField, Min(0), Tooltip("Starting row, counted from the bottom of the stage.")]
        private int spawnRow = 5;
        [SerializeField, Min(0f), Tooltip("Space from the stage's right edge, measured in cells.")]
        private float spawnInset = 0.5f;
        [SerializeField, Min(0f), Tooltip("Move the spawn area farther right, measured in cells.")]
        private float spawnRightOffset = 1.5f;
        [SerializeField] private Color spawnGizmoColor = new Color(0.3f, 1f, 1f, 0.7f);

        private Tetromino activePiece;

        private void Start()
        {
            if (stageArea == null || tetrominoPrefab == null)
            {
                Debug.LogError("Assign the stage and Tetromino prefab on the spawner.", this);
                enabled = false;
                return;
            }

            SpawnNext();
        }

        private void LateUpdate()
        {
            if (activePiece == null || activePiece.IsPlaced || activePiece.IsDiscarded)
            {
                SpawnNext();
            }
        }

        private void SpawnNext()
        {
            if (stageArea.CellSize <= 0f || IsSpawnRegionOccupied(GetSpawnRegion()))
            {
                return;
            }

            Tetromino.Shape shape = (Tetromino.Shape)Random.Range(0, 7);
            activePiece = Instantiate(tetrominoPrefab);
            activePiece.name = $"Tetromino {shape}";
            activePiece.Initialize(stageArea, shape, spawnRow, spawnInset - spawnRightOffset);
        }

        private Bounds GetSpawnRegion()
        {
            Tetromino.GetSpawnPosition(stageArea, Tetromino.Shape.I, spawnRow,
                spawnInset - spawnRightOffset, out Bounds region);
            // reserve enough room for any shape before choosing the next piece.
            for (int i = 1; i < 7; i++)
            {
                Tetromino.GetSpawnPosition(stageArea, (Tetromino.Shape)i, spawnRow,
                    spawnInset - spawnRightOffset, out Bounds bounds);
                region.Encapsulate(bounds);
            }

            return region;
        }

        private bool IsSpawnRegionOccupied(Bounds region)
        {
            float halfCell = stageArea.CellSize * 0.5f;
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

            Bounds region = GetSpawnRegion();
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = stageArea.transform.localToWorldMatrix;
            Gizmos.color = spawnGizmoColor;
            Gizmos.DrawWireCube(region.center, region.size);
            Gizmos.matrix = previousMatrix;
        }

        private void OnValidate()
        {
            spawnRow = Mathf.Max(0, spawnRow);
            spawnInset = Mathf.Max(0f, spawnInset);
            spawnRightOffset = Mathf.Max(0f, spawnRightOffset);
        }
    }
}

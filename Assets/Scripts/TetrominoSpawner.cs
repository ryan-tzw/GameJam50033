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
            Tetromino.Shape shape = (Tetromino.Shape)Random.Range(0, 7);
            activePiece = Instantiate(tetrominoPrefab);
            activePiece.name = $"Tetromino {shape}";
            activePiece.Initialize(stageArea, shape, spawnRow, spawnInset);
        }

        private void OnValidate()
        {
            spawnRow = Mathf.Max(0, spawnRow);
            spawnInset = Mathf.Max(0f, spawnInset);
        }
    }
}

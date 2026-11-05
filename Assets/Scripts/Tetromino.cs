using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class Tetromino : MonoBehaviour
    {
        public enum Shape { I, O, T, S, Z, J, L }

        [SerializeField] private Shape shape = Shape.T;
        [SerializeField] private StageArea stageArea;
        [SerializeField] private Brick[] bricks;

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
            UpdateLayout();
        }

        private void OnValidate()
        {
            UpdateLayout();
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

            Vector2Int[] cells = ShapeCells[(int)shape];
            for (int i = 0; i < bricks.Length; i++)
            {
                if (bricks[i] == null)
                {
                    continue;
                }

                bricks[i].transform.localPosition = new Vector3(cells[i].x * cellSize, cells[i].y * cellSize, 0f);
                bricks[i].FitToCell(cellSize);
            }
        }
    }
}

using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Brick : MonoBehaviour
    {
        [SerializeField] private StageArea stageArea;
        [SerializeField] private Color color = Color.white;

        private SpriteRenderer spriteRenderer;

        private void OnEnable()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.color = color;

            if (stageArea == null || spriteRenderer.sprite == null)
            {
                return;
            }

            FitToCell(stageArea.CellSize);
        }

        public void FitToCell(float cellSize)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer.sprite == null)
            {
                return;
            }

            if (cellSize <= 0f)
            {
                return;
            }

            Vector3 spriteSize = spriteRenderer.sprite.bounds.size;
            transform.localScale = new Vector3(cellSize / spriteSize.x, cellSize / spriteSize.y, 1f);
        }
    }
}

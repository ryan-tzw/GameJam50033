using System.Collections.Generic;
using UnityEngine;

namespace Jam50033
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class Brick : MonoBehaviour
    {
        [SerializeField] private StageArea stageArea;
        [SerializeField] private Color color = Color.white;
        [SerializeField, Tooltip("Snap starting bricks to stage cells when you release a Scene-view drag.")]
        private bool snapInEditor = true;

        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;
        private bool gridCellInitialized;
        private Vector2Int gridCell;

        internal static readonly HashSet<Brick> ActiveBricks = new();
        public StageArea StageArea => stageArea;
        public bool SnapInEditor => snapInEditor;

        private void OnEnable()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            boxCollider = GetComponent<BoxCollider2D>();
            UpdateCollider();
            gridCellInitialized = false;
            ActiveBricks.Add(this);
        }

        private void OnDisable()
        {
            ActiveBricks.Remove(this);
        }

        private void Start()
        {
            if (Application.IsPlaying(gameObject) && stageArea != null && GetComponentInParent<Tetromino>() == null)
            {
                GetGridCell();
                if (stageArea.BridgeRoot != null)
                {
                    transform.SetParent(stageArea.BridgeRoot, true);
                }
            }
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

            if (Application.IsPlaying(gameObject) && GetComponentInParent<Tetromino>() == null)
            {
                transform.position = stageArea.CellToWorldPosition(GetGridCell());
            }
        }

        internal Vector2Int GetGridCell()
        {
            if (!gridCellInitialized)
            {
                gridCell = stageArea.WorldPositionToCell(transform.position);
                gridCellInitialized = true;
            }

            return gridCell;
        }

        public void SetColor(Color newColor)
        {
            color = newColor;
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            spriteRenderer.color = color;
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
            UpdateCollider();
        }

        private void UpdateCollider()
        {
            if (boxCollider == null || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            boxCollider.size = spriteRenderer.sprite.bounds.size;
            boxCollider.offset = spriteRenderer.sprite.bounds.center;
            Tetromino owner = GetComponentInParent<Tetromino>();
            // only bridge terrain is solid; incoming and discarded pieces remain visual.
            boxCollider.enabled = owner == null || owner.IsPlaced;
        }
    }
}

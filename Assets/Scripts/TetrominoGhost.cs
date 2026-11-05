using UnityEngine;

namespace Jam50033
{
    public sealed class TetrominoGhost : MonoBehaviour
    {
        private readonly SpriteRenderer[] sources = new SpriteRenderer[4];
        private readonly SpriteRenderer[] tiles = new SpriteRenderer[4];

        public void Initialize(Brick[] bricks)
        {
            for (int i = 0; i < 4; i++)
            {
                sources[i] = bricks[i] != null ? bricks[i].GetComponent<SpriteRenderer>() : null;
                GameObject tileObject = new GameObject($"Ghost Tile {i + 1}");
                tileObject.transform.SetParent(transform, false);
                tiles[i] = tileObject.AddComponent<SpriteRenderer>();
            }
        }

        public void Show(Vector3 position, Quaternion rotation, float cellSize, Vector2Int[] cells, float opacity)
        {
            transform.SetPositionAndRotation(position, rotation);
            gameObject.SetActive(true);
            for (int i = 0; i < 4; i++)
            {
                SpriteRenderer source = sources[i];
                SpriteRenderer tile = tiles[i];
                tile.enabled = source != null && source.sprite != null;
                if (!tile.enabled)
                {
                    continue;
                }

                tile.gameObject.layer = source.gameObject.layer;
                tile.sprite = source.sprite;
                tile.sharedMaterial = source.sharedMaterial;
                tile.sortingLayerID = source.sortingLayerID;
                tile.sortingOrder = source.sortingOrder - 1;
                tile.flipX = source.flipX;
                tile.flipY = source.flipY;
                Color color = source.color;
                color.a *= opacity;
                tile.color = color;
                Vector3 spriteSize = source.sprite.bounds.size;
                tile.transform.localPosition = new Vector3(cells[i].x * cellSize, cells[i].y * cellSize, 0f);
                tile.transform.localScale = new Vector3(cellSize / spriteSize.x, cellSize / spriteSize.y, 1f);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}

using UnityEngine;

namespace Jam50033
{
    public sealed class TetrominoGhost : MonoBehaviour
    {
        private readonly SpriteRenderer[] sources = new SpriteRenderer[4];
        private readonly SpriteRenderer[] tiles = new SpriteRenderer[4];
        private MaterialPropertyBlock properties;

        public void Initialize(Brick[] bricks)
        {
            properties = new MaterialPropertyBlock();
            for (int i = 0; i < 4; i++)
            {
                sources[i] = bricks[i] != null ? bricks[i].GetComponent<SpriteRenderer>() : null;
                GameObject tileObject = new GameObject($"Ghost Tile {i + 1}");
                tileObject.transform.SetParent(transform, false);
                tiles[i] = tileObject.AddComponent<SpriteRenderer>();
            }
        }

        public void Show(Vector3 position, Quaternion rotation, float cellSize, Vector2Int[] cells, float opacity,
            Color? tint = null)
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
                source.GetPropertyBlock(properties);
                if (tint.HasValue && source.sharedMaterial != null && source.sharedMaterial.HasProperty("_GlowStrength"))
                {
                    // Hazard sprites use shader colours instead of vertex RGB.
                    properties.SetColor("_BaseColor", tint.Value);
                    properties.SetColor("_GlowColor", tint.Value);
                    properties.SetColor("_SymbolColor", tint.Value);
                    properties.SetFloat("_GlowStrength", 1f);
                }
                tile.SetPropertyBlock(properties);
                tile.sortingLayerID = source.sortingLayerID;
                tile.sortingOrder = source.sortingOrder - 1;
                tile.flipX = source.flipX;
                tile.flipY = source.flipY;
                Color color = tint ?? source.color;
                color.a = source.color.a * opacity * (tint.HasValue ? tint.Value.a : 1f);
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

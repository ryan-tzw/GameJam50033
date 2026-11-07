using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class FloatingIsland : MonoBehaviour
    {
        [SerializeField] private VisualSettings visualSettings;
        [SerializeField] private Brick[] bricks;
        [SerializeField] private IslandChest chest;

        internal void Initialize(StageArea stage, Vector2Int origin)
        {
            transform.SetParent(stage.BridgeRoot, false);
            transform.position = stage.CellToWorldPosition(origin);
            for (int i = 0; i < bricks.Length; i++)
            {
                bricks[i].Initialize(stage, origin + new Vector2Int(i % 3, i / 3),
                    visualSettings.island.brickColor);
            }

            chest.Initialize(stage, origin + new Vector2Int(1, 2));
        }

        internal SpriteRenderer DetachChest()
        {
            return chest != null ? chest.DetachForCollapse() : null;
        }
    }
}

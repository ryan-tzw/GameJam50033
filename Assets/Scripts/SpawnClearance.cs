using UnityEngine;

namespace Jam50033
{
    internal static class SpawnClearance
    {
        internal static bool IsClear(StageArea stage, Vector2Int cell, float gapInTiles)
        {
            Vector3 position = stage.transform.InverseTransformPoint(stage.CellToWorldPosition(cell));
            float minimumSeparation = (1f + Mathf.Max(0f, gapInTiles)) * stage.CellSize;
            foreach (Brick brick in Brick.ActiveBricks)
            {
                if (brick == null || brick.gameObject.scene != stage.gameObject.scene)
                {
                    continue;
                }

                Tetromino owner = brick.GetComponentInParent<Tetromino>();
                if ((owner != null ? owner.StageArea : brick.StageArea) != stage)
                {
                    continue;
                }

                Vector3 other = stage.transform.InverseTransformPoint(brick.transform.position);
                if (TooClose(position, other, minimumSeparation))
                {
                    return false;
                }
            }

            foreach (CoinPickup coin in CoinPickup.ActiveCoins)
            {
                if (coin == null || coin.StageArea != stage)
                {
                    continue;
                }

                Vector3 other = stage.transform.InverseTransformPoint(coin.SpawnCheckPosition);
                if (TooClose(position, other, minimumSeparation))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TooClose(Vector3 a, Vector3 b, float separation)
        {
            // Reserve a full cell per object, including coins. Exact clearance is allowed.
            float tolerance = separation * 0.0001f;
            return Mathf.Abs(a.x - b.x) < separation - tolerance &&
                Mathf.Abs(a.y - b.y) < separation - tolerance;
        }
    }
}

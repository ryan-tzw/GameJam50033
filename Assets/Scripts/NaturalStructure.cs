using UnityEngine;

namespace Jam50033
{
    [DisallowMultipleComponent]
    public sealed class NaturalStructure : MonoBehaviour
    {
        private StageArea stage;
        private Brick[] bricks;

        internal void Initialize(StageArea stageArea, Brick[] structureBricks)
        {
            stage = stageArea;
            bricks = structureBricks;
        }

        internal bool HasCrossedEntryZone(PlacementEntryZone zone)
        {
            if (zone == null) return true;
            if (stage == null || bricks == null) return false;
            // Activate the entire structure together, using tile edges rather than cell centres.
            Vector3 halfTile = stage.transform.TransformVector(Vector3.right * stage.CellSize * 0.5f);
            foreach (Brick brick in bricks)
            {
                if (brick != null && !zone.IsActivePlacementPosition(brick.transform.position + halfTile))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

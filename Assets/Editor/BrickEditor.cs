using UnityEditor;
using UnityEngine;

namespace Jam50033
{
    [CustomEditor(typeof(Brick))]
    [CanEditMultipleObjects]
    public sealed class BrickEditor : Editor
    {
        private void OnSceneGUI()
        {
            if (Event.current.rawType != EventType.MouseUp || Event.current.button != 0)
            {
                return;
            }

            // wait for Unity's move handle to finish writing the transform.
            EditorApplication.delayCall -= SnapBricks;
            EditorApplication.delayCall += SnapBricks;
        }

        private void SnapBricks()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            foreach (Object selected in targets)
            {
                Brick brick = selected as Brick;
                if (brick == null || !brick.SnapInEditor || brick.StageArea == null ||
                    brick.StageArea.CellSize <= 0f || EditorUtility.IsPersistent(brick) ||
                    brick.GetComponentInParent<Tetromino>() != null)
                {
                    continue;
                }

                Vector2Int cell = brick.StageArea.WorldPositionToCell(brick.transform.position);
                Vector3 position = brick.StageArea.CellToWorldPosition(cell);
                if ((brick.transform.position - position).sqrMagnitude < 0.00000001f)
                {
                    continue;
                }

                Undo.RecordObject(brick.transform, "Snap bricks to stage grid");
                brick.transform.position = position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(brick.transform);
            }

            SceneView.RepaintAll();
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= SnapBricks;
        }
    }
}

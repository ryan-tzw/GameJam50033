using UnityEditor;
using UnityEngine;

namespace Jam50033
{
    [CustomEditor(typeof(FloatingIslandSpawner))]
    public sealed class FloatingIslandSpawnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying || Time.timeScale <= 0f))
            {
                if (GUILayout.Button("Spawn Test Island Near Player"))
                {
                    ((FloatingIslandSpawner)target).SpawnTestIslandNearPlayer();
                }
            }
        }
    }
}

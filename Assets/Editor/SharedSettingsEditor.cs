using UnityEditor;
using UnityEngine;

namespace Jam50033
{
    public class SharedSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (DrawDefaultInspector())
            {
                // Refresh ExecuteAlways previews when a shared asset changes.
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
        }
    }

    [CustomEditor(typeof(PlayerSettings))]
    public sealed class PlayerSettingsEditor : SharedSettingsEditor { }

    [CustomEditor(typeof(GameplaySettings))]
    public sealed class GameplaySettingsEditor : SharedSettingsEditor { }

    [CustomEditor(typeof(SpawnSettings))]
    public sealed class SpawnSettingsEditor : SharedSettingsEditor { }

    [CustomEditor(typeof(AudioSettings))]
    public sealed class AudioSettingsEditor : SharedSettingsEditor { }

    [CustomEditor(typeof(VisualSettings))]
    public sealed class VisualSettingsEditor : SharedSettingsEditor { }
}
